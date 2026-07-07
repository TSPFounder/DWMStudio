// EconomyRepositoryTests.cs
// Proves the EconomyTestDatabase fixture works and that EconomyRepository reads back what
// EconomySeeder wrote. Infrastructure-proving tests for Day 6, not new invariant coverage —
// see EconomyLedgerInvariantTests.cs / RandomizedEconomyLedgerInvariantTests.cs for that.

using System.Linq;
using DWM.Shared.Economy;
using Xunit;

namespace DWMStudio.Tests
{
    public sealed class EconomyRepositoryTests
    {
        [Fact]
        public void GetCommunities_And_GetCommunityResources_ReturnSeededData()
        {
            using var db = new EconomyTestDatabase();
            var repo = new EconomyRepository(db.DbPath);

            var communities = repo.GetCommunities();
            Assert.Equal(5, communities.Count);

            var mountain = Assert.Single(communities, c => c.CommunityId == "mountain");
            Assert.Equal("Mountain", mountain.Name);
            Assert.Equal("Mountain", mountain.BiomeType);

            var mountainResources = repo.GetCommunityResources("mountain");
            Assert.Contains(mountainResources,
                cr => cr.ResourceId == "timber" && cr.Role == CommunityResourceRole.Produces && cr.Quantity == 100);
            Assert.Contains(mountainResources,
                cr => cr.ResourceId == "grain" && cr.Role == CommunityResourceRole.Needs && cr.Quantity == 20);
        }

        [Fact]
        public void GetDollarVaultBalance_ReturnsSeededBalance()
        {
            using var db = new EconomyTestDatabase();
            var repo = new EconomyRepository(db.DbPath);

            // Seeder writes a single seed-001 entry with DeltaAmount 5000.
            Assert.Equal(5000.0, repo.GetDollarVaultBalance(), precision: 9);
        }

        [Fact]
        public void AppendLedgerEntry_IsReadableAfterwardsAndOnlyForInvolvedCommunities()
        {
            using var db = new EconomyTestDatabase();
            var repo = new EconomyRepository(db.DbPath);

            Assert.Empty(repo.GetLedgerEntries());

            var written = repo.AppendLedgerEntry(
                fromCommunityId: "hillside",
                toCommunityId: "suburb",
                amount: 20,
                resourceId: "textiles",
                quantity: 20,
                memo: "Hillside buys Textiles from Suburb");

            var all = repo.GetLedgerEntries();
            var forSuburb = repo.GetLedgerEntries("suburb");
            var forCity = repo.GetLedgerEntries("city");

            Assert.Single(all);
            Assert.Equal(written, Assert.Single(all));
            Assert.Single(forSuburb);
            Assert.Empty(forCity);
        }
    }
}
