// TradeRequestRepositoryTests.cs
// Day 8: trade lifecycle (Proposed -> Settled/Cancelled) via TradeRequestRepository.
// Reuses the EconomyTestDatabase fixture from Day 5 — same per-test fresh-db isolation
// pattern as every other test file in this project.

using System;
using DWM.Shared.Economy;
using Microsoft.Data.Sqlite;
using Xunit;

namespace DWMStudio.Tests
{
    public sealed class TradeRequestRepositoryTests
    {
        [Fact]
        public void ProposeThenSettle_HappyPath_WritesExactlyOneLedgerRow()
        {
            using var db = new EconomyTestDatabase();
            var tradeRequests = new TradeRequestRepository(db.DbPath);
            var economy = new EconomyRepository(db.DbPath);

            var proposeResult = tradeRequests.ProposeTrade("hillside", "suburb", 20, "textiles", 20,
                "Hillside buys Textiles from Suburb");
            Assert.True(proposeResult.Success);
            Assert.Equal(TradeRequestStatus.Proposed, proposeResult.Request!.Status);
            Assert.Null(proposeResult.Request.ResolvedAt);
            Assert.Empty(economy.GetLedgerEntries());

            var settleResult = tradeRequests.SettleProposedTrade(proposeResult.Request.RequestId);

            Assert.True(settleResult.Success);
            Assert.Equal(TradeRequestStatus.Settled, settleResult.Request!.Status);
            Assert.NotNull(settleResult.Request.ResolvedAt);
            Assert.NotNull(settleResult.LedgerEntry);
            Assert.Equal("hillside", settleResult.LedgerEntry!.FromCommunityId);
            Assert.Equal("suburb", settleResult.LedgerEntry.ToCommunityId);

            var storedLedgerEntries = economy.GetLedgerEntries();
            Assert.Single(storedLedgerEntries);
            Assert.Equal(settleResult.LedgerEntry, storedLedgerEntries[0]);

            var storedRequest = tradeRequests.GetTradeRequest(proposeResult.Request.RequestId);
            Assert.Equal(TradeRequestStatus.Settled, storedRequest!.Status);
        }

        [Fact]
        public void ProposeThenCancel_WritesZeroLedgerRows()
        {
            using var db = new EconomyTestDatabase();
            var tradeRequests = new TradeRequestRepository(db.DbPath);
            var economy = new EconomyRepository(db.DbPath);

            var proposeResult = tradeRequests.ProposeTrade("mountain", "city", 10, null, null,
                "Stone-only trade, will be cancelled");
            Assert.True(proposeResult.Success);

            var cancelResult = tradeRequests.CancelProposedTrade(proposeResult.Request!.RequestId);

            Assert.True(cancelResult.Success);
            Assert.Equal(TradeRequestStatus.Cancelled, cancelResult.Request!.Status);
            Assert.NotNull(cancelResult.Request.ResolvedAt);
            Assert.Null(cancelResult.LedgerEntry);

            Assert.Empty(economy.GetLedgerEntries());

            var storedRequest = tradeRequests.GetTradeRequest(proposeResult.Request.RequestId);
            Assert.Equal(TradeRequestStatus.Cancelled, storedRequest!.Status);
        }

        [Fact]
        public void SettlingAnAlreadySettledRequest_IsRejectedCleanly_NoDoubleCommit()
        {
            using var db = new EconomyTestDatabase();
            var tradeRequests = new TradeRequestRepository(db.DbPath);
            var economy = new EconomyRepository(db.DbPath);

            var propose = tradeRequests.ProposeTrade("valley", "suburb", 12, null, null, "first settle");
            var firstSettle = tradeRequests.SettleProposedTrade(propose.Request!.RequestId);
            Assert.True(firstSettle.Success);
            Assert.Single(economy.GetLedgerEntries());

            var secondSettle = tradeRequests.SettleProposedTrade(propose.Request.RequestId);

            Assert.False(secondSettle.Success);
            Assert.Equal(TradeRequestFailureReason.RequestNotProposed, secondSettle.FailureReason);
            Assert.NotNull(secondSettle.Message);

            // No double-commit: still exactly one StoneLedger row from the first settle.
            Assert.Single(economy.GetLedgerEntries());
        }

        [Fact]
        public void SettlingAnAlreadyCancelledRequest_IsRejectedCleanly_DoesNotResurrectIt()
        {
            using var db = new EconomyTestDatabase();
            var tradeRequests = new TradeRequestRepository(db.DbPath);
            var economy = new EconomyRepository(db.DbPath);

            var propose = tradeRequests.ProposeTrade("valley", "suburb", 12, null, null, "will be cancelled");
            var cancel = tradeRequests.CancelProposedTrade(propose.Request!.RequestId);
            Assert.True(cancel.Success);

            var settleAttempt = tradeRequests.SettleProposedTrade(propose.Request.RequestId);

            Assert.False(settleAttempt.Success);
            Assert.Equal(TradeRequestFailureReason.RequestNotProposed, settleAttempt.FailureReason);

            // The cancelled request must not be resurrected into a settlement.
            Assert.Empty(economy.GetLedgerEntries());
            var storedRequest = tradeRequests.GetTradeRequest(propose.Request.RequestId);
            Assert.Equal(TradeRequestStatus.Cancelled, storedRequest!.Status);
        }

        [Fact]
        public void SettlingAProposedTrade_ThatWouldNowFailValidation_IsRejectedCleanly_RequestStaysProposed()
        {
            using var db = new EconomyTestDatabase();
            var tradeRequests = new TradeRequestRepository(db.DbPath);
            var economy = new EconomyRepository(db.DbPath);

            var propose = tradeRequests.ProposeTrade("mountain", "valley", 30, null, null,
                "Valid when proposed; Mountain will be removed before settling");
            Assert.True(propose.Success);

            // Simulate the "a community was somehow removed" scenario the task calls out:
            // delete Mountain from Communities directly, bypassing TradeRequestRepository
            // (which has no delete-community method — nothing in this domain's public API
            // can currently cause this) via a connection that does NOT enable
            // PRAGMA foreign_keys, so the FK constraints declared on TradeRequests/
            // CommunityResources don't block the delete. This proves SettleProposedTrade
            // re-validates against CURRENT state rather than trusting the stored proposal.
            DeleteCommunityBypassingForeignKeys(db.DbPath, "mountain");

            var settleAttempt = tradeRequests.SettleProposedTrade(propose.Request!.RequestId);

            Assert.False(settleAttempt.Success);
            Assert.Equal(TradeRequestFailureReason.UnknownFromCommunity, settleAttempt.FailureReason);
            Assert.NotNull(settleAttempt.Message);

            Assert.Empty(economy.GetLedgerEntries());

            var storedRequest = tradeRequests.GetTradeRequest(propose.Request.RequestId);
            Assert.Equal(TradeRequestStatus.Proposed, storedRequest!.Status);
            Assert.Null(storedRequest.ResolvedAt);
        }

        // ------------------------------------------------------------------
        // Day 15 gap: ProposeTrade re-implements the same six structural validations
        // TradeSettlementService.SettleTrade already has coverage for (TradeSettlementServiceTests.cs),
        // but ProposeTrade itself was never tested against those cases directly -- the tests
        // above all start from an already-successful ProposeTrade call. Each of these asserts
        // both the rejection reason AND that no TradeRequests row was written at all.

        [Theory]
        [InlineData(0)]
        [InlineData(-5)]
        public void ProposeTrade_WithNonPositiveAmount_IsRejected_NoRowWritten(double badAmount)
        {
            using var db = new EconomyTestDatabase();
            var tradeRequests = new TradeRequestRepository(db.DbPath);

            var result = tradeRequests.ProposeTrade("mountain", "valley", badAmount, null, null, null);

            Assert.False(result.Success);
            Assert.Equal(TradeRequestFailureReason.NonPositiveAmount, result.FailureReason);
            Assert.NotNull(result.Message);
            Assert.Empty(tradeRequests.GetTradeRequests());
        }

        [Fact]
        public void ProposeTrade_SelfTrade_IsRejected_NoRowWritten()
        {
            using var db = new EconomyTestDatabase();
            var tradeRequests = new TradeRequestRepository(db.DbPath);

            var result = tradeRequests.ProposeTrade("mountain", "mountain", 10, null, null, null);

            Assert.False(result.Success);
            Assert.Equal(TradeRequestFailureReason.SelfTrade, result.FailureReason);
            Assert.Empty(tradeRequests.GetTradeRequests());
        }

        [Fact]
        public void ProposeTrade_UnknownFromCommunity_IsRejected_NoRowWritten()
        {
            using var db = new EconomyTestDatabase();
            var tradeRequests = new TradeRequestRepository(db.DbPath);

            var result = tradeRequests.ProposeTrade("atlantis", "valley", 10, null, null, null);

            Assert.False(result.Success);
            Assert.Equal(TradeRequestFailureReason.UnknownFromCommunity, result.FailureReason);
            Assert.Empty(tradeRequests.GetTradeRequests());
        }

        [Fact]
        public void ProposeTrade_UnknownToCommunity_IsRejected_NoRowWritten()
        {
            using var db = new EconomyTestDatabase();
            var tradeRequests = new TradeRequestRepository(db.DbPath);

            var result = tradeRequests.ProposeTrade("mountain", "atlantis", 10, null, null, null);

            Assert.False(result.Success);
            Assert.Equal(TradeRequestFailureReason.UnknownToCommunity, result.FailureReason);
            Assert.Empty(tradeRequests.GetTradeRequests());
        }

        [Fact]
        public void ProposeTrade_UnknownResource_IsRejected_NoRowWritten()
        {
            using var db = new EconomyTestDatabase();
            var tradeRequests = new TradeRequestRepository(db.DbPath);

            var result = tradeRequests.ProposeTrade("mountain", "valley", 10, "unobtainium", 5, null);

            Assert.False(result.Success);
            Assert.Equal(TradeRequestFailureReason.UnknownResource, result.FailureReason);
            Assert.Empty(tradeRequests.GetTradeRequests());
        }

        [Fact]
        public void ProposeTrade_NonPositiveQuantityWithResource_IsRejected_NoRowWritten()
        {
            using var db = new EconomyTestDatabase();
            var tradeRequests = new TradeRequestRepository(db.DbPath);

            var result = tradeRequests.ProposeTrade("mountain", "valley", 10, "grain", 0, null);

            Assert.False(result.Success);
            Assert.Equal(TradeRequestFailureReason.NonPositiveQuantity, result.FailureReason);
            Assert.Empty(tradeRequests.GetTradeRequests());
        }

        // ------------------------------------------------------------------
        private static void DeleteCommunityBypassingForeignKeys(string dbPath, string communityId)
        {
            using var conn = new SqliteConnection($"Data Source={dbPath}");
            conn.Open();
            // Explicitly OFF (this SQLite build enforces foreign keys by default, unlike the
            // classic SQLite default) — this connection needs to delete a Communities row
            // that other rows still reference (CommunityResources seed rows, and the
            // TradeRequests row under test), which SQLite would otherwise block.
            using (var pragma = conn.CreateCommand())
            {
                pragma.CommandText = "PRAGMA foreign_keys = OFF;";
                pragma.ExecuteNonQuery();
            }
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM Communities WHERE CommunityId = $id;";
            cmd.Parameters.AddWithValue("$id", communityId);
            cmd.ExecuteNonQuery();
        }
    }
}
