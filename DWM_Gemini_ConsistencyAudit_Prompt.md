# Prompt for Gemini — DWM Documentation Consistency Audit

## How to use this

Attach/paste these four documents into the same Gemini conversation before
sending the instructions below:
1. `SCOPE.md`
2. `DWM_MVP_Storyline.md`
3. `DWM_MVP_Dialogue.md`
4. `DWM_MVP_Plan.docx` (or its extracted text)

Then send Gemini the instructions in the box below.

---

## Instructions to send Gemini

I'm going to give you four documents for a game project called Dream World
Maker (DWM). Several real decisions changed over the course of one long
work session, and I need you to find every place where an older decision's
language, name, or assumption is still sitting in one of these documents
even though it's been superseded elsewhere. This is a READ-ONLY audit —
don't rewrite anything, just report what you find with exact quotes and
locations.

**Known changes that happened, in order — check that ALL FOUR documents
are consistent with the FINAL state of each, not an earlier one:**

1. Hillside's terrain was originally FreshCan Mountain Grassland, then
   briefly changed to a shared Snowy Peaks base with Mountain, then
   finally reversed to PropHaus's TownShops. Check for any remaining
   references to FreshCan Mountain Grassland or shared-Snowy-Peaks as
   Hillside's terrain.

2. Hillside's trade to Mountain changed from Timber to a new
   `engineering_services` resource (CAD drawings + Simulink model), tied
   to a new premise: the wind turbine came with Mountain's land purchase
   and was never fixed (NOT storm damage, which was the original premise).
   Check for any remaining references to: Timber as Hillside's trade,
   storm damage as the turbine's backstory, or a sawmill-foreman framing
   for Sophia Sandoval (her role changed to engineering firm lead).

3. City's Manufactured Tools NPC was renamed from "Itzel Reyes" to "Mike
   Dayton." Check for any remaining "Itzel" references anywhere.

4. Two new Hillside NPCs were added: Owen Marsh (CAD designer) and Nathan
   Ferris (Simulink modeler), alongside Sophia. Check that all three are
   consistently present wherever Hillside's NPCs are listed or described.

5. Mike Dayton and Kai Sutherland's dialogue was updated to reference
   using Owen's CAD drawings and Nathan's Simulink model specifically (Mike
   for a manufacturing estimate, Kai for generated C controller code plus
   a Fusion 360 enclosure design). Check this connection is described
   consistently, not just in one document.

6. Valley's Grain trade got flavor-text additions (vegetables, meat,
   honey, non-orchard fruit) — as of the last update, this was planned to
   become a REAL mechanical change (new resource types added), not just
   flavor. Check whether any document still describes this as flavor-only
   in a way that contradicts a real mechanical implementation, OR flag if
   it's unclear which state (flavor-only vs. real resources) is actually
   current based on what's in front of you.

7. The World Partition streaming spike was marked OBE, replaced by
   testing Labgames' Level Transition System between separate community
   levels. Check for any remaining World Partition / open-world /
   single-shared-landscape language that contradicts the separate-levels
   architecture.

**Also flag, independent of the list above:**
- Any place one document assumes a decision that doesn't actually appear
  resolved in the others (e.g., an open question in one doc that's
  treated as settled in another).
- Any character name, resource name, or place name spelled/used
  inconsistently across the four documents.
- Any dated SCOPE.md decision that appears to be superseded by a LATER
  dated entry, but the earlier entry's content wasn't explicitly marked
  as superseded.

**Output format:** a table or list with: (1) what's inconsistent, (2)
exact quote and which document it's from, (3) what it should probably say
instead based on the most recent decision, (4) your confidence level (are
you sure this is stale, or is it ambiguous and worth a human double-check).

Don't guess at fixes beyond flagging the likely correct version — if
you're unsure which of two conflicting statements is actually current,
say so explicitly rather than picking one.
