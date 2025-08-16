using Chat.Server;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddFilter(
    "Grpc.AspNetCore.Server.ServerCallHandler",
    Microsoft.Extensions.Logging.LogLevel.Information);

builder.Services.AddGrpc();

var cfg = builder.Configuration;

// Prefer explicit connection string if set
var conn =
    cfg["Chat:ConnectionString"] ??
    cfg["CHAT_CONNECTION"] ??
    "Data Source=./data/chat-dev.db"; // default for dev runtime

var pid = Environment.ProcessId;
var urls = Environment.GetEnvironmentVariable("ASPNETCORE_URLS") ?? "(unset)";
var chatConnEnv = Environment.GetEnvironmentVariable("CHAT_CONNECTION") ?? "(unset)";
Console.WriteLine($"[boot] PID={pid}");
Console.WriteLine($"[cfg] ASPNETCORE_URLS = {urls}");
Console.WriteLine($"[cfg] CHAT_CONNECTION (env) = {chatConnEnv}");
Console.WriteLine($"[cfg] Chat connection (effective) = {conn}");

// Ensure the data directory exists if path-based
try
{
    var csLower = conn.ToLowerInvariant();
    if (csLower.StartsWith("data source="))
    {
        var path = conn.Substring("Data Source=".Length).Trim('"', ' ');
        var dir = System.IO.Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir) && !System.IO.Directory.Exists(dir))
        {
            System.IO.Directory.CreateDirectory(dir);
        }
        Console.WriteLine($"[cfg] Ensured data dir: {dir}");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"[cfg][warn] Ensure data dir failed: {ex.Message}");
}

builder.Services.AddSingleton<IChatRepo>(_ => new SqliteChatRepo(conn));
builder.Services.AddSingleton<SchemaBootstrapper>(_ => new SchemaBootstrapper(conn));

var app = builder.Build();

app.UseGrpcWeb();
app.MapGrpcService<GrpcChatService>().EnableGrpcWeb();
app.MapGet("/", () => "gRPC chat server (Unity Minimal Chat).");

// Ensure DB schema exists for the configured connection (works for temp DBs too)
using (var scope = app.Services.CreateScope())
{
    var boot = scope.ServiceProvider.GetRequiredService<SchemaBootstrapper>();

    var env = app.Environment;
    var dropFlag = Environment.GetEnvironmentVariable("DROP_DB_ON_STARTUP");
    var shouldDrop = env.IsEnvironment("Testing") || dropFlag == "1";

    if (shouldDrop)
    {
        Console.WriteLine("[schema] Testing: dropping messages table if exists.");
        boot.DropMessagesIfExists();
    }

    Console.WriteLine("[schema] Ensuring schema…");
    boot.Ensure();
    Console.WriteLine("[schema] Ready.");
}

app.Run();

public partial class Program { }
