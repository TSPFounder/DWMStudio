# Dream World Maker — MVP Demo Storyline
**"The Mountain's Turbine"**

## Design intent

This storyline exists to give the MVP's required demo beats a narrative
throughline, rather than have them feel like a disconnected feature checklist.
Every beat below maps directly to something already built or scheduled in the
MVP plan — no new systems, no new assets, no new scope. It reuses:

- The five confirmed communities and their assigned environment packs
- The real Stone (St) mutual-credit economy and the canonical example trade
  already in the spec (§4.1: *"Mountain buys grain from Valley"*)
- The Dollar Vault / Cascading Failure mechanic (§4.4)
- The Week 7 wind-turbine mechanism (the pendulum tracer extended into a
  community-relevant device, promoted through the DWM_Dev → gate → DWM path)

Dialogue is written as short static text (interaction-panel copy), not a
branching dialogue tree — full NPC dialogue systems (Narrative Tales) are
explicitly Post-MVP scope (Phase D3), so this stays within what the MVP's
economy-marker interaction pattern can already deliver.

**Full dialogue for Hank and each community's NPC now lives in a companion
document: `DWM_MVP_Dialogue.md`** (drafted 2026-07-14). It contains the
actual approach/trade/farewell lines for all five stops, including four
newly-named NPCs for Hillside, Valley, Suburb, and City. That document has
its own open items (NPC asset assignments, whether to keep a small Valley/
Hank backstory hint, City's one-marker-vs-two-marker structure, and the
Suburb Dollar Vault beat) — resolve those there before treating the
dialogue as final.

---

## Characters

**Player character** — an apprentice/resident of the Mountain community.
No fixed name or backstory beyond that — uses the Character Customizer asset
already adopted for the MVP, so the player's own choices define appearance.

**Hank** — leader of the Mountain community. Practical, direct, a builder
himself. Gives the player their task and stays put at Mountain's marker
throughout — he doesn't travel with the player, keeping this within the
"static set-dressing, no follower AI" constraint already in SCOPE.md.

**Asset (confirmed 2026-07-14):** Yarrawah Interactive's Survival NPC
"Hank 'Murph' Murphy" character (UE 4.22–5.4 supported, covers DWM_Dev's
5.3 target). Use the **civilian outfit preset** (no combat equipment) that
ships with the pack — DWM has no combat systems, and the default
military/survival gear (pistol, holster, ammo pouch) would misread for a
community leader. Since Hank stays stationary at his Mountain marker rather
than traveling with the player, his custom skeleton (not UE5 Manny/Quinn)
isn't a retargeting concern — he only needs idle/talk animation at one
fixed location, not locomotion compatibility with the player's Character
Customizer rig.

**Movement (decided 2026-07-14):** a light scripted loop plus a small
wander radius, not fully static and not autonomous AI — stays within
SCOPE.md's "static or simply-animated dressing" allowance, well short of
the "Autonomous NPC trade AI" line that's explicitly out of scope.

- **Core loop:** Hank periodically walks a few steps toward the turbine,
  pauses, glances up at it (ties directly into the "waiting for it to work
  again" framing from Act 1), then walks back to his marker position. Two
  hardcoded points, no pathfinding complexity — a Blueprint Timeline or a
  simple `AI Move To` between the two spots, each end capped with an idle/
  gesture animation pulled from the asset's existing 59 mocap animations
  (31 unique) — no new animation content needed.
- **Wander layer on top:** a small radius around his marker (a Behavior
  Tree + Blackboard patrol-radius volume) so he's not locked to the exact
  same two-point loop every time, preventing the repetitive tell on longer
  demo playthroughs or if a viewer lingers at Mountain.
- **Explicitly not included:** any reaction to the player's position,
  proximity-triggered behavior changes, or decision-making — he moves on
  his own fixed logic regardless of what the player does. That's the line
  that keeps this "simply-animated dressing" rather than crossing into the
  out-of-scope AI category.

---

## Premise

Mountain's community turbine — the one overlooking the settlement, silent
and unused since the land was settled — needs to spin again before winter.
Nobody left plans for it, and nobody's touched it in years. Hank can't
leave Mountain; he's needed to keep the community running. He
sends the player out to secure what's missing: labor, material, and the
tools no one on the mountain can make alone. What each community actually
gives up in trade is real Stone, moving on a real ledger, watched by whoever
is holding the demo remote.

*(Updated 2026-07-18: premise reframed from storm damage to an old,
neglected turbine — see below. Full detail in `DWM_MVP_Dialogue.md`.)*

---

## Act 1 — The Call (Mountain)

**Setting:** Mountain community, at Hank's marker, turbine visible in the
background (static, per the already-placed Wind Turbine mesh from Day 21 —
old and unused-looking, not damaged, since it's never been running rather
than recently broken).

**Hank (interaction-panel text):**

> "That turbine came with the land when we settled here — nobody's turned
> it in years, and nobody left so much as a drawing of how it goes back
> together. We need real plans before anyone touches a wrench, and hands,
> parts, and food for the crew once we do. Head down to the other
> communities. Trade fair, come back with what we need, and let's get this
> thing spinning again."

**Gameplay beat:** player opens the HUD, sees Mountain's current Stone
balance and resource list — this is the MVP's required "see balances on
screen" beat, delivered as the literal first thing the player does after
being given their task, not as a disconnected UI tutorial.

*(`DWM_MVP_Dialogue.md` has the fuller version of this exchange, including
a "what exactly do we need?" follow-up beat and separate return-visit
lines for before/after all four trades are complete — useful if the player
checks back in with Hank partway through Act 2.)*

**What's needed** (established here, paid off later):
1. **Engineering Services** (CAD drawings + Simulink model) — real plans
   for the mount and rotor, from Hillside's engineers, so nobody's
   guessing (replaces Timber as of 2026-07-18 — see SCOPE.md and
   `DWM_EngineeringServices_Task.md`)
2. **Skilled Labor** — to actually rig and mount the rotor
3. **Manufactured Tools** — precision hardware the mountain can't forge
4. **Grain** — practical, not symbolic: feeding the work crew while they're
   up there (this is the demo's required canonical trade, verbatim from the
   spec's own worked example)

---

## Act 2 — The Search (Hillside → Valley → Suburb → City)

Each stop is a full loop of the MVP's core interaction: walk to the
community's economy marker, read its flavor text, initiate a trade, watch
Stone move on both sides' balances.

### Stop 1 — Hillside (Engineering Services)

**Setting:** Hillside's TownShops dressing (PropHaus, replacing the earlier
shared-Snowy-Peaks-base plan — see SCOPE.md 2026-07-17/18 entries).

**NPCs: Reya Sandoval** (engineering firm lead), **Owen Marsh** (CAD
designer), and **Lena Ferris** (Simulink modeler) — full approach/trade/
farewell dialogue for all three in `DWM_MVP_Dialogue.md`, which also flags
the open question of whether they share one marker or are spread across
Hillside as separate points of interest.

**Trade:** Mountain pays Stone for **Engineering Services** — real CAD
drawings and a Simulink model of the turbine's expected behavior. This
replaces the earlier Timber trade; Timber itself isn't cut from the game,
just deferred to post-MVP.

### Stop 2 — Valley (Grain)

**Setting:** Valley's farmland/grain-silo dressing (Day 23).

**NPC: Marisol Vega**, Valley co-op lead (title updated 2026-07-18 — her
dialogue now reflects Valley growing more than grain, though the trade
itself stays Grain-only for now) — full approach/trade/farewell dialogue in
`DWM_MVP_Dialogue.md` (includes an optional small worldbuilding hint at
prior Hank/Valley history — see that doc's open items).

**Trade:** Mountain buys **Grain** from Valley. This is the exact trade
named in the spec's own settlement example (§4.1) — worth deliberately
using this one verbatim in the demo script later, since it's already the
reference case everyone associates with "how the ledger works."

### Stop 3 — Suburb (Skilled Labor)

**Setting:** Suburb's labor/recycling-hub dressing (Day 25).

**NPC: DeShawn Okafor**, labor hall foreman — full approach/trade/farewell
dialogue in `DWM_MVP_Dialogue.md`, including the written-out Dollar Vault
beat described below.

**Trade:** Mountain buys **Skilled Labor** from Suburb.

**Optional dramatic beat (if the demo wants to show Dollar Vault mechanics
here rather than only at the end):** Suburb's labor crew requires a permit
fee paid in Dollar Vault funds (an "outside resource," per §4.4 — not
Stone-tradeable), giving a natural, low-stakes moment to show the Dollar
Vault actually depleting on screen without needing to push any community
into full Cascading Failure. This is optional polish, not required for the
core loop — cut if time is short.

### Stop 4 — City (Manufactured Tools + Software Services)

**Setting:** City's manufacturing-hub dressing (Day 26), factory/warehouse/
crane backdrop.

**NPCs: Mike Dayton** (factory foreman, Manufactured Tools) **and Kai
Sutherland** (systems engineer, Software Services) — full approach/trade/
farewell dialogue for both in `DWM_MVP_Dialogue.md`, which also flags the
open question of whether they share one marker or sit at two separate
ones.

**Trade:** Mountain buys **Manufactured Tools** *and* **Software Services**
from City — this is the demo's second cross-community trade, and
deliberately the "expensive" one, since City is framed elsewhere in the
plan's own approved narration script as the place that "buys metals/lithium,
sells tools/tech." Two resources changing hands in one visit also shows the
economy handling more than a single line-item trade.

---

## Act 3 — The Build (Return to Mountain)

**Setting:** Mountain, turbine site, Hank's marker again.

**Hank (interaction-panel text):**

> "Real plans for the mount, grain for the crew, hands to do the rigging,
> and tools to finish it right. That's everything. Let's bring this old
> thing back to life."

**Gameplay beat — the payoff:** this is where the demo transitions from
"economy simulation" to "the DWM_Dev → verification gate → DWM promotion"
beat, without the player needing to understand any of the engineering
underneath it. In-world, this reads as simple as: the turbine, driven by
the real Simscape-derived rotor motion already promoted through the gate
(per Week 7), begins to turn.

**Hank, final line:**
> "There it goes. Every community up on that ledger had a hand in this —
> yours included."

---

## Why this structure works for the actual demo constraints

- **No new assets required.** Every location, NPC marker, and piece of
  set-dressing referenced already exists in the Week 5–6 schedule.
- **No new systems required.** Dialogue is static interaction-panel text —
  same pattern as the existing economy markers, not a new dialogue engine.
- **Covers every required MVP "done" criterion in one continuous loop:**
  walk through all five communities, see balances on screen, execute
  cross-community trades (three of them, escalating in complexity),
  optionally demonstrate Dollar Vault depletion, and finish on the promoted
  engineered mechanism actually running.
- **Reuses the spec's own canonical trade example** (Mountain buying grain
  from Valley) as an actual story beat instead of an abstract example —
  useful if you want the Week-9 demo narration to reference the exact same
  trade the written spec already uses, for consistency across every piece
  of DWM's documentation.

## Open decisions for you

1. **Hank's asset/licensing status** — confirmed as Yarrawah Interactive's
   "Hank 'Murph' Murphy" (Survival NPC line). Worth confirming whether this
   is already purchased/owned, or still needs buying before Week 5-6 asset
   assembly — if not yet owned, this belongs alongside the Fusion 360
   Startups program application on the pre-Week-5 purchase checklist.
   **Movement is resolved** (2026-07-14): scripted turbine-check loop +
   small wander radius, detailed above.
2. **Dollar Vault beat placement** — included as optional at the Suburb
   stop above; could just as easily be its own dedicated moment, or cut
   from the MVP demo loop entirely and left as a separate HUD-only beat
   (per the existing plan, showing vault depletion doesn't strictly need
   to be wrapped in narrative).
3. **Length/pacing** — this is written as a full walkthrough; if the
   Week-9 demo needs to stay under a fixed runtime (the earlier narration
   script was ~55 seconds), this storyline is the *full* experience a
   player could have, and the demo recording would likely compress it to
   2–3 of the four community stops rather than all four, to fit the time
   budget.
