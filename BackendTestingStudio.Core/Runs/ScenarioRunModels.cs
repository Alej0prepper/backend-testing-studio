using BackendTestingStudio.Core.Reporting;
using BackendTestingStudio.Core.Scenarios;
using BackendTestingStudio.Core.Plugins;

namespace BackendTestingStudio.Core.Runs;

public sealed record ScenarioRunRequest(
    DeclarativeRunPlugin Plugin,
    string ScenarioId,
    string EnvironmentId,
    IReadOnlyDictionary<string, string?>? Overrides = null,
    bool AllowProductionMutations = false,
    TimeSpan? Timeout = null);

public sealed record DeclarativeRunPlugin(string FilePath, string Id, string Version);

public enum ScenarioRunFailureKind
{
    None,
    Validation,
    Configuration,
    ProductionGuard,
    Cancelled,
    Timeout,
    Execution
}

public sealed record ScenarioRunResult(
    Guid RunId,
    ScenarioRunFailureKind FailureKind,
    string? Error,
    ScenarioExecutionResult? Execution,
    ExecutionReport? Report)
{
    public bool Passed => Execution?.Status == ScenarioExecutionStatus.Succeeded && FailureKind == ScenarioRunFailureKind.None;
}

public sealed record StoredScenarioRun(
    Guid Id,
    DateTimeOffset CreatedAt,
    string PluginId,
    string PluginVersion,
    string ScenarioId,
    string ScenarioName,
    string EnvironmentId,
    string Status,
    string CorrelationId,
    string PluginSnapshotJson,
    string EnvironmentSnapshotJson,
    string ReportJson);

public interface IScenarioRunRepository
{
    Task AddAsync(StoredScenarioRun run, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StoredScenarioRun>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<StoredScenarioRun?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task DeleteOlderThanAsync(DateTimeOffset cutoff, int keepLatest, CancellationToken cancellationToken = default);
}

public interface IScenarioRunService
{
    Task<ScenarioRunResult> RunAsync(ScenarioRunRequest request, CancellationToken cancellationToken = default);
}
