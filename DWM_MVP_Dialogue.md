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
> "That turbine hasn't turned since the storm. We've got the frame. What
> we don't have is the hands, the parts, or the know-how to finish the job
> — and none of it's up here. Head down to the other communities. Trade
> fair, come back with what we need, and let's get this thing spinning
> again."

**[Player prompt: "What exactly do we need?"]**

> "Timber for the frame — storm cracked our bracing. Grain, plain and
> simple, to feed the crew while they're working the mount. Hands that
> know rigging, because none of us have hung something this heavy before.
> And tools and know-how from the city — the kind of precision work no one
> up here can forge."

**[Return visit, before all trades complete — shown each time player
returns to Mountain mid-quest:]**

> "How's it going down there? Whatever you've got, it's a start."

**[Return visit, after all four trades complete:]**

> "Timber for the frame, grain for the crew, hands to do the rigging,
> tools and the brains to run it right. That's everything. Let's finish
> what the storm started to undo."

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

## Hillside — Reya Sandoval (sawmill foreman, Small Town Stores)

*(New NPC — no asset assignment made yet; use any neutral townsperson mesh
from the PropHaus Small Town Stores set, or Suburb Neighborhood pack if a
more specific character mesh is wanted. Flag for follow-up if you want a
named/specific character asset the way Hank got one.)*

**Approach:**
> "Mountain, huh? Storm hit you folks harder than us, from the sound of
> it. We lost a few beams ourselves, but the sawmill's still standing —
> and mountain work's mountain work. We all lean on each other eventually."

**[Trade panel opens — Timber for Stone:]**
> "Take what you need. Just don't let it sit — green timber's no good to
> anybody once it starts to warp."

**Farewell:**
> "Good luck up there. Come back through when it's spinning — I'd like to
> see it."

**Ambient (optional — triggered on repeat visits; references the newly-
added Hillside assets):**

> "That's the sawmill you're looking at — same one my grandfather ran.
> Still cuts straighter than half the newer rigs I've seen."

> "Furnace out back handles the metal fittings — brackets, joints,
> anything the timber needs to actually hold together instead of just
> looking like it does."

> "Got solar on the mill roof now, battery bank right beside it. Doesn't
> run the saw blade — that still wants real power — but it keeps the
> lights on through a cloudy week."

---

## Valley — Marisol Vega (grain co-op lead)

*(New NPC — no asset assignment made yet, same flag as Hillside above.)*

**Approach:**
> "Feeding a work crew on a mountain, in the cold, for however long it
> takes to hang a turbine? That's grain money, not tool money. Good thing
> we had a strong harvest this season."

**[Trade panel opens — Grain for Stone:]**
> "Here — this'll keep your crew fed a good while. Ledger says it's fair,
> and I trust the ledger."

**Farewell:**
> "Tell Hank the Valley says good luck. And tell him he still owes us from
> last winter."

*(That last line is a small worldbuilding hook — implies an existing
relationship/history between communities beyond this one quest. Cut if you
'd rather keep every NPC's dialogue self-contained with no implied
backstory.)*

**Ambient (optional — triggered on repeat visits; references the newly-
added Valley assets):**

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

## City — Itzel Reyes (factory foreman, Manufactured Tools) and Kai
Sutherland (systems engineer, Software Services)

*(Two NPCs at one location, since City covers two resources. New NPCs, no
asset assignment made yet, same flag as above. Consider whether both
should be present at the same marker or two separate markers within City —
two markers is likely cleaner for the trade-panel interaction pattern, one
trade per approach, matching every other community's single-resource
stops.)*

### Itzel Reyes — Manufactured Tools

**Approach:**
> "Precision tooling for a turbine mount — that's exactly what we do
> here. Storm damage, is it? We've fixed worse with less."

**[Trade panel opens — Manufactured Tools for Stone:]**
> "This'll hold. Good steel, properly machined — not the kind of thing
> that fails halfway up a mountain."

**Farewell:**
> "Bring it back down if it ever needs work again. We don't forget good
> customers."

**Ambient (optional — triggered on repeat visits):**

> "Furnace runs near round the clock in here — that's where your fittings
> came from, and half the factory's tooling besides. Advanced setup,
> newer than most of what we had before. Keeps up with demand."

### Kai Sutherland — Software Services

**Approach:**
> "If you want that rotor's monitoring system actually talking to the
> rest of the grid — wind speed, load, the whole picture — that's on us.
> Software's not free, but neither is guessing wrong on wind speed."

**[Trade panel opens — Software Services for Stone:]**
> "Done. It'll report in real-time once it's spinning. You'll know before
> it does if something's wrong."

**Farewell:**
> "Good luck up there. I'll be watching the feed once it's live."

**Ambient (optional — triggered on repeat visits):**

> "Control power station over there's the closest thing City's got to a
> single point of failure — everything downstream leans on it. I don't
> love that, but redesigning the whole grid isn't exactly this week's
> problem."

---

## Open items for you

1. **Named NPCs for Hillside, Valley, Suburb, and City are new
   inventions** (Reya Sandoval, Marisol Vega, DeShawn Okafor, Itzel Reyes,
   Kai Sutherland) — none of these have an asset assignment yet, unlike
   Hank's confirmed Yarrawah "Murph" Murphy character. Worth deciding
   whether each gets a similarly specific named character asset, or stays
   as a generic/unnamed mesh pulled from whichever community pack is
   already in use there (Small Town Stores, Suburb Neighborhood, etc.).
2. **The Valley farewell line** implies an existing relationship between
   Hank and Valley ("he still owes us from last winter") — a small piece
   of worldbuilding not established anywhere else. Fine to keep as a light
   touch, or cut if you'd rather every NPC's dialogue stay fully
   self-contained with no implied history.
3. **City's two-NPC structure** — confirm whether Itzel and Kai should
   share one marker (both trades happen in one stop) or sit at two
   separate markers (matches every other community's one-resource-per-stop
   pattern more cleanly, but means City takes two visits instead of one).
4. **The Suburb Dollar Vault beat is still marked optional**, same as it
   was in the storyline doc — this dialogue includes it, but cut both the
   flagged lines above if you've decided against including it.
5. **Reya's sawmill line assumes QuadArt's Survivor Base is the source**
   for Hillside's sawmill mesh — per SCOPE.md, the pack's public listing
   doesn't name a sawmill explicitly, so this still needs an in-editor
   confirmation of which mesh is actually being used before the line is
   treated as final (it may need a small wording tweak once you've
   identified the exact prop).
