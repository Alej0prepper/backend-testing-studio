using Microsoft.Data.Sqlite;

namespace BackendTestingStudio.Storage;

internal static class SchemaMigrationRunner
{
    private static readonly object Gate = new();

    public static void Apply(
        SqliteConnection connection,
        int version,
        string name,
        Action<SqliteConnection> migration)
    {
        lock (Gate)
        {
            EnsureTable(connection);
            using (var check = connection.CreateCommand())
            {
                check.CommandText = "SELECT COUNT(*) FROM schema_migrations WHERE version = $version;";
                check.Parameters.AddWithValue("$version", version);
                if (Convert.ToInt32(check.ExecuteScalar()) > 0)
                {
                    return;
                }
            }

            migration(connection);
            using var record = connection.CreateCommand();
            record.CommandText =
                """
                INSERT INTO schema_migrations(version, name, applied_at)
                VALUES ($version, $name, $appliedAt);
                """;
            record.Parameters.AddWithValue("$version", version);
            record.Parameters.AddWithValue("$name", name);
            record.Parameters.AddWithValue("$appliedAt", DateTimeOffset.UtcNow.ToString("O"));
            record.ExecuteNonQuery();
        }
    }

    private static void EnsureTable(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            CREATE TABLE IF NOT EXISTS schema_migrations (
                version INTEGER NOT NULL PRIMARY KEY,
                name TEXT NOT NULL DEFAULT '',
                applied_at TEXT NOT NULL
            );
            """;
        command.ExecuteNonQuery();

        var hasName = false;
        command.CommandText = "PRAGMA table_info(schema_migrations);";
        using (var reader = command.ExecuteReader())
        {
            while (reader.Read())
            {
                hasName |= string.Equals(reader.GetString(1), "name", StringComparison.OrdinalIgnoreCase);
            }
        }

        if (!hasName)
        {
            command.CommandText = "ALTER TABLE schema_migrations ADD COLUMN name TEXT NOT NULL DEFAULT '';";
            command.ExecuteNonQuery();
        }

        RepairLegacyVersionOne(connection);
    }

    private static void RepairLegacyVersionOne(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT name FROM schema_migrations WHERE version = 1;";
        var existingName = command.ExecuteScalar() as string;
        if (existingName is null || !string.IsNullOrEmpty(existingName))
        {
            return;
        }

        if (TableExists(connection, "environments"))
        {
            command.CommandText = "UPDATE schema_migrations SET name = 'environments-base' WHERE version = 1;";
            command.ExecuteNonQuery();
        }
        else if (TableExists(connection, "scenario_runs"))
        {
            command.CommandText =
                "UPDATE schema_migrations SET version = 30, name = 'scenario-runs-legacy' WHERE version = 1;";
            command.ExecuteNonQuery();
        }
    }

    private static bool TableExists(SqliteConnection connection, string table)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = $name;";
        command.Parameters.AddWithValue("$name", table);
        return Convert.ToInt32(command.ExecuteScalar()) > 0;
    }
}
