// Program.cs
// Headless CLI for testing/ops: propose, list, settle, and cancel trade requests against an
// existing economy.db. This tool does NOT seed the base economy (Communities/Resources/
// StoneLedger) — that's EconomySeeder's job, run separately first. This tool only manages
// trade lifecycle on top of an already-seeded database; it self-applies the TradeRequests
// migration on first use (TradeRequestRepository's constructor does this).
//
// Usage:
//   dotnet run --project DWMStudio.EconomyCli -- --db <path-to-economy.db> propose <fromCommunityId> <toCommunityId> <amount> [resourceId] [quantity] [memo words...]
//   dotnet run --project DWMStudio.EconomyCli -- --db <path-to-economy.db> list
//   dotnet run --project DWMStudio.EconomyCli -- --db <path-to-economy.db> settle <requestId>
//   dotnet run --project DWMStudio.EconomyCli -- --db <path-to-economy.db> cancel <requestId>

using System.Globalization;
using DWM.Shared.Economy;

if (args.Length < 2 || args[0] != "--db")
{
    PrintUsage();
    return 1;
}

var dbPath = args[1];
var rest = args.Skip(2).ToArray();

if (rest.Length == 0)
{
    PrintUsage();
    return 1;
}

TradeRequestRepository repository;
try
{
    repository = new TradeRequestRepository(dbPath);
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Could not open '{dbPath}': {ex.Message}");
    return 1;
}

var command = rest[0].ToLowerInvariant();
var commandArgs = rest.Skip(1).ToArray();

return command switch
{
    "propose" => Propose(repository, commandArgs),
    "list" => List(repository),
    "settle" => Settle(repository, commandArgs),
    "cancel" => Cancel(repository, commandArgs),
    _ => UnknownCommand(command)
};

static int Propose(TradeRequestRepository repository, string[] a)
{
    if (a.Length < 3)
    {
        Console.Error.WriteLine("Usage: propose <fromCommunityId> <toCommunityId> <amount> [resourceId] [quantity] [memo words...]");
        return 1;
    }

    var fromId = a[0];
    var toId = a[1];
    if (!double.TryParse(a[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var amount))
    {
        Console.Error.WriteLine($"'{a[2]}' is not a valid amount.");
        return 1;
    }

    string? resourceId = a.Length > 3 ? a[3] : null;
    double? quantity = null;
    if (a.Length > 4)
    {
        if (!double.TryParse(a[4], NumberStyles.Float, CultureInfo.InvariantCulture, out var q))
        {
            Console.Error.WriteLine($"'{a[4]}' is not a valid quantity.");
            return 1;
        }
        quantity = q;
    }
    string? memo = a.Length > 5 ? string.Join(' ', a.Skip(5)) : null;

    var result = repository.ProposeTrade(fromId, toId, amount, resourceId, quantity, memo);
    if (!result.Success)
    {
        Console.Error.WriteLine($"Rejected ({result.FailureReason}): {result.Message}");
        return 1;
    }

    Console.WriteLine($"Proposed: {result.Request!.RequestId}");
    PrintRequest(result.Request);
    return 0;
}

static int List(TradeRequestRepository repository)
{
    var pending = repository.GetTradeRequests(TradeRequestStatus.Proposed);
    if (pending.Count == 0)
    {
        Console.WriteLine("No pending (Proposed) trade requests.");
        return 0;
    }

    foreach (var request in pending)
        PrintRequest(request);
    return 0;
}

static int Settle(TradeRequestRepository repository, string[] a)
{
    if (a.Length < 1)
    {
        Console.Error.WriteLine("Usage: settle <requestId>");
        return 1;
    }

    var result = repository.SettleProposedTrade(a[0]);
    if (!result.Success)
    {
        Console.Error.WriteLine($"Rejected ({result.FailureReason}): {result.Message}");
        return 1;
    }

    Console.WriteLine($"Settled: {result.Request!.RequestId} -> StoneLedger TransactionId {result.LedgerEntry!.TransactionId}");
    return 0;
}

static int Cancel(TradeRequestRepository repository, string[] a)
{
    if (a.Length < 1)
    {
        Console.Error.WriteLine("Usage: cancel <requestId>");
        return 1;
    }

    var result = repository.CancelProposedTrade(a[0]);
    if (!result.Success)
    {
        Console.Error.WriteLine($"Rejected ({result.FailureReason}): {result.Message}");
        return 1;
    }

    Console.WriteLine($"Cancelled: {result.Request!.RequestId}");
    return 0;
}

static int UnknownCommand(string command)
{
    Console.Error.WriteLine($"Unknown command '{command}'.");
    PrintUsage();
    return 1;
}

static void PrintRequest(TradeRequest request)
{
    var line =
        $"  {request.RequestId}  {request.FromCommunityId} -> {request.ToCommunityId}  " +
        $"Amount={request.Amount:0.###}  Resource={request.ResourceId ?? "<none>"}  " +
        $"Quantity={request.Quantity?.ToString("0.###") ?? "<none>"}  " +
        $"Status={request.Status}  CreatedAt={request.CreatedAt:o}";
    if (request.ResolvedAt is not null)
        line += $"  ResolvedAt={request.ResolvedAt:o}";
    Console.WriteLine(line);
}

static void PrintUsage()
{
    Console.Error.WriteLine("Usage:");
    Console.Error.WriteLine("  --db <path> propose <fromCommunityId> <toCommunityId> <amount> [resourceId] [quantity] [memo words...]");
    Console.Error.WriteLine("  --db <path> list");
    Console.Error.WriteLine("  --db <path> settle <requestId>");
    Console.Error.WriteLine("  --db <path> cancel <requestId>");
}
