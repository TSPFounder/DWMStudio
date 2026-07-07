# World Partition Streaming Spike — Week 3 Checkpoint

**Question this answers:** does one continuous level with World Partition streaming hold
frame rate across multiple detailed community zones, or does the MVP need to fall back to
separate maps + a level-transition asset? Answer this with two zones, cheaply, BEFORE all
five are fully dressed — a failure found here costs an afternoon; the same failure found in
Week 6 costs a redesign under deadline pressure.

**Do this before real zone-dressing work goes further than blockout.** If the level isn't
already World Partition–enabled, converting it now (near-empty) is trivial; converting it
later (five dressed communities) is a much bigger, riskier operation.

## Setup (once)

- [ ] If the level isn't already World Partition, convert it: **Tools → World Partition
      Conversion**. If starting fresh, use an "Open World" template instead.
- [ ] World Settings → World Partition Setup: start with default cell size / loading range —
      don't hand-tune before you have a baseline to tune against.
- [ ] Open **Window → World Partition** to visualize the streaming grid while you work.

## Asset-specific caveats — READ BEFORE BUILDING THE TEST ZONES

Verified via search, not assumed. World Partition works with standard Landscape actors
generally, but the specific assets on SCOPE.md's watch-list have named, documented quirks —
test for THESE specifically, not generic WP behavior:

- **Brushify (SmartBrush):** a documented issue (UE 5.6 forum report) has SmartBrush causing
  `LandscapeStreamingProxy` to stay "Always Loaded" in the EDITOR regardless of Data Layers
  or loading-range settings. The same report states PIE/Standalone streaming works fine —
  this looks like an editor-authoring-only quirk (the editor feels heavier while actively
  sculpting with SmartBrush), not a player-facing problem. **Test both**: (a) authoring
  performance while using SmartBrush with WP enabled, not just (b) the PIE walk-through —
  don't assume (b) passing means (a) is fine too, they're different questions.
- **MEKA (Nanite landscapes):** Nanite landscape data streams IN ADDITION TO standard
  landscape data (both the Nanite geometry stream and the non-Nanite texture/RVT stream are
  required at runtime) — a Nanite landscape roughly DOUBLES the streamed data versus a
  non-Nanite one. Budget for this explicitly; "Nanite" is not a free streaming-performance
  win here, it's a tradeoff (better VSM/shadow rendering cost, more streaming cost).
  **Also:** there's a documented bug where World Partition + HLOD + Landscape Nanite
  together can visibly BREAK the distant HLOD proxy mesh (not just look worse — render
  incorrectly). This directly affects the MARGINAL fallback below: if a Nanite-landscape
  zone lands in MARGINAL, test the HLOD fix in isolation before trusting it — it may
  introduce a new problem rather than solve the old one.
- **FreshCan (4x4km landscapes):** no verified information found on its specific World
  Partition behavior — don't assume it behaves like the two above. Check FreshCan's own
  documentation/product page, or treat its zone in this spike as the one where you have the
  LEAST prior confidence and watch it most closely.

If the two test zones don't already cover at least one Nanite-landscape asset and one
non-Nanite asset, adjust the zone picks below so the spike actually tests these real risks
rather than generic World Partition behavior.

## Build the two test zones (blockout + REAL heavy assets, not placeholder cubes)

The point of this spike is to test real weight, not an empty room. Use actual intended assets:

- [ ] **Zone 1 (e.g. Mountain):** one real terrain treatment (per SCOPE.md's watch-list —
      Brushify or MEKA landscape, pick whichever is the actual candidate), the landmark
      silhouette, basic settlement-kit placement. Skip fine dressing — this needs to be
      *heavy*, not *finished*.
- [ ] **Zone 2 (e.g. Valley):** same treatment, different terrain asset, enough distance from
      Zone 1 that a player walking between them gives streaming real room to work.
- [ ] Assign each zone's actors to its own **Data Layer** (one per community — this is the
      long-term pattern, set it up now rather than retrofitting later).
- [ ] If each community has its own Landscape actor (recommended — see rationale below),
      confirm both are present and reasonably sized.

## Run the test

- [ ] PIE, standing in Zone 1. Note `stat unit` (game/draw/GPU frame times) at rest.
- [ ] Walk the full distance from Zone 1 to Zone 2 at normal player speed.
- [ ] Watch for: frame-rate drops during the walk, visible pop-in/hitching as Zone 2 streams
      in, Zone 1 streaming out cleanly behind you (check the World Partition window — cells
      should unload, not stay resident).
- [ ] Stand in Zone 2, confirm Zone 1 is genuinely unloaded (not just out of view) via the
      World Partition debug visualization or `stat streaming`.
- [ ] Repeat the walk 2–3 times — first-time asset loading can look worse than steady-state;
      you want the steady-state number.
- [ ] **If either zone uses Brushify SmartBrush:** separately, in the EDITOR (not PIE), do a
      few sculpt operations with SmartBrush active and watch editor responsiveness / whether
      the World Partition window shows the landscape as "Always Loaded" regardless of
      distance. This is a different question from the PIE walk-through above — test it
      explicitly, don't infer it from the PIE result.

## Pass / fail bar

- [ ] **PASS:** frame rate holds at or above your mid-range target throughout, no jarring
      pop-in crossing the boundary, World Partition window confirms cells unload as expected.
      → Proceed with one continuous level for all five communities. Tune cell size/loading
      range per-zone if any zone is heavier than others, rather than changing the global
      default.
- [ ] **MARGINAL:** frame rate dips but recovers, minor pop-in. → Try tightening cell size /
      loading range, reducing one zone's asset density, or building HLOD proxies (**Build →
      Build HLODs**) before re-testing. **If the affected zone uses a Nanite landscape (MEKA),
      test the HLOD build in isolation first** — see the Nanite/HLOD caveat above; this
      specific combination has a documented breakage risk, so confirm it actually helps
      before relying on it. Don't fall back yet otherwise — this usually tunes out.
- [ ] **FAIL:** frame rate doesn't recover even after tuning, or the streaming/unstreaming
      itself is unreliable (things stay loaded that shouldn't, or don't load in time). →
      This is the trigger to fall back to Option B (separate level files + a level-transition
      asset, e.g. the one evaluated earlier) for the remaining zones. Better to know this in
      Week 3 with 2 zones than Week 6 with 5.

## Record the result

- [ ] Add one line to SCOPE.md's decisions log: pass/marginal/fail, and if marginal/fail,
      what was tried and the fallback decision. This is a real architecture decision for the
      whole MVP — it belongs in the permanent record, not just in your head.
- [ ] If PASS: note the confirmed cell size / loading range settings somewhere durable (this
      file, or ARCH.md) so Weeks 4–6's zone work doesn't have to rediscover them per zone.

## Why separate Landscape actors per community (not one mega-landscape)

World Partition streams landscape data via Landscape Streaming Proxies, generated per
Landscape actor. Five separate community landscapes give World Partition clean boundaries to
stream around; one continuous landscape spanning all five is harder to partition cleanly and
works against the "one terrain treatment per zone" rule already in SCOPE.md. Keep them
separate — this also matches how you're already thinking about the zones individually.

## Note on scope

This spike is infrastructure validation, not zone-dressing work — it does not count against
the "one terrain treatment per zone, half-day cutoff" rule for optional accelerators. It's a
foundational architecture check the other zone work depends on, closer in kind to the Day-1
tracer bullet than to a Week-3-6 dressing pass.
