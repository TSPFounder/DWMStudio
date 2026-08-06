// WorldTreeTests.cs
// Covers the world tree, which is a view over the filesystem and therefore testable without
// a single tool installed -- write files, build the tree, read it back.
//
// The tests that matter here are the ones about ABSENCE. A tree that lists what exists is
// easy and mostly self-evidencing; a tree that has to be honest about what does NOT exist is
// where this project has repeatedly lost days, so that is what is pinned.

using System;
using System.IO;
using System.Linq;
using DWM.Shared.Tooling;
using Xunit;

namespace DWMStudio.Tests
{
    public sealed class WorldTreeTests : IDisposable
    {
        private readonly string _root;

        public WorldTreeTests()
        {
            _root = Path.Combine(Path.GetTempPath(), "dwm_tree_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_root);
        }

        public void Dispose()
        {
            try { if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true); }
            catch (IOException) { }
        }

        private string Write(string name, string content = "x")
        {
            var p = Path.Combine(_root, name);
            File.WriteAllText(p, content);
            return p;
        }

        private WorldTreeNode Build(string? deck = null,
            Func<string, System.Collections.Generic.IReadOnlyList<ToolRun>>? runs = null,
            int max = WorldTreeBuilder.DefaultMaxResults) =>
            WorldTreeBuilder.Build(
                "WindTurbine",
                ProjectPipeline.WithStructuralAnalysis(deck ?? Path.Combine(_root, "wtTowerModal.dat")),
                new ToolRegistry(),
                _root,
                runs,
                max);

        [Fact]
        public void TheRoot_IsTheWorld_AndItsChildrenAreTheStages()
        {
            var tree = Build();

            Assert.Equal("WindTurbine", tree.Label);
            Assert.Equal(WorldTreeNodeKind.World, tree.Kind);
            Assert.All(tree.Children, c => Assert.Equal(WorldTreeNodeKind.Stage, c.Kind));

            // WithStructuralAnalysis is the pipeline in use, so FEMAP and MYSTRAN have to be
            // reachable from the tree -- they were the two tools the old enum could not express.
            Assert.Contains(tree.Children, c => c.ToolId == ToolRegistry.Femap);
            Assert.Contains(tree.Children, c => c.ToolId == ToolRegistry.Mystran);
        }

        [Fact]
        public void AMissingArtifact_IsLISTEDAsMissing_NotQuietlyOmitted()
        {
            // THE TEST THIS FILE EXISTS FOR. Dropping absent files would make a prettier tree
            // in which "the deck is not in the list" and "the deck is not on disk" look
            // identical. This project has paid for that confusion more than once: addpath
            // succeeding on a folder with no .m files, ResolveExecutable returning a filename
            // it had never found, a tile reporting "Found on disk" about nothing at all.
            var deck = Path.Combine(_root, "wtTowerModal.dat");   // deliberately NOT written

            var tree = Build(deck);
            var nodes = tree.Descendants()
                .Where(n => n.Kind == WorldTreeNodeKind.Artifact && n.Label == "wtTowerModal.dat")
                .ToList();

            // TWO of them, and that is correct: FEA Mesh and FEA Solve both name the same deck,
            // because one file is the handoff between a mesher that cannot solve and a solver
            // that cannot mesh. Both must report it missing -- a deck absent for MYSTRAN is not
            // somehow present for FEMAP.
            Assert.Equal(2, nodes.Count);
            Assert.All(nodes, n =>
            {
                Assert.True(n.IsMissing);
                Assert.False(n.Exists);
                Assert.Equal("Missing", n.Detail);
            });
        }

        [Fact]
        public void APresentArtifact_ReportsSizeAndTime_FromTheDiskNotFromThePath()
        {
            var deck = Write("wtTowerModal.dat", new string('x', 2048));

            var nodes = Build(deck).Descendants()
                .Where(n => n.Kind == WorldTreeNodeKind.Artifact)
                .ToList();

            Assert.All(nodes, n =>
            {
                Assert.True(n.Exists);
                Assert.False(n.IsMissing);
                Assert.Contains("KB", n.Detail);
            });
        }

        [Fact]
        public void SolverOutputs_AppearAsResults_FoundBesideTheDeckWhereMystranWritesThem()
        {
            var deck = Write("wtTowerModal.dat");
            Write("wtTowerModal.f06");
            Write("wtTowerModal.OP2");

            var results = Build(deck).Descendants()
                .Where(n => n.Kind == WorldTreeNodeKind.Result)
                .Select(n => n.Label)
                .ToList();

            Assert.Contains("wtTowerModal.f06", results);
            Assert.Contains("wtTowerModal.OP2", results);
        }

        [Fact]
        public void ResultsFromADifferentDeck_AreNotClaimedAsThisOnes()
        {
            // Stem matching, not "any .f06 in the folder". Two decks in one directory is
            // ordinary, and attributing one deck's results to another is the same shape as the
            // stale-outputs problem: everything looks right, including the files.
            var deck = Write("wtTowerModal.dat");
            Write("wtTowerModal.f06");
            Write("someOtherModel.f06");

            var results = Build(deck).Descendants()
                .Where(n => n.Kind == WorldTreeNodeKind.Result)
                .Select(n => n.Label)
                .ToList();

            Assert.Contains("wtTowerModal.f06", results);
            Assert.DoesNotContain("someOtherModel.f06", results);
        }

        [Fact]
        public void AFolderThatDoesNotExist_SaysSo_RatherThanShowingNothing()
        {
            // "No results" and "nowhere to look" are different facts, and a blank stage node
            // would render both the same way.
            var deck = Path.Combine(_root, "nope", "deeper", "wtTowerModal.dat");

            var notes = Build(deck).Descendants()
                .Where(n => n.Kind == WorldTreeNodeKind.Note)
                .Select(n => n.Label)
                .ToList();

            Assert.Contains("Folder not found", notes);
        }

        [Fact]
        public void TooManyResults_AreCappedAndTheOverflowIsReported()
        {
            var deck = Write("wtTowerModal.dat");
            for (int i = 0; i < 12; i++) Write($"wtTowerModal_{i:00}.f06");

            var tree = Build(deck, max: 5);
            var stage = tree.Children.Single(c => c.ToolId == ToolRegistry.Mystran);

            Assert.Equal(5, stage.Children.Count(c => c.Kind == WorldTreeNodeKind.Result));
            Assert.Contains(stage.Children, c =>
                c.Kind == WorldTreeNodeKind.Note && c.Label.Contains("more"));
        }

        [Fact]
        public void Runs_AreLabelledSessionScoped_BecauseNothingPersistsThem()
        {
            // Run history is an ObservableCollection on a ViewModel and is saved nowhere, so
            // this node is empty after every restart. Unlabelled, an empty Runs node reads as
            // "nothing has ever been run here" -- a different statement, and a false one.
            var deck = Write("wtTowerModal.dat");
            var run = ToolRun.Complete("fea-solve", ToolRegistry.Mystran,
                DateTime.UtcNow.AddSeconds(-3), Array.Empty<string>(),
                warnings: new[] { "L-SET mass matrix warning" });

            var tree = Build(deck, id => id == "fea-solve" ? new[] { run } : Array.Empty<ToolRun>());
            var runsNode = tree.Descendants().Single(n => n.Label == "Runs");

            Assert.Contains("not saved", runsNode.Detail);

            // Warnings are children, not a swallowed count. The run-history template collected
            // warnings for two builds into a control that never rendered them.
            var warning = runsNode.Descendants()
                .Single(n => n.Label.Contains("L-SET"));
            Assert.Equal("warning", warning.Detail);
        }

        [Fact]
        public void TheDeck_IsNotListedAsAResultOfItself()
        {
            // FEMAP's ResultExtensions include .dat and .bdf, because FEMAP writes decks. So
            // the FEA Mesh stage would show wtTowerModal.dat twice -- once as the artifact it
            // authors, once as a result it produced -- and a duplicated row that means nothing
            // teaches the reader that duplicated rows mean nothing.
            var deck = Write("wtTowerModal.dat");

            var femap = Build(deck).Children.Single(c => c.ToolId == ToolRegistry.Femap);

            Assert.Single(femap.Children, c => c.Label == "wtTowerModal.dat");
            Assert.DoesNotContain(femap.Children,
                c => c.Kind == WorldTreeNodeKind.Result && c.Label == "wtTowerModal.dat");
        }

        [Fact]
        public void AStageWithNoTool_StillAppears_AndSaysWhyItHasNothingToOffer()
        {
            // Co-Sim has no tool. A silently absent row would look like a bug in the tree.
            var stage = Build().Children.Single(c => c.StageId == "cosim");

            Assert.Null(stage.ToolId);
            Assert.Equal("No tool for this stage", stage.Detail);
        }

        [Fact]
        public void TheStageSummary_CountsMissingFiles_SoAProblemIsVisibleWhileCollapsed()
        {
            // A tree whose bad news only appears once expanded is a tree that hides bad news.
            var deck = Path.Combine(_root, "wtTowerModal.dat");   // not written

            var stage = Build(deck).Children.Single(c => c.ToolId == ToolRegistry.Mystran);

            Assert.Contains("missing", stage.Detail);
        }
    }
}
