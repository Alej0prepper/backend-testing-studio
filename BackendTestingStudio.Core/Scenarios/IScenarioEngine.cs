namespace BackendTestingStudio.Core.Scenarios;

public interface IScenarioEngine
{
    Task<ScenarioExecutionResult> ExecuteAsync(
        ScenarioDefinition scenario,
        IReadOnlyDictionary<string, string?>? variables = null,
        CancellationToken cancellationToken = default);
}
