namespace BackendTestingStudio.Core.Environments;

public sealed record Environment
{
    public Environment(
        Guid id,
        string name,
        string baseUrl,
        IReadOnlyList<EnvironmentVariable>? variables = null,
        IReadOnlyList<EnvironmentVariable>? headers = null)
    {
        Id = id;
        Name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("Name is required.", nameof(name)) : name.Trim();
        BaseUrl = string.IsNullOrWhiteSpace(baseUrl) ? throw new ArgumentException("BaseUrl is required.", nameof(baseUrl)) : baseUrl.Trim();
        Variables = variables ?? [];
        Headers = headers ?? [];
    }

    public Guid Id { get; }

    public string Name { get; }

    public string BaseUrl { get; }

    public IReadOnlyList<EnvironmentVariable> Variables { get; }

    public IReadOnlyList<EnvironmentVariable> Headers { get; }
}
