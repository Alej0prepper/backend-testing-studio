using BackendTestingStudio.Core.Assertions;
using BackendTestingStudio.Core.Http;

namespace BackendTestingStudio.Core.Scenarios;

public sealed record ScenarioStepResult(
    string Name,
    ScenarioStepStatus Status,
    HttpResponseResult? Response,
    double ElapsedMilliseconds,
    IReadOnlyList<AssertionResult> Assertions,
    IReadOnlyDictionary<string, string?> SavedVariables,
    string? Error = null,
    HttpRequestDefinition? Request = null,
    string? CorrelationId = null,
    string? ErrorCategory = null,
    string? Method = null);
