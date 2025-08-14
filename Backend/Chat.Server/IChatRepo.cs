namespace Chat.Server;

public interface IChatRepo
{
    Task<(long Id, string CreatedAt)> InsertAsync(string sender, string text, CancellationToken ct);
    Task<List<(long Id, string Sender, string Text, string CreatedAt)>> ReadSinceAsync(long sinceId, CancellationToken ct);
}
