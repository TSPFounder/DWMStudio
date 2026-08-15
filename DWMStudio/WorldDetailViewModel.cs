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

        /// <summary>
        /// The world's contents as a tree: stages, the artifacts they author, the results
        /// their tools produced, and this session's runs.
        ///
        /// A COLLECTION HOLDING ONE ROOT, because TreeView binds ItemsSource to a sequence.
        /// Binding it to a bare node would render that node's CHILDREN as the top level and
        /// lose the world's own row -- the one that names the thing being looked at.
        /// </summary>
        public ObservableCollection<WorldTreeNode> Tree { get; } = new();

        private readonly ToolRegistry _registry = new();

        public WorldDetailViewModel(WorldProject world)
        {
            World = world;
            BuildTiles();
            RefreshTree();
        }

        /// <summary>
        /// Rebuild the tree from disk.
        ///
        /// EXPLICIT RATHER THAN LIVE, and deliberately so. Nothing here sits behind a
        /// database: the world's identity is JSON under %APPDATA%, its artifacts are ordinary
        /// files wherever the engineering work happens, and its run history is in memory. So
        /// nothing can notify this view that a solver has just written an .op2 -- the tree is
        /// a snapshot, and a control that looked live while silently not being live is worse
        /// than one that plainly asks to be refreshed.
        ///
        /// That is the lesson FEMAP's Model Info tree taught on 2026-08-05 from the other
        /// side: six output sets loaded correctly and the tree went on showing the old state,
        /// because nothing had told it. Reload from Model was the telling. This is that button.
        /// </summary>
        [RelayCommand]
        public void RefreshTree()
        {
            Tree.Clear();

            var deckPath = string.IsNullOrWhiteSpace(World.FeaDeckPath)
                ? DefaultFeaDeckPath
                : World.FeaDeckPath;

            var projectRoot = string.IsNullOrWhiteSpace(World.SimulinkModelPath)
                ? DefaultMatlabCodePath
                : World.SimulinkModelPath;

            Tree.Add(WorldTreeBuilder.Build(
                World.Name,
                ProjectPipeline.WithStructuralAnalysis(deckPath),
                _registry,
                projectRoot));
        }

        /// <summary>
        /// The tower modal deck, used when a world does not name its own.
        ///
        /// A machine-specific absolute path, and deliberately HERE rather than in DWM.Shared:
        /// the pipeline library must not know where one developer keeps their files. A world
        /// that sets FeaDeckPath overrides it, and that value is persisted, so this constant
        /// is only ever the starting point for a project that has not said otherwise.
        /// </summary>
        private const string DefaultFeaDeckPath =
            @"C:\DreamWorldMaker\Repos\DWM_Dev\Models\Mystran\wtTowerModal.dat";

        /// <summary>
        /// The turbine MATLAB sources, used when a world does not name its own.
        ///
        /// Without this the project root fell back to Environment.CurrentDirectory, which for
        /// a running WPF app is its bin folder. That fails in the WORST possible way: ADDPATH
        /// ON A FOLDER WITH NO .m FILES SUCCEEDS, so MATLAB opens, the path command reports no
        /// error, and only `wtGui` fails -- with "Undefined function", pointing at the function
        /// rather than at the path. Exactly the trail chased on 2026-08-03 through the CLI.
        /// </summary>
        private const string DefaultMatlabCodePath =
            @"C:\DreamWorldMaker\Repos\DWM_Dev\Models\Simulink\MVP_WindTurbine";

        private void BuildTiles()
        {
            // WithStructuralAnalysis rather than Default, so FEMAP and MYSTRAN appear now that
            // both are installed. Once pipelines are stored per project this comes from the
            // project instead of being chosen here.
            var deckPath = string.IsNullOrWhiteSpace(World.FeaDeckPath)
                ? DefaultFeaDeckPath
                : World.FeaDeckPath;

            var pipeline = ProjectPipeline.WithStructuralAnalysis(deckPath);

            var projectRoot = string.IsNullOrWhiteSpace(World.SimulinkModelPath)
                ? DefaultMatlabCodePath
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
