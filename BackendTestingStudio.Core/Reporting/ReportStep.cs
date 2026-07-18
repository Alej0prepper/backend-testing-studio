namespace BackendTestingStudio.Core.Reporting;

public sealed record ReportStep(
    string Name,
    string Status,
    int? StatusCode,
    double ElapsedMilliseconds,
    int TotalAssertions,
    int PassedAssertions,
    int FailedAssertions,
    IReadOnlyDictionary<string, string?> SavedVariables,
    string? Error);
