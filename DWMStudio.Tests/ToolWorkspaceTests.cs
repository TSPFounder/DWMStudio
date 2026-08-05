// ToolWorkspaceTests.cs
// What each tool tile offers, and -- more usefully -- why it does not.
//
// The disabled cases carry most of the weight here. A greyed-out button with no explanation
// is the same defect as the Create World button that silently did nothing: the UI knows why
// and does not say.

using System;
using System.IO;
using System.Linq;
using DWM.Shared.Tooling;
using Xunit;

namespace DWMStudio.Tests
{
    public sealed class ToolWorkspaceTests : IDisposable
    {
        private readonly string _root;

        public ToolWorkspaceTests()
        {
            _root = Path.Combine(Path.GetTempPath(), "dwm_ws_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_root);
        }

        public void Dispose()
        {
            try { if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true); }
            catch (IOException) { }
        }

        private static ToolAvailability Installed(string _) => ToolAvailability.Found;

        [Fact]
        public void StructuralPipeline_ProducesAFemapTileAndAMystranTile()
        {
            // The point of making tiles data: adding FEA is a pipeline entry, not a fifth
            // hand-written Border plus a fifth command plus a fifth stage accessor.
            var tiles = ToolWorkspaceFactory.Build(
                ProjectPipeline.WithStructuralAnalysis(), new ToolRegistry(), _root, Installed);

            Assert.Contains(tiles, t => t.ToolId == ToolRegistry.Femap);
            Assert.Contains(tiles, t => t.ToolId == ToolRegistry.Mystran);
            Assert.Equal("FEA Solve / MYSTRAN", tiles.Single(t => t.ToolId == ToolRegistry.Mystran).Title);
        }

        [Fact]
        public void StagesWithNoTool_GetNoTile()
        {
            // Co-Sim names no tool. A tile offering nothing to do is worse than no tile.
            var tiles = ToolWorkspaceFactory.Build(
                ProjectPipeline.Default(), new ToolRegistry(), _root, Installed);

            Assert.DoesNotContain(tiles, t => t.StageId == "cosim");
            Assert.Equal(4, tiles.Count);
        }

        [Fact]
        public void EveryTile_HasASubtitle_BecauseABlankOneLooksLikeABug()
        {
            var tiles = ToolWorkspaceFactory.Build(
                ProjectPipeline.WithStructuralAnalysis(), new ToolRegistry(), _root, Installed);

            Assert.All(tiles, t => Assert.False(string.IsNullOrWhiteSpace(t.Subtitle)));
        }

        [Fact]
        public void UnknownToolId_GetsANeutralAccent_RatherThanThrowing()
        {
            // A missing colour must never be a missing-resource exception at runtime. Tiles
            // are data; an unrecognised one should render plainly and still work.
            var registry = new ToolRegistry().WithOverride(new ToolDescriptor
            {
                Id = "openfoam", DisplayName = "OpenFOAM", Kind = ToolKind.BatchExecutable
            });
            var pipeline = new ProjectPipeline();
            pipeline.Add(new PipelineStageDefinition { Id = "cfd", Label = "CFD", ToolId = "openfoam" });

            var tile = ToolWorkspaceFactory.Build(pipeline, registry, _root, Installed).Single();

            Assert.Equal("#8B98A9", tile.AccentColor);
        }

        // ==================================================================
        // What is offered, and why not
        // ==================================================================

        [Fact]
        public void MissingArtifact_OffersCreateButNotEdit()
        {
            var tile = FeaSolveTile(availability: ToolAvailability.Found, writeDeck: false);

            Assert.True(tile.CanCreate);
            Assert.False(tile.CanEdit);
            Assert.Contains("Create it first", tile.WhyNot(ToolAction.Edit));
        }

        [Fact]
        public void ExistingArtifact_OffersEditButNotCreate()
        {
            var tile = FeaSolveTile(availability: ToolAvailability.Found, writeDeck: true);

            Assert.True(tile.CanEdit);
            Assert.False(tile.CanCreate);
            Assert.Contains("already exists", tile.WhyNot(ToolAction.Create));
        }

        [Fact]
        public void BatchToolMerelyFoundOnDisk_CanStillBeRun()
        {
            // "Found" is the CEILING for MYSTRAN -- there is nothing to be connected to. If
            // Run required Running, MYSTRAN would be permanently unrunnable.
            var tile = FeaSolveTile(availability: ToolAvailability.Found, writeDeck: true);

            Assert.Equal(ToolKind.BatchExecutable, tile.Kind);
            Assert.True(tile.CanRun);
        }

        [Fact]
        public void ToolNotInstalled_ExplainsThatThePathsAreOnlyDefaults()
        {
            var tile = FeaSolveTile(availability: ToolAvailability.NotFound, writeDeck: true);

            Assert.False(tile.CanRun);
            Assert.False(tile.CanEdit);
            Assert.Contains("defaults, not facts", tile.WhyNot(ToolAction.Run));
        }

        [Fact]
        public void KnownLimitation_TravelsToTheTile()
        {
            // MYSTRAN's is that it has no API at all and its status can never beat "found".
            // Surfacing it in the workspace is the point of recording it in the registry.
            var tile = FeaSolveTile(availability: ToolAvailability.Found, writeDeck: true);

            Assert.Contains("No API of any kind", tile.KnownLimitation);
        }

        [Fact]
        public void LastRun_IsTheMostRecentOne()
        {
            var started = DateTime.UtcNow.AddMinutes(-5);
            var tiles = ToolWorkspaceFactory.Build(
                ProjectPipeline.WithStructuralAnalysis(), new ToolRegistry(), _root, Installed,
                runsForStage: stage => stage != "fea-solve"
                    ? Array.Empty<ToolRun>()
                    : new[]
                    {
                        ToolRun.Complete("fea-solve", ToolRegistry.Mystran, started,
                            Array.Empty<string>(), failureMessage: "first attempt"),
                        ToolRun.Complete("fea-solve", ToolRegistry.Mystran, DateTime.UtcNow,
                            Array.Empty<string>())
                    });

            var tile = tiles.Single(t => t.StageId == "fea-solve");

            Assert.Equal(2, tile.Runs.Count);
            Assert.Equal(ToolRunStatus.Succeeded, tile.LastRun!.Status);
        }

        // ------------------------------------------------------------------
        private ToolWorkspaceModel FeaSolveTile(ToolAvailability availability, bool writeDeck)
        {
            var feaDir = Path.Combine(_root, "fea");
            Directory.CreateDirectory(feaDir);
            if (writeDeck) File.WriteAllText(Path.Combine(feaDir, "model.bdf"), "SOL 103\n");

            return ToolWorkspaceFactory.Build(
                    ProjectPipeline.WithStructuralAnalysis(), new ToolRegistry(), _root,
                    _ => availability)
                .Single(t => t.StageId == "fea-solve");
        }
    }
}
