// MatlabStageServiceTests.cs
// Covers the MATLAB pipeline stage WITHOUT MATLAB.
//
// The COM transport cannot run here -- it needs Windows, an installed MATLAB, and a licence,
// and the build agent has none of the three. That is exactly why IMatlabSession exists: every
// behaviour worth protecting in this stage is orchestration, not transport. What MATLAB
// actually computes is the model's business and is verified in MATLAB; what this stage owes
// the rest of the system is that it never hands downstream a package built from data that
// did not come from the run it claims to describe.
//
// THE SINGLE MOST IMPORTANT TEST IN THIS FILE is
// StaleRotorCsv_IsTreatedAsMissing_RatherThanExportedAsThisRun. Everything else is hygiene.
//
// SQL helpers are duplicated per-file, matching the convention in the sibling exporter tests.

using System;
using System.Collections.Generic;
using System.IO;
using DWM.Shared.Matlab;
using Microsoft.Data.Sqlite;
using Xunit;

namespace DWMStudio.Tests
{
    public sealed class MatlabStageServiceTests : IDisposable
    {
        private readonly string _dir;

        public MatlabStageServiceTests()
        {
            _dir = Path.Combine(Path.GetTempPath(), "dwm_matlab_stage_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
        }

        public void Dispose()
        {
            SqliteConnection.ClearAllPools();
            try { if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true); }
            catch (IOException) { /* a locked temp dir is not a test failure */ }
        }

        // ==================================================================
        // Command construction
        // ==================================================================

        [Fact]
        public void MatlabLiteral_LeavesBackslashesAlone_BecauseSingleQuotedMatlabStringsAreLiteral()
        {
            // If backslashes were escaped, every Windows path would break. MATLAB single-quoted
            // strings do no escape processing, which is the whole reason this is safe.
            Assert.Equal(@"'C:\Users\henry\MATLAB'",
                MatlabStageService.MatlabLiteral(@"C:\Users\henry\MATLAB"));
        }

        [Fact]
        public void MatlabLiteral_DoublesSingleQuotes()
        {
            // Legal on Windows, and a MATLAB syntax error if not doubled.
            Assert.Equal(@"'C:\Henry''s Models\turbine'",
                MatlabStageService.MatlabLiteral(@"C:\Henry's Models\turbine"));
        }

        [Fact]
        public void BuildGuardedCommand_ClearsTheSentinelBeforeRunning()
        {
            // Order matters: if the sentinel were not cleared first, a previous command's error
            // would still be sitting there and this command would be reported as failing.
            var wrapped = MatlabStageService.BuildGuardedCommand("x = 1;");
            var clearIndex = wrapped.IndexOf("dwmStageErr = ''", StringComparison.Ordinal);
            var tryIndex = wrapped.IndexOf("try", StringComparison.Ordinal);

            Assert.True(clearIndex >= 0, "the sentinel is never cleared");
            Assert.True(tryIndex > clearIndex, "the sentinel must be cleared before the try block");
        }

        [Fact]
        public void BuildGuardedCommand_CapturesTheMessageIntoTheSentinel()
        {
            var wrapped = MatlabStageService.BuildGuardedCommand("wtRunSimulation('ramp')");
            Assert.Contains("catch dwmStageME", wrapped);
            Assert.Contains("dwmStageErr = dwmStageME.message", wrapped);
            Assert.EndsWith("end", wrapped);
        }

        [Fact]
        public void BuildGuardedCommand_IsASingleLine_BecauseComExecuteTakesOneCommand()
        {
            var wrapped = MatlabStageService.BuildGuardedCommand("x = 1;");
            Assert.DoesNotContain("\n", wrapped);
            Assert.DoesNotContain("\r", wrapped);
        }

        // ==================================================================
        // Channel naming -- coupled to wtExportSimSamples.m by agreement only
        // ==================================================================

        [Fact]
        public void ChannelCsvPath_MatchesTheNamingWtExportSimSamplesActuallyUses()
        {
            // wtExportSimSamples.m: fullfile(baseDir, [baseName '_' suffix baseExt]).
            // If that line changes, this test is what should fail.
            var request = Request();

            Assert.Equal(Path.Combine(_dir, "wtSimSamples_rotor.csv"), request.ChannelCsvPath("rotor"));
            Assert.Equal(Path.Combine(_dir, "wtSimSamples_power.csv"), request.ChannelCsvPath("power"));
        }

        [Fact]
        public void ChannelCsvPath_SuppliesCsvExtension_WhenTheBaseNameHasNone()
        {
            var request = new MatlabStageRequest
            {
                TurbineCodeDirectory = _dir,
                CsvBaseName = "wtSimSamples",
                OutputPackagePath = Path.Combine(_dir, "turbine.db")
            };

            Assert.Equal(Path.Combine(_dir, "wtSimSamples_rotor.csv"), request.ChannelCsvPath("rotor"));
        }

        // ==================================================================
        // The command sequence
        // ==================================================================

        [Fact]
        public void RunAndExport_AddsPathThenRunsThenExports_InThatOrder()
        {
            var session = ExportingSession();

            new MatlabStageService(() => session).RunAndExport(Request());

            Assert.Equal(3, session.Commands.Count);
            Assert.Contains("addpath(", session.Commands[0]);
            Assert.Contains("wtRunSimulation(", session.Commands[1]);
            Assert.Contains("wtExportSimSamples(", session.Commands[2]);
        }

        [Fact]
        public void AttachedMatlab_IsNotCdElsewhere_BecauseTheCurrentFolderIsTheUsersOwn()
        {
            var session = ExportingSession();
            session.IsAttachedToExistingInstance = true;

            new MatlabStageService(() => session).RunAndExport(Request());

            Assert.DoesNotContain(session.Commands, c => c.Contains("cd(", StringComparison.Ordinal));
        }

        [Fact]
        public void LaunchedMatlab_IsCdSomewhereWritable_BecauseAFreshOneStartsInProgramFiles()
        {
            // A MATLAB started over COM begins in its own install directory, which is read-only.
            // wtBuildModel saves wtTurbine3MW.mdl relative to the current folder, so without this
            // the run dies with "Permission denied" on a path nobody chose -- which is exactly
            // what happened on the first real launched run.
            var session = ExportingSession();
            session.IsAttachedToExistingInstance = false;

            new MatlabStageService(() => session).RunAndExport(Request());

            Assert.Contains("cd(", session.Commands[0]);
            Assert.Contains(_dir, session.Commands[0]);
            Assert.Contains("addpath(", session.Commands[1]);
        }

        [Fact]
        public void ExplicitWorkingDirectory_IsHonouredEvenWhenAttached()
        {
            var session = ExportingSession();
            session.IsAttachedToExistingInstance = true;

            var elsewhere = Path.Combine(_dir, "run_here");
            Directory.CreateDirectory(elsewhere);

            var request = new MatlabStageRequest
            {
                TurbineCodeDirectory = _dir,
                OutputPackagePath = Path.Combine(_dir, "turbine.db"),
                WorkingDirectory = elsewhere
            };
            new MatlabStageService(() => session).RunAndExport(request);

            Assert.Contains("cd(", session.Commands[0]);
            Assert.Contains(elsewhere, session.Commands[0]);
        }

        [Theory]
        [InlineData(TurbineScenario.Step, "step")]
        [InlineData(TurbineScenario.Ramp, "ramp")]
        [InlineData(TurbineScenario.Turbulent, "turbulent")]
        [InlineData(TurbineScenario.Gust, "gust")]
        public void Scenario_ReachesMatlabAsItsLowerCaseToken(TurbineScenario scenario, string token)
        {
            var session = ExportingSession();

            new MatlabStageService(() => session).RunAndExport(Request(scenario));

            Assert.Contains($"wtRunSimulation('{token}')", session.Commands[1]);
        }

        [Fact]
        public void SampleRate_IsPassedThroughToTheExportCall()
        {
            var session = ExportingSession();
            var request = new MatlabStageRequest
            {
                TurbineCodeDirectory = _dir,
                OutputPackagePath = Path.Combine(_dir, "turbine.db"),
                SampleRateHz = 60
            };

            new MatlabStageService(() => session).RunAndExport(request);

            Assert.Contains(", 60)", session.Commands[2]);
        }

        // ==================================================================
        // MATLAB-side errors
        // ==================================================================

        [Fact]
        public void MatlabError_IsRaisedAsAnException_EvenThoughExecuteReturnedNormally()
        {
            // The heart of it: FakeMatlabSession.Execute returns "" on failure, exactly as the
            // real COM Execute does. Detection has to come from the sentinel, never the
            // return value.
            var session = new FakeMatlabSession();
            session.ErrorsToReturn.Enqueue("");                                    // addpath
            session.ErrorsToReturn.Enqueue("Undefined function or variable 'wtRunSimulation'.");

            var ex = Assert.Throws<MatlabStageException>(
                () => new MatlabStageService(() => session).RunAndExport(Request()));

            Assert.Contains("Undefined function or variable", ex.Message);
            Assert.Contains("wtRunSimulation(", ex.Message);   // says which command failed
        }

        [Fact]
        public void MatlabError_DuringExport_StopsBeforeWritingAPackage()
        {
            var session = new FakeMatlabSession();
            session.ErrorsToReturn.Enqueue("");
            session.ErrorsToReturn.Enqueue("");
            session.ErrorsToReturn.Enqueue("Cannot open 'wtSimSamples_rotor.csv' for writing.");

            var request = Request();
            Assert.Throws<MatlabStageException>(
                () => new MatlabStageService(() => session).RunAndExport(request));

            Assert.False(File.Exists(request.OutputPackagePath),
                "a package must not exist after the export step failed");
        }

        [Fact]
        public void Session_IsDisposed_EvenWhenMatlabErrors()
        {
            var session = new FakeMatlabSession();
            session.ErrorsToReturn.Enqueue("");
            session.ErrorsToReturn.Enqueue("boom");

            Assert.Throws<MatlabStageException>(
                () => new MatlabStageService(() => session).RunAndExport(Request()));

            Assert.True(session.Disposed, "the MATLAB session leaked on the failure path");
        }

        // ==================================================================
        // Stale files -- THE tests this class exists for
        // ==================================================================

        [Fact]
        public void StaleRotorCsv_IsTreatedAsMissing_RatherThanExportedAsThisRun()
        {
            // The scenario: an export ran successfully yesterday, so all five CSVs sit on disk
            // under exactly the right names. Today the export silently does nothing. Without a
            // freshness check WriteTurbine opens yesterday's files, succeeds, and produces a
            // package that is well-formed, plausible, and describes a run that never happened
            // -- and a rotor turning steadily looks completely normal on screen.
            var session = new FakeMatlabSession();          // writes nothing, reports no error
            WriteAllChannels();
            AgeAllChannels(TimeSpan.FromHours(20));

            var request = Request();
            var ex = Assert.Throws<MatlabStageException>(
                () => new MatlabStageService(() => session).RunAndExport(request));

            Assert.Contains("predates this run", ex.Message);
            Assert.False(File.Exists(request.OutputPackagePath),
                "stale data must never reach a package");
        }

        [Fact]
        public void StaleOptionalChannel_IsExcludedAndWarnedAbout_NotSilentlyIncluded()
        {
            // Rotor is fresh, tower is yesterday's. A package built from a mix would be worse
            // than one built entirely from stale data, because the parts would disagree with
            // each other while every one of them looked fine alone.
            var session = new FakeMatlabSession
            {
                OnExecute = command =>
                {
                    if (!IsExport(command)) return;
                    WriteAllChannels();
                    AgeChannel("tower", TimeSpan.FromHours(20));
                }
            };

            var result = new MatlabStageService(() => session).RunAndExport(Request());

            Assert.Contains("tower", result.ChannelsMissing);
            Assert.DoesNotContain("tower", result.ChannelsExported);
            Assert.Contains(result.Warnings, w => w.Contains("STALE") && w.Contains("tower"));
        }

        [Fact]
        public void FreshnessTolerance_StaysNarrow_SoOnlyClockGranularityPasses()
        {
            // If this ever needs widening, widen it deliberately: every second of tolerance is
            // a second of staleness that can slip through the only check that looks for it.
            Assert.True(Request().StaleFileTolerance <= TimeSpan.FromSeconds(5),
                "the default staleness tolerance has grown, weakening the stale-data check");
        }

        // ==================================================================
        // Missing channels
        // ==================================================================

        [Fact]
        public void MissingRotorCsv_Throws_BecauseWriteTurbineCannotDoWithoutIt()
        {
            var session = ExportingSession(omit: "rotor");

            var ex = Assert.Throws<MatlabStageException>(
                () => new MatlabStageService(() => session).RunAndExport(Request()));

            Assert.Contains("rotor CSV is missing", ex.Message);
        }

        [Fact]
        public void MissingOptionalChannel_ProducesAWarning_NotSilence()
        {
            // WriteTurbine tolerates a missing sibling by writing no samples and returning 0.
            // That is correct behaviour there and invisible behaviour here, so this stage says so.
            var session = ExportingSession(omit: "yaw");

            var result = new MatlabStageService(() => session).RunAndExport(Request());

            Assert.Contains("yaw", result.ChannelsMissing);
            Assert.Contains(result.Warnings, w => w.Contains("yaw"));
            Assert.True(result.HasWarnings);
        }

        [Fact]
        public void RequireAllChannels_PromotesAMissingChannelToAnError()
        {
            var session = ExportingSession(omit: "power");
            var request = new MatlabStageRequest
            {
                TurbineCodeDirectory = _dir,
                OutputPackagePath = Path.Combine(_dir, "turbine.db"),
                RequireAllChannels = true
            };

            var ex = Assert.Throws<MatlabStageException>(
                () => new MatlabStageService(() => session).RunAndExport(request));

            Assert.Contains("power", ex.Message);
        }

        // ==================================================================
        // Warnings about meaning rather than mechanics
        // ==================================================================

        [Fact]
        public void GustScenario_WarnsThatTheExportWillLookLikeAPlaceholder()
        {
            var session = ExportingSession();

            var result = new MatlabStageService(() => session)
                .RunAndExport(Request(TurbineScenario.Gust));

            Assert.Contains(result.Warnings, w => w.Contains("GUST"));
        }

        [Fact]
        public void RampScenario_DoesNotWarn()
        {
            var session = ExportingSession();

            var result = new MatlabStageService(() => session)
                .RunAndExport(Request(TurbineScenario.Ramp));

            Assert.DoesNotContain(result.Warnings, w => w.Contains("GUST"));
        }

        // ==================================================================
        // End to end (everything except COM)
        // ==================================================================

        [Fact]
        public void AllFiveChannelsFresh_ProducesAPackageWithFiveBlocks()
        {
            var session = ExportingSession();
            var request = Request();

            var result = new MatlabStageService(() => session).RunAndExport(request);

            Assert.Equal(5, result.ChannelsExported.Count);
            Assert.Empty(result.ChannelsMissing);
            Assert.True(File.Exists(request.OutputPackagePath));

            using var conn = new SqliteConnection(
                new SqliteConnectionStringBuilder
                {
                    DataSource = request.OutputPackagePath,
                    Mode = SqliteOpenMode.ReadOnly
                }.ToString());
            conn.Open();

            Assert.Equal(5L, ScalarLong(conn, "SELECT COUNT(*) FROM Blocks;"));
            Assert.Equal(15L, ScalarLong(conn, "SELECT COUNT(*) FROM SimSamples;"));

            // The Signal block is the documented reuse of Position/Velocity as two plain
            // channel slots. If it silently becomes a RigidBody, something has misread the
            // schema and a HUD reading is about to be driven onto a mesh.
            Assert.Equal(1L, ScalarLong(conn,
                "SELECT COUNT(*) FROM Blocks WHERE BlockType = 'Signal';"));
        }

        // ==================================================================
        // Validation
        // ==================================================================

        [Fact]
        public void MissingTurbineCodeDirectory_ThrowsBeforeOpeningMatlab()
        {
            var session = new FakeMatlabSession();
            var request = new MatlabStageRequest
            {
                TurbineCodeDirectory = Path.Combine(_dir, "does_not_exist"),
                OutputPackagePath = Path.Combine(_dir, "turbine.db")
            };

            Assert.Throws<MatlabStageException>(
                () => new MatlabStageService(() => session).RunAndExport(request));

            Assert.Empty(session.Commands);   // never got as far as opening a session
        }

        [Fact]
        public void NonPositiveSampleRate_Throws()
        {
            var request = new MatlabStageRequest
            {
                TurbineCodeDirectory = _dir,
                OutputPackagePath = Path.Combine(_dir, "turbine.db"),
                SampleRateHz = 0
            };

            Assert.Throws<MatlabStageException>(
                () => new MatlabStageService(() => new FakeMatlabSession()).RunAndExport(request));
        }

        [Fact]
        public void UnknownScenarioValue_ThrowsRatherThanReachingMatlab()
        {
            // Guards the exhaustive switch: a fifth scenario added to the enum but not to
            // ToMatlabToken must fail here, not as a MATLAB error two minutes into a run.
            Assert.Throws<ArgumentOutOfRangeException>(
                () => ((TurbineScenario)99).ToMatlabToken());
        }

        // ==================================================================
        // Helpers
        // ==================================================================

        private MatlabStageRequest Request(TurbineScenario scenario = TurbineScenario.Ramp)
            => new MatlabStageRequest
            {
                TurbineCodeDirectory = _dir,
                OutputPackagePath = Path.Combine(_dir, "turbine.db"),
                Scenario = scenario
            };

        /// <summary>
        /// A session that writes the channel CSVs when the export command runs -- i.e. one
        /// standing in for a MATLAB where everything works. Files are created DURING the run
        /// rather than before it, so their timestamps genuinely postdate the run's start and
        /// the freshness check is exercised for real rather than passing on tolerance.
        /// </summary>
        private FakeMatlabSession ExportingSession(string? omit = null)
            => new FakeMatlabSession
            {
                OnExecute = command =>
                {
                    if (IsExport(command)) WriteAllChannelsExcept(omit);
                }
            };

        private static bool IsExport(string command)
            => command.Contains("wtExportSimSamples(", StringComparison.Ordinal);

        private static long ScalarLong(SqliteConnection conn, string sql)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            return (long)cmd.ExecuteScalar()!;
        }

        private void WriteAllChannels() => WriteAllChannelsExcept(null);

        private void WriteAllChannelsExcept(string? omit)
        {
            foreach (var suffix in MatlabStageService.ChannelSuffixes)
            {
                if (suffix == omit) continue;
                WriteChannel(suffix);
            }
        }

        private void WriteChannel(string suffix)
        {
            File.WriteAllText(
                Path.Combine(_dir, "wtSimSamples_" + suffix + ".csv"),
                "Time,Position,Velocity\n" +
                "0,0,1.2\n" +
                "0.0333333,0.04,1.2\n" +
                "0.0666667,0.08,1.2\n");
        }

        private void AgeAllChannels(TimeSpan by)
        {
            foreach (var suffix in MatlabStageService.ChannelSuffixes)
                AgeChannel(suffix, by);
        }

        private void AgeChannel(string suffix, TimeSpan by)
        {
            var path = Path.Combine(_dir, "wtSimSamples_" + suffix + ".csv");
            if (File.Exists(path))
                File.SetLastWriteTimeUtc(path, DateTime.UtcNow - by);
        }

        /// <summary>
        /// Stands in for MATLAB. Mirrors the two behaviours that matter: Execute returns
        /// normally whether or not the command failed, and the error is discoverable only by
        /// reading the sentinel afterwards.
        /// </summary>
        private sealed class FakeMatlabSession : IMatlabSession
        {
            public List<string> Commands { get; } = new();
            public Queue<string> ErrorsToReturn { get; } = new();
            public Action<string>? OnExecute { get; set; }
            public bool Disposed { get; private set; }

            /// <summary>
            /// DEFAULTS TO TRUE, which is not the arbitrary choice it looks like: an attached
            /// session is the one the stage leaves alone, so it produces the minimal command
            /// sequence (addpath, run, export). Tests asserting on command ORDER and INDEX
            /// depend on that. The launched case adds a CD in front and has its own tests.
            /// </summary>
            public bool IsAttachedToExistingInstance { get; set; } = true;

            private string _lastError = string.Empty;

            public string Execute(string command)
            {
                Commands.Add(command);
                _lastError = ErrorsToReturn.Count > 0 ? ErrorsToReturn.Dequeue() : string.Empty;
                OnExecute?.Invoke(command);
                return string.Empty;       // as the real one does, on success AND on failure
            }

            public string GetCharArray(string variableName) => _lastError;

            public void Dispose() => Disposed = true;
        }
    }
}
