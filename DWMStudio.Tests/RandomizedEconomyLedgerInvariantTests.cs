// RandomizedEconomyLedgerInvariantTests.cs
// Extends the network-sum-zero invariant coverage from EconomyLedgerInvariantTests.cs
// (which proves the invariant for two hand-written trades) with randomized, reproducible
// trade generation, an assert-after-every-insert check, an explicit rollback path, and a
// proof that the schema's CHECK (FromCommunityId <> ToCommunityId) constraint actually
// rejects same-community trades.
//
// Same isolation pattern as EconomyLedgerInvariantTests.cs: each test seeds its own fresh
// temp .db file via EconomySeeder, no shared fixture. Does not modify economy_schema.sql,
// EconomySeeder.cs, or EconomyLedgerInvariantTests.cs.

using System;
using System.IO;
using DWM.Shared.Economy;
using Microsoft.Data.Sqlite;
using Xunit;

namespace DWMStudio.Tests
{
    public sealed class RandomizedEconomyLedgerInvariantTests : IDisposable
    {
        // Reproducible, not flaky: fixed seed so failures are re-runnable and reportable.
        private const int RandomSeed = 42;

        // Turn this up later for deeper fuzzing; kept modest so `dotnet test` stays fast.
        private const int TradeCount = 100;

        private static readonly string[] CommunityIds =
            { "mountain", "hillside", "valley", "suburb", "city" };

        private static readonly string[] ResourceIds =
        {
            "timber", "wind_power", "orchard_fruit", "wool", "grain",
            "water", "skilled_labor", "textiles", "manufactured_tools", "software_services"
        };

        private readonly string _dbPath;

        public RandomizedEconomyLedgerInvariantTests()
        {
            _dbPath = Path.Combine(Path.GetTempPath(), $"dwm_economy_test_{Guid.NewGuid():N}.db");
            new EconomySeeder().WriteEconomy(_dbPath);
        }

        public void Dispose()
        {
            // Microsoft.Data.Sqlite pools connections by connection string, which can
            // keep the file handle open after a `using` block disposes the connection.
            SqliteConnection.ClearAllPools();
            if (File.Exists(_dbPath))
                File.Delete(_dbPath);
        }

        [Fact]
        public void Invariant_HoldsAfterEveryRandomizedTrade()
        {
            using var conn = OpenConnection();
            var rng = new Random(RandomSeed);

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

                InsertLedgerRow(conn, $"rand-trade-{i:D4}", fromId, toId, amount, resourceId, quantity,
                    $"Randomized trade {i} (seed {RandomSeed})");

                var sum = NetworkSum(conn);
                Assert.True(Math.Abs(sum) < 1e-6,
                    $"Invariant broken at trade #{i} (seed {RandomSeed}): " +
                    $"{fromId} -> {toId}, Amount={amount:R}, ResourceId={resourceId ?? "<null>"}, " +
                    $"Quantity={(quantity.HasValue ? quantity.Value.ToString("R") : "<null>")}, " +
                    $"network sum = {sum:R}.");
            }

            Assert.Equal(TradeCount, CountRows(conn, "StoneLedger"));
        }

        [Fact]
        public void Invariant_HoldsAfterRolledBackTrade()
        {
            using var conn = OpenConnection();

            Assert.Equal(0, CountRows(conn, "StoneLedger"));
            Assert.Equal(0.0, NetworkSum(conn), precision: 9);

            using (var tx = conn.BeginTransaction())
            {
                try
                {
                    InsertLedgerRow(conn, tx, "would-be-trade", "mountain", "valley", 42, "grain", 5,
                        "Simulated failed trade — must not persist");

                    // Simulate a failure detected after the write but before commit
                    // (e.g. a downstream validation or a crashed caller).
                    throw new InvalidOperationException("Simulated trade failure before commit.");
                }
                catch (InvalidOperationException)
                {
                    tx.Rollback();
                }
            }

            // The rolled-back trade must leave no partial trace.
            Assert.Equal(0, CountRows(conn, "StoneLedger"));
            Assert.Equal(0.0, NetworkSum(conn), precision: 9);
        }

        [Fact]
        public void SameCommunityTrade_IsRejectedBySchemaCheck()
        {
            using var conn = OpenConnection();

            var ex = Assert.Throws<SqliteException>(() =>
                InsertLedgerRow(conn, "self-trade", "mountain", "mountain", 10, null, null,
                    "Mountain paying itself — should be rejected"));

            Assert.Contains("CHECK", ex.Message, StringComparison.OrdinalIgnoreCase);

            // The rejected insert must leave no trace either.
            Assert.Equal(0, CountRows(conn, "StoneLedger"));
            Assert.Equal(0.0, NetworkSum(conn), precision: 9);
        }

        // ------------------------------------------------------------------
        private (string fromId, string toId) PickDistinctCommunities(Random rng)
        {
            var fromId = CommunityIds[rng.Next(CommunityIds.Length)];
            string toId;
            do
            {
                toId = CommunityIds[rng.Next(CommunityIds.Length)];
            } while (toId == fromId);

            return (fromId, toId);
        }

        // Mixes tiny, ordinary, and large magnitudes rather than only "nice" round
        // numbers, per the task's edge-case requirement (schema only requires > 0).
        private double PickEdgeAmount(Random rng)
        {
            var bucket = rng.Next(3);
            return bucket switch
            {
                0 => 0.001 + rng.NextDouble() * (0.1 - 0.001),     // tiny: [0.001, 0.1)
                1 => 0.1 + rng.NextDouble() * (100 - 0.1),         // ordinary: [0.1, 100)
                _ => 1000 + rng.NextDouble() * (10000 - 1000),     // large: [1000, 10000)
            };
        }

        private SqliteConnection OpenConnection()
        {
            var conn = new SqliteConnection($"Data Source={_dbPath}");
            conn.Open();
            return conn;
        }

        private static long CountRows(SqliteConnection conn, string table)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"SELECT COUNT(*) FROM {table};";
            return (long)cmd.ExecuteScalar()!;
        }

        private static double NetworkSum(SqliteConnection conn)
        {
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

        private static void InsertLedgerRow(SqliteConnection conn, string transactionId,
            string fromCommunityId, string toCommunityId, double amount,
            string? resourceId, double? quantity, string memo)
            => InsertLedgerRow(conn, null, transactionId, fromCommunityId, toCommunityId,
                amount, resourceId, quantity, memo);

        private static void InsertLedgerRow(SqliteConnection conn, SqliteTransaction? tx,
            string transactionId, string fromCommunityId, string toCommunityId, double amount,
            string? resourceId, double? quantity, string memo)
        {
            using var cmd = conn.CreateCommand();
            if (tx is not null)
                cmd.Transaction = tx;
            cmd.CommandText = @"
                INSERT INTO StoneLedger
                    (TransactionId, Timestamp, FromCommunityId, ToCommunityId, Amount, ResourceId, Quantity, Memo)
                VALUES
                    ($id, $ts, $from, $to, $amount, $resource, $qty, $memo);";
            cmd.Parameters.AddWithValue("$id", transactionId);
            cmd.Parameters.AddWithValue("$ts", DateTime.UtcNow.ToString("o"));
            cmd.Parameters.AddWithValue("$from", fromCommunityId);
            cmd.Parameters.AddWithValue("$to", toCommunityId);
            cmd.Parameters.AddWithValue("$amount", amount);
            cmd.Parameters.AddWithValue("$resource", (object?)resourceId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$qty", (object?)quantity ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$memo", memo);
            cmd.ExecuteNonQuery();
        }
    }
}
