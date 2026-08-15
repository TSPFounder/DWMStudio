// ToolingRegistryTests.cs
// Covers the tool registry, the data-driven pipeline, and the generalised freshness check.
//
// None of this needs a tool installed, which is the point: the registry is DATA, the pipeline
// is a list, and the freshness check is arithmetic on timestamps. The parts that genuinely
// need Windows and a licence -- probing COM, spawning MYSTRAN -- are deliberately not here
// and are not in this change.

using System;
using System.IO;
using System.Linq;
using DWM.Shared.Tooling;
using Xunit;

namespace DWMStudio.Tests
{
    public sealed class ToolingRegistryTests : IDisposable
    {
        private readonly string _dir;

        public ToolingRegistryTests()
        {
            _dir = Path.Combine(Path.GetTempPath(), "dwm_tooling_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
        }

        public void Dispose()
        {
            try { if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true); }
            catch (IOException) { }
        }

        // ==================================================================
        // Registry
        // ==================================================================

        [Fact]
        public void EveryBuiltInTool_HasAnIdAndDisplayName()
        {
            foreach (var tool in new ToolRegistry().All)
            {
                Assert.False(string.IsNullOrWhiteSpace(tool.Id), "a tool has no id");
                Assert.False(string.IsNullOrWhiteSpace(tool.DisplayName), $"{tool.Id} has no display name");
            }
        }

        [Fact]
        public void MatlabProgIds_PutTheVersionedOnesFirst()
        {
            // Order is load-bearing, not cosmetic. The generic ProgID resolves to ONE CLSID --
            // whichever release registered last -- so an attach using it misses an open R2011a
            // and launches R2025b instead. If the generic entry ever drifts to the front, that
            // regression is silent until someone notices the wrong MATLAB on screen.
            var progIds = new ToolRegistry().Require(ToolRegistry.Matlab).ProgIds;

            Assert.Equal("Matlab.Application.7.12", progIds[0]);
            Assert.Equal("Matlab.Application", progIds[progIds.Count - 1]);
        }

        [Fact]
        public void MatlabToolboxes_MatchTheVerOutput_BecauseARememberedInventoryIsNotAnInventory()
        {
            // THE TEST THIS PAIR EXISTS FOR. On 2026-08-05 this project's recollected toolbox
            // list named six products where `ver` reports eleven, and on the strength of that
            // omission a confident claim was made that Simulink Control Design and Simulink
            // Design Optimization were NOT licensed and would block OOSEM phase H. Both are
            // licensed. The two named here are pinned first, because they are the two the
            // wrong answer was about.
            var matlab = new ToolRegistry().Require(ToolRegistry.Matlab);

            Assert.True(matlab.HasComponent("Simulink Control Design"));
            Assert.True(matlab.HasComponent("Simulink Design Optimization"));

            // Aerospace Blockset and Aerospace Toolbox are DISTINCT PRODUCTS. Collapsing them
            // into one "Aerospace" entry is precisely how the earlier inventory lost count.
            Assert.True(matlab.HasComponent("Aerospace Blockset"));
            Assert.True(matlab.HasComponent("Aerospace Toolbox"));

            Assert.Equal(11, matlab.Components.Count);

            // Every component version-stamped. A version-less toolbox name is what produced the
            // wrong claim, and pinning tool versions per project is a standing principle.
            Assert.All(matlab.Components, c =>
            {
                Assert.False(string.IsNullOrWhiteSpace(c.Name));
                Assert.False(string.IsNullOrWhiteSpace(c.Version));
            });

            Assert.Equal("1.0.18",
                matlab.Components.Single(c => c.Name.StartsWith("Partial Differential")).Version);
        }

        [Fact]
        public void PdeToolboxIs2DOnly_AndThatLimitReachesSomewhereItCanBeRead()
        {
            // A limit nobody can read is not a limit. The run-history template collected
            // warnings for two builds into a control that never rendered them, so a component
            // limitation that stopped at the descriptor would be the same bug -- which is why
            // AllLimitations folds into the one property the UI already shows.
            var matlab = new ToolRegistry().Require(ToolRegistry.Matlab);
            var pde = matlab.Components.Single(c => c.Name.StartsWith("Partial Differential"));

            Assert.Contains("2-D ONLY", pde.KnownLimitation);

            // Reaches AllLimitations WITHOUT displacing the tool's own limitation -- the ProgID
            // warning and a toolbox's capability are different facts and both have to survive.
            var all = matlab.AllLimitations;
            Assert.Contains("2-D ONLY", all);
            Assert.Contains("generic ProgID", all);

            // And reaches the workspace model, which is what actually renders.
            var model = ToolWorkspaceFactory.Build(
                ProjectPipeline.Default(), new ToolRegistry(), _dir)
                .Single(m => m.ToolId == ToolRegistry.Matlab);

            Assert.Contains("2-D ONLY", model.KnownLimitation);
        }

        [Fact]
        public void Mystran_IsABatchTool_BecauseItHasNoApiOfAnyKind()
        {
            var mystran = new ToolRegistry().Require(ToolRegistry.Mystran);

            Assert.Equal(ToolKind.BatchExecutable, mystran.Kind);
            Assert.Empty(mystran.ProgIds);
            Assert.Null(mystran.HttpPingUrl);
            Assert.NotEmpty(mystran.ExecutableCandidates);
        }

        [Fact]
        public void FemapAndMystran_FormAPair_OneWritesTheDeckTheOtherReadsIt()
        {
            // FEMAP does not solve and MYSTRAN does not mesh. The handoff is the .bdf, and if
            // these two ever stop agreeing on that extension the pairing quietly breaks.
            var registry = new ToolRegistry();
            var femap = registry.Require(ToolRegistry.Femap);
            var mystran = registry.Require(ToolRegistry.Mystran);

            Assert.Contains(".bdf", femap.ResultExtensions);
            Assert.Contains(".bdf", mystran.ArtifactExtensions);
        }

        [Fact]
        public void ToolsWithoutAnApi_SayWhatTheyCannotDo()
        {
            // Every wrong assumption on this project so far has been about a tool's LIMITS,
            // not its features. These three have bitten already or are known traps.
            var registry = new ToolRegistry();

            foreach (var id in new[] { ToolRegistry.Mystran, ToolRegistry.Datcom, ToolRegistry.Femap })
                Assert.False(string.IsNullOrWhiteSpace(registry.Require(id).KnownLimitation),
                    $"{id} should record what it cannot do");
        }

        [Fact]
        public void UnknownToolId_ThrowsAndListsWhatIsAvailable()
        {
            var ex = Assert.Throws<System.Collections.Generic.KeyNotFoundException>(
                () => new ToolRegistry().Require("solidworks"));

            Assert.Contains("mystran", ex.Message);   // the message names the real ids
        }

        [Fact]
        public void WithOverride_ReplacesADescriptorWithoutMutatingTheOriginal()
        {
            // How a wrong default install path gets fixed from settings rather than a rebuild.
            var registry = new ToolRegistry();
            var patched = registry.WithOverride(new ToolDescriptor
            {
                Id = ToolRegistry.Mystran,
                DisplayName = "MYSTRAN",
                Kind = ToolKind.BatchExecutable,
                ExecutableCandidates = new[] { @"D:\Solvers\mystran.exe" }
            });

            Assert.Equal(@"D:\Solvers\mystran.exe",
                patched.Require(ToolRegistry.Mystran).ExecutableCandidates[0]);
            Assert.NotEqual(@"D:\Solvers\mystran.exe",
                registry.Require(ToolRegistry.Mystran).ExecutableCandidates[0]);
        }

        [Fact]
        public void ToolsForExtension_FindsTheAuthoringTool()
        {
            var registry = new ToolRegistry();

            Assert.Contains(registry.ToolsForExtension(".slx"), t => t.Id == ToolRegistry.Matlab);
            Assert.Contains(registry.ToolsForExtension("f3d"), t => t.Id == ToolRegistry.Fusion);
            Assert.Empty(registry.ToolsForExtension(".sldprt"));
        }

        // ==================================================================
        // Pipeline
        // ==================================================================

        [Fact]
        public void DefaultPipeline_ReproducesTheFiveStagesTheEnumHad()
        {
            // A migration nobody notices is the only kind worth attempting on a UI that cannot
            // be built on the test agent.
            var labels = ProjectPipeline.Default().Stages.Select(s => s.Label).ToArray();

            Assert.Equal(new[] { "SysML", "CAD", "MATLAB", "Co-Sim", "Runtime" }, labels);
        }

        [Fact]
        public void StructuralPipeline_AddsTheTwoStagesTheEnumCouldNotExpress()
        {
            var pipeline = ProjectPipeline.WithStructuralAnalysis();
            var ids = pipeline.Stages.Select(s => s.Id).ToArray();

            Assert.Equal(new[] { "sysml", "cad", "matlab", "fea-mesh", "fea-solve", "cosim", "runtime" }, ids);
            Assert.Equal(ToolRegistry.Femap, pipeline.Find("fea-mesh")!.ToolId);
            Assert.Equal(ToolRegistry.Mystran, pipeline.Find("fea-solve")!.ToolId);
        }

        [Fact]
        public void DuplicateStageId_IsRejected()
        {
            // Ids address stages in saved projects and run history; duplicates make both
            // ambiguous, and the ambiguity would only surface when reloading an old project.
            var pipeline = ProjectPipeline.Default();

            Assert.Throws<ArgumentException>(() =>
                pipeline.Add(new PipelineStageDefinition { Id = "cad", Label = "CAD again" }));
        }

        [Fact]
        public void StagesCanBeRemovedAndReordered_BecauseOrderIsDataNow()
        {
            var pipeline = ProjectPipeline.Default();

            Assert.True(pipeline.Remove("cosim"));
            Assert.Null(pipeline.Find("cosim"));

            pipeline.MoveTo("runtime", 0);
            Assert.Equal("runtime", pipeline.Stages[0].Id);
        }

        [Fact]
        public void MoveTo_RejectsAnIndexOutsideTheList()
        {
            var pipeline = ProjectPipeline.Default();
            Assert.Throws<ArgumentOutOfRangeException>(() => pipeline.MoveTo("cad", 99));
        }

        [Fact]
        public void UnknownToolStages_AreReported_SoAProjectFileCanOutliveAToolId()
        {
            var pipeline = ProjectPipeline.Default();
            pipeline.Add(new PipelineStageDefinition { Id = "cfd", Label = "CFD", ToolId = "openfoam" });

            Assert.Equal(new[] { "cfd" }, pipeline.UnknownToolStages(new ToolRegistry()));
        }

        // ==================================================================
        // Freshness -- the check this whole namespace inherits from the MATLAB stage
        // ==================================================================

        [Fact]
        public void OutputsWrittenDuringTheRun_AreAccepted()
        {
            var started = DateTime.UtcNow;
            var path = WriteFile("model.f06");

            Assert.Empty(ToolRun.FindStaleOrMissing(new[] { path }, started));
        }

        [Fact]
        public void OutputsFromAnEarlierRun_AreTreatedAsMissing()
        {
            // The MYSTRAN version of the stale-CSV hazard: yesterday's .f06 parses exactly like
            // today's, has eigenvalues in it, and says nothing about the solver not having run.
            var path = WriteFile("model.f06");
            File.SetLastWriteTimeUtc(path, DateTime.UtcNow - TimeSpan.FromHours(20));

            var stale = ToolRun.FindStaleOrMissing(new[] { path }, DateTime.UtcNow);

            Assert.Single(stale);
        }

        [Fact]
        public void Complete_DerivesStaleOutputs_EvenWhenTheToolReportedSuccess()
        {
            // The dangerous case, and why the status is derived rather than accepted: the tool
            // said it worked, and the file on disk agrees with it, and both are wrong.
            var path = WriteFile("model.f06");
            File.SetLastWriteTimeUtc(path, DateTime.UtcNow - TimeSpan.FromHours(20));

            var run = ToolRun.Complete("fea-solve", ToolRegistry.Mystran,
                DateTime.UtcNow, new[] { path });

            Assert.Equal(ToolRunStatus.StaleOutputs, run.Status);
            Assert.False(run.ProducedUsableOutput);
            Assert.Contains(run.Warnings, w => w.Contains("predate"));
        }

        [Fact]
        public void Complete_KeepsWarningsVisible_RatherThanCollapsingIntoSucceeded()
        {
            var path = WriteFile("model.f06");

            var run = ToolRun.Complete("fea-solve", ToolRegistry.Mystran,
                DateTime.UtcNow, new[] { path }, warnings: new[] { "1 unsupported card ignored" });

            Assert.Equal(ToolRunStatus.SucceededWithWarnings, run.Status);
            Assert.True(run.ProducedUsableOutput);   // usable, but not silently "fine"
            Assert.True(run.HasWarnings);
        }

        [Fact]
        public void Complete_FailureBeatsEverythingElse()
        {
            var run = ToolRun.Complete("fea-solve", ToolRegistry.Mystran,
                DateTime.UtcNow, Array.Empty<string>(), failureMessage: "FATAL: card SPC1 malformed");

            Assert.Equal(ToolRunStatus.Failed, run.Status);
            Assert.False(run.ProducedUsableOutput);
        }

        private string WriteFile(string name)
        {
            var path = Path.Combine(_dir, name);
            File.WriteAllText(path, "placeholder");
            return path;
        }
    }
}
