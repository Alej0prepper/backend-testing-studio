using BackendTestingStudio.Core.Environments;
using Microsoft.Data.Sqlite;

namespace BackendTestingStudio.Storage.Environments;

internal sealed class EnvironmentRepository : IEnvironmentRepository
{
    private const string EnvironmentTableSql = """
        CREATE TABLE IF NOT EXISTS environments (
            id TEXT NOT NULL PRIMARY KEY,
            name TEXT NOT NULL,
            base_url TEXT NOT NULL,
            auth_kind TEXT NULL,
            auth_value1 TEXT NULL,
            auth_value2 TEXT NULL
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

        var environments = new Dictionary<Guid, (Guid Id, string Name, string BaseUrl, List<EnvironmentVariable> Variables, List<EnvironmentVariable> Headers, EnvironmentAuthentication? Authentication)>();

        await using (var command = connection.CreateCommand())
        {
            command.CommandText = """
                SELECT id, name, base_url, auth_kind, auth_value1, auth_value2
                FROM environments
                ORDER BY name;
                """;
            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var id = Guid.Parse(reader.GetString(0));
                environments[id] = (
                    id,
                    reader.GetString(1),
                    reader.GetString(2),
                    [],
                    [],
                    ReadAuthentication(reader, 3, 4, 5));
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
            .Select(entry => new Core.Environments.Environment(entry.Id, entry.Name, entry.BaseUrl, entry.Variables, entry.Headers, entry.Authentication))
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
            AddParameter(deleteVariables, "$environmentId", entity.Id.ToString());
            await deleteVariables.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        await using (var updateEnvironment = connection.CreateCommand())
        {
            updateEnvironment.Transaction = transaction;
            updateEnvironment.CommandText = """
                UPDATE environments
                SET name = $name,
                    base_url = $baseUrl,
                    auth_kind = $authKind,
                    auth_value1 = $authValue1,
                    auth_value2 = $authValue2
                WHERE id = $id;
                """;
            AddParameter(updateEnvironment, "$id", entity.Id.ToString());
            AddParameter(updateEnvironment, "$name", entity.Name);
            AddParameter(updateEnvironment, "$baseUrl", entity.BaseUrl);
            AddAuthenticationParameters(updateEnvironment, entity.Authentication);

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
            AddParameter(deleteVariables, "$environmentId", id.ToString());
            await deleteVariables.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        await using (var deleteEnvironment = connection.CreateCommand())
        {
            deleteEnvironment.Transaction = transaction;
            deleteEnvironment.CommandText = "DELETE FROM environments WHERE id = $id;";
            AddParameter(deleteEnvironment, "$id", id.ToString());
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
                INSERT INTO environments (id, name, base_url, auth_kind, auth_value1, auth_value2)
                VALUES ($id, $name, $baseUrl, $authKind, $authValue1, $authValue2);
                """;
            AddParameter(insertEnvironment, "$id", environment.Id.ToString());
            AddParameter(insertEnvironment, "$name", environment.Name);
            AddParameter(insertEnvironment, "$baseUrl", environment.BaseUrl);
            AddAuthenticationParameters(insertEnvironment, environment.Authentication);
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
            AddParameter(insertVariable, "$id", entry.item.Id == Guid.Empty ? Guid.NewGuid().ToString() : entry.item.Id.ToString());
            AddParameter(insertVariable, "$environmentId", environment.Id.ToString());
            AddParameter(insertVariable, "$kind", entry.kind);
            AddParameter(insertVariable, "$name", entry.item.Name);
            AddParameter(insertVariable, "$value", entry.item.Value);
            AddParameter(insertVariable, "$sortOrder", entry.index);
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

        return new Core.Environments.Environment(id, environment.Name, environment.BaseUrl, variables, headers, NormalizeAuthentication(environment.Authentication));
    }

    private static EnvironmentAuthentication? NormalizeAuthentication(EnvironmentAuthentication? authentication)
        => authentication switch
        {
            null => null,
            EnvironmentAuthenticationBearer bearer => new EnvironmentAuthenticationBearer(bearer.Token),
            EnvironmentAuthenticationBasic basic => new EnvironmentAuthenticationBasic(basic.UserName, basic.Password),
            EnvironmentAuthenticationApiKey apiKey => new EnvironmentAuthenticationApiKey(apiKey.HeaderName, apiKey.Value),
            _ => throw new NotSupportedException($"Authentication type '{authentication.GetType().Name}' is not supported.")
        };

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

        EnsureAuthenticationColumns(connection);
    }

    private static void EnsureAuthenticationColumns(SqliteConnection connection)
    {
        var existingColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        using (var command = connection.CreateCommand())
        {
            command.CommandText = "PRAGMA table_info(environments);";
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                existingColumns.Add(reader.GetString(1));
            }
        }

        AddColumnIfMissing(connection, existingColumns, "auth_kind", "ALTER TABLE environments ADD COLUMN auth_kind TEXT NULL;");
        AddColumnIfMissing(connection, existingColumns, "auth_value1", "ALTER TABLE environments ADD COLUMN auth_value1 TEXT NULL;");
        AddColumnIfMissing(connection, existingColumns, "auth_value2", "ALTER TABLE environments ADD COLUMN auth_value2 TEXT NULL;");
    }

    private static void AddColumnIfMissing(SqliteConnection connection, HashSet<string> existingColumns, string columnName, string sql)
    {
        if (existingColumns.Contains(columnName))
        {
            return;
        }

        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    private static void AddAuthenticationParameters(SqliteCommand command, EnvironmentAuthentication? authentication)
    {
        switch (authentication)
        {
            case null:
                AddParameter(command, "$authKind", DBNull.Value);
                AddParameter(command, "$authValue1", DBNull.Value);
                AddParameter(command, "$authValue2", DBNull.Value);
                return;
            case EnvironmentAuthenticationBearer bearer:
                AddParameter(command, "$authKind", nameof(EnvironmentAuthenticationKind.Bearer));
                AddParameter(command, "$authValue1", bearer.Token);
                AddParameter(command, "$authValue2", DBNull.Value);
                return;
            case EnvironmentAuthenticationBasic basic:
                AddParameter(command, "$authKind", nameof(EnvironmentAuthenticationKind.Basic));
                AddParameter(command, "$authValue1", basic.UserName);
                AddParameter(command, "$authValue2", basic.Password);
                return;
            case EnvironmentAuthenticationApiKey apiKey:
                AddParameter(command, "$authKind", nameof(EnvironmentAuthenticationKind.ApiKey));
                AddParameter(command, "$authValue1", apiKey.HeaderName);
                AddParameter(command, "$authValue2", apiKey.Value);
                return;
            default:
                throw new NotSupportedException($"Authentication type '{authentication.GetType().Name}' is not supported.");
        }
    }

    private static EnvironmentAuthentication? ReadAuthentication(SqliteDataReader reader, int kindOrdinal, int value1Ordinal, int value2Ordinal)
    {
        if (reader.IsDBNull(kindOrdinal))
        {
            return null;
        }

        var kind = reader.GetString(kindOrdinal);
        return kind switch
        {
            nameof(EnvironmentAuthenticationKind.Bearer) => new EnvironmentAuthenticationBearer(reader.IsDBNull(value1Ordinal) ? string.Empty : reader.GetString(value1Ordinal)),
            nameof(EnvironmentAuthenticationKind.Basic) => new EnvironmentAuthenticationBasic(
                reader.IsDBNull(value1Ordinal) ? string.Empty : reader.GetString(value1Ordinal),
                reader.IsDBNull(value2Ordinal) ? string.Empty : reader.GetString(value2Ordinal)),
            nameof(EnvironmentAuthenticationKind.ApiKey) => new EnvironmentAuthenticationApiKey(
                reader.IsDBNull(value1Ordinal) ? string.Empty : reader.GetString(value1Ordinal),
                reader.IsDBNull(value2Ordinal) ? string.Empty : reader.GetString(value2Ordinal)),
            _ => null
        };
    }

    private static void AddParameter(SqliteCommand command, string name, object? value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }
}
