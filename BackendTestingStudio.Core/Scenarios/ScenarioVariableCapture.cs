namespace BackendTestingStudio.Core.Scenarios;

public sealed record ScenarioVariableCapture
{
    public ScenarioVariableCapture(
        string name,
        ScenarioVariableSource source,
        string? path = null,
        bool required = true)
    {
        Name = string.IsNullOrWhiteSpace(name)
            ? throw new ArgumentException("Name is required.", nameof(name))
            : name.Trim();
        Source = source;
        Path = string.IsNullOrWhiteSpace(path) ? null : path.Trim();
        Required = required;

        if (source is ScenarioVariableSource.JsonPath or ScenarioVariableSource.Header && Path is null)
        {
            throw new ArgumentException("Path is required for JSONPath and header captures.", nameof(path));
        }
    }

    public string Name { get; }

    public ScenarioVariableSource Source { get; }

    public string? Path { get; }

    public bool Required { get; }
}
