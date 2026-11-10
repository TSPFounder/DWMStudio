# UAV Monitoring and Assistance: execution package for PD Quick

This folder carries out [the agent coordination plan](UAV-Agent-OOSEM-Plan.md) as far as it can be carried out without the engineering applications, and contains the scripts that do the rest on the Windows machine that has them.

**Read this first.** The package was built in a Linux cloud session. That session had the PD Quick source (commit `32306b6`) and could build it and run its tests. It had no MATLAB, Simulink, UModel, Fusion, Unreal Engine, Word, Excel or DATCOM, and no access to `F:\PDQuick` or `%APPDATA%\PD Quick\PDQuick.db`. So nothing here has touched your project database or produced a native model yet. The table says exactly what was checked and how.

## Results reported from the Windows machine (10 November 2026)

The owner ran these in MATLAB and pasted the output. Two scripts needed fixes found on first contact, both now in this folder.

| Step | Result |
|---|---|
| R2025b import of the R2011a subsystems | `UAV_T01_BACK_TO_BACK_PASS release=2025b worst_continuous_difference=0`. This needs the R2011a result file, so the R2011a build and self-test ran first. |
| Simscape power and propulsion | `UAV_SIMSCAPE_PASS release=2025b`: 1195.960 rad/s and 2.9899 A, equal to the closed form. Fix: the converter's port domains are detected instead of assumed. |
| Unreal co-simulation | `UAV_T01_UE_PASS release=2025b`, all eleven checks. Fix: the scene block's sample time (`Ts`) is set to the model's 0.05 s step. |
| Fusion multirotor candidate | Script ran to completion ("it worked"). Fix: the root component is no longer renamed. Mass figures and whether the drawing was saved were not reported. |
| DATCOM fixed-wing candidate | Case ran after the comment cards were removed (the 2007 executable rejects them). At 0 deg: CL 0.294, CLa 5.59 /rad, CMa -1.11 /rad, so a static margin near 20 % of mean chord; CNB +0.030 /rad. See `datcom/uav_vtol_fixed_wing_NOTES.txt` for why drag and stall are not to be relied on. |

Not yet reported back: seeding and export output, UModel diagrams, requirement links. The table below describes the state at delivery and has not been edited.

## Status against the ten requested items

| # | Item | State | Evidence |
|---|---|---|---|
| 1 | Scenarios with an OOSEM approach | Done | `baseline/uav_baseline.json`: 6 stakeholders, 16 needs with the storyboard wording, 5 excluded scenes, 6 use cases, 6 mission and 14 system requirements, measures, system hierarchy, WBS, verification, task ledger. The generator refuses to write if a need is orphaned, a requirement lacks allocation or verification, or a link names something that does not exist. |
| 2 | PD Quick and its databases | Tool tested, not yet run on your database | `UavOosemSeed` (in the PD Quick change, below) writes the baseline through PD Quick's own stores. Five tests pass against a temporary database. Your live database is untouched until you run `seed`. |
| 3 | Updates to PD Quick | Written and tested, **not pushed** | One new tool, `PDQStudio/tools/UavOosemSeed`, plus tests. This session was refused write access to the PD-Quick repository, so the change is a patch file for you to apply. |
| 4 | R2011a Simulink models with WBS Subsystem blocks, pulled into R2025b | Scripts written, logic executed in a stand-in, **not run in MATLAB** | `matlab/r2011a` builds `uav_wbs_lib.mdl` (18 Subsystem blocks, one per WBS element, tagged with the WBS code) and `uav_t01_mission.mdl`. `matlab/r2025b/uav_r2025b_import_and_test.m` copies the subsystems into an `.slx` and compares the two releases signal by signal. |
| 5 | V&V toolbox in R2011a with UModel, Word and Excel | Scripts written, **not run** | `uav_link_requirements.m` puts three links on each allocated subsystem: PD Quick (`rmipdqlinktype`), Excel and Word. `UavOosemSeed umodel` creates requirement diagrams with deriveReqt, satisfy and verify. |
| 6 | Fusion 3D models and drawings | Script written, **not run** | `fusion/UavMultirotorCandidate`: four components, eight occurrences, mass properties, STEP and STL. The drawing is started, not finished: Fusion has no API for drawing views. |
| 7 | Simscape for systems and components | Script written, **not run** | `uav_build_power_simscape.m` (R2025b): `ElectricalPower` and `Propulsion` physical subsystems, checked against a closed-form steady state. |
| 8 | Simulink with Unreal Engine | Script written, **not run, least certain** | `uav_build_t01_ue.m` (R2025b) adds the Simulation 3D scene and UAV blocks to the mission model. |
| 9 | Only subscription tokens | Met | No API key, paid service or external model was used. Nothing in the package calls one. |
| 10 | Datcom and other libraries | Partly | `datcom/uav_vtol_fixed_wing.dcm` is a Digital DATCOM input deck for the fixed-wing candidate, and the seeder creates the DATCOM model record through `DatcomAnalysisCatalog`. The record's namelists are not filled in through DatcomLibrary's classes, and the deck was not run. |

### What "executed in a stand-in" means for item 4

`matlab/tests/uav_mock_tests.m` runs the real build scripts under GNU Octave against a stand-in for `new_system`, `add_block`, `add_line`, `set_param` and `find_system`. The stand-in rejects a missing block, a port that does not exist and a doubly driven input, then integrates the recorded diagram with fixed-step RK4. The result matches the hand-calculated answers in `uav_t01_expected.m`:

| Quantity | Simulated | Hand calculation |
|---|---|---|
| Ground-truth boundary crossing | 40.05 s | 40.00 s |
| Fixed camera node first detection | 45.05 s | 45.00 s |
| UAV first sighting outside the enclosure | 73.45 s | 73.41 s |
| Recipient notified | 50.05 s | 50.00 s |
| Notification latency | 10.00 s | 10.00 s |
| State of charge after 300 s | 0.753333 | 0.753333 |

This shows the wiring and the arithmetic are right. It does not show that R2011a accepts every block parameter name, so the first MATLAB run may still need a small fix. The 0.05 s offsets are one solver step.

These numbers describe a provisional test scenario. They are not requirements and not aircraft performance. In it the fixed node reports the escape 28 s before the patrolling UAV comes round, which is the plan's argument for a Family of Systems.

## Run order on the Windows machine

Set these once in PowerShell. Use your own paths.

```powershell
$pkg = "F:\DWMStudio\UAV_Monitoring"; $pdq = "F:\PDQuick"; $models = "F:\UAV_Models"; $tool = "$pdq\PDQStudio\tools\UavOosemSeed\UavOosemSeed.csproj"
```

**Step 1. Apply the PD Quick change** (skip if it has been pushed and pulled).

```powershell
git -C $pdq am "<path to>\0001-Add-UavOosemSeed.patch"
```

```powershell
dotnet test "$pdq\PDQStudio\PDQStudio.Tests" --filter "FullyQualifiedName~UavOosemSeeder"
```

**Step 2. Seed the project.** Create a project named *UAV Monitoring and Assistance* on the PD Quick Dashboard, close PD Quick, then:

```powershell
dotnet run --project $tool -- projects
```

```powershell
dotnet run --project $tool -- seed --baseline "$pkg\baseline\uav_baseline.json" --project <id from the previous command>
```

Expect `SEED_OK ... created=98` on the first run and `created=0` on a second. The database is copied to `PDQuick.db.before-uav-seed-<time>` first. Open PD Quick and check Missions, Requirements, Systems, WBS and Analysis.

**Step 3. Export the requirement catalogs and copy the Word specification.**

```powershell
dotnet run --project $tool -- export --baseline "$pkg\baseline\uav_baseline.json" --project <id> --out "$models\requirements"
```

```powershell
Copy-Item "$pkg\documents\UAV_Requirements_Specification.docx" "$models\requirements\"
```

**Step 4. UModel diagrams.** Preview, then create (UModel opens):

```powershell
dotnet run --project $tool -- umodel --baseline "$pkg\baseline\uav_baseline.json" --ump "$env:USERPROFILE\Documents\PD Quick\Worlds\<id>\SysML\SystemModel.ump" --dry-run
```

```powershell
dotnet run --project $tool -- umodel --baseline "$pkg\baseline\uav_baseline.json" --ump "$env:USERPROFILE\Documents\PD Quick\Worlds\<id>\SysML\SystemModel.ump"
```

**Step 5. R2011a: build, self-test, link.** In MATLAB R2011a:

```matlab
addpath('F:\DWMStudio\UAV_Monitoring\matlab\common', 'F:\DWMStudio\UAV_Monitoring\matlab\r2011a');
uav_r2011a_build_and_test('F:\DWMStudio\UAV_Monitoring', 'F:\UAV_Models');
uav_link_requirements('F:\DWMStudio\UAV_Monitoring', 'F:\UAV_Models', 'F:\PDQuick\PDQStudio\tools\rmi');
```

Expect `UAV_T01_SELFTEST_PASS release=2011a`, then `UAV_LINKS_OK rows=32 pdq=32 excel=32 word=32`. In PD Quick, Requirements, *Simulink Traceability*, open `uav_wbs_lib.mdl` to see the trace.

**Step 6. R2025b: import, Simscape, Unreal.** In MATLAB R2025b:

```matlab
addpath('F:\DWMStudio\UAV_Monitoring\matlab\common', 'F:\DWMStudio\UAV_Monitoring\matlab\r2025b');
uav_r2025b_import_and_test('F:\UAV_Models', 'F:\UAV_Models\r2025b');
uav_build_power_simscape('F:\UAV_Models\r2025b');
uav_build_t01_ue('F:\UAV_Models\r2025b', 'F:\UAV_Models\r2025b', true);
```

Expect `UAV_T01_BACK_TO_BACK_PASS`, `UAV_SIMSCAPE_PASS` and `UAV_T01_UE_PASS`.

**Step 7. Fusion.** Sign in, select the hub and project, then Utilities, Scripts and Add-Ins, add `fusion\UavMultirotorCandidate`, Run. Finish the drawing dialog and save the drawing as *UAV Airframe Drawing*. Register the documents on the PD Quick Designs tab.

**Step 8. DATCOM.** Copy `datcom\uav_vtol_fixed_wing.dcm` to `for005.dat` beside your Digital DATCOM executable and run it. Read `datcom\uav_vtol_fixed_wing_NOTES.txt` on applicability first.

**Step 9. Register the models in PD Quick.** Apps, MATLAB / Simulink, *Add to DB* for the `.mdl` and `.slx` files; set each Analysis record's model path; set the Unreal project on the Simulations record if you use one.

## Where each script is most likely to need a fix

| Script | Risk | Why |
|---|---|---|
| `uav_build_wbs_library.m`, `uav_build_t01_mission.m` | Low | Ten core block types, executed in the stand-in. Parameter names (`Expr`, `InitialCondition`, `DelayTime`, `InitialOutput`, `SaveFormat`) are from memory of R2011a. |
| `uav_link_requirements.m` | Medium | Uses PD Quick's `pdq_link`, whose own README lists the Excel route and `.xlsx` in R2011a as unverified. The Word link type name and `@bookmark` form follow the documented RMI convention. |
| `uav_r2025b_import_and_test.m` | Low to medium | Relies on R2025b opening an R2011a `.mdl` library and on the Fcn block still being available. |
| `uav_build_power_simscape.m` | Medium | Foundation blocks are found by name and one-port blocks by "the only conserving port", but two-port left/right order and parameter names (`v0`, `R`, `K`, `inertia`, `D`) are from memory. A wrong parameter name prints the block's real ones. |
| `uav_build_t01_ue.m` | High | The Simulation 3D UAV Vehicle block's inputs differ by release and vehicle type. The script stops and prints what it found instead of guessing. |
| `UavMultirotorCandidate.py` | Medium | Standard Fusion API calls; the material library name and the save-then-draw timing vary by version. |
| `UavOosemSeed umodel` | Medium | Same `SysmlDiagramCreator.Create` call PD Quick's smoke tool exercises, with larger diagrams than its templates. |

## Decisions and limits you should know about

- **No threshold is approved.** Every numeric acceptance value is the text `TBD (G2)`, as the plan requires. Model parameters are marked provisional in the baseline and in `uav_params.m`.
- **Bill's and Tom's storyboards were not in the session.** Their six needs use the plan's summaries and are marked so. Four storyboards (Dave, Sally, Kim, Jen) were read and quoted verbatim, including their typos.
- **Three wording problems were found in the storyboards** beyond those the plan lists; see `open_questions` in the baseline.
- **System requirements sit under the Farm mission.** PD Quick stores a requirement under one mission. Derivations that cross missions are in `documents/trace_manifest.csv`.
- **R2011a has no Simscape** on the licence recorded in PD Quick's `TOOLING.md`, so the Simscape model starts in R2025b.
- **Unreal versions differ.** PD Quick's own bridge targets UE 5.8. Simulink's co-simulation uses the engine build MathWorks ships with R2025b and its packaged scenes. The script uses the Simulink route. Driving a PD Quick UE 5.8 project from Simulink would need a separate bridge and is not attempted.
- **The Simscape and mission energy models are not sizing tools.** Linear propeller load, ideal source, cubic power law.
- **Gates G1 to G7 are not passed by this package.** It prepares evidence. People accept needs, approve thresholds and authorize tests and flight.

## Contents

| Path | What it is |
|---|---|
| `baseline/build_baseline.py` | The single source. Edit here, run `python baseline/build_baseline.py`, and everything below regenerates. |
| `baseline/uav_baseline.json` | The baseline the seeder loads. |
| `baseline/wbs_elements.tsv`, `requirement_allocation.tsv` | Tables the MATLAB scripts read. |
| `documents/UAV_Requirements_Specification.docx` | One Word bookmark per requirement (`SR_T01_01`). |
| `documents/UAV_Verification_Matrix.xlsx` | Requirements, needs, verification, measures, WBS, exclusions, open questions. |
| `documents/trace_manifest.csv`, `task_ledger.csv` | Cross-tool trace rows and the work packets. |
| `matlab/common` | Shared build, expected-answer and check functions; run in both releases. |
| `matlab/r2011a`, `matlab/r2025b` | Release-specific entry points. |
| `matlab/tests` | Octave stand-in and `uav_mock_tests.m`. Run: `octave --no-gui --eval "cd matlab/tests; uav_mock_tests"`. |
| `fusion/UavMultirotorCandidate` | Fusion script. |
| `datcom/uav_vtol_fixed_wing.dcm` | Digital DATCOM input deck. |
