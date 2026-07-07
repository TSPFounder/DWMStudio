// EconomyTestDatabase.cs
// Reusable test fixture: a freshly-seeded temp economy.db per test. Extracts the isolation
// pattern already used by EconomyLedgerInvariantTests.cs and
// RandomizedEconomyLedgerInvariantTests.cs (fresh temp file via EconomySeeder, no shared
// fixture between tests) so Day 6's settlement tests don't have to re-duplicate it.
//
// Usage: `using var db = new EconomyTestDatabase();` in a test constructor, then pass
// db.DbPath to whatever's under test (e.g. new EconomyRepository(db.DbPath)).

using System;
using System.IO;
using DWM.Shared.Economy;
using Microsoft.Data.Sqlite;

namespace DWMStudio.Tests
{
    public sealed class EconomyTestDatabase : IDisposable
    {
        public string DbPath { get; }

        public EconomyTestDatabase()
        {
            DbPath = Path.Combine(Path.GetTempPath(), $"dwm_economy_test_{Guid.NewGuid():N}.db");
            new EconomySeeder().WriteEconomy(DbPath);
        }

        public void Dispose()
        {
            // Microsoft.Data.Sqlite pools connections by connection string, which can
            // keep the file handle open after a `using` block disposes the connection.
            SqliteConnection.ClearAllPools();
            if (File.Exists(DbPath))
                File.Delete(DbPath);
        }
    }
}
