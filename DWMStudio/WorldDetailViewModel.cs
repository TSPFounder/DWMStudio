// WorldDetailViewModel.cs
// Backs the World Detail view — the per-world pipeline orchestration screen.
//
// THE TILES ARE NOW DATA. This class used to hand-write one command per tool
// (OpenInUModel, OpenInFusion, BuildSimulinkModel, OpenInUnreal) against four hand-written
// Borders in WorldDetailView.xaml. Adding FEMAP and MYSTRAN that way would have meant two
// more commands, two more Borders and two more stage accessors -- the same shape as
// `enum PipelineStage`, and with the same consequence: the cost of a new tool spread across
// four files and two languages, one of which cannot be compiled on the build agent.
//
// Tiles now come from the project's ProjectPipeline plus the ToolRegistry, and every tile
// opens the SAME workspace window. Adding a tool is a registry row.

using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DWM.Shared.Tooling;
using DWMStudio.Models;
using DWMStudio.Views;

namespace DWMStudio.ViewModels
{
    public sealed partial class WorldDetailViewModel : ObservableObject
    {
        public WorldProject World { get; }

        [ObservableProperty] private string _statusMessage = string.Empty;
        [ObservableProperty] private bool _isBusy;

        /// <summary>One tile per pipeline stage that names a tool.</summary>
        public ObservableCollection<ToolWorkspaceModel> Tiles { get; } = new();

        private readonly ToolRegistry _registry = new();

        public WorldDetailViewModel(WorldProject world)
        {
            World = world;
            BuildTiles();
        }

        private void BuildTiles()
        {
            // WithStructuralAnalysis rather than Default, so FEMAP and MYSTRAN appear now that
            // both are installed. Once pipelines are stored per project this comes from the
            // project instead of being chosen here.
            var pipeline = ProjectPipeline.WithStructuralAnalysis();

            var projectRoot = string.IsNullOrWhiteSpace(World.SimulinkModelPath)
                ? System.Environment.CurrentDirectory
                : World.SimulinkModelPath;

            foreach (var tile in ToolWorkspaceFactory.Build(
                         pipeline, _registry, projectRoot, ProbeAvailability))
            {
                Tiles.Add(tile);
            }
        }

        /// <summary>
        /// Cheap, honest availability. Nothing here launches or attaches: a COM tool reports
        /// Unknown rather than guessing, because "is a server registered" is not "is the
        /// release this project needs available" and pretending otherwise is how the wrong
        /// MATLAB got launched. Batch tools CAN be answered honestly -- an executable either
        /// exists on disk or it does not.
        /// </summary>
        private ToolAvailability ProbeAvailability(string toolId)
        {
            var descriptor = _registry.Find(toolId);
            if (descriptor is null) return ToolAvailability.Unknown;

            if (descriptor.Kind != ToolKind.BatchExecutable) return ToolAvailability.Unknown;

            return ProcessRunner.ResolveExecutable(descriptor) is null
                ? ToolAvailability.NotFound
                : ToolAvailability.Found;
        }

        [RelayCommand]
        private void OpenWorkspace(ToolWorkspaceModel? tile)
        {
            if (tile is null) return;

            var window = new ToolWorkspaceWindow(new ToolWorkspaceViewModel(tile))
            {
                Owner = Application.Current?.MainWindow
            };
            window.Show();
        }

        // ------------------------------------------------------------------
        // Stage accessors, still used by the progress strip at the top of the view.
        // These remain enum-based until WorldProject itself moves to ProjectPipeline --
        // see TOOLING.md, step 1's migration note.
        // ------------------------------------------------------------------

        public PipelineStageStatus SysmlStage => World.GetStage(PipelineStage.SysML);
        public PipelineStageStatus CadStage => World.GetStage(PipelineStage.Cad);
        public PipelineStageStatus MatlabStage => World.GetStage(PipelineStage.Matlab);
        public PipelineStageStatus CoSimStage => World.GetStage(PipelineStage.CoSim);
        public PipelineStageStatus RuntimeStage => World.GetStage(PipelineStage.Runtime);
    }
}
