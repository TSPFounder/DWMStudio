// FusionApplication.cs
// Stub for the Autodesk Fusion 360 add-in bridge.
// Replace with a real implementation once the Fusion add-in pipe is wired up.

using System.Threading.Tasks;

namespace Fusion.Application
{
    public sealed class FusionApplication : System.IAsyncDisposable
    {
        public string Version { get; private set; } = string.Empty;

        public Task<bool> PingAsync() => Task.FromResult(false);

        public Task OpenDocumentAsync(string path) => Task.CompletedTask;

        public System.Threading.Tasks.ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
