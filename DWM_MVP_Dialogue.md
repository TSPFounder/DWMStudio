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

**Cross-community references (added 2026-08-01).** The NPC reference chain
used to run one way — Owen's drawings reach Mike, Nathan's model reaches
Kai — and then stop. It is now a CLOSED LOOP: Sophia credits Suburb scrap,
Mike credits both Suburb feedstock and Owen's drawings, DeShawn credits
City castings and Valley grain, Maria credits Hillside/City/Suburb for
one small control box, and Hank credits Sophia and Mike for the verdict.
Every community now appears in another community's mouth, so the theme —
the communities are strongest united — is carried structurally rather than
announced. See the Theme section of DWM_MVP_Storyline.md. **None of these
lines say "we are stronger together," and none of them should.**

**Recycling beats (added 2026-08-01).** Recycling is a UNIVERSAL capability,
not one community's specialisation — sustainment is the point. Suburb is the
major recycler (already established), City is the other major node (scrap
feedstock for the foundry, plus electronics recovery in Kai's block), and
every community has at least a small method: Mountain keeps a scrap bin,
Valley composts and runs a digester. Valley's line also resolves the
storyline's open question about Valley's role in the manufacturing arc.
NARRATIVE ONLY for the MVP — no schema change. A tradeable scrap resource
is post-MVP; see SCOPE.md 2026-08-01.

**Navigation hand-offs (added 2026-08-01).** Each stop now ends by telling
the player where to physically go next, in-character, so the demo route is
walkable without a quest marker or minimap:

| From | Direction given | To |
| --- | --- | --- |
| Hank (Mountain) | Out through the gate, follow the track down | Sophia, room above the Hillside realty office |
| Sophia (Hillside) | Down the stairs, right out of the office, straight down the market street, road climbs out the far end | Maria, on her porch in the Valley |
| Maria (Valley) | Left out of the house, down the dirt road | DeShawn, the realty office in the Suburb |
| DeShawn (Suburb) | Out to the stop on the corner, take the bus loop | Mike, on the City machine-shop floor |
| Mike (City) | Upstairs, same building | Kai, in the office above the shop floor |
| Kai (City) | Bus loop back round to Mountain | Hank, for Act 3 |

**TWO CONSEQUENCES, both worth knowing before these lines are treated as
final:**

1. **The dialogue is now coupled to level geometry.** It was previously
   level-agnostic. These lines name a gate, a realty office, a market
   street, a porch and a dirt road — if a level is rearranged, the dialogue
   becomes wrong, and wrong directions are worse than none. Any level edit
   to Mountain, Hillside or Valley now needs a check against this table.
2. **They imply an ORDER: Mountain → Hillside → Valley → onward.** The rest
   of the design is deliberately order-agnostic (SCOPE.md 2026-08-01 notes
   every Act 2 purchase stays justified in any visit order, and the
   return-visit states handle partial progress). Directions do not BREAK
   that — they are a character's suggestion, not a gate — but a player who
   wanders off-route will be holding stale instructions. Acceptable for a
   guided demo; worth knowing it is a deliberate trade.

**COMPLETE as of 2026-08-02.** The three links that were open on 2026-08-01
are all closed, and the shared bus fleet (see below) closed all three rather
than one:

1. **Suburb after DeShawn** — he sends the player to the stop and onto the
   bus loop.
2. **Reaching Mike and Kai** — the bus loop runs to City; the player
   arrives at the City stop and walks to the machine shop.
3. **Back to Mountain for Act 3** — the bus loop again, landing at
   Mountain's existing return spawn point.

**This also settles the one-marker-vs-two question for City.** Mike is on
the shop floor, Kai is in the office above it — two markers, one building,
matching every other community's one-trade-per-approach pattern. The
hand-off between them is "upstairs, same building," which needs no map
directions at all, so the second marker costs nothing in navigation
complexity. The parenthetical under the City heading records this as
resolved.

**TRANSITION RULE (decided 2026-08-02) — ALL transitions are transition
volumes. Every one of them, walked or ridden, without exception.** The
player crosses a volume and arrives at the destination level's spawn point.
There is one mechanism in the game for changing communities and this is it.

What differs between links is only what the volume is SITTING ON and what
the fiction says is happening:

| Link | Volume placed | Reads as |
| --- | --- | --- |
| Mountain → Hillside | on the track past the gate | walking down |
| Hillside → Valley | where the market street road climbs out | walking on |
| Valley → Suburb | on the dirt road out of the Valley | walking down |
| Suburb → City | at the Suburb bus stop | boarding the loop |
| City → Mountain | at the City bus stop | boarding the loop |

Three things follow, and they are the reason this rule is worth stating
rather than leaving as an implementation detail:

1. **NOT the E key.** `Interact()` already has a three-case ordering (open
   dialogue → trade terminal → NPC) that is deliberately fragile and has
   been verified against regression. Adding transitions as a fourth case
   means touching that function again. An overlap volume is a completely
   separate code path and cannot regress it. This is the single most
   important consequence of the rule.
2. **One thing to build, one thing to debug.** Walking and riding are the
   same Blueprint with a different placement and a different destination.
   The bus loop is a SKIN on the transition system, not a second system —
   Codex's Track B task 1 is the Mountain ↔ Hillside transition, so the
   mechanism the whole game needs is already the one being built.
3. **Placement is the whole design.** Since every volume behaves
   identically, the only thing that makes a link read as a walk rather than
   a ride is WHERE the volume sits and what is standing next to it. Put a
   walking volume somewhere with no road and the player will not understand
   what happened to them.

**Place volumes so that crossing is deliberate.** On a road, set it far
enough along that a player idling near the edge of the level does not
trip it. At a bus stop, put it at the door or on the pavement immediately
beside the bus, not spanning the street — walking past should do nothing.

**The realty offices are deliberate, not a collision (recorded 2026-08-02).**
An earlier draft of this section flagged the "realty office" appearing in
both Hillside and the Suburb as a naming hazard. That was wrong, and the
correction matters enough to write down so nobody "fixes" it later.

The realty company is ESTABLISHED SHARED INFRASTRUCTURE — a cross-community
organisation that places community members in housing and runs its offices
as community support sites. The offices are separate places in separate
buildings; they already carry the SAME SIGN out front in the current UE
levels. So the repetition is a system the player learns, not an ambiguity
they trip over: "go to the realty office" parses in any community, exactly
like "go to the bus stop." Keep the shared sign and keep the buildings
distinct — that combination is what makes it read as one institution with
five branches rather than five coincidences.

Two things follow from this:

1. **It carries the theme without a speech.** A realty company that
   ALLOCATES housing rather than sells it puts a familiar name on a changed
   function. The player arrives with assumptions about what happens inside a
   realty office and learns the economy by having them corrected — which is
   worth more than any line of dialogue explaining mutual credit.
2. **It is the same pattern as the shared bus fleet** (below): two
   cross-community institutions, both concerned with moving people between
   communities, and neither one workable by a single community alone. The
   fleet is therefore the SECOND instance of a pattern the player has
   already met, not a cold introduction of the unity theme.

Sophia and DeShawn both stay where they are. A support office is exactly
where a stranger would be sent, so both placements are already right. The
only outstanding nicety is that neither location currently shows what its
person TRADES: a drafting table with drawings pinned up in Sophia's room
reads "engineer" instantly, and DeShawn's office looking out onto the
sorting yard reads "recycler." Props, not relocations.

**The shared bus fleet (added 2026-08-02).** The five communities jointly
run a small fleet of school buses between them — the service is called
**"the loop,"** which is literally true: Mountain → Hillside → Valley →
Suburb → City → Mountain. Characters saying "catch the loop" teaches the
shape of the map for free.

**NAMING CONVENTION (2026-08-02), because these documents were already using
"loop" for three other things** — the core interaction cycle, Hank's
two-point movement loop, and the closed cross-community reference loop.
In PROSE, the service is always **"the bus loop."** In SPOKEN LINES,
characters say **"the loop"** and always will: in-world there is nothing
else it could mean, and "catch the bus loop" is not how anyone talks. So
the in-world name never changed — only the way the docs refer to it, which
is a reading convenience for whoever picks these up next, not a design
decision.

Design rules, so this stays MVP-sized:

- **One bus parked at every community, always.** This is the whole reason a
  fleet beats a single bus: a player who reaches a stop and finds it empty
  would need a timetable to explain itself. A fleet never has that problem,
  and never needs a schedule.
- **Walk the short links, ride the long ones.** Mountain → Hillside →
  Valley → Suburb stay on foot. The bus loop covers only Suburb → City and
  City → Mountain. If every leg is a bus ride the level design stops paying
  off.

  This is a distinction in FICTION AND LEVEL LAYOUT ONLY — the mechanism is
  identical either way. See the transition rule below.
- **The bus goes all the way to Mountain (corrected 2026-08-02).** Boarding
  in the City lands the player at Mountain's existing return spawn point.
  There is no walk up from a stop at the bottom of the hill; an earlier
  draft had one and it is withdrawn — see the note under Act 3 in the
  storyline doc for what was removed and why nothing load-bearing went with
  it.

  **MOUNTAIN'S ARRIVAL — recorded 2026-08-02.** Mountain is a very large
  terrain and its only road runs about a MILE from the village. Three
  things follow, in order of how much they matter:

  1. **SPAWN ROTATION IS THE ONLY REQUIREMENT, and it is free.** If the
     return spawn faces the village with the road behind the player, none
     of the rest of this is ever seen. Face them out toward the road. One
     rotation field, and it decides whether any of the below pays off.
  2. **THE ROAD READS; A BUS DOES NOT.** A road is a long linear feature
     that stays legible at distance. A bus is a point object, and at a mile
     — about 160,000 UE units — an 1,100-unit vehicle subtends roughly
     0.39°, which is about 8 × 2 PIXELS at 1080p on a default 90° FOV.
     Nobody will read that as a bus; they will read it as a yellow speck if
     they notice it at all. Two defaults will erase even that: CULL
     DISTANCE (set Desired Max Draw Distance to 0 or it vanishes long
     before a mile) and EXPONENTIAL HEIGHT FOG. Park a bus there if it
     pleases you, as a grace note for players who look — but do not spend
     effort making it legible, and do not rely on it to close the ride.
  3. **THE RIDE ALREADY CLOSES AT THE BOARDING END.** The player is told to
     take the loop, walks into a bus in the City, and the screen fades.
     That is the readable act. Arriving without a bus in view is how riding
     anything works when you are not looking at it.

  **THE MILE IS WORTH MORE THAN THE BUS, and it recovers something the
  withdrawn walk cost.** A village a mile from the only road is a stronger
  logistics argument than the grade ever was: everything reaching Mountain
  crosses a mile of open ground. That is why repairing the large turbine
  costs what it does — and the player does not have to WALK it to feel it,
  only to SEE it once, from the village, facing the right way. Act 3's cost
  argument gets visual support at zero traversal cost, which is strictly
  better than what the walk was going to buy.
- **Co-locate stop, support office and trade terminal.** Same three things
  in the same relationship in every community. This is the strongest form of
  "repetition teaches a rule," and it gives the Suburb and City trade
  terminals a natural home instead of an arbitrary placement.

**ASSET ASSIGNED (2026-08-02): HQ Retro School Bus by NotLonely**, already
owned. This is a rare case of an asset choice that improves the fiction
rather than merely satisfying it — a RETRO bus reads as a vehicle kept
running long past the era that built it, which is what a salvage-and-repair
economy would actually be riding. Nobody has to say that; the mesh says it.

The fleet is intentionally UNIFORM — same bus at every stop, no per-community
paint. Fleet numbers on the side are the right way to tell them apart if you
want that, because a number says "one fleet, five branches," where community
colours would say "five towns' buses" and quietly contradict the shared
ownership the whole thing is for.

TWO THINGS TO CHECK ON THE ASSET BEFORE COMMITTING TO A BOARDING
INTERACTION, neither of which is a criticism of the pack — both are near-
universal in marketplace vehicles:

1. **Does it have a modelled interior?** Many vehicle assets are exterior
   shells. If this one is, boarding is off the table and the MVP default
   below is the only option — which is fine, and was the recommendation
   anyway.
2. **What is its collision?** A single convex hull or box collision makes
   the interior solid and un-enterable. This is the SAME failure mode as the
   Village Log House staircase, with the same fix (Collision Complexity →
   "Use Complex Collision As Simple"). Also sanity-check scale against the
   character capsule: a real school bus is roughly 1100 × 250 × 300 units.

**MVP DEFAULT — the stop is the trigger, the bus is the landmark.** Put the
trigger volume on the pavement at the stop rather than inside the vehicle.
Zero collision work, no dependency on an interior existing, and it still
reads correctly, because standing at a stop and then being somewhere else is
exactly how riding a bus feels. Upgrade to actual boarding later if the
asset turns out to support it.

**Biofuel conversion — planted now, paid off post-MVP.** The fleet currently
burns purchased fuel, which is a DOLLAR cost draining the Vault rather than
a Stone trade. The stated future plan is to convert the buses to run on
biofuels the VALLEY can help supply — feedstock from what Valley already
produces — with the conversion work itself done in the CITY or the SUBURB,
whichever suits. That routes the whole project through three communities and
nets the running cost into the Stone ledger instead of the Vault.

NARRATIVE ONLY for the MVP — no schema change, no `transport_services`
resource, no fuel mechanic. It costs one line each from DeShawn and Maria to
set up, and the payoff is available whenever it is wanted.

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

**[Directions — hand-off to Hillside. See the navigation note in the format
section: these lines describe REAL level geometry and break if the level
changes.]**

> "Out through the gate and follow the track down to Hillside. Ask for
> Sophia Sandoval — she's got the room above the realty office. She'll be
> expecting you."

**[Return visit, before all trades complete — shown each time player
returns to Mountain mid-quest:]**

> "How's it going down there? Whatever you've got, it's a start."

**[Return visit, after all four trades complete:]**

> "Real plans for the mount, grain for the crew, hands to do the rigging,
> and tools to finish it right. That's everything. Let's bring this old
> thing back to life."

**[The verdict — Hank synthesises Hillside's and City's numbers. Revised
2026-08-01. Neither community can reach this conclusion alone, which is the
point; see the Theme section of DWM_MVP_Storyline.md. Maps to the
ReturnAllTradesComplete dialogue state, which already gates on trades with
every required community.]**

> "Something you should hear before you get comfortable. Sophia priced the
> engineering. Mike priced the parts. Put those two together and this old
> machine costs more to keep alive than it gives back — not this winter,
> but soon enough."

**[Player prompt: "So the repair was a waste?"]**

> "Opposite. It buys us the winter, and it bought us a straight answer,
> which we didn't have before. Nobody up here could have worked that out
> alone — took their drawings and their shop floor both. What we do with it
> is build our own, smaller, and build the tools to make them."

**Farewell (after the turbine spins):**
> "There it goes. Every community up on that ledger had a hand in this —
> yours included. And now we've all got the same drawings. Next one, we
> build ourselves."

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

> "We keep a scrap bin by the workshop — bolts, offcuts, anything with
> metal left in it. Not much on its own. But it goes down to the Suburb
> with the empty carts, and something useful comes back up. Everybody
> keeps a bin now."

---

## Hillside — Sophia Sandoval (engineering firm lead), Owen Marsh (CAD
designer), and Nathan Ferris (Simulink modeler) — TownShops

*(New NPCs — no asset assignments made yet; use neutral character meshes
from PropHaus TownShops, or Suburb Neighborhood pack if more specific
options are wanted. Flag for follow-up if named/specific character assets
are wanted the way Hank got one.)*

*(Updated 2026-07-18: Hillside's trade changed from Timber to
`engineering_services` — see SCOPE.md and DWM_EngineeringServices_Task.md.
Sophia is reframed from "sawmill foreman" to leading the small engineering
outfit that produced the CAD drawings and Simulink model for the old
turbine. Timber/the sawmill are NOT gone from the world — deferred to
post-MVP — so Sophia's location can still visually be the sawmill/workshop
building even though her trade and dialogue no longer center on lumber.)*

**NPC PLACEMENT — RESOLVED 2026-08-02. All three Hillside NPCs are in the
SECOND-FLOOR ROOM ABOVE THE REALTY OFFICE.** Sophia, Owen and Nathan share
one room; none of them is anywhere else in Hillside. This closes the
long-open "one marker or spread across Hillside" question, and it closes it
differently from City, for a reason worth stating.

**City needed two markers because City has TWO TRADES.** Hillside has ONE.
Sophia sells `engineering_services`; Owen and Nathan do not sell anything —
they explain what went into her package. So the room holds:

| NPC | Marker | Opens a trade panel? |
| --- | --- | --- |
| Sophia Sandoval | the trade marker | YES — `engineering_services` |
| Owen Marsh | optional conversation | no |
| Nathan Ferris | optional conversation | no |

That still satisfies the one-trade-per-approach pattern every community
uses: exactly one of the three approaches opens a panel. Owen and Nathan are
pure flavour and can be cut entirely if the stop runs long, without
touching the trade.

**Two placement consequences:**

1. **Space the three within the room.** Three markers in one room can
   overlap if their interaction radii touch, and a player aiming at Nathan
   who gets Owen will read that as a bug. Put each at their own work —
   Sophia at the drafting table, Owen at a board, Nathan at a terminal —
   and keep the radii clear of one another.
2. **Sophia's directions now start with stairs.** She is on the second
   floor, so her hand-off opens with *"Down the stairs, right as you come
   out of the office"* rather than sending the player into the street from
   a room they cannot walk out of. The navigation table reflects this.

**THE MILL RUNS (clarified 2026-08-02).** An earlier ambient line had the
sawmill idle — *"not running these days"* — and that was wrong. Hillside
operates the mill. What is deferred is the **timber TRADE**, not the
building and not the work: Sophia's tradeable output in the MVP is still
`engineering_services` and nothing about the trade panel changes.

**Sophia does NOT work in the mill** — an intermediate draft briefly had her
drafting in a loft above it, which contradicts the placement resolved above.
She is in the second-floor room over the realty office, and the mill is
elsewhere in Hillside; her ambient line now places it by SOUND from that
window rather than by her being inside it. The mill runs, she just is not
standing in it.

This costs nothing in the schema and it already fits the seeder as written.
Mountain **Produces** `timber`; Hillside **Needs** `timber` (20). A working
mill is exactly what consumes that — raw logs down from the Mountain,
milled stock back out. The economy has been describing a running mill all
along; only the dialogue said otherwise.

**COMPOSITE LUMBER — Hillside is the node (added 2026-08-02, post-MVP,
narrative only).** The mill also presses structural beam and flat board out
of RECYCLED wood: salvage sorted by the Suburb, plus Mountain's offcuts.
Three reasons it belongs here rather than anywhere else:

1. **The building already exists and already runs.** No new set dressing,
   no resurrection beat needed — this is an extension of working plant,
   which is a far easier thing to believe than a mill restarted from cold.
2. **The industry term for this is ENGINEERED WOOD.** The community that
   sells engineering services makes engineered lumber. The connection needs
   no line of dialogue; it is in the name.
3. **It is the wood mirror of a supply chain the player already sees in
   metal.**
   DeShawn's and Mike's ambient lines already establish Suburb strips
   salvage → City melts it → castings come back. Suburb sorts wood →
   Hillside presses it → board and beam come back reads as *of course*
   rather than as a new idea.

**WHAT IT IS FOR — CORRECTED 2026-08-02: HOUSES AND STRUCTURES.** An earlier
draft of this section led with the wind-turbine blade molds. That was the
wrong emphasis and it is now demoted. **The primary product is structural
beam and board for BUILDINGS** — housing first, other structures after.
Turbine tooling is a beneficiary of the capability, not the reason for it.

That correction matters because of what it connects to.

**It makes the mill a supplier to the REALTY COMPANY.** That company's whole
function is placing community members in housing, and housing has to be
built out of something. So the third cross-community institution turns out
to feed the first, and all three now interlock around the same thing: the
bus fleet MOVES people between communities, the realty company HOUSES them
when they get there, and the mill makes the material the housing is made
of. None of the three works inside one community alone, which is the theme
carried structurally rather than said.

**The world already shows this, which is why it needs no setup.** Hank's
existing Mountain ambient line has new houses going up right now — *"Freshcan
crew finished the new houses last week... nobody's sleeping in a barn"* —
and "new housing" is already in the placed-props list at the top of this
document. Those houses are made of something. Naming Hillside's mill as the
source explains set dressing the player can already see, in the same way the
wood salvage chain reads as obvious next to the metal one.

**Post-MVP this is also the bigger economic story**, not the smaller one.
Mold stock is a one-off for a blade programme. Housing lumber is continuous
demand from all five communities, which makes it a far more plausible
tradeable resource if one is ever added.

**THE MOLD APPLICATION IS STILL REAL AND WORTH KEEPING — it is the SECOND
product, not a throwaway hint (expanded 2026-08-02).** Houses are what the
mill is FOR. Mold stock is the other thing the same press makes, and it is
worth its own beats for reasons that are not just cost:

- **Flat board is where the bio-adhesive actually WINS COMPLETELY.** Mold
  stock is dry, indoors, non-structural tooling — the easy chemistry case,
  which is exactly what structural beam is not. So the mill's SECONDARY
  product is where they reach full independence FIRST, while the primary
  one is still importing hardener. That inversion is worth playing: the
  thing they built the industry for is the thing they cannot finish.
- **MDF is genuinely the standard material for this**, not a substitute for
  something better. It is used for plugs, patterns and low-run molds
  because it is homogeneous, has no grain to tear out, machines to a clean
  surface and holds dimension. Recycled-fibre board with a bio-binder is
  FIT FOR PURPOSE here in a way it is not in a roof beam — a mold carries
  no sustained load and threatens nobody if it fails.
- **Thick sections are LAMINATED, not solid.** A blade mold is machined
  from boards bonded into a billet, not from one slab — which lines up with
  the segmented-mold approach already in the manufacturing plan, and means
  the glue-up matters as much as the board.
- **THE REAL UNLOCK IS ITERATION, NOT COST.** Molds wear, get damaged, and
  are superseded whenever the blade design changes. A community that must
  buy mold stock can build *a* blade. A community that presses its own can
  afford to make the mold again — and remaking the mold is the whole
  difference between having a blade design and being able to IMPROVE one.
  That is a better argument than any saving on sheet goods, and it is the
  one to put in a character's mouth if only one survives.

**Two honest limits, if this is ever detailed further.** MDF is hygroscopic
and moves with moisture, so a mold wants sealing and does not love a damp
shed. And the resin makes it abrasive — it eats cutting tools faster than
solid wood does, which matters for a community that has to make or sharpen
its own.

**The metaphor is worth leaning on because it is literally true.** In sawn
timber a knot is a concentrated weak point and the board is only as good as
its worst spot; in pressed and laminated products the defects disperse, so
strength is both higher and far more PREDICTABLE — engineered lumber
carries tighter design values precisely because no single flaw dominates.
Many small salvaged pieces, bonded, holding up the buildings people live
in. That is the theme as a load-bearing object, and Sophia's ambient line
states it as a fact about wood rather than as a moral.

**Three frictions, kept rather than smoothed:**

- **Adhesive — DECIDED 2026-08-02: BIO-ADHESIVE, from two sources, and it is
  PARTLY solved rather than fully.** This was previously "plant it, do not
  resolve it"; it is now the route the communities take. The chemistry is
  real and mostly old, which is why it can be leaned on:

  **Tannin from BARK is the elegant one, because a working mill already
  makes it.** Logs get debarked; bark is the tannin source. Wattle,
  quebracho and pine-bark tannin adhesives have been in commercial panel
  production in South Africa, Brazil and Australia for decades. The
  feedstock is already falling on Hillside's own floor as waste, so this
  needs no new agriculture at all — and it turns a waste stream nobody had
  a use for into an input.

  **Soy protein from the VALLEY is the other**, and it is the
  commercially-proven modern route: soy flour adhesives dominated US
  plywood in the 1930s–40s before phenol-formaldehyde displaced them, and
  came back at industrial scale in the mid-2000s. This is the source that
  needs the Valley, so it pairs with the biofuel thread — same fields, two
  products.

  **KEEP THE RESIDUAL IMPORT. Do not write this as a clean win.** The real
  systems need a small HARDENER fraction the communities cannot make — the
  commercial soy route uses a petrochemical crosslinker for water
  resistance, and tannin systems traditionally need a hardener too. Soy
  alone is moisture-sensitive. So the honest position, and the better one
  dramatically, is: the BULK of the adhesive is local, a small bought
  fraction remains. Same shape as the biofuel thread, which also does not
  fully close.

  **THE GRADIENT — REVISED 2026-08-02, and it now cuts the OTHER WAY.** An
  earlier draft called this a lucky alignment, on the reading that the mill
  existed mainly to make blade molds. Once the primary product is HOUSING,
  that reverses, and the honest version is better.

  Bio-adhesives are strongest in DRY, INTERIOR, NON-STRUCTURAL service.
  Structural beam carrying load in a building is the HARDEST case there is —
  wet service, life safety, and the one place you cannot quietly accept a
  weaker bond. So the mill's MAIN product is precisely the product that
  keeps needing the bought hardener longest.

  That is a real constraint, not a lucky break, and it should be written as
  one: they can go fully local on interior board, panelling, sheathing and
  tooling stock long before they can go local on the beams holding a roof
  up. The bought fraction shrinks but does not disappear, and it is the
  houses that keep it alive.

  This is a BETTER position dramatically than the version it replaces. A
  community that has solved everything except the one thing that matters
  most is a more interesting place than one that got lucky — and "we can
  make everything but the part that holds the roof up" is a sentence with
  weight in a story about people being housed.
- **Salvaged wood is full of metal.** Nails and screws must come out before
  anything is chipped — magnetic separation plus a lot of hand sorting.
  That is a labour cost, and the Suburb produces `skilled_labor`, so it is
  already priced in the economy rather than being a hole in it. Sophia's
  line calls the nails the worst of it.
- **The press is the hard machine.** Heat plus sustained pressure over a
  large platen is genuinely harder than anything in the Gingery chain.
  Worth knowing before anyone treats this as easy; also a decent thing for
  that arc to be ABOUT.

**Sophia Sandoval — Approach:**
> "So that's the turbine that came with your land. Ambitious purchase —
> nobody's touched that thing in years. Good news is, my two here already
> worked up what you need to bring it back."

**[Player prompt: "What exactly did you put together?"]**

> "Full CAD drawings of the mount and rotor assembly, plus a Simulink
> model of how it should actually behave once it's running. Owen did the
> drawings, Nathan built the model. Between the two, whoever's doing the
> repair up there won't be guessing."

**[Trade panel opens — Engineering Services for Stone:]**
> "Here's everything — drawings and model both. Take care of it; that's
> real engineering hours in your hands, not just a sketch on a napkin."

**[The design-cost half of the Act 3 verdict. Added 2026-08-01. Sophia
delivers HALF the answer and says so — City holds the other half. She must
not be able to conclude alone.]**

> "One more thing, and you won't like it. Doing this properly — not
> patching it, properly — the design hours alone are steep. Whether that's
> worth paying depends on what the parts cost, and that's not my number to
> give you. Ask Mike in the City. Put his figure next to mine before you
> decide anything."

**Farewell — includes the hand-off to Valley:**
> "Good luck up there. Come back through when it's spinning — I'd like to
> see it."

> "Down the stairs, right as you come out of the office, then straight on
> down the market street — don't turn off it. Road climbs out the far end
> and drops you into the Valley. Maria Vega'll be on her porch. She
> generally is."

**Sophia — Ambient (optional, triggered on repeat visits):**

> "Mill's still running — you can hear it from this window when the wind
> sits right. Some people would call that noise. I'd rather hear it going
> than not."

**[Composite lumber — added 2026-08-02. Hillside RUNS the mill; see the
note under this community's heading. Narrative only, no schema change.]**

> "Half of what comes off that saw now isn't logs. Suburb sends us
> salvage — old framing, pallets, whatever's been pulled out of a
> building — and we press it back into board and beam. Takes more sorting
> than milling does. The nails are the worst of it."

> "Most of it goes back into houses. Realty office finds people somewhere
> to live and then somebody has to actually build the somewhere — that's
> been us for a while now. Half the new places in three towns came off
> that saw."

> "Funny thing about a pressed beam — it's stronger than the tree it came
> out of, and steadier with it. A sawn board is only ever as good as its
> worst knot. Break the wood up and bond it back together and the flaws
> are all still in there. They've just stopped being in the same place."

> "Flat board's the easy one — panelling, sheathing, anything that lives
> indoors and doesn't hold weight. Beams are the hard part, and beams are
> what a house actually needs."

> "Same flat board would do for your blade molds too, if that ever comes
> to anything. Gets bought in with dollars today. It doesn't have to."

> "And that's the part worth having, to my mind. Anybody can build one
> blade if they buy the board for the mold. It's making the mold AGAIN
> that matters — you never get the first one right, and if you can't
> afford to redo it you're stuck with whatever you managed first try.
> That's not designing anything. That's just guessing once."

**[Bio-adhesive — added 2026-08-02. Two sources and a residual import; see
the frictions note under this community's heading. The bark line is the one
to keep if these get cut for length: it turns a waste stream into an input
without anybody explaining that that is what it does.]**

> "Glue's the part nobody thinks about. It's the one thing in that press we
> buy in — and we buy it in barrels."

> "Bark, though. We strip tons of it off the logs and it's done nothing for
> us but pile up. Turns out you can cook a binder out of it. Maria reckons
> she can grow the other half of what we'd need. Between the two we'd be
> most of the way there."

> "Most of the way, mind. There's a hardener in it we haven't a hope of
> making, and there's no talking a beam into holding without it. But the
> flat board for your molds is an easier ask than a beam — dry, indoors,
> nothing hanging off it. That's the part we could stop buying first."

> "Got solar on the roof now, battery bank right beside it. Doesn't run
> much, but it keeps the lights on through a cloudy week while these two
> are hunched over a screen."

> "Half the fittings on that bank came up from the Suburb — somebody else's
> junk, cleaned up and re-cut. Saves me drawing parts that already exist."

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

### Nathan Ferris — Simulink Modeler

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

## Valley — Maria Vega (co-op lead)

*(New NPC — no asset assignment made yet, same flag as Hillside above.
Updated 2026-07-18: dialogue broadened to reflect Valley producing more
than just grain — vegetables, meat, honey, and non-orchard fruits
(berries, melons, and the like — distinct from Hillside's existing
Orchard Fruit resource, so the two don't overlap). This is FLAVOR ONLY for
now — the actual trade stays Grain-for-Stone, no schema change. A real
resource change is planned for later; when that happens, this dialogue
will need a matching pass, same as Sophia's did for engineering_services.)*

**Approach:**
> "Feeding a work crew on a mountain, in the cold, for however long it
> takes to hang a turbine? That's food money, not tool money. Good thing
> we had a strong season — grain's the bulk of it, but there's plenty
> else coming off this land too."

**[Trade panel opens — Grain for Stone:]**
> "Here — grain'll keep your crew fed a good while on its own. Ledger
> says it's fair, and I trust the ledger."

**Farewell — includes the hand-off out of Valley:**
> "Tell Hank the Valley says good luck. And tell him he still owes us from
> last winter."

> "Left out of the house and follow the dirt road down — it runs all the
> way into the Suburb. Ask for DeShawn Okafor; he works out of the realty
> office there. Walk straight in, he won't stand on ceremony."

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

> "That control box on the silo — Hillside drew it, City built it, and
> DeShawn's people put it in. Three towns for one little grey box. Nobody
> thought that was strange, which is the part I like."

> "Nothing organic leaves this valley. Stalks, husks, what the animals
> make — it all goes back on the fields or into the digester. Everybody's
> got their own way of not wasting things. Ours just smells worse than
> most."

**[Biofuel set-up — added 2026-08-02. Pairs with DeShawn's line; between
them the post-MVP conversion project is fully planted from both ends.
Narrative only.]**

> "DeShawn keeps asking what else that digester could do. Truth is, plenty
> — we could grow and press what those buses burn, if somebody sorted out
> the engines. That's not our end of it. But it's the first thing anybody's
> asked us for that we'd be growing instead of just handing over."

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

**[Navigation hand-off — added 2026-08-02. Sends the player onto the bus
loop to City, and names Mike so the arrival has a target:]**

> "City's too far to walk and I'm not sending you. Stop's on the corner —
> take the loop. Ask for Mike Dayton, he'll be on the shop floor, and he'll
> be the loudest thing in the building."

**Ambient (optional — triggered on repeat visits; references the newly-
added Suburb assets):**

> "Control power station runs the whole block now — panels on half these
> roofs, power bank in the old garage soaking it up. Wasn't cheap to set
> up, but it beats waiting on somebody else's grid."

> "Funny thing about recycled parts — half that control station's
> housing used to be something else entirely. We don't waste much out
> here. Can't afford to."

> "Anything we strip that's worth melting goes up to Mike in the City —
> comes back as castings we couldn't make ourselves. Fair trade. Neither
> of us has the whole shop, but between us it's most of one."

**[Composite lumber feedstock — added 2026-08-02. The wood counterpart to
the metal line directly above; deliberately placed next to it so the two
read as one pattern. Narrative only.]**

> "Wood's the same story, different direction. Framing, pallets, anything
> we pull out of a building that isn't rotted — that goes up to Hillside
> and comes back as beam. We pull the nails here. They'd rather we did,
> and honestly so would their saw."

> "Crews eat Valley grain on every job over a week. Maria won't let me
> send anybody up a mountain on what the canteen calls food."

**[Biofuel set-up — added 2026-08-02. Plants the post-MVP conversion
project. Narrative only; no fuel mechanic exists or is implied.]**

> "Five towns, one set of buses, and every drop of fuel in them bought with
> dollars we don't get back. That's the part that keeps me up. Maria
> reckons the Valley could grow what we'd need instead, and converting the
> engines is shop work — we could do it here, or City could. Nobody's
> arguing about whether. Just about when."

---

## City — Mike Dayton (factory foreman, Manufactured Tools) and Kai
Sutherland (systems engineer, Software Services)

*(Two NPCs at one location, since City covers two resources. New NPCs, no
asset assignment made yet, same flag as above.)*

**RESOLVED 2026-08-02 — two markers, one building.** Mike is on the machine-
shop floor, Kai is in the office above it. That keeps the one-trade-per-
approach pattern every other community uses, and because both markers sit in
the same building the hand-off is "upstairs at the back" — no map directions,
no navigation cost for the second marker. The player arrives from the Suburb
on the bus loop and reaches Mike first.

### Mike Dayton — Manufactured Tools

**Approach:**
> "Got Owen's CAD drawings from Hillside a few days back — went through
> them, worked up what it'll actually take to machine replacement parts
> for that mount. Good drawings, too. Made the estimate a lot less of a
> guess."

**[Trade panel opens — Manufactured Tools for Stone:]**
> "This'll hold. Good steel, machined straight off Owen's drawings — not
> the kind of thing that fails halfway up a mountain."

**[The manufacturing-cost half of the Act 3 verdict. Added 2026-08-01. Mike
gives the OTHER half — he can price parts but not judge the engineering, so
he also cannot conclude alone. The two halves meet at Hank.]**

> "Now — what you're paying me for is this batch. If you're asking what it
> costs to keep that machine running for good, every part on Owen's
> drawings has to be made one at a time, by us, forever. I can put a number
> on that. You won't enjoy it, and it's only half the picture — Sophia's
> got the design side. Take both to Hank."

**[The interchangeable-part beat. Concrete expression of "common designs"
— no speech about standardisation, just a part fitting.]**

> "Here — that bearing's off our own shelf, not made for your mount.
> Same drawing though, so it'll drop straight in. That's the part that
> ought to interest you more than the invoice."

**Farewell:**
> "Bring it back down if it ever needs work again. We don't forget good
> customers."

**[Navigation hand-off — added 2026-08-02. Two markers, one building, so
this needs no map directions:]**

> "You'll want Kai before you go anywhere. Straight up the stairs at the
> back — office over the shop floor. Metal's only half of what you're
> carrying home."

**Ambient (optional — triggered on repeat visits):**

> "Furnace runs near round the clock in here — that's where your fittings
> came from, and half the factory's tooling besides. Advanced setup,
> newer than most of what we had before. Keeps up with demand."

> "Wouldn't have quoted this job as tight without Owen's numbers. Half
> our estimating headaches come from guessing at parts nobody's measured
> properly."

> "Feedstock's mostly Suburb scrap these days. DeShawn's crews strip it,
> we melt it. Cheaper than buying billet and it keeps two towns working."

### Kai Sutherland — Software Services

**Approach:**
> "Took Nathan's Simulink model and turned it into real controller code —
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

**[Navigation hand-off — added 2026-08-02, revised same day. Closes the
route into Act 3. An earlier version had the bus stop short of the summit
and the player walk up; that is withdrawn — the loop runs all the way to
Mountain.]**

> "Loop runs back round the way you came — stays on it all the way up to
> Mountain. Get on, and the next thing you'll be looking at is that
> turbine. Hank'll be waiting."

**Ambient (optional — triggered on repeat visits):**

> "Control power station over there's the closest thing City's got to a
> single point of failure — everything downstream leans on it. I don't
> love that, but redesigning the whole grid isn't exactly this week's
> problem."

> "Enclosure's nothing fancy — just needed to keep the electronics dry
> and let heat out without letting weather in. Modeled it in Fusion in an
> afternoon. The code took a lot longer than the box did."

> "Half the boards in that station were pulled out of dead equipment and
> re-flowed. We strip more electronics here than anyone — the Suburb sends
> us the carcasses, we take the parts nobody else can reuse. Cheaper than
> new, and there isn't a 'new' to buy most weeks anyway."

---

## Open items for you

1. **Named NPCs for Hillside, Valley, Suburb, and City are new
   inventions** (Sophia Sandoval, Owen Marsh, Nathan Ferris, Maria Vega,
   DeShawn Okafor, Mike Dayton, Kai Sutherland) — none of these have an
   asset assignment yet, unlike Hank's confirmed Yarrawah "Murph" Murphy
   character. Worth deciding whether each gets a similarly specific named
   character asset, or stays as a generic/unnamed mesh pulled from
   whichever community pack is already in use there (TownShops, Suburb
   Neighborhood, etc.).
   **RENAMED 2026-08-01:** Reya Sandoval → **Sophia** Sandoval; Lena Ferris
   → **Nathan** Ferris, who is now a man (his lines are all first person, so
   no third-person pronouns needed changing); Marisol Vega → **Maria** Vega.
   SURNAMES CONFIRMED UNCHANGED (2026-08-01): Sandoval, Ferris and Vega all
   stay as they were — only the given names changed. Settled, not an open
   question.
2. **The Valley farewell line** implies an existing relationship between
   Hank and Valley ("he still owes us from last winter") — a small piece
   of worldbuilding not established anywhere else. Fine to keep as a light
   touch, or cut if you'd rather every NPC's dialogue stay fully
   self-contained with no implied history.
3. **City's two-NPC structure — RESOLVED 2026-08-02.** Two markers, one
   building: Mike on the machine-shop floor, Kai in the office above it.
   Keeps the one-trade-per-approach pattern, and the hand-off between them
   is "upstairs at the back", so the second marker costs nothing in
   navigation. **Hillside's equivalent question is also resolved** — see
   the note under that community's heading; all three of its NPCs are in
   the second-floor room above the realty office.
4. **The Suburb Dollar Vault beat is still marked optional**, same as it
   was in the storyline doc — this dialogue includes it, but cut both the
   flagged lines above if you've decided against including it.
5. **Hillside now has THREE NPCs at one stop** (Sophia, Owen, Nathan) — same
   one-marker-vs-multiple-marker question as City above applies here too;
   worth deciding whether all three appear together at a single Hillside
   marker (matches the "one trade, one stop" pattern elsewhere since it's
   still a single `engineering_services` trade) or are spread across the
   Hillside level as separate points of interest to walk between.
6. **Sophia's old sawmill-line assumption is now removed** — since her
   dialogue no longer centers on lumber, the earlier open item about
   confirming QuadArt Survivor Base's sawmill mesh naming is no longer
   tied to her dialogue specifically. That mesh-naming question still
   matters for the timber trade's eventual post-MVP return, just not for
   this MVP dialogue pass.
7. **Mike and Kai's dialogue now references a connected digital-thread
   chain** (Owen's CAD → Mike's manufacturing estimate; Nathan's Simulink
   model → Kai's generated C controller code → Kai's Fusion 360 enclosure
   design) — added 2026-07-18. Currently DIALOGUE-ONLY: nothing new is
   actually being built or modeled for the MVP itself. If you want a real
   Fusion 360 controller-enclosure model to exist (the same way the
   Hillside-sawmill capability-demo idea was captured), that's a separate
   post-MVP task, not automatically implied by this dialogue update —
   confirm if you want that added to Todoist the way the sawmill idea was.
8. **Valley's food variety (vegetables, meat, honey, non-orchard fruits)
   is FLAVOR-ONLY as of 2026-07-18** — Maria's dialogue now mentions
   these, but the actual trade stays Grain-for-Stone; no schema change.
   Confirmed the future fruit resource will be specifically NON-orchard
   fruit (berries, melons, etc.) to avoid overlapping with Hillside's
   existing Orchard Fruit resource. A real mechanical expansion is planned
   for later — when that happens, this needs the same treatment as the
   engineering_services change (new resource(s), guardrail files, its own
   SCOPE.md entry, and another dialogue pass here).
