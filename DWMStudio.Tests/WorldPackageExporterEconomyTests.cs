// WorldPackageExporterEconomyTests.cs
// Day 15: closes three of the five Phase 1 closing-review test gaps for
// WorldPackageExporter.WriteEconomySnapshot (Day 12):
//   1. The export loop's actual StoneLedger row-writing logic (including
//      ResourceId/Quantity null-handling) had zero coverage -- the only fixture that
//      exercises it (golden_world_economy.db) always has an empty ledger by design.
//   4. TradeRequests is absent from the exported schema -- pin that as a deliberate,
//      test-enforced decision instead of an implicit assumption.
//   5. CommunityResources.Role / CommunityFailureStatus.State are written as plain enum
//      ToString() strings with no schema CHECK -- pin the exact literals so a future
//      enum rename/refactor fails loudly here instead of silently changing the wire format.
//
// SQL helpers duplicated per-file rather than shared, matching the existing convention
// (EconomyLedgerInvariantTests.cs, TradeSettlementServiceTests.cs, etc.).

using System;
using System.Collections.Generic;
using System.IO;
using DWM.Shared;
using DWM.Shared.Economy;
using Microsoft.Data.Sqlite;
using Xunit;

namespace DWMStudio.Tests
{
    public sealed class WorldPackageExporterEconomyTests : IDisposable
    {
        private readonly string _economyDbPath;
        private readonly string _exportedDbPath;

        public WorldPackageExporterEconomyTests()
        {
            var runId = Guid.NewGuid().ToString("N");
            _economyDbPath = Path.Combine(Path.GetTempPath(), $"dwm_exporter_test_economy_{runId}.db");
            _exportedDbPath = Path.Combine(Path.GetTempPath(), $"dwm_exporter_test_world_{runId}.db");
        }

        public void Dispose()
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(_economyDbPath)) File.Delete(_economyDbPath);
            if (File.Exists(_exportedDbPath)) File.Delete(_exportedDbPath);
        }

        [Fact]
        public void WriteEconomySnapshot_WithNonEmptyStoneLedger_RoundTripsEveryRowCorrectly()
        {
            new EconomySeeder().WriteEconomy(_economyDbPath);
            var economy = new EconomyRepository(_economyDbPath);
            var service = new TradeSettlementService(economy);

            // Resource-attached trade #1: hillside Needs textiles, suburb Produces textiles.
            var trade1 = service.SettleTrade("hillside", "suburb", 20, "textiles", 20,
                "Hillside buys Textiles from Suburb");
            Assert.True(trade1.Success);

            // Stone-only trade: ResourceId and Quantity both deliberately null.
            var trade2 = service.SettleTrade("mountain", "city", 15, null, null,
                "Stone-only settlement, no attached resource");
            Assert.True(trade2.Success);

            // Resource-attached trade #2, a different pair: valley Needs wool, hillside Produces wool.
            var trade3 = service.SettleTrade("valley", "hillside", 8, "wool", 8,
                "Valley buys Wool from Hillside");
            Assert.True(trade3.Success);

            new WorldPackageExporter().WriteEconomySnapshot(_exportedDbPath, _economyDbPath, worldId: "gap_test");

            var exportedRows = ReadStoneLedger(_exportedDbPath);
            Assert.Equal(3, exportedRows.Count);

            AssertRoundTripped(exportedRows, trade1.LedgerEntry!, expectedResourceId: "textiles", expectedQuantity: 20.0);
            AssertRoundTripped(exportedRows, trade2.LedgerEntry!, expectedResourceId: null, expectedQuantity: null);
            AssertRoundTripped(exportedRows, trade3.LedgerEntry!, expectedResourceId: "wool", expectedQuantity: 8.0);
        }

        [Fact]
        public void ExportedSchema_DoesNotIncludeTradeRequestsTable()
        {
            new EconomySeeder().WriteEconomy(_economyDbPath);
            new WorldPackageExporter().WriteEconomySnapshot(_exportedDbPath, _economyDbPath, worldId: "gap_test");

            using var conn = OpenReadOnly(_exportedDbPath);
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = 'TradeRequests';";
            var count = (long)cmd.ExecuteScalar()!;

            Assert.Equal(0, count);
        }

        [Fact]
        public void ExportedRoleAndFailureStateStrings_MatchThePinnedLiterals()
        {
            // Seed the golden scenario (gives every community Produces AND Needs rows, all
            // Healthy) then drive City into failure so both State literals appear.
            GoldenEconomyScenario.Seed(_economyDbPath);
            CityCascadingFailureScenario.Run(_economyDbPath);

            new WorldPackageExporter().WriteEconomySnapshot(_exportedDbPath, _economyDbPath, worldId: "gap_test");

            using var conn = OpenReadOnly(_exportedDbPath);

            var roles = new HashSet<string>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT DISTINCT Role FROM CommunityResources;";
                using var reader = cmd.ExecuteReader();
                while (reader.Read()) roles.Add(reader.GetString(0));
            }
            Assert.Equal(new HashSet<string> { "Produces", "Needs" }, roles);

            Assert.Equal("CascadingFailure", FailureStateFor(conn, "city"));
            Assert.Equal("Healthy", FailureStateFor(conn, "mountain"));
        }

        // ------------------------------------------------------------------
        private static string FailureStateFor(SqliteConnection conn, string communityId)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT State FROM CommunityFailureStatus WHERE CommunityId = $id;";
            cmd.Parameters.AddWithValue("$id", communityId);
            return (string)cmd.ExecuteScalar()!;
        }

        private static void AssertRoundTripped(
            Dictionary<string, (double Amount, string From, string To, string? ResourceId, double? Quantity, string? Memo)> exportedRows,
            LedgerEntry original, string? expectedResourceId, double? expectedQuantity)
        {
            Assert.True(exportedRows.TryGetValue(original.TransactionId, out var row),
                $"TransactionId '{original.TransactionId}' not found in exported StoneLedger.");

            Assert.Equal(original.Amount, row.Amount, precision: 9);
            Assert.Equal(original.FromCommunityId, row.From);
            Assert.Equal(original.ToCommunityId, row.To);
            Assert.Equal(expectedResourceId, row.ResourceId);
            Assert.Equal(expectedQuantity, row.Quantity);
            Assert.Equal(original.Memo, row.Memo);
        }

        private static Dictionary<string, (double Amount, string From, string To, string? ResourceId, double? Quantity, string? Memo)>
            ReadStoneLedger(string dbPath)
        {
            var results = new Dictionary<string, (double, string, string, string?, double?, string?)>();
            using var conn = OpenReadOnly(dbPath);
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT TransactionId, Amount, FromCommunityId, ToCommunityId, ResourceId, Quantity, Memo FROM StoneLedger;";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                results[reader.GetString(0)] = (
                    reader.GetDouble(1),
                    reader.GetString(2),
                    reader.GetString(3),
                    reader.IsDBNull(4) ? null : reader.GetString(4),
                    reader.IsDBNull(5) ? null : reader.GetDouble(5),
                    reader.IsDBNull(6) ? null : reader.GetString(6));
            }
            return results;
        }

        private static SqliteConnection OpenReadOnly(string dbPath)
        {
            var conn = new SqliteConnection(new SqliteConnectionStringBuilder
            {
                DataSource = dbPath,
                Mode = SqliteOpenMode.ReadOnly
            }.ToString());
            conn.Open();
            return conn;
        }
    }
}
