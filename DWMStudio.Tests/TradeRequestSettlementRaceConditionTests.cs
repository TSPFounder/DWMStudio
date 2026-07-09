// TradeRequestSettlementRaceConditionTests.cs
// Proves the compare-and-swap fix in TradeRequestRepository.SettleProposedTrade actually
// closes the double-commit gap: a crash between the StoneLedger write and the Settled status
// update used to leave a request looking Proposed, so a retry would double-append. The fix
// introduces a transient Settling status, claimed via an atomic
// "UPDATE ... WHERE Status = 'Proposed'" immediately before the ledger write.
//
// Reuses the EconomyTestDatabase fixture from Day 5. Does not modify
// TradeRequestRepositoryTests.cs — this is new, additional coverage for the race-condition
// fix specifically, not a replacement for the Day 8 lifecycle tests.

using System;
using System.Threading.Tasks;
using DWM.Shared.Economy;
using Microsoft.Data.Sqlite;
using Xunit;

namespace DWMStudio.Tests
{
    public sealed class TradeRequestSettlementRaceConditionTests
    {
        [Fact]
        public void SettlingAStuckSettlingRequest_IsRejectedCleanly_NoDoubleCommit()
        {
            using var db = new EconomyTestDatabase();
            var tradeRequests = new TradeRequestRepository(db.DbPath);
            var economy = new EconomyRepository(db.DbPath);

            var propose = tradeRequests.ProposeTrade("mountain", "valley", 40, "grain", 4,
                "Will be forced into the post-crash Settling state");
            Assert.True(propose.Success);
            var requestId = propose.Request!.RequestId;

            // Reconstruct the exact crash state WITHOUT going through the normal
            // SettleProposedTrade flow: the ledger write happened (AppendLedgerEntry
            // succeeded and durably committed), but the process died before the status
            // update that would have marked the request Settled — so it's stuck at
            // Settling, which is exactly what the compare-and-swap claim sets it to right
            // before the ledger write.
            var crashLedgerEntry = economy.AppendLedgerEntry(
                "mountain", "valley", 40, "grain", 4, propose.Request.Memo);
            ForceStatus(db.DbPath, requestId, "Settling");

            Assert.Single(economy.GetLedgerEntries());

            var retryResult = tradeRequests.SettleProposedTrade(requestId);

            Assert.False(retryResult.Success);
            Assert.Equal(TradeRequestFailureReason.RequestNotProposed, retryResult.FailureReason);
            Assert.NotNull(retryResult.Message);

            // The actual proof: still exactly the one ledger row from the simulated crash —
            // the retry did NOT append a second one.
            var storedEntries = economy.GetLedgerEntries();
            Assert.Single(storedEntries);
            Assert.Equal(crashLedgerEntry, storedEntries[0]);

            // And the request itself is untouched by the rejected retry — still Settling,
            // not silently flipped to Settled or anything else.
            var storedRequest = tradeRequests.GetTradeRequest(requestId);
            Assert.Equal(TradeRequestStatus.Settling, storedRequest!.Status);
        }

        [Fact]
        public async Task ConcurrentSettleAttempts_OnSameRequest_ExactlyOneSucceeds()
        {
            using var db = new EconomyTestDatabase();
            var economy = new EconomyRepository(db.DbPath);
            var proposer = new TradeRequestRepository(db.DbPath);

            var propose = proposer.ProposeTrade("suburb", "city", 18, "skilled_labor", 6,
                "Contended by two near-simultaneous settle attempts");
            Assert.True(propose.Success);
            var requestId = propose.Request!.RequestId;

            // Two independent repository instances (independent connections), same
            // requestId, fired as close to simultaneously as Task.Run allows — this is what
            // actually exercises the compare-and-swap's cross-connection safety, not just
            // in-process sequencing.
            var attempt1 = Task.Run(() => new TradeRequestRepository(db.DbPath).SettleProposedTrade(requestId));
            var attempt2 = Task.Run(() => new TradeRequestRepository(db.DbPath).SettleProposedTrade(requestId));

            var results = await Task.WhenAll(attempt1, attempt2);

            var successes = Array.FindAll(results, r => r.Success);
            var rejections = Array.FindAll(results, r => !r.Success);

            Assert.Single(successes);
            Assert.Single(rejections);
            Assert.Equal(TradeRequestFailureReason.RequestNotProposed, rejections[0].FailureReason);

            // Exactly one StoneLedger row for the contended request, not zero, not two.
            Assert.Single(economy.GetLedgerEntries());

            var storedRequest = proposer.GetTradeRequest(requestId);
            Assert.Equal(TradeRequestStatus.Settled, storedRequest!.Status);
        }

        // ------------------------------------------------------------------
        private static void ForceStatus(string dbPath, string requestId, string status)
        {
            using var conn = new SqliteConnection($"Data Source={dbPath}");
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE TradeRequests SET Status = $status WHERE RequestId = $id;";
            cmd.Parameters.AddWithValue("$status", status);
            cmd.Parameters.AddWithValue("$id", requestId);
            cmd.ExecuteNonQuery();
        }
    }
}
