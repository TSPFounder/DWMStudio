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
        public string? EditTooltip =>
            SelectedNode is { Exists: true, Path: not null } n
                ? $"Open {System.IO.Path.GetFileName(n.Path)}"
                : SelectedNode is { IsMissing: true } m
                    ? $"{System.IO.Path.GetFileName(m.Path)} is not on disk."
                    : Model.WhyNot(ToolAction.Edit);
        public string? RunTooltip => Model.WhyNot(ToolAction.Run);

        /// <summary>
        /// This stage's contents: the artifact it works on, the results beside it, and this
        /// session's runs. One root, because TreeView binds to a sequence.
        /// </summary>
        public ObservableCollection<WorldTreeNode> Tree { get; } = new();

        /// <summary>
        /// The tree row the verbs act on. Set from the view, because TreeView.SelectedItem is
        /// read-only in WPF and cannot be bound.
        ///
        /// Null is the ordinary state, not an error -- nothing selected means Edit falls back
        /// to the stage's configured artifact, which is what it did before the tree existed.
        /// </summary>
        [ObservableProperty] private WorldTreeNode? _selectedNode;

        /// <summary>
        /// The file Edit will open: the selected row when it names one, otherwise the stage's
        /// own artifact. Notes and Run rows name no file and therefore fall through.
        /// </summary>
        public string? EditTarget =>
            SelectedNode?.Path is { Length: > 0 } p ? p : Model.ArtifactPath;

        partial void OnSelectedNodeChanged(WorldTreeNode? value)
        {
            OnPropertyChanged(nameof(EditTarget));
            OnPropertyChanged(nameof(EditTooltip));
            EditCommand.NotifyCanExecuteChanged();
        }

        private readonly ToolRegistry _registry = new();

        public ToolWorkspaceViewModel(ToolWorkspaceModel model)
        {
            Model = model ?? throw new ArgumentNullException(nameof(model));
            foreach (var run in model.Runs) Runs.Add(run);
            RefreshTree();
        }

        /// <summary>
        /// Re-read this stage's files from disk.
        ///
        /// CALLED AFTER EVERY RUN, which is the point of having it here rather than only on
        /// the world view: a MYSTRAN solve writes .f06, .ERR and .op2 beside the deck, and the
        /// window that just ran it is exactly where someone looks for them. A tree that still
        /// showed the pre-run state would be the FEMAP Model Info problem rebuilt in our own
        /// UI -- the work succeeded, the view kept the old picture, and the gap between them
        /// read as failure.
        /// </summary>
        [RelayCommand]
        public void RefreshTree()
        {
            Tree.Clear();
            Tree.Add(WorldTreeBuilder.BuildForWorkspace(Model, _registry, Runs));
        }

        private bool CanCreate() => Model.CanCreate && !IsBusy;

        /// <summary>
        /// Enabled when a SELECTED FILE EXISTS, or when the stage's own artifact does.
        ///
        /// The selection widens this deliberately. Model.CanEdit is about the configured
        /// artifact, so without the first clause a solve's .f06 could be selected, sit there
        /// plainly present, and the button would stay greyed out -- a control refusing to do
        /// the thing the row in front of it obviously means.
        /// </summary>
        private bool CanEdit() => !IsBusy && (SelectedNode?.Exists == true || Model.CanEdit);

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
            //
            // OPENS WHATEVER IS SELECTED, falling back to the stage's artifact when nothing
            // is. That is the point of the tree being here: a MYSTRAN run leaves a .f06 and an
            // .ERR beside the deck, and reading those is the normal next step after a solve --
            // being able to open only the deck would make the tree a display case.
            var target = EditTarget;

            if (string.IsNullOrWhiteSpace(target))
            {
                StatusMessage = "Nothing to open. Select a file in Contents, or configure this stage's artifact.";
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo(target) { UseShellExecute = true });
                StatusMessage = $"Opened {Path.GetFileName(target)} in its associated application.";
            }
            catch (Exception ex)
            {
                StatusMessage =
                    $"Could not open {target}: {ex.Message}\n" +
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

                    case ToolRegistry.Femap:
                        await OpenInFemapAsync();
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

                // IN THE FINALLY, NOT AFTER A SUCCESSFUL RUN. A failed solve still writes
                // files -- MYSTRAN leaves a .f06 and an .ERR after a FATAL, and those are the
                // two anyone needs to read to find out what went wrong. Refreshing only on
                // success would hide the evidence exactly when it matters.
                RefreshTree();
            }
        }

        /// <summary>
        /// Load the solved deck and its results into FEMAP.
        ///
        /// Two imports, in order, because MYSTRAN's .op2 for this deck carries eigenvectors
        /// and NO geometry -- results with nowhere to land until the model exists. That is
        /// what produced FEMAP's "Your model does not currently contain Nodes and Elements"
        /// when the .op2 was tried on its own.
        ///
        /// FEMAP is left open, which is the whole point: the user ends up looking at mode
        /// shapes rather than at a status line saying some did once exist.
        /// </summary>
        private async Task OpenInFemapAsync()
        {
            if (Model.ArtifactPath is null)
            {
                StatusMessage = "This stage has no deck configured, so there is nothing to post-process.";
                return;
            }

            var deck = Model.ArtifactPath;

            // startNewModel: true, and this call site is the reason the library defaults it off.
            //
            // The library cannot know what is open in FEMAP, so it declines to clear. THIS
            // BUTTON CAN: it says "load results", it will be pressed repeatedly while someone
            // iterates on a deck, and a repeat load into a populated FEMAP does not replace --
            // it collides. The 2026-08-05 second run produced "Overwriting existing Property
            // 101..110" and twelve output sets where six belong.
            //
            // Clearing first makes the button IDEMPOTENT: press it any number of times and
            // FEMAP shows this run, once. That is what someone pressing it expects, and it is
            // what makes a re-press after an apparently-empty first attempt safe rather than
            // the thing that doubles everything.
            var result = await Task.Run(() =>
                new FemapPostProcessor(
                        () => new FemapComSession(allowLaunch: true),
                        startNewModel: true)
                    .Load(deck));

            Runs.Add(result.Run);

            if (result.Succeeded)
            {
                // The accepted call signatures go in the STATUS LINE as well as the run
                // history. They are the one fact that turns three candidate shapes into one
                // known-good one, and a fact that exists only somewhere nobody reads is not
                // much better than not having it -- the run history collected these warnings
                // for two builds before anyone noticed the template never rendered them.
                StatusMessage =
                    $"Loaded into FEMAP: model from {Path.GetFileName(result.DeckPath)}, results " +
                    $"from {Path.GetFileName(result.ResultsPath)}. Cleared to an empty model " +
                    "first, so this is one clean copy however many times you press it. Switch " +
                    "to FEMAP's PostProcessing tab for the deformed shapes.\n\n" +
                    // THE TREE LIES, AND SAYING SO IS PART OF THE RESULT. FEMAP's Model Info
                    // does not repaint for entities that arrive over the API, so a load that
                    // worked can leave the Results node looking empty -- which on 2026-08-05
                    // read exactly like a failed import and sent three runs chasing a bug that
                    // was not there. FemapApiNames.RefreshUi tries to fix it over the API, but
                    // those three call names are guesses and may all be wrong. This click is not.
                    "If the Results node looks empty, press Reload from Model (second button " +
                    "on the Model Info toolbar). FEMAP's tree does not repaint for entities " +
                    "that arrive over the API -- the results are loaded either way." +
                    (result.Run.Warnings.Count == 0
                        ? string.Empty
                        : "\n\n" + string.Join("\n", result.Run.Warnings));
            }
            else
            {
                StatusMessage = result.Run.FailureMessage ?? "FEMAP failed for an unrecorded reason.";
            }
        }

        /// <summary>
        /// Hand MATLAB/Simulink work to MATLAB.
        ///
        /// wtGui already does this job: scenario picker with ramp as default, the six result
        /// plots, the post-run pass/fail panel, and the channel CSV export. Rebuilding any of
        /// it in WPF would be a worse copy of a tool the project already owns.
        ///
        /// WHY THIS DOES NOT LAUNCH MATLAB THROUGH COM
        ///
        /// A MATLAB started as a COM automation server IS OWNED BY ITS CLIENT. When the last
        /// reference is released it shuts down -- no Quit() involved, that is simply how an
        /// out-of-process COM server's lifetime works. So the first version of this launched
        /// R2011a, opened wtGui, reported success, and then took MATLAB down with it the
        /// moment the session was disposed. Suppressing the explicit Quit did not help,
        /// because the Quit was never the mechanism.
        ///
        /// So there are two paths, and which one runs depends on what is already open:
        ///
        ///   ATTACH  A MATLAB the user started is theirs, not ours. Releasing our reference
        ///           cannot close it, so COM is safe here and lands wtGui in the session they
        ///           are already watching.
        ///   LAUNCH  Start matlab.exe as an ORDINARY PROCESS with -r. It belongs to the user
        ///           from the first instant, outlives DWMStudio, and is never ours to close.
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
            string? failure = null;

            try
            {
                var attached = await Task.Run(() => TryOpenInRunningMatlab(codeDirectory));

                if (attached)
                {
                    StatusMessage =
                        $"wtGui is open in the MATLAB you already had running ({MatlabProgId}). " +
                        "Run the scenario and export the channel CSVs there.";
                }
                else
                {
                    LaunchMatlabWithGui(codeDirectory);
                    StatusMessage =
                        "Starting MATLAB with wtGui. A cold R2011a start takes a little while, and " +
                        "this session is yours -- closing DWMStudio will not close it.";
                }
            }
            catch (Exception ex)
            {
                failure = ex.Message;
                StatusMessage = ex.Message;
            }
            finally
            {
                Runs.Add(ToolRun.Complete(
                    Model.StageId, ToolRegistry.Matlab, startedUtc,
                    expectedOutputs: Array.Empty<string>(),
                    failureMessage: failure,
                    resolvedVia: MatlabProgId));
            }
        }

        /// <summary>
        /// Run wtGui in an ALREADY-RUNNING MATLAB. Returns false when there is none, which is
        /// not a failure -- it is the signal to launch one instead.
        /// </summary>
        private static bool TryOpenInRunningMatlab(string codeDirectory)
        {
            MatlabComSession session;
            try
            {
                // allowLaunch: false is the whole point. Letting this launch would recreate
                // the bug: a COM-launched MATLAB dies with our reference.
                session = new MatlabComSession(allowLaunch: false, progId: MatlabProgId);
            }
            catch (MatlabStageException)
            {
                return false;
            }

            using (session)
            {
                session.Detach();   // belt and braces; we did not launch it, so it is not ours

                session.Execute(MatlabStageService.BuildGuardedCommand(
                    $"addpath({MatlabStageService.MatlabLiteral(codeDirectory)});"));

                var pathError = session.GetCharArray(MatlabStageService.ErrorSentinel);
                if (!string.IsNullOrWhiteSpace(pathError))
                    throw new MatlabStageException($"MATLAB could not add the path: {pathError}");

                session.Execute(MatlabStageService.BuildGuardedCommand("wtGui"));

                var guiError = session.GetCharArray(MatlabStageService.ErrorSentinel);
                if (!string.IsNullOrWhiteSpace(guiError))
                    throw new MatlabStageException(
                        $"wtGui did not start.\n\n  MATLAB said: {guiError}\n\n" +
                        $"  Path added: {codeDirectory}\n\n" +
                        "ADDPATH SUCCEEDS ON A FOLDER WITH NO .m FILES IN IT, so a wrong folder " +
                        "surfaces here rather than one step earlier. In MATLAB, run " +
                        "`which wtGui` and set the world's Simulink model path to the folder " +
                        "it reports.");
            }

            return true;
        }

        /// <summary>
        /// Start matlab.exe with -r, so the session is an ordinary user process rather than an
        /// automation server we own.
        /// </summary>
        private void LaunchMatlabWithGui(string codeDirectory)
        {
            var descriptor = new ToolRegistry().Require(ToolRegistry.Matlab);
            var executable = ProcessRunner.ResolveExecutable(descriptor);

            if (executable is null)
                throw new MatlabStageException(
                    "No MATLAB is running, and matlab.exe was not found to start one. Looked for:\n  " +
                    string.Join("\n  ", descriptor.ExecutableCandidates) +
                    "\n\nOpen MATLAB yourself and press this again -- it will attach to it.");

            // Single-quoted MATLAB strings need no backslash escaping, which is what makes a
            // Windows path safe to drop straight in; a quote inside one is doubled.
            var command = $"addpath({MatlabStageService.MatlabLiteral(codeDirectory)}); wtGui";

            Process.Start(new ProcessStartInfo
            {
                FileName = executable,
                Arguments = $"-r \"{command}\"",
                WorkingDirectory = codeDirectory,
                UseShellExecute = true
            });
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
