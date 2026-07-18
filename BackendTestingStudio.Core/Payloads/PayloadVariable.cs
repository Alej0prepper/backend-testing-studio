namespace BackendTestingStudio.Core.Payloads;

public sealed record PayloadVariable
{
    public PayloadVariable(Guid id, string name, string? value = null)
    {
        Id = id;
        Name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("Name is required.", nameof(name)) : name.Trim();
        Value = value;
    }

    public Guid Id { get; }

    public string Name { get; }

    public string? Value { get; }
}
