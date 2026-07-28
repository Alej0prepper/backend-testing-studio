namespace BackendTestingStudio.Storage.Runs;

public sealed class ScenarioRunStorageOptions
{
    public required string DatabasePath { get; init; }
    public int MaximumReportBytes { get; init; } = 2 * 1024 * 1024;
}
