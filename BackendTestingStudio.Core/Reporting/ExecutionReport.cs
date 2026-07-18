namespace BackendTestingStudio.Core.Reporting;

public sealed record ExecutionReport(
    string ScenarioId,
    string ScenarioName,
    ReportSummary Summary,
    IReadOnlyList<ReportStep> Steps,
    IReadOnlyList<ReportAssertion> Assertions,
    IReadOnlyList<ReportVariable> Variables,
    IReadOnlyList<ReportError> Errors,
    DateTimeOffset GeneratedAt);
