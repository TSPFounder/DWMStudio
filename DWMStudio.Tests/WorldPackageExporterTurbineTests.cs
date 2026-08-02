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
        private readonly string _dbPath;

        public WorldPackageExporterTurbineTests()
        {
            var runId = Guid.NewGuid().ToString("N");
            _csvPath = Path.Combine(Path.GetTempPath(), $"dwm_turbine_test_samples_{runId}.csv");
            _dbPath = Path.Combine(Path.GetTempPath(), $"dwm_turbine_test_world_{runId}.db");
        }

        public void Dispose()
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(_csvPath)) File.Delete(_csvPath);
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

        [Fact]
        public void AssetPath_IsStillThePlaceholder_SoThisFailsOnceItIsSetProperly()
        {
            // A DELIBERATE TRIPWIRE, not an assertion that the placeholder is correct.
            //
            // The Mountain turbine mesh was placed on Day 21 and its real content path is not
            // recorded anywhere this exporter can see, so WriteTurbine ships "REPLACE_ME/...".
            // A wrong asset path binds nothing and the rotor simply does not appear, with no
            // error anywhere -- the worst kind of failure to debug.
            //
            // When the real path is filled in, THIS TEST WILL FAIL. That is the intended
            // behaviour: update the expected value here at the same time, and the tripwire has
            // done its job of making sure the change was deliberate rather than forgotten.
            WriteFakeSimCsv();
            new WorldPackageExporter().WriteTurbine(_dbPath, "turbine", _csvPath);

            var assetPath = ScalarString("SELECT AssetPath FROM AssetBindings;");
            Assert.Equal("REPLACE_ME/WindTurbineRotor", assetPath);
        }
    }
}
