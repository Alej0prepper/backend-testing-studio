using System.Text.Json;
using BackendTestingStudio.Core.Payloads;
using Microsoft.Data.Sqlite;

namespace BackendTestingStudio.Storage.Payloads;

internal sealed class PayloadRepository : IPayloadRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private const string PayloadTableSql = """
        CREATE TABLE IF NOT EXISTS payloads (
            id TEXT NOT NULL PRIMARY KEY,
            name TEXT NOT NULL,
            description TEXT NOT NULL,
            json TEXT NOT NULL
        );
        """;

    private const string PayloadVariableTableSql = """
        CREATE TABLE IF NOT EXISTS payload_variables (
            id TEXT NOT NULL PRIMARY KEY,
            payload_id TEXT NOT NULL,
            name TEXT NOT NULL,
            value TEXT NULL,
            sort_order INTEGER NOT NULL,
            FOREIGN KEY (payload_id) REFERENCES payloads(id) ON DELETE CASCADE
        );
        """;

    private const string PayloadTagTableSql = """
        CREATE TABLE IF NOT EXISTS payload_tags (
            payload_id TEXT NOT NULL,
            tag TEXT NOT NULL,
            sort_order INTEGER NOT NULL,
            FOREIGN KEY (payload_id) REFERENCES payloads(id) ON DELETE CASCADE
        );
        """;

    private readonly string _connectionString;

    public PayloadRepository(Environments.EnvironmentStorageOptions options)
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

    public async Task<IReadOnlyList<PayloadDefinition>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var payloads = new Dictionary<Guid, PayloadBuilder>();

        await using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT id, name, description, json FROM payloads ORDER BY name;";
            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var id = Guid.Parse(reader.GetString(0));
                payloads[id] = new PayloadBuilder(
                    id,
                    reader.GetString(1),
                    reader.GetString(2),
                    reader.GetString(3));
            }
        }

        if (payloads.Count == 0)
        {
            return [];
        }

        await using (var command = connection.CreateCommand())
        {
            command.CommandText = """
                SELECT payload_id, id, name, value
                FROM payload_variables
                ORDER BY sort_order ASC;
                """;
            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var payloadId = Guid.Parse(reader.GetString(0));
                if (!payloads.TryGetValue(payloadId, out var payload))
                {
                    continue;
                }

                payload.Variables.Add(new PayloadVariable(
                    Guid.Parse(reader.GetString(1)),
                    reader.GetString(2),
                    reader.IsDBNull(3) ? null : reader.GetString(3)));
            }
        }

        await using (var command = connection.CreateCommand())
        {
            command.CommandText = """
                SELECT payload_id, tag
                FROM payload_tags
                ORDER BY sort_order ASC;
                """;
            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var payloadId = Guid.Parse(reader.GetString(0));
                if (!payloads.TryGetValue(payloadId, out var payload))
                {
                    continue;
                }

                payload.Tags.Add(reader.GetString(1));
            }
        }

        return payloads.Values
            .OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .Select(item => item.ToPayload())
            .ToArray();
    }

    public async Task<PayloadDefinition?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var all = await GetAllAsync(cancellationToken).ConfigureAwait(false);
        return all.FirstOrDefault(item => item.Id == id);
    }

    public async Task<PayloadDefinition> CreateAsync(PayloadDefinition payload, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(payload);

        var entity = Normalize(payload);
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        await InsertAsync(connection, transaction, entity, cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

        return entity;
    }

    public async Task<PayloadDefinition> UpdateAsync(PayloadDefinition payload, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(payload);

        var entity = Normalize(payload);
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        await using (var deleteVariables = connection.CreateCommand())
        {
            deleteVariables.Transaction = transaction;
            deleteVariables.CommandText = "DELETE FROM payload_variables WHERE payload_id = $payloadId;";
            AddParameter(deleteVariables, "$payloadId", entity.Id.ToString());
            await deleteVariables.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        await using (var deleteTags = connection.CreateCommand())
        {
            deleteTags.Transaction = transaction;
            deleteTags.CommandText = "DELETE FROM payload_tags WHERE payload_id = $payloadId;";
            AddParameter(deleteTags, "$payloadId", entity.Id.ToString());
            await deleteTags.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        await using (var updatePayload = connection.CreateCommand())
        {
            updatePayload.Transaction = transaction;
            updatePayload.CommandText = """
                UPDATE payloads
                SET name = $name,
                    description = $description,
                    json = $json
                WHERE id = $id;
                """;
            AddParameter(updatePayload, "$id", entity.Id.ToString());
            AddParameter(updatePayload, "$name", entity.Name);
            AddParameter(updatePayload, "$description", entity.Description);
            AddParameter(updatePayload, "$json", entity.Json);
            var affected = await updatePayload.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            if (affected == 0)
            {
                throw new KeyNotFoundException($"Payload '{entity.Id}' was not found.");
            }
        }

        await InsertDetailsAsync(connection, transaction, entity, cancellationToken).ConfigureAwait(false);
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
            deleteVariables.CommandText = "DELETE FROM payload_variables WHERE payload_id = $payloadId;";
            AddParameter(deleteVariables, "$payloadId", id.ToString());
            await deleteVariables.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        await using (var deleteTags = connection.CreateCommand())
        {
            deleteTags.Transaction = transaction;
            deleteTags.CommandText = "DELETE FROM payload_tags WHERE payload_id = $payloadId;";
            AddParameter(deleteTags, "$payloadId", id.ToString());
            await deleteTags.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        await using (var deletePayload = connection.CreateCommand())
        {
            deletePayload.Transaction = transaction;
            deletePayload.CommandText = "DELETE FROM payloads WHERE id = $id;";
            AddParameter(deletePayload, "$id", id.ToString());
            await deletePayload.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task InsertAsync(SqliteConnection connection, SqliteTransaction transaction, PayloadDefinition payload, CancellationToken cancellationToken)
    {
        await using (var insertPayload = connection.CreateCommand())
        {
            insertPayload.Transaction = transaction;
            insertPayload.CommandText = """
                INSERT INTO payloads (id, name, description, json)
                VALUES ($id, $name, $description, $json);
                """;
            AddParameter(insertPayload, "$id", payload.Id.ToString());
            AddParameter(insertPayload, "$name", payload.Name);
            AddParameter(insertPayload, "$description", payload.Description);
            AddParameter(insertPayload, "$json", payload.Json);
            await insertPayload.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        await InsertDetailsAsync(connection, transaction, payload, cancellationToken).ConfigureAwait(false);
    }

    private async Task InsertDetailsAsync(SqliteConnection connection, SqliteTransaction transaction, PayloadDefinition payload, CancellationToken cancellationToken)
    {
        foreach (var variable in payload.Variables.Select((item, index) => (item, index)))
        {
            await using var insertVariable = connection.CreateCommand();
            insertVariable.Transaction = transaction;
            insertVariable.CommandText = """
                INSERT INTO payload_variables (id, payload_id, name, value, sort_order)
                VALUES ($id, $payloadId, $name, $value, $sortOrder);
                """;
            AddParameter(insertVariable, "$id", variable.item.Id == Guid.Empty ? Guid.NewGuid().ToString() : variable.item.Id.ToString());
            AddParameter(insertVariable, "$payloadId", payload.Id.ToString());
            AddParameter(insertVariable, "$name", variable.item.Name);
            AddParameter(insertVariable, "$value", variable.item.Value);
            AddParameter(insertVariable, "$sortOrder", variable.index);
            await insertVariable.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        foreach (var tag in payload.Tags.Select((item, index) => (item, index)))
        {
            await using var insertTag = connection.CreateCommand();
            insertTag.Transaction = transaction;
            insertTag.CommandText = """
                INSERT INTO payload_tags (payload_id, tag, sort_order)
                VALUES ($payloadId, $tag, $sortOrder);
                """;
            AddParameter(insertTag, "$payloadId", payload.Id.ToString());
            AddParameter(insertTag, "$tag", tag.item);
            AddParameter(insertTag, "$sortOrder", tag.index);
            await insertTag.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private static PayloadDefinition Normalize(PayloadDefinition payload)
        => new(
            payload.Id == Guid.Empty ? Guid.NewGuid() : payload.Id,
            payload.Name,
            payload.Description,
            payload.Json,
            payload.Variables.Select(variable => new PayloadVariable(variable.Id == Guid.Empty ? Guid.NewGuid() : variable.Id, variable.Name, variable.Value)).ToArray(),
            payload.Tags.ToArray());

    private SqliteConnection CreateConnection() => new(_connectionString);

    private void EnsureCreated()
    {
        using var connection = CreateConnection();
        connection.Open();

        using (var command = connection.CreateCommand())
        {
            command.CommandText = PayloadTableSql;
            command.ExecuteNonQuery();
        }

        using (var command = connection.CreateCommand())
        {
            command.CommandText = PayloadVariableTableSql;
            command.ExecuteNonQuery();
        }

        using (var command = connection.CreateCommand())
        {
            command.CommandText = PayloadTagTableSql;
            command.ExecuteNonQuery();
        }
    }

    private static void AddParameter(SqliteCommand command, string name, object? value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }

    private sealed class PayloadBuilder
    {
        public PayloadBuilder(Guid id, string name, string description, string json)
        {
            Id = id;
            Name = name;
            Description = description;
            Json = json;
        }

        public Guid Id { get; }
        public string Name { get; }
        public string Description { get; }
        public string Json { get; }
        public List<PayloadVariable> Variables { get; } = [];
        public List<string> Tags { get; } = [];

        public PayloadDefinition ToPayload() => new(Id, Name, Description, Json, Variables, Tags);
    }
}
