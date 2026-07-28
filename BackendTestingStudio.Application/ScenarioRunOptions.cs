namespace BackendTestingStudio.Application;

public sealed class ScenarioRunOptions
{
    public int RetentionDays { get; set; } = 30;
    public int KeepLatestRuns { get; set; } = 200;
}
