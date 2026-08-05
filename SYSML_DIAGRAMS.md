# SYSML_DIAGRAMS.md — The UModel Diagram Inventory

> ## A CAPABILITY REQUIREMENT ON DWM_Dev — not a modelling backlog
>
> **Settled 2026-08-05, and it inverts what this document is.** DWM_Dev is **not** the System
> of Interest. The SoI is **whatever system the user is developing**, and DWM users will bring
> a great many of them. So the list below is not a set of diagrams to draw of DWM — it is the
> set of diagram kinds **DWM_Dev must support its users in producing, for systems this project
> will never see.**
>
> Read every row as a requirement of the form *"a user must be able to create, edit, track and
> version a `<kind>` for their SoI"*, not as a task assigned to anyone here.
>
> **What this changes:**
>
> - **The count stops being an estimate and becomes a coverage list.** 79 was previously a
>   worrying amount of drawing. As a capability list it is the opposite of worrying — the
>   *kinds* are what must be supported, and there are only **15 distinct types** behind the 79
>   rows. See the totals table.
> - **The recursion multiplier stops blocking anything.** The blank `Recursive` column was
>   flagged as preventing an estimate. It no longer needs filling in: the platform must simply
>   **not assume a fixed depth**, because the user's system tree decides it. That is the same
>   finding as Fragility Audit item 3 — a pipeline that is an enum cannot represent a hierarchy
>   whose depth it does not know.
> - **Model organisation becomes a per-project template, not a one-time decision.** Figure 17.4
>   puts a single SoI at the centre of the package tree. A platform serving many users with
>   many systems needs that structure **scaffolded per project**, which is exactly TOOLING.md's
>   unbuilt **Create** verb. See *One SoI per project* below.
>
> **Still out of MVP scope.** The Frozen Scope Table has listed *"SysML/OOSEM authoring in
> UModel feeding the pipeline (XMI)"* under Out of Scope since the scope was frozen, and it
> stays there. Nothing here is an MVP deliverable — but as a requirements source rather than a
> backlog, it now bears on how the *tooling* is shaped, and the tooling is in scope.
>
> **Consequence for maintenance.** A document that carries requirements has to stay correct,
> so the six inconsistencies near the end are worth fixing in the source spreadsheet rather
> than merely noted here.

**Source:** `DWM_OOSEM_Criteria_and_Tasks.xlsx`, sheet `Criteria`, 499 rows, extracted
2026-08-05. Every row below is a named entry in that sheet's **Outputs** column — nothing
here is inferred, invented, or filled in from what OOSEM "usually" wants.

**Method reference:** Friedenthal, Moore & Steiner, *A Practical Guide to SysML*, **Chapter
17** — *Residential Security System Example Using the Object-Oriented Systems Engineering
Method*. The spreadsheet is a tailoring of that chapter's process. Figures 17.1 (*Develop
System*) and 17.2 (*Specify and Design System*) are what the phase letters follow.

**79 modelling artifacts. 76 of them are UModel.** That is the number that would matter for
scoping the UModel work if it were scheduled, and it is larger than a scan of the sheet
suggests, for a reason worth stating first.

---

## How the spreadsheet's phases map to OOSEM

Checked against the criteria, not assumed from the letters. Figure 17.2's seven activities
account for phases C through H; Figure 17.1's outer *Develop System* process accounts for
the rest.

| Phase | Criteria | OOSEM activity | Fig |
| --- | --- | --- | --- |
| **A** | 76 | **Not from Chapter 17.** WBS, IMP, IMS, SEMP, the plans — program-management practice, sitting where Fig 17.1's *Manage System Development* box is | — |
| **B** | 36 | **Not from Chapter 17.** CM, cost estimating, risk, supplier rating — same | — |
| **C** | 20 | **17.3.2 Analyze Stakeholder Needs** — as-is analysis, mission requirements, MoEs, use cases | 17.2 |
| **D** | 21 | **17.3.3 Analyze System Requirements** — scenarios, black-box BDD, state machines | 17.2 |
| **E** | 10 | **17.3.4 Define Logical Architecture** — SoI logical decomposition | 17.2 |
| **F** | 38 | **17.3.5 Synthesize Candidate Physical Architectures** — partitioning criteria, node/SW/HW/data architecture | 17.2 |
| **G** | 41 | **The recursion.** E+F applied at component level, plus trade studies (G.1, G.10, G.11) | 17.1 |
| **H** | 20 | **17.3.6 Optimize and Evaluate Alternatives** — analysis contexts, parametrics, design-space optimisation | 17.2 |
| **I** | 8 | *Develop Hardware / Software* — Concept of Manufacture, tooling, code | 17.1 |
| **J** | 9 | *Integrate and Verify System* — integration | 17.1 |
| **K** | 10 | *Integrate and Verify System* — inspection | 17.1 |
| **M** | 3 | *Integrate and Verify System* — demonstration and validation | 17.1 |

**Phases A and B are not a tailoring of Chapter 17 — they are additional.** Figure 17.1 has a
*Manage System Development* box, and the chapter describes it in general terms (planning,
project control, life-cycle model selection, tailoring), but it populates none of it. A WBS,
an IMP/IMS, control accounts and work packages, cost point estimates, schedule risk
mitigation and supplier rating are program-management practice from elsewhere. **Do not read
the phase letters as one continuous OOSEM sequence** — C through H are the method, A and B
are the program wrapped around it, and I through M are the right-hand side of the Vee.

**This confirms the recursion is the method, not a naming coincidence.** The structural claim
below — *phase G is E+F run again per component* — was inferred from diagram names before the
reference was to hand. Chapter 17 states it directly: the development process *"can be applied
recursively to multiple levels of a system's hierarchy… where the development process is
applied to successively lower levels."* Phases E, F and G are one process at two depths.

**Two activities from Figure 17.2 have no phase of their own.**

- **17.3.7 Manage Requirements Traceability** is distributed rather than absent — C.7 (Mission
  Requirements Traceability Matrix), D.10 (Change Impact Analysis), D.11 (linked to the
  SharePoint requirements list). Reasonable; traceability is continuous in the figure too,
  running alongside the other activities rather than after them.
- **17.3.1 Set-up Model** appears to have **no criteria at all.** In the chapter this is where
  modelling guidelines and model organisation are established. Worth checking whether it was
  deliberately dropped or simply not reached yet — the chapter's own answer is below, and it
  is the piece the spreadsheet is missing.

---

## Starting point: 17.3.2 Analyze Stakeholder Needs (phase C)

**Decided 2026-08-05.** The UModel work starts here — meaning **DWM_Dev must support a user
doing 17.3.2 for their own SoI first**, before any later activity. Phase C is Figure 17.5's
*Analyze Stakeholder Needs* activity almost action for action, which is the strongest
confirmation available that the spreadsheet and the chapter describe one process:

| Figure 17.5 action | Phase C criteria |
| --- | --- |
| characterize as-is system and enterprise | C.4.1, C.4.2 |
| perform causal analysis | C.4.3 |
| specify mission requirements | C.5.1, C.5.2 |
| define enterprise use cases | C.9.1 |
| define to-be domain bdd | C.8.1 |
| capture measures of effectiveness | C.6.1 |
| conduct mission requirements review | **no criterion** |

### The first capability increment: six diagram kinds, not seventy-nine

Every UModel artifact in phase C — the set DWM_Dev must support first. `C.5.2` updates
`C.2.2` rather than adding a diagram, so the count of distinct kinds is six:

| Order | Criteria | Diagram | Type |
| --- | --- | --- | --- |
| 1 | C.2.2 | Views & Viewpoints Diagram | `pkg` |
| 2 | C.4.1 | As-Is Operational Domain Diagram | `bdd` |
| 3 | C.5.1 | Mission Requirements Diagrams | `req` |
| 4 | C.5.2 | *(updates the C.2.2 viewpoints)* | `pkg` |
| 5 | C.8.1 | To-Be Operational Domain Block Definition Diagram | `bdd` |
| 6 | C.6.1 | MoE Parameter Diagrams | `par` |
| 7 | C.9.1 | Mission Use Case Diagrams | `uc` |

**A tractable first increment** — six kinds against fifteen in the full method, and it lands
on the one output every later phase consumes: mission requirements with measures of
effectiveness traced to them. A platform that supports only phase C is already useful to a
user, which is not true of most single-phase subsets further down.

### Do 17.3.1 first — and for a platform it is not optional

**Set-up Model has no criteria in the spreadsheet, and it precedes this.** C.2.2 is a package
diagram; it has to be created *somewhere*, and the somewhere is the model organisation from
Figure 17.4 below.

For a single team modelling one system, skipping it costs a rearrangement later. **For a
platform it is the deliverable itself** — the user cannot be asked to hand-build the package
tree before drawing their first viewpoint diagram, so *scaffolding the structure is the
prerequisite feature, not the preliminary decision.* 17.3.1 having no criteria in the sheet
is therefore the most consequential of the gaps found here, not the smallest.

### Four things to reconcile before starting

1. **The spreadsheet serialises what Figure 17.5 runs in parallel.** The figure forks use
   cases, the to-be domain BDD and the MoEs into three concurrent branches. The spreadsheet's
   own Inputs column does not: **C.9.1 takes *To-Be Mission Models & Simulations* as input**,
   which is C.8.2, which needs C.8.1. So use cases sit downstream of the to-be BDD rather
   than beside it. Either is defensible — just know which one is being followed, because the
   figure suggests three people can work at once and the spreadsheet says they cannot.
2. **C.1–C.3 are an addition.** Stakeholder Analysis, Stakeholder Needs Statement, Mission
   Needs Analysis and the Views & Viewpoints diagram all precede Figure 17.5's first action.
   The chapter treats stakeholder analysis as context; the spreadsheet makes it explicit
   work. Consistent with Viewpoints being a top-level package in Figure 17.4, so this looks
   deliberate rather than accidental.
3. **No "conduct mission requirements review" criterion exists.** Figure 17.5 ends with one.
   C.3.2 approves the *Mission Needs Analysis*, which is a different and earlier gate. The
   phase currently has no exit gate for the requirements it produces.
4. **C.4.2 and C.8.2 require STK** (Systems Tool Kit) alongside Simulink, for the as-is and
   to-be mission models. **DWM has no STK, and it is not in the tool registry.** This looks
   like an artifact of the template's aerospace origin. For DWM the mission model is far more
   likely to be Simulink alone — or the existing economy simulation — but that is a tailoring
   decision, not something to discover mid-phase. Note that Simulink V&V *is* available: the
   R2011a full licence includes it.

---

## Model organisation — Figure 17.4, the ESS model structure

The chapter's answer to *"how is the UModel project laid out?"*, which is the question the
missing 17.3.1 criteria would have covered. Reproduced from the ESS example's browser view:

```
OOSEM Profile Extensions
Model
├── Process Guidance          -- the OOSEM process itself, as activity diagrams
├── Security Domain as-is     -- what exists today, and what of it can be reused
├── Security Domain to-be
│   ├── Installation          -- one package per life-cycle phase
│   └── Operational           -- (could also hold Manufacturing, Support, Disposal)
│       ├── 1-Requirements
│       ├── 2-Structure
│       ├── 3-Use Cases
│       ├── 4-Behavior
│       ├── 5-Parametrics
│       ├── 6-Interface Definitions
│       └── ESS               -- the system of interest
│           ├── 1-Black Box Specification
│           ├── 2-Logical Design
│           ├── 3-Node Logical Design
│           ├── 4-Node Physical Design
│           └── 5-Verification
├── Value Types               -- imports SI Definitions; reused at every level
└── Viewpoints                -- stakeholder viewpoints and their views
```

**The ESS sub-packages are the spreadsheet's phases.** The correspondence is exact, and it is
the clearest confirmation yet that the two describe one process:

| ESS package | Phase |
| --- | --- |
| 1-Black Box Specification | **D** — SoI Black-Box BDD, system requirements, state machines |
| 2-Logical Design | **E** — SoI Logical BDD, subsystem decomposition |
| 3-Node Logical Design | **F.2** — SoI Logical Node Architecture BDD, node IBDs |
| 4-Node Physical Design | **F.3** — SoI Physical Node BDDs, allocation |
| 5-Verification | **J / K / M** — integration, inspection, demonstration |

### This closes both open UModel-structure questions

They were listed at the end of this document as unanswered. They are answered:

**How is phase G's per-component recursion represented?** *"The model organization typically
includes a recursive package structure that mirrors the system hierarchy. A package may be
defined for a block that is further decomposed."* So: **a package per decomposed block**, and
the `ESS` package's five sub-packages are the template that repeats at each level. Phase G is
literally a copy of that structure one level down — which is the same recursion the diagram
inventory showed, now visible in the package tree rather than in diagram names.

**What about things reused across levels?** They sit **outside** the hierarchy: *"The model
organization also includes other packages that are not nested within the system hierarchy
packages… such as packages for value types and viewpoints."* Value Types and Viewpoints are
siblings of the domain packages, not children — which is why the Views & Viewpoints diagrams
appear at C.2.2 and are then pulled from a library at C.3.1 and C.4.3 rather than rebuilt.

`Node Physical Design` nests one package each for **hardware, software, data and operational
procedures** — which is exactly the F.3/F.4/F.5/F.6 split, and G.5/G.6/G.7/G.8 one level down.

Four conventions worth copying:

- **Numeric name prefixes** (`1-Requirements`, `2-Structure`) exist purely to force browser
  ordering. The numbers are not part of the names in prose.
- **`Process Guidance` holds the method itself** — Figure 17.2 and the 17.3.x activity
  diagrams live *in the model*. The process is modelled alongside the system it produces.
- **A `Navigation` BDD of hyperlinks.** A block definition diagram whose contents are links to
  the diagrams of interest, so the model can be navigated *without knowing the package
  structure*. Cheap, and it is the difference between a model people use and one only its
  author can find their way around. Worth creating early, not at the end.
- **The diagram frame is a model element, and it decides where the diagram lives.** The frame
  represents an element, and that element dictates the diagram's position in the browser
  hierarchy. So a diagram is not filed by hand — it is filed by what it is drawn on. Related:
  a model element from another package appears with its **fully qualified name**, which is
  what keeps two same-named elements in different packages distinguishable.

---

## One SoI per project — what a multi-user platform does with Figure 17.4

**Figure 17.4 holds exactly one System of Interest.** `ESS` sits at the centre of
`Operational`, with `Security Domain as-is` and `Security Domain to-be` named for that one
problem. It is the model organisation for *a* system, and it assumes there is one.

DWM_Dev serves users with many systems. The structure therefore cannot be a fixture of the
product — it has to be **instantiated per project**, with the SoI's name substituted
throughout:

```
<Project>.ump
├── OOSEM Profile Extensions        -- product-supplied, identical every time
├── Process Guidance                -- product-supplied, identical every time
├── <Domain> as-is                  -- named for the user's problem domain
├── <Domain> to-be
│   └── Operational
│       ├── 1-Requirements … 6-Interface Definitions
│       └── <SoI>                   -- named for the user's system
│           ├── 1-Black Box Specification
│           ├── 2-Logical Design
│           ├── 3-Node Logical Design
│           ├── 4-Node Physical Design   -- hardware / software / data / procedures
│           └── 5-Verification
├── Value Types                     -- product-supplied; user extends
├── Viewpoints                      -- user-authored (C.2.2)
└── Navigation                      -- hyperlink BDD, generated
```

**This is TOOLING.md's `Create` verb, and it is the strongest case yet for building it.**
`Create` is defined there as *"scaffold the artifact from a template"* and is the one verb of
the three still unbuilt. For every other tool it is a convenience — a `.slx` or a `.f3d` can
be made in the native application just as easily. **For UModel it is the feature.** Nobody
should be hand-building a fourteen-package tree correctly, per project, from a book they may
not own; and a structure that is wrong in project one is wrong in every diagram that lands
inside it thereafter.

Three consequences worth carrying into that work:

- **Product-supplied packages must be distinguishable from user content.** `OOSEM Profile
  Extensions`, `Process Guidance` and the `Value Types` base library ship with the platform
  and should be updatable without touching what a user has authored. Nothing in Figure 17.4
  marks that boundary — the chapter had no reason to care, having one model and one team.
- **The SoI name is a parameter, not a constant.** It appears in the package tree, in
  qualified names on diagrams, and in the as-is/to-be domain names. Renaming a system after
  the fact is where a hand-built model becomes unusable.
- **Depth is a parameter too.** Phase G is `2-Logical Design` through `5-Verification`
  repeated one level down. A user with a three-level system tree needs it three times. The
  scaffolder must therefore generate structure from a declared hierarchy rather than emit a
  fixed template — the same demand `ProjectPipeline` already makes of the stage list, for the
  same reason.

---

## Why a search for "Diagram" undercounts by a third

Searching the Outputs column for the word *diagram* returns **52** entries. The other **27**
are named `BDD` or `IBD` — `System of Interest Black-Box BDD`, `SoI Logical Node IBD's`,
`Component Physical Node BDD's` — and are exactly as much UModel work as the ones that spell
it out. Any estimate built from a text search for "diagram" is a third short before it starts.

---

## The shape of it: one pattern, applied at three levels

The list is long but it is not 79 different things. It is **one architectural pattern
recursing down the system tree**, and once that is visible the UModel work becomes tractable.

```
Mission / Operational level   (C, D)   -- the problem, the stakeholders, the requirements
  System of Interest level    (E, F)   -- decompose the SoI: logical, node, SW, HW, data
    Component level           (G)      -- THE SAME DECOMPOSITION, per component
```

**Phase G is phases E and F run again, one level down.** The correspondence is near
exact — pair them up and the parallel is unmistakable:

| Concern | SoI level (E/F) | Component level (G) |
| --- | --- | --- |
| Logical decomposition | E.2.3 / E.2.4 / E.3.1 | G.2.1 / G.2.4 / G.2.5 |
| Logical state machines | E.4.2 | G.3.2 |
| Logical node architecture | F.2.1 – F.2.4 | G.4.1 – G.4.4 |
| Physical node architecture | F.3.2 – F.3.6 | G.5.2 – G.5.6 |
| Software architecture | F.4.1 – F.4.5 | G.6.1 – G.6.5 |
| Hardware architecture | F.5.1 | G.7.1 |
| Data architecture | F.6.1 / F.6.3 / F.6.4 | G.8.1 / G.8.3 / G.8.4 |

The consequence for the UModel work: **a phase-G diagram is a template instantiation, not a
new diagram type.** Build the F-level set once, get its structure right, and G is the same
set applied per component. Getting F wrong means getting it wrong N times in G.

---

## The count is a floor, not a total

Six criteria are flagged **Recursive for Each WBS Item = Yes** (D.4.2, D.4.3, D.9.1, E.2.3,
E.2.4, E.4.2). Those multiply by the number of subsystems.

**But 47 of the 79 have that column blank** — including all of F and all of G, which are the
most obviously per-item entries in the sheet. `SoI Physical Node IBD's` and
`Component Logical Node BDD's` are plural in their own names; there is one per node, per
component. The flag is not tracking what it appears to track.

So: **79 is the number of diagram *kinds*. The number of *diagrams* is 79 plus whatever the
system tree multiplies out to**, and the sheet does not currently say which entries multiply.
Worth settling before anyone estimates the UModel effort from this list.

---

## Full inventory

`Per-item` reproduces the sheet's *Recursive for Each WBS Item* column exactly — `—` means
the cell is blank, which as above is not the same as "no".


### Phase C — Stakeholder Needs & Mission Analysis

| Criteria | Diagram | Type | Per-item | Tool |
| --- | --- | --- | --- | --- |
| C.2.2 | Views & Viewpoints Diagram | `pkg` | no | Umodel |
| C.4.1 | As-Is Operational Domain Diagram | `bdd` | no | Umodel |
| C.4.3 | Causal Analysis Fishbone Diagram | `(not SysML)` | — | Minitab |
| C.5.1 | Mission Requirements Diagrams | `req` | no | Umodel |
| C.5.2 | Updated View & Viewpoint Diagrams | `pkg` | no | Umodel |
| C.6.1 | MoE Parameter Diagrams | `par` | no | Umodel |
| C.8.1 | To-Be Operational Domain Block Definition Diagram | `bdd` | no | Umodel |
| C.9.1 | Mission Use Case Diagrams | `uc` | no | Umodel |

### Phase D — System Requirements & Operational Analysis

| Criteria | Diagram | Type | Per-item | Tool |
| --- | --- | --- | --- | --- |
| D.1.1 | Scenario Activity & Sequence  Diagrams | `act` | no | Umodel |
| D.2.1 | Operational Domain Internal Block Diagram | `ibd` | no | Umodel |
| D.3.1 | System Parameter Diagram | `par` | no | Umodel |
| D.3.1 | System Requirements Diagram | `req` | no | Umodel |
| D.4.2 | Updated System Requirements Diagrams | `req` | **yes** | Umodel |
| D.4.3 | Updated MoP/TPM Parameter Diagrams | `par` | **yes** | Umodel |
| D.6.1 | System of Interest Black-Box BDD | `bdd` | no | Umodel |
| D.9.1 | SoI State Machine Diagrams | `stm` | **yes** | Umodel |

### Phase E — Logical Architecture (System of Interest)

| Criteria | Diagram | Type | Per-item | Tool |
| --- | --- | --- | --- | --- |
| E.1.1 | System of Interest BDD | `bdd` | no | Umodel |
| E.1.2 | System of Interest Logical BDD | `bdd` | no | Umodel |
| E.2.1 | Subsystem Activity Diagrams | `act` | no | Umodel |
| E.2.2 | Subsystem Sequence Diagrams | `sd` | no | Umodel |
| E.2.3 | SubSystem BDD's | `bdd` | **yes** | Umodel |
| E.2.4 | Subsystem IBD's | `ibd` | **yes** | Umodel |
| E.3.1 | SoI Logical IBD | `ibd` | no | Umodel |
| E.4.2 | Logical Component State Machine Diagrams | `stm` | **yes** | Umodel |

### Phase F — SoI Node / Software / Hardware / Data Architecture

| Criteria | Diagram | Type | Per-item | Tool |
| --- | --- | --- | --- | --- |
| F.2.1 | SoI Logical Node Architecture BDD | `bdd` | — | Umodel |
| F.2.2 | SoI Logical Node Activity Diagrams | `act` | — | Umodel |
| F.2.3 | SoI Logical Node Sequence Diagrams | `sd` | — | Umodel |
| F.2.4 | SoI Logical Node IBD's | `ibd` | — | Umodel |
| F.3.2 | SoI Physical Node BDD's | `bdd` | — | Umodel |
| F.3.3 | SoI Physical Node Activity Diagrams | `act` | — | Umodel |
| F.3.4 | SoI Physical Node Sequence Diagrams | `sd` | — | Umodel |
| F.3.5 | SoI Physical Node State Diagrams | `stm` | — | Umodel |
| F.3.6 | SoI Physical Node IBD's | `ibd` | — | Umodel |
| F.4.1 | SoI Software Architecture BDD | `bdd` | — | Umodel |
| F.4.1 | SoI Software Node Software BDD's | `bdd` | — | Umodel |
| F.4.2 | SoI Software Activity Diagrams | `act` | — | Umodel |
| F.4.3 | SoI Software Sequence Diagrams | `sd` | — | Umodel |
| F.4.4 | SoI Software Architecture State Diagrams | `stm` | — | Umodel |
| F.4.5 | SoI UML Deployment Diagrams | `UML dep` | — | Umodel |
| F.4.5 | SoI Artifacts UML Deployment Diagrams | `UML dep` | — | Umodel |
| F.4.5 | SoI UML Component Diagrams | `UML comp` | — | Umodel |
| F.4.5 | SoI UML Class Diagrams | `UML class` | — | Umodel |
| F.5.1 | SoI Hardware Architecture BDD | `bdd` | — | Umodel |
| F.5.1 | SoI Hardware Node Hardware BDD | `bdd` | — | Umodel |
| F.6.1 | SoI Data Architecture BDD | `bdd` | — | Umodel |
| F.6.3 | SoI Entity Relationship Diagram | `ERD` | — | Umodel |
| F.6.4 | SoI Data Flow Diagram | `DFD` | — | Umodel |
| F.8.1 | Component Specification Requirements Diagram | `req` | — | Umodel |
| F.8.2 | Component Specification Blackbox BDD's | `bdd` | — | Umodel |

### Phase G — Component Architecture (repeats E+F per component)

| Criteria | Diagram | Type | Per-item | Tool |
| --- | --- | --- | --- | --- |
| G.2.1 | Component Logical BDD's | `bdd` | — | Umodel |
| G.2.2 | Component Subsystem Logical Activity Diagrams | `act` | — | Umodel |
| G.2.3 | Component Subsystem Sequence Diagrams | `sd` | — | Umodel |
| G.2.4 | Component Subsystem BDD's | `bdd` | — | **(blank)** |
| G.2.5 | Component Logical IBD's | `ibd` | — | Umodel |
| G.3.2 | Sub-Component Logical State Machine Diagrams | `stm` | — | Umodel |
| G.4.1 | Component Logical Node BDD's | `bdd` | — | Umodel |
| G.4.2 | Component Logical Node Activity Diagrams | `act` | — | Umodel |
| G.4.3 | Component Logical Node Sequence Diagrams | `sd` | — | Umodel |
| G.4.4 | Component Logical Node IBD's | `ibd` | — | Umodel |
| G.5.2 | Component Physical Node BDD's | `bdd` | — | Umodel |
| G.5.3 | Component Physical Node Activity Diagrams | `act` | — | Umodel |
| G.5.4 | Component Physical Node Sequence Diagrams | `sd` | — | Umodel |
| G.5.5 | Component Physical Node State Diagrams | `stm` | — | Umodel |
| G.5.6 | Component Physical Node IBD's | `ibd` | — | Umodel |
| G.6.1 | Component Software Architecture BDD's | `bdd` | — | Umodel |
| G.6.1 | Component Software Node BDD's | `bdd` | — | Umodel |
| G.6.2 | Component Software Node Activity Diagrams | `act` | — | Umodel |
| G.6.3 | Component Software Node Sequence Diagrams | `sd` | — | Umodel |
| G.6.4 | Component Software Node State Diagrams | `stm` | — | Umodel |
| G.6.5 | Component UML Deployment Diagram | `UML dep` | — | Umodel |
| G.6.5 | Component Artifacts UML Deployment Diagram | `UML dep` | — | Umodel |
| G.6.5 | Component UML Component Diagram | `UML comp` | — | Umodel |
| G.6.5 | Component UML Class Diagram | `UML class` | — | Umodel |
| G.7.1 | Component Hardware Architecture BDD | `bdd` | — | Umodel |
| G.8.1 | Component Data Architecture BDD's | `bdd` | — | Umodel |
| G.8.3 | Component Entity Relationship Diagrams | `ERD` | — | Umodel |
| G.8.4 | Component Data Flow Diagrams | `DFD` | — | Visio |

### Phase H — Analysis & Trade Studies

| Criteria | Diagram | Type | Per-item | Tool |
| --- | --- | --- | --- | --- |
| H.1.1 | Analysis Context BDD Diagrams | `bdd` | — | Umodel |
| H.1.2 | Analysis Context Parametric Diagram | `par` | — | Umodel |

---

## Totals by diagram type

What UModel actually has to produce, counted by kind rather than by phase:

| Type | Count | Notes |
| --- | --- | --- |
| `bdd` Block Definition | 23 | The bulk of it. Includes the 4 As-Is/To-Be operational domain diagrams |
| `act` Activity | 9 | |
| `ibd` Internal Block | 8 | |
| `sd` Sequence | 8 | |
| `stm` State Machine | 7 | |
| `req` Requirements | 4 | |
| `par` Parametric | 4 | MoE, MoP/TPM, system and analysis-context constraints |
| UML Deployment | 4 | Not SysML — UML, and UModel does both |
| UML Class | 2 | |
| UML Component | 2 | |
| `pkg` Package (Views & Viewpoints) | 2 | C.2.2 and its C.5.2 update |
| ERD Entity Relationship | 2 | |
| DFD Data Flow | 2 | |
| `uc` Use Case | 1 | **One.** C.9.1, mission-level |
| Fishbone | 1 | Minitab, not a SysML diagram at all |

**Two observations worth acting on.**

**Only one use-case diagram, at mission level.** Nothing below C.9.1 produces use cases;
the scenarios become activity and sequence diagrams at D.1.1 instead. That is a legitimate
OOSEM choice, but it means the use-case model never refines — worth confirming it is
deliberate rather than an omission, because it is cheap to notice now and expensive later.

**23 BDDs against 8 IBDs.** Structure is defined roughly three times as often as it is
connected. In OOSEM the IBD is where interfaces actually live, so a 3:1 ratio is worth a
second look — particularly at F.4.x and G.6.x (software), which produce architecture BDDs
and UML component/class diagrams but **no software IBD at all**.

---

## Six things in the sheet that need a decision

Found while extracting; none of them are blocking, all of them are cheaper to fix now than
after 79 diagrams exist.

| # | Where | What | Suggested resolution |
| --- | --- | --- | --- |
| 1 | **G.2.4** | Output `Component Subsystem BDD's` has **no tool in parentheses**. Every other artifact names one. | Almost certainly `(Umodel)` — it is a BDD, and its siblings G.2.1/G.2.5 are UModel. Confirm and fill in. |
| 2 | **G.8.4** | `Component Data Flow Diagrams (Visio)` — but its own SoI-level counterpart **F.6.4 is (Umodel)**. The same artifact, one level down, in a different tool. | Pick one. Splitting a diagram type across two tools by nesting depth guarantees they drift, and the DFD is exactly where that hurts. |
| 3 | **D.1.1** | One output bundles **two diagram types**: `Scenario Activity & Sequence Diagrams`. Counted here as one row, as the sheet has it. | Split into two criteria, or accept that this row is two diagrams. Everywhere else the sheet keeps activity and sequence separate (E.2.1/E.2.2, F.2.2/F.2.3). |
| 4 | **D.3.1** | Produces **two diagrams under one criterion** — `System Parameter Diagram` *and* `System Requirements Diagram` — and the criterion's description mentions neither ("Constraints Added to Systems Requirements Specification and List"). | Give the requirements diagram its own criterion; it is a major artifact hidden inside a documentation task. |
| 5 | **F.5.1 vs G.7.1** | F.5.1 produces **two** hardware BDDs (architecture *and* node). G.7.1 produces **one** — there is no component-level hardware-node BDD. | The only break in an otherwise exact E+F → G correspondence. Either it is deliberate (components have no internal hardware nodes) or G.7 is missing an entry. |
| 6 | **The Recursive column** | Set on 6 of 79. Blank on all of F and G, which are the most obviously per-item entries in the sheet. | **Not a spreadsheet defect — a decision that has not been made.** Chapter 17 gives the governing rule: *"The leaf level of the process is the level at which an element or component is procured or implemented,"* and *"the development team must determine what level of specification is appropriate for the particular application."* The multiplier is a property of the project's chosen leaf level, not of the diagram, which is why no cell in the sheet could carry it. Decide the leaf level and the column fills itself in. |

---

## What this does not tell you

Deliberately out of scope for this extraction, and each one is real work:

- **How much of the method a given user should follow.** This is the full OOSEM set for a
  ground-up development, and most users will not want all of it. Worth building **tailoring
  in as a feature rather than treating a subset as a shortcut**: Chapter 17 makes tailoring
  part of the management process, driven by *"the extent to which the system is a new
  design…, the system size and complexity, the available time and resources, and the level of
  experience of the development team."* The book itself ships a tailored subset — the
  simplified method in its Chapter 3 §3.4 **omits the logical architecture activity
  entirely.** A platform that only supports the full 79 supports almost nobody.
- **Diagram dependencies.** The sheet's Inputs column names them (C.4.3 consumes the Views &
  Viewpoints diagrams, C.3.1 pulls them from the library) and a real build order could be
  derived from it. Worth doing — under the platform framing this is what a *guided* workflow
  would need, rather than a list of diagrams a user is left to sequence themselves. Not done
  here.
- ~~**UModel project structure.**~~ **Answered** — see *Model organisation* and *One SoI per
  project* above. Figure 17.4 gives the layout, the naming conventions and the recursion rule;
  the per-project instantiation of it is the `Create` verb TOOLING.md has not built yet.
- **How a user's SoI hierarchy is declared.** The scaffolder needs to know the system tree
  before it can generate structure for it, and nothing in the spreadsheet or the chapter says
  where that declaration lives. It is the same question `ProjectPipeline` answers for stages,
  one dimension over.

---

## Regenerating this

The extraction is scripted and the source spreadsheet is the authority. If the sheet changes,
re-run rather than hand-editing: pull every line of the **Outputs** column matching
`diagram|BDD|IBD`, drop the lines containing *Template* (those are inputs named in an outputs
cell), and split the trailing parenthesis off as the tool. That last step is what surfaces
items 1 and 2 above.
