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
    /// Requires dotnet to be installed; test is Inconclusive if not found on this machine/session.
    /// </summary>
    public sealed class RemoteGrpc_E2E_EditMode
    {
        [Test]
        public async Task RemoteGrpc_Send_Then_Stream_EndToEnd()
        {
            var port = PickPort(5005, 5999);
            var baseUrl = "http://127.0.0.1:" + port.ToString();

            var dotnetExe = ResolveDotnetPath();
            if (string.IsNullOrEmpty(dotnetExe))
            {
                Assert.Inconclusive("dotnet not found in current Unity test environment PATH. " +
                                    "Install .NET or expose DOTNET_ROOT / add to PATH.");
            }

            using var backend = StartBackend(baseUrl, dotnetExe);
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

        private static Process StartBackend(string baseUrl, string dotnetExe)
        {
            // Create per-test temp DB path
            var tmp = System.IO.Path.Combine(System.IO.Path.GetTempPath(),
                "chat-unity-e2e-" + System.Guid.NewGuid().ToString("N") + ".db");

            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = dotnetExe,
                Arguments = "run --project Backend/Chat.Server/Chat.Server.csproj --no-build --configuration Debug",
                WorkingDirectory = GetRepoRoot(),
                UseShellExecute = false,
                RedirectStandardOutput = false,
                RedirectStandardError = false
            };
            psi.Environment["ASPNETCORE_URLS"] = baseUrl;
            psi.Environment["CHAT_CONNECTION"] = "Data Source=" + tmp;

            return System.Diagnostics.Process.Start(psi);
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
                    // Backend may 404 on '/', but still means Kestrel is up. Treat any response as ready.
                    var res = await http.GetAsync(baseUrl + "/");
                    if (res != null)
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

        // ---------- dotnet path resolution -----------------------------------

        private static string ResolveDotnetPath()
        {
            // 1) DOTNET_EXE / DOTNET_ROOT envs
            var fromExe = TryFile(System.Environment.GetEnvironmentVariable("DOTNET_EXE"));
            if (!string.IsNullOrEmpty(fromExe)) { return fromExe; }

            var dotnetRoot = System.Environment.GetEnvironmentVariable("DOTNET_ROOT");
            var fromRoot = TryFile(Combine(dotnetRoot, "dotnet"));
            if (!string.IsNullOrEmpty(fromRoot)) { return fromRoot; }

            var dotnetRootX64 = System.Environment.GetEnvironmentVariable("DOTNET_ROOT_X64");
            var fromRootX64 = TryFile(Combine(dotnetRootX64, "dotnet"));
            if (!string.IsNullOrEmpty(fromRootX64)) { return fromRootX64; }

            // 2) Common macOS/Homebrew locations
            var candidates = new[]
            {
                "/usr/local/bin/dotnet",
                "/opt/homebrew/bin/dotnet",
                "/usr/local/share/dotnet/dotnet",
                "/usr/bin/dotnet"
            };

            for (int i = 0; i < candidates.Length; i++)
            {
                var c = candidates[i];
                if (System.IO.File.Exists(c)) { return c; }
            }

            // 3) which dotnet
            var which = WhichDotnet();
            if (!string.IsNullOrEmpty(which) && System.IO.File.Exists(which))
            {
                return which;
            }

            return string.Empty;
        }

        private static string WhichDotnet()
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "/usr/bin/which",
                    Arguments = "dotnet",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using var p = Process.Start(psi);
                if (p == null)
                {
                    return string.Empty;
                }

                string output = p.StandardOutput.ReadLine();
                p.WaitForExit(1000);
                if (!string.IsNullOrEmpty(output))
                {
                    return output.Trim();
                }
            }
            catch { }

            return string.Empty;
        }

        private static string TryFile(string path)
        {
            if (string.IsNullOrEmpty(path)) { return string.Empty; }
            return System.IO.File.Exists(path) ? path : string.Empty;
        }

        private static string Combine(string a, string b)
        {
            if (string.IsNullOrEmpty(a)) { return string.Empty; }
            return System.IO.Path.Combine(a, b);
        }
    }
}