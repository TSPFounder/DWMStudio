# Patches for sibling repositories

These apply to repositories this session cannot push to. `CAD_Library` and
`FusionLibrary` are not in the session's authorized repository set, and the git
proxy will not mint a credential for them, so changes to those repos are
delivered as patches instead of commits.

They live here rather than being sent as file attachments because attachments
have to be downloaded to a known path first, and that step failed twice on
2026-08-06 in a way that looked like the patch had been applied when it had
not. A path inside a repo that is already cloned cannot go missing.

## Applying

Repositories are assumed to be checked out side by side under a common folder:

    Repos/CAD_Library/
    Repos/FusionLibrary/
    Repos/DWMStudio/          <- this repo

From `Repos/`:

    cd CAD_Library
    git checkout -b claude/economy-domain-repository-1ugmlv
    git am ../DWMStudio/tools/patches/0001-Add-revolve-circle-and-polyline-to-the-operation-IR.patch

    cd ../FusionLibrary
    git checkout -b claude/economy-domain-repository-1ugmlv
    git am ../DWMStudio/tools/patches/0001-Emit-revolve-and-the-new-sketch-ops-and-fix-two-trap.patch

`git am` fails loudly if a patch does not apply, and prints nothing on success.
Confirm with `git log --oneline -1` in each repo rather than assuming.

## What they contain

**CAD_Library** adds `RevolveOp`, `SketchCircleOp` and `SketchPolylineOp` to the
operation IR, plus an explicit `ProfileIndex` on the operations that consume a
sketch profile. Without it, DWMStudio.Tests and DWMStudio.WorldPackageCli fail
with CS0246 on the new operation names.

**FusionLibrary** emits those operations as Fusion Python, and fixes two traps in
the generated wrapper: a modal `ui.messageBox` in the failure path, which holds
the one thread the add-in needs to answer anything, and `design`/`root` never
being defined for a sequence that runs against an already open document.

## Not included

The `CAD_Library.csproj` and `FusionLibrary.csproj` changes were delivered as
whole replacement files and are already applied. `CAD_Library/CAD_Library/lib/`
holds three assemblies that cannot be rebuilt from source because
CAD_Library, SystemsEngineeringLibrary and ApplicationLibrary reference each
other in a cycle. Committing that folder to CAD_Library is still outstanding.
