namespace BackendTestingStudio.Core.Plugins;

public sealed record PluginPayloadDefinition
{
    public PluginPayloadDefinition(
        string name,
        string? description = null,
        string? contentType = null,
        string? schema = null)
    {
        Name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("Name is required.", nameof(name)) : name.Trim();
        Description = description?.Trim();
        ContentType = contentType?.Trim();
        Schema = schema?.Trim();
    }

    public string Name { get; }

    public string? Description { get; }

    public string? ContentType { get; }

    public string? Schema { get; }
}
