// GoldenWorldPackageTests.cs
// Day 13: regression test for the committed golden-scenario world package
// (Fixtures/golden_world_economy.db). Opens the FIXTURE FILE DIRECTLY (read-only, raw SQL --
// deliberately not going through EconomyRepository/DollarVaultRepository, since those expect
// the live economy.db table shape, not the exported world-package shape written by
// WorldPackageExporter.WriteEconomySnapshot) and asserts the calibrated starting scenario
// hasn't silently drifted.
//
// If this test ever needs to change, it means the golden scenario changed ON PURPOSE --
// regenerate the fixture (see GoldenEconomyScenario.Seed / WorldPackageExporter.
// WriteGoldenEconomySnapshot) and update these assertions together, don't let them drift apart.

using System.IO;
using Microsoft.Data.Sqlite;
using Xunit;

namespace DWMStudio.Tests
{
    public sealed class GoldenWorldPackageTests
    {
        private static string FixturePath =>
            Path.Combine(AppContext.BaseDirectory, "Fixtures", "golden_world_economy.db");

        private static SqliteConnection OpenReadOnly()
        {
            var connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = FixturePath,
                Mode = SqliteOpenMode.ReadOnly
            }.ToString();
            var conn = new SqliteConnection(connectionString);
            conn.Open();
            return conn;
        }

        [Fact]
        public void FixtureFile_Exists()
        {
            Assert.True(File.Exists(FixturePath), $"Golden fixture not found at '{FixturePath}'.");
        }

        [Fact]
        public void HasExpectedTableRowCounts()
        {
            using var conn = OpenReadOnly();

            Assert.Equal(5, Scalar(conn, "SELECT COUNT(*) FROM Communities;"));
            Assert.Equal(10, Scalar(conn, "SELECT COUNT(*) FROM Resources;"));
            Assert.Equal(24, Scalar(conn, "SELECT COUNT(*) FROM CommunityResources;"));
            Assert.Equal(5, Scalar(conn, "SELECT COUNT(*) FROM CommunityDollarVault;"));
            Assert.Equal(5, Scalar(conn, "SELECT COUNT(*) FROM CommunityFailureStatus;"));
        }

        [Fact]
        public void StoneLedger_IsEmpty()
        {
            using var conn = OpenReadOnly();
            Assert.Equal(0, Scalar(conn, "SELECT COUNT(*) FROM StoneLedger;"));
        }

        [Theory]
        [InlineData("mountain", 4200.0)]
        [InlineData("hillside", 4400.0)]
        [InlineData("valley", 4600.0)]
        [InlineData("suburb", 4000.0)]
        [InlineData("city", 5000.0)]
        public void CommunityHasExpectedCalibratedBalance(string communityId, double expectedBalance)
        {
            using var conn = OpenReadOnly();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Balance, Threshold FROM CommunityDollarVault WHERE CommunityId = $id;";
            cmd.Parameters.AddWithValue("$id", communityId);
            using var reader = cmd.ExecuteReader();

            Assert.True(reader.Read(), $"No CommunityDollarVault row for '{communityId}'.");
            Assert.Equal(expectedBalance, reader.GetDouble(0), precision: 9);
            Assert.Equal(500.0, reader.GetDouble(1), precision: 9);
        }

        [Fact]
        public void AllCommunities_AreHealthyAtGoldenStart()
        {
            using var conn = OpenReadOnly();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT CommunityId, State FROM CommunityFailureStatus;";
            using var reader = cmd.ExecuteReader();

            var count = 0;
            while (reader.Read())
            {
                count++;
                Assert.Equal("Healthy", reader.GetString(1));
            }
            Assert.Equal(5, count);
        }

        private static long Scalar(SqliteConnection conn, string sql)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            return (long)cmd.ExecuteScalar()!;
        }
    }
}
