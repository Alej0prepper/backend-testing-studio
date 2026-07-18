namespace BackendTestingStudio.Core.Plugins;

public sealed record PluginVariableDefinition
{
    public PluginVariableDefinition(string name, string? value = null, string? description = null)
    {
        Name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("Name is required.", nameof(name)) : name.Trim();
        Value = value;
        Description = description?.Trim();
    }

    public string Name { get; }

    public string? Value { get; }

    public string? Description { get; }
}
