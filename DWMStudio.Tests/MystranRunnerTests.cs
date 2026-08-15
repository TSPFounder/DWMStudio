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
        public void WarningsInTheErrLog_AreNotLost_EvenWhenTheF06IsClean()
        {
            // THE REAL CASE. The 2026-08-03 tower run produced a .f06 with no warnings at all
            // and an .ERR carrying "THE L-SET MASS MATRIX HAS ONLY 30 NONZEROS ON ITS
            // DIAGONAL" -- a real statement about what the model can tell you. Reporting
            // "0 warnings" while that sat unread would give a cleaner account of the solve
            // than the solver did.
            var deck = WriteDeck();
            var runner = new MystranRunner(FakeDescriptor(), new FakeProcessRunner
            {
                ExitCode = 0,
                F06Content = GoodF06,
                F06Dir = _dir,
                BeforeReturning = () => File.WriteAllText(
                    Path.Combine(_dir, "tower.ERR"),
                    " *WARNING    : THE L-SET MASS MATRIX HAS ONLY 30 NONZEROS ON ITS DIAGONAL\n")
            });

            var result = runner.Run(deck);

            Assert.True(result.Succeeded);
            Assert.Contains(result.Run.Warnings, w => w.Contains("L-SET MASS MATRIX"));
            Assert.Equal(ToolRunStatus.SucceededWithWarnings, result.Run.Status);
            Assert.NotNull(result.ErrPath);
        }

        [Fact]
        public void FatalInTheErrLog_FailsTheRun_EvenWhenTheF06LooksClean()
        {
            var deck = WriteDeck();
            var runner = new MystranRunner(FakeDescriptor(), new FakeProcessRunner
            {
                ExitCode = 0,
                F06Content = GoodF06,
                F06Dir = _dir,
                BeforeReturning = () => File.WriteAllText(
                    Path.Combine(_dir, "tower.ERR"),
                    " *FATAL: singular stiffness matrix at grid 7\n")
            });

            var result = runner.Run(deck);

            Assert.False(result.Succeeded);
            Assert.Contains("FATAL in the .ERR", result.Run.FailureMessage);
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
        public void BareFilename_IsNotTreatedAsFound_UnlessItIsActuallyOnPath()
        {
            // The bug this replaces: a bare name was returned unchecked on the theory it might
            // be on PATH, so "resolved" could mean "guessed". ToolAvailability then reported
            // Found, the tile said so, and the truth only arrived as a Win32Exception out of
            // Process.Start. A tool that claims Found and cannot start is worse than one that
            // admits NotFound.
            var descriptor = new ToolDescriptor
            {
                Id = ToolRegistry.Mystran,
                DisplayName = "MYSTRAN",
                Kind = ToolKind.BatchExecutable,
                ExecutableCandidates = new[] { "definitely-not-a-real-solver-xyz.exe" }
            };

            Assert.Null(ProcessRunner.ResolveExecutable(descriptor));
        }

        [Fact]
        public void SearchRoots_FindTheExecutableInASubfolder()
        {
            // Installers disagree about whether the binary sits at the root, under bin/, or in
            // a version-stamped folder. None of the exact MYSTRAN paths matched on the machine
            // that has it installed, which is what motivated searching rather than guessing
            // harder.
            var nested = Path.Combine(_dir, "bin", "v19");
            Directory.CreateDirectory(nested);
            var exe = Path.Combine(nested, "mystran.exe");
            File.WriteAllText(exe, "");

            var descriptor = new ToolDescriptor
            {
                Id = ToolRegistry.Mystran,
                DisplayName = "MYSTRAN",
                Kind = ToolKind.BatchExecutable,
                ExecutableCandidates = new[] { @"C:
ope\mystran.exe" },
                ExecutableSearchRoots = new[] { _dir }
            };

            Assert.Equal(exe, ProcessRunner.ResolveExecutable(descriptor));
        }

        [Fact]
        public void SearchPattern_MatchesAVersionStampedExecutable()
        {
            // THE REAL CASE. MYSTRAN 19 installs as mystran-19.0.0-windows-x86_64.exe, so a
            // search for the exact name "mystran.exe" found nothing on a machine where it was
            // plainly installed. Pinning the versioned name instead would only move the
            // breakage to the next release, which is why the search takes a pattern.
            var exe = Path.Combine(_dir, "mystran-19.0.0-windows-x86_64.exe");
            File.WriteAllText(exe, "");

            var descriptor = new ToolDescriptor
            {
                Id = ToolRegistry.Mystran,
                DisplayName = "MYSTRAN",
                Kind = ToolKind.BatchExecutable,
                ExecutableCandidates = new[] { @"C:\nope\mystran.exe" },
                ExecutableSearchRoots = new[] { _dir },
                ExecutableSearchPatterns = new[] { "mystran*.exe" }
            };

            Assert.Equal(exe, ProcessRunner.ResolveExecutable(descriptor));
        }

        [Fact]
        public void TwoVersionsSideBySide_ResolveDeterministically()
        {
            // EnumerateFiles guarantees no order, so an install with two builds in it would
            // otherwise resolve to whichever the filesystem handed back first -- a different
            // answer on different machines, and a different SOLVER, silently.
            File.WriteAllText(Path.Combine(_dir, "mystran-19.0.0-windows-x86_64.exe"), "");
            File.WriteAllText(Path.Combine(_dir, "mystran-20.1.0-windows-x86_64.exe"), "");

            var descriptor = new ToolDescriptor
            {
                Id = ToolRegistry.Mystran,
                DisplayName = "MYSTRAN",
                Kind = ToolKind.BatchExecutable,
                ExecutableSearchRoots = new[] { _dir },
                ExecutableSearchPatterns = new[] { "mystran*.exe" }
            };

            var first = ProcessRunner.ResolveExecutable(descriptor);

            Assert.Equal(first, ProcessRunner.ResolveExecutable(descriptor));
            Assert.EndsWith("mystran-19.0.0-windows-x86_64.exe", first);
        }

        [Fact]
        public void TheRegistrysMystranEntry_WouldFindTheRealInstalledBinary()
        {
            // Guards the shipped defaults, not just the mechanism: the pattern has to match the
            // filename actually observed on disk on 2026-08-03.
            var pattern = new ToolRegistry().Require(ToolRegistry.Mystran).ExecutableSearchPatterns;

            Assert.Contains("mystran*.exe", pattern);
        }

        [Fact]
        public void AFailedSpawn_IsReportedAsSuch_NotAsAnEmptySolve()
        {
            // "Exited but wrote no print file" would send someone looking at their deck for a
            // problem that is in the toolchain.
            var deck = WriteDeck();
            var runner = new MystranRunner(FakeDescriptor(), new FakeProcessRunner
            {
                ExitCode = -1,
                SpawnFailure = "Could not start 'mystran.exe': The system cannot find the file specified."
            });

            var result = runner.Run(deck);

            Assert.False(result.Succeeded);
            Assert.Contains("Could not start", result.Run.FailureMessage);
            Assert.DoesNotContain("wrote no print file", result.Run.FailureMessage);
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

        /// <summary>
        /// A descriptor pointing at a REAL FILE in this test's own temp folder.
        ///
        /// This used to be <c>ExecutableCandidates = { "mystran.exe" }</c>, with a comment
        /// claiming a bare name was "accepted as on PATH". That was true of an OLDER
        /// ResolveExecutable, which returned bare filenames WITHOUT CHECKING -- the same
        /// defect that had a workspace tile reporting "Found on disk" about nothing, and
        /// Process.Start throwing an unhandled Win32Exception when the button was pressed.
        ///
        /// Fixing that in production invalidated this fixture, and nothing noticed for a day
        /// because these tests had never been executed. Ten of them failed the first time
        /// anyone ran them, every one with "MYSTRAN was not found" from a resolution step that
        /// never reached the behaviour being tested.
        ///
        /// The stub is a real file now, so resolution is genuinely exercised rather than
        /// stepped over. It is never executed -- FakeProcessRunner intercepts the spawn -- so
        /// its contents do not matter, only its existence.
        /// </summary>
        private ToolDescriptor FakeDescriptor()
        {
            var stub = Path.Combine(_dir, "mystran-stub.exe");
            if (!File.Exists(stub)) File.WriteAllText(stub, "not a real executable");

            return new ToolDescriptor
            {
                Id = ToolRegistry.Mystran,
                DisplayName = "MYSTRAN",
                Kind = ToolKind.BatchExecutable,
                ExecutableCandidates = new[] { stub }
            };
        }

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
            public string? SpawnFailure { get; set; }
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
                    StandardError = SpawnFailure ?? string.Empty,
                    StandardOutput = StandardOutput,
                    TimedOut = TimedOut,
                    Duration = TimeSpan.FromSeconds(1)
                };
            }
        }
    }
}
