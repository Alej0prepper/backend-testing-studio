namespace BackendTestingStudio.Core.Plugins;

public sealed record PluginScenarioStepDefinition
{
    public PluginScenarioStepDefinition(
        string name,
        string endpointName,
        string? payloadName = null,
        IReadOnlyDictionary<string, string?>? variables = null,
        string? description = null)
    {
        Name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("Name is required.", nameof(name)) : name.Trim();
        EndpointName = string.IsNullOrWhiteSpace(endpointName) ? throw new ArgumentException("EndpointName is required.", nameof(endpointName)) : endpointName.Trim();
        PayloadName = string.IsNullOrWhiteSpace(payloadName) ? null : payloadName.Trim();
        Variables = variables is null
            ? new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string?>(variables, StringComparer.OrdinalIgnoreCase);
        Description = description?.Trim();
    }

    public string Name { get; }

    public string EndpointName { get; }

    public string? PayloadName { get; }

    public IReadOnlyDictionary<string, string?> Variables { get; }

    public string? Description { get; }
}
