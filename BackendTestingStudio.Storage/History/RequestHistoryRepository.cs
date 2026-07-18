using System.Text.Json;
using BackendTestingStudio.Core.History;
using Microsoft.Data.Sqlite;

namespace BackendTestingStudio.Storage.History;

internal sealed class RequestHistoryRepository : IRequestHistoryRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private const string RequestHistoryTableSql = """
        CREATE TABLE IF NOT EXISTS request_history (
            id TEXT NOT NULL PRIMARY KEY,
            created_at TEXT NOT NULL,
            environment_id TEXT NULL,
            environment_name TEXT NULL,
            method TEXT NOT NULL,
            url TEXT NOT NULL,
            headers_json TEXT NOT NULL,
            body_kind INTEGER NOT NULL,
            json_body TEXT NULL,
            multipart_json TEXT NULL,
            status_code INTEGER NOT NULL,
            response_headers_json TEXT NOT NULL,
            response_body TEXT NULL,
            elapsed_ms REAL NOT NULL
        );
        """;

    private readonly string _connectionString;

    public RequestHistoryRepository(Environments.EnvironmentStorageOptions options)
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

    public async Task<IReadOnlyList<RequestHistoryEntry>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var items = new List<RequestHistoryEntry>();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT id, created_at, environment_id, environment_name, method, url, headers_json, body_kind, json_body,
                   multipart_json, status_code, response_headers_json, response_body, elapsed_ms
            FROM request_history
            ORDER BY datetime(created_at) DESC, rowid DESC;
            """;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            items.Add(ReadEntry(reader));
        }

        return items;
    }

    public async Task<RequestHistoryEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT id, created_at, environment_id, environment_name, method, url, headers_json, body_kind, json_body,
                   multipart_json, status_code, response_headers_json, response_body, elapsed_ms
            FROM request_history
            WHERE id = $id;
            """;
        command.Parameters.AddWithValue("$id", id.ToString());

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        return await reader.ReadAsync(cancellationToken).ConfigureAwait(false)
            ? ReadEntry(reader)
            : null;
    }

    public async Task<RequestHistoryEntry> AddAsync(RequestHistoryEntry entry, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        var entity = Normalize(entry);
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO request_history (
                id, created_at, environment_id, environment_name, method, url, headers_json, body_kind,
                json_body, multipart_json, status_code, response_headers_json, response_body, elapsed_ms
            )
            VALUES (
                $id, $createdAt, $environmentId, $environmentName, $method, $url, $headersJson, $bodyKind,
                $jsonBody, $multipartJson, $statusCode, $responseHeadersJson, $responseBody, $elapsedMs
            );
            """;
        AddParameters(command, entity);
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        return entity;
    }

    private static void AddParameters(SqliteCommand command, RequestHistoryEntry entry)
    {
        AddParameter(command, "$id", entry.Id.ToString());
        AddParameter(command, "$createdAt", entry.CreatedAt.UtcDateTime.ToString("O"));
        AddParameter(command, "$environmentId", entry.EnvironmentId?.ToString());
        AddParameter(command, "$environmentName", entry.EnvironmentName);
        AddParameter(command, "$method", entry.Request.Method);
        AddParameter(command, "$url", entry.Request.Url);
        AddParameter(command, "$headersJson", JsonSerializer.Serialize(entry.Request.Headers, JsonOptions));
        AddParameter(command, "$bodyKind", (int)entry.Request.BodyKind);
        AddParameter(command, "$jsonBody", entry.Request.JsonBody);
        AddParameter(command, "$multipartJson", SerializeMultipart(entry.Request.MultipartParts));
        AddParameter(command, "$statusCode", (int)entry.Response.StatusCode);
        AddParameter(command, "$responseHeadersJson", JsonSerializer.Serialize(entry.Response.Headers, JsonOptions));
        AddParameter(command, "$responseBody", entry.Response.Body);
        AddParameter(command, "$elapsedMs", entry.ElapsedMilliseconds);
    }

    private static void AddParameter(SqliteCommand command, string name, object? value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }

    private static string? SerializeMultipart(IReadOnlyList<RequestHistoryMultipartPart> multipartParts)
        => multipartParts.Count == 0
            ? null
            : JsonSerializer.Serialize(multipartParts, JsonOptions);

    private static RequestHistoryEntry ReadEntry(SqliteDataReader reader)
    {
        var requestHeaders = JsonSerializer.Deserialize<Dictionary<string, string?>>(reader.GetString(6), JsonOptions)
            ?? new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        var bodyKind = (RequestHistoryBodyKind)reader.GetInt32(7);
        var multipartParts = reader.IsDBNull(9)
            ? []
            : JsonSerializer.Deserialize<List<RequestHistoryMultipartPart>>(reader.GetString(9), JsonOptions) ?? [];
        var responseHeaders = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(reader.GetString(11), JsonOptions)
            ?? new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        return new RequestHistoryEntry(
            Guid.Parse(reader.GetString(0)),
            DateTimeOffset.Parse(reader.GetString(1)),
            reader.IsDBNull(2) ? null : Guid.Parse(reader.GetString(2)),
            reader.IsDBNull(3) ? null : reader.GetString(3),
            new RequestHistoryRequestSnapshot(
                reader.GetString(4),
                reader.GetString(5),
                requestHeaders,
                bodyKind,
                reader.IsDBNull(8) ? null : reader.GetString(8),
                multipartParts),
            new RequestHistoryResponseSnapshot(
                (System.Net.HttpStatusCode)reader.GetInt32(10),
                responseHeaders.ToDictionary(pair => pair.Key, pair => (IReadOnlyList<string>)pair.Value, StringComparer.OrdinalIgnoreCase),
                reader.IsDBNull(12) ? null : reader.GetString(12)),
            reader.GetDouble(13));
    }

    private static RequestHistoryEntry Normalize(RequestHistoryEntry entry)
        => new(
            entry.Id == Guid.Empty ? Guid.NewGuid() : entry.Id,
            entry.CreatedAt,
            entry.EnvironmentId,
            entry.EnvironmentName,
            new RequestHistoryRequestSnapshot(
                entry.Request.Method,
                entry.Request.Url,
                entry.Request.Headers,
                entry.Request.BodyKind,
                entry.Request.JsonBody,
                entry.Request.MultipartParts),
            new RequestHistoryResponseSnapshot(
                entry.Response.StatusCode,
                entry.Response.Headers,
                entry.Response.Body),
            entry.ElapsedMilliseconds);

    private SqliteConnection CreateConnection() => new(_connectionString);

    private void EnsureCreated()
    {
        using var connection = CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = RequestHistoryTableSql;
        command.ExecuteNonQuery();
    }
}
