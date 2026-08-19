// WorldPackageExporterTurbineTests.cs
// Covers WorldPackageExporter.WriteTurbine, added 2026-08-02 when the MVP's turbine data
// source became the R2011a Simulink model (SCOPE.md 2026-08-02) rather than Simscape or the
// analytic fallback.
//
// THE TEST THAT MATTERS MOST HERE IS THE ONE THAT ASSERTS A THROW.
//
// WriteTurbine deliberately does NOT degrade silently the way WritePendulum does, and that
// asymmetry is easy to "fix" later by someone who reads the two methods side by side and
// assumes it was an oversight. MissingCsv_WithoutAllowFallback_Throws is what stops that.
//
// The reasoning, restated so it survives here as well as in the method's remarks: a pendulum
// on the analytic curve still visibly swings, so nobody is fooled. A turbine on a constant-rate
// curve is indistinguishable on screen from a turbine on real model output -- a rotor turning
// steadily looks the same either way. The MVP's engineering-rigor claim rests on that data
// being model output, so an accidental placeholder would make a public claim false with
// nothing on screen to give it away.
//
// SQL helpers are duplicated per-file, matching the convention in the sibling exporter tests.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using DWM.Shared;
using Microsoft.Data.Sqlite;
using Xunit;

namespace DWMStudio.Tests
{
    public sealed class WorldPackageExporterTurbineTests : IDisposable
    {
        private readonly string _csvPath;
        private readonly List<string> _extraPaths = new();
        private readonly string _dbPath;

        public WorldPackageExporterTurbineTests()
        {
            var runId = Guid.NewGuid().ToString("N");
            _csvPath = Path.Combine(Path.GetTempPath(), $"dwm_turbine_test_samples_{runId}_rotor.csv");
            _dbPath = Path.Combine(Path.GetTempPath(), $"dwm_turbine_test_world_{runId}.db");
        }

        public void Dispose()
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(_csvPath)) File.Delete(_csvPath);
            foreach (var p in _extraPaths) if (File.Exists(p)) File.Delete(p);
            if (File.Exists(_dbPath)) File.Delete(_dbPath);
        }

        // ------------------------------------------------------------------
        /// <summary>
        /// Stands in for wtExportSimSamples.m: same header and column meanings
        /// (time s, rotor azimuth rad UNWRAPPED, rotor speed rad/s), with a rotor
        /// accelerating rather than holding steady, so the samples are distinguishable
        /// from the constant-rate placeholder.
        /// </summary>
        private void WriteFakeSimCsv(int rows = 10)
        {
            var lines = new List<string> { "Time,Position,Velocity" };
            double azimuth = 0.0, dt = 1.0 / 30.0;
            for (int i = 0; i < rows; i++)
            {
                double t = i * dt;
                double omega = 1.0 + 0.05 * i;          // spinning up
                azimuth += omega * dt;
                lines.Add(string.Format(CultureInfo.InvariantCulture,
                    "{0:R},{1:R},{2:R}", t, azimuth, omega));
            }
            File.WriteAllLines(_csvPath, lines);
        }

        private List<(double Time, double Position, double Velocity)> ReadSamples()
        {
            var result = new List<(double, double, double)>();
            using var conn = new SqliteConnection($"Data Source={_dbPath}");
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Time, Position, Velocity FROM SimSamples ORDER BY Time;";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                result.Add((reader.GetDouble(0), reader.GetDouble(1), reader.GetDouble(2)));
            return result;
        }

        private string ScalarString(string sql)
        {
            using var conn = new SqliteConnection($"Data Source={_dbPath}");
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            return cmd.ExecuteScalar()?.ToString();
        }

        // ==================================================================
        [Fact]
        public void MissingCsv_WithoutAllowFallback_Throws()
        {
            // The guard this whole method exists for. See the file header.
            var exporter = new WorldPackageExporter();

            var ex = Assert.Throws<FileNotFoundException>(
                () => exporter.WriteTurbine(_dbPath, "turbine", "/no/such/file.csv"));

            // The message has to tell the caller how to produce the file; an unadorned
            // FileNotFoundException would send them reading the exporter source.
            Assert.Contains("wtExportSimSamples", ex.Message);
            Assert.Contains("allowFallback", ex.Message);
        }

        [Fact]
        public void MissingCsv_WithoutAllowFallback_LeavesNoDatabaseBehind()
        {
            // Loading happens before the transaction opens precisely so a refusal does not
            // leave a half-built .db that a later run would mistake for a real export.
            var exporter = new WorldPackageExporter();

            Assert.Throws<FileNotFoundException>(
                () => exporter.WriteTurbine(_dbPath, "turbine", null));

            var samples = File.Exists(_dbPath) ? ReadSamples() : new List<(double, double, double)>();
            Assert.Empty(samples);
        }

        [Fact]
        public void NullCsv_WithAllowFallback_WritesPlaceholderAtConstantSpeed()
        {
            // The escape hatch still works when asked for by name.
            new WorldPackageExporter().WriteTurbine(_dbPath, "turbine", null, allowFallback: true);

            var samples = ReadSamples();
            Assert.NotEmpty(samples);

            // Constant by construction -- the placeholder deliberately makes no attempt to
            // imitate the model, so that it stays easy to notice.
            var firstSpeed = samples[0].Velocity;
            Assert.All(samples, s => Assert.Equal(firstSpeed, s.Velocity, 9));
        }

        [Fact]
        public void CsvPresent_IsPreferredOverFallbackEvenWhenFallbackAllowed()
        {
            // Passing allowFallback must not mean "use the fallback"; real data still wins.
            WriteFakeSimCsv();
            new WorldPackageExporter().WriteTurbine(_dbPath, "turbine", _csvPath, allowFallback: true);

            var samples = ReadSamples();
            Assert.Equal(10, samples.Count);

            // The CSV's rotor accelerates; the placeholder's does not.
            Assert.NotEqual(samples[0].Velocity, samples[^1].Velocity);
        }

        [Fact]
        public void CsvSamples_RoundTripValuesAndOrderExactly()
        {
            WriteFakeSimCsv();
            new WorldPackageExporter().WriteTurbine(_dbPath, "turbine", _csvPath);

            var samples = ReadSamples();
            Assert.Equal(10, samples.Count);

            // Azimuth is UNWRAPPED, so it must increase monotonically across the whole run.
            // If someone later "helpfully" wraps it to [0, 2*pi) in the exporter, this fails --
            // which is the point, because interpolating across a wrap produces a full
            // backwards spin on screen.
            for (int i = 1; i < samples.Count; i++)
            {
                Assert.True(samples[i].Time > samples[i - 1].Time, "Time must be strictly increasing");
                Assert.True(samples[i].Position > samples[i - 1].Position,
                            "Azimuth must increase monotonically -- it is unwrapped by design");
            }

            Assert.Equal(0.0, samples[0].Time, 9);
            Assert.Equal(1.0, samples[0].Velocity, 9);
        }

        [Fact]
        public void DuplicateTimestampsInCsv_FailLoudlyRatherThanSilentlyDroppingSamples()
        {
            // SimSamples has PRIMARY KEY (BlockId, Time). A variable-step solver can emit the
            // same timestamp twice around events, which is exactly why wtExportSimSamples.m
            // strips duplicates and resamples onto a uniform grid before writing. If that
            // guarantee is ever broken upstream, this is what it looks like from here: a
            // constraint violation, not a quietly shorter animation.
            File.WriteAllLines(_csvPath, new[]
            {
                "Time,Position,Velocity",
                "0.0,0.0,1.0",
                "0.1,0.1,1.0",
                "0.1,0.2,1.0",   // duplicate Time
            });

            Assert.ThrowsAny<SqliteException>(
                () => new WorldPackageExporter().WriteTurbine(_dbPath, "turbine", _csvPath));
        }

        [Fact]
        public void WorldInfo_DescriptionDoesNotClaimSimscape()
        {
            // SCOPE.md 2026-08-02: demo materials may say "real engineering model" and may NOT
            // say Simscape, multibody, or CAD-verified physics -- there is still no Simscape
            // licence. The package description travels with the data, so it is one of the
            // places that claim could escape from. Pin it.
            WriteFakeSimCsv();
            new WorldPackageExporter().WriteTurbine(_dbPath, "turbine", _csvPath);

            var description = ScalarString("SELECT Description FROM WorldInfo;");
            Assert.NotNull(description);

            Assert.DoesNotContain("CAD-linked multibody dynamics claim",
                description, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("NOT Simscape Multibody", description, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Simulink", description, StringComparison.OrdinalIgnoreCase);
        }

        // ==================================================================
        // MULTI-CHANNEL: the turbine has four moving parts plus a signal block.
        // SimSamples is keyed on (BlockId, Time), so these are extra BLOCKS rather
        // than extra columns -- the schema used as designed, with no migration.

        /// <summary>Writes the sibling channel files wtExportSimSamples.m produces.</summary>
        private void WriteChannelCsv(string suffix, int rows, double scale)
        {
            var at = _csvPath.LastIndexOf("_rotor", StringComparison.Ordinal);
            var path = _csvPath.Substring(0, at) + "_" + suffix + _csvPath.Substring(at + 6);
            var lines = new List<string> { "Time,Position,Velocity" };
            for (int i = 0; i < rows; i++)
            {
                double time = i / 30.0;
                lines.Add(string.Format(CultureInfo.InvariantCulture, "{0:R},{1:R},{2:R}",
                    time, scale * i, scale));
            }
            File.WriteAllLines(path, lines);
            _extraPaths.Add(path);
        }

        private List<string> BlockIds()
        {
            var ids = new List<string>();
            using var conn = new SqliteConnection($"Data Source={_dbPath}");
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT BlockId FROM Blocks ORDER BY BlockId;";
            using var r = cmd.ExecuteReader();
            while (r.Read()) ids.Add(r.GetString(0));
            return ids;
        }

        private double ScalarDouble(string sql)
        {
            using var conn = new SqliteConnection($"Data Source={_dbPath}");
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            return Convert.ToDouble(cmd.ExecuteScalar(), CultureInfo.InvariantCulture);
        }

        private int SampleCount(string blockId)
        {
            using var conn = new SqliteConnection($"Data Source={_dbPath}");
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"SELECT COUNT(*) FROM SimSamples WHERE BlockId = '{blockId}';";
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        [Fact]
        public void SiblingChannelFiles_AreLoadedAsAdditionalBlocks()
        {
            WriteFakeSimCsv();
            foreach (var s in new[] { "pitch", "yaw", "tower", "power" }) WriteChannelCsv(s, 10, 0.1);

            new WorldPackageExporter().WriteTurbine(_dbPath, "turbine", _csvPath);

            Assert.Equal(
                new[] { "block_pitch", "block_power", "block_rotor", "block_tower", "block_yaw" },
                BlockIds());
            foreach (var b in new[] { "block_rotor", "block_pitch", "block_yaw", "block_tower", "block_power" })
                Assert.Equal(10, SampleCount(b));
        }

        [Fact]
        public void MissingSiblingChannels_AreSimplySkipped()
        {
            // Rotor-only is a legitimate export, and it is what every earlier test does.
            WriteFakeSimCsv();
            new WorldPackageExporter().WriteTurbine(_dbPath, "turbine", _csvPath);

            Assert.Equal(new[] { "block_rotor" }, BlockIds());
        }

        [Fact]
        public void MeasuredTowerFrequency_TravelsWithThePackage_AndSoDoesTheModelsDisagreement()
        {
            // The only numbers in the package measured by a solver rather than copied from a
            // model. BOTH are written deliberately: MYSTRAN says 0.2810991 Hz, the Simulink
            // model assumes 0.320 Hz, and the 13.8% gap is the verification gate's finding.
            // Publishing only the comfortable one would be the easier choice and the wrong one.
            WriteFakeSimCsv();
            foreach (var s in new[] { "pitch", "yaw", "tower", "power" }) WriteChannelCsv(s, 10, 0.1);

            new WorldPackageExporter().WriteTurbine(_dbPath, "turbine", _csvPath);

            // Compared as numbers, not as strings. Value is REAL, and double.ToString() puts a
            // comma in for the decimal separator under a good many cultures -- a test that
            // passes in one locale and fails in another tells you about the agent, not the code.
            Assert.Equal(0.2810991, ScalarDouble(
                "SELECT Value FROM Parameters WHERE BlockId='block_tower' AND Name='f_tower_measured';"), 7);
            Assert.Equal(0.320, ScalarDouble(
                "SELECT Value FROM Parameters WHERE BlockId='block_tower' AND Name='f_tower_model';"), 7);

            // Units are carried, which is the whole reason this belongs in Parameters rather
            // than being smuggled into SimSamples -- that table has no Unit column and is keyed
            // on Time, and a mode shape is not on a timeline.
            Assert.Equal("Hz", ScalarString(
                "SELECT Unit FROM Parameters WHERE BlockId='block_tower' AND Name='f_tower_measured';"));
        }

        [Fact]
        public void NoTowerBlock_MeansNoTowerParameters_BecauseNothingWouldCatchAnOrphan()
        {
            // THE GUARD THIS TEST EXISTS FOR. A rotor-only export creates no block_tower --
            // SeedTurbineChannel returns 0 without inserting when the sibling CSV is absent --
            // and the mechanism schema has NO FOREIGN KEYS (fragility audit item 2). So an
            // unguarded insert would not throw: it would write two parameter rows pointing at a
            // block that does not exist, and `verify` would report green.
            WriteFakeSimCsv();

            new WorldPackageExporter().WriteTurbine(_dbPath, "turbine", _csvPath);

            Assert.Equal(new[] { "block_rotor" }, BlockIds());
            Assert.Equal("0", ScalarString(
                "SELECT COUNT(*) FROM Parameters WHERE BlockId='block_tower';"));

            // Stated as the general property rather than the specific case, because the next
            // orphan will be some other block: no Parameters row may name a missing block.
            Assert.Equal("0", ScalarString(
                "SELECT COUNT(*) FROM Parameters p WHERE NOT EXISTS " +
                "(SELECT 1 FROM Blocks b WHERE b.BlockId = p.BlockId);"));
        }

        [Fact]
        public void PowerBlock_IsTypedSignal_AndHasNoAssetBinding()
        {
            // The one non-kinematic block: Position/Velocity are plain channel slots
            // (watts and m/s), which is why BlockType marks it and nothing binds a mesh.
            WriteFakeSimCsv();
            WriteChannelCsv("power", 5, 1000.0);

            new WorldPackageExporter().WriteTurbine(_dbPath, "turbine", _csvPath);

            Assert.Equal("Signal", ScalarString("SELECT BlockType FROM Blocks WHERE BlockId = 'block_power';"));
            Assert.Equal("0", ScalarString("SELECT COUNT(*) FROM AssetBindings WHERE BlockId = 'block_power';"));
            // The kinematic blocks DO bind meshes.
            Assert.Equal("RigidBody", ScalarString("SELECT BlockType FROM Blocks WHERE BlockId = 'block_rotor';"));
        }

        [Fact]
        public void PathWithoutRotorSuffix_DoesNotTriggerChannelDiscovery()
        {
            // Substitution keys on the LAST "_rotor"; a path lacking it must stay
            // rotor-only rather than probing for arbitrary sibling files.
            var plain = Path.Combine(Path.GetTempPath(), $"dwm_plain_{Guid.NewGuid():N}.csv");
            _extraPaths.Add(plain);
            File.WriteAllLines(plain, new[] { "Time,Position,Velocity", "0,0,1", "0.1,0.1,1" });

            new WorldPackageExporter().WriteTurbine(_dbPath, "turbine", plain);

            Assert.Equal(new[] { "block_rotor" }, BlockIds());
        }

        [Fact]
        public void AssetPath_IsTheMountainRotorMesh()
        {
            // THE TRIPWIRE THIS REPLACES HAS FIRED AND DONE ITS JOB (2026-08-18).
            //
            // It previously asserted the rotor binding was still "REPLACE_ME/WindTurbineRotor",
            // deliberately written to FAIL the moment a real path was filled in, so the change
            // could not happen by accident. It failed, on purpose, and this is the update it
            // was asking for.
            //
            // The property being guarded has not changed and is still worth a test: a wrong
            // asset path binds nothing and the rotor simply does not appear, with no error
            // anywhere. This now pins the exact path instead of pinning its absence.
            //
            // NOT VERIFIED BY THIS TEST: that the path resolves to an actual mesh. It cannot
            // be -- /Content/Wind_Turbine/ is a gitignored Marketplace pack that the C# test
            // project has no view of. This asserts the string, and the editor asserts the rest.
            WriteFakeSimCsv();
            new WorldPackageExporter().WriteTurbine(_dbPath, "turbine", _csvPath);

            var assetPath = ScalarString("SELECT AssetPath FROM AssetBindings;");
            Assert.Equal("/Game/Wind_Turbine/Meshes/SM_Rotor.SM_Rotor", assetPath);
        }
    }
}
