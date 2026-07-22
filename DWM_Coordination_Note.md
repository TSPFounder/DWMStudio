# Coordination Note — Parallel CC/Codex Task Tracks (2026-07-18)

## Why split this way

Two chains, each internally sequential (same tool, same order — never
skip ahead within a chain), running in parallel against each other. This
avoids the two real file-collision risks in tonight's task queue:
character input handling (`DWM_DevCharacter`) and the guardrail-protected
economy files (`economy_schema.sql`, `EconomySeeder.cs`). Keeping each
risk category on ONE tool, sequentially, means no two agents touch the
same file without knowing about each other's changes.

**Shared rule for both tracks:** commit and push at the end of each task
before starting the next one in the same chain — don't let local,
uncommitted changes pile up across multiple tasks. This is the same
discipline that caught real problems earlier this project (the `.uproject`
surprise, the disconnected-database saga) — verify and push often, not
just at the end of a whole session.

---

## Track A — Claude Code (sequential, in this order)

**1. Traversal fix** (`DWM_Traversal_Fix_Task.md`)
Diagnose and fix jump, stair-climbing, and door interaction. Two likely
separate causes: a Day 18 input regression (jump/doors) and a Character
Movement Component Max Step Height mismatch (stairs). Verify trade
terminals still work post-fix.

**2. NPC dialogue — Hank proof-of-concept** (`DWM_NPCDialogue_Task.md`)
DEPENDS ON TASK 1 BEING COMMITTED FIRST — adds E as a second input
consumer alongside trade terminals, needs the traversal fix's input work
settled first. Task 0 fixes Hank's T-pose (basic idle only, NOT the
fuller Week-9-deferred scripted loop). Tasks 1-6 build the actual
dialogue display system (6 states, UMG panel) for Hank specifically.
Explicitly does NOT solve dialogue+terminal co-location for the other six
NPCs — that's flagged as future work, not scoped here.

**3. Add `engineering_services` resource** (`DWM_EngineeringServices_Task.md`)
DEPENDS ON TASK 2 BEING COMMITTED FIRST (not a hard technical dependency,
but keeps the chain disciplined). First real touch of the guardrail files
this project. Confirm `TradeSettlementService.cs` needs zero changes
before assuming so. Swaps Hillside's terminal from Timber to
`engineering_services`. Re-verify ALL 5 terminals after, not just the
changed one. Update Reya/Hank dialogue per the task doc.

**4. Add Valley food resources** (`DWM_ValleyFood_Task.md`)
DEPENDS ON TASK 3 BEING COMMITTED FIRST — second touch of the guardrail
files; if Task 3 changed anything about how resources are added, apply
the same pattern here for consistency. THREE OPEN QUESTIONS need answers
before/during this task (see the doc's own top section): fruit resource
naming, whether food is a checklist or a menu of options, and whether
Valley's asymmetric terminal structure (1 hardcoded + 4 placed) is
acceptable. Re-verify ALL 9 terminals after (5 from Task 3's state + 4
new).

---

## Track B — Codex (sequential, in this order)

**1. Level Transition System — Mountain ↔ Hillside** (`DWM_LevelTransition_Task.md`)
Implements the Week 3 checkpoint (Day 24 Phase 3a gate dependency).
Mountain and Hillside are now visually distinct (TownShops reversal), so
lean on a disguised diegetic transition point, not visual continuity.
Bidirectional, both directions PIE-tested. Confirm existing Mountain
functionality (Hank, trade terminal, turbine) isn't regressed.

**2. Narrative Navigator evaluation** (`DWM_NarrativeNavigator_Eval_Task.md`)
HARD half-day cutoff, per SCOPE.md's existing accelerator rule — report
actual time spent honestly. Quick compatibility scan first (does it even
fit the separate-levels architecture — waypoints/compass, not a
continuous minimap), then the timed integration attempt. Clear bail
conditions listed in the doc; if any hit, drop it and ship the simple
version, no exceptions to the cutoff.

---

## What's explicitly NOT covered by either track

- The multi-NPC dialogue+terminal co-location problem (Task 5 in the NPC
  dialogue doc) — flagged as future work once Track A Task 2 proves the
  pattern on Hank.
- Character assets for the other six NPCs (Reya, Owen, Lena, Marisol,
  DeShawn, Mike, Kai) — still unassigned, not blocking any current task
  but will eventually need deciding.
- The fuller Hank scripted movement loop (turbine-check-and-back +
  wander) — deliberately deferred to Week 9.
- The terminal mesh swap (gold cube → QuadArt Trader Place) — also
  deferred to Week 9.

## Reporting back

Both tracks should report using the same evidence standard established
this project: actual query results/screenshots, not just claims of
"done." State explicitly what's committed vs. still local. If a task's
open questions (Task 3/4 for engineering_services and Valley food
specifically) weren't resolved before implementation, flag that clearly
rather than silently picking an answer.
