# Dream World Maker — MVP Demo Dialogue
**Companion to DWM_MVP_Storyline.md**

## Format note

All dialogue below is written as **static interaction-panel text** — the
player approaches a marker, the panel displays the NPC's lines in sequence,
the player has at most one generic acknowledgment prompt to advance (not a
branching choice tree). This matches Hank's dialogue pattern from the
storyline doc and stays inside the MVP's actual scope — a full branching
dialogue system (Narrative Tales) is Post-MVP (Phase D3), not built yet.

The player character has no fixed voice or spoken lines of their own —
consistent with using the Character Customizer for player identity, there's
no canonical player VO. Where a "player line" appears below, it's shown as
on-screen text the player selects to advance the conversation, not voiced
dialogue.

Each NPC gets: an **approach** line (first contact), a **trade** line
(shown when the trade panel opens), and a **farewell** line (after the
trade completes). This three-beat structure repeats identically across all
five stops, so it reads as one consistent interaction pattern rather than
five differently-shaped conversations.

Each NPC also gets an **ambient** block (added 2026-07-15) — optional,
triggered on repeat visits after the core trade beat, referencing the
specific props now placed in that community (solar panels, furnace,
control power station, power bank, fish traps, sawmill, new housing).
These are flavor-only and don't gate anything — cut freely if a community
stop runs long, or keep all of them for a richer world if the player
lingers.

---

## Mountain — Hank "Murph" Murphy

*(Community leader. Stays at his Mountain marker throughout — see
storyline doc for his movement loop.)*

**Approach:**
> "That turbine came with the land when we settled here — nobody's turned
> it in years, and nobody left so much as a drawing of how it goes back
> together. We need real plans before anyone touches a wrench, and hands,
> parts, and food for the crew once we do. Head down to the other
> communities. Trade fair, come back with what we need, and let's get
> this thing spinning again."

**[Player prompt: "What exactly do we need?"]**

> "Start with Hillside — they've got engineers who can put together real
> CAD drawings and a simulation model, so whoever fixes this thing isn't
> guessing. After that: Grain, to feed the crew while they're working the
> mount. Hands that know rigging, because none of us have hung something
> this heavy before. And tools from the city — precision work no one up
> here can forge."

**[Return visit, before all trades complete — shown each time player
returns to Mountain mid-quest:]**

> "How's it going down there? Whatever you've got, it's a start."

**[Return visit, after all four trades complete:]**

> "Real plans for the mount, grain for the crew, hands to do the rigging,
> and tools to finish it right. That's everything. Let's bring this old
> thing back to life."

**Farewell (after the turbine spins):**
> "There it goes. Every community up on that ledger had a hand in this —
> yours included."

**Ambient (optional — triggered on repeat visits, not part of the core
quest flow; references the newly-added Mountain assets):**

> "Freshcan crew finished the new houses last week — good timing, since
> the storm put three families out of their own. Village's a little more
> crowded now, but nobody's sleeping in a barn."

> "Solar panels are holding the mountain over while the turbine's down.
> Control station keeps it steady, and the bank stores what we don't use
> by day. Won't power much more than lights and the radio, but it's
> something."

> "Fish trap's been good to us this season — one less thing to worry
> about while everyone's hands are full with the turbine. Small mercy."

---

## Hillside — Reya Sandoval (engineering firm lead), Owen Marsh (CAD
designer), and Lena Ferris (Simulink modeler) — TownShops

*(New NPCs — no asset assignments made yet; use neutral character meshes
from PropHaus TownShops, or Suburb Neighborhood pack if more specific
options are wanted. Flag for follow-up if named/specific character assets
are wanted the way Hank got one.)*

*(Updated 2026-07-18: Hillside's trade changed from Timber to
`engineering_services` — see SCOPE.md and DWM_EngineeringServices_Task.md.
Reya is reframed from "sawmill foreman" to leading the small engineering
outfit that produced the CAD drawings and Simulink model for the old
turbine. Timber/the sawmill are NOT gone from the world — deferred to
post-MVP — so Reya's location can still visually be the sawmill/workshop
building even though her trade and dialogue no longer center on lumber.)*

**Reya Sandoval — Approach:**
> "So that's the turbine that came with your land. Ambitious purchase —
> nobody's touched that thing in years. Good news is, my two here already
> worked up what you need to bring it back."

**[Player prompt: "What exactly did you put together?"]**

> "Full CAD drawings of the mount and rotor assembly, plus a Simulink
> model of how it should actually behave once it's running. Owen did the
> drawings, Lena built the model. Between the two, whoever's doing the
> repair up there won't be guessing."

**[Trade panel opens — Engineering Services for Stone:]**
> "Here's everything — drawings and model both. Take care of it; that's
> real engineering hours in your hands, not just a sketch on a napkin."

**Farewell:**
> "Good luck up there. Come back through when it's spinning — I'd like to
> see it."

**Reya — Ambient (optional, triggered on repeat visits):**

> "Old sawmill building still stands out back — not running these days,
> but I like the space. Good light for drafting."

> "Got solar on the roof now, battery bank right beside it. Doesn't run
> much, but it keeps the lights on through a cloudy week while these two
> are hunched over a screen."

---

### Owen Marsh — CAD Designer

**Approach:**
> "I pulled the mount and rotor assembly apart in CAD, piece by piece.
> Whoever repairs that thing won't have to reverse-engineer it in the
> field — every bracket, every bolt pattern, it's all there."

**Ambient (optional):**
> "Hardest part wasn't the rotor — it was the mount. Whoever built that
> turbine originally didn't leave much documentation. Had to measure half
> of it by hand from photos."

> "If the repair crew finds something in the field that doesn't match my
> drawings, tell them to trust what they're looking at, not the paper.
> Old hardware doesn't always match what was on file."

---

### Lena Ferris — Simulink Modeler

**Approach:**
> "Owen's drawings tell you what the turbine looks like. My model tells
> you how it's supposed to behave once it's spinning again — load,
> response, where it'll struggle. Saves you finding out the hard way."

**Ambient (optional):**
> "Model's only as good as what we know about that specific turbine.
> I built it off Owen's CAD data and some reasonable assumptions — real
> sensor data once it's running would tighten it up, but it'll get you
> started."

> "Half my job is knowing when the simple model is good enough and when
> it isn't. For a first repair pass, simple's fine."

---

## Valley — Marisol Vega (co-op lead)

*(New NPC — no asset assignment made yet, same flag as Hillside above.
Updated 2026-07-18: dialogue broadened to reflect Valley producing more
than just grain — vegetables, meat, honey, and non-orchard fruits
(berries, melons, and the like — distinct from Hillside's existing
Orchard Fruit resource, so the two don't overlap). This is FLAVOR ONLY for
now — the actual trade stays Grain-for-Stone, no schema change. A real
resource change is planned for later; when that happens, this dialogue
will need a matching pass, same as Reya's did for engineering_services.)*

**Approach:**
> "Feeding a work crew on a mountain, in the cold, for however long it
> takes to hang a turbine? That's food money, not tool money. Good thing
> we had a strong season — grain's the bulk of it, but there's plenty
> else coming off this land too."

**[Trade panel opens — Grain for Stone:]**
> "Here — grain'll keep your crew fed a good while on its own. Ledger
> says it's fair, and I trust the ledger."

**Farewell:**
> "Tell Hank the Valley says good luck. And tell him he still owes us from
> last winter."

*(That last line is a small worldbuilding hook — implies an existing
relationship/history between communities beyond this one quest. Cut if you
'd rather keep every NPC's dialogue self-contained with no implied
backstory.)*

**Ambient (optional — triggered on repeat visits; references the newly-
added Valley assets):**

> "Grain's what moves on the ledger, but it's not all we grow. Vegetables,
> some meat, honey from the hives out past the east field — none of it's
> tracked the way the grain is, but nobody up here goes hungry either."

> "Silo's got solar on it now — panel, battery, the little control box
> that keeps it all from frying itself. Doesn't run the auger, but it
> keeps the moisture sensors alive through harvest, and that's the part
> that actually matters."

> "Power bank's mostly for the slow season — store up what the panels
> catch in the long days, spend it down when the sun's not cooperating.
> Same idea as the grain, really. Save when you can, draw when you need
> to."

---

## Suburb — DeShawn Okafor (labor hall foreman)

*(New NPC — no asset assignment made yet, same flag as above.)*

**Approach:**
> "Rigging work on a mountain turbine? That's a real job, not a favor.
> We've got hands that know heavy lifting and know how to stay safe doing
> it — but good labor isn't free, and neither's the paperwork to send them
> up there."

**[Trade panel opens — Skilled Labor for Stone:]**
> "Stone covers the crew's time. Straightforward."

**[OPTIONAL — Dollar Vault beat, per storyline doc's "optional dramatic
beat" note. Only include if you want to demonstrate Dollar Vault depletion
here rather than cut it or place it elsewhere:]**

> "One more thing — insurance and permits for a job this size, that's not
> Stone. That comes out of the community vault, and it's not cheap. You
> sure Mountain can cover it?"

**[Dollar Vault payment confirmation — this is the moment the vault
balance visibly drops on screen:]**

> "Paperwork's clear. Crew's yours."

**Farewell:**
> "Tell your people to keep the ropes tied right. We'll see them when the
> job's done."

**Ambient (optional — triggered on repeat visits; references the newly-
added Suburb assets):**

> "Control power station runs the whole block now — panels on half these
> roofs, power bank in the old garage soaking it up. Wasn't cheap to set
> up, but it beats waiting on somebody else's grid."

> "Funny thing about recycled parts — half that control station's
> housing used to be something else entirely. We don't waste much out
> here. Can't afford to."

---

## City — Mike Dayton (factory foreman, Manufactured Tools) and Kai
Sutherland (systems engineer, Software Services)

*(Two NPCs at one location, since City covers two resources. New NPCs, no
asset assignment made yet, same flag as above. Consider whether both
should be present at the same marker or two separate markers within City —
two markers is likely cleaner for the trade-panel interaction pattern, one
trade per approach, matching every other community's single-resource
stops.)*

### Mike Dayton — Manufactured Tools

**Approach:**
> "Got Owen's CAD drawings from Hillside a few days back — went through
> them, worked up what it'll actually take to machine replacement parts
> for that mount. Good drawings, too. Made the estimate a lot less of a
> guess."

**[Trade panel opens — Manufactured Tools for Stone:]**
> "This'll hold. Good steel, machined straight off Owen's drawings — not
> the kind of thing that fails halfway up a mountain."

**Farewell:**
> "Bring it back down if it ever needs work again. We don't forget good
> customers."

**Ambient (optional — triggered on repeat visits):**

> "Furnace runs near round the clock in here — that's where your fittings
> came from, and half the factory's tooling besides. Advanced setup,
> newer than most of what we had before. Keeps up with demand."

> "Wouldn't have quoted this job as tight without Owen's numbers. Half
> our estimating headaches come from guessing at parts nobody's measured
> properly."

### Kai Sutherland — Software Services

**Approach:**
> "Took Lena's Simulink model and turned it into real controller code —
> C, running on the hardware, not just a diagram anymore. And since it
> needed a home, I laid out the controller enclosure myself in Fusion.
> Wind speed, load, the whole picture — that's on us. Software's not
> free, but neither is guessing wrong on wind speed."

**[Trade panel opens — Software Services for Stone:]**
> "Done. Controller's built, code's flashed, it'll report in real-time
> once it's spinning. You'll know before
> it does if something's wrong."

**Farewell:**
> "Good luck up there. I'll be watching the feed once it's live."

**Ambient (optional — triggered on repeat visits):**

> "Control power station over there's the closest thing City's got to a
> single point of failure — everything downstream leans on it. I don't
> love that, but redesigning the whole grid isn't exactly this week's
> problem."

> "Enclosure's nothing fancy — just needed to keep the electronics dry
> and let heat out without letting weather in. Modeled it in Fusion in an
> afternoon. The code took a lot longer than the box did."

---

## Open items for you

1. **Named NPCs for Hillside, Valley, Suburb, and City are new
   inventions** (Reya Sandoval, Owen Marsh, Lena Ferris, Marisol Vega,
   DeShawn Okafor, Mike Dayton, Kai Sutherland) — none of these have an
   asset assignment yet, unlike Hank's confirmed Yarrawah "Murph" Murphy
   character. Worth deciding whether each gets a similarly specific named
   character asset, or stays as a generic/unnamed mesh pulled from
   whichever community pack is already in use there (TownShops, Suburb
   Neighborhood, etc.).
2. **The Valley farewell line** implies an existing relationship between
   Hank and Valley ("he still owes us from last winter") — a small piece
   of worldbuilding not established anywhere else. Fine to keep as a light
   touch, or cut if you'd rather every NPC's dialogue stay fully
   self-contained with no implied history.
3. **City's two-NPC structure** — confirm whether Mike and Kai should
   share one marker (both trades happen in one stop) or sit at two
   separate markers (matches every other community's one-resource-per-stop
   pattern more cleanly, but means City takes two visits instead of one).
4. **The Suburb Dollar Vault beat is still marked optional**, same as it
   was in the storyline doc — this dialogue includes it, but cut both the
   flagged lines above if you've decided against including it.
5. **Hillside now has THREE NPCs at one stop** (Reya, Owen, Lena) — same
   one-marker-vs-multiple-marker question as City above applies here too;
   worth deciding whether all three appear together at a single Hillside
   marker (matches the "one trade, one stop" pattern elsewhere since it's
   still a single `engineering_services` trade) or are spread across the
   Hillside level as separate points of interest to walk between.
6. **Reya's old sawmill-line assumption is now removed** — since her
   dialogue no longer centers on lumber, the earlier open item about
   confirming QuadArt Survivor Base's sawmill mesh naming is no longer
   tied to her dialogue specifically. That mesh-naming question still
   matters for the timber trade's eventual post-MVP return, just not for
   this MVP dialogue pass.
7. **Mike and Kai's dialogue now references a connected digital-thread
   chain** (Owen's CAD → Mike's manufacturing estimate; Lena's Simulink
   model → Kai's generated C controller code → Kai's Fusion 360 enclosure
   design) — added 2026-07-18. Currently DIALOGUE-ONLY: nothing new is
   actually being built or modeled for the MVP itself. If you want a real
   Fusion 360 controller-enclosure model to exist (the same way the
   Hillside-sawmill capability-demo idea was captured), that's a separate
   post-MVP task, not automatically implied by this dialogue update —
   confirm if you want that added to Todoist the way the sawmill idea was.
8. **Valley's food variety (vegetables, meat, honey, non-orchard fruits)
   is FLAVOR-ONLY as of 2026-07-18** — Marisol's dialogue now mentions
   these, but the actual trade stays Grain-for-Stone; no schema change.
   Confirmed the future fruit resource will be specifically NON-orchard
   fruit (berries, melons, etc.) to avoid overlapping with Hillside's
   existing Orchard Fruit resource. A real mechanical expansion is planned
   for later — when that happens, this needs the same treatment as the
   engineering_services change (new resource(s), guardrail files, its own
   SCOPE.md entry, and another dialogue pass here).
