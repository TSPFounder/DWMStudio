// FemapPostProcessorTests.cs
// Covers the FEMAP hand-off without FEMAP.
//
// The COM transport is faked, which leaves exactly what is worth testing: the ORDER of the
// two imports, that FEMAP is left open, and that a missing file is reported before anything
// is opened. The API method names themselves cannot be validated here -- they are unverified
// against FEMAP 10.2 and only a real FEMAP can settle them, which is why they are data.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DWM.Shared.Tooling;
using DWM.Shared.Tooling.Fea;
using Xunit;

namespace DWMStudio.Tests
{
    public sealed class FemapPostProcessorTests : IDisposable
    {
        private readonly string _dir;

        public FemapPostProcessorTests()
        {
            _dir = Path.Combine(Path.GetTempPath(), "dwm_femap_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
        }

        public void Dispose()
        {
            try { if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true); }
            catch (IOException) { }
        }

        [Fact]
        public void ModelIsReadBeforeResults_BecauseResultsHaveNowhereToLandOtherwise()
        {
            // THE TEST THIS FILE EXISTS FOR. MYSTRAN's .op2 for this deck holds six OUGV1
            // eigenvector blocks and no GEOM datablocks -- results without geometry. Reading
            // it into an empty model is what produced FEMAP's "Your model does not currently
            // contain Nodes and Elements" on 2026-08-04.
            var (deck, op2) = WriteRun();
            var session = new FakeFemapSession();

            new FemapPostProcessor(() => session).Load(deck, op2);

            var model = session.Calls.FindIndex(c => c.Method == "feFileReadNastran");
            var results = session.Calls.FindIndex(c => c.Method == "feFileReadNastranResults");

            Assert.True(model >= 0, "the model was never read");
            Assert.True(results > model, "results must be read AFTER the model, not before");
        }

        [Fact]
        public void FemapIsDetached_SoItStaysOpenWithTheResultsOnScreen()
        {
            // The MATLAB hand-off bug, one tool along: a COM server launched by a client dies
            // with the client's last reference. Closing FEMAP the instant the results loaded
            // would defeat the entire point.
            var (deck, op2) = WriteRun();
            var session = new FakeFemapSession();

            new FemapPostProcessor(() => session).Load(deck, op2);

            Assert.True(session.Detached);
        }

        [Fact]
        public void MissingResults_AreReportedWithoutOpeningFemap()
        {
            var (deck, _) = WriteRun(writeOp2: false);
            var session = new FakeFemapSession();

            var result = new FemapPostProcessor(() => session).Load(deck);

            Assert.False(result.Succeeded);
            Assert.Contains("Solve the deck first", result.Run.FailureMessage);
            Assert.Empty(session.Calls);
        }

        [Fact]
        public void MissingDeck_IsReportedWithoutOpeningFemap()
        {
            var session = new FakeFemapSession();

            var result = new FemapPostProcessor(() => session)
                .Load(Path.Combine(_dir, "nope.dat"));

            Assert.False(result.Succeeded);
            Assert.Contains("Deck not found", result.Run.FailureMessage);
            Assert.Empty(session.Calls);
        }

        [Fact]
        public void ResultsPath_DefaultsToTheDecksOwnStem()
        {
            // MYSTRAN writes beside its input, which is also why the solve runs in the deck's
            // folder. Deriving the path here keeps the two halves agreeing by construction.
            var (deck, op2) = WriteRun();

            var result = new FemapPostProcessor(() => new FakeFemapSession()).Load(deck);

            Assert.Equal(op2, result.ResultsPath);
        }

        [Fact]
        public void ALaunchedFemap_IsFlaggedAsAWarningNotHiddenAsASuccess()
        {
            var (deck, op2) = WriteRun();
            var session = new FakeFemapSession { IsAttachedToExistingInstance = false };

            var result = new FemapPostProcessor(() => session).Load(deck, op2);

            Assert.True(result.Succeeded);
            Assert.False(result.AttachedToExistingFemap);
            Assert.Contains(result.Run.Warnings, w => w.Contains("launched by this command"));
        }

        [Fact]
        public void NoExpectedOutputs_BecauseThisPutsAModelOnScreenRatherThanAFileOnDisk()
        {
            // Naming a file here would make the freshness check fail every time, and mean
            // nothing on the occasions it passed.
            var (deck, op2) = WriteRun();

            var result = new FemapPostProcessor(() => new FakeFemapSession()).Load(deck, op2);

            Assert.Equal(ToolRunStatus.Succeeded, result.Run.Status);
            Assert.Empty(result.Run.Outputs);
        }

        [Fact]
        public void ARefusedCallShape_FallsThroughToTheNextOne()
        {
            // THE REAL FAILURE, from 2026-08-05: feFileReadNastran(filename) came back
            // DISP_E_TYPEMISMATCH. That told us the ProgID was right and the METHOD EXISTS --
            // a wrong name raises MissingMethodException -- and only the argument shape was
            // wrong. Guessing one harder would have been the same mistake again, so the shapes
            // are tried in order instead.
            var (deck, op2) = WriteRun();
            var session = new FakeFemapSession
            {
                RefuseArgCount = 2   // the two-argument shapes are refused; one-arg wins
            };

            var result = new FemapPostProcessor(() => session).Load(deck, op2);

            Assert.True(result.Succeeded);
            Assert.Contains(session.Calls, c => c.Args.Length == 1);
        }

        [Fact]
        public void TheShapeThatWorked_IsReported_SoItCanBecomeTheDefaultWithEvidence()
        {
            var (deck, op2) = WriteRun();

            var result = new FemapPostProcessor(() => new FakeFemapSession()).Load(deck, op2);

            Assert.Contains(result.Run.Warnings, w => w.Contains("FEMAP accepted:"));
        }

        [Fact]
        public void EveryShapeRefused_FailsAndListsWhatWasTried()
        {
            // Honest dead end: name each attempt and where the real answer lives, rather than
            // "FEMAP call failed" which would send someone through the whole API reference.
            var (deck, op2) = WriteRun();
            var session = new FakeFemapSession { RefuseEverything = true };

            var result = new FemapPostProcessor(() => session).Load(deck, op2);

            Assert.False(result.Succeeded);
            Assert.Contains("Every candidate call shape was refused", result.Run.FailureMessage);
            Assert.Contains("API reference", result.Run.FailureMessage);
        }

        [Fact]
        public void CallShapesAreOverridable_SoACorrectionNeedsNoRebuild()
        {
            var (deck, op2) = WriteRun();
            var session = new FakeFemapSession();

            new FemapPostProcessor(() => session, new FemapApiNames
            {
                ReadNastranModel = new[]
                {
                    new FemapCallShape
                    {
                        Method = "feSomethingElseEntirely",
                        Args = path => new object[] { path },
                        Signature = "feSomethingElseEntirely(filename)"
                    }
                }
            }).Load(deck, op2);

            Assert.Contains(session.Calls, c => c.Method == "feSomethingElseEntirely");
        }

        // ------------------------------------------------------------------
        private (string deck, string op2) WriteRun(bool writeOp2 = true)
        {
            var deck = Path.Combine(_dir, "wtTowerModal.dat");
            var op2 = Path.Combine(_dir, "wtTowerModal.OP2");

            File.WriteAllText(deck, "SOL 103\nCEND\nBEGIN BULK\nENDDATA\n");
            if (writeOp2) File.WriteAllText(op2, "binary-ish");

            return (deck, op2);
        }

        private sealed class FakeFemapSession : IFemapSession
        {
            public List<(string Method, object[] Args)> Calls { get; } = new();
            public bool IsAttachedToExistingInstance { get; set; } = true;
            public bool Detached { get; private set; }
            public string? ThrowOn { get; set; }
            public string? ThrowWith { get; set; }

            /// <summary>Refuse any call with this many arguments, as a COM type mismatch would.</summary>
            public int? RefuseArgCount { get; set; }

            public bool RefuseEverything { get; set; }

            public object? Invoke(string method, params object[] args)
            {
                if (method == ThrowOn) throw new FemapSessionException(ThrowWith ?? "boom");

                // feAppVisible is cosmetic and always allowed, so refusing "everything" still
                // exercises the import paths rather than dying before them.
                if (method != "feAppVisible")
                {
                    if (RefuseEverything)
                        throw new FemapSessionException("Type mismatch. (0x80020005 (DISP_E_TYPEMISMATCH))");

                    if (RefuseArgCount is int n && args.Length == n)
                        throw new FemapSessionException("Type mismatch. (0x80020005 (DISP_E_TYPEMISMATCH))");
                }

                Calls.Add((method, args));
                return 0;
            }

            public void Detach() => Detached = true;
            public void Dispose() { }
        }
    }
}
