using System;
using Microsoft.Data.Sqlite;

namespace Chat.Server
{
    /// <summary>
    /// Ensures the SQLite schema matches the current app expectations.
    /// If a legacy schema is detected (presence of 'ts' or absence of 'created_at'),
    /// the messages table is dropped and recreated. Idempotent.
    /// </summary>
    public sealed class SchemaBootstrapper
    {
        private readonly string _connectionString;

        public SchemaBootstrapper(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new ArgumentException("connectionString required", nameof(connectionString));
            }

            _connectionString = connectionString;
        }

        public void Ensure()
        {
            using (var conn = new SqliteConnection(_connectionString))
            {
                conn.Open();
                Console.WriteLine("[schema] Ensure start for: " + _connectionString);

                if (!TableExists(conn, "messages"))
                {
                    Console.WriteLine("[schema] Creating fresh messages table");
                    CreateFreshMessagesTable(conn);
                }
                else
                {
                    var hasCreatedAt = ColumnExists(conn, "messages", "created_at");
                    var hasTs        = ColumnExists(conn, "messages", "ts");

                    // Normalize if ANY legacy indicator is present:
                    // - hasTs (regardless of created_at)
                    // - or created_at missing
                    if (hasTs || !hasCreatedAt)
                    {
                        Console.WriteLine("[schema] Legacy layout detected " +
                            $"(hasTs={hasTs}, hasCreatedAt={hasCreatedAt}) → DROP & RECREATE");

                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.CommandText = "DROP TABLE messages;";
                            cmd.ExecuteNonQuery();
                        }

                        CreateFreshMessagesTable(conn);
                    }
                    else
                    {
                        Console.WriteLine("[schema] Messages table OK (created_at present, no ts).");
                    }
                }

                // Indexes (safe to re-run)
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "CREATE INDEX IF NOT EXISTS idx_messages_id ON messages(id);";
                    cmd.ExecuteNonQuery();
                }
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText =
                        "CREATE INDEX IF NOT EXISTS idx_messages_created_at ON messages(created_at);";
                    cmd.ExecuteNonQuery();
                }

                Console.WriteLine("[schema] Ensure done.");
            }
        }

        public void DropMessagesIfExists()
        {
            using var conn = new Microsoft.Data.Sqlite.SqliteConnection(_connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DROP TABLE IF EXISTS messages;";
            cmd.ExecuteNonQuery();
        }

        private static void CreateFreshMessagesTable(SqliteConnection conn)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText =
                    "CREATE TABLE IF NOT EXISTS messages (" +
                    "  id INTEGER PRIMARY KEY AUTOINCREMENT," +
                    "  created_at INTEGER NOT NULL," +  // epoch millis
                    "  sender TEXT NOT NULL," +
                    "  text TEXT NOT NULL" +
                    ");";
                cmd.ExecuteNonQuery();
            }
        }

        private static bool TableExists(SqliteConnection conn, string table)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText =
                    "SELECT 1 FROM sqlite_master WHERE type='table' AND name=$name LIMIT 1;";
                cmd.Parameters.AddWithValue("$name", table);
                return cmd.ExecuteScalar() != null;
            }
        }

        private static bool ColumnExists(SqliteConnection conn, string table, string column)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = $"PRAGMA table_info({table});";
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        // PRAGMA table_info: cid|name|type|notnull|dflt_value|pk
                        var name = reader.GetString(1);
                        if (string.Equals(name, column, StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public System.Threading.Tasks.Task EnsureAsync(
            System.Threading.CancellationToken ct = default)
        {
            Ensure();
            return System.Threading.Tasks.Task.CompletedTask;
        }
    }
}
