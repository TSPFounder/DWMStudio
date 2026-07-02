# WEEK1_FRESH_CLONE_GATE.md — Friday Gate Checklist

Per the MVP schedule, Day 4's gate is: **fresh clone → build → tracer round-trip on BOTH
DWM and DWM_Dev.** This is a different check than `preflight.ps1` — preflight verifies your
*current* environment is healthy; this gate verifies the *repo* is complete enough that a
clean checkout builds and runs without any of today's local fixes silently missing.

**Honest scope note:** running this on the SAME machine proves *repo completeness* (nothing
required is uncommitted), not *environment portability* (a different machine would still
need UE 5.3 installed, Smart App Control off, .NET 8 SDK present, etc. — those are captured
in RUNBOOK.md/preflight.ps1, not this repo). If you want a true portability proof, this needs
running on a second machine or a clean VM eventually — not required for Friday's gate, worth
knowing as a limit of what passing this actually proves.

## Step 0 — Confirm what SHOULD be committed actually is

Before cloning anything, from your current working copies:

```
cd C:\DreamWorldMaker\Repos\DWMStudio
git status
```

Should be **clean** (nothing uncommitted) before you trust a fresh clone to prove anything.
If it's not clean, commit or stash first — testing against uncommitted changes defeats the
point of the gate.

**Specifically confirm these are tracked** (not accidentally gitignored or forgotten) —
`git ls-files | findstr /I "<name>"` for each, or just check they show up in `git status`
history / `git log --stat` for recent commits:

- [ ] `economy_schema.sql`, `EconomySeeder.cs` (DWM.Shared/Economy/)
- [ ] The Dashboard button wiring (`DashboardViewModel.cs`, `DashboardView.xaml` changes)
- [ ] `DWMStudio.Tests/` — the whole test project, including `.csproj`, and BOTH
      `EconomyLedgerInvariantTests.cs` and `RandomizedEconomyLedgerInvariantTests.cs`
- [ ] `DWMStudio.slnx` reflects the new test project reference
- [ ] `SCOPE.md`, `AGENT_LOG.md`, `ARCH.md`, `RUNBOOK.md`, `PROMPTS.md`, `preflight.ps1`
- [ ] The `WorldPackageExporter` caller that Claude Code added (whatever file/button invokes
      `WritePendulum` — confirm it's not sitting only in an untracked file)

Do the same check on the DWM_Dev side:

```
cd C:\DreamWorldMaker\Repos\DWM_Dev
git status
```

- [ ] **The `.uproject` with SQLiteCore + SQLiteSupport enabled** — this is the single most
      important one to verify. If this fix is only on-disk and never committed, a fresh
      clone reproduces the ENTIRE two-day module-load failure from scratch. Check it
      explicitly: `git log -p -- DWM_Dev.uproject` or open the file after a fresh clone and
      confirm the Plugins array still has both entries `"Enabled": true`.
- [ ] `DWM_Dev.Build.cs` with the SQLiteCore/SQLiteSupport dependency lines
- [ ] Any source changes from the pendulum-caller fix

## Step 1 — Fresh clone (both repos)

Clone to NEW folders — do not overwrite your working copies, so you have a fallback if
something's genuinely missing:

```
git clone <DWMStudio repo url> C:\DreamWorldMaker\GateCheck\DWMStudio
git clone <DWM_Dev repo url>    C:\DreamWorldMaker\GateCheck\DWM_Dev
```

## Step 2 — Build DWMStudio from the fresh clone

```
cd C:\DreamWorldMaker\GateCheck\DWMStudio
dotnet build
cd DWMStudio.Tests
dotnet test
```

- [ ] Builds with no errors.
- [ ] All 5 (or however many exist by Friday) tests pass — this proves the ENTIRE test
      project, including this week's new files, actually made it into the repo correctly.

## Step 3 — Build DWM_Dev from the fresh clone

Follow RUNBOOK.md §2–3 exactly, on the fresh clone:

- [ ] Right-click `DWM_Dev.uproject` → *Generate Visual Studio project files* (or the UBT
      command-line equivalent from RUNBOOK.md if the file association is still flaky).
- [ ] Open the generated `.sln`, Development Editor / Win64, Rebuild DWM_Dev.
- [ ] **Builds AND loads with no module-load dialog on first try.** This is the real test —
      if the `.uproject` plugin fix from Step 0 wasn't actually committed, THIS is where
      you'd find out, by reproducing Tuesday's entire saga on a fresh clone. If that
      happens, don't re-debug it — you already know the fix (RUNBOOK.md §4, row 1); just
      confirm it's applied and commit it properly this time.

## Step 4 — Tracer round-trip on the fresh clone

- [ ] Run whatever caller Claude Code wired up to export `pendulum.db` — confirm it lands at
      `DWM_Dev\Content\Databases\pendulum.db` inside the FRESH clone's folder (not pointing
      at your old working copy by an absolute path left over from testing).
- [ ] Close DWMStudio fully.
- [ ] Launch the fresh-clone DWM_Dev editor, PIE, confirm the pendulum moves.

**If any exported-path reference is hardcoded to your original `C:\DreamWorldMaker\Repos\...`
working copy rather than being relative/configurable**, that's worth knowing now — it means
the export path isn't truly portable, which matters less today (you'll always use the one
machine) but is worth a one-line note in SCOPE.md if so, rather than a surprise later.

## Step 5 — Economy package round-trip on the fresh clone (bonus, same idea)

- [ ] Run "Export Economy Package" from the fresh-clone DWMStudio.
- [ ] Confirm `economy.db` appears with 5/10/24 rows (Communities/Resources/CommunityResources)
      as before — same reasoning as Step 4.

## Step 6 — Clean up and record the result

- [ ] Delete `C:\DreamWorldMaker\GateCheck\` once done (or keep it briefly if you want a
      second reference copy — your call).
- [ ] Add one line to SCOPE.md's decisions log: pass/fail, and if anything was found
      uncommitted, what it was and that it's now fixed and committed.
- [ ] If everything passed cleanly: tag the checkpoint —
      `git tag week1-gate-passed && git push --tags` (both repos) — and write the Week 2
      kickoff note (already drafted — `WEEK2_KICKOFF.md` — read it, adjust if anything from
      the gate changes the picture, commit it).

## If something's missing

That's the gate doing its job, not a failure of the week. Fix it, commit it properly, and
**re-run the fresh clone step for whatever was affected** — don't just trust the fix without
re-proving it from a clean checkout, since "I fixed it locally" is exactly the state that
caused the gap in the first place.
