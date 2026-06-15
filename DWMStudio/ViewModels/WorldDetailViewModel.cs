// WorldDetailViewModel.cs
// Backs the World Detail view — the per-world pipeline orchestration screen.
// Shows each of the five pipeline stages with status and a primary action,
// and launches the relevant external tool (Fusion, MATLAB, UModel) per stage.

using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DWMStudio.Models;

namespace DWMStudio.ViewModels
{
    public sealed partial class WorldDetailViewModel : ObservableObject
    {
        public WorldProject World { get; }

        [ObservableProperty] private string _statusMessage = string.Empty;
        [ObservableProperty] private bool   _isBusy;

        public WorldDetailViewModel(WorldProject world)
        {
            World = world;
        }

        // ------------------------------------------------------------------
        // Stage accessors for binding
        // ------------------------------------------------------------------

        public PipelineStageStatus SysmlStage   => World.GetStage(PipelineStage.SysML);
        public PipelineStageStatus CadStage     => World.GetStage(PipelineStage.Cad);
        public PipelineStageStatus MatlabStage  => World.GetStage(PipelineStage.Matlab);
        public PipelineStageStatus CoSimStage   => World.GetStage(PipelineStage.CoSim);
        public PipelineStageStatus RuntimeStage => World.GetStage(PipelineStage.Runtime);

        // ------------------------------------------------------------------
        // Stage actions
        // ------------------------------------------------------------------

        [RelayCommand]
        private void OpenInUModel()
        {
            // TODO: UModelService.OpenProjectForWorld(World.WorldId)
            StatusMessage = "Opening UModel…";
        }

        [RelayCommand]
        private async Task OpenInFusionAsync()
        {
            IsBusy = true;
            StatusMessage = "Connecting to Fusion…";
            try
            {
                await using var app = new Fusion.Application.FusionApplication();
                if (await app.PingAsync())
                {
                    StatusMessage = $"Fusion connected ({app.Version}).";
                    // TODO: open World.FusionDocumentPath via app.OpenDocumentAsync
                }
                else
                {
                    StatusMessage = "Fusion add-in not reachable. Is Fusion running?";
                }
            }
            finally { IsBusy = false; }
        }

        [RelayCommand]
        private void BuildSimulinkModel()
        {
            // TODO: MatlabService.BuildSimulinkModel(World)
            StatusMessage = "Building Simulink model…";
        }

        [RelayCommand]
        private void OpenInUnreal()
        {
            // TODO: launch UE with the world package
            StatusMessage = "Launching Unreal Engine…";
        }
    }
}
