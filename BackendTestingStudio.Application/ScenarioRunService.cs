using System.Text;
using System.Text.Json;
using BackendTestingStudio.Core.Http;
using BackendTestingStudio.Core.Plugins;
using BackendTestingStudio.Core.Reporting;
using BackendTestingStudio.Core.Runs;
using BackendTestingStudio.Core.Scenarios;
using BackendTestingStudio.Core.Security;
using Microsoft.Extensions.Logging;

namespace BackendTestingStudio.Application;

public sealed class ScenarioRunService : IScenarioRunService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly IDeclarativePluginLoader _loader;
    private readonly IPluginCompiler _compiler;
    private readonly IScenarioEngine _engine;
    private readonly IReportEngine _reports;
    private readonly ISecretStore _secrets;
    private readonly ISensitiveDataRedactor _redactor;
    private readonly IScenarioRunRepository _repository;
    private readonly ILogger<ScenarioRunService>? _logger;
    private readonly ScenarioRunOptions _options;

    public ScenarioRunService(
        IDeclarativePluginLoader loader,
        IPluginCompiler compiler,
        IScenarioEngine engine,
        IReportEngine reports,
        ISecretStore secrets,
        ISensitiveDataRedactor redactor,
        IScenarioRunRepository repository,
        ILogger<ScenarioRunService>? logger = null,
        ScenarioRunOptions? options = null)
    {
        _loader = loader;
        _compiler = compiler;
        _engine = engine;
        _reports = reports;
        _secrets = secrets;
        _redactor = redactor;
        _repository = repository;
        _logger = logger;
        _options = options ?? new ScenarioRunOptions();
    }

    public async Task<ScenarioRunResult> RunAsync(
        ScenarioRunRequest request,
        CancellationToken cancellationToken = default)
    {
        var runId = Guid.NewGuid();
        using var logScope = _logger?.BeginScope(new Dictionary<string, object?>
        {
            ["RunId"] = runId,
            ["PluginId"] = request.Plugin.Id,
            ["EnvironmentId"] = request.EnvironmentId,
            ["ScenarioId"] = request.ScenarioId
        });
        _logger?.LogInformation(
            "Starting run {RunId} for plugin {PluginId}, environment {EnvironmentId}, scenario {ScenarioId}.",
            runId,
            request.Plugin.Id,
            request.EnvironmentId,
            request.ScenarioId);
        var load = await _loader.LoadAsync(request.Plugin.FilePath, cancellationToken).ConfigureAwait(false);
        if (!load.IsValid || load.Plugin is null)
        {
            var error = string.Join(Environment.NewLine, load.Diagnostics.Select(item =>
                $"{item.JsonPath} [{item.Rule}] {item.Message}"));
            return new ScenarioRunResult(runId, ScenarioRunFailureKind.Validation, error, null, null);
        }

        var plugin = load.Plugin;
        var environment = plugin.Environments.FirstOrDefault(item =>
            string.Equals(item.Id, request.EnvironmentId, StringComparison.OrdinalIgnoreCase));
        var scenario = plugin.Scenarios.FirstOrDefault(item =>
            string.Equals(item.Id, request.ScenarioId, StringComparison.OrdinalIgnoreCase));
        if (environment is null || scenario is null)
        {
            return new ScenarioRunResult(
                runId,
                ScenarioRunFailureKind.Configuration,
                "The selected scenario or environment does not exist.",
                null,
                null);
        }

        if (string.Equals(environment.Level, "Production", StringComparison.OrdinalIgnoreCase) &&
            !request.AllowProductionMutations &&
            ContainsMutation(plugin, scenario))
        {
            return new ScenarioRunResult(
                runId,
                ScenarioRunFailureKind.ProductionGuard,
                "Production mutations are blocked. Explicit authorization is required.",
                null,
                null);
        }

        var sensitiveValues = await ResolveSecretsAsync(plugin, cancellationToken).ConfigureAwait(false);
        var missing = plugin.Variables
            .Where(item => item.Sensitive && item.Required && !item.Computed)
            .Where(item => !sensitiveValues.TryGetValue(item.Name, out var value) || string.IsNullOrWhiteSpace(value))
            .Select(item => item.Name)
            .ToArray();
        if (missing.Length > 0)
        {
            return new ScenarioRunResult(
                runId,
                ScenarioRunFailureKind.Configuration,
                $"Missing required secrets: {string.Join(", ", missing)}.",
                null,
                null);
        }

        var overrides = new Dictionary<string, string?>(sensitiveValues, StringComparer.OrdinalIgnoreCase);
        if (request.Overrides is not null)
        {
            foreach (var item in request.Overrides)
            {
                overrides[item.Key] = item.Value;
            }
        }

        var timeout = request.Timeout ?? TimeSpan.FromMilliseconds(environment.TimeoutMilliseconds);
        using var timeoutSource = new CancellationTokenSource(timeout);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutSource.Token);

        try
        {
            var definition = _compiler.Compile(plugin, scenario.Id, environment.Id, overrides);
            var rawExecution = await _engine.ExecuteAsync(definition, overrides, linked.Token).ConfigureAwait(false);
            var allSensitive = BuildSensitiveValues(plugin, rawExecution, sensitiveValues);
            var execution = Sanitize(rawExecution, allSensitive);
            var report = _reports.CreateReport(execution);
            await PersistAsync(runId, plugin, environment, report, allSensitive, cancellationToken).ConfigureAwait(false);
            var technicalFailure = execution.Steps.FirstOrDefault(step =>
                step.Status == ScenarioStepStatus.Failed &&
                step.ErrorCategory is not null and not ("Assertion" or "Capture"));
            var failureKind = technicalFailure?.ErrorCategory == "Configuration"
                ? ScenarioRunFailureKind.Configuration
                : technicalFailure is null
                    ? ScenarioRunFailureKind.None
                    : ScenarioRunFailureKind.Execution;
            _logger?.LogInformation(
                "Run {RunId} completed with status {Status} and failure kind {FailureKind}.",
                runId,
                execution.Status,
                failureKind);
            return new ScenarioRunResult(runId, failureKind, technicalFailure?.Error, execution, report);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return new ScenarioRunResult(runId, ScenarioRunFailureKind.Cancelled, "Execution cancelled.", null, null);
        }
        catch (OperationCanceledException) when (timeoutSource.IsCancellationRequested)
        {
            return new ScenarioRunResult(runId, ScenarioRunFailureKind.Timeout, $"Execution timed out after {timeout}.", null, null);
        }
        catch (Exception ex)
        {
            return new ScenarioRunResult(runId, ScenarioRunFailureKind.Execution, ex.Message, null, null);
        }
    }

    private async Task<Dictionary<string, string?>> ResolveSecretsAsync(
        DeclarativePlugin plugin,
        CancellationToken cancellationToken)
    {
        var result = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var variable in plugin.Variables.Where(item => item.Sensitive && !item.Computed))
        {
            result[variable.Name] = await _secrets.GetAsync(plugin.Id, variable.Name, cancellationToken)
                .ConfigureAwait(false);
        }

        return result;
    }

    private static Dictionary<string, string?> BuildSensitiveValues(
        DeclarativePlugin plugin,
        ScenarioExecutionResult execution,
        IReadOnlyDictionary<string, string?> inputSecrets)
    {
        var result = new Dictionary<string, string?>(inputSecrets, StringComparer.OrdinalIgnoreCase);
        foreach (var variable in plugin.Variables.Where(item => item.Sensitive))
        {
            if (execution.Variables.TryGetValue(variable.Name, out var value))
            {
                result[variable.Name] = value;
            }
        }

        return result;
    }

    private ScenarioExecutionResult Sanitize(
        ScenarioExecutionResult execution,
        IReadOnlyDictionary<string, string?> secrets)
    {
        var steps = execution.Steps.Select(step =>
        {
            var saved = step.SavedVariables.ToDictionary(
                item => item.Key,
                item => secrets.ContainsKey(item.Key) ? "[REDACTED]" : _redactor.RedactText(item.Value, secrets),
                StringComparer.OrdinalIgnoreCase);
            var response = step.Response is null
                ? null
                : new HttpResponseResult(
                    step.Response.StatusCode,
                    _redactor.RedactText(step.Response.Content, secrets),
                    step.Response.Headers.ToDictionary(
                        item => item.Key,
                        item => IsSensitiveHeader(item.Key)
                            ? (IReadOnlyList<string>)["[REDACTED]"]
                            : item.Value.Select(value => _redactor.RedactText(value, secrets) ?? string.Empty).ToArray(),
                        StringComparer.OrdinalIgnoreCase));
            var request = step.Request is null ? null : SanitizeRequest(step.Request, secrets);
            var assertions = step.Assertions.Select(assertion => assertion with
            {
                ActualValue = _redactor.RedactText(assertion.ActualValue, secrets),
                ExpectedValue = _redactor.RedactText(assertion.ExpectedValue, secrets),
                Message = _redactor.RedactText(assertion.Message, secrets) ?? string.Empty
            }).ToArray();
            return step with
            {
                SavedVariables = saved,
                Response = response,
                Request = request,
                Assertions = assertions,
                Error = _redactor.RedactText(step.Error, secrets)
            };
        }).ToArray();
        var variables = execution.Variables.ToDictionary(
            item => item.Key,
            item => secrets.ContainsKey(item.Key) ? "[REDACTED]" : _redactor.RedactText(item.Value, secrets),
            StringComparer.OrdinalIgnoreCase);
        return execution with { Steps = steps, Variables = variables };
    }

    private HttpRequestDefinition SanitizeRequest(
        HttpRequestDefinition request,
        IReadOnlyDictionary<string, string?> secrets)
    {
        var headers = request.Headers is null
            ? null
            : _redactor.RedactHeaders(request.Headers, secrets);
        var query = request.QueryParameters?.ToDictionary(
            item => item.Key,
            item => _redactor.RedactText(item.Value, secrets),
            StringComparer.OrdinalIgnoreCase);
        var body = request.Body switch
        {
            HttpRequestBody.RawJson raw => new HttpRequestBody.RawJson(
                _redactor.RedactText(raw.Text, secrets) ?? string.Empty,
                raw.ContentType),
            HttpRequestBody.Json json => new HttpRequestBody.RawJson(
                _redactor.RedactText(JsonSerializer.Serialize(json.Value), secrets) ?? string.Empty,
                json.ContentType),
            HttpRequestBody.Multipart => new HttpRequestBody.RawJson("\"[REDACTED MULTIPART]\""),
            _ => null
        };
        var sanitizedUrl = _redactor.RedactText(request.Url.OriginalString, secrets) ?? request.Url.OriginalString;
        return new HttpRequestDefinition(new Uri(sanitizedUrl, UriKind.Absolute), headers, query, body, null);
    }

    private async Task PersistAsync(
        Guid runId,
        DeclarativePlugin plugin,
        PluginEnvironment environment,
        ExecutionReport report,
        IReadOnlyDictionary<string, string?> secrets,
        CancellationToken cancellationToken)
    {
        var pluginSnapshot = _redactor.RedactText(JsonSerializer.Serialize(plugin, JsonOptions), secrets) ?? "{}";
        var environmentSnapshot = _redactor.RedactText(JsonSerializer.Serialize(environment, JsonOptions), secrets) ?? "{}";
        var reportJson = _reports.Export(report, ReportExportFormat.Json);
        await _repository.AddAsync(new StoredScenarioRun(
            runId,
            DateTimeOffset.UtcNow,
            plugin.Id,
            plugin.Version,
            report.ScenarioId,
            report.ScenarioName,
            environment.Id,
            report.Summary.Status,
            runId.ToString("N"),
            pluginSnapshot,
            environmentSnapshot,
            reportJson), cancellationToken).ConfigureAwait(false);
        await _repository.DeleteOlderThanAsync(
                DateTimeOffset.UtcNow.AddDays(-Math.Max(1, _options.RetentionDays)),
                Math.Max(1, _options.KeepLatestRuns),
                cancellationToken)
            .ConfigureAwait(false);
    }

    private static bool ContainsMutation(DeclarativePlugin plugin, PluginScenario scenario)
    {
        var methods = plugin.Modules.SelectMany(item => item.Endpoints)
            .ToDictionary(item => item.Id, item => item.Method, StringComparer.OrdinalIgnoreCase);
        return scenario.Steps.Any(step =>
            methods.TryGetValue(step.Execute, out var method) &&
            method is "POST" or "PUT" or "PATCH" or "DELETE");
    }

    private static bool IsSensitiveHeader(string name)
        => name.Equals("Authorization", StringComparison.OrdinalIgnoreCase) ||
           name.Equals("Cookie", StringComparison.OrdinalIgnoreCase) ||
           name.Equals("Set-Cookie", StringComparison.OrdinalIgnoreCase) ||
           name.Contains("api-key", StringComparison.OrdinalIgnoreCase);
}
