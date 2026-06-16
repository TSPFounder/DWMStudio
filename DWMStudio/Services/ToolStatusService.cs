// ToolStatusService.cs
// Tracks live connection status for external DWM tools (Fusion, MATLAB,
// UModel, Unreal). Polls every few seconds on a background timer.
// Self-contained — no dependency on FusionLibrary; the Fusion check is a
// plain HTTP ping to the add-in's /ping endpoint.

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DWMStudio.Services
{
    public sealed partial class ToolStatusService : ObservableObject, IDisposable
    {
        private static readonly HttpClient _http = new()
        {
            Timeout = TimeSpan.FromSeconds(2)
        };

        private readonly Timer _timer;

        [ObservableProperty] private bool _fusionConnected;
        [ObservableProperty] private bool _matlabConnected;
        [ObservableProperty] private bool _umodelConnected;
        [ObservableProperty] private bool _unrealConnected;   // reserved — no API yet

        public ToolStatusService()
        {
            // Poll immediately, then every 5 seconds
            _timer = new Timer(_ => _ = PollAsync(), null,
                TimeSpan.Zero, TimeSpan.FromSeconds(5));
        }

        private async Task PollAsync()
        {
            FusionConnected = await PingFusionAsync();
            MatlabConnected = ComServerRegistered("matlab.application");
            UmodelConnected = ComServerRegistered("UModel.Application");
            UnrealConnected = false;   // future: UE WebSocket/HTTP endpoint
        }

        private static async Task<bool> PingFusionAsync()
        {
            try
            {
                using var resp = await _http.GetAsync("http://127.0.0.1:18750/ping");
                return resp.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        private static bool ComServerRegistered(string progId)
        {
            try
            {
                return Type.GetTypeFromProgID(progId) is not null;
            }
            catch
            {
                return false;
            }
        }

        public void Dispose() => _timer.Dispose();
    }
}
