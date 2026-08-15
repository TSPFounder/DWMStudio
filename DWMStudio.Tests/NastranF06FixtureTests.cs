// NastranF06FixtureTests.cs
// The parser against REAL MYSTRAN OUTPUT, not a synthetic sample.
//
// Fixtures/wtTowerModal.f06 is the genuine article: MYSTRAN 19.0.0, run 2026-08-03 on
// wtTowerModal.dat, SOL 103, six modes extracted by Lanczos. Until this file existed the
// parser was written entirely from the Nastran-standard layout and had never seen output
// from this solver.
//
// It also carries the engineering result. The Simulink model ASSUMES f_tower = 0.320 Hz;
// this run MEASURES 0.2811 Hz. The assumption is 13.8% high, and the tests below pin the
// measured value so a parser change cannot quietly move an answer the model depends on.

using System;
using System.IO;
using System.Linq;
using DWM.Shared.Tooling.Fea;
using Xunit;

namespace DWMStudio.Tests
{
    public sealed class NastranF06FixtureTests
    {
        private static ModalResults Parsed() =>
            NastranF06Parser.Parse(File.ReadAllText(
                Path.Combine("Fixtures", "wtTowerModal.f06")));

        [Fact]
        public void RealMystranOutput_YieldsExactlySixModes()
        {
            // SIX, not seventy-two. An eigenvector GRID row is structurally identical to a
            // mode row -- two leading integers then numeric columns -- so a parser that never
            // closes the eigenvalue table reads every grid line of every mode shape as a mode.
            // Measured against this exact file, that mistake produces 72.
            Assert.Equal(6, Parsed().Modes.Count);
        }

        [Fact]
        public void FirstTowerMode_Is0Point2811Hz()
        {
            var results = Parsed();

            Assert.Equal(0.2810991, results.FirstFrequencyHz!.Value, 7);
            Assert.Equal(1.766198, results.Modes[0].RadiansPerSecond, 6);
            Assert.Equal(3.119454, results.Modes[0].Eigenvalue, 6);
        }

        [Fact]
        public void ModesComeInDegeneratePairs_AsAnAxisymmetricTowerShould()
        {
            // Fore-aft and side-side are the same beam. Pairs at 0.2811, 2.2212 and 6.6339 Hz
            // are a strong sign the model is behaving; if this ever fails, suspect the
            // boundary conditions before suspecting the parser.
            var hz = Parsed().Modes.Select(m => Math.Round(m.Hertz, 6)).ToArray();

            Assert.Equal(hz[0], hz[1]);
            Assert.Equal(hz[2], hz[3]);
            Assert.Equal(hz[4], hz[5]);
        }

        [Fact]
        public void CleanRun_HasNoFatalsAndNoWarnings()
        {
            var results = Parsed();

            Assert.False(results.HasFatal);
            Assert.Empty(results.WarningMessages);
        }

        [Fact]
        public void SolverSuppliedMaterialProperty_IsSurfacedAsInformation()
        {
            // "MAT1 ENTRY 1 HAD FIELD FOR G BLANK. MYSTRAN CALCULATED G = 8.076923E+10".
            // The deck omitted the shear modulus and the solver derived it. Not a warning,
            // not an error, and worth knowing -- a derived property is still an assumption.
            Assert.Contains(Parsed().InfoMessages, m => m.Contains("MAT1") && m.Contains("BLANK"));
        }

        [Fact]
        public void TowerPassesTheSoftStiffWindow_ButWithMuchLessMarginThanTheModelImplies()
        {
            // Window 0.257-0.630 Hz (1P +10%, 3P -10%).
            //   model's assumed 0.320 Hz -> 24.5% above the lower bound
            //   measured      0.2811 Hz ->  9.4% above the lower bound
            // Same verdict, very different situation. This is exactly why CheckWindow returns
            // the margin instead of a boolean.
            var (inWindow, lowerMargin, _) = Parsed().CheckWindow(0.257, 0.630)!.Value;

            Assert.True(inWindow);
            Assert.InRange(lowerMargin, 9.0, 10.0);
        }

        [Fact]
        public void MeasuredFrequency_AgreesWithTheIndependentBeamPrediction()
        {
            // Before MYSTRAN was available the deck header recorded an expected 0.2815 Hz,
            // computed by an independent numpy beam eigen-solve, precisely so a disagreement
            // would read as "the deck was misread" rather than "the tower is surprising".
            // The two agree to 0.14%, which retires that doubt: the 13.8% gap to the Simulink
            // model's 0.320 Hz is a real modelling difference, not a deck error.
            var measured = Parsed().FirstFrequencyHz!.Value;

            Assert.InRange(Math.Abs(measured - 0.2815) / 0.2815 * 100.0, 0.0, 0.5);
        }
    }
}
