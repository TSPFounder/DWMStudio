# Week 2 Kickoff — Phase 1a: Stone Ledger Core (Jul 6–10)

**Theme:** Build the Stone ledger and settlement function. Per the MVP plan, this is *the
single most important week* — the invariant must become unbreakable. **No UE work happens
until the randomized invariant test is green.**

## Where Week 2 actually starts from (not from zero)

Week 1 overran its own plan in the best direction: the economy schema (Day 3) and a
randomized-trade invariant suite (pulled forward from Day 7) both landed on 7/2, alongside
the tracer bullet. That changes Week 2's starting line:

**Already done, carries forward:**
- `Communities` / `Resources` / `CommunityResources` / `StoneLedger` / `DollarVaultLedger`
  schema, live and seeded (`EconomySeeder.cs`, `DWM.Shared/Economy/`).
- The append-only, single-row-transfer ledger design (network-sum-zero by construction) —
  this IS the invariant-safety design Day 5's Cw review would otherwise be checking for the
  first time. It's already been reasoned through; Day 5's review becomes a confirmation
  pass, not a from-scratch audit.
- `DWMStudio.Tests` (xUnit) exists with 5 passing tests: seed-alone, one synthetic trade,
  100 randomized trades (fixed seed, mixed magnitudes, per-trade invariant check), a
  rollback test, and a same-community constraint-rejection test.
- The SQLite journal-mode decision (ARCH.md) — settled, no code impact.

**Still genuinely Week 2's job — this is real, not just polish:**
- **Domain types + repository layer** (Day 5): the schema exists, but the C# `Community` /
  `LedgerEntry` domain objects and a proper repository (vs. the seeder's raw
  `Microsoft.Data.Sqlite` calls) are not yet built. This is the actual Day 5 task.
- **The settlement function itself** (Day 6): nothing settles a trade end-to-end yet — the
  existing tests insert `StoneLedger` rows directly to prove the invariant holds, they don't
  exercise a real "propose → validate → commit-or-reject" settlement path. §4.3's
  assert-then-commit function is unwritten.
- **Scale + adversarial cases** (Day 7): today's suite is 100 trades with a fixed seed and
  three magnitude buckets. Day 7 wants **100,000 iterations** and specifically **zero-qty,
  negative-price, and concurrent-write** adversarial cases — meaningfully more than what
  exists. Self-trade rejection is already covered (today's test 3) — one adversarial case
  down, three to go.
- **Trade lifecycle** (Day 8): `proposed → settled/cancelled` status, plus a CLI to issue
  trades headlessly — none of this exists; today's tests write directly to the ledger, they
  don't model a trade's lifecycle.
- **DEMO_ECONOMY.md + consolidation** (Day 9): unwritten.

**Bottom line:** Week 1 didn't finish Week 2's work — it proved the *foundation* Week 2
builds on is sound. The riskiest unknown (does the invariant hold at all?) already has a
100-trial, four-way-repeated "yes." That's real risk reduction, not schedule compression —
Week 2's actual scope (domain layer, settlement function, trade lifecycle, 100k-scale
adversarial testing) is unchanged and still the week's real work.

## Agent routing for this week

Same triage lens as Week 1: quick, single-file, one-shot tasks stay as G/X/Cw chat; tasks
that are genuinely multi-step, span multiple files, or benefit from actually running code
are Cowork-favorable. Applying it to this week's non-Claude-Code tasks:

| Day | Task | Recommended | Why |
| --- | --- | --- | --- |
| 5 | Cw — review domain model for invariant safety | **Chat** | Small, targeted review; the design was already reasoned through in Week 1, this is confirmation not discovery. |
| 6 | X — independently implement settlement to diff | **Borderline** | A reference implementation to eyeball → chat is fine. Actually running it against the real fixtures to prove it works → Cowork. |
| 7 | Cw — review test coverage, find untested invariant paths | **Cowork** | By Day 7 the test suite spans several files (seeder, 2 original + 3 randomized tests, Day-6 settlement tests). Finding real gaps needs to read the actual multi-file suite systematically, not a pasted excerpt. |
| 8 | G — research command/request table patterns for UE writes | **Chat** | Single research question, one-shot. |
| 9 | **Cw — code-review the week's output, produce a refactor punch-list** | **Cowork — best fit of the week** | Reviewing everything built across Days 5–9 and producing a structured, actionable punch-list is exactly the multi-step, multi-file, durable-artifact profile Cowork is built for. Pasting a week of code into chat is the wrong tool for this. |
| 9 | X — draft DEMO_ECONOMY.md narration | **Chat** | Quick content task once the headless demo session exists to narrate. |

**One thing worth deciding deliberately, not by default:** Day 6's X-vs-Cowork call depends
on how seriously you want the "independently implement so you can diff" exercise taken. If
the point is a quick sanity check on your approach, chat is faster. If the point is a real
second implementation that gets run against the same fixtures — genuinely useful for
catching a design blind spot — that's a Cowork task.

## The hard gate, restated

Per the plan's own words: **no UE work this week until the randomized invariant test is
green at Day-7 scale (100k iterations) with the adversarial cases added.** Today's 100-trial
result is encouraging, not sufficient — treat it as "the design survived a first stress
test," not "the invariant is proven." Don't let early momentum shortcut Day 7's actual bar.
