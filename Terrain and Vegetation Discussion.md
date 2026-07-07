are the Brushify brushes just big meshes that are combined to look like a terrain?  How do you scale them in the project?

Let me verify this rather than rely on general impressions of how Brushify works — the mesh-vs-landscape distinction matters for how you'd actually use it, and "scaling" could mean a few different things depending on the answer.Searched the webSearched the webGood — this clears it up with real specifics, and the answer is genuinely "both/either," which is worth understanding clearly since it changes how you'd approach a mountain vs. a distant backdrop.

Not "just big meshes" — the core terrain is real sculpted Landscape, done two different ways:



SmartBrush (the sculpting tool): these are not meshes at all — they're height-alpha brushes (8K, 16-bit heightmap data captured from real-world terrain) applied through Epic's Landmass plugin, which is a non-destructive, Blueprint-driven system for sculpting an actual Landscape actor's heightmap in real time. So when you use a SmartBrush "Mountains" brush, you're not placing a mountain-shaped mesh — you're stamping real terrain height data directly into your Landscape's geometry, the same underlying heightmap system as any hand-sculpted terrain. This is genuine terrain, not meshes dressed up to look like terrain.

Distance Meshes: this is where meshes do come in, but for a specific, different purpose — background scenery far beyond the actual playable area ("extend your world far into the horizon without affecting performance"). These are lightweight static meshes generated from the same real-world height data, meant to sit on the horizon as cheap-to-render backdrop, not to be walked on. So the terrain you actually walk on is sculpted Landscape; the mountains you see way off in the distance that you'll never reach are meshes standing in for far-away geography.



Given your five-community layout, this maps cleanly: each community's actual walkable ground is a sculpted Landscape actor (SmartBrush-shaped), and Distance Meshes could be used for any far-off scenery beyond the playable zones — e.g., if Mountain's zone should show a horizon of more distant peaks the player never visits.

On scaling — the search surfaced two distinct answers, and they matter for different things:



Landscape actor scale (the whole terrain's size): confirmed directly from Brushify's own docs — "Go to Landscape actor and increase to your desired scale (1:1 ratio only)." You select the Landscape actor itself and adjust its Scale in the Details panel, keeping X/Y/Z uniform (1:1:1) so it doesn't distort. This is the standard Unreal way to scale any Landscape, Brushify or not.

One real technical limit worth knowing: Brushify's own product listing warns "Landmass works best on landscapes 2048×2048 and lower." If you scale a Landscape up significantly beyond that resolution, the SmartBrush/Landmass sculpting tool itself may not perform well, even though the base Landscape system can technically go bigger. So there's a practical ceiling on how large a single SmartBrush-sculpted community terrain should be before the tool (not the Landscape actor) starts struggling — worth respecting for your five community-sized (not open-world-sized) terrains.

Individual asset/brush intensity, separately: Brushify's docs also mention adjustable brush settings like height, edge falloff, and incline strength — that's "scaling" a specific sculpting stroke's effect, distinct from scaling the whole Landscape actor.



So for your five communities: you'd size each one's Landscape actor to whatever footprint that community needs (staying comfortably under the \~2048×2048 Landmass-friendly resolution, which should be generous for a single community zone rather than an open world), then use SmartBrush strokes to sculpt the actual terrain shape within it — real geometry, not meshes standing in for terrain.There’s not much documentation for it so it may not be the right choiceWeighed documentation risks against deadline constraints and alternativesWeighed documentation risks against deadline constraints and alternativesFair, and I think that's the right read. Undocumented marketplace tools are a real time-risk category of their own — even confirmed 5.3-compatible, if something doesn't behave as expected, you have no reference to consult and could burn real hours reverse-engineering a niche plugin instead of building your zones. Against a September deadline, that's a legitimate reason to deprioritize it, separate from whether it would technically work.

One thing worth a two-minute check before writing it off completely: many marketplace plugins that lack formal written docs still ship with an example/demo level in their content folder — that's often the de facto documentation. If you have it installed already, glance at its content folder for anything like "DemoLevel," "ExampleMap," or similar. If a demo level exists showing two landscapes already stitched together, you can learn the workflow by inspecting it directly rather than needing written docs. If there's genuinely nothing — no demo, no example, no readme — that's a stronger signal to set it aside for now.

My honest recommendation: don't plan your zone architecture around it. Treat it as "worth a curious 20-minute experiment sometime if you have slack," not as something Week 3–6's real work depends on. Here's why that's the right call given everything we've worked through:

You already have a full toolkit that requires zero new, unverified dependencies — everything from earlier in this conversation:



Chokepoints (mountain pass, treeline, river crossing) to hide the seam from view entirely — free, no tool needed, and thematically appropriate for your world.

Edge dressing (rocks, fallen logs, a fence line) to visually break up any residual seam.

Manual height-matching by hand — slower, but fully within your control, no dependency on anything undocumented.



Combined, those three get you a genuinely workable result using only things you fully understand and control. The separate-levels-plus-transition-asset route (the Labgames Level Transition System from the very start of this conversation) remains your clean, well-understood structural fallback if the manual techniques don't satisfy you visually.

So the practical plan: proceed with the manual techniques as your default, keep the stitching plugin in your back pocket as a "try it if there's ever a slow afternoon" curiosity rather than a scheduled task, and treat the separate-levels-plus-transition-asset architecture as your real fallback if manual blending isn't good enough once you actually see it in the engine.

Given that, I'd revise WORLD\_PARTITION\_SPIKE.md to test the manual techniques (chokepoint + dressing + hand-matching) between two separate Landscape actors as the primary path, rather than assuming either the single-continuous-landscape approach or the stitching plugin. Want me to update it that way now?Can you place one Landscape actor on top of another and have a character walk off one to the other?Examined layering mechanics and designed natural elevation transitionsExamined layering mechanics and designed natural elevation transitionsYes, mechanically it works — but it's worth being precise about which version of "on top of" you mean, because there are two quite different setups hiding under that phrase, and one of them is actually a genuinely good idea for your situation.

Basic fact first: a character's movement doesn't care what object the ground is. Unreal's character collision just follows whatever solid surface exists at that location — it makes no distinction between "still on the same Landscape actor" and "now on a different one." So walking from one Landscape actor onto another works exactly like walking from Landscape onto a static mesh floor, or from one room into another — completely seamless from a pure movement standpoint, as long as the two surfaces meet with no gap and no un-climbable cliff in between.

Configuration 1 — genuinely stacked (same footprint, different height), like a floating plateau above a valley floor. You can technically position two Landscape actors so one sits directly above the other in the same X/Y space. But this creates real design complications: whichever surface is physically on top is the only one the character can stand on from above — the lower one becomes hidden or inaccessible underneath unless you deliberately build an opening (a cave mouth, a cliff face) to reach it. This is genuinely more advanced setup (think floating islands, overhangs, or a cave system beneath the surface) — not what you'd want for "walk from Mountain into Valley."

Configuration 2 — adjacent footprints at different heights, meeting at a boundary. This is almost certainly the more useful idea for you, and it reframes your whole seam problem rather than just working around it. Instead of trying to make Mountain and City meet at the same elevation (which is what's been driving this whole discussion), let them be at genuinely different heights — Mountain sits higher, City sits lower, because that's realistic — and build a deliberate connecting slope, switchback trail, or stairway between them as the transition. The player experiences it as walking up into the mountains or walking down into the valley, which is a natural, expected thing in real terrain, not a disguised technical seam.

Why this is actually a better idea than height-matching: it turns your problem inside out. You're no longer trying to hide a mismatch — you're designing the transition as a real feature: a mountain path, a set of terraced steps, a winding road down a hillside. That path becomes your "buffer zone" from earlier, except now it's doing honest work (getting the player up or down a real elevation change) instead of pretending two flat things match. You control exactly how steep it is, how long it takes to traverse, and you can dress it richly (switchbacks, small overlooks, waypoint markers) since it's now a deliberate piece of your world rather than a seam to camouflage.

The one technical requirement: the connecting path needs a slope gentle enough for the character to actually walk (most UE character movement components have a "walkable slope angle" limit — steeper than that reads as a wall, and the character will slide or be blocked). So the transition zone's design job becomes: get from Mountain's height to City's height over enough horizontal distance that the average slope stays walkable — classic switchback trail design, same principle as a real mountain road.

So, concretely: rather than sculpting Mountain and City to secretly agree at the same elevation, let them genuinely differ, and put a real, walkable, gently-sloped connector between them. That's likely less fighting-the-tools than trying to force two dramatically different landscapes to silently match.What cliff and rock assets could I use for that based on the assets I previously posted?Inventoried asset capabilities and identified Brushify's cliff toolsInventoried asset capabilities and identified Brushify's cliff toolsLet me be precise about what I can actually confirm here, since "based on the assets I previously posted" spans a few different packages with very different levels of documented detail.

Brushify — the one I can confirm has real cliff/rock content, directly from its own docs:



Landform: Brushify's SmartBrush set includes dedicated "Canyons" and "Desert Cliffs" brush categories (4 brushes each, per its feature list) — these sculpt actual cliff-face and canyon-wall shapes into your landscape's heightmap, which is exactly what you'd want to carve the walls of a switchback trail.

A specific technique for steep faces, straight from Brushify's own documentation: their docs explicitly cover "how to add more realistic Cliffs... and how to enable Tri-planar mapping for more vertical landscape detail" — Tri-planar mapping exists specifically to fix the ugly texture-stretching that normally happens on steep or vertical landscape slopes, so this is the right technique to apply anywhere your trail's cliff walls get steep.

Decoration: Brushify's docs also state plainly "it's very important to decorate cliffs using Static Meshes in order to maximize the detail" — and separately, its Auto-Material automatically scatters optimized rocks as part of its normal ground coverage. So you'd have both landscape-level cliff shaping and rock-mesh dressing available from the same package.



So for the trail itself: sculpt the cliff walls with Brushify's Canyons/Desert Cliffs brushes, enable Tri-planar mapping on the steep sections, and dress the trail edges and switchback corners with Brushify's rock scatter (or hand-placed rocks pulled from its Distance Mesh set for the more prominent, close-up ones).

MEKA and FreshCan — I genuinely don't know, and I don't want to guess. My earlier research on both was limited to landscape/streaming behavior; I never confirmed what static meshes (rocks, cliff decoration, etc.) either package actually ships with. The fastest way to find out is direct, not another search: open each asset's folder in your Content Browser and look for a "Rocks," "Cliffs," or "Props" subfolder — that tells you definitively what's available, faster and more reliably than anything I can find secondhand.

One more resource worth naming, if you have access to it: Quixel's broader Megascans library (distinct from the "Megaplants" foliage pack named in your SCOPE.md) has an extensive collection of scanned rock and cliff-face meshes, widely used for exactly this kind of terrain dressing. That's a different, bigger resource than what's currently in your plan — not something I'm confirming you own, just flagging in case Quixel Bridge access is available to you and Brushify's own rocks turn out to be too sparse for a prominent trail.

My concrete recommendation: start with Brushify for the trail, since it's the one I can verify has purpose-built cliff-sculpting and rock-decoration tools that work together as a system. Check MEKA/FreshCan's content folders for anything additional, and keep Megascans in your back pocket only if you need denser or more varied rock dressing than Brushify's own set provides.Do you know if the Level Transition System asset has screen transition images other than those weird colorful globs?Investigated asset details and sought marketplace verificationInvestigated asset details and sought marketplace verificationLet me check its actual store listing and preview images rather than guess at what you're describing — this is exactly the kind of visual/content detail I didn't verify in the original search.Searched the webSearched the webThis confirms what the asset actually is, but it doesn't let me directly see the preview images or video themselves — text search can surface the store page's description text, but I can't view the screenshots/video content, so I can't personally confirm or deny the "colorful globs" you're describing from having seen it.

Here's what I can tell you confidently from the actual product description, though, and it directly answers your question:

Each transition container includes "a mesh for the rotating actor" — meaning the visual centerpiece of the transition screen is whatever 3D mesh you assign, not a fixed built-in animation. The system's whole design is a content-agnostic container: heading text, info text, minimum display time, a mesh, and a rotation speed. You supply the mesh. So if what you saw in a preview video was some default/sample mesh that reads as an abstract colorful blob, that's very likely just whatever placeholder or demo mesh the developer used to showcase the system — not a fixed, unchangeable visual you're stuck with.

This means the actual answer to your question is: yes, almost certainly. Since you assign the rotating mesh yourself, you could swap it for anything appropriate to DWM — a low-poly version of your wind turbine, a Stone/coin icon representing the ledger, a stylized version of one of your community landmarks, or literally any mesh you already have in your project. The "colorful globs" in the preview are the demo content showing the system works, not a constraint on what you're allowed to display.

To actually confirm this rather than take my inference on faith: the product page has an "Implementation" video specifically (separate from the general preview) — that one's more likely to show the container-configuration workflow itself, including where you plug in your own mesh, rather than just the flashy demo reel. Worth watching that one specifically if you want to see the actual swap-in-your-own-mesh step before buying/committing time to it.

So: the "weird colorful globs" you saw are almost certainly demo dressing, not a limitation — but I'd confirm by watching the Implementation video (or checking the documentation, which the listing says is included) rather than trusting my inference alone, since I genuinely can't see the images myself to be 100% sure.

You helped me with a foliage removal issue back in May:  how do i Switch to Landscape Paint mode and paint a layer that has no Grass Type assigned (e.g., a bare dirt or rock layer) over the area you want cleared.   Switch to Landscape Paint Mode

Press Shift+3 or click the Landscape icon in the mode toolbar (left side of the editor)
At the top of the Landscape panel, you'll see three tabs: Manage / Sculpt / Paint — click Paint
Identify a Layer With No Grass Type
In the Paint panel you'll see a Target Layers list — these are all the material layers your landscape material exposes. You need to find one that has no LandscapeGrassType asset connected to it in the material.
If you're not sure which layer is "bare":

Open your landscape material in the Material Editor (find it in the Content Browser — it's assigned to your landscape actor)
Look for the LandscapeGrassOutput node
See which layers feed into it — those are the ones with grass
Any layer that does not feed into LandscapeGrassOutput is safe to paint — rock, dirt, gravel, road, etc. are usually bare
Paint the Area Clear

Back in Landscape Paint mode, select the bare layer from the Target Layers list (click it to highlight it)
Set your brush size large enough to cover the volume area you're clearing
Set Tool Strength to 1.0 so it fully overrides the grass layer
Click and drag over the area — you're essentially overwriting the grass-spawning layer with a non-grass layer
The Landscape Grass instances will regenerate in real time as you paint — you should see the ground cover disappear immediately as you paint over it
If You Don't Have a Bare Layer
If every layer in your material feeds into LandscapeGrassOutput, you have a couple of options:

Edit the material — duplicate an existing layer entry and remove its connection to the GrassOutput node, then save and reassign it
Temporarily set Grass Density to 0 — open the LandscapeGrassType asset itself (find it in the Content Browser), and set the Grass Density property to 0 for the varieties you want gone. This is a global change though, not localized.
Use a Landscape Layer Info Object with a blank/null grass reference and paint that in your target area
Show more

Recognized technique validity while acknowledging missing context and seeking clarification
Recognized technique validity while acknowledging missing context and seeking clarification
Quick honesty check before anything else: I don't have that May conversation in front of me — my memory of past sessions is drawn from stored summaries, and nothing about a foliage removal issue shows up in what I have on file for this project. So I can't confirm I personally recall it, but I can absolutely evaluate what you've pasted on its own merits, and it's worth doing that regardless of where it came from.

The technique itself checks out — this is accurate, standard Unreal workflow. Landscape grass instances are driven by LandscapeGrassType assets wired into a LandscapeGrassOutput node inside the landscape material, tied to specific paint layers. Painting a competing layer that has no grass type connected, at full strength, directly overrides the grass layer's weight at that spot — so the foliage instances disappear in real time as you paint, exactly as described. The fallback options (duplicate a layer and strip its grass connection; zero out density on the LandscapeGrassType asset itself, understanding that's global; or use a blank Layer Info Object) are all valid, correctly distinguished approaches too. Nothing to correct here.

This is genuinely useful right now, not just archived advice — it directly solves something we just designed. The switchback trail we talked about, cut into Mountain's cliff face, almost certainly needs bare dirt/rock under the player's feet rather than Brushify's Auto-Material grass scattered across it — a grassy hiking trail reads oddly, and dense grass underfoot can also visually clutter a narrow path. This is exactly the tool for that: paint whichever of Brushify's layers has no grass type (likely something like its Rock or bare-ground layer — worth confirming directly in the material per your own step 2–4, since I don't have Brushify's complete layer list confirmed) along the trail path, and you get a clean, walkable dirt trail cutting through the otherwise grass-covered terrain.

So — want to apply this concretely to the trail now? If you tell me what layers Brushify's Auto-Material actually exposes (from opening it and checking the LandscapeGrassOutput node, per your own step 2), I can help you pick the right bare layer and think through brush size/strength for a natural-looking trail edge rather than a hard-edged stripe. Or if this was really just a "make sure this still holds up" check, that's confirmed — the technique's sound.

How do I create a hole of just the right size for a basement in a house so I don’t have floating vegetation in the basement?



Masking terrain visibility using landscape tools strategically
Masking terrain visibility using landscape tools strategically
Let me verify the exact current steps for this — Unreal has a specific, purpose-built tool for cutting holes in landscape, and I want to give you the precise current terminology and setup rather than recite from memory.



Searched the web
Searched the web
This confirms the exact mechanism, and there's a genuinely important current-best-practice detail (Epic explicitly recommends against the older approach). Here's the accurate, current workflow — and importantly, this solves both your problems (the hole shape and the floating-vegetation issue) at once, since a true hole removes the landscape surface entirely, and Landscape Grass only spawns where the landscape surface actually exists.

The two setup methods — use the newer one, not the older one
Epic's current documentation is explicit: there are two ways to enable holes, and they now recommend one over the other:

Preferred (current): the Landscape Visibility Mask node. In your landscape material, add a LandscapeVisibilityMask node and wire its output into the material's Opacity Mask slot, with the material's Blend Mode set to Masked. This node exists specifically to punch holes in the landscape.

Legacy (Epic now recommends against it): the separate "Landscape Hole Material" slot. This older method — assigning a distinct hole material in the Landscape actor's Details panel — still works, but Epic's own docs now state it's "legacy... more costly to render" and recommend switching to the Visibility Mask node approach instead. If you find older tutorials describing the Hole Material slot, that's the outdated path — worth knowing so you don't build around something Epic is steering people away from.

The actual steps
Set up your landscape material correctly first (one-time setup, not per-hole): add the LandscapeVisibilityMask node, connect it to Opacity Mask, and set the material's Blend Mode to Masked.
Select your Landscape actor, and in Sculpt mode, choose the Visibility tool.
Adjust your brush size to match the basement footprint — for a precise rectangular hole matching a house's foundation exactly, a large soft round brush isn't ideal; you'd want the brush shape/size to closely match the basement outline, working in Manage/Sculpt mode with a size close to your foundation dimensions. (For an exact rectangular cut, some people also union a separate technique — masking via a spline or a precise brush stamp — but the round brush at the right size, built up carefully, is the standard starting approach.)
Left-click and drag over the basement footprint to punch the hole; Shift+Left-click to erase/unmask if you overshoot.
Place your house (with its basement geometry as a separate static mesh or BSP interior) into the resulting hole.
Why this specifically fixes your floating-vegetation problem
This is the key mechanical point: Landscape Grass (the auto-scattered foliage system Brushify and similar packages use) only spawns on landscape surface that actually exists. A true hole, made via the Visibility Mask, genuinely removes that section of landscape geometry — not just its texture, but the surface itself. Since there's no landscape surface left under the house/basement, there's nothing there for grass instances to spawn onto — no floating foliage, because there's no ground for the system to think it should be growing on.

This is a meaningfully different (and better) fix than the foliage-removal technique from your earlier message. That approach — painting a no-grass-type layer over an area — stops grass from spawning while leaving the solid landscape surface intact underneath. That's the right tool if you just want bare dirt you can still walk on. But for a basement, you want the ground to not be there at all (so the interior space is actually open, and the house can have a below-ground floor) — which is exactly what the Visibility tool's true hole does, with the "no floating vegetation" benefit following automatically as a side effect of genuinely removing the surface.

One real limitation worth knowing, called out directly in the docs: "When painting masked out areas, you only get an on or off state — there is no way to have a transitional gradient from fully masked to unmasked." So the hole's edge is a hard cutoff, not a soft fade — which is fine for a basement (you want a clean foundation edge anyway), just worth knowing if you'd expected a gradual blend.

The vegetation Should be removed by making the LandscapeGrassOutput bare like it says above.  I then need typically a rectangular “hole” with the exact dimensions of the house to fit the house into.  How do i do that?



Reconciling vegetation suppression with precise rectangular cutout dimensions
Reconciling vegetation suppression with precise rectangular cutout dimensions
Let me verify the exact current brush options for this — getting a precise rectangular shape (rather than an eyeballed round brush) needs a specific brush type, and I want to confirm the current terminology rather than guess.



Searched the web
Searched the web
Confirmed — Unreal has a purpose-built brush type for exactly this, called Alpha (a close cousin, Pattern, works similarly). Here's precisely how to get a true, exact-dimension rectangle rather than an eyeballed round shape.

The tool: Alpha brush with a custom rectangle texture
Why a round brush can't do this: the standard Circle brush is fundamentally circular, with adjustable radius and falloff — no amount of resizing gives it a sharp 90° corner. To get a genuine rectangle, you need the Alpha brush, which uses a texture you supply as the shape stamp. Per Epic's own docs: "the Alpha brush... orients the brush texture in the direction of your painting and drags the shape as you move the cursor" — meaning it stamps your custom shape once, at the size and rotation you set, rather than tiling repeatedly (that tiling behavior is what the related Pattern brush does instead — you want Alpha, not Pattern, for a single precise stamp).

Step by step

1. Create your rectangle mask texture. In any image editor (Photoshop, GIMP, even Paint), make a simple grayscale image: pure white rectangle on a pure black background, sized to match your house footprint's aspect ratio exactly (e.g., if your basement is 20m × 12m, make the white rectangle's width:height ratio 20:12, i.e., 5:3). White = "hole fully applied here," black = "no effect." Save as a texture format Unreal can import (PNG works fine).
2. Import it into Unreal via the Content Browser as a regular Texture asset.
3. Switch to the Visibility tool (as before — landscape material already set up with the LandscapeVisibilityMask node, Sculpt mode, Visibility tool selected).
4. Change Brush Type to "Alpha" in the Landscape panel, and assign your imported rectangle texture as the Alpha brush's source texture.
5. Set the exact size. This is where you get precision: the Alpha brush's size settings let you scale the stamp numerically — since you control both the texture's exact aspect ratio and the brush's overall scale, you can dial in the precise real-world footprint dimensions rather than eyeballing it. Position the brush stamp precisely over where the house will sit (lining it up with a reference — e.g., a placeholder box the size of your house dropped into the level first, so you can see exactly where the corners need to land).
6. Click once to stamp it. Since Alpha "drags the shape as you move the cursor" rather than tiling, a single click-and-release at the right position and scale should apply your exact rectangle in one clean stamp, rather than needing to paint repeated strokes.

The practical workflow I'd actually recommend
Rather than eyeballing the stamp's position in the viewport, place your house's static mesh (or a simple placeholder box sized exactly to its footprint) into the level first, positioned exactly where it'll go. Then line up the Alpha brush stamp against that placeholder's visible edges before clicking — using the actual object as your alignment guide gives you a far more reliable exact match than trying to judge dimensions purely from the brush-size number fields.

One thing worth double-checking once it's done, given the hard on/off nature of the Visibility tool (no soft gradient, as we discussed): zoom in on all four corners after stamping to confirm the hole's edges land where you expect, since a slightly mis-scaled or mis-rotated alpha texture would show up as a corner that doesn't quite align with your house's actual foundation edge — better to catch that immediately than after the house mesh is dropped in and the mismatch is half-hidden.

Is there a way to automate the removal of vegetation and creating a hole to the exact outer dimensions of a house?



Weighing automation complexity against manual workflow practicality
Weighing automation complexity against manual workflow practicality
Yes, it's genuinely automatable — but let me verify the specific API before I describe a workflow, since "automate landscape editing" spans a range of real capabilities and I want to point you at the actual current mechanism rather than something outdated or partially right.



Evaluated Blueprint Brush automation versus manual approaches for house footprints
Evaluated Blueprint Brush automation versus manual approaches for house footprints
Yes — and there's a genuinely elegant, currently-supported way to do it that goes beyond a one-time script: build a custom Landscape Blueprint Brush that ties directly to each house actor and stays live.

Why this is the right mechanism, not just a mechanism
Epic's own current documentation on Edit Layers explicitly describes the system as designed to "integrate specialized deformation systems such as Landscape Patches and Splines" through Blueprint Brushes sitting in the layer stack — this is precisely the built-in extension point for exactly your use case. And since you already need the Landmass plugin enabled for Brushify's SmartBrush anyway, there's no new dependency to add.

What makes this better than a one-off automation script: a Landscape Blueprint Brush is part of the non-destructive Edit Layer stack — it recomputes automatically whenever its inputs change. So if you build the brush correctly, it doesn't just cut a hole once; it stays permanently tied to the house. Move the house, resize it, the hole and cleared-vegetation zone follow automatically. That's a meaningfully different (and better) result than "run a script once."

What you'd actually build
A custom Blueprint (or C++) actor deriving from Unreal's Landscape Blueprint Brush base class, that:

Holds a reference to a house actor (or, if you want it to handle every house automatically, iterates all actors of a "House" class/tag in the level).
Reads that house's footprint dimensions — I'd recommend a dedicated property on your house Blueprint (e.g., FoundationExtent) rather than raw mesh bounds, since a mesh's visual bounding box often includes roof overhangs or eaves that extend past the actual foundation — you want the foundation's exact footprint, not the whole building's silhouette.
Writes a rectangular shape into two places on execution: the assigned edit layer's Visibility data (creating the hole — Epic's docs confirm "Clear Visibility Layer" and visibility editing are explicit, supported Sculpt-mode operations this system exposes) and the bare/no-grass Target Layer's weightmap over that same footprint (handling the vegetation removal in the same pass).
Since it's a proper Blueprint Brush, this recalculates live — place one brush actor per house (or one smart brush that handles all of them), and every house's hole-plus-clearing stays correct automatically as you iterate on your level layout.
The honest cost/benefit call
Building this custom brush is real, contained coding work — not a checkbox to flip. It's a genuinely good, well-scoped Claude Code task (a Blueprint/C++ class following a documented base-class pattern, testable in isolation), but it's still effort against your MVP clock.

So the actual decision, using the same lens your own SCOPE.md already applies to everything else: how many houses/buildings does the MVP actually need this for? If it's a handful — say, one per community, five or six total — the manual Alpha-brush-plus-bare-layer-paint approach from a few messages ago is probably faster to just do than to build, test, and debug a custom automation tool for. If City or another community needs many building placements (a real settlement with dozens of structures), the automation pays for itself quickly and is worth the investment.

How many house/building placements are you actually expecting to need this for in the MVP? That tells us whether this is a "build the tool" situation or a "just do it by hand five times" situation.

