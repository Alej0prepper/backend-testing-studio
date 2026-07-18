namespace BackendTestingStudio.Storage.Environments;

public sealed record EnvironmentStorageOptions
{
    public required string DatabasePath { get; init; }
}
