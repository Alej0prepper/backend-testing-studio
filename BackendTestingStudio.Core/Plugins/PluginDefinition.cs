namespace BackendTestingStudio.Core.Plugins;

public sealed record PluginDefinition
{
    public PluginDefinition(
        string name,
        Version version,
        string author,
        string description,
        IReadOnlyList<PluginEndpointDefinition>? endpoints = null,
        IReadOnlyList<PluginScenarioDefinition>? scenarios = null,
        IReadOnlyList<PluginPayloadDefinition>? payloads = null,
        IReadOnlyList<PluginVariableDefinition>? variables = null)
    {
        Name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("Name is required.", nameof(name)) : name.Trim();
        Version = version ?? throw new ArgumentNullException(nameof(version));
        Author = string.IsNullOrWhiteSpace(author) ? throw new ArgumentException("Author is required.", nameof(author)) : author.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? throw new ArgumentException("Description is required.", nameof(description)) : description.Trim();
        Endpoints = endpoints ?? [];
        Scenarios = scenarios ?? [];
        Payloads = payloads ?? [];
        Variables = variables ?? [];
    }

    public string Name { get; }

    public Version Version { get; }

    public string Author { get; }

    public string Description { get; }

    public IReadOnlyList<PluginEndpointDefinition> Endpoints { get; }

    public IReadOnlyList<PluginScenarioDefinition> Scenarios { get; }

    public IReadOnlyList<PluginPayloadDefinition> Payloads { get; }

    public IReadOnlyList<PluginVariableDefinition> Variables { get; }
}
