namespace BackendTestingStudio.Core.Scenarios;

public sealed record ScenarioExecutionResult(
    string ScenarioId,
    string ScenarioName,
    ScenarioExecutionStatus Status,
    IReadOnlyList<ScenarioStepResult> Steps,
    IReadOnlyDictionary<string, string?> Variables,
    bool StoppedEarly,
    DateTimeOffset StartedAt,
    double ElapsedMilliseconds);
