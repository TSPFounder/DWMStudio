# SCOPE.md — Dream World Maker MVP

**Target:** Playable vertical slice by **September 1, 2026**
**Engine:** Unreal Engine 5.3 (both DWM and DWM_Dev for the MVP)
**Rule:** The date is fixed. Fidelity flexes. Nothing moves from *Out* to *In* without a dated entry in the Decisions Log below, stating what is being cut to make room.

---

## Definition of Done

The MVP is shipped when a person can, without developer intervention:

1. Load the DWM world in UE 5.3 and move an avatar through five distinct communities (Mountain, Hillside, Valley, Suburb, City).
2. See each community's headline resources and current Stone (St) balance on screen, sourced from the authoritative SQLite ledger.
3. Execute at least one cross-community trade that mints Stone correctly (buyer negative, seller positive, network total = 0).
4. Watch a Dollar Vault deplete and a Cascading Failure warning state trigger when a vault is exhausted.
5. Observe one engineered system promoted through DWM_Dev → verification gate → DWM (the Mountain wind-turbine driven by real Simscape physics).

---

## Frozen Scope Table

| In Scope (MVP) | Out of Scope (Backlog) |
| --- | --- |
| 5 communities: Mountain, Hillside, Valley, Suburb, City | Port, Island, Desert, Seafloor |
| Stone (St) LETS ledger + mutual-credit minting | Speculation / interest mechanics (none by design) |
| Network-sum-zero invariant (test-proven) | Autonomous NPC trade AI |
| Dollar Vault depletion + Cascading Failure flag | Full economic balancing / tuning passes |
| Manual + scripted cross-community trades | Richer Cascading Failure simulation (food/power/water cascades) |
| In-world trade UI (panel: seller / resource / qty) | — |
| 1 promoted mechanism (Mountain wind-turbine) via gate | Full CAD library across all systems; additional mechanisms |
| One-Way Verification Gate (real promotion step) | — |
| Single-player, local | Multiplayer netcode; at-scale persistence |
| Runs in UE 5.3 | UE 5.8 migration (Lumen / Nanite / high-fidelity) |
| Readable in-world economy HUD + failure banner | — |
| Player avatar (Character Customizer, 5.3-compatible) | MetaHuman roster as named NPCs (5.3-incompatible) |
| Assemble-and-dress zones from owned assets | NPC dialog (Convai), node-based quests (Narrative Tales) |
| Ultra Dynamic Sky (per-zone mood, visual only) | Dynamic / rotating environmental hazards |
| Static vehicle & character set-dressing | Drivable-vehicle possession / physics; ambient crowds |
| Golden-scenario world `.db` (reproducible state) | — |
| — | Gameplay frameworks: Hyper Building / Farming / Choppable Trees / Skill Manager |
| — | Landscaping Mapbox → real-world Digital Twin path |
| — | Aquatic Surface water shader (Port/Island/Seafloor) |
| — | Multiplayer Dynamic Weather as a *mechanic* (MVP = visuals only) |
| — | SysML/OOSEM authoring in UModel feeding the pipeline (XMI) |

---

## Optional Accelerators (evaluate, never on the critical path)

Each may be tried only with a **hard half-day integration cutoff**. If not cleanly integrated in half a day, drop it and ship the simple version.

| Asset | Candidate use | Rule |
| --- | --- | --- |
| Hyper Vendor / Narrative Common UI | Trade-panel widget layer (Day 27) | UI layer only; SQLite ledger stays authoritative |
| Hyper Minimap OR Narrative Navigator | 5-zone navigability (Week 6) | Pick ONE; drop if not clean in half a day |
| City Sample Crowds | Ambient population (Week 8) | Perf-permitting only; do not commit |

---

## Not Used in DWM

Military / aviation / Vigilante assets (tanks, Strykers, bombers, carrier, submarine, drones, missiles; jets, airliners, transports, helicopters; Flying AI Pathfinding). Unrelated to DWM's civilian resilience theme.

---

## Performance Watch-List (do not stack)

Heavy assets that cannot all be loaded at once on UE 5.3 across five zones. One terrain treatment per zone; aggressive streaming / instancing.

- Brushify packs; MEKA 8K/Nanite landscapes; FreshCan 4x4km landscapes
- Quixel Megaplants & Nanite Plant — hero accents only; default to 60fps Trees + Farming Impostors
- Evermotion / archviz interiors; "15M-triangle + Nanite" sets
- City Sample Crowds/Vehicles (Week 8 evaluation only)

---

## Decisions Log

> Every scope change goes here, with date and what was traded.

| Date | Change | What was cut to make room | Rationale |
| --- | --- | --- | --- |
| 2026-06-30 | Initial scope frozen | — | Baseline established from MVP Delivery Plan |
| 2026-07-02 | Tracer bullet fully proven: DWMStudio → SQLite → UE round-trip confirmed in PIE, pendulum actor driven by exported data (data source: **[CONFIRM: Simscape CSV / analytic fallback]**). Root causes of the two-day build blocker, now fixed: (1) SQLiteCore/SQLiteSupport plugin not enabled in .uproject despite being declared in Build.cs — module built but wouldn't load; (2) Windows Smart App Control blocking UBT-compiled DLLs (0x800711C7) — disabled (one-way, requires Windows reinstall to re-enable); (3) WorldPackageExporter.cs existed but had no caller — nothing had ever produced a .db. Fixed via Claude Code; pendulum.db now at DWM_Dev\Content\Databases\. | — | Week 1 Friday-gate criterion (tracer round-trips) met early. |
