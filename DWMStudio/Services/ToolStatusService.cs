// ToolStatusService.cs
// Tracks live connection status for external tools (Fusion, MATLAB, Unreal, UModel).
// Bound to the status bar in MainWindow via MainViewModel.ToolStatus.

using CommunityToolkit.Mvvm.ComponentModel;

namespace DWMStudio.Services
{
    public sealed partial class ToolStatusService : ObservableObject
    {
        [ObservableProperty] private bool _fusionConnected;
        [ObservableProperty] private bool _matlabConnected;
        [ObservableProperty] private bool _unrealConnected;
        [ObservableProperty] private bool _umodelConnected;
    }
}
