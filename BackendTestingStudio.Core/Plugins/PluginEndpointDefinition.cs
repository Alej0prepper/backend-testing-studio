namespace BackendTestingStudio.Core.Plugins;

public sealed record PluginEndpointDefinition
{
    public PluginEndpointDefinition(
        string name,
        string method,
        string route,
        string? description = null)
    {
        Name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("Name is required.", nameof(name)) : name.Trim();
        Method = string.IsNullOrWhiteSpace(method) ? throw new ArgumentException("Method is required.", nameof(method)) : method.Trim().ToUpperInvariant();
        Route = string.IsNullOrWhiteSpace(route) ? throw new ArgumentException("Route is required.", nameof(route)) : route.Trim();
        Description = description?.Trim();
    }

    public string Name { get; }

    public string Method { get; }

    public string Route { get; }

    public string? Description { get; }
}
