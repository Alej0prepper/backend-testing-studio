namespace BackendTestingStudio.Core.Reporting;

public sealed record ReportSummary(
    string Status,
    DateTimeOffset StartedAt,
    double ElapsedMilliseconds,
    int TotalSteps,
    int SucceededSteps,
    int FailedSteps,
    int SkippedSteps,
    int TotalAssertions,
    int PassedAssertions,
    int FailedAssertions,
    bool StoppedEarly);
