namespace BackendTestingStudio.Assertions.Assertions;

public sealed record AssertionResult(
    string Name,
    bool Passed,
    string Message,
    string? ActualValue = null,
    string? ExpectedValue = null);
