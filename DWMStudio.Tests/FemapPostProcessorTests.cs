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
        public void StartNewModel_ClearsFemapBeforeImporting_SoARepeatLoadIsIdempotent()
        {
            // Re-importing into a populated FEMAP does not replace, it collides. The real
            // second run gave "Overwriting existing Property 101..110" and twelve output sets
            // where six belong. Someone re-pressing the button after an apparently-empty first
            // attempt is the ordinary case, not a mistake, so it has to be safe.
            var (deck, op2) = WriteRun();
            var session = new FakeFemapSession();

            new FemapPostProcessor(() => session, startNewModel: true).Load(deck, op2);

            var cleared = session.Calls.FindIndex(c => c.Method == "feFileNew");
            var model = session.Calls.FindIndex(c => c.Method == "feFileReadNastran");

            Assert.True(cleared >= 0, "FEMAP was never cleared");
            Assert.True(model > cleared, "the model must be read AFTER the clear, not before");
        }

        [Fact]
        public void ClearingIsOff_ByDefault_BecauseItWouldDiscardWhateverWasOpen()
        {
            // Asymmetric damage: a duplicated results tree is one File > New away from fixed;
            // somebody's unsaved meshing is not. The library declines to clear, and the call
            // site that knows its own semantics opts in.
            var (deck, op2) = WriteRun();
            var session = new FakeFemapSession();

            new FemapPostProcessor(() => session).Load(deck, op2);

            Assert.DoesNotContain(session.Calls, c => c.Method == "feFileNew");
        }

        [Fact]
        public void ANonSuccessReturnCode_IsAFailure_NotAnAcceptedCall()
        {
            // THE BUG THIS CLOSES. FEMAP signals failure with a return code, not an exception,
            // so before -1 was known to mean success a refused call was indistinguishable from
            // an accepted one. Three runs reported success while leaving Out: 0 in FEMAP.
            var (deck, op2) = WriteRun();
            var session = new FakeFemapSession { ReturnValue = 0 };   // 0 = FEMAP said no

            var result = new FemapPostProcessor(() => session).Load(deck, op2);

            Assert.False(result.Succeeded);
            Assert.Contains("returned 0, not -1", result.Run.FailureMessage);
        }

        [Fact]
        public void TheVerifiedShapes_AreTheOnesShipped()
        {
            // Pinned from a run confirmed by FEMAP's own Out: 6, not by the call not throwing.
            var api = new FemapApiNames();

            Assert.Equal("feFileReadNastran", Assert.Single(api.ReadNastranModel).Method);
            Assert.Equal("feFileReadNastranResults", Assert.Single(api.ReadNastranResults).Method);
            Assert.Equal(-1, FemapApiNames.Success);
        }

        [Fact]
        public void TheShapeThatWorked_IsReportedAsResolvedVia_NotAsAWarning()
        {
            // IT WAS A WARNING, AND THAT WAS WRONG. Firing on every successful load meant
            // every run came back SucceededWithWarnings, so a channel meant for exceptions
            // triggered unconditionally and stopped carrying information -- the run-history
            // bug inverted. There, warnings existed and could not be read; here they could be
            // read and meant nothing. ResolvedVia answers "how did this actually get done",
            // which is exactly what an accepted call shape is.
            var (deck, op2) = WriteRun();

            var result = new FemapPostProcessor(() => new FakeFemapSession()).Load(deck, op2);

            Assert.Contains("feFileReadNastran(setId, filename)", result.Run.ResolvedVia);
            Assert.Contains("feFileReadNastranResults(setId, filename)", result.Run.ResolvedVia);
            Assert.DoesNotContain(result.Run.Warnings, w => w.Contains("FEMAP accepted"));
        }

        [Fact]
        public void ACleanLoad_WarnsAboutNothing_SoTheChannelStillMeansSomething()
        {
            // The test the move makes possible. Attached to a running FEMAP, not clearing the
            // model, every call accepted: there is nothing to warn about, and the status
            // should be able to say so plainly rather than being permanently qualified.
            var (deck, op2) = WriteRun();

            var result = new FemapPostProcessor(() => new FakeFemapSession()).Load(deck, op2);

            Assert.Empty(result.Run.Warnings);
            Assert.Equal(ToolRunStatus.Succeeded, result.Run.Status);
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

            /// <summary>What every call returns. -1 is FEMAP's success value.</summary>
            public int ReturnValue { get; set; } = -1;

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
                return ReturnValue;
            }

            public void Detach() => Detached = true;
            public void Dispose() { }
        }
    }
}
