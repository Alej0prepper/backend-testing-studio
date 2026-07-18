namespace BackendTestingStudio.Core.Reporting;

public sealed record ReportError(
    string Scope,
    string Message);
