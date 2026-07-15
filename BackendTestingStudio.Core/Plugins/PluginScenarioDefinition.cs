namespace BackendTestingStudio.Core.Plugins;

public sealed record PluginScenarioDefinition
{
    public PluginScenarioDefinition(
        string name,
        string description,
        IReadOnlyList<PluginScenarioStepDefinition>? steps = null)
    {
        Name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("Name is required.", nameof(name)) : name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? throw new ArgumentException("Description is required.", nameof(description)) : description.Trim();
        Steps = steps ?? [];
    }

    public string Name { get; }

    public string Description { get; }

    public IReadOnlyList<PluginScenarioStepDefinition> Steps { get; }
}
