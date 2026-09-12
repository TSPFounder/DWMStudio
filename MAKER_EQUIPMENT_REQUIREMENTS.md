# Maker Equipment — Mission and System Requirements

**Status:** FIRST TAKE. Draft for discussion; nothing here is baselined.
**Date:** 2026-09-12
**Scope:** The affordable, open-source manufacturing tooling of Project Goal 5 — CNC router, laser cutter/engraver, 3D printers, foundry, lathe, mill and drill press, sheet-metal and press tooling, CNC lathe.
**Traces to:** `Dream_World_Maker_Project_Goals.md` §§2, 4, 5, 6; `SCOPE.md` decisions dated 2026-08-01 and 2026-08-02.

---

## 0. How to read this

Requirements are numbered by group and carry three things: the **statement** (`shall` = binding, `should` = preferred), the **rationale**, and how it is **verified**. A requirement with no verification method is an aspiration, and this document tries not to contain any.

Three labels appear throughout, and the difference matters:

| Label | Meaning |
|---|---|
| **DECIDED** | Already settled in `SCOPE.md` or the Goals document. Restated here, not re-opened. |
| **PROPOSED** | This document's suggestion. Needs a decision before it binds. |
| **OPEN** | A genuine gap. Listed in §13 rather than papered over. |

**What this document is not.** It is not a purchase list, not a build plan, and not a schedule. It also does not claim any of this is implemented — per `SCOPE.md` (2026-08-01), the manufacturing arc is **explicitly outside MVP scope** and appears in the MVP as static set-dressing at most. This document exists so that when the arc does get built, the requirements were written before the machines were chosen.

---

## 1. Purpose

Project Goal 5 commits DWM to developing *"affordable, open-source manufacturing tooling, designed for low construction cost, maintainability, and practical reproduction."* Goal 4 commits to a library of open-source designs. Those two commitments only connect if the tooling can actually make what the library contains.

This document defines what that tooling must do. It covers both halves of the project's dual nature, because they impose different requirements on the same machines:

- **Real equipment** — machines a real community (including this one) builds, owns and runs. Bound by physics, budget and skill.
- **Simulated equipment** — the representation of those machines inside DWM, which must be truthful enough that a player who learns the tool in the game has learned something about the tool in the world.

Where a requirement applies to only one, it says so.

---

## 2. Mission context

From the Goals document, three constraints shape everything below.

**The economy has two moneys, and only one of them is renewable.** Internal exchange runs on the **Stone** (1 hour of labour = 1 St), mutual credit, netting to zero across the network. Purchases from outside the network draw on the **Dollar Vault**, which is finite and does not refill from trade. A machine the communities build costs Stones — labour they have. A machine they buy costs Dollars — the scarce thing. **This single distinction is the strongest driver in this document.**

**No community is meant to be self-sufficient alone.** Goal 2 is explicit that capability is distributed and cooperation is the point. `SCOPE.md` (2026-08-01) turns this into structure: the dependency chain *is* the quest structure, and no single community holds the whole of it.

**Capability claims must survive contact with physics.** The 2026-08-01 laser entry in `SCOPE.md` exists because the marketing figure ("cuts 0.1–0.15 mm stainless") and the useful figure (sheet metal starts around 1.5 mm) are different claims, and only one of them is true of the work. That discipline is a requirement here, not a footnote — see MR-7.

---

## 3. The sizing problem, stated up front

Goal 5's table says CNC routers make *"molds for turbine blades."* The MVP turbine's blade, per `DWM_Dev/Models/Fusion/MVP_WindTurbine/README.md`, is **58.5 m long**, root chord 2.6 m, max chord 3.9 m, 3.0 m prebend.

Those two statements cannot both describe the same machine.

| | Community-scale, single-piece mold | The MVP turbine |
|---|---|---|
| Blade length | ~2.4 m | 58.5 m |
| Rotor diameter | ~5.4 m | ~120 m |
| Swept area | 22.9 m² | 11,310 m² |
| Ratio | — | **24× the blade, 494× the area** |

A 58.5 m blade mold is a dedicated-facility, capital-intensive object. It is not made on a community CNC router at any plausible size, and no amount of requirement-writing changes that.

**The resolution is already in the source material.** Goal 5's closing line describes the turbine story's ending as: *"use the immediate repair to gain time and knowledge, then develop smaller machines and the shared tools to make them."* The Mountain's turbine is an **inherited utility-scale machine the communities repair**. The turbines they *build* are the smaller ones. The router serves the second, never the first.

This document therefore sizes the router from sheet stock and derives the turbine that fits it, rather than the reverse:

> Standard sheet goods are 2440 × 1220 mm. A router that works a full sheet cuts a single-piece blade mold up to ~2.4 m. With a 0.3 m hub radius that is a 5.4 m rotor, 22.9 m² swept — about **2.5 kW at 8 m/s and 6.5 kW at 11 m/s** (Cp = 0.35, ρ = 1.225 kg/m³). That is a credible community machine, and it falls out of the sheet size rather than being chosen.

Blades longer than the bed need segmented molds, which is a normal technique and not a defeat — but it is a decision, not an assumption. See **OPEN-1**.

---

## 4. Mission requirements

| ID | Requirement | Rationale | Verification |
|---|---|---|---|
| **MR-1** | The tooling set **shall** enable a community to make, modify and repair the physical items in the open-source library without requiring a machine tool it can neither build itself nor afford to buy. | Goal 5. A library of designs nobody can fabricate is a catalogue, not a capability. | Trace every item in the library BOM to at least one tool in the set that can make it, or to an explicit external-purchase entry. |
| **MR-2** | The set **shall** be reachable by an incremental bootstrap in which earlier tools produce parts for later ones, starting from hand tools and purchased stock only. | Goal 5; `SCOPE.md` 2026-08-01 (Gingery chain). Makes the capability reproducible by a community that starts with nothing. | Produce a dependency graph from tool to tool with no cycle and no unresolved external machine-tool dependency at the root. |
| **MR-3** | No single community **shall** hold every stage of the bootstrap chain. | Goal 2; `SCOPE.md` 2026-08-01, which makes the dependency chain the quest structure. Cooperation must be structural, not exhorted. | Inspect the allocation in §8: every complete artifact must cross at least one community boundary. |
| **MR-4** | Each tool design **shall** minimise, and **shall** explicitly enumerate, the parts that must be bought from outside the network. | Goal 3 (Dollar Vault). Outside purchases consume the non-renewable money. What cannot be made locally must at least be *visible*. | Every tool's BOM carries a per-line Stone/Dollar designation; the Dollar subtotal is a published figure. |
| **MR-5** | Every tool in the set **shall** itself be a library item, carrying the full design package of Goal 4. | Goal 4/5. Tooling that is not documented to the same standard as what it makes cannot be reproduced, serviced or improved. | Package completeness check against the §11 checklist. |
| **MR-6** | The set **shall** accept recovered and salvaged material as feedstock wherever the process allows. | Goal 2; `SCOPE.md` 2026-08-01/02 (Suburb scrap recovery feeds the City foundry; every community recycles; Valley composts). | Each process states its accepted feedstock, including the salvage grades and the contaminants it rejects. |
| **MR-7** | Stated capability **shall** be bounded by demonstrated physics, not by manufacturer specification. Where the two differ, the document **shall** record both and say which governs the work. | `SCOPE.md` 2026-08-01 laser entry. The failure mode this project keeps hitting is a confident claim nobody checked. | Each capability figure cites its source and, where it matters, the test that established it. |
| **MR-8** | Each tool **shall** be representable in DWM with sufficient fidelity to drive the economy and energy models — at minimum a time-per-operation model and an electrical demand profile. | Goal 6; the ledger prices output in Stones (1 hr = 1 St), so machine time *is* money. Energy is a simulated resource. | The DWM tool record carries both figures; ledger entries for fabricated goods reconcile against them. |
| **MR-9** | Affordability **shall** be assessed over the full life — construction, consumables, maintenance, and reproduction of the tool — not first cost alone. | Goal 5 states this explicitly. A cheap machine with a proprietary consumable is not affordable. | Published life-cost model per tool, with consumable and wear-part costs separated from build cost. |
| **MR-10** | Each tool **shall** be buildable and maintainable by a competent amateur with the set's own prior tools, and its documentation **shall** state the skills assumed. | Goal 5 (practical reproduction); Goal 7 (users become able to make and repair). An unstated skill floor is a hidden dependency. | Skill prerequisites listed per build step; no step requires a trade qualification without saying so. |

---

## 5. The bootstrap chain — the organising constraint

`SCOPE.md` (2026-08-01) fixes the chain, following Gingery's *Build Your Own Metal Working Shop From Scrap*:

**charcoal foundry → lathe → shaper → drill press → dividing head → mill → sheet metal brake**, cast largely from scrap aluminium.

Modernised in two places, both already decided:

- **3D printers replace carved wood patterns.** Printed patterns are the modern route into the foundry.
- **The CNC lathe is a modification of the Gingery lathe design**, not a separate machine.

The production dependency that makes the chain a *structure* rather than a list:

```
   3D printer ──▶ casting pattern ──▶ foundry ──▶ raw casting ──▶ lathe / mill ──▶ finished part
        │                                  ▲                                            │
        │                                  │                                            ▼
   laser ──▶ pattern, core box,      salvaged metal                             parts for the NEXT machine
             jig, template            (Suburb → City)
```

**SR-BOOT-1** — The chain **shall** be traversable in dependency order with no step requiring a tool from a later step.
*Verification:* topological sort of the tool-to-tool dependency graph; a cycle is a defect.

**SR-BOOT-2** — Where a step cannot yet be met internally, the gap **shall** be recorded as an explicit external purchase with its Dollar cost, not silently assumed.
*Rationale:* MR-4, MR-7. The first lathe has to come from somewhere, and pretending otherwise breaks the bootstrap claim at its root.

> **Note on the first machine.** Gingery's chain bootstraps a *machine shop* from a foundry, but the foundry's own build, the crucible, and the initial stock are purchased. The honest statement is that the chain removes the need to buy *most* machine tools, not that it starts from literally nothing. **OPEN-2.**

---

## 6. System-level requirements

These apply to the kit as a whole. Machine-specific requirements are in §7.

### 6.1 Interoperability with the existing DWM toolchain

| ID | Requirement | Verification |
|---|---|---|
| **SR-1** | Every CNC machine in the set **shall** accept toolpaths derived from the CAD artifacts the DWM pipeline already produces (Fusion via `CAD_Library`/`FusionLibrary`), without hand re-modelling. | Round-trip a library part from CAD artifact to machine-ready file with no manual geometry recreation. |
| **SR-2** | The CAD toolchain **shall** support deriving a *casting pattern* model from a *part* model — shrinkage scaling, draft, machining allowance, parting line — as a recorded operation rather than a manual edit. | A pattern generated from a part in `CAD.Scripting`'s operation IR, re-runnable when the part changes. |
| **SR-3** | All CNC machines in the set **should** share one open, documented control stack (e.g. GRBL/FluidNC class for the light machines, LinuxCNC class for the heavy). | One documented control stack; spares and skills demonstrably common across machines. |
| **SR-4** | Machine-readable capability data — work envelope, materials, tolerance, time model, power — **shall** be published per tool in a form DWM can consume. | DWM tool records populate from the published data without transcription. |

**On SR-2.** This is the requirement most likely to be dismissed as a nicety and it is not one. Aluminium shrinks roughly **1.3%** on solidification; brass and bronze roughly **1.5%**. A pattern made from the part model at 1:1 produces a casting that is uniformly undersize, and the error is invisible until the part is measured. Add draft (**1.5–2° minimum** on vertical faces) and machining allowance (**1.5–3 mm** on surfaces to be finished) and a pattern is a genuinely different model from the part. Doing that by hand, every time the part changes, is where the mistakes will come from.

### 6.2 Physical and infrastructure envelope

| ID | Requirement | Rationale |
|---|---|---|
| **SR-5** | Each tool **shall** publish peak and average electrical demand, and its duty cycle. | MR-8. Energy is a simulated and a real constraint; the Mountain's whole storyline is an energy asset. |
| **SR-6** | The set **should** operate from single-phase supply, with any three-phase requirement called out as a siting constraint. | Community buildings and off-grid supply are single-phase. A three-phase machine is a building decision, not just a tool decision. |
| **SR-7** | Each tool **shall** publish its footprint, working clearances and mass, including the clearance needed to load full-size stock. | A 2440 mm bed needs ~5 m of aisle to feed a full sheet. Footprint alone misleads. |
| **SR-8** | Each tool **shall** state its extraction, ventilation and fire-separation requirements as part of the tool definition, not as shop-fitting advice. | §10. MDF dust, laser fume, foundry flue and metal fire are tool properties, not optional extras. |

### 6.3 Commonality

| ID | Requirement | Rationale |
|---|---|---|
| **SR-9** | The set **should** standardise on one fastener system, one stock-size family and a common workholding interface across machines. | Goal 5: *"a common set of designs and compatible parts that several communities can use."* Also halves the spares inventory. |
| **SR-10** | Wear parts and consumables **should** be either locally producible or available from more than one outside supplier. | MR-9. A single-source consumable is a dependency wearing a different hat. |

---

## 7. Machine requirements

Figures marked **PROPOSED** are this document's suggestion and need a decision. Figures marked **DECIDED** come from `SCOPE.md`.

### 7.1 CNC router — `RTR`

Primary role: mold and plug machining, sheet goods, jigs, fixtures, building components.

| ID | Requirement | Basis |
|---|---|---|
| **RTR-1** | Work envelope **shall** be at least 2440 × 1220 mm in X–Y. | **PROPOSED.** Full standard sheet; see §3. Any smaller and every sheet needs breaking down first. |
| **RTR-2** | Z clearance **shall** be at least 150 mm, with cutting depth sufficient for mold cavity depth. | **PROPOSED.** Derived from blade mold section depth; confirm against the community turbine once OPEN-1 closes. |
| **RTR-3** | Positional accuracy **should** be ±0.2 mm or better over 1 m. | **PROPOSED.** Adequate for molds that are faired by hand afterward; not adequate for machine-tool metalwork, which is the lathe's job. |
| **RTR-4** | Materials: MDF, plywood, tooling board, machinable foam, solid timber, plastics. Non-ferrous metal in light cuts **should** be possible but is not the design case. | A gantry stiff enough for production aluminium is a different, costlier machine. |
| **RTR-5** | Dust extraction **shall** be integral, not an accessory. | §10. MDF dust is a respiratory hazard, and this machine's primary material is MDF. |
| **RTR-6** | The machine **shall** be capable of producing its own replacement flat parts. | MR-2. Closes the reproduction loop. |

### 7.2 Laser cutter / engraver — `LSR`

**This machine is already owned**, which makes it the one place where capability is measured rather than specified.

**DECIDED** (`SCOPE.md` 2026-08-01): Creality Falcon2 40W diode (8 × 5.5 W combined), in a Creality Laser Engraver Enclosure 2.0, 28.3 × 28.3 × 15.7 in, vented and fireproof.

| ID | Requirement | Basis |
|---|---|---|
| **LSR-1** | The laser's role **shall** be jigs, fixtures, templates, gaskets, stencils, and **foundry patterns and core boxes** — not sheet-metal cutting. | **DECIDED.** Manufacturer spec is 0.1–0.15 mm stainless in one pass: that is shim and foil. A drink-can wall is ~0.10 mm; typical brake-formed parts are 1.5 mm. It cannot cut usable sheet metal. |
| **LSR-2** | Rated cutting capability **shall** be stated as ~30 mm acrylic, 20 mm cork, plywood and MDF. | **DECIDED.** What it actually does well. |
| **LSR-3** | The marking workflow **shall** be supported as a first-class capability: scribing cut lines, bend lines, hole centres and part numbers onto sheet of **any** gauge, for manual cutting afterward. | **DECIDED.** Marking is a different energy budget from cutting and is thickness-independent. This is the workaround that makes the machine useful on metal at all. |
| **LSR-4** | The set **shall** include a **throatless (Beverly) shear or nibbler** wherever LSR-3 is used on curved profiles. | **DECIDED.** A guillotine or bench shear cuts straight lines only. Without this, the marking workflow only half works. |
| **LSR-5** | Hand-cut-to-scribed-line accuracy **shall** be planned at ±0.5 mm. | **DECIDED.** Design tolerances downstream must accept it. |
| **LSR-6** | The machine **shall not** be pointed at bare reflective stock at cutting power. | **DECIDED, safety-critical.** Polished aluminium at normal incidence reflects a large fraction of the beam back into the module. Diode arrays have **no optical isolator** (fiber lasers do), so back-reflection destroys the diodes and optics **with no obvious warning**. |
| **LSR-7** | Aluminium marking **shall** use one of three routes: (a) anodised — excellent, true material removal 5–25 µm; (b) bare — requires CerMark/moly compound, which is an *additive bonded layer*, not a recess; (c) real depth in bare aluminium — laser-ablate a resist coating, then chemically etch, then strip. | **DECIDED.** Route (c) burns paint rather than metal, so reflectivity stops mattering. |
| **LSR-8** | Graduated dials and scales **should** be produced by laser-etching anodised aluminium. | **DECIDED.** Exactly what the Gingery dividing head (book 5) needs, and far easier than dividing them mechanically. Also machine nameplates and control-panel legends. |

> **General rule, worth keeping visible:** CO₂ lasers do **not** cut bare metal (10.6 µm is highly reflective); fiber lasers do; diodes mark it. If metal profile cutting is ever wanted, that is a fiber laser or a CNC plasma table — a different purchase, with different economics and different requirements.

### 7.3 3D printers — `PRN`

Primary role per `SCOPE.md`: jigs, fixtures, and **casting patterns** — the modern replacement for Gingery's carved wood patterns.

| ID | Requirement | Basis |
|---|---|---|
| **PRN-1** | Printers **shall** produce casting patterns and core boxes dimensionally suitable for the foundry, incorporating shrinkage, draft and machining allowance per SR-2. | **DECIDED** role; allowances **PROPOSED**. |
| **PRN-2** | Build volume **should** be at least 250 mm cubed, with larger patterns split and bonded on a documented joint scheme. | **PROPOSED.** Derive the real floor from the largest casting in the tooling BOM. |
| **PRN-3** | Pattern surface finish **shall** be specified to the standard the moulding sand requires, including any sealing or filling step. | Layer lines transfer to the casting and then have to be machined off — paying twice. |
| **PRN-4** | Printer feedstock **shall** be treated as an outside purchase against the Dollar Vault unless and until local filament production is established. | MR-4. Filament is an import. Saying so keeps the Vault honest. |

### 7.4 Foundry — `FDY`

**DECIDED:** charcoal-fired, casting largely from scrap aluminium, sited with the City, fed by Suburb scrap recovery.

| ID | Requirement | Basis |
|---|---|---|
| **FDY-1** | The furnace **shall** melt and pour aluminium alloys — melt ~660 °C, pour ~700–760 °C. | **PROPOSED** figures, standard practice. |
| **FDY-2** | Melt capacity **shall** be at least the largest single casting in the tooling BOM plus sprue and riser allowance, conventionally 1.5–2× part mass. | **PROPOSED.** Derived rather than guessed; needs the BOM to close. |
| **FDY-3** | Copper-alloy capability (brass/bronze, pour ~950–1100 °C) **should** be available for bearings and fittings. | Bushings and fittings are named in Goal 5's lathe row and are classically bronze. |
| **FDY-4** | Ferrous casting is **out of scope** for the charcoal foundry and **shall** be stated as such. Cast iron needs ~1200–1400 °C — cupola or induction territory. | MR-7. A capability limit that will otherwise get quietly assumed. |
| **FDY-5** | Accepted salvage feedstock **shall** be specified by grade, with rejected contaminants named. | MR-6. |
| **FDY-6** | All charge material and tooling **shall** be dry before contact with melt. | **Safety-critical**, §10. |

> **Consistency item.** `SCOPE.md` (2026-08-02) derives iron-oxide pigment from *"the City's foundry (mill scale and slag)."* Mill scale comes from hot-worked **steel**. Either the City has a ferrous capability beyond the charcoal foundry, or the pigment source is salvage rather than the foundry's own process. Minor, but it should be settled rather than inherited. **OPEN-3.**

### 7.5 Lathe, mill, drill press, shaper, dividing head — `MCH`

The core Gingery chain. Cast from foundry output, finished by each other.

| ID | Requirement | Basis |
|---|---|---|
| **MCH-1** | Swing, between-centres and table travel **shall** be derived from the largest turned and milled parts in the tooling and library BOMs — not adopted from the Gingery baseline by default. | **PROPOSED.** The Gingery lathe is a small bench machine; large shafts are outside it and must be named as external work. |
| **MCH-2** | Achievable tolerance and surface finish **shall** be published per machine, as-built rather than as-designed. | MR-7. A scraped-in machine's accuracy is a property of the build, not the drawing. |
| **MCH-3** | Each machine **shall** be capable of producing the castings-to-finished-parts for the next machine in the chain. | MR-2. This is what makes it a chain. |
| **MCH-4** | Dividing-head graduations **should** be produced per LSR-8. | **DECIDED** in the laser entry. |

### 7.6 Sheet metal and press tooling — `SHM`

| ID | Requirement | Basis |
|---|---|---|
| **SHM-1** | The brake **shall** form the gauges the library's sheet parts actually use; 1.5 mm mild steel is the working assumption until the BOM says otherwise. | **PROPOSED.** |
| **SHM-2** | A throatless shear or nibbler **shall** be present per LSR-4. | **DECIDED.** |
| **SHM-3** | Bend allowance and minimum bend radius for each gauge and material **shall** be published, and the CAD flat-pattern step **shall** use them. | Flat patterns computed with the wrong K-factor produce parts that are wrong after bending and right on the screen. |
| **SHM-4** | Scribed layout lines **shall not** cross the tension surface of any cyclically loaded part. | **DECIDED, engineering-critical.** A scribed line is a stress raiser — the same fatigue reasoning that drives material selection in `Wind_Turbine_BOM.xlsx`. |

### 7.7 CNC lathe — `CLA`

**DECIDED:** built as a modification of the Gingery lathe design.

| ID | Requirement | Basis |
|---|---|---|
| **CLA-1** | The conversion **shall** preserve manual operation. | Losing a working manual lathe to gain a CNC one is a net loss of capability, and the manual machine is the fallback when the control fails. |
| **CLA-2** | Control **shall** conform to SR-3's common stack. | Commonality of skills and spares. |
| **CLA-3** | The conversion **shall** document the accuracy it adds *and the backlash it inherits* from the host machine. | MR-7. A stepper does not fix a worn leadscrew; it automates it. |

---

## 8. Community allocation

**PROPOSED** in `SCOPE.md` (2026-08-01), refined by the 2026-08-02 recycling entry:

| Community | Role in the manufacturing arc | Status |
|---|---|---|
| **Hillside** | Design; 3D printers; laser. Engineering services. | DECIDED |
| **City** | Heavy machines; the foundry; the CNC lathe. Consumes scrap feedstock. | DECIDED |
| **Suburb** | Scrap recovery and foundry feedstock. Their existing ambient dialogue already establishes them as recyclers — *"half that control station's housing used to be something else entirely"* — so no retrofit is needed. | DECIDED |
| **Mountain** | Assembly. The turbine site. | DECIDED |
| **Valley** | Composting and biomass; food. Sorted wood into the Hillside press loop. | Partially resolved — see note |

> The 2026-08-01 entry recorded Valley's absence from this arc as an open question; the 2026-08-02 recycling entry gives them composting and biomass, and the wood loop gives them a materials role. Whether that is *enough* of a manufacturing role, or whether Valley should hold a tool, is still worth a decision. **OPEN-4.**

**SR-ALLOC-1** — Every community **shall** hold at least one capability the others need, and **no** community shall hold a complete chain.
*Verification:* the allocation table plus the §5 dependency graph; any single-community complete path is a defect against MR-3.

**SR-ALLOC-2** — Cross-community transfers of parts, patterns and feedstock **shall** be expressible as ledger trades in Stones.
*Rationale:* Goal 3. If the chain cannot be priced, it cannot be played.

---

## 9. Economic requirements

This is the group that distinguishes DWM's tooling requirements from any other maker-space specification.

| ID | Requirement | Rationale |
|---|---|---|
| **ECO-1** | Each tool **shall** publish a time-per-operation model sufficient to price its output in Stones at 1 hour = 1 St. | Goal 3. The ledger prices labour by the hour; machine time is how fabricated goods enter the economy. Without this, a made object has no price. |
| **ECO-2** | Each tool's BOM **shall** carry a per-line Stone/Dollar designation, and publish the Dollar subtotal. | MR-4. The Vault is finite and the drain must be visible per decision, not per year. |
| **ECO-3** | Consumables **shall** be classified as locally producible (Stone) or imported (Dollar), with the import list maintained as a standing target for substitution. | Goal 2 (*"reduce recurring dependence on outside purchases"*). Consumables are the recurring drain — worse than capital cost, because they never stop. |
| **ECO-4** | Where a tool displaces an outside purchase, the displaced Dollar cost **should** be recorded, so the tool's payback is expressible in the project's own terms. | Makes "affordable" measurable rather than rhetorical. |
| **ECO-5** | Build effort **shall** be estimated in Stones, so that building a tool and buying one are comparable in the model. | MR-9. The whole argument for building rather than buying is that it spends the renewable money. That argument needs numbers. |

> **Precedent already set.** The 2026-08-02 paint decision worked exactly this way: the palette is what the communities can make; any other colour is available *"from outside vendors at a Dollar cost, not a Stone trade"* — and `CommunityDollarVaultLedger` already carries `CommunityId`, a signed `DeltaAmount` and a free-text `Reason`, so recording it needs no schema change. The tooling arc should reuse that mechanism rather than invent one. **This is a solved problem; don't re-solve it.**

---

## 10. Safety requirements

Safety items belong in the tool definition, not in shop-fitting advice appended afterward. The laser items below are **DECIDED** and quoted from `SCOPE.md`.

| ID | Requirement | Hazard |
|---|---|---|
| **SAF-1** | PVC and vinyl **shall never** be lasered. | Chlorine gas; it also corrodes the machine. |
| **SAF-2** | Galvanised and coated steel **shall never** be lasered, **even for marking**. | Zinc fumes. The enclosure's extraction reduces exposure; it does not eliminate it. |
| **SAF-3** | MDF machining **shall** have extraction and a proper respirator — not a nuisance mask. | Genuine respiratory hazard, and MDF is the router's primary material. |
| **SAF-4** | The laser **shall not** be aimed at bare reflective stock at cutting power; scuff to matte or use marking compound. | Back-reflection destroys the diode module silently (LSR-6). Scuffing also kills the reflectivity that spoils the mark. |
| **SAF-5** | Resist-mask etching with sodium hydroxide **shall** be ventilated, kept clear of ignition sources, and done with mandatory eye protection. | NaOH on aluminium **evolves hydrogen** — flammable, and it accumulates. |
| **SAF-6** | All foundry charge material, tools and moulds **shall** be dry before contact with molten metal. | Water flashing to steam under melt is the foundry's characteristic catastrophic failure. |
| **SAF-7** | Foundry operation **shall** specify PPE, flue and extraction, a defined pour path, and a spill response. | Molten metal, CO from charcoal. |
| **SAF-8** | Each tool's definition **shall** name its hazards, required PPE and required extraction as published fields. | SR-8. A hazard recorded only in prose is a hazard that gets skipped. |
| **SAF-9** | Class D (metal) fire provision **shall** be specified for the foundry area; water **shall not** be used on a metal fire. | Distinct from the shop's general fire provision, and the wrong extinguisher makes it worse. |

---

## 11. Documentation and library requirements

Per Goal 4, each tool's library package **shall** contain:

- [ ] The need it addresses, its requirements, and its intended operating conditions
- [ ] Editable design files **and** accessible exchange formats
- [ ] Drawings, dimensions, bills of materials, suitable material choices
- [ ] Manufacturing, assembly, operation, maintenance and repair information
- [ ] Relevant calculations, simulation results, test evidence, and **known limitations**
- [ ] Version history, contributor attribution, explicit open-source licensing

Plus, specific to tooling:

| ID | Requirement |
|---|---|
| **DOC-1** | The package **shall** include the tool's published capability data per SR-4 — envelope, materials, tolerance, time model, power, hazards. |
| **DOC-2** | The package **shall** state which prior tools in the chain are needed to build it, per MR-2. |
| **DOC-3** | Capability figures **shall** distinguish *measured* from *specified*, per MR-7. |
| **DOC-4** | Commercial software or assets used during development **shall not** be represented as part of the open-source package. | 
| **DOC-5** | The package **should** record what was tried and rejected, and why. |

> **DOC-3 and DOC-5 are the ones that will get skipped, and they are the two that carry the most value.** The laser entry in `SCOPE.md` is worth more than a datasheet precisely because it records that the manufacturer's stainless figure describes shim rather than sheet, and that the aluminium route was found by elimination. A package that records only the final answer forces the next community to rediscover the eliminations.

---

## 12. Verification approach

Requirements here are verified by one of four methods, and each requirement above names one:

| Method | Applies to |
|---|---|
| **Inspection** | Documentation completeness, allocation structure, BOM designations. |
| **Analysis** | Dependency graphs, energy and time models, life-cost, tolerance stack-ups. |
| **Demonstration** | A tool making the part the next tool needs. The chain's real acceptance test. |
| **Test** | Measured capability — accuracy, envelope, melt capacity, cut and mark limits. |

**VER-1** — The set's acceptance criterion **shall** be an end-to-end demonstration: a part designed in the DWM CAD pipeline, patterned by printer or laser, cast in the foundry, machined on the chain's own machines, and recorded in the ledger as a priced trade crossing at least one community boundary.
*Rationale:* every mission requirement is exercised by that one demonstration, and nothing short of it proves the chain closes.

**VER-2** — A capability **shall not** be recorded as verified on the strength of a tool reporting success. Where the tool can report success without having done the work, the output **shall** be inspected directly.
*Rationale:* this project's most repeated failure, across MATLAB, FEMAP, MYSTRAN and the Fusion bridge. It applies to machine controllers exactly as it applies to software — a G-code interpreter that runs to completion has not necessarily cut the part.

---

## 13. Open questions

| ID | Question | Why it matters | Blocks |
|---|---|---|---|
| **OPEN-1** | What is the community-scale turbine's rotor diameter, and are blade molds single-piece or segmented? | Sizes the router (RTR-1, RTR-2) and settles §3. The 2.4 m / 5.4 m / ~2.5 kW figures here are derived from sheet size as a *placeholder*, not chosen. | RTR-1, RTR-2 |
| **OPEN-2** | What exactly is purchased to start the chain — furnace build, crucible, first stock, the shear? | MR-4 and the honesty of the bootstrap claim. | SR-BOOT-2 |
| **OPEN-3** | Does the City have ferrous capability beyond the charcoal foundry, or is the pigment's mill scale salvage? | FDY-4 vs the 2026-08-02 pigment entry. | FDY-4 |
| **OPEN-4** | Does Valley hold a tool, or only materials? | MR-3 and the 2026-08-01 entry's own open question. | §8 |
| **OPEN-5** | Largest casting, turned part and sheet part in the library BOM. | Every derived-from-BOM requirement — FDY-2, MCH-1, SHM-1, PRN-2 — is parked until these are known. | FDY-2, MCH-1, SHM-1, PRN-2 |
| **OPEN-6** | Is the adhesive import substituted or accepted? | `SCOPE.md` 2026-08-02 plants soy/starch/lignin bio-adhesives off the Valley's biofuel fields but deliberately does **not** resolve it. Affects ECO-3. | ECO-3 |
| **OPEN-7** | Are these requirements for real machines, simulated machines, or both — per tool? | This draft assumes both, with the laser real and the rest aspirational. Worth stating deliberately. | §1 |

---

## 14. Sources

| Source | Used for |
|---|---|
| `DWMStudio/Dream_World_Maker_Project_Goals.md` §§2, 4, 5, 6, 7 | Mission framing, the Goal 5 tooling table, library package contents, the development cycle, Stone/Dollar Vault |
| `DWMStudio/SCOPE.md` 2026-08-01 (manufacturing arc) | Gingery chain, printer-as-patternmaker, CNC lathe as Gingery modification, community allocation, the dependency-chain-as-quest-structure |
| `DWMStudio/SCOPE.md` 2026-08-01 (laser capability) | Every figure in §7.2, and SAF-1 through SAF-5 |
| `DWMStudio/SCOPE.md` 2026-08-02 (recycling, paint, wood) | Feedstock roles, the Dollar-cost precedent in ECO-2, Valley's partial role, the adhesive import |
| `DWM_Dev/Models/Fusion/MVP_WindTurbine/README.md` | The 58.5 m blade and its planform — the basis of §3 |
| `DWM_Dev/Wind_Turbine_BOM.xlsx` (referenced, not read) | Cited by `SCOPE.md` for the fatigue reasoning behind SHM-4 |

Derived figures in §3 (swept area, power) are computed here from the stated geometry at Cp = 0.35 and ρ = 1.225 kg/m³; they are indicative, not a turbine design.

Unattributed numeric values — shrinkage, draft, machining allowance, pour temperatures, melt ratios — are standard foundry and machining practice stated as **PROPOSED** starting points. They are not measured on this equipment and should be treated as placeholders until they are.
