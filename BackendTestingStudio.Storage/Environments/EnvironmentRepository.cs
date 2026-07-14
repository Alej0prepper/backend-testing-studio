using BackendTestingStudio.Core.Environments;
using Microsoft.Data.Sqlite;

namespace BackendTestingStudio.Storage.Environments;

internal sealed class EnvironmentRepository : IEnvironmentRepository
{
    private const string EnvironmentTableSql = """
        CREATE TABLE IF NOT EXISTS environments (
            id TEXT NOT NULL PRIMARY KEY,
            name TEXT NOT NULL,
            base_url TEXT NOT NULL
        );
        """;

    private const string EnvironmentVariableTableSql = """
        CREATE TABLE IF NOT EXISTS environment_variables (
            id TEXT NOT NULL PRIMARY KEY,
            environment_id TEXT NOT NULL,
            kind TEXT NOT NULL,
            name TEXT NOT NULL,
            value TEXT NOT NULL,
            sort_order INTEGER NOT NULL,
            FOREIGN KEY (environment_id) REFERENCES environments(id) ON DELETE CASCADE
        );
        """;

    private readonly string _connectionString;

    public EnvironmentRepository(EnvironmentStorageOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.DatabasePath);

        var directory = Path.GetDirectoryName(options.DatabasePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = options.DatabasePath
        }.ToString();

        EnsureCreated();
    }

    public async Task<IReadOnlyList<Core.Environments.Environment>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var environments = new Dictionary<Guid, (Guid Id, string Name, string BaseUrl, List<EnvironmentVariable> Variables, List<EnvironmentVariable> Headers)>();

        await using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT id, name, base_url FROM environments ORDER BY name;";
            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var id = Guid.Parse(reader.GetString(0));
                environments[id] = (id, reader.GetString(1), reader.GetString(2), [], []);
            }
        }

        if (environments.Count == 0)
        {
            return [];
        }

        await using (var command = connection.CreateCommand())
        {
            command.CommandText = """
                SELECT environment_id, id, kind, name, value
                FROM environment_variables
                ORDER BY sort_order ASC;
                """;
            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var environmentId = Guid.Parse(reader.GetString(0));
                if (!environments.TryGetValue(environmentId, out var entry))
                {
                    continue;
                }

                var variable = new EnvironmentVariable(
                    Guid.Parse(reader.GetString(1)),
                    reader.GetString(3),
                    reader.GetString(4));

                var kind = reader.GetString(2);
                if (string.Equals(kind, "Header", StringComparison.OrdinalIgnoreCase))
                {
                    entry.Headers.Add(variable);
                }
                else
                {
                    entry.Variables.Add(variable);
                }
            }
        }

        return environments.Values
            .Select(entry => new Core.Environments.Environment(entry.Id, entry.Name, entry.BaseUrl, entry.Variables, entry.Headers))
            .ToArray();
    }

    public async Task<Core.Environments.Environment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var all = await GetAllAsync(cancellationToken).ConfigureAwait(false);
        return all.FirstOrDefault(item => item.Id == id);
    }

    public async Task<Core.Environments.Environment> CreateAsync(Core.Environments.Environment environment, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(environment);

        var entity = Normalize(environment);
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        await InsertEnvironmentAsync(connection, transaction, entity, cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

        return entity;
    }

    public async Task<Core.Environments.Environment> UpdateAsync(Core.Environments.Environment environment, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(environment);

        var entity = Normalize(environment);
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        await using (var deleteVariables = connection.CreateCommand())
        {
            deleteVariables.Transaction = transaction;
            deleteVariables.CommandText = "DELETE FROM environment_variables WHERE environment_id = $environmentId;";
            deleteVariables.Parameters.AddWithValue("$environmentId", entity.Id.ToString());
            await deleteVariables.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        await using (var updateEnvironment = connection.CreateCommand())
        {
            updateEnvironment.Transaction = transaction;
            updateEnvironment.CommandText = """
                UPDATE environments
                SET name = $name,
                    base_url = $baseUrl
                WHERE id = $id;
                """;
            updateEnvironment.Parameters.AddWithValue("$id", entity.Id.ToString());
            updateEnvironment.Parameters.AddWithValue("$name", entity.Name);
            updateEnvironment.Parameters.AddWithValue("$baseUrl", entity.BaseUrl);

            var affected = await updateEnvironment.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            if (affected == 0)
            {
                throw new KeyNotFoundException($"Environment '{entity.Id}' was not found.");
            }
        }

        await InsertVariablesAsync(connection, transaction, entity, cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

        return entity;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        await using (var deleteVariables = connection.CreateCommand())
        {
            deleteVariables.Transaction = transaction;
            deleteVariables.CommandText = "DELETE FROM environment_variables WHERE environment_id = $environmentId;";
            deleteVariables.Parameters.AddWithValue("$environmentId", id.ToString());
            await deleteVariables.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        await using (var deleteEnvironment = connection.CreateCommand())
        {
            deleteEnvironment.Transaction = transaction;
            deleteEnvironment.CommandText = "DELETE FROM environments WHERE id = $id;";
            deleteEnvironment.Parameters.AddWithValue("$id", id.ToString());
            await deleteEnvironment.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task InsertEnvironmentAsync(SqliteConnection connection, SqliteTransaction transaction, Core.Environments.Environment environment, CancellationToken cancellationToken)
    {
        await using (var insertEnvironment = connection.CreateCommand())
        {
            insertEnvironment.Transaction = transaction;
            insertEnvironment.CommandText = """
                INSERT INTO environments (id, name, base_url)
                VALUES ($id, $name, $baseUrl);
                """;
            insertEnvironment.Parameters.AddWithValue("$id", environment.Id.ToString());
            insertEnvironment.Parameters.AddWithValue("$name", environment.Name);
            insertEnvironment.Parameters.AddWithValue("$baseUrl", environment.BaseUrl);
            await insertEnvironment.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        await InsertVariablesAsync(connection, transaction, environment, cancellationToken).ConfigureAwait(false);
    }

    private async Task InsertVariablesAsync(SqliteConnection connection, SqliteTransaction transaction, Core.Environments.Environment environment, CancellationToken cancellationToken)
    {
        var entries = environment.Variables.Select((item, index) => (item, kind: "Variable", index))
            .Concat(environment.Headers.Select((item, index) => (item, kind: "Header", index)));

        foreach (var entry in entries)
        {
            await using var insertVariable = connection.CreateCommand();
            insertVariable.Transaction = transaction;
            insertVariable.CommandText = """
                INSERT INTO environment_variables (id, environment_id, kind, name, value, sort_order)
                VALUES ($id, $environmentId, $kind, $name, $value, $sortOrder);
                """;
            insertVariable.Parameters.AddWithValue("$id", entry.item.Id == Guid.Empty ? Guid.NewGuid().ToString() : entry.item.Id.ToString());
            insertVariable.Parameters.AddWithValue("$environmentId", environment.Id.ToString());
            insertVariable.Parameters.AddWithValue("$kind", entry.kind);
            insertVariable.Parameters.AddWithValue("$name", entry.item.Name);
            insertVariable.Parameters.AddWithValue("$value", entry.item.Value);
            insertVariable.Parameters.AddWithValue("$sortOrder", entry.index);
            await insertVariable.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private Core.Environments.Environment Normalize(Core.Environments.Environment environment)
    {
        var id = environment.Id == Guid.Empty ? Guid.NewGuid() : environment.Id;
        var variables = environment.Variables
            .Select(variable => new EnvironmentVariable(variable.Id == Guid.Empty ? Guid.NewGuid() : variable.Id, variable.Name, variable.Value))
            .ToArray();
        var headers = environment.Headers
            .Select(header => new EnvironmentVariable(header.Id == Guid.Empty ? Guid.NewGuid() : header.Id, header.Name, header.Value))
            .ToArray();

        return new Core.Environments.Environment(id, environment.Name, environment.BaseUrl, variables, headers);
    }

    private SqliteConnection CreateConnection() => new(_connectionString);

    private void EnsureCreated()
    {
        using var connection = CreateConnection();
        connection.Open();

        using (var command = connection.CreateCommand())
        {
            command.CommandText = EnvironmentTableSql;
            command.ExecuteNonQuery();
        }

        using (var command = connection.CreateCommand())
        {
            command.CommandText = EnvironmentVariableTableSql;
            command.ExecuteNonQuery();
        }
    }
}
