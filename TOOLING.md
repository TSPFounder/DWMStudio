# TOOLING.md — External Tool Integration Design

**Status:** steps 1–3 implemented and verified against the real tools. Step 5 partly built.
Step 4 (Unreal) not started.
**Started:** 2026-08-03, when FEMAP and MYSTRAN were installed.
**Chain closed:** 2026-08-05 — deck → MYSTRAN → `.op2` → FEMAP → six mode shapes on screen,
driven from a DWMStudio button, on the machine that has the tools.

---

## Why this exists

DWMStudio was built around four tools that all work the same way: a long-lived application
you connect to over COM or localhost HTTP. `ToolStatusService` models exactly that, as four
hardcoded booleans, and `WorldProject` models the pipeline as
`enum PipelineStage { SysML, Cad, Matlab, CoSim, Runtime }` cast to a list index.

MYSTRAN breaks both. It is a batch solver: no COM server, no port, no notion of "connected".
Its status can never be better than *an executable exists at this path*, and its results
arrive as files it leaves on disk. It **cannot be represented** in the existing model — not
badly, not at all. FEMAP breaks the second one on its own: there is no FEA stage, and adding
one costs an enum change plus a list-index invariant plus a ViewModel plus XAML.

Both were predicted. SCOPE.md's Architectural Fragility Audit calls them items 3 and 4.

---

## The three tool shapes

The central claim of this design: **there are three shapes, and the old code modelled one.**

| Shape | `ToolKind` | Tools | How you drive it |
| --- | --- | --- | --- |
| Attached application | `InteractiveCom` | MATLAB/Simulink, FEMAP, UModel | COM automation; attach to a running instance or launch one |
| Attached application | `InteractiveHttp` | Fusion 360 | HTTP to an add-in on `127.0.0.1:18750` |
| Batch process | `BatchExecutable` | MYSTRAN, DATCOM, UE commandlets | Write an input file, spawn a process, read the artifacts |
| No automation | `FileOnly` | anything without an API | A human does the work; DWMStudio tracks the files |

`FileOnly` is deliberate. A tool with no API should be representable **honestly**, rather
than by pretending it has one and showing a status dot that can never turn green.

### Why `Found` and `Running` are different

`ToolAvailability` distinguishes `NotFound` / `Found` / `Running` / `Connected`. This is not
pedantry. `ToolStatusService`'s MATLAB dot has always meant *a COM server is registered* —
which is not running, not licensed, and **not the release the project needs**. For MYSTRAN,
`Found` is the ceiling and always will be.

---

## Step 1 — the registry and the pipeline *(implemented)*

`DWM.Shared/Tooling/`:

| Type | Role |
| --- | --- |
| `ToolKind` | The three shapes above |
| `ToolDescriptor` | One tool as data: ProgIDs, executable candidates, extensions, known limitation |
| `ToolAvailability` / `ToolStatus` | What is currently known about a tool, and how it was resolved |
| `ToolRegistry` | The seven built-in tools; `WithOverride` for wrong install paths |
| `PipelineStageDefinition` | One stage: id, label, tool id, artifact, optional |
| `ProjectPipeline` | An **ordered list**, with add / insert / remove / reorder |
| `ToolRun` | One execution, with the freshness check |

### Two things the descriptors carry that are easy to skip

**ProgIDs are ordered, most specific first.** `Matlab.Application.7.12` before
`Matlab.Application`. The generic ProgID resolves to *one* CLSID — whichever release
registered last — and an attach searches the Running Object Table for exactly that. On a
machine with R2011a and R2025b, the generic form **misses an open R2011a and launches
R2025b**. This cost four rounds of debugging on 2026-08-03. A test pins the order.

**Every tool can record what it *cannot* do.** `KnownLimitation` is shown next to the tool in
the UI, because every wrong assumption on this project so far has been about a tool's limits
rather than its features: DATCOM knowing nothing about rotors, FEMAP not solving, `addpath`
succeeding on a folder with no code in it.

### Migration note

This does **not** delete `WorldProject.PipelineStage`. DWMStudio targets `net10.0-windows`
and cannot be built or tested on the Linux agent, so removing the enum blind would be
reckless. `ProjectPipeline.Default()` reproduces the existing five stages exactly — a
migration nobody notices is the only kind worth attempting on a UI that cannot be compiled
where the tests run. Wiring `WorldProject` to it is a separate change, made where it builds.

---

## Step 2 — the batch tool shape *(implemented, 2026-08-04)*

`IMatlabSession` already existed for attached tools. Its sibling now exists too:

| Type | Role |
| --- | --- |
| `IProcessRunner` | Spawn, capture stdout/stderr, resolve the executable |
| `MystranRunner` | Solve a deck; read `.f06` **and** `.ERR`; report the `.op2` |
| `NastranF06Parser` | Eigenvalues out of the `.f06`, with the soft-stiff window check |

Both shapes produce a `ToolRun`. **MYSTRAN was the forcing function** as predicted, and
DATCOM should follow nearly free.

### Four things that were wrong before they were right

**The executable is version-stamped.** `mystran-19.0.0-windows-x86_64.exe`, not
`mystran.exe`. Three guesses failed before `ls -R /c/Mystran` settled it in one command.
Fixed with a glob (`mystran*.exe`) over `ExecutableSearchRoots` plus deterministic ordering,
because the next version bump renames the binary again.

**`ResolveExecutable` returned an unchecked bare filename**, so a tile reported *"Found on
disk"* when nothing had been found and `Process.Start` threw an unhandled `Win32Exception`
on click. Null now means not found. Same failure family as everything else in this file:
*something reported success without having checked.*

**The `.ERR` file was never read.** A run reported zero warnings while the `.ERR` beside it
carried MYSTRAN's L-SET mass-matrix warning. Both files are read now.

**Async stdout/stderr reads.** Sequential `ReadToEnd` on both pipes deadlocks a child that
fills the other buffer. Not hit in practice; fixed anyway, because the failure is a hang with
no output rather than an error.

### The parser was worth more than the process runner, as expected

MYSTRAN's `.f06` is hostile in three specific ways, and each one was caught by running the
parser's logic against a real captured file rather than a synthetic one:

- The section header is printed **letter-spaced**: `R E A L   E I G E N V A L U E S`.
  Matched whitespace-insensitively.
- Fortran **exponent shorthand**: `2.815-1` means `2.815e-1`. There is no `E`.
- Eigenvector GRID rows are **structurally identical to eigenvalue rows** — two leading
  integers then numerics. Without a table terminator the parser reported **72 modes** for a
  6-mode run. `IsSectionHeader` now includes `>>LINK`, and a fixture test pins it.

The fixture is real MYSTRAN 19.0.0 output, checked into `DWMStudio.Tests/Fixtures/`. Tests
run on the Linux agent with no solver installed.

---

## Step 3 — FEMAP, which completes a workflow *(implemented, 2026-08-05)*

FEMAP's COM API is the same shape as MATLAB's, so the adapter is largely the one that
already worked. The value is the pairing:

```
FEMAP  (geometry, mesh, write .bdf)
  -> MYSTRAN  (solve, write .f06/.op2)
    -> FEMAP  (read results, post-process)
```

FEMAP does not solve; MYSTRAN does not mesh. Together they are a complete FEA capability
with no gap, and they retire the hand-written `wtTowerModal.dat` approach.

`FemapComSession` / `FemapPostProcessor` in `DWM.Shared/Tooling/Fea`, 13 tests.

### The verified call shapes

Pinned on 2026-08-05 against FEMAP 10.2 (64-bit) — **confirmed by FEMAP's own `Out: 6`, not
by the calls failing to throw**, a distinction this project has now had to make about four
separate tools:

| Job | Call | Returns |
| --- | --- | --- |
| Clear | `feFileNew()` | `-1` |
| Model | `feFileReadNastran(setId, filename)` | `-1` |
| Results | `feFileReadNastranResults(setId, filename)` | `-1` |

**`-1` is success.** It is VB `TRUE`, and it is now *checked*, not merely reported. Same
family as MATLAB's `Execute` returning error text as an ordinary string and MYSTRAN exiting 0
after a FATAL: **the status lives somewhere other than where a caller would naturally look.**
Until `-1` was known, a refused call and an accepted one were indistinguishable — which is
exactly how three consecutive runs reported success while leaving `Out: 0` in FEMAP.

There was also a genuinely harmful fallback in the results candidate list: it ended with
`feFileReadNastran(setId, filename)`, feeding the `.OP2` to the **model** reader. It did not
throw, so the try-each-shape loop accepted it. Removed. A fallback that cannot fail is worse
than no fallback.

### Two orderings that are not stylistic

**Model before results.** MYSTRAN's `.op2` for this deck holds six `OUGV1` eigenvector blocks
and **no `GEOM` datablocks** — results without geometry. Read on its own it produces *"Your
model does not currently contain Nodes and Elements"*. The deck is read first to build the
mesh; node and element ids match because both come from the same file.

**Clear before importing, but only from the button.** A repeat load into a populated FEMAP
does not replace, it collides — *"Overwriting existing Property 101..110"*, twelve output sets
where six belong. `startNewModel` therefore defaults to **off** in the library and is turned
**on** by the DWMStudio button. Asymmetric damage: a duplicated results tree is one
`File > New` away from fixed; somebody's unsaved meshing is not. The button knows it will be
pressed repeatedly and is made idempotent; the library, which cannot know what is open,
declines to destroy anything.

That the button gets pressed twice is not a user error — it happened because the *first*
press appeared to do nothing, for the reason two sections down.

### Proving it by hand first is why this worked at all

The manual run took two minutes and caught something that would have been near
undiagnosable over COM: FEMAP's Import dialog defaults to **Femap Neutral** with a blank
NASTRAN flavour, which happily accepts a Nastran deck, reports *"Database Update Completed.
No Errors."*, and imports nothing. Through the API that is a silent no-op with a success
return.

**This is the method, not an anecdote:** drive the workflow by hand, watch what the dialogs
actually default to, then automate the thing you saw work.

### One thing the UI does that the API cannot see

FEMAP's Model Info tree **does not repaint for entities that arrive over the API**. A load
that fully worked leaves the Results node looking empty — which reads precisely like a failed
import, and cost several runs chasing a bug that was not there.

The fix is the tree's own **Reload from Model** button (second on the Model Info toolbar),
confirmed 2026-08-05: it populated all six modes instantly, no re-import. `RefreshUi` carries
three unverified candidate calls, all attempted, all ignored on failure — acceptable guessing
precisely because the fallback is one known click. **Nothing downstream treats a repainted
tree as evidence the import worked, or an empty one as evidence it did not.** The status
bar's output-set count is the fact; the tree is cosmetics.

### What it settled

**The tower disagreement — the first real job for it, and it delivered.** The Simulink model
assumes `f_tower = 0.320 Hz`. Three independent methods now agree with each other and
disagree with it:

| Method | First tower bending |
| --- | --- |
| numpy 10-element beam | 0.2815 Hz |
| MYSTRAN 19.0.0 `.f06` | 0.2810991 Hz |
| FEMAP reading the `.op2` | 0.281099 Hz |

0.14% apart, and the Simulink figure is the outlier at **+13.8%**. Both still pass the
soft-stiff window (0.257–0.630 Hz), so nothing is broken — but the model's number is not the
structure's number.

FEMAP also showed something the `.f06` scan did not make obvious: the six output sets are
**three physical modes in orthogonal pairs** (0.281099 ×2, 2.221241 ×2, 6.633947 ×2), which
is what an axisymmetric tower must produce. That is a check on the deck in its own right, and
it came free from looking at the tree.

Implied `K_t = m_t·ω² = 2.105e5 × 1.766198² = 6.566e5 N/m`, against the model's 8.508e5
(**−23%**). **Recorded, deliberately not applied** — changing a tuned model's stiffness
mid-MVP is a separate decision from measuring it. See SCOPE.md.

---

## Step 4 — Unreal *(proposed, deferred)*

The only tool with no automation wired at all. Two routes:

- **Python Remote Execution** — editor plugin, UDP; gives live control of a running editor
- **`-run=` commandlets** — batch; needs nothing enabled, works headless

Registered as `BatchExecutable` for now because the commandlet route needs nothing added.
Deferred because it is the most speculative and the least blocking.

---

## Step 5 — the GUI *(Edit / Run built; Create not started)*

### Three verbs per stage, and only three

| Verb | Means | State |
| --- | --- | --- |
| **Create** | Scaffold the artifact from a template (`.f3d`, `.slx`, `.ump`, `.bdf`, `.uproject`) | **not built** |
| **Edit** | **Launch the native app on the file.** That is what edit means | built |
| **Run** | Automate: a COM command sequence, or a batch process | built for MATLAB, MYSTRAN, FEMAP |

**DWMStudio should never try to be an editor.** Editing happens in the tool that owns the
format. This is not a limitation to apologise for — it is the only division of labour that
stays true as tools are added.

### One window per tool, and it is not one window per tool in the source

Each tile opens its own workspace window. There is **one** `ToolWorkspaceWindow`,
parameterised by a `ToolWorkspaceModel` built from the registry and the pipeline — because the
alternative was the thing this whole document exists to stop: adding FEMAP would have meant a
fifth Border in XAML, a fifth command, a fifth stage accessor, spread over four files and two
languages, one of which cannot be compiled on the build agent.

So `WorldDetailView`'s four hand-written Borders became a data-driven `ItemsControl`, and the
*decision* about what a tool can do lives in `DWM.Shared/Tooling/ToolWorkspaceModel.cs` where
it is data and has 12 tests. FEMAP/MYSTRAN tiles cost a registry entry.

**`CanRun` is blocked only by a positive `NotFound`, never by `Unknown`.** An interactive COM
tool reports `Unknown` *on purpose* — checking whether a COM server is registered tells you
almost nothing worth having. Treating that as "cannot run" disabled the MATLAB button
entirely and made clicking it do nothing. **Attempting is the probe.** And every disabled
button carries a `WhyNot()` tooltip, because a dead control with no explanation is the same
bug as the Create World button that silently did nothing.

### Hand over; do not reimplement

The MATLAB tile's Run button says **"Open wtGui in MATLAB"**, not "Run". `wtGui` already
picks the scenario, shows the six plots and the pass/fail panel, and exports the channel
CSVs — under R2011a, which is the release the model needs. Rebuilding any of that in WPF
would be a worse copy of a tool the project already owns.

**This taught the COM lifetime rule the hard way.** MATLAB kept closing the instant the
hand-off finished. The first fix — suppressing `Quit()` via `Detach()` — was wrong, because a
COM server *launched by a client is owned by it* and exits when the last reference is
released; no `Quit()` is involved. The real fix is to attach over COM if MATLAB is already
running, and otherwise launch `matlab.exe -r "addpath(...); wtGui"` as an **ordinary
process**, which nobody owns. FEMAP inherits the same rule, one tool along.

### A run history, not a checkbox *(built)*

`MarkStageComplete` sets a boolean. That cannot express *"ran, passed, three warnings"* —
exactly what a turbine export produces when a channel is missing. A green tick over a run
with warnings is the same class of problem as a placeholder that looks like model output:
correct-seeming, and silent about the one thing worth knowing.

`ToolRun` now carries status, duration, outputs, warnings and the resolved tool version, and
`ToolRunStatus` includes `SucceededWithWarnings` and `StaleOutputs` as first-class outcomes.

**And a warning nobody can read is not a warning.** The run-history template silently failed
to render the `Warnings` collection for two builds — during which the FEMAP work was
generating precisely the diagnostic that would have shortened it, into a control that did not
display it. Fixed, *and* the same text is now appended to the status line: one fact, two
places, because the cost of duplication is far below the cost of it being invisible again.

### Status dots that tell the truth

Four booleans become per-tool `ToolStatus`, showing *not installed* / *found* / *running* /
*connected* — with the resolved ProgID or executable path in the tooltip, so "which MATLAB?"
is answerable at a glance.

---

## Worlds are saved now *(2026-08-05)*

Not strictly tooling, but it landed with this work and it was load-bearing: **new worlds were
never persisted.** They lived in an `ObservableCollection` and vanished on exit.

`WorldLibraryStore` writes `%APPDATA%\DWMStudio\worlds.json`. Three decisions worth keeping:

- **Atomic writes** — temp file plus `File.Replace`, so a crash mid-save cannot leave a
  truncated library where a valid one was.
- **A corrupt file is quarantined, never deleted.** Renamed aside so the app starts, with the
  original still there to look at. Deleting somebody's library because it failed to parse
  would be the worst possible response to a parse bug.
- **`SchemaVersion` is actually read**, and a future version is *refused* rather than
  best-effort parsed. A field that is written but never checked is decoration.

Sample worlds now seed only on a genuinely empty first run, rather than reappearing forever
beside the user's real ones.

The bug underneath it was worse than the missing file: `WorldCreatedMessage` was sent and
**no type anywhere implemented `IRecipient<WorldCreatedMessage>`** — see the last section.

---

## Tools the OOSEM spreadsheet assumes — reconciled against DWM, 2026-08-05

Every tool named in a parenthesis in `DWM_OOSEM_Criteria_and_Tasks.xlsx`'s Inputs and Outputs
columns, counted by mention. **This is what the method assumes it has**, which is not the same
as what DWM has or wants.

| Mentions | Tool | Status |
| --- | --- | --- |
| 91 | Word | Document artifacts. Fine, not automated |
| 76 | UModel | **Registered.** The SysML tool |
| 72 | SharePoint | CM and lists backbone of phases A/B. **Status unconfirmed — see below** |
| 41 | Excel | HoQ, traceability matrices, WBS. Fine |
| 22 | Simulink | **Registered** (R2011a) |
| 14 | MATLAB | **Registered** (`Matlab.Application.7.12`) |
| 12 | Code / Visual Studio | Software implementation |
| 10 | **MS Project** | **OBE** |
| 8 | **Visio** | **OBE** |
| 10 | **SolidWorks** | **OBE** |
| 6 | **STK** | Not owned. Aerospace-template inheritance |
| 6 | FEMAP | **Registered**, and now driven end to end |
| 6 | "FEA App" | Generic. **MYSTRAN fills this**, and is registered |
| 6 | PDE Toolbox | Licensed under R2011a |
| 5 | Flow / PowerApps | Microsoft 365 automation |
| 4 | Outlook | Notifications |
| 4 | Minitab | Fishbone (C.4.3), statistics |
| 1 each | Eagle, LT Spice | Electrical analysis (H.3). Neither registered |
| 1 each | Control System, Optimization, Mupad | Licensed under R2011a |
| 1 each | **Simulink Controls Design**, **Simulink Design Optimization** | **Not in the licensed set** |

### What this reconciliation turns up

**Four tools are OBE or unowned:** MS Project, Visio, SolidWorks (all confirmed OBE
2026-08-05) and STK. That is **34 mentions** of tooling the spreadsheet assumes and DWM will
not be using — concentrated in phases A, I and J, plus C.4.2/C.8.2.

**No CAD tool remains named that DWM will use.** SolidWorks was the spreadsheet's CAD tool
across phases I and J (tooling, machine, facility and inspection models). Fusion is what the
registry holds, and Fusion appears **nowhere** in the spreadsheet. One of the two has to give
before manufacturing work starts; neither is urgent now.

**Unreal is not named anywhere in the spreadsheet.** No criteria, no outputs, no mentions —
yet "simulate them in UE" is the closing step of DWM's whole loop. The method the spreadsheet
encodes ends at verification and demonstration (phases K, M) without ever reaching the
simulation target. That is the largest structural gap between the process as written and DWM
as described, and it is a different gap from TOOLING.md's own unbuilt step 4.

**Two MATLAB toolboxes are named but not licensed.** *Simulink Control Design* and *Simulink
Design Optimization* are separate products; the R2011a full licence per the 2026-07-02
decision covers Control System, Optimization, Aerospace, Symbolic Math, PDE and Simulink V&V —
not those two. Cheap to catch now, expensive to discover in the middle of phase H.

**SharePoint's status needs an answer, and cannot be inferred.** At 72 mentions it is the
second-most-named tool in the sheet and the entire CM, lists and traceability backbone of
phases A and B. Everything else confirmed OBE so far is Microsoft project/authoring tooling,
which makes SharePoint's continued use a fair question rather than a safe assumption — but
"probably also OBE" is a guess, and a wrong guess here silently invalidates a fifth of the
spreadsheet. **Asked, not assumed.**

---

## The one failure mode, seen five times

Worth naming, because it stopped being a coincidence some time around the third instance.
Every tool integration on this project has failed the same way at least once:

| Tool | It said | It meant |
| --- | --- | --- |
| MATLAB | `Execute` returned a string | The string *was* the error text |
| MATLAB | `addpath` succeeded | The folder had no `.m` files in it |
| MYSTRAN | Exit code 0 | FATAL in the `.f06`; nothing solved |
| FEMAP | The call did not throw | Return code `0`; import refused |
| FEMAP | *"Database Update Completed. No Errors."* | Wrong importer; read nothing |
| DWMStudio | Tile said "Found on disk" | `ResolveExecutable` returned an unchecked guess |

**Something reported success without having checked.** In every case the status existed
somewhere other than where a caller would naturally look — a return code, a second file, a
string's contents, a dialog's default.

Two habits follow, and they are the practical content of this document:

1. **Find where the tool actually keeps its verdict, and read that.** Not the exception, not
   the exit code, not the absence of a crash.
2. **Drive it by hand once before automating it.** Two minutes in the UI beats an afternoon
   of a silent no-op returning success.

---

## Two principles carried forward from the turbine work

**Pin the tool version per project.** Not "MATLAB" — `Matlab.Application.7.12`. The MVP
turbine model runs under R2011a and will silently run under R2025b if nobody says otherwise.
The same applies to UE 5.3 and FEMAP 10.2.

**Verify output freshness on every run, for every tool.** `MatlabStageService` refuses any
CSV predating the run that produced it, because an export that silently does nothing leaves
yesterday's correctly-named files on disk, and the package built from them is well-formed,
plausible, and describes a run that never happened.

**A MYSTRAN `.f06` from yesterday's deck is identical in kind.** It parses, it has
eigenvalues in it, and nothing about it says the solver did not run today. `ToolRun` therefore
*derives* its status from the evidence rather than accepting the caller's word — a run
claiming success with stale outputs is reported as `StaleOutputs`, which is a distinct and
more dangerous state than `Failed`, because everything looks right including the files.

---

## Where this code lives, and why

**`DWM.Shared`, not `DWMStudio`.** The MATLAB stage proved the pattern: orchestration in a
`net10.0` library with tests, while `DWMStudio` is `net10.0-windows` and has **no test
coverage at all and cannot have any** where it currently sits.

That is not theoretical. On 2026-08-03 the New World wizard's Create button did nothing,
because `WorldCreatedMessage` was sent and **no type anywhere implemented
`IRecipient<WorldCreatedMessage>`**. A messenger send with no recipient is silent by design.
The bug compiled, ran, and did nothing — and no test could have caught it, because the
ViewModel layer cannot be referenced from the test project.

Moving the ViewModels into a shared project would fix that class of bug permanently. It is a
real refactor and is not part of this design, but it is the reason every new integration
piece goes into `DWM.Shared` first.
