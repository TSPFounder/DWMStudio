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
5. Observe one engineered system promoted through DWM_Dev → verification gate → DWM (the Mountain wind-turbine driven by real Simscape physics, OR the approved fallback data source below if Simscape licensing isn't resolved in time).

---

## CONTINGENCY: Turbine Data Source Is Decoupled From the MVP Launch Date

**Decision (2026-07-02): the Sept 1 launch does NOT wait on MathWorks/Simscape licensing being
resolved.** MATLAB/Simscape acquisition is tracked as its own item (see Decisions Log) with its
own timeline; it is explicitly NOT allowed to become a blocker on the MVP ship date.

**Why this works with zero architecture change:** `WorldPackageExporter.cs` already has exactly
the mechanism needed — `WritePendulum`'s `simResultsCsv` parameter falls back to
`GenerateSmallAngleFallback()` (an analytic motion curve) when no real Simscape-exported CSV
exists. The UE-side reader (`DwmGameInstance`/`ADwmPendulumActor`) reads `SimSamples` from the
package and has no idea, and no need to know, whether the data came from Simscape or an
analytic/hand-tuned curve. **Same pattern applies to the turbine mechanism.**

**The plan:**
- **If Simscape Multibody is licensed and the real CAD-linked turbine model is verified through
  the gate before Week 7 — ship with real Simscape physics data**, exactly as originally planned.
- **If not — ship Week 7 with the turbine driven by a hand-tuned or analytic motion curve**
  (same fallback mechanism, extended from the pendulum to the turbine's actual motion profile).
  The Verification Gate ceremony and ledger/economy behavior are IDENTICAL either way; only the
  data source behind `SimSamples` differs. Definition-of-Done item 5 above is satisfied by either.
- **Swapping in real Simscape data later is a fast-follow, not a re-launch.** Once Simscape is
  licensed, re-run the real pipeline, export the real CSV, and replace the fallback data in a
  routine content update — no UE code changes, no re-architecture, no schedule risk to anything
  already shipped.
- **Be honest in the campaign/demo materials about which data source is live** at time of
  recording — don't imply real CAD-verified physics if shipping on the fallback curve. The
  engineering-rigor thesis is still demonstrated by the PIPELINE and the GATE (the promotion
  ceremony, the verification discipline) even when the underlying motion data is provisional.

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
| 2026-07-02 | Economy ledger schema implemented and seeded: Communities/Resources/CommunityResources/StoneLedger/DollarVault live in DWM.Shared (EconomySeeder.cs), producing economy.db (5 communities, 10 resources, 24 CommunityResources rows — every resource has both a producer and consumer). Design: single-row transfer model for StoneLedger (network-sum-zero by construction); economy.db kept separate from pendulum.db (different consumers, different change rates). New DWMStudio.Tests project asserts the network-sum-zero invariant (2/2 passing, including after synthetic trades). | — | Appendix B.4 milestone met a day early. Resource cross-links include the canonical Primer interdependency (City Software Services → Mountain). |
| 2026-07-02 | Randomized-trade invariant testing (Day 4 task, done same day): 100 randomized trades (fixed seed, mixed magnitudes) plus rollback and constraint-enforcement tests, all passing (5/5 total, re-run 4× for determinism). No invariant breaks found — the ledger design holds under stress, not just the two hand-picked cases. SQLite journal-mode decision also finalized in ARCH.md: default rollback-journal (NOT WAL), since the DWMStudio↔UE access pattern is sequential handoff, not concurrent access; enforced by process discipline (RUNBOOK + preflight.ps1), not SQLite's concurrency features. | — | Day 4's "C" task met a day early, alongside Day 3. Three technical milestones landed 7/2: tracer bullet, economy schema, randomized ledger testing. |
| 2026-07-02 | No MVP-scope NPC voice or dialogue system. Any NPC/marker interaction in the MVP uses text (UMG "balloons") only — no audio voice pipeline, no lip-sync, no TTS in the shipped build. Real NPC conversation (voiced, via Convai) was already correctly scheduled in Post-MVP Phase D and stays there — not pulled forward. Separately, one-time campaign-video narration (recorded or TTS, laid over the Week-9 demo video in editing) is a distinct, optional, much cheaper task if desired — not an in-engine system and not evaluated here. Also confirmed: no in-game intro cinematic for MVP — the Week-9 campaign video already carries the "vision" beat to the audience that matters for September; a second in-engine cinematic doing the same job would be redundant. If any in-game opening is added later, cap it at the existing half-day accelerator cutoff and always include a skip button. | — | Splash screen (Project Settings) and a basic UMG title screen remain simple, low-cost MVP items — distinct from a cinematic intro and unaffected by this decision. |
| 2026-07-02 | Sept 1 MVP launch is DECOUPLED from MATLAB/Simscape licensing timing. The Week-7 turbine gate is satisfied by EITHER real Simscape-verified physics OR the existing analytic/hand-tuned fallback data path (WorldPackageExporter.WritePendulum's GenerateSmallAngleFallback pattern, extended to the turbine) — no UE-side code change either way, since the reader consumes SimSamples regardless of source. Real Simscape data becomes a fast-follow content update once licensing resolves, not a pre-launch blocker. Campaign/demo materials must be honest about which data source is live at time of recording. | — | Protects the ship date from an external vendor-licensing dependency entirely outside the team's control. |
| 2026-07-02 | REVERSED Phase D decision: Convai (AI-driven NPC dialogue) dropped in favor of Narrative Tales (already-owned node-based, hand-authored dialogue/quest system) for Post-MVP Phase D3. Reason: Convai bills per player-NPC interaction at RUNTIME (confirmed from its own FAQ/pricing docs) — an ongoing, subscriber-count-scaling operational cost for the life of the product, not a one-time dev fee, similar in kind to hosting costs. Narrative Tales is purchased once; authored dialogue then runs locally at runtime with zero ongoing per-interaction cost. Secondary reason: hand-authored dialogue keeps every NPC strictly on-lore with the Primer's named roster and drama axes, which open-ended AI-generated conversation cannot guarantee — likely the better creative fit for DWM's authored-narrative structure, not just the cheaper one. | — | Removes a future usage-scaling cost line and a commercial-license verification step from the Phase D financial/legal to-do list entirely. |
| 2026-07-02 | MATLAB toolbox inventory clarified: existing FULL license is MATLAB/Simulink 2011a + Control System, Optimization, Aerospace, Symbolic Math, PDE, and Simulink V&V toolboxes. CONFIRMED: no Simscape or Simscape Multibody license at any version. DECISION: not using 2011a for DWM development \u2014 acquire a modern release with Simscape Multibody directly rather than splitting effort across two versions. CORRECTED REQUIREMENT: the actual Week-7 turbine-gate need is SIMSCAPE MULTIBODY (any reasonably modern release, stable since R2012a), NOT specifically R2025b as earlier logged \u2014 that R2025b figure conflated the mechanism-authoring requirement with the separate MATLAB\u2194UE5.3 LIVE CO-SIMULATION plugin, which the actual proven pipeline (WorldPackageExporter reads a CSV MATLAB already wrote, offline/asynchronous \u2014 same pattern as the working pendulum tracer) does not use at all. R2025b only matters if live co-simulation is ever wanted as a future capability \u2014 not a Week-7 requirement. | — | Simplifies the acquisition decision: ask MathWorks for the cheapest path to Simscape Multibody specifically, not necessarily the latest release. Still confirm: (1) commercial-use terms of any new/upgraded license, (2) whether the existing full-license contract can be upgraded rather than purchased fresh. |
| 2026-07-06 | RESOLVED: DWM_Dev had never successfully pushed to GitHub (multiple prior attempts, all failed) — root cause was a marketplace terrain pack (PhotoR_Landscape_4: several .umap files 172–594MB, textures 24–72MB) baked into git history early on, before .gitignore was solid. GitHub hard-rejects any single file over 100MB, so every push failed regardless of current .gitignore content (history retains old blobs regardless of later ignore rules). DECIDED: not worth Git LFS (10GB free storage+bandwidth/month, but bandwidth is consumed by every fresh clone — in direct tension with the established WEEK1_FRESH_CLONE_GATE.md habit of periodically re-cloning to verify repo completeness; LFS also risked silent pointer-only clones once quota is hit). FIX: moved the asset folder out of the repo temporarily → git filter-repo --path Content/PhotoR_Landscape_4 --invert-paths --force (strips it from ALL history, not just HEAD) → re-added origin remote (filter-repo strips it as a safety measure) → added Content/PhotoR_Landscape_4/ to .gitignore → moved the folder back into Content/ (now ignored, not tracked — fully usable locally by UE, invisible to git) → force-pushed (819MB), succeeded. Same treatment applied to Backup/ (a full local solution backup — must never be committed) and Content/Databases/ (regenerated exporter output, not source data) via .gitignore. | — | GitHub flagged Content/StarterContent/HDRI_Epic_Courtyard_Daylight.uasset (69MB) as being over its 50MB *recommended* soft guideline (not the 100MB hard limit — push succeeded). Not urgent, but the same pattern (large non-authored content bloating the repo) will recur as more terrain/starter content accumulates — worth proactively .gitignore-ing bulky template/marketplace content going forward rather than waiting for it to become a problem again. |
