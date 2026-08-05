# SYSML_DIAGRAMS.md — The UModel Diagram Inventory

**Source:** `DWM_OOSEM_Criteria_and_Tasks.xlsx`, sheet `Criteria`, 499 rows, extracted
2026-08-05. Every row below is a named entry in that sheet's **Outputs** column — nothing
here is inferred, invented, or filled in from what OOSEM "usually" wants.

**79 modelling artifacts. 76 of them are UModel.** That is the number that matters for
scoping the UModel work, and it is larger than a scan of the sheet suggests, for a reason
worth stating first.

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
| 6 | **The Recursive column** | Set on 6 of 79. Blank on all of F and G, which are the most obviously per-item entries in the sheet. | Fill it in, or the diagram count cannot be turned into an estimate. See "The count is a floor" above. |

---

## What this does not tell you

Deliberately out of scope for this extraction, and each one is real work:

- **Which diagrams DWM actually needs for the MVP.** This is the full OOSEM set for a
  ground-up development. The MVP is a wind turbine in a LETS economy simulation, and most of
  phases F and G describe a level of decomposition it will never reach. **Selecting a subset
  is a separate decision and should be logged in SCOPE.md when it is made** — the value of
  having the full list is knowing exactly what is being skipped.
- **Diagram dependencies.** The sheet's Inputs column names them (C.4.3 consumes the Views &
  Viewpoints diagrams, C.3.1 pulls them from the library) and a real build order could be
  derived from it. Not done here.
- **UModel project structure.** Package layout, naming conventions, and how the recursion in
  phase G is represented — one package per component, or one model with a component
  hierarchy — is a modelling decision, not a list.

---

## Regenerating this

The extraction is scripted and the source spreadsheet is the authority. If the sheet changes,
re-run rather than hand-editing: pull every line of the **Outputs** column matching
`diagram|BDD|IBD`, drop the lines containing *Template* (those are inputs named in an outputs
cell), and split the trailing parenthesis off as the tool. That last step is what surfaces
items 1 and 2 above.
