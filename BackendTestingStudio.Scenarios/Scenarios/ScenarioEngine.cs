using System.Diagnostics;
using BackendTestingStudio.Core.Assertions;
using BackendTestingStudio.Core.Http;
using BackendTestingStudio.Core.Scenarios;

namespace BackendTestingStudio.Scenarios.Scenarios;

public sealed class ScenarioEngine : IScenarioEngine
{
    private readonly IHttpEngine _httpEngine;
    private readonly IAssertionEngine _assertionEngine;

    public ScenarioEngine(IHttpEngine httpEngine, IAssertionEngine assertionEngine)
    {
        _httpEngine = httpEngine ?? throw new ArgumentNullException(nameof(httpEngine));
        _assertionEngine = assertionEngine ?? throw new ArgumentNullException(nameof(assertionEngine));
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

            var result = await ExecuteStepAsync(step, runtimeVariables, cancellationToken).ConfigureAwait(false);
            stepResults.Add(result);

            if (result.Status is ScenarioStepStatus.Succeeded)
            {
                foreach (var variable in result.SavedVariables)
                {
                    runtimeVariables[variable.Key] = variable.Value;
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
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var request = ResolveRequest(step, runtimeVariables);
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
                    "One or more assertions failed.");
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
                    captures.Error);
            }

            return new ScenarioStepResult(
                step.Name,
                ScenarioStepStatus.Succeeded,
                response,
                stopwatch.Elapsed.TotalMilliseconds,
                assertions,
                captures.Values);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return new ScenarioStepResult(
                step.Name,
                ScenarioStepStatus.Failed,
                null,
                stopwatch.Elapsed.TotalMilliseconds,
                [],
                EmptyVariables(),
                ex.Message);
        }
    }

    private static HttpRequestDefinition ResolveRequest(
        ScenarioStepDefinition step,
        IReadOnlyDictionary<string, string?> runtimeVariables)
    {
        var merged = CreateRuntimeVariables(runtimeVariables, step.Request.Variables);
        foreach (var variable in step.Variables)
        {
            merged[variable.Key] = variable.Value;
        }

        var request = new HttpRequestDefinition(
            step.Request.Url,
            step.Request.Headers,
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
