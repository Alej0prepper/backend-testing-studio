using System.Net;
using System.Text.Json;
using BackendTestingStudio.Core.Assertions;
using BackendTestingStudio.Core.Http;
using BackendTestingStudio.Core.Reporting;
using BackendTestingStudio.Core.Scenarios;
using BackendTestingStudio.Reporting;
using Xunit;

namespace BackendTestingStudio.Reporting.Tests;

public sealed class ReportEngineTests
{
    [Fact]
    public void CreateReport_AggregatesScenarioExecution()
    {
        var engine = new ReportEngine();

        var report = engine.CreateReport(CreateExecution());

        Assert.Equal("checkout-flow", report.ScenarioId);
        Assert.Equal("Failed", report.Summary.Status);
        Assert.Equal(2, report.Summary.TotalSteps);
        Assert.Equal(1, report.Summary.SucceededSteps);
        Assert.Equal(1, report.Summary.FailedSteps);
        Assert.Equal(2, report.Summary.TotalAssertions);
        Assert.Equal(1, report.Summary.PassedAssertions);
        Assert.Equal(1, report.Summary.FailedAssertions);
        Assert.True(report.Summary.StoppedEarly);
        Assert.Contains(report.Variables, variable => variable.Name == "OrderId" && variable.Value == "order-42");
        Assert.Contains(report.Errors, error => error.Scope == "Confirm order");
    }

    [Fact]
    public void Export_CreatesMarkdownReport()
    {
        var engine = new ReportEngine();
        var report = engine.CreateReport(CreateExecution());

        var markdown = engine.Export(report, ReportExportFormat.Markdown);

        Assert.Contains("# Report: Checkout flow", markdown);
        Assert.Contains("| Confirm order | Failed | 409 | 24.5 ms | 0/1 | Conflict returned. |", markdown);
        Assert.Contains("| OrderId | order-42 |", markdown);
    }

    [Fact]
    public void Export_CreatesHtmlReportAndEscapesContent()
    {
        var engine = new ReportEngine();
        var execution = CreateExecution("<script>alert(1)</script>");
        var report = engine.CreateReport(execution);

        var html = engine.Export(report, ReportExportFormat.Html);

        Assert.Contains("<!doctype html>", html);
        Assert.Contains("&lt;script&gt;alert(1)&lt;/script&gt;", html);
        Assert.DoesNotContain("<script>alert(1)</script>", html);
    }

    [Fact]
    public void Export_CreatesJsonReport()
    {
        var engine = new ReportEngine();
        var report = engine.CreateReport(CreateExecution());

        var json = engine.Export(report, ReportExportFormat.Json);
        using var document = JsonDocument.Parse(json);

        Assert.Equal("checkout-flow", document.RootElement.GetProperty("scenarioId").GetString());
        Assert.Equal("Failed", document.RootElement.GetProperty("summary").GetProperty("status").GetString());
        Assert.Equal(2, document.RootElement.GetProperty("steps").GetArrayLength());
    }

    private static ScenarioExecutionResult CreateExecution(string scenarioName = "Checkout flow")
        => new(
            "checkout-flow",
            scenarioName,
            ScenarioExecutionStatus.Failed,
            [
                new ScenarioStepResult(
                    "Create order",
                    ScenarioStepStatus.Succeeded,
                    Response(HttpStatusCode.Created),
                    18.75,
                    [
                        new AssertionResult("created", true, "Status code matched.", "201", "201")
                    ],
                    new Dictionary<string, string?> { ["OrderId"] = "order-42" }),
                new ScenarioStepResult(
                    "Confirm order",
                    ScenarioStepStatus.Failed,
                    Response(HttpStatusCode.Conflict),
                    24.5,
                    [
                        new AssertionResult("confirmed", false, "Status code did not match.", "409", "200")
                    ],
                    new Dictionary<string, string?>(),
                    "Conflict returned.")
            ],
            new Dictionary<string, string?> { ["OrderId"] = "order-42" },
            true,
            DateTimeOffset.Parse("2026-07-18T12:00:00Z"),
            43.25);

    private static HttpResponseResult Response(HttpStatusCode statusCode)
        => new(statusCode, """{"ok":true}""", new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase));
}
