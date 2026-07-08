// RandomizedTradeSettlementAtScaleTests.cs
// Week 2 / Day 7 target: 100,000 randomized trades through TradeSettlementService (the real
// caller-facing code path, not raw StoneLedger inserts like RandomizedEconomyLedgerInvariantTests.cs
// used), plus three adversarial cases not yet covered: zero-quantity rejection, negative-amount
// rejection, and concurrent writes. Reuses the EconomyTestDatabase fixture from Day 5 — same
// per-test fresh-db isolation pattern as every other test file in this project.
//
// CONCURRENCY NOTE (read before changing the concurrent test): ARCH.md documents a deliberate
// decision to run economy.db in SQLite's default rollback-journal (DELETE) mode, not WAL, with
// correctness enforced by single-writer *process* discipline rather than SQLite's concurrency
// features. EconomyRepository.OpenConnection() sets no busy_timeout override. That means this
// suite's concurrent-writes test is deliberately probing a scenario the architecture was not
// designed to guarantee lock-free throughput for — see ConcurrentSettleTrade_ProducesConsistentLedger
// for what it actually asserts (no corruption among whatever commits) versus what it does NOT
// assert (that every concurrent attempt succeeds without contention).

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DWM.Shared.Economy;
using Microsoft.Data.Sqlite;
using Xunit;

namespace DWMStudio.Tests
{
    public sealed class RandomizedTradeSettlementAtScaleTests
    {
        // Same fixed seed as RandomizedEconomyLedgerInvariantTests.cs, for the same reason:
        // reproducible, re-runnable failures, not flaky randomness.
        private const int RandomSeed = 42;
        private const int TradeCount = 100_000;

        // Checking the network-sum invariant after every one of 100,000 trades would be
        // O(n^2) (each check re-aggregates the whole StoneLedger table) and make this test
        // impractically slow. Instead: check every CheckpointInterval trades (cheap, O(n) total
        // across all checkpoints), and if a checkpoint ever fails, bisect within just that
        // window to name the exact offending trade — see BisectFailingTrade.
        private const int CheckpointInterval = 5_000;

        private static readonly string[] CommunityIds =
            { "mountain", "hillside", "valley", "suburb", "city" };

        private static readonly string[] ResourceIds =
        {
            "timber", "wind_power", "orchard_fruit", "wool", "grain",
            "water", "skilled_labor", "textiles", "manufactured_tools", "software_services"
        };

        // ~8-9 minutes by itself — excluded from the default `dotnet test` run via
        // DWMStudio.Tests.csproj's VSTestTestCaseFilter (Category!=Slow). Run it
        // deliberately with: dotnet test --filter "Category=Slow"
        [Fact]
        [Trait("Category", "Slow")]
        public void Invariant_HoldsAcross100000TradesThroughSettlementService()
        {
            using var db = new EconomyTestDatabase();
            var repository = new EconomyRepository(db.DbPath);
            var service = new TradeSettlementService(repository);
            var rng = new Random(RandomSeed);

            var trades = new List<(int Index, string From, string To, double Amount, string? ResourceId, double? Quantity)>();

            for (int i = 0; i < TradeCount; i++)
            {
                var (fromId, toId) = PickDistinctCommunities(rng);
                var amount = PickEdgeAmount(rng);
                var attachResource = rng.NextDouble() < 0.5;
                string? resourceId = null;
                double? quantity = null;
                if (attachResource)
                {
                    resourceId = ResourceIds[rng.Next(ResourceIds.Length)];
                    quantity = PickEdgeAmount(rng);
                }

                var result = service.SettleTrade(fromId, toId, amount, resourceId, quantity,
                    $"Randomized trade {i} (seed {RandomSeed})");

                Assert.True(result.Success,
                    $"Trade #{i} was unexpectedly rejected (this is a valid trade by construction): " +
                    $"{fromId} -> {toId}, Amount={amount:R}, ResourceId={resourceId ?? "<null>"}, " +
                    $"Quantity={(quantity.HasValue ? quantity.Value.ToString("R") : "<null>")}, " +
                    $"Reason={result.RejectionReason}, Message={result.Message}");

                trades.Add((i, fromId, toId, amount, resourceId, quantity));

                bool isCheckpoint = (i + 1) % CheckpointInterval == 0;
                bool isLastTrade = i == TradeCount - 1;
                if (isCheckpoint || isLastTrade)
                {
                    var sum = NetworkSum(db.DbPath);
                    if (Math.Abs(sum) >= 1e-6)
                    {
                        var windowStart = i + 1 - ((i + 1) % CheckpointInterval == 0 ? CheckpointInterval : (i + 1) % CheckpointInterval);
                        var failing = BisectFailingTrade(db.DbPath, trades, windowStart, i);
                        Assert.Fail(
                            $"NETWORK-SUM-ZERO INVARIANT BROKEN after trade #{i} (checkpoint sum={sum:R}). " +
                            $"Bisection isolated the first offending trade as #{failing.Index}: " +
                            $"{failing.From} -> {failing.To}, Amount={failing.Amount:R}, " +
                            $"ResourceId={failing.ResourceId ?? "<null>"}, " +
                            $"Quantity={(failing.Quantity.HasValue ? failing.Quantity.Value.ToString("R") : "<null>")} " +
                            $"(seed {RandomSeed}). This is a real bug in the settlement/persistence path, " +
                            $"not a test bug — do not loosen this assertion to make it pass.");
                    }
                }
            }

            Assert.Equal(TradeCount, repository.GetLedgerEntries().Count);
            Assert.True(Math.Abs(NetworkSum(db.DbPath)) < 1e-6,
                $"Final network sum was {NetworkSum(db.DbPath):R}, expected ~0.");
        }

        [Fact]
        public void ZeroQuantityTrade_WithResourceId_IsRejectedCleanly()
        {
            using var db = new EconomyTestDatabase();
            var repository = new EconomyRepository(db.DbPath);
            var service = new TradeSettlementService(repository);

            var result = service.SettleTrade("mountain", "valley", 10, "grain", 0,
                "Zero-quantity trade with a resource attached — must be rejected, not inserted");

            Assert.False(result.Success);
            Assert.Equal(TradeRejectionReason.NonPositiveQuantity, result.RejectionReason);
            Assert.NotNull(result.Message);
            Assert.Empty(repository.GetLedgerEntries());
        }

        [Fact]
        public void NegativeAmount_IsRejectedCleanly_NotAnException()
        {
            using var db = new EconomyTestDatabase();
            var repository = new EconomyRepository(db.DbPath);
            var service = new TradeSettlementService(repository);

            var result = service.SettleTrade("mountain", "valley", -250, null, null,
                "Negative amount — must be rejected with a clear reason, not throw");

            Assert.False(result.Success);
            Assert.Equal(TradeRejectionReason.NonPositiveAmount, result.RejectionReason);
            Assert.NotNull(result.Message);
            Assert.Empty(repository.GetLedgerEntries());
        }

        [Fact]
        public async Task ConcurrentSettleTrade_ProducesConsistentLedger()
        {
            // What this test asserts: whatever subset of concurrent trades actually commits
            // produces a ledger with no corruption — no lost rows, no duplicated rows, no
            // partial/torn writes, and the network-sum-zero invariant holds over exactly what
            // committed.
            //
            // What this test deliberately does NOT assert: that every concurrent SettleTrade
            // call succeeds without contention. Per ARCH.md, this .db runs in SQLite's default
            // rollback-journal mode with no busy_timeout override, and the documented
            // concurrency model is single-writer-by-process-discipline — genuine concurrent
            // writers were an explicitly out-of-scope scenario for that decision, not an
            // oversight. If SQLite serializes or busy-rejects some concurrent attempts, that is
            // the lock working as designed, not ledger corruption.
            const int ConcurrentSeed = 43;
            const int ConcurrentTradeCount = 200;

            using var db = new EconomyTestDatabase();

            var rng = new Random(ConcurrentSeed);
            var plannedTrades = new List<(string From, string To, double Amount, string? ResourceId, double? Quantity)>();
            for (int i = 0; i < ConcurrentTradeCount; i++)
            {
                var (fromId, toId) = PickDistinctCommunities(rng);
                var amount = PickEdgeAmount(rng);
                plannedTrades.Add((fromId, toId, amount, null, null));
            }

            var tasks = plannedTrades.Select((trade, i) => Task.Run(() =>
            {
                var repository = new EconomyRepository(db.DbPath);
                var service = new TradeSettlementService(repository);
                try
                {
                    var result = service.SettleTrade(trade.From, trade.To, trade.Amount,
                        trade.ResourceId, trade.Quantity, $"Concurrent trade {i} (seed {ConcurrentSeed})");
                    return (Outcome: result.Success ? "Succeeded" : "Rejected", Exception: (Exception?)null);
                }
                catch (Exception ex)
                {
                    return (Outcome: "Threw", Exception: ex);
                }
            })).ToArray();

            await Task.WhenAll(tasks);
            var outcomes = tasks.Select(t => t.Result).ToList();

            var unexpectedExceptions = outcomes
                .Where(o => o.Outcome == "Threw" && o.Exception is not SqliteException)
                .ToList();
            Assert.True(unexpectedExceptions.Count == 0,
                "Unexpected (non-SQLite-lock) exception(s) during concurrent settlement: " +
                string.Join("; ", unexpectedExceptions.Select(o => o.Exception!.ToString())));

            var succeededCount = outcomes.Count(o => o.Outcome == "Succeeded");
            var rejectedCount = outcomes.Count(o => o.Outcome == "Rejected");
            var threwCount = outcomes.Count(o => o.Outcome == "Threw");
            Assert.True(rejectedCount == 0,
                "A trade generated by this test was rejected by validation — the generator is " +
                "supposed to only produce structurally valid trades, so this points at a bug in " +
                "the test's trade generation, not in TradeSettlementService.");

            var finalRepository = new EconomyRepository(db.DbPath);
            var storedEntries = finalRepository.GetLedgerEntries();

            // The core "no corruption" assertion: exactly one ledger row per reported success,
            // no more, no fewer — proves no lost writes and no duplicate/phantom writes.
            Assert.Equal(succeededCount, storedEntries.Count);

            // No two committed rows share a TransactionId — proves no torn/duplicate commit
            // produced two rows for what should be one logical write.
            Assert.Equal(storedEntries.Count, storedEntries.Select(e => e.TransactionId).Distinct().Count());

            Assert.True(Math.Abs(NetworkSum(db.DbPath)) < 1e-6,
                $"Network sum after {succeededCount} concurrent trades ({threwCount} lock-contention " +
                $"exceptions) was {NetworkSum(db.DbPath):R}, expected ~0 — this would mean SQLite's " +
                $"locking failed to prevent a corrupted concurrent write, a real bug.");
        }

        // ------------------------------------------------------------------
        // Generation helpers — deliberately duplicated per-file rather than shared, matching
        // the existing pattern (RandomizedEconomyLedgerInvariantTests.cs already duplicates its
        // own copies rather than sharing with EconomyLedgerInvariantTests.cs).

        private static (string fromId, string toId) PickDistinctCommunities(Random rng)
        {
            var fromId = CommunityIds[rng.Next(CommunityIds.Length)];
            string toId;
            do
            {
                toId = CommunityIds[rng.Next(CommunityIds.Length)];
            } while (toId == fromId);

            return (fromId, toId);
        }

        private static double PickEdgeAmount(Random rng)
        {
            var bucket = rng.Next(3);
            return bucket switch
            {
                0 => 0.001 + rng.NextDouble() * (0.1 - 0.001),
                1 => 0.1 + rng.NextDouble() * (100 - 0.1),
                _ => 1000 + rng.NextDouble() * (10000 - 1000),
            };
        }

        // ------------------------------------------------------------------
        // Invariant / bisection helpers.

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

        // Network sum computed over only the first `maxRowId` inserted StoneLedger rows
        // (SQLite's implicit rowid tracks insertion order for this table, which has no
        // INTEGER PRIMARY KEY of its own). Used only for bisection within a failing
        // checkpoint window in the sequential 100k test — not meaningful under concurrent
        // writers, where insertion order isn't deterministic.
        private static double NetworkSumUpToRowId(SqliteConnection conn, long maxRowId)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT COALESCE(SUM(bal), 0) FROM (
                  SELECT CommunityId,
                         COALESCE((SELECT SUM(Amount) FROM StoneLedger WHERE ToCommunityId = c.CommunityId AND rowid <= $max), 0)
                       - COALESCE((SELECT SUM(Amount) FROM StoneLedger WHERE FromCommunityId = c.CommunityId AND rowid <= $max), 0)
                         AS bal
                  FROM Communities c
                ) t;";
            cmd.Parameters.AddWithValue("$max", maxRowId);
            var result = cmd.ExecuteScalar();
            return Convert.ToDouble(result);
        }

        // Binary-searches the [windowStart, windowEnd] trade-index range (0-based, matching
        // `trades`' Index field, which equals rowid - 1 for this single-threaded, sequential
        // test) for the first trade whose cumulative network sum leaves tolerance, and returns
        // that trade's recorded parameters.
        private static (int Index, string From, string To, double Amount, string? ResourceId, double? Quantity)
            BisectFailingTrade(
                string dbPath,
                List<(int Index, string From, string To, double Amount, string? ResourceId, double? Quantity)> trades,
                int windowStart,
                int windowEnd)
        {
            using var conn = new SqliteConnection($"Data Source={dbPath}");
            conn.Open();

            int lo = windowStart, hi = windowEnd;
            while (lo < hi)
            {
                int mid = lo + (hi - lo) / 2;
                var sumThroughMid = NetworkSumUpToRowId(conn, mid + 1); // rowid is 1-based
                if (Math.Abs(sumThroughMid) >= 1e-6)
                    hi = mid;
                else
                    lo = mid + 1;
            }

            return trades[lo];
        }
    }
}
