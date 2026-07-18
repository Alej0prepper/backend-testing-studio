using System.Net;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using BackendTestingStudio.Core.Reporting;
using BackendTestingStudio.Core.Scenarios;

namespace BackendTestingStudio.Reporting;

public sealed class ReportEngine : IReportEngine
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public ExecutionReport CreateReport(ScenarioExecutionResult execution)
    {
        ArgumentNullException.ThrowIfNull(execution);

        var steps = execution.Steps
            .Select(CreateStep)
            .ToArray();
        var assertions = execution.Steps
            .SelectMany(step => step.Assertions.Select(assertion => new ReportAssertion(
                step.Name,
                assertion.Name,
                assertion.Passed,
                assertion.Message,
                assertion.ActualValue,
                assertion.ExpectedValue)))
            .ToArray();
        var variables = execution.Variables
            .OrderBy(variable => variable.Key, StringComparer.OrdinalIgnoreCase)
            .Select(variable => new ReportVariable(variable.Key, variable.Value))
            .ToArray();
        var errors = execution.Steps
            .Where(step => !string.IsNullOrWhiteSpace(step.Error))
            .Select(step => new ReportError(step.Name, step.Error!))
            .ToArray();
        var summary = new ReportSummary(
            execution.Status.ToString(),
            execution.StartedAt,
            execution.ElapsedMilliseconds,
            steps.Length,
            steps.Count(step => step.Status == ScenarioStepStatus.Succeeded.ToString()),
            steps.Count(step => step.Status == ScenarioStepStatus.Failed.ToString()),
            steps.Count(step => step.Status == ScenarioStepStatus.Skipped.ToString()),
            assertions.Length,
            assertions.Count(assertion => assertion.Passed),
            assertions.Count(assertion => !assertion.Passed),
            execution.StoppedEarly);

        return new ExecutionReport(
            execution.ScenarioId,
            execution.ScenarioName,
            summary,
            steps,
            assertions,
            variables,
            errors,
            DateTimeOffset.UtcNow);
    }

    public string Export(ExecutionReport report, ReportExportFormat format)
    {
        ArgumentNullException.ThrowIfNull(report);

        return format switch
        {
            ReportExportFormat.Html => ExportHtml(report),
            ReportExportFormat.Markdown => ExportMarkdown(report),
            ReportExportFormat.Json => JsonSerializer.Serialize(report, JsonOptions),
            _ => throw new NotSupportedException($"Report export format '{format}' is not supported.")
        };
    }

    private static ReportStep CreateStep(ScenarioStepResult step)
        => new(
            step.Name,
            step.Status.ToString(),
            ToStatusCode(step.Response?.StatusCode),
            step.ElapsedMilliseconds,
            step.Assertions.Count,
            step.Assertions.Count(assertion => assertion.Passed),
            step.Assertions.Count(assertion => !assertion.Passed),
            step.SavedVariables,
            step.Error);

    private static int? ToStatusCode(HttpStatusCode? statusCode)
        => statusCode is null ? null : (int)statusCode.Value;

    private static string ExportMarkdown(ExecutionReport report)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"# Report: {EscapeMarkdown(report.ScenarioName)}");
        builder.AppendLine();
        builder.AppendLine("## Summary");
        builder.AppendLine();
        builder.AppendLine($"- Scenario: `{EscapeMarkdown(report.ScenarioId)}`");
        builder.AppendLine($"- Status: `{report.Summary.Status}`");
        builder.AppendLine($"- Started: `{report.Summary.StartedAt:O}`");
        builder.AppendLine($"- Time: `{report.Summary.ElapsedMilliseconds:0.##} ms`");
        builder.AppendLine($"- Steps: `{report.Summary.SucceededSteps} passed / {report.Summary.FailedSteps} failed / {report.Summary.SkippedSteps} skipped`");
        builder.AppendLine($"- Assertions: `{report.Summary.PassedAssertions} passed / {report.Summary.FailedAssertions} failed`");
        builder.AppendLine($"- Stopped early: `{report.Summary.StoppedEarly}`");
        builder.AppendLine();
        builder.AppendLine("## Steps");
        builder.AppendLine();
        builder.AppendLine("| Step | Status | HTTP | Time | Assertions | Error |");
        builder.AppendLine("| --- | --- | --- | ---: | ---: | --- |");

        foreach (var step in report.Steps)
        {
            builder.AppendLine($"| {EscapeMarkdown(step.Name)} | {step.Status} | {FormatStatusCode(step.StatusCode)} | {step.ElapsedMilliseconds:0.##} ms | {step.PassedAssertions}/{step.TotalAssertions} | {EscapeMarkdown(step.Error ?? string.Empty)} |");
        }

        builder.AppendLine();
        builder.AppendLine("## Assertions");
        builder.AppendLine();
        builder.AppendLine("| Step | Assertion | Result | Expected | Actual | Message |");
        builder.AppendLine("| --- | --- | --- | --- | --- | --- |");

        foreach (var assertion in report.Assertions)
        {
            builder.AppendLine($"| {EscapeMarkdown(assertion.StepName)} | {EscapeMarkdown(assertion.Name)} | {FormatPassed(assertion.Passed)} | {EscapeMarkdown(assertion.ExpectedValue ?? string.Empty)} | {EscapeMarkdown(assertion.ActualValue ?? string.Empty)} | {EscapeMarkdown(assertion.Message)} |");
        }

        builder.AppendLine();
        builder.AppendLine("## Variables");
        builder.AppendLine();
        builder.AppendLine("| Name | Value |");
        builder.AppendLine("| --- | --- |");

        foreach (var variable in report.Variables)
        {
            builder.AppendLine($"| {EscapeMarkdown(variable.Name)} | {EscapeMarkdown(variable.Value ?? string.Empty)} |");
        }

        builder.AppendLine();
        builder.AppendLine("## Errors");
        builder.AppendLine();

        if (report.Errors.Count == 0)
        {
            builder.AppendLine("No errors.");
        }
        else
        {
            foreach (var error in report.Errors)
            {
                builder.AppendLine($"- `{EscapeMarkdown(error.Scope)}`: {EscapeMarkdown(error.Message)}");
            }
        }

        return builder.ToString();
    }

    private static string ExportHtml(ExecutionReport report)
    {
        var builder = new StringBuilder();
        builder.AppendLine("<!doctype html>");
        builder.AppendLine("<html lang=\"en\">");
        builder.AppendLine("<head>");
        builder.AppendLine("  <meta charset=\"utf-8\">");
        builder.AppendLine($"  <title>Backend Testing Studio Report - {Html(report.ScenarioName)}</title>");
        builder.AppendLine("  <style>");
        builder.AppendLine("    body{font-family:Segoe UI,Arial,sans-serif;margin:32px;color:#172026;background:#f6f8fa}");
        builder.AppendLine("    main{max-width:1120px;margin:0 auto;background:#fff;border:1px solid #d8dee4;padding:28px}");
        builder.AppendLine("    table{border-collapse:collapse;width:100%;margin:16px 0}");
        builder.AppendLine("    th,td{border:1px solid #d8dee4;padding:8px;text-align:left;vertical-align:top}");
        builder.AppendLine("    th{background:#edf2f7}");
        builder.AppendLine("    code{background:#edf2f7;padding:2px 4px}");
        builder.AppendLine("  </style>");
        builder.AppendLine("</head>");
        builder.AppendLine("<body>");
        builder.AppendLine("<main>");
        builder.AppendLine($"<h1>Report: {Html(report.ScenarioName)}</h1>");
        builder.AppendLine("<h2>Summary</h2>");
        builder.AppendLine("<table>");
        builder.AppendLine($"<tr><th>Scenario</th><td><code>{Html(report.ScenarioId)}</code></td></tr>");
        builder.AppendLine($"<tr><th>Status</th><td>{Html(report.Summary.Status)}</td></tr>");
        builder.AppendLine($"<tr><th>Started</th><td>{report.Summary.StartedAt:O}</td></tr>");
        builder.AppendLine($"<tr><th>Time</th><td>{report.Summary.ElapsedMilliseconds:0.##} ms</td></tr>");
        builder.AppendLine($"<tr><th>Steps</th><td>{report.Summary.SucceededSteps} passed / {report.Summary.FailedSteps} failed / {report.Summary.SkippedSteps} skipped</td></tr>");
        builder.AppendLine($"<tr><th>Assertions</th><td>{report.Summary.PassedAssertions} passed / {report.Summary.FailedAssertions} failed</td></tr>");
        builder.AppendLine($"<tr><th>Stopped early</th><td>{report.Summary.StoppedEarly}</td></tr>");
        builder.AppendLine("</table>");
        builder.AppendLine("<h2>Steps</h2>");
        builder.AppendLine("<table><thead><tr><th>Step</th><th>Status</th><th>HTTP</th><th>Time</th><th>Assertions</th><th>Error</th></tr></thead><tbody>");

        foreach (var step in report.Steps)
        {
            builder.AppendLine($"<tr><td>{Html(step.Name)}</td><td>{Html(step.Status)}</td><td>{FormatStatusCode(step.StatusCode)}</td><td>{step.ElapsedMilliseconds:0.##} ms</td><td>{step.PassedAssertions}/{step.TotalAssertions}</td><td>{Html(step.Error ?? string.Empty)}</td></tr>");
        }

        builder.AppendLine("</tbody></table>");
        builder.AppendLine("<h2>Assertions</h2>");
        builder.AppendLine("<table><thead><tr><th>Step</th><th>Assertion</th><th>Result</th><th>Expected</th><th>Actual</th><th>Message</th></tr></thead><tbody>");

        foreach (var assertion in report.Assertions)
        {
            builder.AppendLine($"<tr><td>{Html(assertion.StepName)}</td><td>{Html(assertion.Name)}</td><td>{FormatPassed(assertion.Passed)}</td><td>{Html(assertion.ExpectedValue ?? string.Empty)}</td><td>{Html(assertion.ActualValue ?? string.Empty)}</td><td>{Html(assertion.Message)}</td></tr>");
        }

        builder.AppendLine("</tbody></table>");
        builder.AppendLine("<h2>Variables</h2>");
        builder.AppendLine("<table><thead><tr><th>Name</th><th>Value</th></tr></thead><tbody>");

        foreach (var variable in report.Variables)
        {
            builder.AppendLine($"<tr><td>{Html(variable.Name)}</td><td>{Html(variable.Value ?? string.Empty)}</td></tr>");
        }

        builder.AppendLine("</tbody></table>");
        builder.AppendLine("<h2>Errors</h2>");

        if (report.Errors.Count == 0)
        {
            builder.AppendLine("<p>No errors.</p>");
        }
        else
        {
            builder.AppendLine("<ul>");
            foreach (var error in report.Errors)
            {
                builder.AppendLine($"<li><code>{Html(error.Scope)}</code>: {Html(error.Message)}</li>");
            }

            builder.AppendLine("</ul>");
        }

        builder.AppendLine("</main>");
        builder.AppendLine("</body>");
        builder.AppendLine("</html>");
        return builder.ToString();
    }

    private static string Html(string value)
        => HtmlEncoder.Default.Encode(value);

    private static string EscapeMarkdown(string value)
        => value.Replace("|", "\\|", StringComparison.Ordinal);

    private static string FormatStatusCode(int? statusCode)
        => statusCode?.ToString() ?? "n/a";

    private static string FormatPassed(bool passed)
        => passed ? "Passed" : "Failed";
}
