namespace Chat.Server;

using Microsoft.Data.Sqlite;

public sealed class SqliteChatRepo : IChatRepo
{
    private readonly string _connStr;
    public SqliteChatRepo(string connStr) { _connStr = connStr; }

    public async Task<(long, string)> InsertAsync(string sender, string text, CancellationToken ct)
    {
        await using var conn = new SqliteConnection(_connStr);
        await conn.OpenAsync(ct);

        var ts = DateTimeOffset.UtcNow.ToString("o");
        var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT INTO messages(sender,text,created_at) VALUES($s,$t,$c); SELECT last_insert_rowid();";
        cmd.Parameters.AddWithValue("$s", sender);
        cmd.Parameters.AddWithValue("$t", text);
        cmd.Parameters.AddWithValue("$c", ts);
        var id = (long)(await cmd.ExecuteScalarAsync(ct))!;
        return (id, ts);
    }

    public async Task<List<(long, string, string, string)>> ReadSinceAsync(long sinceId, CancellationToken ct)
    {
        await using var conn = new SqliteConnection(_connStr);
        await conn.OpenAsync(ct);

        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT id,sender,text,created_at FROM messages WHERE id > $id ORDER BY id ASC;";
        cmd.Parameters.AddWithValue("$id", sinceId);

        var list = new List<(long, string, string, string)>(32);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            list.Add((reader.GetInt64(0), reader.GetString(1), reader.GetString(2), reader.GetString(3)));
        }
        return list;
    }
}
