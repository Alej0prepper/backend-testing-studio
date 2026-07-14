namespace BackendTestingStudio.Core.Environments;

public sealed record EnvironmentVariable
{
    public EnvironmentVariable(Guid id, string name, string value)
    {
        Id = id;
        Name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("Name is required.", nameof(name)) : name.Trim();
        Value = value ?? string.Empty;
    }

    public Guid Id { get; }

    public string Name { get; }

    public string Value { get; }
}
