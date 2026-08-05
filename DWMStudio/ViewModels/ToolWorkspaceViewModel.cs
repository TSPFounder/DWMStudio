// ToolWorkspaceViewModel.cs
// Backs the one workspace window, for whichever tool it was opened on.
//
// ONE WINDOW, NOT SIX. The verbs are the same everywhere -- Create, Edit, Run, plus the run
// history -- so six windows would be six XAML files kept in sync by hand, and adding FEMAP
// would have made it seven. The tool-specific part is data (ToolWorkspaceModel) and the
// window renders whatever it is handed.

using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DWM.Shared.Matlab;
using DWM.Shared.Tooling;
using DWM.Shared.Tooling.Fea;

namespace DWMStudio.ViewModels
{
    public sealed partial class ToolWorkspaceViewModel : ObservableObject
    {
        public ToolWorkspaceModel Model { get; }

        [ObservableProperty] private string _statusMessage = string.Empty;
        [ObservableProperty] private bool _isBusy;

        public ObservableCollection<ToolRun> Runs { get; } = new();

        public string Title => Model.Title;
        public string RunLabel => Model.RunLabel;
        public string Subtitle => Model.Subtitle;
        public string AccentColor => Model.AccentColor;
        public string? KnownLimitation => Model.KnownLimitation;
        public bool HasLimitation => !string.IsNullOrWhiteSpace(Model.KnownLimitation);

        public string ArtifactLabel => Model.ArtifactPath ?? "No artifact configured for this stage";

        public string AvailabilityLabel => Model.Availability switch
        {
            ToolAvailability.NotFound  => "Not found on this machine",
            ToolAvailability.Found     => Model.Kind == ToolKind.BatchExecutable
                                          ? "Found on disk (a batch tool can report no more than this)"
                                          : "Installed",
            ToolAvailability.Running   => "Running",
            ToolAvailability.Connected => "Connected",
            _                          => "Not checked"
        };

        // Tooltips carry the reason a button is disabled. A greyed-out control that will not
        // say why is the same defect as a button that silently does nothing.
        public string? CreateTooltip => Model.WhyNot(ToolAction.Create);
        public string? EditTooltip => Model.WhyNot(ToolAction.Edit);
        public string? RunTooltip => Model.WhyNot(ToolAction.Run);

        public ToolWorkspaceViewModel(ToolWorkspaceModel model)
        {
            Model = model ?? throw new ArgumentNullException(nameof(model));
            foreach (var run in model.Runs) Runs.Add(run);
        }

        private bool CanCreate() => Model.CanCreate && !IsBusy;
        private bool CanEdit() => Model.CanEdit && !IsBusy;
        private bool CanRun() => Model.CanRun && !IsBusy;

        [RelayCommand(CanExecute = nameof(CanCreate))]
        private void Create()
        {
            // Scaffolding from templates is not built yet, and creating an empty file would
            // be worse than doing nothing: Edit would then open a blank document and Run
            // would fail somewhere further downstream with a less obvious message.
            StatusMessage =
                $"Templates for {Model.ToolId} are not built yet. Create the artifact in " +
                $"{Model.Title.Split('/')[^1].Trim()} and save it to:\n{Model.ArtifactPath}";
        }

        [RelayCommand(CanExecute = nameof(CanEdit))]
        private void Edit()
        {
            // EDIT MEANS LAUNCH THE OWNING APPLICATION. DWMStudio does not edit these formats
            // and should not try to -- the shell association is what knows which app owns a
            // .bdf, a .slx or an .f3d.
            try
            {
                Process.Start(new ProcessStartInfo(Model.ArtifactPath!) { UseShellExecute = true });
                StatusMessage = $"Opened {Path.GetFileName(Model.ArtifactPath)} in its associated application.";
            }
            catch (Exception ex)
            {
                StatusMessage =
                    $"Could not open {Model.ArtifactPath}: {ex.Message}\n" +
                    "If Windows has no association for this extension, open it from the tool itself.";
            }
        }

        [RelayCommand(CanExecute = nameof(CanRun))]
        private async Task RunAsync()
        {
            IsBusy = true;
            RunCommand.NotifyCanExecuteChanged();
            StatusMessage = $"Running {Model.Title}…";

            try
            {
                switch (Model.ToolId)
                {
                    case ToolRegistry.Mystran:
                        await RunMystranAsync();
                        break;

                    case ToolRegistry.Matlab:
                        await OpenMatlabGuiAsync();
                        break;

                    default:
                        // Say so plainly rather than showing a spinner over nothing. Only
                        // MATLAB and MYSTRAN have runners; the rest are TOOLING.md steps 3-5.
                        StatusMessage =
                            $"No runner is wired for {Model.ToolId} yet. MYSTRAN runs from here; " +
                            "MATLAB runs through the WorldPackageCli 'turbine' command.";
                        break;
                }
            }
            finally
            {
                IsBusy = false;
                RunCommand.NotifyCanExecuteChanged();
            }
        }

        /// <summary>
        /// Hand MATLAB/Simulink work to MATLAB.
        ///
        /// wtGui already does this job: scenario picker with ramp as default, the six result
        /// plots, the post-run pass/fail panel, and the channel CSV export. Rebuilding any of
        /// it in WPF would be a worse copy of a tool the project already owns and would have
        /// to be kept in step with the model by hand. So this stage opens MATLAB, puts the
        /// model folder on the path, and starts wtGui.
        ///
        /// The ProgID is PINNED to R2011a. The generic "Matlab.Application" resolves to
        /// whichever release registered last, and an attach using it MISSES an open R2011a and
        /// launches R2025b instead -- which the turbine model cannot run under. That cost four
        /// rounds of debugging on 2026-08-03 and is not a mistake worth making twice.
        /// </summary>
        private async Task OpenMatlabGuiAsync()
        {
            var codeDirectory = Model.ProjectRoot;
            if (string.IsNullOrWhiteSpace(codeDirectory) || !Directory.Exists(codeDirectory))
            {
                StatusMessage =
                    $"No MATLAB code folder for this world ({codeDirectory}). Set the world's " +
                    "Simulink model path to the folder holding wtRunSimulation.m and wtGui.m.";
                return;
            }

            var startedUtc = DateTime.UtcNow;

            try
            {
                await Task.Run(() =>
                {
                    using var session = new MatlabComSession(
                        allowLaunch: true, progId: MatlabProgId);

                    // Same two commands the CLI's turbine stage issues, and for the same
                    // reason: addpath rather than cd, so the user's current folder is theirs.
                    session.Execute(MatlabStageService.BuildGuardedCommand(
                        $"addpath({MatlabStageService.MatlabLiteral(codeDirectory)});"));

                    var pathError = session.GetCharArray(MatlabStageService.ErrorSentinel);
                    if (!string.IsNullOrWhiteSpace(pathError))
                        throw new MatlabStageException($"MATLAB could not add the path: {pathError}");

                    // wtGui returns as soon as its figure is up, so this does not block on the
                    // user's session -- MATLAB stays open and is theirs from here.
                    session.Execute(MatlabStageService.BuildGuardedCommand("wtGui"));

                    var guiError = session.GetCharArray(MatlabStageService.ErrorSentinel);
                    // THE SESSION IS THE USER'S NOW. Without this, Dispose would quit the
                    // MATLAB this just launched -- taking wtGui with it -- and the whole
                    // hand-off would look like nothing had happened.
                    session.Detach();

                    if (!string.IsNullOrWhiteSpace(guiError))
                        throw new MatlabStageException(
                            $"wtGui did not start.\n\n  MATLAB said: {guiError}\n\n" +
                            $"  Path added: {codeDirectory}\n\n" +
                            "ADDPATH SUCCEEDS ON A FOLDER WITH NO .m FILES IN IT, so a wrong "
                            + "folder surfaces here rather than one step earlier. In MATLAB, "
                            + "run `which wtGui` and set the world's Simulink model path to "
                            + "the folder it reports.");
                });

                StatusMessage =
                    $"wtGui is open in MATLAB ({MatlabProgId}). Run the scenario and export the " +
                    "channel CSVs there; the world package is built from those.";
            }
            catch (Exception ex)
            {
                StatusMessage = ex.Message;
            }
            finally
            {
                Runs.Add(ToolRun.Complete(
                    Model.StageId, ToolRegistry.Matlab, startedUtc,
                    expectedOutputs: Array.Empty<string>(),
                    resolvedVia: MatlabProgId));
            }
        }

        /// <summary>
        /// R2011a. Verified present on this machine 2026-08-03 via
        /// `reg query HKCR /f "matlab.application" /k`, alongside 25.2.
        /// </summary>
        private const string MatlabProgId = "Matlab.Application.7.12";

        private async Task RunMystranAsync()
        {
            if (Model.ArtifactPath is null || !File.Exists(Model.ArtifactPath))
            {
                StatusMessage = $"No deck to solve at {Model.ArtifactPath ?? "(no path configured)"}.";
                return;
            }

            var deck = Model.ArtifactPath;
            var stageId = Model.StageId;

            // Off the UI thread: a solve is seconds to minutes and the window must stay alive.
            var result = await Task.Run(() => new MystranRunner().Run(deck, stageId));

            Runs.Add(result.Run);

            if (result.Succeeded && result.Modal is not null)
            {
                var first = result.Modal.FirstFrequencyHz;
                StatusMessage =
                    $"Solved in {result.Run.Duration.TotalSeconds:F1} s. " +
                    $"{result.Modal.Modes.Count} modes" +
                    (first is null ? "." : $", first at {first:F4} Hz.");
            }
            else
            {
                StatusMessage = result.Run.FailureMessage ?? "MYSTRAN failed for an unrecorded reason.";
            }
        }
    }
}
