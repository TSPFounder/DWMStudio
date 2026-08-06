// ArtifactTemplateTests.cs
// Covers what Create writes.
//
// The rule under test is that a template must SURVIVE CONTACT WITH Run -- so these check
// that the deck is a complete model rather than a header, and that formats nobody here can
// write honestly are absent rather than faked.

using System;
using System.Linq;
using DWM.Shared.Tooling;
using Xunit;

namespace DWMStudio.Tests
{
    public sealed class ArtifactTemplateTests
    {
        [Fact]
        public void TheNastranTemplate_IsACompleteModel_NotAnEmptySkeleton()
        {
            // THE TEST THIS FILE EXISTS FOR. A deck with Case Control and no GRID cards would
            // parse and then FATAL -- Run breaking on a file Create had just reported making,
            // which is the exact failure the templates decision was taken to avoid.
            var deck = ArtifactTemplates.For(".dat")!.Render("wtTowerModal");

            Assert.Contains("SOL 103", deck);
            Assert.Contains("BEGIN BULK", deck);
            Assert.Contains("ENDDATA", deck);

            // Geometry, connectivity, property, material, constraint and an extraction
            // method. Remove any one and it stops solving.
            Assert.Contains("GRID,1,", deck);
            Assert.Contains("CBAR,1,", deck);
            Assert.Contains("PBAR,1,", deck);
            Assert.Contains("MAT1,1,", deck);
            Assert.Contains("SPC1,1,", deck);
            Assert.Contains("EIGRL,100,", deck);

            // Enough grids for six modes to mean something. One element has too few degrees
            // of freedom to return the modes the Case Control asks for.
            Assert.Equal(6, deck.Split('\n').Count(l => l.StartsWith("GRID,", StringComparison.Ordinal)));
        }

        [Fact]
        public void TheDeck_NamesItself_SoAFolderOfSixIsNavigable()
        {
            var deck = ArtifactTemplates.For(".dat")!.Render("wtTowerModal");

            Assert.Contains("TITLE = wtTowerModal NORMAL MODES", deck);
        }

        [Fact]
        public void TheSetIdsAgree_BetweenCaseControlAndBulkData()
        {
            // A deck whose METHOD names a set the bulk data does not define fatals in a way
            // that reads as a solver problem rather than a template problem.
            var deck = ArtifactTemplates.For(".dat")!.Render("beam");

            Assert.Contains("METHOD = 100", deck);
            Assert.Contains("EIGRL,100,", deck);
            Assert.Contains("SPC = 1", deck);
            Assert.Contains("SPC1,1,", deck);
        }

        [Fact]
        public void BinaryFormats_HaveNoTemplate_AndThatIsRecordedAsADecision()
        {
            // A .slx is a zip archive; a .f3d and a .ump are proprietary binaries. Writing an
            // invalid one and calling it a template would produce a file that exists, opens as
            // corrupt, and sends someone hunting for the fault in their tool.
            Assert.Null(ArtifactTemplates.For(".slx"));
            Assert.Null(ArtifactTemplates.For(".f3d"));
            Assert.Null(ArtifactTemplates.For(".ump"));

            // Absent from All, but PRESENT in ToolsWithoutTemplates, so Create can say which
            // application does own the format instead of reporting a bare "no template" that
            // reads like an oversight.
            Assert.Contains(".slx", ArtifactTemplates.ToolsWithoutTemplates.Keys);
            Assert.Contains(".f3d", ArtifactTemplates.ToolsWithoutTemplates.Keys);
            Assert.Contains(".ump", ArtifactTemplates.ToolsWithoutTemplates.Keys);
        }

        [Fact]
        public void NoExtensionIsBothTemplatedAndDeclaredUntemplatable()
        {
            // Two lists that could disagree. If one ever claims a format the other denies,
            // Create's message and Create's behaviour part company.
            var both = ArtifactTemplates.All.Keys
                .Intersect(ArtifactTemplates.ToolsWithoutTemplates.Keys, StringComparer.OrdinalIgnoreCase)
                .ToList();

            Assert.Empty(both);
        }

        [Fact]
        public void ATemplateIsFoundByFullPath_NotOnlyByBareExtension()
        {
            // Create passes the artifact path straight through.
            Assert.NotNull(ArtifactTemplates.For(@"C:\work\fea\wtTowerModal.dat"));
            Assert.NotNull(ArtifactTemplates.For("/home/x/model.BDF"));   // case-insensitive
        }

        [Fact]
        public void GarbageIn_ReturnsNullRatherThanThrowing()
        {
            // Create calls this before it has validated anything.
            Assert.Null(ArtifactTemplates.For(null));
            Assert.Null(ArtifactTemplates.For(""));
            Assert.Null(ArtifactTemplates.For("   "));
            Assert.Null(ArtifactTemplates.For("no-extension-here"));
        }
    }
}
