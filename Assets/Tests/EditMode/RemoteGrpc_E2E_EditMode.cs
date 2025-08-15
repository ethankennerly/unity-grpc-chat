using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace MinimalChat.Tests
{
    /// <summary>
    /// End-to-end EditMode test: spins up the backend process and exercises RemoteGrpcChatService.
    /// Requires 'dotnet' available on PATH.
    /// </summary>
    public sealed class RemoteGrpc_E2E_EditMode
    {
        [Test]
        public async Task RemoteGrpc_Send_Then_Stream_EndToEnd()
        {
            var port = PickPort(5005, 5999);
            var baseUrl = "http://127.0.0.1:" + port.ToString();

            using var backend = StartBackend(baseUrl);
            try
            {
                Assert.IsTrue(await WaitForServer(baseUrl, 8000),
                    "Backend did not become ready in time.");

                var svc = new RemoteGrpcChatService(baseUrl);

                // Send two messages
                var a = await svc.SendMessageAsync(new SendMessageRequest
                {
                    Sender = "Alice",
                    Text = "Hello"
                }, CancellationToken.None);

                var b = await svc.SendMessageAsync(new SendMessageRequest
                {
                    Sender = "Bob",
                    Text = "Hi"
                }, CancellationToken.None);

                Assert.Greater(a.Id, 0);
                Assert.Greater(b.Id, a.Id);

                // Stream from a.Id - 1 and expect both a and b
                using var cts = new CancellationTokenSource(2000);

                int seen = 0;
                long lastId = 0;

                await foreach (var msg in svc.SubscribeMessagesAsync(a.Id - 1, cts.Token))
                {
                    lastId = msg.Id;
                    seen = seen + 1;

                    if (seen >= 2)
                    {
                        break;
                    }
                }

                Assert.AreEqual(b.Id, lastId);
            }
            finally
            {
                TryKill(backend);
            }
        }

        private static int PickPort(int min, int max)
        {
            var rnd = new System.Random();
            return rnd.Next(min, max);
        }

        private static Process StartBackend(string baseUrl)
        {
            // Run: dotnet run --project Backend/Chat.Server with ASPNETCORE_URLS
            var psi = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "run --project Backend/Chat.Server/Chat.Server.csproj --no-build --configuration Debug",
                WorkingDirectory = GetRepoRoot(),
                UseShellExecute = false
            };
            psi.Environment["ASPNETCORE_URLS"] = baseUrl;

            return Process.Start(psi);
        }

        private static string GetRepoRoot()
        {
            // Unity project root is repo root in this setup.
            return System.IO.Path.GetFullPath(".");
        }

        private static async Task<bool> WaitForServer(string baseUrl, int timeoutMs)
        {
            using var http = new HttpClient();
            var start = Environment.TickCount;

            while (Environment.TickCount - start < timeoutMs)
            {
                try
                {
                    var res = await http.GetAsync(baseUrl + "/");
                    if (res.IsSuccessStatusCode)
                    {
                        return true;
                    }
                }
                catch
                {
                    // keep trying
                }

                await Task.Delay(100);
            }

            return false;
        }

        private static void TryKill(Process p)
        {
            try
            {
                if (p != null && !p.HasExited)
                {
                    p.Kill();
                }
            }
            catch
            {
            }
        }
    }
}
