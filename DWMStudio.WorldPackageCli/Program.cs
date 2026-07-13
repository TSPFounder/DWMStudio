// Program.cs
// Day 12 Task 3: verification tool for WorldPackageExporter.WriteEconomySnapshot. Not
// player-facing -- a debugging/ops aid that (a) runs the export and (b) opens the resulting
// .db fresh (via its own ReadOnly connection, independent of whatever wrote it) and dumps
// table row counts + a few sample rows to console, so the export can be visually confirmed
// without a UE-side reader existing yet.
//
// Also proves Day 12 Task 4: `export` opens a fresh ReadOnly connection against its own
// output immediately after the write connection is disposed (same process, proves the
// written file is well-formed and the write handle was actually released); `verify` opens
// ANY given .db path with a brand-new connection and no awareness of who wrote it -- running
// it as a SEPARATE process right after `export` finishes is the direct proof of RUNBOOK.md's
// "DWMStudio must fully close the handle before UE opens the .db" rule for these new tables.
//
// Usage:
//   dotnet run --project DWMStudio.WorldPackageCli -- export --economy-db <path> --out <path> [--world-id <id>]
//   dotnet run --project DWMStudio.WorldPackageCli -- verify --db <path>

using DWM.Shared;
using Microsoft.Data.Sqlite;

if (args.Length == 0)
{
    PrintUsage();
    return 1;
}

var command = args[0].ToLowerInvariant();
var rest = args.Skip(1).ToArray();

return command switch
{
    "export" => Export(rest),
    "verify" => Verify(rest),
    _ => UnknownCommand(command)
};

static int Export(string[] a)
{
    var economyDb = GetOption(a, "--economy-db");
    var outPath = GetOption(a, "--out");
    var worldId = GetOption(a, "--world-id") ?? "economy";

    if (economyDb is null || outPath is null)
    {
        Console.Error.WriteLine("Usage: export --economy-db <path> --out <path> [--world-id <id>]");
        return 1;
    }

    var exporter = new WorldPackageExporter();
    exporter.WriteEconomySnapshot(outPath, economyDb, worldId);
    // WriteEconomySnapshot has already disposed its write connection by the time it returns.

    Console.WriteLine($"[verify] Re-opening '{outPath}' fresh (ReadOnly) immediately after export...");
    DumpDatabase(outPath);
    return 0;
}

static int Verify(string[] a)
{
    var dbPath = GetOption(a, "--db");
    if (dbPath is null)
    {
        Console.Error.WriteLine("Usage: verify --db <path>");
        return 1;
    }

    DumpDatabase(dbPath);
    return 0;
}

static void DumpDatabase(string dbPath)
{
    var connectionString = new SqliteConnectionStringBuilder
    {
        DataSource = dbPath,
        Mode = SqliteOpenMode.ReadOnly
    }.ToString();

    using var conn = new SqliteConnection(connectionString);
    conn.Open();

    var tableNames = new List<string>();
    using (var cmd = conn.CreateCommand())
    {
        cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' ORDER BY name;";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            tableNames.Add(reader.GetString(0));
    }

    Console.WriteLine($"[verify] Opened '{dbPath}' successfully. {tableNames.Count} tables found.");

    foreach (var table in tableNames)
    {
        long rowCount;
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = $"SELECT COUNT(*) FROM \"{table}\";";
            rowCount = (long)cmd.ExecuteScalar()!;
        }

        Console.WriteLine($"[verify]   {table}: {rowCount} row(s)");

        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = $"SELECT * FROM \"{table}\" LIMIT 3;";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var fields = new List<string>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var value = reader.IsDBNull(i) ? "<null>" : reader.GetValue(i)!.ToString();
                    fields.Add($"{reader.GetName(i)}={value}");
                }
                Console.WriteLine($"[verify]     {string.Join("  ", fields)}");
            }
        }
    }
}

static string? GetOption(string[] a, string name)
{
    var idx = Array.IndexOf(a, name);
    return idx >= 0 && idx + 1 < a.Length ? a[idx + 1] : null;
}

static int UnknownCommand(string command)
{
    Console.Error.WriteLine($"Unknown command '{command}'.");
    PrintUsage();
    return 1;
}

static void PrintUsage()
{
    Console.Error.WriteLine("Usage:");
    Console.Error.WriteLine("  export --economy-db <path> --out <path> [--world-id <id>]");
    Console.Error.WriteLine("  verify --db <path>");
}
