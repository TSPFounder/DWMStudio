// DollarVaultRepositoryTests.cs
// Day 10: per-community Dollar Vault (DollarVaultRepository), layered on top of the original
// global DollarVaultLedger/DollarVaultConfig (economy_schema.sql), which this does not touch
// or exercise. Reuses the EconomyTestDatabase fixture from Day 5 -- seeding for these tests
// comes for free from DollarVaultPerCommunityMigration's own seed data (5 communities,
// $5000 opening balance, $500 threshold each), applied automatically when
// DollarVaultRepository is constructed.

using DWM.Shared.Economy;
using Xunit;

namespace DWMStudio.Tests
{
    public sealed class DollarVaultRepositoryTests
    {
        private static readonly string[] AllCommunityIds =
            { "mountain", "hillside", "valley", "suburb", "city" };

        [Fact]
        public void SeededCommunities_HaveExpectedOpeningBalanceAndThreshold()
        {
            using var db = new EconomyTestDatabase();
            var vault = new DollarVaultRepository(db.DbPath);

            foreach (var communityId in AllCommunityIds)
            {
                Assert.Equal(5000.0, vault.GetVaultBalance(communityId), precision: 9);
                Assert.Equal(500.0, vault.GetVaultThreshold(communityId), precision: 9);
                Assert.False(vault.IsInCascadingFailure(communityId));
            }
        }

        [Fact]
        public void DebitVault_DecreasesBalanceByExactlyTheDebitedAmount()
        {
            using var db = new EconomyTestDatabase();
            var vault = new DollarVaultRepository(db.DbPath);

            var entry = vault.DebitVault("mountain", 1200, "Outside purchase placeholder");

            Assert.Equal("mountain", entry.CommunityId);
            Assert.Equal(-1200, entry.DeltaAmount);
            Assert.Equal(3800.0, vault.GetVaultBalance("mountain"), precision: 9);
        }

        [Fact]
        public void DebitVault_ThatCrossesThreshold_FlipsIsInCascadingFailure()
        {
            using var db = new EconomyTestDatabase();
            var vault = new DollarVaultRepository(db.DbPath);

            // Opening balance 5000, threshold 500 -- debiting 4600 leaves 400, below threshold.
            Assert.False(vault.IsInCascadingFailure("city"));

            vault.DebitVault("city", 4600, "Large outside purchase crossing the failure threshold");

            Assert.True(vault.IsInCascadingFailure("city"));
            Assert.Equal(400.0, vault.GetVaultBalance("city"), precision: 9);
        }

        [Fact]
        public void CommunityCrossingThreshold_DoesNotAffectAnotherCommunitysState()
        {
            using var db = new EconomyTestDatabase();
            var vault = new DollarVaultRepository(db.DbPath);

            vault.DebitVault("city", 4600, "Drives City into cascading failure");

            Assert.True(vault.IsInCascadingFailure("city"));

            // Every other community's balance and failure state must be completely
            // unaffected -- this is the core per-community isolation Day 11 needs.
            foreach (var otherCommunityId in new[] { "mountain", "hillside", "valley", "suburb" })
            {
                Assert.Equal(5000.0, vault.GetVaultBalance(otherCommunityId), precision: 9);
                Assert.False(vault.IsInCascadingFailure(otherCommunityId));
            }
        }

        [Fact]
        public void IsInCascadingFailure_WhenBalanceExactlyAtThreshold_IsTrue()
        {
            using var db = new EconomyTestDatabase();
            var vault = new DollarVaultRepository(db.DbPath);

            // Debit exactly 4500 leaves exactly 500 -- the threshold itself.
            vault.DebitVault("suburb", 4500, "Debit leaving balance exactly at threshold");

            Assert.Equal(500.0, vault.GetVaultBalance("suburb"), precision: 9);
            Assert.True(vault.IsInCascadingFailure("suburb"),
                "Balance exactly at threshold must count as cascading failure (<=, not <).");
        }

        [Fact]
        public void IsInCascadingFailure_WhenBalanceOneUnitAboveThreshold_IsFalse()
        {
            using var db = new EconomyTestDatabase();
            var vault = new DollarVaultRepository(db.DbPath);

            // Debit 4499 leaves 501 -- one unit above the 500 threshold.
            vault.DebitVault("suburb", 4499, "Debit leaving balance one unit above threshold");

            Assert.Equal(501.0, vault.GetVaultBalance("suburb"), precision: 9);
            Assert.False(vault.IsInCascadingFailure("suburb"));
        }

        [Fact]
        public void IsInCascadingFailure_WhenBalanceOneUnitBelowThreshold_IsTrue()
        {
            using var db = new EconomyTestDatabase();
            var vault = new DollarVaultRepository(db.DbPath);

            // Debit 4501 leaves 499 -- one unit below the 500 threshold.
            vault.DebitVault("suburb", 4501, "Debit leaving balance one unit below threshold");

            Assert.Equal(499.0, vault.GetVaultBalance("suburb"), precision: 9);
            Assert.True(vault.IsInCascadingFailure("suburb"));
        }

        [Fact]
        public void DebitVault_WithNonPositiveAmount_Throws()
        {
            using var db = new EconomyTestDatabase();
            var vault = new DollarVaultRepository(db.DbPath);

            Assert.Throws<System.ArgumentOutOfRangeException>(() => vault.DebitVault("mountain", 0, "zero"));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => vault.DebitVault("mountain", -50, "negative"));

            // Neither rejected call should have touched the balance.
            Assert.Equal(5000.0, vault.GetVaultBalance("mountain"), precision: 9);
        }
    }
}
