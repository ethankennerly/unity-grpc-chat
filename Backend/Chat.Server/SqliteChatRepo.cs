namespace Chat.Server;

using Microsoft.Data.Sqlite;

public sealed class SqliteChatRepo : IChatRepo
{
    private readonly string _connStr;

    public SqliteChatRepo(string connStr)
    {
        _connStr = connStr;
    }

    public async Task<(long, string)> InsertAsync(
        string sender,
        string text,
        CancellationToken ct)
    {
        await using var conn = new SqliteConnection(_connStr);
        await conn.OpenAsync(ct);

        var ts = DateTimeOffset.UtcNow.ToString("o");

        // Begin a transaction; some frameworks return DbTransaction.
        await using var tx = await conn.BeginTransactionAsync(ct);
        var sqliteTx = (SqliteTransaction)tx;

        // Insert + purge inside the same transaction.
        var ins = conn.CreateCommand();
        ins.Transaction = sqliteTx;
        ins.CommandText =
            "INSERT INTO messages(sender, text, created_at) VALUES($s,$t,$c); " +
            "SELECT last_insert_rowid();";
        ins.Parameters.AddWithValue("$s", sender);
        ins.Parameters.AddWithValue("$t", text);
        ins.Parameters.AddWithValue("$c", ts);
        var id = (long)(await ins.ExecuteScalarAsync(ct))!;

        var purge = conn.CreateCommand();
        purge.Transaction = sqliteTx;
        purge.CommandText =
            "DELETE FROM messages " +
            "WHERE id <= (SELECT IFNULL(MAX(id),0) - 1024 FROM messages);";
        await purge.ExecuteNonQueryAsync(ct);

        await tx.CommitAsync(ct);
        return (id, ts);
    }

    public async Task<List<(long, string, string, string)>> ReadSinceAsync(
        long sinceId,
        CancellationToken ct)
    {
        await using var conn = new SqliteConnection(_connStr);
        await conn.OpenAsync(ct);

        var cmd = conn.CreateCommand();
        cmd.CommandText =
            "SELECT id, sender, text, created_at " +
            "FROM messages WHERE id > $id ORDER BY id ASC LIMIT 256;";
        cmd.Parameters.AddWithValue("$id", sinceId);

        var list = new List<(long, string, string, string)>(capacity: 256);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            list.Add(
                (reader.GetInt64(0),
                 reader.GetString(1),
                 reader.GetString(2),
                 reader.GetString(3)));
        }

        return list;
    }
}
