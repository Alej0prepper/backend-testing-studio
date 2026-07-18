namespace BackendTestingStudio.Core.Scenarios;

public sealed record ScenarioDefinition
{
    public ScenarioDefinition(
        string id,
        string name,
        IReadOnlyList<ScenarioStepDefinition> steps,
        IReadOnlyDictionary<string, string?>? variables = null,
        string? description = null)
    {
        Id = string.IsNullOrWhiteSpace(id)
            ? throw new ArgumentException("Id is required.", nameof(id))
            : id.Trim();
        Name = string.IsNullOrWhiteSpace(name)
            ? throw new ArgumentException("Name is required.", nameof(name))
            : name.Trim();
        Steps = steps ?? throw new ArgumentNullException(nameof(steps));
        Variables = variables is null
            ? new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string?>(variables, StringComparer.OrdinalIgnoreCase);
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    public string Id { get; }

    public string Name { get; }

    public IReadOnlyList<ScenarioStepDefinition> Steps { get; }

    public IReadOnlyDictionary<string, string?> Variables { get; }

    public string? Description { get; }
}
