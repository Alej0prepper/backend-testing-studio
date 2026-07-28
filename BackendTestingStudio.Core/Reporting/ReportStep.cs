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
    string? Error)
{
    public ReportRequestSnapshot? Request { get; init; }
    public ReportResponseSnapshot? Response { get; init; }
    public string? CorrelationId { get; init; }
    public string? ErrorCategory { get; init; }
}

public sealed record ReportRequestSnapshot(
    string Method,
    string Url,
    IReadOnlyDictionary<string, string?> Headers,
    string? Body);

public sealed record ReportResponseSnapshot(
    int StatusCode,
    IReadOnlyDictionary<string, IReadOnlyList<string>> Headers,
    string? Body);
