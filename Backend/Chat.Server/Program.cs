using Chat.Server;
using Microsoft.Data.Sqlite;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpc();

// simple connection string (file in temp dir)
var dbPath = Path.Combine(Path.GetTempPath(), "chat.sqlite");
builder.Services.AddSingleton(new SqliteConnectionStringBuilder
{
    DataSource = dbPath,
    Mode = SqliteOpenMode.ReadWriteCreate
}.ToString());

builder.Services.AddSingleton<IChatRepo, SqliteChatRepo>();
builder.Services.AddHostedService<SchemaBootstrapper>();
builder.Logging.AddFilter("Grpc.AspNetCore.Server.ServerCallHandler",
                          Microsoft.Extensions.Logging.LogLevel.Information);

var app = builder.Build();
app.MapGrpcService<GrpcChatService>();
app.MapGet("/", () => "gRPC chat server");
app.Run();

public partial class Program { } // for WebApplicationFactory
