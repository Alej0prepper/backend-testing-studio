namespace BackendTestingStudio.Core.Reporting;

public sealed record ReportAssertion(
    string StepName,
    string Name,
    bool Passed,
    string Message,
    string? ActualValue,
    string? ExpectedValue);
