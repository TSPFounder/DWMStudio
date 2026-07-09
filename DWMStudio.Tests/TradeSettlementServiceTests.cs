// TradeSettlementServiceTests.cs
// Edge-case coverage for TradeSettlementService.SettleTrade. Reuses the EconomyTestDatabase
// fixture from Day 5 (fresh temp db per test, no shared fixture) — same isolation pattern as
// EconomyLedgerInvariantTests.cs / RandomizedEconomyLedgerInvariantTests.cs.
//
// DOMAIN RULE under test: this is a LETS mutual-credit system — a negative community balance
// is normal and expected (Stones are minted at exchange), not an error. See
// TradeThatDrivesBalanceNegative_Succeeds, which exists specifically to prove the service
// does NOT reject on resulting balance.

using System;
using DWM.Shared.Economy;
using Microsoft.Data.Sqlite;
using Xunit;

namespace DWMStudio.Tests
{
    public sealed class TradeSettlementServiceTests
    {
        [Fact]
        public void ValidTrade_WithResourceAndQuantity_Succeeds()
        {
            using var db = new EconomyTestDatabase();
            var service = new TradeSettlementService(new EconomyRepository(db.DbPath));

            var result = service.SettleTrade("hillside", "suburb", 20, "textiles", 20,
                "Hillside buys Textiles from Suburb");

            Assert.True(result.Success);
            Assert.NotNull(result.LedgerEntry);
            Assert.Equal("hillside", result.LedgerEntry!.FromCommunityId);
            Assert.Equal("suburb", result.LedgerEntry.ToCommunityId);
            Assert.Equal(20, result.LedgerEntry.Amount);
            Assert.Equal("textiles", result.LedgerEntry.ResourceId);
            Assert.Equal(20, result.LedgerEntry.Quantity);

            var stored = Assert.Single(new EconomyRepository(db.DbPath).GetLedgerEntries());
            Assert.Equal(result.LedgerEntry, stored);
        }

        [Fact]
        public void ValidTrade_StoneOnlyNoResource_Succeeds()
        {
            using var db = new EconomyTestDatabase();
            var service = new TradeSettlementService(new EconomyRepository(db.DbPath));

            var result = service.SettleTrade("mountain", "city", 15, null, null,
                "Stone-only settlement, no attached resource");

            Assert.True(result.Success);
            Assert.NotNull(result.LedgerEntry);
            Assert.Null(result.LedgerEntry!.ResourceId);
            Assert.Null(result.LedgerEntry.Quantity);

            var stored = Assert.Single(new EconomyRepository(db.DbPath).GetLedgerEntries());
            Assert.Equal(result.LedgerEntry, stored);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-5)]
        public void NonPositiveAmount_IsRejected_NotARawSqlException(double badAmount)
        {
            using var db = new EconomyTestDatabase();
            var service = new TradeSettlementService(new EconomyRepository(db.DbPath));

            var result = service.SettleTrade("mountain", "valley", badAmount, null, null, null);

            Assert.False(result.Success);
            Assert.Equal(TradeRejectionReason.NonPositiveAmount, result.RejectionReason);
            Assert.NotNull(result.Message);
            Assert.Empty(new EconomyRepository(db.DbPath).GetLedgerEntries());
        }

        [Fact]
        public void SelfTrade_IsRejected()
        {
            using var db = new EconomyTestDatabase();
            var service = new TradeSettlementService(new EconomyRepository(db.DbPath));

            var result = service.SettleTrade("mountain", "mountain", 10, null, null, null);

            Assert.False(result.Success);
            Assert.Equal(TradeRejectionReason.SelfTrade, result.RejectionReason);
            Assert.Empty(new EconomyRepository(db.DbPath).GetLedgerEntries());
        }

        [Fact]
        public void NonexistentFromCommunity_IsRejected_NotAnUnhandledForeignKeyException()
        {
            using var db = new EconomyTestDatabase();
            var service = new TradeSettlementService(new EconomyRepository(db.DbPath));

            var result = service.SettleTrade("atlantis", "valley", 10, null, null, null);

            Assert.False(result.Success);
            Assert.Equal(TradeRejectionReason.UnknownFromCommunity, result.RejectionReason);
            Assert.Empty(new EconomyRepository(db.DbPath).GetLedgerEntries());
        }

        [Fact]
        public void NonexistentToCommunity_IsRejected_NotAnUnhandledForeignKeyException()
        {
            using var db = new EconomyTestDatabase();
            var service = new TradeSettlementService(new EconomyRepository(db.DbPath));

            var result = service.SettleTrade("mountain", "atlantis", 10, null, null, null);

            Assert.False(result.Success);
            Assert.Equal(TradeRejectionReason.UnknownToCommunity, result.RejectionReason);
            Assert.Empty(new EconomyRepository(db.DbPath).GetLedgerEntries());
        }

        [Fact]
        public void NonexistentResource_IsRejected_WhenProvided()
        {
            using var db = new EconomyTestDatabase();
            var service = new TradeSettlementService(new EconomyRepository(db.DbPath));

            var result = service.SettleTrade("mountain", "valley", 10, "unobtainium", 5, null);

            Assert.False(result.Success);
            Assert.Equal(TradeRejectionReason.UnknownResource, result.RejectionReason);
            Assert.Empty(new EconomyRepository(db.DbPath).GetLedgerEntries());
        }

        [Fact]
        public void TradeThatDrivesBalanceNegative_Succeeds()
        {
            // Domain rule: Stones are minted at exchange (LETS mutual credit). A community's
            // balance going negative is normal, not an error — this test exists specifically
            // to prove SettleTrade does NOT reject on resulting balance.
            using var db = new EconomyTestDatabase();
            var repository = new EconomyRepository(db.DbPath);
            var service = new TradeSettlementService(repository);

            // Mountain has never received anything yet, so any outgoing payment drives its
            // balance negative immediately.
            var result = service.SettleTrade("mountain", "valley", 500, null, null,
                "Large outgoing payment with no prior inflow — must still succeed");

            Assert.True(result.Success);

            var mountainBalance = BalanceFor(repository, "mountain");
            Assert.True(mountainBalance < 0,
                $"Expected Mountain's balance to be negative after this trade, was {mountainBalance}.");
        }

        [Fact]
        public void TwoSequentialValidTrades_BothSucceed_NetworkSumStaysZero()
        {
            using var db = new EconomyTestDatabase();
            var repository = new EconomyRepository(db.DbPath);
            var service = new TradeSettlementService(repository);

            var first = service.SettleTrade("hillside", "suburb", 20, "textiles", 20,
                "Hillside buys Textiles from Suburb");
            var second = service.SettleTrade("hillside", "suburb", 8, "wool", 8,
                "Hillside sells Wool to Suburb (second, unrelated trade)");

            Assert.True(first.Success);
            Assert.True(second.Success);
            Assert.NotEqual(first.LedgerEntry!.TransactionId, second.LedgerEntry!.TransactionId);

            var entries = repository.GetLedgerEntries();
            Assert.Equal(2, entries.Count);

            Assert.Equal(0.0, NetworkSum(db.DbPath), precision: 9);
        }

        // ------------------------------------------------------------------
        // Balance/invariant helpers — deliberately duplicated per-file rather than shared,
        // matching the existing pattern in EconomyLedgerInvariantTests.cs /
        // RandomizedEconomyLedgerInvariantTests.cs (each test file owns its own small SQL
        // helpers rather than a cross-file shared utility).

        private static double BalanceFor(EconomyRepository repository, string communityId)
        {
            double balance = 0;
            foreach (var entry in repository.GetLedgerEntries(communityId))
            {
                if (entry.ToCommunityId == communityId)
                    balance += entry.Amount;
                if (entry.FromCommunityId == communityId)
                    balance -= entry.Amount;
            }
            return balance;
        }

        private static double NetworkSum(string dbPath)
        {
            using var conn = new SqliteConnection($"Data Source={dbPath}");
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT COALESCE(SUM(bal), 0) FROM (
                  SELECT CommunityId,
                         COALESCE((SELECT SUM(Amount) FROM StoneLedger WHERE ToCommunityId = c.CommunityId), 0)
                       - COALESCE((SELECT SUM(Amount) FROM StoneLedger WHERE FromCommunityId = c.CommunityId), 0)
                         AS bal
                  FROM Communities c
                ) t;";
            var result = cmd.ExecuteScalar();
            return Convert.ToDouble(result);
        }
    }
}
