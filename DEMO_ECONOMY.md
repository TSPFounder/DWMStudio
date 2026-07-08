# DEMO_ECONOMY.md — Living Reference for the Stone Ledger Economy

This is a **reference document**, not a demo script (that's a separate Week-9 deliverable).
It exists so a future contributor — including future-you — can understand how the economy
system works end to end without having sat through Days 5–8 of Week 2. It covers what
exists today, how to exercise it by hand, and the pattern to follow when extending it.

**Status note (write this section current, then delete it once it's stale):** as of this
writing, `TradeRequestRepository.SettleProposedTrade`'s compare-and-swap fix (the `Settling`
status, described below) and the "exclude the 100k-trade test from default `dotnet test`"
change are both sitting in open PRs, not yet on `master`. Check `git log` / open PRs before
assuming either is live. Everything else described here is merged.

## 1. What this system is

A **LETS (Local Exchange Trading System) mutual-credit economy** for five MVP communities
(Mountain, Hillside, Valley, Suburb, City). The core idea, from `ECONOMY_SCHEMA_SPEC.md` and
the Primer: **Stones are minted at the moment of exchange.** There's no pre-funded pool of
currency communities draw down — every trade creates exactly as many Stones as it needs, one
side credited, the other debited, by the same amount. This means:

- A community's balance going **negative is normal**, not an error. It just means that
  community has paid out more than it's received so far — expected in an economy with
  producer/consumer specialization (see the "Starter resource list" in
  `ECONOMY_SCHEMA_SPEC.md` for why each community both produces and needs different things).
- The **network sum across all communities is always exactly zero** — see §3.

Separately, a **Dollar Vault** models a finite, real-world-backed currency pool (not
mutual credit) — structurally kept apart from Stones so one can never accidentally offset
the other.

## 2. End to end: schema → seeding → settlement → lifecycle

```
economy_schema.sql            (DWM.Shared/Economy/)  — the frozen, original schema
        │
        ▼
EconomySeeder.WriteEconomy()  (DWM.Shared/Economy/)  — creates + seeds a fresh economy.db
        │
        ▼
EconomyRepository              — read/write access to Communities, Resources,
                                  CommunityResources, StoneLedger, DollarVaultLedger
        │
        ├──────────────────────────────────┐
        ▼                                   ▼
TradeSettlementService              TradeRequestRepository
  .SettleTrade(...)                   .ProposeTrade(...)
  — ONE atomic call:                  .SettleProposedTrade(id)
    validate, then commit             .CancelProposedTrade(id)
    to StoneLedger immediately        — a lifecycle: Proposed → Settling → Settled,
    (no intermediate state)             or Proposed → Cancelled. StoneLedger only
                                         gets a row once SettleProposedTrade actually
                                         settles it.
```

**`economy_schema.sql`** defines five tables: `Communities`, `Resources`,
`CommunityResources` (what each community produces/needs), `StoneLedger` (the append-only
mutual-credit ledger), and `DollarVaultLedger`/`DollarVaultConfig`. This file and
`EconomySeeder.cs` are treated as frozen — every PR since Day 5 has deliberately not touched
either; new tables are added via separate, additive migration files instead (see
`trade_requests_migration.sql` as the example).

**`EconomySeeder.WriteEconomy(path)`** deletes and recreates the `.db` file at `path`, runs
the schema DDL, and seeds it with the five communities, ten resources, their
produces/needs links, and the Dollar Vault's opening balance. `StoneLedger` starts **empty**
— the first trade is the first real transaction, by design.

**`EconomyRepository`** is the plain data-access layer: `GetCommunities()`, `GetResources()`,
`GetCommunityResources(id)`, `GetLedgerEntries(communityId?)`, `GetDollarVaultBalance()`, and
the one write path, `AppendLedgerEntry(...)` — **insert-only**, matching `StoneLedger`'s
append-only design (no Update/Delete exists for it; that's intentional, not missing).

There are **two ways to settle a trade**, both ending at the same `AppendLedgerEntry` call:

1. **`TradeSettlementService.SettleTrade(...)`** — one call, validate-then-commit
   immediately. This is what the MVP trade panel (fill in seller/resource/quantity, press
   Confirm) uses. No intermediate persisted state.
2. **`TradeRequestRepository`'s propose/settle/cancel lifecycle** — for workflows that need
   a pending state between "someone proposed a trade" and "it actually happened" (e.g. the
   CLI below, or a future UI that wants a review step). `ProposeTrade` records intent without
   touching `StoneLedger`; `SettleProposedTrade` re-validates against *current* state (not
   just what was true at propose time) and only then commits; `CancelProposedTrade` marks it
   cancelled and never touches `StoneLedger`.

Both paths apply the same structural validation before ever writing: `Amount > 0`, no
self-trade, real community IDs, and (if a resource is attached) a real resource ID with a
positive quantity. Neither path ever validates against *resulting balance* — see §1.

## 3. The network-sum-zero invariant

**The invariant:** at any point in time, summing every community's balance
(`SUM(inflow) - SUM(outflow)` across `StoneLedger`) across the whole network must equal
exactly zero.

```sql
SELECT SUM(bal) AS network_sum FROM (
  SELECT CommunityId,
         COALESCE((SELECT SUM(Amount) FROM StoneLedger WHERE ToCommunityId = c.CommunityId), 0)
       - COALESCE((SELECT SUM(Amount) FROM StoneLedger WHERE FromCommunityId = c.CommunityId), 0)
         AS bal
  FROM Communities c
) t;
```

**Why it matters:** this is the one property that has to hold for the mutual-credit model to
be internally consistent — every row in `StoneLedger` both credits `ToCommunityId` and debits
`FromCommunityId` by the same `Amount`, so the sum is zero *by construction* for any
well-formed set of rows. If this query ever returns nonzero, something wrote a malformed or
partial row — a real bug, not a tuning issue.

**Where it's tested:** `EconomyLedgerInvariantTests.cs` (seed-alone + one synthetic trade),
`RandomizedEconomyLedgerInvariantTests.cs` (100 randomized trades via direct SQL, plus a
rollback case and a same-community-rejection case), and
`RandomizedTradeSettlementAtScaleTests.cs` (the Week-2 target: 100,000 trades through
`TradeSettlementService`, checked every 5,000 trades with bisection-on-failure to name the
exact offending trade — checking after *every* trade at this scale would be O(n²) and
impractically slow).

**A duplicated ledger row would NOT trip this invariant** — a row appended twice still nets
to zero across the network. That's exactly why the `SettleProposedTrade` double-commit bug
(§5) went undetected by these tests: the invariant tests prove internal consistency, not
"did the right number of trades happen." Keep that distinction in mind before assuming green
invariant tests mean "no bugs."

## 4. Running the CLI

`DWMStudio.EconomyCli` is a headless console tool for testing/ops — not player-facing UI. It
manages the `TradeRequests` lifecycle only; it does **not** seed the base economy (run
`EconomySeeder` separately first to produce an `economy.db`).

```bash
# Propose a trade: Hillside sells 20 Textiles to Suburb for 20 Stones
dotnet run --project DWMStudio.EconomyCli -- --db ./economy.db propose hillside suburb 20 textiles 20 "Hillside sells Textiles to Suburb"

# List pending (Proposed) trade requests
dotnet run --project DWMStudio.EconomyCli -- --db ./economy.db list

# Settle a specific request by id (id comes from the propose/list output)
dotnet run --project DWMStudio.EconomyCli -- --db ./economy.db settle <requestId>

# Cancel a specific request by id instead
dotnet run --project DWMStudio.EconomyCli -- --db ./economy.db cancel <requestId>

# A Stone-only trade (no resource attached) is also valid — omit resourceId/quantity:
dotnet run --project DWMStudio.EconomyCli -- --db ./economy.db propose mountain city 15
```

Every command prints a clean rejection message (with a typed reason) instead of a raw
exception for bad input — e.g. proposing against a nonexistent community or resource,
settling an already-settled or already-cancelled request, or settling with a zero/negative
amount.

## 5. If you're extending this

**Follow the existing layering, don't collapse it.** The pattern so far:

- **`EconomyRepository`** — pure data access to the tables `economy_schema.sql` defines.
  Read methods return non-null `IReadOnlyList<T>` (empty if nothing matches, never null).
  `GetDollarVaultBalance()` returns a plain non-nullable `double` (SQL-level `COALESCE(...,
  0)` — an empty ledger and one that nets to zero are indistinguishable by design). The one
  write method, `AppendLedgerEntry`, does zero validation of its own — the schema's `CHECK`
  constraints are the source of truth for structural rules, and callers above this layer
  (`TradeSettlementService`, `TradeRequestRepository`) are expected to pre-validate for a
  clean error message rather than let a raw `SqliteException` reach calling code.
- **`TradeSettlementService`** and **`TradeRequestRepository`** are both layered *on top of*
  `EconomyRepository`, composing it rather than reimplementing or absorbing it. If you add a
  new way to move Stones, follow this same shape: a thin service/repository that calls
  `EconomyRepository.AppendLedgerEntry` for the actual write, and does its own structural
  pre-validation first.
- **Connection pattern:** every repository method opens its own short-lived
  `SqliteConnection` (via a private `OpenConnection()` that sets `PRAGMA foreign_keys = ON`
  and returns), does its work, and lets `using` close it. Writes wrap their `INSERT`/`UPDATE`
  in an explicit `BeginTransaction()`/`Commit()`, even for a single statement, for
  consistency across the codebase — not because SQLite requires it for a lone statement.
- **Domain types are plain C# records**, not tied to SQLite, nullable fields matching the
  schema's nullable columns exactly (e.g. `LedgerEntry.ResourceId`/`Quantity` are `null` for
  a Stone-only trade).
- **Result types, not exceptions, for expected failures.** `TradeSettlementResult` and
  `TradeRequestResult` both follow the same shape: `Success` + a typed enum reason + a
  human-readable message on failure, a payload on success. A rejected trade is a normal
  return value; only genuinely exceptional failures (e.g. the database file being
  unreachable) should throw.
- **Tests:** every test file uses the `EconomyTestDatabase` fixture (`DWMStudio.Tests/`) for
  a fresh, isolated, freshly-seeded temp `.db` per test — no shared fixture between tests.
  Small SQL helpers (network-sum queries, etc.) are deliberately duplicated per test file
  rather than shared, matching the existing convention.
- **Adding a new table?** Don't touch `economy_schema.sql`. Add a new `..._migration.sql`
  file (see `trade_requests_migration.sql`) plus a small `...Migration.cs` with an
  `EnsureCreated(dbPath)` that runs the same DDL via `CREATE TABLE/INDEX IF NOT EXISTS`,
  called idempotently from your new repository's constructor.
- **Known, deliberate duplication:** `TradeRejectionReason` (`TradeSettlementResult.cs`) and
  `TradeRequestFailureReason` (`TradeRequestResult.cs`) share six overlapping values on
  purpose — kept as two separate enums to avoid coupling the frozen `SettleTrade` path to the
  newer, still-evolving lifecycle path. Don't "clean this up" by merging them without
  re-reading why first.
- **A known race-condition lesson (worth internalizing before writing concurrency tests):**
  a naive two-thread `Task.Run` contention test passed even against code with a confirmed,
  real double-commit bug — the unlucky timing window was too narrow for a bare race to
  reliably hit. The test that actually proved anything was one that *forced* the exact
  crash/interleaving state directly (bypassing the normal call path to reconstruct
  "the ledger write already happened, the status update didn't"). If you're testing
  concurrent-safety of anything with a read-then-conditionally-write pattern on shared
  mutable state, don't trust a passing `Task.Run` race test alone — construct the state
  directly too.
