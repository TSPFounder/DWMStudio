// CommunityFailureStateServiceTests.cs
// Day 11: failure_state trigger (CommunityFailureStateService, layered on Day 10's
// DollarVaultRepository) + the City cascading-failure scenario script. Reuses the
// EconomyTestDatabase fixture from Day 5, same as DollarVaultRepositoryTests.

using DWM.Shared.Economy;
using Xunit;

namespace DWMStudio.Tests
{
    public sealed class CommunityFailureStateServiceTests
    {
        [Fact]
        public void SeededCommunity_IsHealthy()
        {
            using var db = new EconomyTestDatabase();
            var failureState = new CommunityFailureStateService(db.DbPath);

            var status = failureState.GetFailureState("mountain");

            Assert.Equal(CommunityFailureState.Healthy, status.State);
            Assert.Equal(5000.0, status.VaultBalance, precision: 9);
            Assert.Equal(500.0, status.VaultThreshold, precision: 9);
        }

        [Fact]
        public void DebitLeavingBalanceExactlyAtThreshold_IsCascadingFailure()
        {
            using var db = new EconomyTestDatabase();
            var vault = new DollarVaultRepository(db.DbPath);
            var failureState = new CommunityFailureStateService(db.DbPath);

            // 5000 - 4500 = 500, exactly the threshold.
            vault.DebitVault("suburb", 4500, "Debit leaving balance exactly at threshold");

            var status = failureState.GetFailureState("suburb");
            Assert.Equal(CommunityFailureState.CascadingFailure, status.State);
            Assert.Equal(500.0, status.VaultBalance, precision: 9);
        }

        [Fact]
        public void DebitLeavingBalanceOneUnitAboveThreshold_IsHealthy()
        {
            using var db = new EconomyTestDatabase();
            var vault = new DollarVaultRepository(db.DbPath);
            var failureState = new CommunityFailureStateService(db.DbPath);

            // 5000 - 4499 = 501, one unit above the 500 threshold.
            vault.DebitVault("suburb", 4499, "Debit leaving balance one unit above threshold");

            var status = failureState.GetFailureState("suburb");
            Assert.Equal(CommunityFailureState.Healthy, status.State);
            Assert.Equal(501.0, status.VaultBalance, precision: 9);
        }

        [Fact]
        public void DebitLeavingBalanceOneUnitBelowThreshold_IsCascadingFailure()
        {
            using var db = new EconomyTestDatabase();
            var vault = new DollarVaultRepository(db.DbPath);
            var failureState = new CommunityFailureStateService(db.DbPath);

            // 5000 - 4501 = 499, one unit below the 500 threshold.
            vault.DebitVault("suburb", 4501, "Debit leaving balance one unit below threshold");

            var status = failureState.GetFailureState("suburb");
            Assert.Equal(CommunityFailureState.CascadingFailure, status.State);
            Assert.Equal(499.0, status.VaultBalance, precision: 9);
        }

        [Fact]
        public void CommunityAlreadyInFailureState_WhenQueriedFresh_ReportsCascadingFailure()
        {
            using var db = new EconomyTestDatabase();

            // Push suburb into failure using one service/repository instance...
            var vault = new DollarVaultRepository(db.DbPath);
            vault.DebitVault("suburb", 4600, "Drives suburb into cascading failure ahead of the query");

            // ...then read its state from a BRAND NEW CommunityFailureStateService instance,
            // proving the trigger is computed fresh from persisted data every call, not cached
            // on some prior in-memory state.
            var freshFailureState = new CommunityFailureStateService(db.DbPath);
            var status = freshFailureState.GetFailureState("suburb");

            Assert.Equal(CommunityFailureState.CascadingFailure, status.State);
            Assert.Equal(400.0, status.VaultBalance, precision: 9);
        }

        [Fact]
        public void CommunityTransitionsFromHealthyToCascadingFailureViaADebit()
        {
            using var db = new EconomyTestDatabase();
            var vault = new DollarVaultRepository(db.DbPath);
            var failureState = new CommunityFailureStateService(db.DbPath);

            Assert.Equal(CommunityFailureState.Healthy, failureState.GetFailureState("suburb").State);

            vault.DebitVault("suburb", 4600, "Drives suburb into cascading failure");

            Assert.Equal(CommunityFailureState.CascadingFailure, failureState.GetFailureState("suburb").State);
        }

        [Fact]
        public void CityScenario_ProducesCascadingFailureAtExpectedBalance()
        {
            using var db = new EconomyTestDatabase();

            var status = CityCascadingFailureScenario.Run(db.DbPath);

            Assert.Equal("city", status.CommunityId);
            Assert.Equal(CommunityFailureState.CascadingFailure, status.State);
            Assert.Equal(400.0, status.VaultBalance, precision: 9);
        }

        [Fact]
        public void CityScenario_DoesNotAffectAnyOtherCommunitysVaultState()
        {
            using var db = new EconomyTestDatabase();
            var failureState = new CommunityFailureStateService(db.DbPath);

            CityCascadingFailureScenario.Run(db.DbPath);

            foreach (var otherCommunityId in new[] { "mountain", "hillside", "valley", "suburb" })
            {
                var status = failureState.GetFailureState(otherCommunityId);
                Assert.Equal(CommunityFailureState.Healthy, status.State);
                Assert.Equal(5000.0, status.VaultBalance, precision: 9);
            }
        }

        [Fact]
        public void CityScenario_IsDeterministicAcrossIndependentRuns()
        {
            using var dbA = new EconomyTestDatabase();
            using var dbB = new EconomyTestDatabase();

            var statusA = CityCascadingFailureScenario.Run(dbA.DbPath);
            var statusB = CityCascadingFailureScenario.Run(dbB.DbPath);

            Assert.Equal(statusA.State, statusB.State);
            Assert.Equal(statusA.VaultBalance, statusB.VaultBalance, precision: 9);
            Assert.Equal(CommunityFailureState.CascadingFailure, statusA.State);
        }
    }
}
