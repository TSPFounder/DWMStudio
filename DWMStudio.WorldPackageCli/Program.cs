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
// Day 34: adds `turbine`, which is how the MATLAB stage gets exercised at all. DWMStudio's UI
// has no button wired to TurbineStageRunner yet, and MatlabComSession's COM transport -- two
// P/Invokes, a Running Object Table attach, and late-bound IDispatch calls -- has NO automated
// coverage and cannot get any: it needs Windows, an installed MATLAB, and a licence. This
// command is the only thing that runs it.
//
// Usage:
//   dotnet run --project DWMStudio.WorldPackageCli -- export --economy-db <path> --out <path> [--world-id <id>]
//   dotnet run --project DWMStudio.WorldPackageCli -- verify --db <path>
//   dotnet run --project DWMStudio.WorldPackageCli -- turbine --csv <path to *_rotor.csv> --out <path>
//   dotnet run --project DWMStudio.WorldPackageCli -- turbine --matlab-dir <dir> --out <path> [--scenario ramp]

using System.Runtime.Versioning;
using DWM.Shared;
using DWM.Shared.Matlab;
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
    "turbine" => Turbine(rest),
    _ => UnknownCommand(command)
};

static int Export(string[] a)
{
    // --economy-db is now OPTIONAL (Day 13): if omitted, exports the canonical golden demo
    // scenario (GoldenEconomyScenario) instead of requiring an existing hand-prepared .db,
    // same "default when nothing else is given" shape as WritePendulum's CSV fallback.
    var economyDb = GetOption(a, "--economy-db");
    var outPath = GetOption(a, "--out");
    var worldId = GetOption(a, "--world-id") ?? "economy";

    if (outPath is null)
    {
        Console.Error.WriteLine("Usage: export [--economy-db <path>] --out <path> [--world-id <id>]");
        Console.Error.WriteLine("  (omit --economy-db to export the canonical golden demo scenario)");
        return 1;
    }

    var exporter = new WorldPackageExporter();
    if (economyDb is null)
    {
        Console.WriteLine("[export] No --economy-db given; exporting the canonical golden demo scenario.");
        exporter.WriteGoldenEconomySnapshot(outPath, worldId);
    }
    else
    {
        exporter.WriteEconomySnapshot(outPath, economyDb, worldId);
    }
    // WriteEconomySnapshot/WriteGoldenEconomySnapshot have already disposed their write
    // connection by the time they return.

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

// ======================================================================
// turbine
// ======================================================================
// TWO MODES, and the difference between them is not convenience -- it is how much the tool
// can promise about the data.
//
//   --csv <path>         Build a package from CSVs ALREADY on disk. No MATLAB involved.
//   --matlab-dir <dir>   Drive MATLAB: run the scenario, export, then build the package.
//
// --matlab-dir is the stronger mode because MatlabStageService knows when the run started and
// can therefore refuse CSVs that predate it. --csv HAS NO SUCH PROTECTION and cannot have any:
// with no run to compare against, a file from last week and a file from a minute ago are
// indistinguishable. That is why this command prints each channel file's modification time in
// --csv mode -- it is the only staleness signal available, and it is a human one.
static int Turbine(string[] a)
{
    var outPath = GetOption(a, "--out");
    var csvPath = GetOption(a, "--csv");
    var matlabDir = GetOption(a, "--matlab-dir");
    var worldId = GetOption(a, "--world-id") ?? "turbine";

    if (outPath is null || (csvPath is null) == (matlabDir is null))
    {
        PrintTurbineUsage();
        return 1;
    }

    try
    {
        return csvPath is not null
            ? TurbineFromCsv(csvPath, outPath, worldId, HasFlag(a, "--allow-placeholder"))
            : TurbineFromMatlab(a, matlabDir!, outPath, worldId);
    }
    catch (MatlabStageException ex)
    {
        Console.Error.WriteLine();
        Console.Error.WriteLine("[turbine] FAILED");
        Console.Error.WriteLine(ex.Message);
        if (ex.InnerException is not null)
            Console.Error.WriteLine($"  Underlying: {ex.InnerException.Message}");
        return 1;
    }
    catch (FileNotFoundException ex)
    {
        // WriteTurbine's own refusal to substitute the placeholder without being asked.
        Console.Error.WriteLine();
        Console.Error.WriteLine("[turbine] FAILED");
        Console.Error.WriteLine(ex.Message);
        return 1;
    }
}

static int TurbineFromCsv(string rotorCsv, string outPath, string worldId, bool allowPlaceholder)
{
    if (rotorCsv.LastIndexOf("_rotor", StringComparison.Ordinal) < 0)
    {
        Console.Error.WriteLine(
            "--csv must point at the ROTOR channel file, whose name contains \"_rotor\".\n" +
            $"  Got: {rotorCsv}\n\n" +
            "WriteTurbine locates the other four channels by substituting on the LAST \"_rotor\"\n" +
            "in the path. A path without it silently yields a rotor-only package, which is why\n" +
            "this refuses rather than proceeding. wtExportSimSamples.m writes wtSimSamples_rotor.csv\n" +
            "alongside _pitch, _yaw, _tower and _power.");
        return 1;
    }

    ReportChannelFiles(rotorCsv);

    if (allowPlaceholder)
    {
        Console.WriteLine();
        Console.WriteLine("[turbine] --allow-placeholder given: if the rotor CSV is absent, a CONSTANT-RATE");
        Console.WriteLine("[turbine] placeholder will be written instead. It is not model output and looks");
        Console.WriteLine("[turbine] identical on screen to data that is.");
    }

    new WorldPackageExporter().WriteTurbine(outPath, worldId, rotorCsv, allowPlaceholder);

    Console.WriteLine();
    Console.WriteLine($"[verify] Re-opening '{outPath}' fresh (ReadOnly) immediately after export...");
    DumpDatabase(outPath);
    return 0;
}

static int TurbineFromMatlab(string[] a, string matlabDir, string outPath, string worldId)
{
    // Argument parsing happens BEFORE the platform check so a typo in --scenario is reported
    // on any platform, not only where MATLAB could have run.
    var scenarioText = (GetOption(a, "--scenario") ?? "ramp").ToLowerInvariant();
    if (!TryParseScenario(scenarioText, out var scenario))
    {
        Console.Error.WriteLine($"Unknown --scenario '{scenarioText}'. Use step, ramp, turbulent or gust.");
        return 1;
    }

    var rate = 30;
    var rateText = GetOption(a, "--rate");
    if (rateText is not null && !int.TryParse(rateText, out rate))
    {
        Console.Error.WriteLine($"--rate must be a whole number of Hz (got '{rateText}').");
        return 1;
    }

    // POSITIVE guard, deliberately. CA1416 recognises `if (OperatingSystem.IsWindows())` around
    // a call to a [SupportedOSPlatform("windows")] member; the Windows-only work lives in
    // TurbineMatlabRunner, which carries that attribute on the type where the analyzer can see
    // it. See that file for why a local function and a lambda both failed to express this.
    if (OperatingSystem.IsWindows())
    {
        return DWMStudio.WorldPackageCli.TurbineMatlabRunner.Run(
            matlabDir,
            outPath,
            worldId,
            scenario,
            rate,
            GetOption(a, "--base-name") ?? "wtSimSamples.csv",
            GetOption(a, "--csv-dir"),
            HasFlag(a, "--require-all-channels"),
            allowLaunch: !HasFlag(a, "--no-launch"),
            progId: GetOption(a, "--progid"),
            dumpDatabase: DumpDatabase);
    }

    Console.Error.WriteLine(
        "--matlab-dir drives MATLAB over Windows COM automation and cannot work here.\n" +
        "Run the export inside MATLAB yourself (wtGui, or wtRunSimulation + wtExportSimSamples)\n" +
        "and then use --csv against the files it wrote.");
    return 1;
}

/// <summary>
/// List every channel file the package will be built from, with its modification time.
/// In --csv mode this is the ONLY staleness signal there is, so it prints even when
/// everything is present -- a channel quietly a week older than its siblings is exactly
/// the thing that would otherwise reach UE unremarked.
/// </summary>
static void ReportChannelFiles(string rotorCsv)
{
    var at = rotorCsv.LastIndexOf("_rotor", StringComparison.Ordinal);
    var tail = rotorCsv.Substring(at + "_rotor".Length);
    var head = rotorCsv.Substring(0, at);

    Console.WriteLine("[turbine] Channel files:");
    foreach (var suffix in MatlabStageService.ChannelSuffixes)
    {
        var path = head + "_" + suffix + tail;
        if (File.Exists(path))
        {
            Console.WriteLine($"[turbine]   found    {suffix,-6} {Path.GetFileName(path),-28} " +
                              $"written {File.GetLastWriteTime(path):yyyy-MM-dd HH:mm:ss}");
        }
        else
        {
            Console.WriteLine($"[turbine]   ABSENT   {suffix,-6} {Path.GetFileName(path)}");
        }
    }
    Console.WriteLine("[turbine] An absent non-rotor channel is not an error: the package is written");
    Console.WriteLine("[turbine] without that block, and nothing downstream will mention it again.");
}

static bool HasFlag(string[] a, string name) => Array.IndexOf(a, name) >= 0;

static bool TryParseScenario(string text, out TurbineScenario scenario)
{
    switch (text)
    {
        case "step": scenario = TurbineScenario.Step; return true;
        case "ramp": scenario = TurbineScenario.Ramp; return true;
        case "turbulent": scenario = TurbineScenario.Turbulent; return true;
        case "gust": scenario = TurbineScenario.Gust; return true;
        default: scenario = TurbineScenario.Ramp; return false;
    }
}

static void PrintTurbineUsage()
{
    Console.Error.WriteLine("Usage, one of:");
    Console.Error.WriteLine("  turbine --csv <path to *_rotor.csv> --out <path> [--world-id <id>] [--allow-placeholder]");
    Console.Error.WriteLine("  turbine --matlab-dir <dir> --out <path> [options]");
    Console.Error.WriteLine();
    Console.Error.WriteLine("  --matlab-dir options:");
    Console.Error.WriteLine("    --scenario <step|ramp|turbulent|gust>  default ramp");
    Console.Error.WriteLine("    --rate <hz>                            default 30");
    Console.Error.WriteLine("    --base-name <name.csv>                 default wtSimSamples.csv");
    Console.Error.WriteLine("    --csv-dir <dir>                        default: the MATLAB code directory");
    Console.Error.WriteLine("    --require-all-channels                 fail if any of the five is absent");
    Console.Error.WriteLine("    --no-launch                            attach only; never start MATLAB");
    Console.Error.WriteLine("    --progid <progid>                      which MATLAB; default matlab.application");
    Console.Error.WriteLine();
    Console.Error.WriteLine("  WITH MORE THAN ONE MATLAB INSTALLED, PASS --progid. The generic ProgID");
    Console.Error.WriteLine("  resolves to one release -- whichever registered last -- and the attach looks");
    Console.Error.WriteLine("  for THAT release in the Running Object Table. It will miss an open R2011a and");
    Console.Error.WriteLine("  launch the newer one instead. R2011a is matlab.application.7.12; list what is");
    Console.Error.WriteLine("  registered with:  reg query HKCR /f \"matlab.application\" /k");
    Console.Error.WriteLine();
    Console.Error.WriteLine("  Give exactly one of --csv or --matlab-dir.");
    Console.Error.WriteLine("  --csv builds from files already on disk and CANNOT detect stale ones;");
    Console.Error.WriteLine("  --matlab-dir runs the model and rejects any CSV predating that run.");
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
    Console.Error.WriteLine("  export  --economy-db <path> --out <path> [--world-id <id>]");
    Console.Error.WriteLine("  verify  --db <path>");
    Console.Error.WriteLine("  turbine --csv <path to *_rotor.csv> --out <path>");
    Console.Error.WriteLine("  turbine --matlab-dir <dir> --out <path> [--scenario ramp] [--rate 30]");
    Console.Error.WriteLine();
    Console.Error.WriteLine("  'turbine' with no arguments prints its own fuller usage.");
}
