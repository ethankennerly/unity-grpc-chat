using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace Chat.Server
{
    /// <summary>
    /// SQLite implementation with a simple OnInserted event for live tailing.
    /// </summary>
    public sealed class SqliteChatRepo : IChatRepo
    {
        private readonly string _connectionString;

        public SqliteChatRepo(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new ArgumentException("connectionString required",
                                            nameof(connectionString));
            }

            _connectionString = connectionString;
        }

        public event Action<RepoMessage>? OnInserted;

        public async Task<(long id, long createdAt)> InsertAsync(
            string sender,
            string text,
            CancellationToken ct)
        {
            // Created-at epoch ms.
            var createdAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            long id;

            using (var conn = new SqliteConnection(_connectionString))
            {
                await conn.OpenAsync(ct);

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText =
                        "INSERT INTO messages (created_at, sender, text) " +
                        "VALUES ($created_at, $sender, $text); " +
                        "SELECT last_insert_rowid();";

                    cmd.Parameters.AddWithValue("$created_at", createdAt);
                    cmd.Parameters.AddWithValue("$sender", sender);
                    cmd.Parameters.AddWithValue("$text", text);

                    var obj = await cmd.ExecuteScalarAsync(ct);
                    id = Convert.ToInt64(obj);
                }
            }

            // Notify subscribers after commit.
            try
            {
                OnInserted?.Invoke(new RepoMessage
                {
                    Id        = id,
                    CreatedAt = createdAt,
                    Sender    = sender,
                    Text      = text
                });
            }
            catch
            {
                // Never allow subscriber exceptions to bubble; they are best-effort.
            }

            return (id, createdAt);
        }

        public async Task<List<RepoMessage>> ReadBacklogAsync(
            long sinceId,
            CancellationToken ct)
        {
            var list = new List<RepoMessage>(128);

            using (var conn = new SqliteConnection(_connectionString))
            {
                await conn.OpenAsync(ct);

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText =
                        "SELECT id, created_at, sender, text " +
                        "FROM messages " +
                        "WHERE id > $since " +
                        "ORDER BY id ASC;";
                    cmd.Parameters.AddWithValue("$since", sinceId);

                    using (var reader = await cmd.ExecuteReaderAsync(ct))
                    {
                        while (await reader.ReadAsync(ct))
                        {
                            var m = new RepoMessage
                            {
                                Id        = reader.GetInt64(0),
                                CreatedAt = reader.GetInt64(1),
                                Sender    = reader.GetString(2),
                                Text      = reader.GetString(3)
                            };

                            list.Add(m);
                        }
                    }
                }
            }

            return list;
        }
    }
}
