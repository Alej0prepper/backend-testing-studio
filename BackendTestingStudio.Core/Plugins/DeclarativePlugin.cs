using System.Text.Json;

namespace BackendTestingStudio.Core.Plugins;

public sealed class DeclarativePlugin
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Version { get; init; } = string.Empty;
    public string SchemaVersion { get; init; } = string.Empty;
    public string EngineVersion { get; init; } = string.Empty;
    public string Author { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string DefaultEnvironment { get; init; } = string.Empty;
    public IReadOnlyList<string> Tags { get; init; } = [];
    public IReadOnlyList<PluginVariable> Variables { get; init; } = [];
    public IReadOnlyList<PluginEnvironment> Environments { get; init; } = [];
    public IReadOnlyList<PluginModule> Modules { get; init; } = [];
    public IReadOnlyList<PluginPayload> Payloads { get; init; } = [];
    public IReadOnlyList<PluginAssertion> Assertions { get; init; } = [];
    public IReadOnlyList<PluginScenario> Scenarios { get; init; } = [];
}

public sealed class PluginVariable
{
    public string Name { get; init; } = string.Empty;
    public string Type { get; init; } = "string";
    public string? DefaultValue { get; init; }
    public bool Required { get; init; }
    public bool Sensitive { get; init; }
    public bool Computed { get; init; }
    public bool Exportable { get; init; } = true;
    public string? Description { get; init; }
}

public sealed class PluginEnvironment
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string BaseUrl { get; init; } = string.Empty;
    public string Level { get; init; } = "Development";
    public IReadOnlyList<string> AllowedHosts { get; init; } = [];
    public IReadOnlyDictionary<string, string?> Headers { get; init; } =
        new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
    public IReadOnlyDictionary<string, string?> Variables { get; init; } =
        new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
    public PluginAuthentication? Authentication { get; init; }
    public int TimeoutMilliseconds { get; init; } = 30_000;
}

public sealed class PluginAuthentication
{
    public string Type { get; init; } = "None";
    public string? Token { get; init; }
    public string? Username { get; init; }
    public string? Password { get; init; }
    public string? HeaderName { get; init; }
    public string? Value { get; init; }
}

public sealed class PluginModule
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string BasePath { get; init; } = string.Empty;
    public IReadOnlyList<string> Tags { get; init; } = [];
    public IReadOnlyDictionary<string, string?> DefaultHeaders { get; init; } =
        new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
    public IReadOnlyList<PluginEndpoint> Endpoints { get; init; } = [];
}

public sealed class PluginEndpoint
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Method { get; init; } = string.Empty;
    public string Path { get; init; } = string.Empty;
    public string? Description { get; init; }
    public IReadOnlyList<string> Tags { get; init; } = [];
    public IReadOnlyDictionary<string, string?> Headers { get; init; } =
        new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
    public IReadOnlyDictionary<string, string?> Query { get; init; } =
        new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
    public string? Payload { get; init; }
    public JsonElement? Body { get; init; }
    public IReadOnlyList<string> Assertions { get; init; } = [];
    public IReadOnlyList<PluginVariableCapture> SaveVariables { get; init; } = [];
    public int? TimeoutMilliseconds { get; init; }
}

public sealed class PluginPayload
{
    public string Id { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string ContentType { get; init; } = "application/json";
    public JsonElement Content { get; init; }
}

public sealed class PluginAssertion
{
    public string Id { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public string? Operator { get; init; }
    public string? Path { get; init; }
    public string? Header { get; init; }
    public JsonElement? Expected { get; init; }
    public double? MaximumMilliseconds { get; init; }
    public string? Message { get; init; }
}

public sealed class PluginScenario
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public IReadOnlyList<string> Tags { get; init; } = [];
    public string OnFailure { get; init; } = "Stop";
    public IReadOnlyDictionary<string, string?> Variables { get; init; } =
        new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
    public IReadOnlyList<PluginScenarioStep> Steps { get; init; } = [];
}

public sealed class PluginScenarioStep
{
    public string? Id { get; init; }
    public string Execute { get; init; } = string.Empty;
    public string? Name { get; init; }
    public string? Description { get; init; }
    public bool Enabled { get; init; } = true;
    public bool? StopOnFailure { get; init; }
    public IReadOnlyDictionary<string, string?> With { get; init; } =
        new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
    public IReadOnlyList<string> Assertions { get; init; } = [];
    public IReadOnlyList<PluginVariableCapture> SaveVariables { get; init; } = [];
    public IReadOnlyList<string> DependsOn { get; init; } = [];
}

public sealed class PluginVariableCapture
{
    public string Name { get; init; } = string.Empty;
    public string Source { get; init; } = string.Empty;
    public string? Path { get; init; }
    public bool Required { get; init; } = true;
}
