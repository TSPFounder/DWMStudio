# RUNBOOK.md — DWM_Dev Build, Export & Open Procedures

The exact sequence to build, export, and run the DWMStudio → SQLite → UE pipeline, plus
recovery procedures for every failure mode hit so far. When something breaks, check the
Failure Modes table before debugging from scratch — most of these cost hours the first time
and minutes the second.

**Environment:** Windows 11 · UE 5.3.2 (Launcher install, `C:\Program Files\Epic Games\UE_5.3`)
· .NET 8 · MATLAB R2025b · Repos at `C:\DreamWorldMaker\Repos\` (`DWM_Dev`, `DWMStudio`).
Smart App Control: OFF (was blocking UBT-compiled DLLs; note: re-enabling requires a Windows
reinstall — leave off on this dev machine).

---

## 1. The Core Loop (happy path)

1. **Pre-flight:** run `preflight.ps1` (checks plugins, SDK, processes, paths). Fix anything red.
2. **Build DWMStudio** (.NET): open `DWMStudio.slnx` → build, or `dotnet build` in the repo.
3. **Export the world package** from DWMStudio → produces the `.db`.
4. **CLOSE DWMSTUDIO COMPLETELY.** It holds the SQLite file handle after export; UE gets
   `disk I/O error` on `FSQLiteDatabase::Open` if DWMStudio is still running. Non-negotiable.
5. **Open DWM_Dev in UE 5.3** (from Visual Studio's green ▶ with DWM_Dev as startup project,
   or the `.uproject`).
6. **PIE** → the reader opens the `.db` in `OnStart()` and spawns actors from package data.

## 2. Building DWM_Dev (UE side) in Visual Studio

- Open `DWM_Dev.sln`. Config **Development Editor / Win64**. Startup project **DWM_Dev**
  (under Games — right-click → *Set as Startup Project*; if the toolbar says
  "UnrealBuildTool," the green button runs the wrong thing and prints a help screen).
- **Right-click DWM_Dev → Build** (or Rebuild). Do NOT "Build Solution" — Epic's bundled C#
  tooling projects (EpicGames.OIDC, BuildGraph.Automation, AutomationScripts) fail with
  NuGet/framework errors on this machine. **Those errors are engine tooling, not ours —
  ignore them.** Only the `2>` DWM_Dev section of the Build Output matters.
- **Beware "Target is up to date" in ~0.5s** — that is a fake success (nothing compiled).
  If in doubt, use **Rebuild**, which forces a full compile.
- Success = `Binaries\Win64\UnrealEditor-DWM_Dev.dll` (+ `.pdb`, `DWM_DevEditor.target`,
  `UnrealEditor.modules`) with a fresh timestamp. (`UnrealEditor.modules` is the CORRECT
  name — it is a per-platform manifest, not per-module.)

## 3. Clean Rebuild Procedure (when builds/loads act haunted)

1. Close the editor AND Visual Studio. Check the system tray for lingering UE processes.
2. Delete from `DWM_Dev\`: `Binaries`, `Intermediate` (add `Saved`, `DerivedDataCache`, `.vs`
   for the full nuke — all regenerable, all gitignored).
3. Regenerate project files (right-click `.uproject` → *Generate Visual Studio project
   files*; if that menu item is missing, run):
   ```
   "C:\Program Files\Epic Games\UE_5.3\Engine\Binaries\DotNET\UnrealBuildTool\UnrealBuildTool.exe" -projectfiles -project="C:\DreamWorldMaker\Repos\DWM_Dev\DWM_Dev.uproject" -game -engine -progress
   ```
   (`GenerateProjectFiles.bat` does NOT exist in Launcher installs.)
   To restore the right-click menu permanently:
   ```
   "C:\Program Files\Epic Games\UE_5.3\Engine\Binaries\Win64\UnrealVersionSelector.exe" /fileassociations
   ```
4. Open the fresh `.sln` → Rebuild DWM_Dev → launch from VS.
5. Note: after deleting `.vs`, the solution shows "(not found)" projects until step 3 runs.

## 4. Failure Modes (in the order they were earned)

| Symptom | Cause | Fix |
| --- | --- | --- |
| "The game module 'DWM_Dev' could not be loaded" + log spams `Looked in:` engine folders | A module in Build.cs whose **plugin isn't enabled in the .uproject** (this was SQLiteCore/SQLiteSupport). Builds fine, won't load. | Every Build.cs dependency provided by a plugin must have an `"Enabled": true` entry in the `.uproject` Plugins array. |
| Build "succeeds" in 0.5s, nothing changes | "Target is up to date" — no compile happened | Rebuild (not Build); or delete Binaries+Intermediate first |
| `UnrealBuildTool.dll not found` / MSB3073 exit code -1 | Broken/incomplete engine install | Epic Launcher → UE 5.3 → **Verify** (quiet; done when button returns to "Launch") |
| `Application Control policy has blocked this file (0x800711C7)` on MarketplaceRules.dll etc. | Windows Smart App Control blocking UBT's freshly-compiled rules DLLs | Turn Smart App Control OFF (one-way switch) + reboot; delete `C:\Users\henry\AppData\Local\UnrealEngine\Intermediate\Build\BuildRules\` so the blocked DLLs recompile |
| `disk I/O error` on FSQLiteDatabase::Open | DWMStudio still holds the .db handle | Close DWMStudio fully before UE touches the file |
| "Missing or built with a different engine version: DWM_Dev — rebuild now?" | DLL/.modules hash mismatch (stale artifacts) | Yes is safe; if it loops, do the Clean Rebuild Procedure |
| Editor exits after a long-idle module dialog | The dialog just sat open (it blocks; timestamps in the log jump hours) | Nothing is hung "in the background" — close it and fix the underlying cause |
| OIDC / BuildGraph / AutomationScripts errors in the Error List | Epic's bundled C# tooling, broken NuGet refs | Ignore. Filter Error List to "Current Project (DWM_Dev)" |
| Right-click "Generate Visual Studio project files" missing | Lost `.uproject` file association | UBT `-projectfiles` command above; `/fileassociations` to restore the menu |

**Escalation order when stuck:** full project clean (§3) → search `Saved\Logs\DWM_Dev.log`
for the FIRST `DWM_Dev`/`Failed`/`Fatal` mention (not the bottom) → post log + Binaries
listing to the Unreal community → engine reinstall LAST (the engine is rarely the problem
once it builds).

## 5. SQLite Conventions (the ledger path)

- Plugins: engine built-in **SQLiteCore + SQLiteSupport** (both in `.uproject` AND Build.cs).
  `USQLite` marketplace plugin stays **disabled**.
- Open in **OnStart()** override, never `GameInstance::Init()` (`GetWorld()` is null pre-level).
- Open **ReadOnly** with an absolute `FPaths::…` path; log the resolved path.
- **By-name binding everywhere**: `TEXT("$col")`. Never by index — bind is **1-based**,
  column read is **0-based** (verified in 5.3 headers); mixing them corrupts silently.
- Check every `Step()` against Row/Done/Error; log `GetLastError()` on failure. The
  network-sum-zero invariant depends on no unchecked calls.
- **Packaging (before Week 9):** add to `DefaultGame.ini`:
  `+DirectoriesToAlwaysStageAsNonUFS=(Path="Databases")` — the .db does not auto-package
  in 5.3 and Open fails *silently* in shipped builds.

## 6. MATLAB Co-Simulation Notes (R2025b ↔ UE 5.3)

- R2025b requires **UE 5.3** for co-sim — this is why the engine version is frozen.
- MathWorks plugins install to `UE_5.3\Plugins\Marketplace\Mathworks`; once wired, the
  project opens **from MATLAB/Simulink**, not by double-clicking the `.uproject`.
- Known bug: R2025b **Update 2** plugin-version mismatch ("Incompatible version of 3D
  Simulation engine: 25.1.0"). If co-sim fails with that error, it's MathWorks' bug, not ours.
- Simscape STL visual paths are **relative** and break when a model moves — fix inside each
  body subsystem.
- Company licensing: the founder's MATLAB **Home license does not cover business use**;
  MathWorks **Startup program** application must precede campaign use of MATLAB-derived
  content (see business plan §4.4).

## 7. Golden Demo Scenario & Demo Reset (Day 13)

- **Fixture:** `DWMStudio.Tests/Fixtures/golden_world_economy.db` — a committed, already-
  exported world package. Starting state: 5 communities/10 resources/24 CommunityResources
  (Day 5 seed, unmodified), StoneLedger **empty** (the demo's live trade is meant to visibly
  be the first), and calibrated per-community Dollar Vault balances (Mountain 4200, Hillside
  4400, Valley 4600, Suburb 4000, City 5000 — all against the shared $500 threshold). All 5
  communities read `Healthy` at this starting point.
- **Demo arc:** City's descent into `CascadingFailure` is a LIVE, in-take action, not baked
  into the fixture — run `CityCascadingFailureScenario` (DWM.Shared/Economy) against a working
  copy of the golden .db's source economy.db to drive City from $5000 to $400 (below the $500
  threshold) via 3 scripted debits. That's 3 demo actions to visibly flip City's failure state
  — reused as-is from Day 11, not recalibrated.
- **Reset before each take:** copy the golden `.db` back over the working world package file
  UE reads (`Databases\...`) to restore the exact starting state, then re-run the live demo
  trade/failure sequence fresh.
- **Regenerate the fixture** (only when the scenario changes ON PURPOSE):
  `dotnet run --project DWMStudio.WorldPackageCli -- export --out DWMStudio.Tests/Fixtures/golden_world_economy.db --world-id dwm_golden_demo`
  (omit `--economy-db` to use the canonical `GoldenEconomyScenario` default). Update
  `GoldenWorldPackageTests.cs`'s assertions in the same change — they'll fail otherwise, which
  is the point: that test exists to catch an accidental drift in the starting scenario.

## 8. Economy Export Procedure & the Phase 1 Gate (Day 14)

**What it produces:** a world-package `.db` (separate file from `pendulum.db` — see §7)
containing `WorldInfo`, `Communities`, `Resources`, `CommunityResources`, `StoneLedger` (full
trade history), `CommunityDollarVault` (current per-community balance/threshold), and
`CommunityFailureStatus` (current Healthy/CascadingFailure per community). Written by
`WorldPackageExporter.WriteEconomySnapshot` (Day 12); `WriteEconomySnapshot` READS an existing
economy.db and exports its current state, it does not create or seed one.

**Two ways to produce it:**
- `dotnet run --project DWMStudio.WorldPackageCli -- export --economy-db <path-to-economy.db> --out <path> [--world-id <id>]`
  — exports whatever state that specific economy.db is currently in (live/authored data).
- `dotnet run --project DWMStudio.WorldPackageCli -- export --out <path>` (no `--economy-db`)
  — exports the canonical golden demo scenario instead (§7), generated fresh from code.

**The Phase 1 gate** (`DWMStudio.Tests/Phase1GateTests.cs`) is the automated proof that the
whole economy stack (Days 5-13) works as one continuous flow, not just as isolated unit tests.
Run it on its own with:
```
dotnet test --project DWMStudio.Tests --filter "FullyQualifiedName~Phase1GateTests"
```
In one headless run it: seeds the golden scenario → settles one trade via
`TradeSettlementService` (the direct path) AND one via `TradeRequestRepository`'s
Propose/Settle lifecycle (both paths that exist, not just one) → runs
`CityCascadingFailureScenario` and confirms City actually reaches `CascadingFailure` in this
run → confirms the network-sum-zero invariant over the resulting ledger → exports the result
→ opens the exported `.db` with a brand-new read-only connection (simulating what UE's
`FSQLiteDatabase` will eventually do) and confirms City's failure state and both settled
trades are present and correct in the export, not just that the file opens. Ran clean on first
write and repeatedly on rerun — no bugs or workarounds were needed to make it pass.

**Gotcha worth flagging (a documentation trap, not a bug):** the network-sum-zero invariant is
easy to check WRONG by summing `StoneLedger.Amount` directly — that column is always positive
(`Amount > 0` is a schema `CHECK`), so a naive `SUM(Amount)` is never zero and proves nothing.
The correct check is each community's **net** (sum of `Amount` where it's `ToCommunityId`,
minus sum where it's `FromCommunityId`), summed across all communities — see
`EconomyLedgerInvariantTests.cs`'s `NetworkSum` query or `Phase1GateTests.cs`'s
`perCommunityNet` for the two ways this project already computes it correctly.

**Scope note (Day 15):** the Phase 1 gate above DOES export a non-empty StoneLedger, but
neither of its two trades uses a null `ResourceId` — so the null-vs-populated
`ResourceId`/`Quantity` round-trip through the export loop was still untested until
`WorldPackageExporterEconomyTests.cs` (Day 15) added a dedicated test with a Stone-only trade
alongside two resource-attached ones. The gate proves the INTEGRATED FLOW works; it isn't a
substitute for exporter-specific edge-case coverage.

## 9. Building DWMStudio against the CAD Libraries (2026-08-06)

DWM.Shared references `CAD_Library` and `FusionLibrary`. Both must be checked out **beside**
the other repos, which is the layout `FusionLibrary.csproj` already assumed:

```
Repos\CAD_Library\
Repos\FusionLibrary\
Repos\DWM.Shared\
Repos\DWMStudio\
```

A checkout elsewhere fails at restore naming the path it wanted, which is the right way for
this to break.

### DO NOT DELETE `CAD_Library\CAD_Library\lib\`

Three assemblies live there — `ApplicationLibrary.dll`, `MathematicsLibrary.dll`,
`SystemsEngineeringLibrary.dll` — and **they cannot be rebuilt from a clean checkout.**

`CAD_Library` → SystemsEngineeringLibrary, ApplicationLibrary, MathematicsLibrary
`SystemsEngineeringLibrary` → **CAD_Library**, ApplicationLibrary, MathematicsLibrary
`ApplicationLibrary` → SystemsEngineeringLibrary

That is a cycle. MSBuild cannot express it with `ProjectReference`, which is why these are
assembly references and not a mistake. Building SystemsEngineeringLibrary from source fails
with 91 errors, all of them CAD_Library types.

They used to live in `bin\Debug\net10.0`, so **the ordinary clean-rebuild reflex destroyed
them** on 2026-08-06 and cost an evening. `lib\` is not a build output; nothing cleans it. It
is committed for the same reason.

### The clean rebuild, when the C# side acts haunted

```bash
cd /c/DreamWorldMaker/Repos
find CAD_Library FusionLibrary DWM.Shared DWMStudio -type d \( -name bin -o -name obj \) -prune -exec rm -rf {} +
cd DWMStudio && dotnet build --no-incremental
```

Safe **only** because `lib\` sits outside `bin\`. Before 2026-08-06 this command was
destructive.

### Failure modes earned on 2026-08-06

| Symptom | Cause | Fix |
| --- | --- | --- |
| `MSB4025: An XML comment cannot contain '--'` | A double hyphen inside a `<!-- -->` in a `.csproj` or `.xaml`. Legal in C#, illegal in XML, and it takes the **whole project file** down | Use a comma or an em dash. `ProjectFilesAreWellFormedTests` now catches it in the suite |
| A type added minutes ago reports `CS0246`, while its neighbours resolve | Two assemblies claiming one identity — a stale DLL winning over the project just built. Was caused by `CAD_Library.csproj` referencing its own output | Fixed; if it recurs, look for a `<Reference>` whose `HintPath` points into any `bin\Debug` |
| `CS0104: 'Expression' is ambiguous` / `CS0266: double? to double` in CAD_Library | A **newer** MathematicsLibrary. `Expression` arrived 2026-04-02, `Vector.X_Value` became nullable 2026-04-14; CAD_Library's source predates both | Keep the pre-April build in `lib\` (66048 bytes). Migrating CAD_Library forward is real work, done deliberately |
| `CS0117: does not contain a definition for X` on a member you just wrote | The member is `internal`, and DWM.Shared declares no `InternalsVisibleTo` | Make it `public`. The error reads as missing, not as out of reach |
| Tests reference types that exist in the source | A sibling repo is behind. `git pull` in **DWM.Shared**, not only DWMStudio | Check `git log --oneline -1` in each repo before diagnosing anything else |

That last row is the expensive one. Several hours on 2026-08-06 went into diagnosing errors
against a `DWM.Shared` that was four commits behind, and two `git am` calls that had silently
failed because the patch files were not where the command looked. **Confirm the tree before
diagnosing the build.**

### Patches for the sibling repos

`CAD_Library` and `FusionLibrary` are outside the agent session's authorized repository set,
so changes to them arrive as patches in `DWMStudio/tools/patches/` rather than as commits.
`git am` prints nothing on success — always follow it with `git log --oneline -1`.

---

## 10. Driving Fusion (the `fusion` command)

```bash
cd /c/DreamWorldMaker/Repos/DWMStudio
dotnet run --project DWMStudio.WorldPackageCli -- fusion revolve --dry-run   # no Fusion needed
dotnet run --project DWMStudio.WorldPackageCli -- fusion ping
dotnet run --project DWMStudio.WorldPackageCli -- fusion revolve
dotnet run --project DWMStudio.WorldPackageCli -- fusion massprops
dotnet run --project DWMStudio.WorldPackageCli -- fusion export --out C:\Temp\x.step --format step
```

**Preconditions:** Fusion open, the intended document **active**, and no dialog showing.
Nothing outside Fusion can make it open a file — the add-in works on whatever has focus.

**Load the add-in first:** Utilities → Scripts and Add-Ins → **Add-Ins** tab →
`DWM_FusionAddIn` → Run, then **close that dialog**. It is modal and blocks the add-in it just
started. Tick *Run on Startup* to avoid repeating this.

| Symptom | Meaning |
| --- | --- |
| Refuses immediately | Port not bound. Fusion closed, or the add-in not loaded. `netstat -ano \| grep 18750` settles it |
| Hangs | Port bound, main thread blocked. A dialog is open somewhere |
| `[fusion] OK.` | The add-in answered through the main thread, so Fusion is genuinely responsive |

`revolve` builds a hollow tube into the **active** document — use File → New Design first, or
it lands in whatever you had open. It then reads the mass properties back and checks them
against closed form. Expect ρ ≈ 7850, a centre of mass at ±0.25 m on one axis, and an axial
`I/m` ratio of 1.0.

**Inertia is reported about the document origin, not the centre of mass.** Subtract the
parallel-axis term before handing it to Simscape. See TOOLING.md step 6.

---

## 11. Housekeeping

- Log build-system incidents in AGENT_LOG.md; scope-affecting decisions in SCOPE.md's log.
- `UE_Library5_7` was an obsolete repo (deleted). If any tool "finds" a 5.7 project, it is
  working in the wrong place — the real sandbox is `DWM_Dev` on 5.3.
