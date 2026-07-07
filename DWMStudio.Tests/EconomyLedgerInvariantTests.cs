// EconomyLedgerInvariantTests.cs
// Verifies the network-sum-zero invariant from ECONOMY_SCHEMA_SPEC.md (Appendix B.4):
// per-community StoneLedger balances must always sum to exactly 0 across the network,
// because every row both credits ToCommunityId and debits FromCommunityId by the same
// Amount. Checked (a) right after the seed, with no ledger entries yet, and (b) after
// inserting synthetic trade rows.

using System;
using System.IO;
using DWM.Shared.Economy;
using Microsoft.Data.Sqlite;
using Xunit;

namespace DWMStudio.Tests
{
    public sealed class EconomyLedgerInvariantTests : IDisposable
    {
        private readonly string _dbPath;

        public EconomyLedgerInvariantTests()
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
        public void NetworkSum_IsZero_AfterSeedAlone()
        {
            using var conn = OpenConnection();

            Assert.Equal(0, CountRows(conn, "StoneLedger"));
            Assert.Equal(0.0, NetworkSum(conn), precision: 9);
        }

        [Fact]
        public void NetworkSum_IsZero_AfterSyntheticTrade()
        {
            using var conn = OpenConnection();

            // Suburb sells 20 units of Textiles to Hillside for 20 Stones —
            // a buyer/seller pair moving value from Hillside to Suburb.
            InsertLedgerRow(conn, "trade-001", "hillside", "suburb", 20, "textiles", 20,
                "Hillside buys Textiles from Suburb");

            // A second, unrelated trade: Mountain pays City for Software Services.
            InsertLedgerRow(conn, "trade-002", "mountain", "city", 10, "software_services", 10,
                "Mountain pays City for Software Services");

            Assert.Equal(2, CountRows(conn, "StoneLedger"));
            Assert.Equal(0.0, NetworkSum(conn), precision: 9);
        }

        // ------------------------------------------------------------------
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
            string resourceId, double quantity, string memo)
        {
            using var cmd = conn.CreateCommand();
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
            cmd.Parameters.AddWithValue("$resource", resourceId);
            cmd.Parameters.AddWithValue("$qty", quantity);
            cmd.Parameters.AddWithValue("$memo", memo);
            cmd.ExecuteNonQuery();
        }
    }
}
