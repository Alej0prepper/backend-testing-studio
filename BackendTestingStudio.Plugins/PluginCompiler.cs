using System.Text.Json;
using BackendTestingStudio.Core.Assertions;
using BackendTestingStudio.Core.Http;
using BackendTestingStudio.Core.Plugins;
using BackendTestingStudio.Core.Scenarios;

namespace BackendTestingStudio.Plugins;

public sealed class PluginCompiler : IPluginCompiler
{
    public ScenarioDefinition Compile(
        DeclarativePlugin plugin,
        string scenarioId,
        string environmentId,
        IReadOnlyDictionary<string, string?>? variables = null)
    {
        ArgumentNullException.ThrowIfNull(plugin);
        var scenario = plugin.Scenarios.FirstOrDefault(item =>
                string.Equals(item.Id, scenarioId, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Scenario '{scenarioId}' does not exist.");
        var environment = plugin.Environments.FirstOrDefault(item =>
                string.Equals(item.Id, environmentId, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Environment '{environmentId}' does not exist.");
        var endpoints = plugin.Modules.SelectMany(module =>
                module.Endpoints.Select(endpoint => (module, endpoint)))
            .ToDictionary(item => item.endpoint.Id, StringComparer.OrdinalIgnoreCase);
        var assertions = plugin.Assertions.ToDictionary(item => item.Id, StringComparer.OrdinalIgnoreCase);
        var payloads = plugin.Payloads.ToDictionary(item => item.Id, StringComparer.OrdinalIgnoreCase);
        var defaults = BuildDefaults(plugin, environment, scenario, variables);
        var steps = scenario.Steps.Select(step =>
            CompileStep(step, scenario, environment, endpoints, assertions, payloads, defaults)).ToArray();

        return new ScenarioDefinition(scenario.Id, scenario.Name, steps, defaults, scenario.Description);
    }

    private static ScenarioStepDefinition CompileStep(
        PluginScenarioStep step,
        PluginScenario scenario,
        PluginEnvironment environment,
        IReadOnlyDictionary<string, (PluginModule module, PluginEndpoint endpoint)> endpoints,
        IReadOnlyDictionary<string, PluginAssertion> assertions,
        IReadOnlyDictionary<string, PluginPayload> payloads,
        IReadOnlyDictionary<string, string?> defaults)
    {
        var (module, endpoint) = endpoints[step.Execute];
        var stepVariables = step.With
            .Where(item => !string.Equals(item.Key, "payload", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(item => item.Key, item => item.Value, StringComparer.OrdinalIgnoreCase);
        var baseUri = environment.BaseUrl.TrimEnd('/');
        var route = $"{module.BasePath.TrimEnd('/')}/{endpoint.Path.TrimStart('/')}";
        var rawUrl = baseUri + (route.StartsWith('/') ? route : "/" + route);
        var headers = Merge(environment.Headers, module.DefaultHeaders, endpoint.Headers);
        var payloadId = step.With.TryGetValue("payload", out var selectedPayload)
            ? selectedPayload
            : endpoint.Payload;
        var body = CreateBody(payloadId, endpoint.Body, payloads);
        var request = new HttpRequestDefinition(
            new Uri(rawUrl, UriKind.Absolute),
            headers,
            endpoint.Query,
            body,
            CreateAuthentication(environment.Authentication),
            variables: null);
        var assertionIds = step.Assertions.Count > 0 ? step.Assertions : endpoint.Assertions;
        var captureDefinitions = step.SaveVariables.Count > 0 ? step.SaveVariables : endpoint.SaveVariables;

        return new ScenarioStepDefinition(
            step.Name ?? endpoint.Name,
            Enum.Parse<ScenarioHttpMethod>(endpoint.Method, true),
            request,
            assertionIds.Select(id => CompileAssertion(assertions[id])).ToArray(),
            captureDefinitions.Select(CompileCapture).ToArray(),
            stepVariables,
            step.StopOnFailure ?? string.Equals(scenario.OnFailure, "Stop", StringComparison.OrdinalIgnoreCase),
            step.Enabled,
            step.Description ?? endpoint.Description,
            endpoint.Id);
    }

    private static IReadOnlyDictionary<string, string?> BuildDefaults(
        DeclarativePlugin plugin,
        PluginEnvironment environment,
        PluginScenario scenario,
        IReadOnlyDictionary<string, string?>? overrides)
    {
        var result = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var variable in plugin.Variables.Where(item => !item.Sensitive && !item.Computed))
        {
            result[variable.Name] = variable.DefaultValue;
        }

        foreach (var variable in plugin.Variables.Where(item => item.Computed))
        {
            result[variable.Name] = string.Empty;
        }

        Add(result, environment.Variables);
        Add(result, scenario.Variables);
        Add(result, overrides);
        return result;
    }

    private static HttpRequestBody? CreateBody(
        string? payloadId,
        JsonElement? inlineBody,
        IReadOnlyDictionary<string, PluginPayload> payloads)
    {
        if (!string.IsNullOrWhiteSpace(payloadId))
        {
            var payload = payloads[payloadId];
            return new HttpRequestBody.RawJson(payload.Content.GetRawText(), payload.ContentType);
        }

        return inlineBody is null
            ? null
            : new HttpRequestBody.RawJson(inlineBody.Value.GetRawText());
    }

    private static HttpAuthentication? CreateAuthentication(PluginAuthentication? authentication)
        => authentication?.Type.ToLowerInvariant() switch
        {
            null or "" or "none" => null,
            "bearer" => new HttpAuthentication.Bearer(authentication.Token ?? string.Empty),
            "basic" => new HttpAuthentication.Basic(authentication.Username ?? string.Empty, authentication.Password ?? string.Empty),
            "apikey" or "api-key" => new HttpAuthentication.ApiKey(
                authentication.HeaderName ?? "X-Api-Key",
                authentication.Value ?? string.Empty),
            _ => throw new InvalidOperationException($"Unsupported authentication type '{authentication.Type}'.")
        };

    private static AssertionDefinition CompileAssertion(PluginAssertion assertion)
    {
        var target = assertion.Type.ToLowerInvariant() switch
        {
            "statuscode" => AssertionTargetKind.StatusCode,
            "header" => AssertionTargetKind.Header,
            "jsonpath" => AssertionTargetKind.JsonPath,
            "maxtime" or "time" => AssertionTargetKind.Time,
            _ => throw new InvalidOperationException($"Unsupported assertion type '{assertion.Type}'.")
        };
        var expected = assertion.Expected is null ? null : Format(assertion.Expected.Value);
        var opText = assertion.Operator ?? expected;
        var @operator = target == AssertionTargetKind.Time
            ? AssertionOperatorKind.MaxTime
            : opText?.ToLowerInvariant() switch
            {
                "contains" => AssertionOperatorKind.Contains,
                "null" => AssertionOperatorKind.Null,
                "notnull" => AssertionOperatorKind.NotNull,
                _ => AssertionOperatorKind.Equals
            };

        return new AssertionDefinition(
            assertion.Id,
            target,
            @operator,
            assertion.Path ?? assertion.Header,
            @operator is AssertionOperatorKind.Null or AssertionOperatorKind.NotNull ? null : expected,
            assertion.MaximumMilliseconds);
    }

    private static ScenarioVariableCapture CompileCapture(PluginVariableCapture capture)
        => new(
            capture.Name,
            Enum.Parse<ScenarioVariableSource>(capture.Source, true),
            capture.Path,
            capture.Required);

    private static string? Format(JsonElement element)
        => element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Null => null,
            _ => element.GetRawText()
        };

    private static IReadOnlyDictionary<string, string?> Merge(
        params IReadOnlyDictionary<string, string?>?[] dictionaries)
    {
        var result = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var dictionary in dictionaries)
        {
            Add(result, dictionary);
        }

        return result;
    }

    private static void Add(
        IDictionary<string, string?> target,
        IReadOnlyDictionary<string, string?>? source)
    {
        if (source is null)
        {
            return;
        }

        foreach (var item in source)
        {
            target[item.Key] = item.Value;
        }
    }
}
