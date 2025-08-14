using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Chat.Server;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit; // <-- required for IAsyncLifetime

namespace Chat.Tests
{
    /// <summary>
    /// Spins up the server with a unique SQLite file per factory instance.
    /// No env vars. Cleans up the DB file after tests.
    /// </summary>
    public sealed class TestChatFactory : WebApplicationFactory<Program>, IAsyncLifetime
    {
        private string _dbPath = string.Empty;

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            _dbPath = Path.Combine(Path.GetTempPath(), $"chat_{Guid.NewGuid():N}.sqlite");

            builder.ConfigureLogging(logging =>
            {
                logging.SetMinimumLevel(LogLevel.Warning);
                logging.AddFilter("Grpc.AspNetCore.Server.ServerCallHandler",
                                  LogLevel.Information);
            });

            builder.ConfigureServices(services =>
            {
                // Remove any existing string registration (conn string).
                var toRemove = services.Where(d => d.ServiceType == typeof(string)).ToList();
                foreach (var d in toRemove)
                {
                    services.Remove(d);
                }

                var connStr = new SqliteConnectionStringBuilder
                {
                    DataSource = _dbPath,
                    Mode = SqliteOpenMode.ReadWriteCreate
                }.ToString();

                services.AddSingleton(connStr);
            });
        }

        public Task InitializeAsync() => Task.CompletedTask;

        // 'new' to avoid hiding warning against WebApplicationFactory.DisposeAsync()
        public new Task DisposeAsync()
        {
            try
            {
                if (!string.IsNullOrEmpty(_dbPath) && File.Exists(_dbPath))
                {
                    File.Delete(_dbPath);
                }
            }
            catch
            {
                // Ignore cleanup failures in tests.
            }

            return Task.CompletedTask;
        }
    }
}
