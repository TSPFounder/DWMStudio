// FusionApplication.cs
// Minimal in-project Fusion bridge. Pings the DWM Fusion add-in over HTTP.
// Replace with a ProjectReference to FusionLibrary once you want the full API.

using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Fusion.Application
{
    public sealed class FusionApplication : IAsyncDisposable
    {
        private static readonly HttpClient _http = new()
        {
            Timeout = TimeSpan.FromSeconds(3)
        };

        public string Version { get; private set; } = "(not connected)";

        public async Task<bool> PingAsync()
        {
            try
            {
                using var resp = await _http.GetAsync("http://127.0.0.1:18750/ping");
                if (resp.IsSuccessStatusCode)
                {
                    Version = "Fusion add-in";
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        public Task OpenDocumentAsync(string path) => Task.CompletedTask;

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}