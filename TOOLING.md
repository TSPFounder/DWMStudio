# TOOLING.md — External Tool Integration Design

**Status:** step 1 implemented (`DWM.Shared/Tooling`, 19 tests). Steps 2–5 proposed.
**Started:** 2026-08-03, when FEMAP and MYSTRAN were installed.

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

## Step 2 — the batch tool shape *(proposed)*

`IMatlabSession` already exists for attached tools and works. Add its sibling:

```
IToolSession   Execute(command), read values          MATLAB, FEMAP, UModel, Fusion
IBatchTool     Run(exe, args, workdir) -> artifacts   MYSTRAN, DATCOM
```

Both produce a `ToolRun`. **MYSTRAN is the forcing function** — build the batch shape against
it and DATCOM follows almost free, since both are "write a deck, spawn a process, parse the
output file".

The parser matters more than the process runner. MYSTRAN writes `.f06` (text) and `.op2`
(binary); reading eigenvalues out of the `.f06` is the piece worth testing, and it can be
tested with a captured sample file and no solver installed.

---

## Step 3 — FEMAP, which completes a workflow *(proposed)*

FEMAP's COM API is the same shape as MATLAB's, so the adapter is largely the one that
already works. The value is the pairing:

```
FEMAP  (geometry, mesh, write .bdf)
  -> MYSTRAN  (solve, write .f06/.op2)
    -> FEMAP  (read results, post-process)
```

FEMAP does not solve; MYSTRAN does not mesh. Together they are a complete FEA capability
with no gap, and they retire the hand-written `wtTowerModal.dat` approach.

**First real job for it:** the open tower disagreement. The Simulink model reports
`f_tower = 0.320 Hz`; the independent 10-element beam deck predicts **0.2815 Hz**. Both pass
the soft-stiff window (0.257–0.630 Hz), but at 24% and 9.5% margin. A meshed FEMAP model
solved by MYSTRAN would settle it.

---

## Step 4 — Unreal *(proposed, deferred)*

The only tool with no automation wired at all. Two routes:

- **Python Remote Execution** — editor plugin, UDP; gives live control of a running editor
- **`-run=` commandlets** — batch; needs nothing enabled, works headless

Registered as `BatchExecutable` for now because the commandlet route needs nothing added.
Deferred because it is the most speculative and the least blocking.

---

## Step 5 — the GUI *(proposed)*

### Three verbs per stage, and only three

| Verb | Means |
| --- | --- |
| **Create** | Scaffold the artifact from a template (`.f3d`, `.slx`, `.ump`, `.bdf`, `.uproject`) |
| **Edit** | **Launch the native app on the file.** That is what edit means |
| **Run** | Automate: a COM command sequence, or a batch process |

**DWMStudio should never try to be an editor.** Editing happens in the tool that owns the
format. This is not a limitation to apologise for — it is the only division of labour that
stays true as tools are added.

### A run history, not a checkbox

`MarkStageComplete` sets a boolean. That cannot express *"ran, passed, three warnings"* —
which is exactly what a turbine export produces when a channel is missing or the gust
scenario was used. A green tick over a run with warnings is the same class of problem as a
placeholder that looks like model output: correct-seeming, and silent about the one thing
worth knowing.

Each stage should show its runs: when, how long, status, warnings, which tool version.

### Status dots that tell the truth

Four booleans become per-tool `ToolStatus`, showing *not installed* / *found* / *running* /
*connected* — with the resolved ProgID or executable path in the tooltip, so "which MATLAB?"
is answerable at a glance.

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
