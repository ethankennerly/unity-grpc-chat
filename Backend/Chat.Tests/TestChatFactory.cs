using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Chat.Tests
{
    /// <summary>
    /// In-process test server using a per-run temp SQLite file.
    /// </summary>
    public sealed class TestChatFactory : WebApplicationFactory<global::Program>
    {
        private string _dbPath = string.Empty;

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureLogging(logging =>
            {
                logging.SetMinimumLevel(LogLevel.Information);
                logging.AddFilter("Grpc.AspNetCore.Server.ServerCallHandler",
                                  LogLevel.Information);
            });

            var dir = Path.Combine(Path.GetTempPath(), "unity-grpc-chat-tests");
            Directory.CreateDirectory(dir);

            _dbPath = Path.Combine(dir, $"chat_{Guid.NewGuid():N}.sqlite");

            var connString = new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder
            {
                DataSource = _dbPath,
                Mode = Microsoft.Data.Sqlite.SqliteOpenMode.ReadWriteCreate,
                Cache = Microsoft.Data.Sqlite.SqliteCacheMode.Shared
            }.ToString();

            builder.ConfigureAppConfiguration((context, config) =>
            {
                var overrides = new Dictionary<string, string?>
                {
                    ["Chat:ConnectionString"] = connString,
                    // Optional: tell Program.cs to drop table in Testing.
                    ["DROP_DB_ON_STARTUP"] = "1"
                };
                config.AddInMemoryCollection(overrides);
            });

            // Backstop if anything reads env directly.
            Environment.SetEnvironmentVariable("CHAT_CONNECTION", connString);
            Environment.SetEnvironmentVariable("DROP_DB_ON_STARTUP", "1");
        }

        public GrpcChannel CreateGrpcChannel()
        {
            var handler = this.Server.CreateHandler();
            var httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri("http://localhost")
            };

            return GrpcChannel.ForAddress(httpClient.BaseAddress!,
                                          new GrpcChannelOptions
                                          {
                                              HttpClient = httpClient
                                          });
        }

        public Chat.Proto.ChatService.ChatServiceClient CreateChatClient()
        {
            return new Chat.Proto.ChatService.ChatServiceClient(CreateGrpcChannel());
        }
    }
}
