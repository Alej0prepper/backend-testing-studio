using System.Diagnostics;
using BackendTestingStudio.Core.Assertions;
using BackendTestingStudio.Core.Http;
using BackendTestingStudio.Core.Scenarios;
using Microsoft.Extensions.Logging;

namespace BackendTestingStudio.Scenarios.Scenarios;

public sealed class ScenarioEngine : IScenarioEngine
{
    private readonly IHttpEngine _httpEngine;
    private readonly IAssertionEngine _assertionEngine;
    private readonly ILogger<ScenarioEngine>? _logger;

    public ScenarioEngine(
        IHttpEngine httpEngine,
        IAssertionEngine assertionEngine,
        ILogger<ScenarioEngine>? logger = null)
    {
        _httpEngine = httpEngine ?? throw new ArgumentNullException(nameof(httpEngine));
        _assertionEngine = assertionEngine ?? throw new ArgumentNullException(nameof(assertionEngine));
        _logger = logger;
    }

    public async Task<ScenarioExecutionResult> ExecuteAsync(
        ScenarioDefinition scenario,
        IReadOnlyDictionary<string, string?>? variables = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(scenario);

        var startedAt = DateTimeOffset.UtcNow;
        var scenarioStopwatch = Stopwatch.StartNew();
        var runtimeVariables = CreateRuntimeVariables(scenario.Variables, variables);
        var executionOverrides = variables is null
            ? new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string?>(variables, StringComparer.OrdinalIgnoreCase);
        var stepResults = new List<ScenarioStepResult>(scenario.Steps.Count);
        var stoppedEarly = false;

        foreach (var step in scenario.Steps)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!step.Enabled)
            {
                stepResults.Add(CreateSkippedResult(step));
                continue;
            }

            var result = await ExecuteStepAsync(step, runtimeVariables, executionOverrides, cancellationToken).ConfigureAwait(false);
            stepResults.Add(result);

            if (result.Status is ScenarioStepStatus.Succeeded)
            {
                foreach (var variable in result.SavedVariables)
                {
                    if (!executionOverrides.ContainsKey(variable.Key))
                    {
                        runtimeVariables[variable.Key] = variable.Value;
                    }
                }
            }
            else if (step.StopOnFailure)
            {
                stoppedEarly = true;
                break;
            }
        }

        scenarioStopwatch.Stop();
        var status = stepResults.Any(step => step.Status is ScenarioStepStatus.Failed)
            ? ScenarioExecutionStatus.Failed
            : ScenarioExecutionStatus.Succeeded;

        return new ScenarioExecutionResult(
            scenario.Id,
            scenario.Name,
            status,
            stepResults,
            Snapshot(runtimeVariables),
            stoppedEarly,
            startedAt,
            scenarioStopwatch.Elapsed.TotalMilliseconds);
    }

    private async Task<ScenarioStepResult> ExecuteStepAsync(
        ScenarioStepDefinition step,
        Dictionary<string, string?> runtimeVariables,
        IReadOnlyDictionary<string, string?> executionOverrides,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var correlationId = Guid.NewGuid().ToString("N");
            using var logScope = _logger?.BeginScope(new Dictionary<string, object?>
            {
                ["EndpointId"] = step.EndpointId,
                ["CorrelationId"] = correlationId
            });
            var request = ResolveRequest(step, runtimeVariables, executionOverrides, correlationId);
            _logger?.LogInformation(
                "Executing endpoint {EndpointId} with correlation {CorrelationId}.",
                step.EndpointId,
                correlationId);
            var response = await SendAsync(step.Method, request, cancellationToken).ConfigureAwait(false);
            stopwatch.Stop();

            var assertionContext = new AssertionContext(
                response.StatusCode,
                response.Headers,
                response.Content,
                stopwatch.Elapsed.TotalMilliseconds);
            var assertions = _assertionEngine.Evaluate(
                ResolveAssertions(step.Assertions, request.Variables),
                assertionContext);

            if (assertions.Any(assertion => !assertion.Passed))
            {
                return new ScenarioStepResult(
                    step.Name,
                    ScenarioStepStatus.Failed,
                    response,
                    stopwatch.Elapsed.TotalMilliseconds,
                    assertions,
                    EmptyVariables(),
                    "One or more assertions failed.",
                    request,
                    correlationId,
                    "Assertion",
                    step.Method.ToString().ToUpperInvariant());
            }

            var captures = CaptureVariables(step.SaveVariables, response, request.Variables);
            if (captures.Error is not null)
            {
                return new ScenarioStepResult(
                    step.Name,
                    ScenarioStepStatus.Failed,
                    response,
                    stopwatch.Elapsed.TotalMilliseconds,
                    assertions,
                    EmptyVariables(),
                    captures.Error,
                    request,
                    correlationId,
                    "Capture",
                    step.Method.ToString().ToUpperInvariant());
            }

            _logger?.LogInformation(
                "Endpoint {EndpointId} completed with HTTP {StatusCode}.",
                step.EndpointId,
                (int)response.StatusCode);
            return new ScenarioStepResult(
                step.Name,
                ScenarioStepStatus.Succeeded,
                response,
                stopwatch.Elapsed.TotalMilliseconds,
                assertions,
                captures.Values,
                Request: request,
                CorrelationId: correlationId,
                Method: step.Method.ToString().ToUpperInvariant());
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger?.LogWarning(
                "Endpoint {EndpointId} failed in category {ErrorCategory}.",
                step.EndpointId,
                ClassifyError(ex));
            return new ScenarioStepResult(
                step.Name,
                ScenarioStepStatus.Failed,
                null,
                stopwatch.Elapsed.TotalMilliseconds,
                [],
                EmptyVariables(),
                ex.Message,
                ErrorCategory: ClassifyError(ex),
                Method: step.Method.ToString().ToUpperInvariant());
        }
    }

    private static string ClassifyError(Exception exception)
        => exception switch
        {
            TimeoutException => "Timeout",
            HttpRequestException { HttpRequestError: HttpRequestError.NameResolutionError } => "Dns",
            HttpRequestException { HttpRequestError: HttpRequestError.SecureConnectionError } => "Tls",
            HttpRequestException { HttpRequestError: HttpRequestError.ConnectionError } => "Connection",
            System.Text.Json.JsonException => "Parsing",
            InvalidOperationException => "Configuration",
            _ => "Execution"
        };

    private static HttpRequestDefinition ResolveRequest(
        ScenarioStepDefinition step,
        IReadOnlyDictionary<string, string?> runtimeVariables,
        IReadOnlyDictionary<string, string?> executionOverrides,
        string correlationId)
    {
        var merged = CreateRuntimeVariables(runtimeVariables, step.Request.Variables);
        foreach (var variable in step.Variables)
        {
            merged[variable.Key] = variable.Value;
        }
        foreach (var variable in executionOverrides)
        {
            merged[variable.Key] = variable.Value;
        }

        var headers = step.Request.Headers is null
            ? new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string?>(step.Request.Headers, StringComparer.OrdinalIgnoreCase);
        headers.TryAdd("X-BTS-Correlation-Id", correlationId);
        var request = new HttpRequestDefinition(
            step.Request.Url,
            headers,
            step.Request.QueryParameters,
            step.Request.Body,
            step.Request.Authentication,
            merged);

        return HttpRequestTemplateResolver.Resolve(request);
    }

    private Task<HttpResponseResult> SendAsync(
        ScenarioHttpMethod method,
        HttpRequestDefinition request,
        CancellationToken cancellationToken)
        => method switch
        {
            ScenarioHttpMethod.Get => _httpEngine.GetAsync(request, cancellationToken),
            ScenarioHttpMethod.Post => _httpEngine.PostAsync(request, cancellationToken),
            ScenarioHttpMethod.Put => _httpEngine.PutAsync(request, cancellationToken),
            ScenarioHttpMethod.Patch => _httpEngine.PatchAsync(request, cancellationToken),
            ScenarioHttpMethod.Delete => _httpEngine.DeleteAsync(request, cancellationToken),
            _ => throw new NotSupportedException($"HTTP method '{method}' is not supported.")
        };

    private static IReadOnlyList<AssertionDefinition> ResolveAssertions(
        IReadOnlyList<AssertionDefinition> assertions,
        IReadOnlyDictionary<string, string?>? variables)
        => assertions.Select(assertion => new AssertionDefinition(
            assertion.Name,
            assertion.Target,
            assertion.Operator,
            HttpRequestTemplateResolver.ResolveNullableText(assertion.Path, variables),
            HttpRequestTemplateResolver.ResolveNullableText(assertion.ExpectedValue, variables),
            assertion.MaximumMilliseconds)).ToArray();

    private static CaptureResult CaptureVariables(
        IReadOnlyList<ScenarioVariableCapture> captures,
        HttpResponseResult response,
        IReadOnlyDictionary<string, string?>? variables)
    {
        var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        foreach (var capture in captures)
        {
            var path = HttpRequestTemplateResolver.ResolveNullableText(capture.Path, variables);
            var value = capture.Source switch
            {
                ScenarioVariableSource.JsonPath => ReadJsonPath(response.Content, path!),
                ScenarioVariableSource.Header => ReadHeader(response.Headers, path!),
                ScenarioVariableSource.StatusCode => ((int)response.StatusCode).ToString(),
                ScenarioVariableSource.Body => response.Content,
                _ => throw new NotSupportedException($"Variable source '{capture.Source}' is not supported.")
            };

            if (value is null && capture.Required)
            {
                return new CaptureResult(
                    EmptyVariables(),
                    $"Required variable '{capture.Name}' could not be captured from {capture.Source}.");
            }

            values[capture.Name] = value;
        }

        return new CaptureResult(values, null);
    }

    private static string? ReadJsonPath(string? body, string path)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return null;
        }

        return JsonPathReader.ReadValues(body, path).FirstOrDefault();
    }

    private static string? ReadHeader(
        IReadOnlyDictionary<string, IReadOnlyList<string>> headers,
        string name)
        => headers.FirstOrDefault(header => string.Equals(header.Key, name, StringComparison.OrdinalIgnoreCase))
            .Value?
            .FirstOrDefault();

    private static Dictionary<string, string?> CreateRuntimeVariables(
        IReadOnlyDictionary<string, string?>? defaults,
        IReadOnlyDictionary<string, string?>? overrides)
    {
        var result = defaults is null
            ? new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string?>(defaults, StringComparer.OrdinalIgnoreCase);

        if (overrides is not null)
        {
            foreach (var variable in overrides)
            {
                result[variable.Key] = variable.Value;
            }
        }

        return result;
    }

    private static ScenarioStepResult CreateSkippedResult(ScenarioStepDefinition step)
        => new(
            step.Name,
            ScenarioStepStatus.Skipped,
            null,
            0,
            [],
            EmptyVariables());

    private static IReadOnlyDictionary<string, string?> EmptyVariables()
        => new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

    private static IReadOnlyDictionary<string, string?> Snapshot(IReadOnlyDictionary<string, string?> variables)
        => new Dictionary<string, string?>(variables, StringComparer.OrdinalIgnoreCase);

    private sealed record CaptureResult(IReadOnlyDictionary<string, string?> Values, string? Error);
}
