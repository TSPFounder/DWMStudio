# ARCH.md — Architecture Decisions

## SQLite Concurrency & Journal Mode (DWMStudio ↔ UE handoff)

**Status: DRAFT — for your review and confirmation (Day 3 task, §4.2/ARCH.md).**

### Decision: default rollback-journal (DELETE) mode. Do NOT enable WAL. Single-writer/
### single-reader enforced by process discipline, not by SQLite's concurrency features.

### Context

DWMStudio (.NET writer) and DWM_Dev/UE (C++ reader via `FSQLiteDatabase`, opened
`ReadOnly`) are separate processes on the same machine, accessing the same `.db` file —
never over a network filesystem. `WorldPackageExporter.WritePendulum`/`WriteEconomy`
**delete and recreate the file on every export** (`File.Delete` then a fresh
`ReadWriteCreate` open). The RUNBOOK already mandates that DWMStudio be **fully closed**
before UE opens the file (a file-handle rule learned the hard way — see RUNBOOK §4).

That access pattern is **sequential handoff, not concurrent access**: the writer finishes
completely and closes, then the reader opens. At no point do a writer and a reader touch
the file at the same time by design.

### Why NOT WAL, despite it usually being the "obviously correct" SQLite choice

WAL mode's entire value proposition is letting readers and writers run **concurrently**
without blocking each other — multiple readers, one writer, all live at once. DWM's
pipeline doesn't have that scenario: it's single-writer-then-close, single-reader-after.
WAL would add real complexity for zero benefit here:

- WAL requires companion `-wal` and `-shm` files alongside the main `.db`. The latest data
  can live in the `-wal` file, not yet transferred to the main file.
- **"Never copy/recreate a WAL-mode database by touching just the main .db file"** — exactly
  the kind of footgun that produces confusing, intermittent-looking failures (the same
  *category* of problem — a file that looks fine but isn't in the state a process expects —
  that cost real time this week with the engine/plugin issues). A delete-and-recreate
  exporter is precisely the operation that needs care under WAL if the `-wal`/`-shm`
  lifecycle isn't handled explicitly.
- WAL requires shared memory between processes and only works when all processes are on the
  same machine (satisfied today) — but it's one more environmental assumption baked in for a
  concurrency benefit we don't use.
- The default rollback-journal mode requires no companion files, no checkpoint timing, and
  is the simplest correct choice for "one process writes and fully closes, then another
  process reads."

### What actually enforces correctness here

Not a journal mode — **process discipline**, already written down:
1. DWMStudio completes its write transaction and closes fully (releases the file handle).
2. UE (or any reader) opens the file `ReadOnly` only after DWMStudio is confirmed closed.
3. `preflight.ps1` checks for a running DWMStudio process before the pipeline runs, as a
   guardrail against violating rule 1.

### When to revisit this

- If the pipeline ever needs a writer and a reader open **at the same time** (e.g. live
  telemetry while DWMStudio stays open) — that's a genuine concurrency need WAL is built for.
- Note: the post-MVP shared/persistent world (Phase A3, Post-MVP Plan) moves multiplayer
  state to a **hosted backend database** (Path 1, backend-authoritative), not this local
  `.db` file — so this decision may simply become moot rather than needing to change.

### Confirm before finalizing

- [ ] Confirm the delete-and-recreate export pattern is intended to continue (vs. an
      incremental-update pattern, which would change this analysis).
- [ ] Confirm no other reader/writer (e.g. a future dashboard, a second tool) will ever hold
      the file open concurrently with DWMStudio during MVP.
- [ ] X's second-opinion review (Day 4 task) — flag this recommendation for that review.
