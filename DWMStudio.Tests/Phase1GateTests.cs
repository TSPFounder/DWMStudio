// Phase1GateTests.cs
// Day 14: the Phase 1 gate. Days 5-13 proved each piece of the economy stack in isolation;
// this proves them as ONE CONTINUOUS HEADLESS RUN: seed -> settle via both trade paths that
// exist -> drive City into Cascading Failure -> confirm the network-sum-zero invariant ->
// export -> re-open the exported package fresh and confirm the data is actually correct, not
// just present. Same spirit as the Week-1 fresh-clone gate: one repeatable, automatable
// command (`dotnet test --filter "FullyQualifiedName~Phase1GateTests"`), not a manual
// walkthrough.
//
// Deliberately exercises EXISTING code end to end -- no new economy features. Any gap found
// here belongs to the integration, not to any one day's isolated unit tests, which is exactly
// the class of bug this gate exists to catch that they can't.

using System;
using System.IO;
using System.Linq;
using DWM.Shared;
using DWM.Shared.Economy;
using Microsoft.Data.Sqlite;
using Xunit;
using Xunit.Abstractions;

namespace DWMStudio.Tests
{
    public sealed class Phase1GateTests : IDisposable
    {
        private readonly ITestOutputHelper _output;
        private readonly string _economyDbPath;
        private readonly string _exportedDbPath;

        public Phase1GateTests(ITestOutputHelper output)
        {
            _output = output;
            var runId = Guid.NewGuid().ToString("N");
            _economyDbPath = Path.Combine(Path.GetTempPath(), $"dwm_gate_economy_{runId}.db");
            _exportedDbPath = Path.Combine(Path.GetTempPath(), $"dwm_gate_world_{runId}.db");
        }

        public void Dispose()
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(_economyDbPath)) File.Delete(_economyDbPath);
            if (File.Exists(_exportedDbPath)) File.Delete(_exportedDbPath);
        }

        [Fact]
        public void FullEconomyFlow_SeedToTradesToFailureToExport_IsCorrectEndToEnd()
        {
            // ---------------------------------------------------------------
            // Step 1: seed the canonical Day 13 scenario.
            // ---------------------------------------------------------------
            GoldenEconomyScenario.Seed(_economyDbPath);
            _output.WriteLine($"[1] Seeded golden scenario at {_economyDbPath}");

            // ---------------------------------------------------------------
            // Step 2a: settle a trade via TradeSettlementService (Day 6's one-shot path).
            // Hillside buys Textiles from Suburb -- a real seeded Needs/Produces cross-link
            // (economy_schema.sql: hillside Needs textiles, suburb Produces textiles), not an
            // invented pairing.
            // ---------------------------------------------------------------
            var economyRepository = new EconomyRepository(_economyDbPath);
            var settlementService = new TradeSettlementService(economyRepository);

            var directResult = settlementService.SettleTrade(
                fromCommunityId: "hillside", toCommunityId: "suburb",
                amount: 20, resourceId: "textiles", quantity: 20,
                memo: "Phase 1 gate: Hillside buys Textiles from Suburb (direct SettleTrade path)");

            Assert.True(directResult.Success, $"Direct SettleTrade failed: {directResult.Message}");
            Assert.NotNull(directResult.LedgerEntry);
            _output.WriteLine($"[2a] SettleTrade (direct path) succeeded: TransactionId={directResult.LedgerEntry!.TransactionId}");

            // ---------------------------------------------------------------
            // Step 2b: settle a trade via the TradeRequests lifecycle (Day 8's Propose ->
            // Settle path). Mountain buys Grain from Valley -- another real seeded
            // Needs/Produces cross-link (mountain Needs grain, valley Produces grain).
            // ---------------------------------------------------------------
            var tradeRequestRepository = new TradeRequestRepository(_economyDbPath);

            var proposeResult = tradeRequestRepository.ProposeTrade(
                fromCommunityId: "mountain", toCommunityId: "valley",
                amount: 15, resourceId: "grain", quantity: 15,
                memo: "Phase 1 gate: Mountain buys Grain from Valley (propose/settle lifecycle path)");

            Assert.True(proposeResult.Success, $"ProposeTrade failed: {proposeResult.Message}");
            _output.WriteLine($"[2b] ProposeTrade succeeded: RequestId={proposeResult.Request!.RequestId}");

            var lifecycleResult = tradeRequestRepository.SettleProposedTrade(proposeResult.Request!.RequestId);

            Assert.True(lifecycleResult.Success, $"SettleProposedTrade failed: {lifecycleResult.Message}");
            Assert.NotNull(lifecycleResult.LedgerEntry);
            _output.WriteLine($"[2b] SettleProposedTrade succeeded: TransactionId={lifecycleResult.LedgerEntry!.TransactionId}");

            // ---------------------------------------------------------------
            // Step 3: drive City into Cascading Failure via Day 11's scenario script, reused
            // as-is (not reimplemented), and confirm it actually fired IN THIS RUN.
            // ---------------------------------------------------------------
            var cityStatusFromScenario = CityCascadingFailureScenario.Run(_economyDbPath);

            Assert.Equal(CommunityFailureState.CascadingFailure, cityStatusFromScenario.State);
            Assert.Equal(400.0, cityStatusFromScenario.VaultBalance, precision: 9);
            _output.WriteLine($"[3] CityCascadingFailureScenario fired: State={cityStatusFromScenario.State}, Balance={cityStatusFromScenario.VaultBalance}");

            // Re-confirm via a FRESH CommunityFailureStateService instance (not the same
            // object the scenario used internally), matching Day 11's own "read fresh, not
            // cached" discipline.
            var freshFailureCheck = new CommunityFailureStateService(_economyDbPath).GetFailureState("city");
            Assert.Equal(CommunityFailureState.CascadingFailure, freshFailureCheck.State);
            _output.WriteLine($"[3] Re-confirmed fresh: City State={freshFailureCheck.State}");

            // ---------------------------------------------------------------
            // Step 4: network-sum-zero invariant, computed from the SAME read path a real
            // caller would use (EconomyRepository.GetLedgerEntries), not a duplicated raw
            // SQL query -- exercises the actual repository, not a re-implementation of it.
            // ---------------------------------------------------------------
            var allLedgerEntries = economyRepository.GetLedgerEntries();
            Assert.True(allLedgerEntries.Count >= 2, "Expected at least the 2 trades settled above.");

            // Amount is always positive per schema CHECK, so summing it directly proves
            // nothing -- the real invariant is per-community net (inflow - outflow) summed
            // across the network, which must be exactly 0 by construction.
            var perCommunityNet = economyRepository.GetCommunities()
                .Select(c =>
                    allLedgerEntries.Where(e => e.ToCommunityId == c.CommunityId).Sum(e => e.Amount)
                    - allLedgerEntries.Where(e => e.FromCommunityId == c.CommunityId).Sum(e => e.Amount))
                .Sum();

            Assert.Equal(0.0, perCommunityNet, precision: 9);
            _output.WriteLine($"[4] Network-sum-zero invariant holds: {allLedgerEntries.Count} ledger entries, sum={perCommunityNet}");

            // ---------------------------------------------------------------
            // Step 5: export via the Day 12 WorldPackageExporter extension, pointed at THIS
            // run's actual exercised database (not a fresh golden reseed).
            // ---------------------------------------------------------------
            new WorldPackageExporter().WriteEconomySnapshot(_exportedDbPath, _economyDbPath, worldId: "phase1_gate");
            _output.WriteLine($"[5] Exported world package to {_exportedDbPath}");

            // ---------------------------------------------------------------
            // Step 6: open the exported .db with a FRESH read-only connection (simulating
            // what UE's FSQLiteDatabase will eventually do) and confirm the data is
            // ACTUALLY correct, not just present.
            // ---------------------------------------------------------------
            using var readConn = new SqliteConnection(new SqliteConnectionStringBuilder
            {
                DataSource = _exportedDbPath,
                Mode = SqliteOpenMode.ReadOnly
            }.ToString());
            readConn.Open();

            // City's failure state made it into the export correctly.
            using (var cmd = readConn.CreateCommand())
            {
                cmd.CommandText = "SELECT State FROM CommunityFailureStatus WHERE CommunityId = 'city';";
                var state = (string)cmd.ExecuteScalar()!;
                Assert.Equal("CascadingFailure", state);
                _output.WriteLine($"[6] Exported package confirms City.State={state}");
            }

            // Both settled trades from step 2 made it into the exported StoneLedger, with
            // matching amounts.
            AssertExportedLedgerEntryExists(readConn, directResult.LedgerEntry!.TransactionId, 20.0);
            AssertExportedLedgerEntryExists(readConn, lifecycleResult.LedgerEntry!.TransactionId, 15.0);
            _output.WriteLine("[6] Exported package confirms both StoneLedger entries (direct SettleTrade + Propose/Settle lifecycle) with correct amounts.");

            using (var cmd = readConn.CreateCommand())
            {
                cmd.CommandText = "SELECT COUNT(*) FROM StoneLedger;";
                var count = (long)cmd.ExecuteScalar()!;
                Assert.Equal(allLedgerEntries.Count, count);
                _output.WriteLine($"[6] Exported StoneLedger row count ({count}) matches the live economy.db ({allLedgerEntries.Count}).");
            }
        }

        private static void AssertExportedLedgerEntryExists(SqliteConnection conn, string transactionId, double expectedAmount)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Amount FROM StoneLedger WHERE TransactionId = $id;";
            cmd.Parameters.AddWithValue("$id", transactionId);
            var result = cmd.ExecuteScalar();
            Assert.NotNull(result);
            Assert.Equal(expectedAmount, Convert.ToDouble(result), precision: 9);
        }
    }
}
