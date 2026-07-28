using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using BackendTestingStudio.Core.Plugins;

namespace BackendTestingStudio.Plugins;

public sealed class DeclarativePluginLoader : IDeclarativePluginLoader
{
    public const string SupportedSchemaVersion = "1.0.0";
    public const string EngineVersion = "1.0.0";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };

    public async Task<PluginLoadResult> LoadAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return Invalid(filePath ?? string.Empty, "$", "file.required", "A plugin.json path is required.");
        }

        var fullPath = Path.GetFullPath(filePath);
        if (!string.Equals(Path.GetFileName(fullPath), "plugin.json", StringComparison.OrdinalIgnoreCase))
        {
            return Invalid(fullPath, "$", "file.name", "The canonical file must be named plugin.json.");
        }

        if (!File.Exists(fullPath))
        {
            return Invalid(fullPath, "$", "file.exists", "The plugin file does not exist.");
        }

        try
        {
            await using var stream = File.OpenRead(fullPath);
            var plugin = await JsonSerializer.DeserializeAsync<DeclarativePlugin>(stream, JsonOptions, cancellationToken)
                .ConfigureAwait(false);
            if (plugin is null)
            {
                return Invalid(fullPath, "$", "json.empty", "The plugin file is empty.");
            }

            var diagnostics = Validate(plugin, fullPath);
            return new PluginLoadResult(
                fullPath,
                diagnostics.Any(item => item.Severity == PluginDiagnosticSeverity.Error) ? null : plugin,
                diagnostics);
        }
        catch (JsonException ex)
        {
            return Invalid(
                fullPath,
                ex.Path ?? "$",
                "json.structure",
                $"Invalid JSON contract at line {ex.LineNumber}, byte {ex.BytePositionInLine}: {ex.Message}");
        }
        catch (IOException ex)
        {
            return Invalid(fullPath, "$", "file.read", ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Invalid(fullPath, "$", "file.access", ex.Message);
        }
    }

    private static IReadOnlyList<PluginDiagnostic> Validate(DeclarativePlugin plugin, string file)
    {
        var diagnostics = new List<PluginDiagnostic>();
        Required(plugin.Id, "$.id", "id.required", "Plugin id is required.");
        CheckId(plugin.Id, "$.id");
        Required(plugin.Name, "$.name", "name.required", "Plugin name is required.");
        SemVersion(plugin.Version, "$.version", "version.invalid");
        CompatibleVersion(plugin.SchemaVersion, SupportedSchemaVersion, "$.schemaVersion", "schema.incompatible");
        MinimumEngineVersion(plugin.EngineVersion, "$.engineVersion");
        Required(plugin.Author, "$.author", "author.required", "Author is required.");
        Required(plugin.Description, "$.description", "description.required", "Description is required.");
        Required(plugin.DefaultEnvironment, "$.defaultEnvironment", "environment.default", "Default environment is required.");
        if (plugin.Environments.Count == 0) Error("$.environments", "environment.required", "At least one environment is required.");
        if (plugin.Modules.Count == 0) Error("$.modules", "module.required", "At least one module is required.");
        if (plugin.Scenarios.Count == 0) Error("$.scenarios", "scenario.required", "At least one scenario is required.");

        CheckUnique(plugin.Variables.Select(item => item.Name), "$.variables", "variable.duplicate");
        CheckUnique(plugin.Environments.Select(item => item.Id), "$.environments", "environment.duplicate");
        CheckUnique(plugin.Modules.Select(item => item.Id), "$.modules", "module.duplicate");
        CheckUnique(plugin.Payloads.Select(item => item.Id), "$.payloads", "payload.duplicate");
        CheckUnique(plugin.Assertions.Select(item => item.Id), "$.assertions", "assertion.duplicate");
        CheckUnique(plugin.Scenarios.Select(item => item.Id), "$.scenarios", "scenario.duplicate");

        var environments = Set(plugin.Environments.Select(item => item.Id));
        var variables = Set(plugin.Variables.Select(item => item.Name));
        var payloads = Set(plugin.Payloads.Select(item => item.Id));
        var assertions = Set(plugin.Assertions.Select(item => item.Id));
        var endpoints = plugin.Modules.SelectMany(item => item.Endpoints).ToArray();
        for (var index = 0; index < plugin.Modules.Count; index++)
        {
            CheckId(plugin.Modules[index].Id, $"$.modules[{index}].id");
            Required(plugin.Modules[index].Name, $"$.modules[{index}].name", "module.name", "Module name is required.");
            if (plugin.Modules[index].Endpoints.Count == 0)
            {
                Error($"$.modules[{index}].endpoints", "module.endpoints", "Module requires at least one endpoint.");
            }
        }
        CheckUnique(endpoints.Select(item => item.Id), "$.modules[*].endpoints", "endpoint.duplicate");
        var endpointIds = Set(endpoints.Select(item => item.Id));

        if (!environments.Contains(plugin.DefaultEnvironment))
        {
            Error("$.defaultEnvironment", "environment.missing", $"Environment '{plugin.DefaultEnvironment}' does not exist.");
        }

        for (var index = 0; index < plugin.Environments.Count; index++)
        {
            var environment = plugin.Environments[index];
            var path = $"$.environments[{index}]";
            CheckId(environment.Id, $"{path}.id");
            Required(environment.Name, $"{path}.name", "environment.name", "Environment name is required.");
            if (!Uri.TryCreate(environment.BaseUrl, UriKind.Absolute, out var uri) ||
                uri.Scheme is not ("http" or "https"))
            {
                Error($"{path}.baseUrl", "environment.url", "baseUrl must be an absolute HTTP or HTTPS URL.");
            }
            else if (environment.AllowedHosts.Count == 0 ||
                     !environment.AllowedHosts.Contains(uri.Host, StringComparer.OrdinalIgnoreCase))
            {
                Error($"{path}.allowedHosts", "environment.host", $"allowedHosts must include '{uri.Host}'.");
            }

            if (environment.TimeoutMilliseconds is < 1 or > 600_000)
            {
                Error($"{path}.timeoutMilliseconds", "environment.timeout", "Timeout must be between 1 and 600000 ms.");
            }

            var sensitiveNames = plugin.Variables.Where(item => item.Sensitive)
                .Select(item => item.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var variable in environment.Variables.Where(item =>
                         sensitiveNames.Contains(item.Key) && !string.IsNullOrEmpty(item.Value)))
            {
                Error($"{path}.variables.{variable.Key}", "secret.inline", "Sensitive values cannot be stored in environments.");
            }

            ValidateAuthentication(environment.Authentication, path);
        }

        for (var index = 0; index < plugin.Variables.Count; index++)
        {
            var variable = plugin.Variables[index];
            Required(variable.Name, $"$.variables[{index}].name", "variable.name", "Variable name is required.");
            if (!Regex.IsMatch(variable.Name, "^[A-Za-z][A-Za-z0-9_.-]*$"))
            {
                Error($"$.variables[{index}].name", "variable.name", $"Invalid variable name '{variable.Name}'.");
            }
            if (variable.Sensitive && !string.IsNullOrEmpty(variable.DefaultValue))
            {
                Error($"$.variables[{index}].defaultValue", "secret.inline", "Sensitive variables cannot define a value in plugin.json.");
            }
        }

        for (var index = 0; index < endpoints.Length; index++)
        {
            var endpoint = endpoints[index];
            var path = $"$.modules[*].endpoints[{index}]";
            CheckId(endpoint.Id, $"{path}.id");
            Required(endpoint.Name, $"{path}.name", "endpoint.name", "Endpoint name is required.");
            Required(endpoint.Path, $"{path}.path", "endpoint.path", "Endpoint path is required.");
            if (endpoint.Method is not ("GET" or "POST" or "PUT" or "PATCH" or "DELETE"))
            {
                Error($"{path}.method", "endpoint.method", $"Unsupported HTTP method '{endpoint.Method}'.");
            }

            CheckReference(endpoint.Payload, payloads, $"{path}.payload", "payload.missing");
            if (!string.IsNullOrWhiteSpace(endpoint.Payload) && endpoint.Body is not null)
            {
                Error(path, "endpoint.body.conflict", "Endpoint cannot define both payload and inline body.");
            }
            CheckReferences(endpoint.Assertions, assertions, $"{path}.assertions", "assertion.missing");
            CheckCaptures(endpoint.SaveVariables, variables, $"{path}.saveVariables");
        }

        for (var scenarioIndex = 0; scenarioIndex < plugin.Scenarios.Count; scenarioIndex++)
        {
            var scenario = plugin.Scenarios[scenarioIndex];
            CheckId(scenario.Id, $"$.scenarios[{scenarioIndex}].id");
            Required(scenario.Name, $"$.scenarios[{scenarioIndex}].name", "scenario.name", "Scenario name is required.");
            if (scenario.Steps.Count == 0)
            {
                Error($"$.scenarios[{scenarioIndex}].steps", "scenario.steps", "Scenario requires at least one step.");
            }
            var stepIds = Set(scenario.Steps.Select((step, index) => step.Id ?? $"step-{index + 1}"));
            for (var stepIndex = 0; stepIndex < scenario.Steps.Count; stepIndex++)
            {
                var step = scenario.Steps[stepIndex];
                var path = $"$.scenarios[{scenarioIndex}].steps[{stepIndex}]";
                CheckReference(step.Execute, endpointIds, $"{path}.execute", "endpoint.missing");
                CheckReferences(step.Assertions, assertions, $"{path}.assertions", "assertion.missing");
                CheckCaptures(step.SaveVariables, variables, $"{path}.saveVariables");
                CheckReferences(step.DependsOn, stepIds, $"{path}.dependsOn", "step.missing");

                if (step.DependsOn.Contains(step.Id ?? $"step-{stepIndex + 1}", StringComparer.OrdinalIgnoreCase))
                {
                    Error($"{path}.dependsOn", "step.cycle", "A step cannot depend on itself.");
                }

                if (step.With.TryGetValue("payload", out var payloadId))
                {
                    CheckReference(payloadId, payloads, $"{path}.with.payload", "payload.missing");
                }
            }

            DetectCycles(scenario, scenarioIndex);
        }

        for (var index = 0; index < plugin.Payloads.Count; index++)
        {
            CheckId(plugin.Payloads[index].Id, $"$.payloads[{index}].id");
            if (plugin.Payloads[index].Content.ValueKind == JsonValueKind.Undefined)
            {
                Error($"$.payloads[{index}].content", "payload.content", "Payload content is required.");
            }
        }

        for (var index = 0; index < plugin.Assertions.Count; index++)
        {
            var assertion = plugin.Assertions[index];
            var path = $"$.assertions[{index}]";
            CheckId(assertion.Id, $"{path}.id");
            switch (assertion.Type)
            {
                case "StatusCode" when assertion.Expected is null:
                    Error($"{path}.expected", "assertion.expected", "StatusCode assertion requires expected.");
                    break;
                case "Header" when string.IsNullOrWhiteSpace(assertion.Header):
                    Error($"{path}.header", "assertion.header", "Header assertion requires header.");
                    break;
                case "JsonPath" when string.IsNullOrWhiteSpace(assertion.Path):
                    Error($"{path}.path", "assertion.path", "JsonPath assertion requires path.");
                    break;
                case "MaxTime" when assertion.MaximumMilliseconds is null or < 0:
                    Error($"{path}.maximumMilliseconds", "assertion.time", "MaxTime requires a non-negative maximumMilliseconds.");
                    break;
                case not ("StatusCode" or "Header" or "JsonPath" or "MaxTime"):
                    Error($"{path}.type", "assertion.type", $"Unsupported assertion type '{assertion.Type}'.");
                    break;
            }
        }

        ValidatePlaceholders(plugin, variables, file, diagnostics);
        return diagnostics;

        void Required(string? value, string path, string rule, string message)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                Error(path, rule, message);
            }
        }

        void CheckId(string value, string path)
        {
            if (!Regex.IsMatch(value ?? string.Empty, "^[a-z0-9]+(?:-[a-z0-9]+)*$"))
            {
                Error(path, "id.format", $"'{value}' must use kebab-case.");
            }
        }

        void SemVersion(string value, string path, string rule)
        {
            if (!Version.TryParse(value, out _))
            {
                Error(path, rule, $"'{value}' is not a valid version.");
            }
        }

        void CompatibleVersion(string value, string supported, string path, string rule)
        {
            if (!Version.TryParse(value, out var parsed) || !Version.TryParse(supported, out var expected) ||
                parsed.Major != expected.Major)
            {
                Error(path, rule, $"Schema version '{value}' is incompatible with supported version '{supported}'.");
            }
        }

        void MinimumEngineVersion(string value, string path)
        {
            if (!Version.TryParse(value, out var required) || !Version.TryParse(EngineVersion, out var current))
            {
                Error(path, "engine.version", $"'{value}' is not a valid engine version.");
            }
            else if (required > current)
            {
                Error(path, "engine.incompatible", $"Plugin requires engine {required}; current engine is {current}.");
            }
        }

        void CheckUnique(IEnumerable<string> values, string path, string rule)
        {
            foreach (var duplicate in values.Where(item => !string.IsNullOrWhiteSpace(item))
                         .GroupBy(item => item, StringComparer.OrdinalIgnoreCase)
                         .Where(group => group.Count() > 1))
            {
                Error(path, rule, $"Duplicate id '{duplicate.Key}'.");
            }
        }

        void CheckReference(string? id, HashSet<string> known, string path, string rule)
        {
            if (!string.IsNullOrWhiteSpace(id) && !known.Contains(id))
            {
                Error(path, rule, $"Reference '{id}' does not exist.");
            }
        }

        void CheckReferences(IEnumerable<string> ids, HashSet<string> known, string path, string rule)
        {
            foreach (var id in ids)
            {
                CheckReference(id, known, path, rule);
            }
        }

        void CheckCaptures(IEnumerable<PluginVariableCapture> captures, HashSet<string> known, string path)
        {
            foreach (var capture in captures)
            {
                CheckReference(capture.Name, known, path, "variable.missing");
                if (capture.Source is "JsonPath" or "Header" && string.IsNullOrWhiteSpace(capture.Path))
                {
                    Error(path, "capture.path", $"Capture '{capture.Name}' requires path.");
                }
            }
        }

        void DetectCycles(PluginScenario scenario, int scenarioIndex)
        {
            var ids = scenario.Steps.Select((step, index) => step.Id ?? $"step-{index + 1}").ToArray();
            var graph = scenario.Steps.Select((step, index) => (Id: ids[index], step.DependsOn))
                .ToDictionary(item => item.Id, item => item.DependsOn, StringComparer.OrdinalIgnoreCase);
            var visiting = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            bool Visit(string id)
            {
                if (!visiting.Add(id))
                {
                    return true;
                }

                if (visited.Contains(id))
                {
                    visiting.Remove(id);
                    return false;
                }

                if (graph.TryGetValue(id, out var dependencies) && dependencies.Any(Visit))
                {
                    return true;
                }

                visiting.Remove(id);
                visited.Add(id);
                return false;
            }

            if (ids.Any(Visit))
            {
                Error($"$.scenarios[{scenarioIndex}].steps", "step.cycle", "Step dependencies contain a cycle.");
            }
        }

        void Error(string path, string rule, string message)
            => diagnostics.Add(new PluginDiagnostic(PluginDiagnosticSeverity.Error, file, path, rule, message));

        void ValidateAuthentication(PluginAuthentication? authentication, string environmentPath)
        {
            if (authentication is null ||
                string.Equals(authentication.Type, "None", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var (value, field) = authentication.Type.ToLowerInvariant() switch
            {
                "bearer" => (authentication.Token, "token"),
                "basic" => (authentication.Password, "password"),
                "apikey" or "api-key" => (authentication.Value, "value"),
                _ => ((string?)null, "type")
            };
            if (field == "type")
            {
                Error($"{environmentPath}.authentication.type", "authentication.type",
                    $"Unsupported authentication type '{authentication.Type}'.");
            }
            else if (!string.IsNullOrWhiteSpace(value) && !value.Contains("{{", StringComparison.Ordinal))
            {
                Error($"{environmentPath}.authentication.{field}", "secret.inline",
                    "Authentication secrets must reference a sensitive variable.");
            }
        }
    }

    private static void ValidatePlaceholders(
        DeclarativePlugin plugin,
        HashSet<string> variables,
        string file,
        ICollection<PluginDiagnostic> diagnostics)
    {
        var serialized = JsonSerializer.Serialize(plugin, JsonOptions);
        var matches = Regex.Matches(serialized, @"\{\{\s*(?<name>[A-Za-z0-9_.-]+)\s*\}\}");
        foreach (var name in matches.Select(match => match.Groups["name"].Value)
                     .Distinct(StringComparer.OrdinalIgnoreCase)
                     .Where(name => !variables.Contains(name)))
        {
            diagnostics.Add(new PluginDiagnostic(
                PluginDiagnosticSeverity.Error,
                file,
                "$",
                "variable.placeholder",
                $"Placeholder '{{{{{name}}}}}' does not reference a declared variable."));
        }
    }

    private static HashSet<string> Set(IEnumerable<string> values)
        => new(values.Where(item => !string.IsNullOrWhiteSpace(item)), StringComparer.OrdinalIgnoreCase);

    private static PluginLoadResult Invalid(string file, string path, string rule, string message)
        => new(
            file,
            null,
            [new PluginDiagnostic(PluginDiagnosticSeverity.Error, file, path, rule, message)]);
}
