// MystranRunnerTests.cs
// Covers the batch tool shape without MYSTRAN installed.
//
// The process spawn is faked; everything around it is real -- executable resolution, working
// directory choice, .f06 parsing, freshness, and the decision about what counts as failure.
// That last one is the reason this file matters: MYSTRAN returns exit code 0 after writing
// FATAL, so "did it work" is a judgement made from the print file, not from the status the
// process handed back.
//
// THE .f06 SAMPLES BELOW ARE SYNTHETIC. They follow the Nastran-standard eigenvalue layout,
// which MYSTRAN follows closely, but no output from this MYSTRAN build has been seen yet.
// When a real one exists, it belongs here as a fixture and any disagreement belongs in the
// parser.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DWM.Shared.Tooling;
using DWM.Shared.Tooling.Fea;
using Xunit;

namespace DWMStudio.Tests
{
    public sealed class MystranRunnerTests : IDisposable
    {
        private readonly string _dir;

        public MystranRunnerTests()
        {
            _dir = Path.Combine(Path.GetTempPath(), "dwm_mystran_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
        }

        public void Dispose()
        {
            try { if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true); }
            catch (IOException) { }
        }

        // ==================================================================
        // The .f06 parser
        // ==================================================================

        private const string GoodF06 = @"
                                              R E A L   E I G E N V A L U E S
   MODE    EXTRACTION      EIGENVALUE            RADIANS             CYCLES            GENERALIZED         GENERALIZED
    NO.       ORDER                                                                       MASS              STIFFNESS
        1         1        3.128967E+00        1.768888E+00        2.815329E-01        1.000000E+00        3.128967E+00
        2         2        3.140112E+00        1.772035E+00        2.820338E-01        1.000000E+00        3.140112E+00
        3         3        1.284772E+02        1.133478E+01        1.804000E+00        1.000000E+00        1.284772E+02
";

        [Fact]
        public void Parser_ReadsTheEigenvalueTable()
        {
            var results = NastranF06Parser.Parse(GoodF06);

            Assert.Equal(3, results.Modes.Count);
            Assert.False(results.HasFatal);
            Assert.Equal(0.2815329, results.FirstFrequencyHz!.Value, 6);
            Assert.Equal(1.768888, results.Modes[0].RadiansPerSecond, 5);
        }

        [Fact]
        public void Parser_FindsTheHeaderDespiteItsLetterSpacing()
        {
            // The header is printed as "R E A L   E I G E N V A L U E S". Matching it
            // literally would break on any spacing difference between releases, so detection
            // strips whitespace first. This test is what stops someone "tidying" that.
            var spacedDifferently = GoodF06.Replace(
                "R E A L   E I G E N V A L U E S",
                "R E A L    E I G E N V A L U E S");

            Assert.Equal(3, NastranF06Parser.Parse(spacedDifferently).Modes.Count);
        }

        [Fact]
        public void Parser_AcceptsFortranExponentShorthand()
        {
            // "2.815-1" with no E is legal in this lineage and defeats double.Parse. The deck
            // WRITER had to emit this form, so the READER has to accept it.
            var shorthand = GoodF06.Replace("2.815329E-01", "2.815329-1");

            var results = NastranF06Parser.Parse(shorthand);

            Assert.Equal(0.2815329, results.FirstFrequencyHz!.Value, 6);
        }

        [Fact]
        public void Parser_CollectsFatalMessages()
        {
            var results = NastranF06Parser.Parse(
                "*** USER FATAL MESSAGE 3001: SPC1 card references undefined grid 9999\n");

            Assert.True(results.HasFatal);
            Assert.Empty(results.Modes);
            Assert.Contains("9999", results.FatalMessages[0]);
        }

        [Fact]
        public void Parser_DoesNotStopAtBlankLinesInsideTheTable()
        {
            // Print files pad tables with blank lines. Treating the first one as the end of
            // the table would silently truncate the mode list at mode 1 -- and a modal result
            // with one mode in it looks perfectly reasonable.
            var padded = GoodF06.Replace(
                "        2         2", "\n\n        2         2");

            Assert.Equal(3, NastranF06Parser.Parse(padded).Modes.Count);
        }

        [Fact]
        public void CheckWindow_ReportsMarginNotJustPassFail()
        {
            // The turbine's soft-stiff window is 0.257-0.630 Hz. The Simulink model says the
            // tower is at 0.320 Hz and the beam deck predicts 0.2815. BOTH PASS. The margins
            // are 24% and 9.5%, which is the same verdict describing two different situations
            // -- so the check returns the margin, not a boolean.
            var results = NastranF06Parser.Parse(GoodF06);

            var (inWindow, lower, upper) = results.CheckWindow(0.257, 0.630)!.Value;

            Assert.True(inWindow);
            Assert.InRange(lower, 9.0, 10.0);      // ~9.5% above the 1P bound
            Assert.InRange(upper, 55.0, 56.0);
        }

        // ==================================================================
        // The runner
        // ==================================================================

        [Fact]
        public void ExitCodeZeroWithAFatal_IsAFailure()
        {
            // THE TEST THIS FILE EXISTS FOR. MYSTRAN returns 0 after writing FATAL, so a
            // runner that trusts the exit code reports success on a deck that never solved.
            // Same shape as MATLAB's COM Execute returning error text as an ordinary string.
            var deck = WriteDeck();
            var runner = MakeRunner(exitCode: 0, f06: "*** USER FATAL MESSAGE 3001: bad SPC1\n");

            var result = runner.Run(deck);

            Assert.Equal(0, result.ExitCode);
            Assert.False(result.Succeeded);
            Assert.Equal(ToolRunStatus.Failed, result.Run.Status);
            Assert.Contains("does NOT indicate failure", result.Run.FailureMessage);
        }

        [Fact]
        public void GoodRun_ParsesModesAndReportsSuccess()
        {
            var deck = WriteDeck();
            var runner = MakeRunner(exitCode: 0, f06: GoodF06);

            var result = runner.Run(deck);

            Assert.True(result.Succeeded);
            Assert.Equal(3, result.Modal!.Modes.Count);
            Assert.Equal(0.2815329, result.Modal.FirstFrequencyHz!.Value, 6);
        }

        [Fact]
        public void NoF06AtAll_IsAFailureThatShowsTheConsoleOutput()
        {
            var deck = WriteDeck();
            var runner = MakeRunner(exitCode: 8, f06: null, stdout: "cannot open scratch file");

            var result = runner.Run(deck);

            Assert.False(result.Succeeded);
            Assert.Contains("wrote no print file", result.Run.FailureMessage);
            Assert.Contains("scratch file", result.Run.FailureMessage);
        }

        [Fact]
        public void F06WithNoModes_FailsAndSaysTheParserMightBeWrong()
        {
            // Honest failure: "no modes" can mean the deck asked for none, or that the table
            // layout differs from what the parser expects. Claiming the structure has no modes
            // would be the confident wrong answer.
            var deck = WriteDeck();
            var runner = MakeRunner(exitCode: 0, f06: "MYSTRAN finished normally.\n");

            var result = runner.Run(deck);

            Assert.False(result.Succeeded);
            Assert.Contains("no eigenvalue table", result.Run.FailureMessage);
            Assert.Contains("has not yet been checked", result.Run.FailureMessage);
        }

        [Fact]
        public void StaleF06_FromAnEarlierRun_IsCaught()
        {
            // The FEA version of the stale-CSV hazard: yesterday's .f06 parses, has
            // eigenvalues in it, and says nothing about the solver not having run today.
            var deck = WriteDeck();
            var f06 = Path.Combine(_dir, "tower.f06");
            File.WriteAllText(f06, GoodF06);

            var runner = new MystranRunner(FakeDescriptor(), new FakeProcessRunner
            {
                ExitCode = 0,
                BeforeReturning = () => File.SetLastWriteTimeUtc(f06, DateTime.UtcNow.AddHours(-20))
            });

            var result = runner.Run(deck);

            Assert.Equal(ToolRunStatus.StaleOutputs, result.Run.Status);
            Assert.False(result.Succeeded);
        }

        [Fact]
        public void MissingDeck_FailsBeforeSpawningAnything()
        {
            var runner = new MystranRunner(FakeDescriptor(), new FakeProcessRunner());

            var result = runner.Run(Path.Combine(_dir, "nope.bdf"));

            Assert.False(result.Succeeded);
            Assert.Contains("Deck not found", result.Run.FailureMessage);
        }

        [Fact]
        public void MissingExecutable_SaysWhereItLookedAndThatPathsAreDefaults()
        {
            var deck = WriteDeck();
            var runner = new MystranRunner(
                new ToolDescriptor
                {
                    Id = ToolRegistry.Mystran,
                    DisplayName = "MYSTRAN",
                    Kind = ToolKind.BatchExecutable,
                    ExecutableCandidates = new[] { @"Z:\definitely\not\here\mystran.exe" }
                },
                new FakeProcessRunner());

            var result = runner.Run(deck);

            Assert.False(result.Succeeded);
            Assert.Contains(@"Z:\definitely\not\here\mystran.exe", result.Run.FailureMessage);
            Assert.Contains("defaults, not facts", result.Run.FailureMessage);
        }

        [Fact]
        public void Timeout_IsReportedAsSuchRatherThanAsASolverError()
        {
            var deck = WriteDeck();
            var runner = new MystranRunner(FakeDescriptor(),
                new FakeProcessRunner { TimedOut = true });

            var result = runner.Run(deck, timeout: TimeSpan.FromMinutes(3));

            Assert.True(result.TimedOut);
            Assert.False(result.Succeeded);
            Assert.Contains("did not finish within 3", result.Run.FailureMessage);
        }

        [Fact]
        public void RunsInTheDecksOwnFolder_BecauseThatIsWhereResultsLand()
        {
            var deck = WriteDeck();
            var fake = new FakeProcessRunner { ExitCode = 0, F06Content = GoodF06, F06Dir = _dir };
            new MystranRunner(FakeDescriptor(), fake).Run(deck);

            Assert.Equal(_dir, fake.LastRequest!.WorkingDirectory);
            Assert.Contains("tower.bdf", fake.LastRequest.Arguments);
        }

        // ==================================================================
        // Helpers
        // ==================================================================

        private string WriteDeck()
        {
            var path = Path.Combine(_dir, "tower.bdf");
            File.WriteAllText(path, "SOL 3\nCEND\nBEGIN BULK\nENDDATA\n");
            return path;
        }

        private static ToolDescriptor FakeDescriptor() => new()
        {
            Id = ToolRegistry.Mystran,
            DisplayName = "MYSTRAN",
            Kind = ToolKind.BatchExecutable,
            ExecutableCandidates = new[] { "mystran.exe" }   // bare name: accepted as "on PATH"
        };

        private MystranRunner MakeRunner(int exitCode, string? f06, string stdout = "")
            => new(FakeDescriptor(), new FakeProcessRunner
            {
                ExitCode = exitCode,
                F06Content = f06,
                F06Dir = _dir,
                StandardOutput = stdout
            });

        /// <summary>
        /// Stands in for the solver: writes the .f06 it was told to write, then returns the
        /// exit code it was told to return. The two are independent ON PURPOSE -- that
        /// independence is the whole subject of these tests.
        /// </summary>
        private sealed class FakeProcessRunner : IProcessRunner
        {
            public int ExitCode { get; set; }
            public string? F06Content { get; set; }
            public string? F06Dir { get; set; }
            public string StandardOutput { get; set; } = string.Empty;
            public bool TimedOut { get; set; }
            public Action? BeforeReturning { get; set; }

            public ProcessRequest? LastRequest { get; private set; }

            public ProcessOutcome Run(ProcessRequest request)
            {
                LastRequest = request;

                if (F06Content is not null && F06Dir is not null)
                    File.WriteAllText(Path.Combine(F06Dir, "tower.f06"), F06Content);

                BeforeReturning?.Invoke();

                return new ProcessOutcome
                {
                    ExitCode = ExitCode,
                    StandardOutput = StandardOutput,
                    TimedOut = TimedOut,
                    Duration = TimeSpan.FromSeconds(1)
                };
            }
        }
    }
}
