using Chat.Server;
using Microsoft.Data.Sqlite;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddFilter(
    "Grpc.AspNetCore.Server.ServerCallHandler",
    Microsoft.Extensions.Logging.LogLevel.Information);

builder.Services.AddGrpc();

// Local file DB by default (override via tests)
var dbPath = Path.Combine(Path.GetTempPath(), "chat.sqlite");

builder.Services.AddSingleton(new SqliteConnectionStringBuilder
{
    DataSource = dbPath,
    Mode = SqliteOpenMode.ReadWriteCreate
}.ToString());

builder.Services.AddSingleton<IChatRepo, SqliteChatRepo>();
builder.Services.AddHostedService<SchemaBootstrapper>();

var app = builder.Build();

app.MapGrpcService<GrpcChatService>();
app.MapGet("/", () => "gRPC chat server (Unity Minimal Chat).");

app.Run();

public partial class Program { }
