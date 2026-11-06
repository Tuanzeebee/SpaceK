# Assembly placement and Earth sky

Requested behavior: attach fins to the hull, build higher, remove mistakes, and use a terrestrial blue/cloudy sky while keeping URP and existing assets.

The current raycast accepts only upward normals and the horizontal grid moves small modules away from their parent. SocketStep and LocalSockets were metadata, not working attachment code. Reach is 9 metres. There is no removal input.

Use the existing ground-grid placement for free-standing parts. When hitting a placed module, align stackable parts to its top centre, even when aiming at its side. Fins attach radially to hull side faces, with automatic outward orientation and R selecting the next face. Record the support relationship. Increase configurable build reach to 18 metres; colliders and line of sight still apply.

In build mode X removes the aimed module and Z undoes the most recent remaining placement. Refund its paid item atomically, reject full inventory, and require dependent parts to be removed first. Modal UI continues to block building.

Sky options researched: AllSky Free supports Unity 6/URP under the Asset Store EULA; not cached locally and requires an authenticated Asset Store acquisition. Use a directly downloadable CC0 Poly Haven pure-sky HDRI with Unity's Skybox/Panoramic shader instead. No executable, plugin or render pipeline change is needed. Preserve hangar colliders and lighting settings except the sky material and modest reflection tuning if visually necessary.

Implementation/checklist:

- Add PlayMode regressions for radial fins, inaccessible upper surfaces, keyboard undo and safe full-inventory removal. Run and confirm failures before changing runtime code.
- Extend module data and placement math, track attachments, add removal/undo and Input System bindings, update HUD and builder defaults. Configure hull/fin assets and player reach through the Editor.
- Import verified HDRI, retain source/license details, assign sky in the demo and preserve Main build entry.
- Run EditMode and PlayMode suites, inspect an assembled rocket/sky in the Game view, confirm advancing frames and Console, save scenes and leave Play Mode off.

Limits: attachment graph is static assembly, not a structural or flight simulation. Undo/removal applies to runtime-built modules in this assembly, not decorative models or environment art. No saved-game persistence is introduced.

## Validation, 2026-11-06

- Before implementation: 12 existing PlayMode cases passed; all 4 new cases failed on the expected missing behavior.
- Extended fin case reproduced a fitted front fin obscuring the remaining connectors; fixed by resolving that fin's hull support when selecting other radial mounting sides.
- Final suite: 30/30 EditMode, 16/16 PlayMode. Saved PlayMode details: AssemblyPlayModeResults.json.
- Fin case fits all four sides with F, verifies matching mounting heights, blocks hull removal while fins remain, then undoes all four and refunds each item once. R reverses the fin at the same connector.
- Upper module case is beyond the old 9-metre reach and is attached through its side, with no view onto its top.
- X removal refuses a full refund inventory without destroying the module. Z repeated undo does not duplicate items. Existing movement, cameras, interaction, crafting, ghost LOD, ground placement and overlap cases still pass.
- In a fresh normal Play session, installed an engine, two hulls, nose and four fins through BuildingController.TryPlace. Confirmed normal camera Update, frames 22387 to 22412 advancing, 1600x900 Game view, and Earth Sky active. Screenshot: Images/assembly-earth-sky.png.
- Fresh Console: no errors or compile failures; recurring Unity AI Account API network warning remains unrelated to building.
- No new UPM/Asset Store package, Blender or executable installed. HDRI source/license/hash are stored in Assets/_Project/Art/Sky/SOURCE-LICENSE.txt. Main remains first in the build list.

Usage: B enters assembly mode; Q selects a part; aim at the hull side for fins, R reverses the fin in place, F chooses another mounting side, LMB installs. Aim at the highest installed module to extend a stack without needing to see its top. X removes the aimed module; Z undoes the latest remaining installation. Remove dependent parts before the support; free cargo space if a refund cannot fit.

## Fin orientation correction

The original fin mesh has its long 0.9-metre root edge at local +Z and a 0.21-metre tip edge at -Z. The placement contract expected the root at -Z. Rotated the prefab Visuals 180 degrees about Y for all three LODs while preserving its root, collider, GUID, dimensions and materials. The long edge now meets the hull and the small end faces outwards by default.

Separated rotation from connector selection: R reverses a radial fin 180 degrees at the same connector; F cycles the four hull sides. R remains 90-degree rotation for free-standing and stacked modules. Both keys reset when selecting a different part or restarting placement. Added a mesh-shape regression across all LODs and keyboard checks for fixed-position rotation, two-turn restoration and four-side installation.

Reproduced both regressions before the fix: root-edge span was 0.21 m instead of over 0.7 m; R moved the preview 1.30 m to another connector. After correction, 31/31 EditMode and 16/16 PlayMode passed. Fresh normal Play verification again assembled all eight parts; frames 7578 to 7603 advanced, normal camera enabled, Console had 0 errors and the existing Unity AI Account warning. Close view of corrected long-root/short-tip alignment: Images/fin-orientation-fixed.png.
