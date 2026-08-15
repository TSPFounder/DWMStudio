# STAGE2_TECHNICAL_SPEC.md — the launch-fulfillment build, in code terms

**Status: DRAFT for review.** Written 2026-08-07 against the MVP codebase as it stands.

`DWM_PostMVP_Plan.docx` §3 already defines Phase A as *milestones* (A1 accounts, A2 billing,
A3 hosting, A4 shared world, A5 retention) with a gate and a descope ladder. That plan is
sound and this document does not restate it. **This is the layer underneath it**: which
existing code changes, which architectural decisions expire, and what the migration actually
costs — so the estimate in the business plan ("founder development time already covered by the
draw") can be checked against reality rather than assumed.

The headline: **Stage 2 is not additive.** It invalidates two deliberate MVP-era architecture
decisions, and one schema assumption. None of that is a mistake — those decisions were correct
for a single-player local slice — but they must be retired knowingly rather than discovered.

---

## 1. What expires when the world goes hosted

### 1.1 The no-WAL decision (ARCH.md)

`ARCH.md` chooses SQLite's default rollback-journal mode over WAL, and is explicit about why:

> *"That access pattern is **sequential handoff, not concurrent access**: the writer finishes
> completely and closes, then the reader opens."*

It also names its own expiry condition:

> *"If the pipeline ever needs a writer and a reader open **at the same time** … that's a
> genuine concurrency need WAL is built for."*
> *"the post-MVP shared/persistent world moves multiplayer state to a **hosted backend
> database** … so this decision may simply become moot rather than needing to change."*

**Stage 2 is that trigger.** A hosted world has many concurrent readers and writers by
definition. The recommendation is the second branch ARCH.md anticipated: not WAL, but **move
the authoritative economy to PostgreSQL** and let the SQLite decision become moot exactly as
predicted. Reasons:

- Concurrent writers are the normal case on a server, not the exception WAL grudgingly allows.
- SQLite on a hosted PaaS means a persistent volume, which complicates backup, restore, and
  any future horizontal scaling.
- The existing code is already written against parameterised SQL with explicit transactions —
  the port is mechanical, not a rewrite (see §3).

SQLite stays exactly where it is good: the **world-package format** (`WorldInfo` / `Blocks` /
`Parameters` / `SimSamples`) shipped from DWMStudio to the client. That is a file artifact, not
shared mutable state, and nothing about Stage 2 changes it.

### 1.2 The delete-and-recreate export pattern

`WorldPackageExporter.WritePendulum` / `WriteEconomy` currently `File.Delete` then re-create the
database on every export. For a local slice that is the simplest correct thing.

**In a hosted world it is a data-loss bug.** You cannot delete the live economy that members
are trading in. Stage 2 needs the economy database to be long-lived and migrated forward,
which means:

- Economy state moves out of the exported package entirely and into the backend.
- The package keeps carrying *mechanism* data (turbine, pendulum) — that is genuinely
  regenerable and delete-and-recreate remains right for it.
- A schema-versioning and migration path replaces "recreate from scratch". `TradeRequestsMigration.cs`
  and `DollarVaultPerCommunityMigration.cs` already establish the pattern to follow.

This is the split `ECONOMY_SCHEMA_SPEC.md` already recommends — *"recommend separate files for
now; they change at different rates and serve different consumers"*. Stage 2 turns that
recommendation into a requirement, and the two halves end up in different places entirely
(backend database vs. shipped file).

### 1.3 The schema has no tenancy

`Communities`, `Resources`, `CommunityResources`, `StoneLedger`, `DollarVaultLedger` all assume
**one world**. A hosted platform needs at least two levels above that:

- **`WorldId`** on every economy row — members share a world, and Community-tier groups get
  their own.
- **`AccountId`** — who a member is, which world(s) they may enter, at what tier.

This is the single largest schema change in Stage 2 and it touches every query in
`EconomyRepository` and `TradeRequestRepository`. Do it once, early, before there is production
data to migrate.

> **Invariant note.** `ECONOMY_SCHEMA_SPEC.md`'s network-sum-zero test is currently global.
> Once worlds exist it must become **per-world** (`GROUP BY WorldId`), or a second world's rows
> will mask a first world's imbalance. The existing invariant tests
> (`EconomyLedgerInvariantTests`, `RandomizedEconomyLedgerInvariantTests`) need the same
> partitioning — this is the easiest place in the whole migration to introduce a silent bug.

---

## 2. What survives, unchanged

Worth stating, because it is most of the value and it means Stage 2 is a port rather than a
rebuild:

| Asset | Why it survives |
|---|---|
| **Append-only ledger design** | Immutability is *more* correct under concurrency, not less. No update conflicts are possible on an insert-only table. |
| **Single-row transfer model** | `ECONOMY_SCHEMA_SPEC.md`: network sum zero *"falls out structurally"*. That property is database-independent. |
| **Compare-and-swap state transitions** | `TradeRequestRepository` already settles and cancels via `UPDATE … WHERE Status = 'Proposed'` and checks `rowsAffected == 1`, so *"exactly one concurrent caller can ever see rowsAffected == 1"*. That is precisely the right pattern on a multi-user server and needs no change. |
| **Race-condition test suite** | `TradeRequestCancelRaceConditionTests`, `TradeRequestSettlementRaceConditionTests`, `RandomizedTradeSettlementAtScaleTests` were written for a threat model that only fully arrives in Stage 2. They become more valuable, and they port. |
| **`TradeSettlementService` validation rules** | Structural sanity checks with typed rejection reasons — transport-agnostic. Becomes the API's request validator verbatim. |
| **Mutual-credit rule (no balance checks)** | The deliberate decision *not* to check resulting balances means no read-modify-write on balances, so no lost-update class of bug. This is a real architectural gift to the concurrent case. |

The MVP's discipline pays off here. The parts most at risk in a hosted rewrite — balance
arithmetic, transaction ordering, correction semantics — were designed in a way that is
concurrency-safe by construction.

---

## 3. The seam

`EconomyRepository` takes a file path and opens SQLite directly:

```csharp
public EconomyRepository(string dbPath)
{
    _connectionString = new SqliteConnectionStringBuilder
    {
        DataSource = dbPath, Mode = SqliteOpenMode.ReadWrite
    }.ToString();
}
```

Every consumer (`TradeSettlementService`, `CommunityFailureStateService`, the CLIs, the WPF
view models) goes through this class. That makes it the **single seam** for the whole
migration.

**Recommended sequence — smallest reversible steps:**

1. **Extract interfaces** from the current concrete classes — `IEconomyStore`,
   `ITradeRequestStore` — with the existing SQLite classes as the first implementation.
   No behaviour change; the whole test suite must still pass untouched. This step alone is
   worth doing even if Stage 2 slipped, because it is what makes the rest incremental.
2. **Add `WorldId`** through the schema, the interfaces, and the invariant tests. Still local
   SQLite, still single-user. Verify the per-world invariant partitioning here, where it is
   cheap to get wrong.
3. **Add a Postgres implementation** of the same interfaces. Run the *entire* existing test
   suite against both implementations — the race-condition and randomised-scale tests are the
   ones that matter, and they are already written.
4. **Put an HTTP API in front of it** (§4). The UE client gets a third implementation that
   talks to the API rather than a database.
5. **Only then** wire accounts and billing, which are a separate service that never touches
   the economy tables directly.

Steps 1–3 are pure refactor-and-port with a green test suite as the gate at every point. Step 4
is where genuinely new code starts.

---

## 4. API surface (minimum for A3/A4)

Backend-authoritative, async-first, per `DWM_PostMVP_Plan.docx` §3.2. Read paths are cacheable;
write paths are few and all go through validation that already exists.

```
GET  /v1/worlds/{worldId}/communities          -> communities + current Stone balances
GET  /v1/worlds/{worldId}/resources
GET  /v1/worlds/{worldId}/ledger?since={cursor} -> append-only, cursor-paged
GET  /v1/worlds/{worldId}/vault                 -> dollar vault + failure states
POST /v1/worlds/{worldId}/trades                -> body = TradeSettlementService's inputs
GET  /v1/worlds/{worldId}/trades/{id}
POST /v1/worlds/{worldId}/trades/{id}/cancel
GET  /v1/me                                     -> account, tier, entitlements
```

Notes that matter:

- **`POST /trades` must be idempotent.** A client retry after a timeout must not mint Stone
  twice. Require a client-supplied idempotency key, store it with a uniqueness constraint, and
  return the original result on replay. This is the one place where the append-only design
  works *against* you — there is no natural duplicate detection on an insert-only table.
- **The ledger cursor** makes async-first sync trivial: the client asks "what happened since
  X?" and an append-only table answers that perfectly. This is the payoff for step 2 of the
  descope ladder in the Post-MVP plan.
- **Rejection reasons are already typed** (`TradeRejectionReason`) — map them to HTTP status +
  a stable error code rather than inventing a second vocabulary.
- **Entitlement checks belong at the API boundary**, not in the economy layer. Keep
  `TradeSettlementService` ignorant of tiers and payments.

---

## 5. Accounts and billing — keep them out of the game

A1 and A2 should be a **separate service with a separate database**, joined to the world only
by an opaque `AccountId`.

- Use a hosted identity provider rather than rolling authentication. The plan's own note that
  *"security-sensitive code (authentication, payment webhooks) gets a second-agent review as a
  rule"* is right, and the strongest version of that rule is writing less of it.
- Use the billing provider's hosted checkout and customer portal so card data never reaches
  DWM infrastructure, and so plan changes, dunning, and cancellation come for free.
- **Webhooks are the source of truth for subscription state**, and they must be idempotent and
  replay-safe — the same discipline as `POST /trades`.
- Sales-tax/VAT: use the billing provider's tax handling. **[PROFESSIONAL REVIEW REQUIRED]** —
  business plan §2.3 and §5.4 both flag this, and it must be settled before the first charge.
- **Founding-member redemption** (Kickstarter backers) is an entitlement grant with an expiry,
  not a payment. Model it as a credit on the account so it flows through the same tier check as
  a paid subscription, and so the financial model's prepaid-window accounting has something
  real to read.

---

## 6. Cost reality check

The business plan budgets **$10,000 hosting for Year 1** and calls it *"modeled
conservatively"*. Against the architecture above — a small PaaS app service, a managed Postgres
with backups, object storage for world packages, and egress — that is plausible *precisely
because* the design is backend-authoritative and async-first rather than real-time.

It stops being plausible the moment live co-presence (dedicated UE servers) enters scope. That
is the cost argument for holding the A4 line where the Post-MVP plan puts it, and it is worth
recording here because the pressure to add real-time presence will come from members, not from
the plan.

---

## 7. Open questions

- [ ] Confirm Postgres over "SQLite with WAL on a persistent volume". This spec recommends
      Postgres; ARCH.md's authors should confirm, since it supersedes a documented decision.
- [ ] Does the economy database stay one-world-per-row (`WorldId` column) or one-database-per-world?
      Column is simpler and recommended; per-database isolates Community-tier customers better.
- [ ] Where does the verification gate run in a hosted world — still founder-side in DWMStudio,
      or as a backend service? MVP assumes the former; Stage 2 does not force a change, but
      Phase B's content cadence probably does.
- [ ] Client-side: does the UE build talk to the API directly, or keep reading a locally cached
      package that a sync process refreshes? The latter preserves more MVP code and is the
      faster path to A3.
