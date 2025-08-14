using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Hosting;

namespace Chat.Server;

public sealed class SchemaBootstrapper : IHostedService
{
    private readonly string _connStr;
    public SchemaBootstrapper(string connStr) { _connStr = connStr; }

    public async Task StartAsync(CancellationToken ct)
    {
        await using var conn = new SqliteConnection(_connStr);
        await conn.OpenAsync(ct);
        var cmd = conn.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS messages(
              id INTEGER PRIMARY KEY AUTOINCREMENT,
              sender TEXT NOT NULL,
              text   TEXT NOT NULL,
              created_at TEXT NOT NULL
            );
            """;
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
