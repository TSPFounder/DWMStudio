// EngineeringServicesResourceTests.cs
// Covers the engineering_services resource added 2026-08-01, when Hillside's MVP trade moved
// from Timber to engineering services (CAD drawings + Simulink model). See SCOPE.md
// 2026-07-18 for the trade change and 2026-08-01 for the revised ending that leans on it.
//
// WHY THESE TESTS EXIST RATHER THAN JUST TRUSTING THE SEEDER LINE:
//
// StoneLedger.ResourceId is a FOREIGN KEY into Resources(ResourceId), and PRAGMA
// foreign_keys is ON throughout the economy layer. So a resource id that is accepted by
// UE-side validation but absent from the Resources table does not merely mis-label a trade
// -- the INSERT fails outright. The seeder row and the trade path have to be verified
// together, which is what SettleTrade_EngineeringServices_Succeeds does.
//
// TimberStillSeeded_NotRemoved guards the other direction: Timber is DEFERRED to post-MVP,
// not deleted, and a future edit that "cleans up" the unused resource would silently break
// any saved world that still references it.

using System.Linq;
using DWM.Shared.Economy;
using Xunit;

namespace DWMStudio.Tests
{
    public sealed class EngineeringServicesResourceTests
    {
        [Fact]
        public void EngineeringServices_IsSeeded()
        {
            using var db = new EconomyTestDatabase();
            var resources = new EconomyRepository(db.DbPath).GetResources();

            var eng = Assert.Single(
                resources.Where(r => r.ResourceId == "engineering_services"));

            Assert.Equal("Engineering Services", eng.Name);
            Assert.Equal("hour", eng.Unit);
            Assert.Equal("labor", eng.Category);
        }

        [Fact]
        public void SettleTrade_EngineeringServices_Succeeds()
        {
            // The trade the MVP storyline actually performs at Act 2, Stop 1: Mountain pays
            // Stone, Hillside provides the engineering package.
            using var db = new EconomyTestDatabase();
            var service = new TradeSettlementService(new EconomyRepository(db.DbPath));

            var result = service.SettleTrade(
                "mountain", "hillside", 20, "engineering_services", 20,
                "Mountain buys engineering services from Hillside");

            Assert.True(result.Success, result.Message);
            Assert.NotNull(result.LedgerEntry);
            Assert.Equal("mountain", result.LedgerEntry!.FromCommunityId);
            Assert.Equal("hillside", result.LedgerEntry.ToCommunityId);
            Assert.Equal("engineering_services", result.LedgerEntry.ResourceId);

            // Round-trip it: proves the row survived the foreign-key check on the way in,
            // which is the failure mode this whole change is about.
            var stored = Assert.Single(new EconomyRepository(db.DbPath).GetLedgerEntries());
            Assert.Equal(result.LedgerEntry, stored);
        }

        [Fact]
        public void SettleTrade_UnknownResource_StillRejected()
        {
            // Adding a resource must not weaken validation for everything else.
            using var db = new EconomyTestDatabase();
            var service = new TradeSettlementService(new EconomyRepository(db.DbPath));

            var result = service.SettleTrade(
                "mountain", "hillside", 20, "engineering_servcies", 20,   // deliberate typo
                "Typo in the resource id");

            Assert.False(result.Success);
        }

        [Fact]
        public void CommunityResources_HillsideProduces_MountainNeeds()
        {
            using var db = new EconomyTestDatabase();
            var repo = new EconomyRepository(db.DbPath);

            // GetCommunityResources takes a community id, so query each side separately.
            Assert.Contains(repo.GetCommunityResources("hillside"),
                cr => cr.ResourceId == "engineering_services"
                      && cr.Role == CommunityResourceRole.Produces);

            Assert.Contains(repo.GetCommunityResources("mountain"),
                cr => cr.ResourceId == "engineering_services"
                      && cr.Role == CommunityResourceRole.Needs);
        }

        [Fact]
        public void TimberStillSeeded_NotRemoved()
        {
            // Timber is deferred to post-MVP, NOT cut. Storyline doc is explicit that the
            // sawmill and the timber trade still exist in the world.
            using var db = new EconomyTestDatabase();
            var resources = new EconomyRepository(db.DbPath).GetResources();

            Assert.Contains(resources, r => r.ResourceId == "timber");
        }
    }
}
