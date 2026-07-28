using BackendTestingStudio.Core.Runs;
using Microsoft.Data.Sqlite;

namespace BackendTestingStudio.Storage.Runs;

public sealed class ScenarioRunRepository : IScenarioRunRepository
{
    private readonly ScenarioRunStorageOptions _options;

    public ScenarioRunRepository(ScenarioRunStorageOptions options)
    {
        _options = options;
        EnsureSchema();
    }

    public async Task AddAsync(StoredScenarioRun run, CancellationToken cancellationToken = default)
    {
        if (System.Text.Encoding.UTF8.GetByteCount(run.ReportJson) > _options.MaximumReportBytes)
        {
            throw new InvalidOperationException(
                $"Report exceeds the configured {_options.MaximumReportBytes} byte persistence limit.");
        }

        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO scenario_runs (
                id, created_at, plugin_id, plugin_version, scenario_id, scenario_name,
                environment_id, status, correlation_id, plugin_snapshot_json,
                environment_snapshot_json, report_json)
            VALUES (
                $id, $createdAt, $pluginId, $pluginVersion, $scenarioId, $scenarioName,
                $environmentId, $status, $correlationId, $pluginSnapshot,
                $environmentSnapshot, $reportJson);
            """;
        AddParameters(command, run);
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<StoredScenarioRun>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var result = new List<StoredScenarioRun>();
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM scenario_runs ORDER BY created_at DESC;";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            result.Add(Read(reader));
        }

        return result;
    }

    public async Task<StoredScenarioRun?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM scenario_runs WHERE id = $id LIMIT 1;";
        command.Parameters.AddWithValue("$id", id.ToString("D"));
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        return await reader.ReadAsync(cancellationToken).ConfigureAwait(false) ? Read(reader) : null;
    }

    public async Task DeleteOlderThanAsync(
        DateTimeOffset cutoff,
        int keepLatest,
        CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            DELETE FROM scenario_runs
            WHERE created_at < $cutoff
               OR id NOT IN (
                   SELECT id FROM scenario_runs ORDER BY created_at DESC LIMIT $keepLatest
               );
            """;
        command.Parameters.AddWithValue("$cutoff", cutoff.ToString("O"));
        command.Parameters.AddWithValue("$keepLatest", Math.Max(1, keepLatest));
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private void EnsureSchema()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_options.DatabasePath) ?? AppContext.BaseDirectory);
        using var connection = CreateConnection();
        connection.Open();
        SchemaMigrationRunner.Apply(connection, 30, "scenario-runs", migratedConnection =>
        {
            using var command = migratedConnection.CreateCommand();
            command.CommandText =
                """
                CREATE TABLE IF NOT EXISTS scenario_runs (
                    id TEXT NOT NULL PRIMARY KEY,
                    created_at TEXT NOT NULL,
                    plugin_id TEXT NOT NULL,
                    plugin_version TEXT NOT NULL,
                    scenario_id TEXT NOT NULL,
                    scenario_name TEXT NOT NULL,
                    environment_id TEXT NOT NULL,
                    status TEXT NOT NULL,
                    correlation_id TEXT NOT NULL,
                    plugin_snapshot_json TEXT NOT NULL,
                    environment_snapshot_json TEXT NOT NULL,
                    report_json TEXT NOT NULL
                );
                CREATE INDEX IF NOT EXISTS ix_scenario_runs_created_at ON scenario_runs(created_at DESC);
                """;
            command.ExecuteNonQuery();
        });
    }

    private SqliteConnection CreateConnection()
        => new($"Data Source={_options.DatabasePath}");

    private static StoredScenarioRun Read(SqliteDataReader reader)
        => new(
            Guid.Parse(reader.GetString(reader.GetOrdinal("id"))),
            DateTimeOffset.Parse(reader.GetString(reader.GetOrdinal("created_at"))),
            reader.GetString(reader.GetOrdinal("plugin_id")),
            reader.GetString(reader.GetOrdinal("plugin_version")),
            reader.GetString(reader.GetOrdinal("scenario_id")),
            reader.GetString(reader.GetOrdinal("scenario_name")),
            reader.GetString(reader.GetOrdinal("environment_id")),
            reader.GetString(reader.GetOrdinal("status")),
            reader.GetString(reader.GetOrdinal("correlation_id")),
            reader.GetString(reader.GetOrdinal("plugin_snapshot_json")),
            reader.GetString(reader.GetOrdinal("environment_snapshot_json")),
            reader.GetString(reader.GetOrdinal("report_json")));

    private static void AddParameters(SqliteCommand command, StoredScenarioRun run)
    {
        command.Parameters.AddWithValue("$id", run.Id.ToString("D"));
        command.Parameters.AddWithValue("$createdAt", run.CreatedAt.ToString("O"));
        command.Parameters.AddWithValue("$pluginId", run.PluginId);
        command.Parameters.AddWithValue("$pluginVersion", run.PluginVersion);
        command.Parameters.AddWithValue("$scenarioId", run.ScenarioId);
        command.Parameters.AddWithValue("$scenarioName", run.ScenarioName);
        command.Parameters.AddWithValue("$environmentId", run.EnvironmentId);
        command.Parameters.AddWithValue("$status", run.Status);
        command.Parameters.AddWithValue("$correlationId", run.CorrelationId);
        command.Parameters.AddWithValue("$pluginSnapshot", run.PluginSnapshotJson);
        command.Parameters.AddWithValue("$environmentSnapshot", run.EnvironmentSnapshotJson);
        command.Parameters.AddWithValue("$reportJson", run.ReportJson);
    }
}
