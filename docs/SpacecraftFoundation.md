# Spacecraft Simulator Foundation — audit and phase log

Date: 2026-11-05. Project: `D:/CodeGame/SpaceK`. Base commit: `8f66bf1`. Branch: `codex/spacecraft-foundation`.

## Phase 1 — audit
- Actual Editor **6000.3.28f1** (not 6000.2 in the brief), URP **17.3.0**, Input System **1.20.1**, activeInputHandler **1**, uGUI **2.0.0**, Test Framework **1.6.0**, Navigation **2.0.16**, Pipeline **0.10.0-exp.1**.
- Only SampleScene, URP settings, template InputSystem_Actions and tutorial scripts exist. No player, inventory, interaction, crafting or building implementation.
- PC/Mobile quality profiles already reference URP assets. GraphicsSettings default pipeline is empty but quality overrides select URP; do not replace configuration.
- Pre-existing modifications: deletion of Assets/Editor/HubForceResolve.cs and metadata; changes to volume, PC/Mobile RP assets, build settings, player/quality/shader settings; untracked package and scene-template settings. Preserved.
- Editor was no longer running. Hub's Store-packaged CLI executable could not run (access denied). Installed Unity's official CLI **1.0.0-beta.16** from `https://unity.com/install.ps1`, whose installer verifies SHA-256; registered the existing Editor executable and opened the existing project. No new Editor/project downloaded.

## Phase 2 — package decisions
| Package | Source / license / cost | Compatibility and decision |
|---|---|---|
| Input System 1.20.1 | Unity Registry, Unity package license, no separate purchase | Already installed; reuse. |
| URP 17.3.0, uGUI 2.0.0, Test Framework 1.6.0 | Unity official/bundled, Unity package licenses, no separate purchase | Already installed; preserve. |
| Cinemachine 3.1 | [Unity documentation](https://docs.unity3d.com/Packages/com.unity.cinemachine@3.1/manual/index.html), Unity Registry, Unity Companion License, no separate purchase | Use released compatible version from UPM; avoid previews. Owns camera blending/collision. |
| Old Starter Assets FirstPerson URP / combined URP | [FirstPerson](https://assetstore.unity.com/packages/essentials/starter-assets-firstperson-urp-196525), [combined](https://assetstore.unity.com/packages/essentials/starter-assets-character-controllers-urp-267961) | Both official pages report deprecated; do not import. |
| First Person + Third Person Character Controllers 2.0.2 | [Unity Technologies Asset Store](https://assetstore.unity.com/packages/3d/characters/first-person-third-person-character-controllers-196526), FREE, **Non standard EULA** | Page lists URP / Unity 6000.3 compatibility. Candidate for a future rig/model; not imported, license/account acquisition not assumed. One shared custom motor meets requested ownership without duplicated controllers. |
| ProBuilder | [Unity manual](https://docs.unity3d.com/Packages/com.unity.probuilder@6.0/manual/index.html), Unity Registry, Unity package license, no separate purchase | Unity 6 support; defer because primitives suffice and no mesh-editing workflow is needed yet. |
| AI Navigation 2.0.16 | Unity Registry, Unity package license, no separate purchase | Already installed; retain, no NPC requirement so no NavMesh setup. |
| Animation Rigging | Unity Registry, Unity package license, no separate purchase | Defer: no rigged character/model. Optional Animator adapter will be provided. |
| Interaction/inventory/crafting frameworks | Assessed against required feature size | No third-party package needed for this small foundation. Implement focused modules, no added license or overlapping framework. |

No paid package selected. Free assets still have licenses. Package-local LICENSE.md is authoritative for the installed package; asset-store replacement's non-standard EULA must be reviewed before future import. Existing preview AI/Pipeline packages are pre-existing and are not upgraded/removed.

## Phase 3 — dependency installation
Cinemachine **3.1.7** installed through the live Pipeline `package add` command backed by Unity PackageManager.Client. Registry search and completed operation confirmed exact version and direct dependency. Dependencies resolved automatically (including Cinemachine dependencies recorded in packages-lock). No manifest hand editing. No other package added.

## Phase 4 — player and camera
Files: `Scripts/Player/{PlayerInputReader,PlayerMotor}.cs`, `Scripts/Camera/{PlayerCameraRig,CameraWallGuard}.cs`, `Settings/Spacecraft.inputactions`, `Prefabs/Player/Engineer.prefab`.
One shared CharacterController, private action asset instance, keyboard/mouse actions and initial gamepad bindings. FP/TP blends, free orbit, sprint/walk/jump/crouch, headroom check, optional Animator adapter. Cinemachine third-person collision plus a final blended-camera wall guard. Body is a capsule placeholder; no humanoid animation controller without a suitable model. Inspector exposes movement/camera tuning. New Input System and URP preserved.

## Phase 5 — interaction
Files: `Scripts/Interaction/{IInteractable,PlayerInteractor,DoorInteractable,PickupInteractable,TerminalInteractable}.cs`, `Scripts/Crafting/CraftingStation.cs`, corresponding `Prefabs/Interactables` assets.
Camera raycast respects occluding solids and player reach in either camera mode. Press-E prompt, parent interface lookup, sliding airlock, atomic pickup, workbench and terminal. Independent review identified third-person ray length; corrected by extending camera ray while enforcing player-to-hit distance.

## Phase 6 — gameplay data, building and UI
Files: `Scripts/Inventory/*`, `Scripts/Crafting/*`, `Scripts/Building/*`, `Scripts/UI/SimulatorHUD.cs`, runtime/editor/test asmdefs, `ScriptableObjects/{Items,Recipes,Modules}`, `Materials`, `Art` icons, module/UI prefabs.
Inventory handles IDs, icons, descriptions, stacks, slots, add/remove/count and atomic exchanges. Timed crafting commits at completion; cancellation retains ingredients. Materials are deliberately **not reserved**: changes during a job are revalidated at completion, failure costs nothing; one job per station. No persistence yet.
Building provides renderer-only ghost, grid, quarter-turn rotation, selection, overlap and full rotated-footprint support checks, cost consumption and a ship assembly parent. Module attachments now snap stacks to their support centre and fins to hull sides; there is no ship flight or structural simulation yet. Ghosts never run prefab scripts. Navy/cyan uGUI HUD uses TMP, CanvasScaler, masked scroll lists, inventory/crafting/terminal panels, progress and camera/build indicators.

## Phase 7 — scene and validation (completed; known limits documented)
Created `Scenes/SpacecraftSimulator_Test.unity` using `Editor/FoundationSceneBuilder.cs`; saved scene and prefabs through Editor APIs, appended build entry without removing SampleScene. Generator refuses to overwrite an existing scene or discard dirty scenes.
Initial contract tests produced 3 expected missing-type failures before implementation. Final results: **21/21 EditMode** (`Logs/foundation-all-tests.json`) and **10/10 PlayMode** (`Logs/foundation-playmode-final.json`). Mode=All starts PlayMode asynchronously; the separate PlayMode report confirms those ten results. Compilation status reported no errors. Tests exercise actual keyboard events, player identity during FP/TP switching, collision, crouch clearance, modal suppression, interaction, timed recipe buttons, placement costs/overlap and serialized references. Live frame advancement was independently observed. No standalone build was tested.

Visually checked screenshots: `Logs/foundation-hud-1600.png`, `Logs/foundation-inventory-1600.png`, `Logs/foundation-crafting-1600.png`. Independent review prompted fixes for third-person interaction reach and rotated module overhang.

Native rendering crashes reproduced on D3D12 and D3D11 at `ScriptableBatchRenderer::BindBuiltInTexturesForBatchRendererGroup`, preceded by FallbackLoading/DOTS shader errors. `Scripts/Rendering/DemoRendererCompatibility.cs` uses a local MaterialPropertyBlock on demo renderers to select conventional rendering without changing existing URP assets. Physics-only test obstacles have rendering disabled. These mitigate the crash; its engine root cause is not established. Existing cloud/entitlement/network messages are independent of gameplay.

**Editor handoff updated 2026-11-06:** The previous automatic-approval usage limit is no longer blocking commands. During FBX inspection, reopened the existing project, confirmed Play Mode off and scene not dirty, restored `EditorSettings.asyncShaderCompilation=true`, saved Project settings and verified the file contains 1. Run In Background is restored to its original value without an override. Fresh Console inspection after FBX import/render: 0 errors, 1 warning from Unity AI Account API timing out after 30 seconds; compilationFailed=false, compiling=false. The optional third-person building screenshot was not captured; camera/build functionality is covered by the passing PlayMode suite. Existing native-rendering mitigation remains in place; no standalone build/performance certification is claimed.

## Quick start
Open `Assets/_Project/Scenes/SpacecraftSimulator_Test.unity`, enter Play Mode and click the Game view. Stop any existing QA play session first to restart at the saved spawn. Escape closes the current panel/build mode first, otherwise releases/locks the cursor.

| Control | Action |
|---|---|
| WASD / mouse | Move / look |
| Space / Left Shift / Ctrl | Jump / sprint / crouch |
| V / E | Switch FP/TP / interact |
| I or Tab / C | Inventory / crafting near a workbench |
| B / Q / R | Building mode / next module / rotate |
| F (radial fins) | Select the next hull mounting side; R reverses the fin at its current connector |
| Left / right mouse | Place / cancel |
| X / Z (in B mode) | Remove aimed module / undo last installation, refund item |

Extend ItemData, CraftingRecipe and BuildModuleDefinition assets for content. Replace meshes while preserving component references. Configure Mount, AcceptsSideAttachments and SupportsStacking for attachment rules. Structural simulation, saving/loading, flight and humanoid rigging remain future work. Main is first in build settings and loads SpacecraftSimulator_Test; SampleScene is retained but disabled.

## Visual reference — 2026-11-06
The supplied image establishes realistic industrial art direction: modular rocket parts, worn painted metal, workshop props, daylight through a hangar opening, first-person hands and navy UI. It does not establish runtime performance. Keep URP. Recommended workflow: Blender for precise modular parts/pivots, export FBX and textures into Unity. Optional AI props require topology, UV, scale, collider and license checks. No Blender/Tripo software or plugin was installed for this recommendation.

The user subsequently supplied a Tripo FBX, imported and visually inspected in Unity without Blender. See `docs/TripoShipReview.md`: multiple rocket forms combined in one mesh, 9,393 triangles, UVs present, textures absent. Imported under `Art/Spacecraft/TripoShip`; not substituted into the gameplay scene.

## Workbench art replacement — 2026-11-06
At the user's request, replaced the demo Workbench prefab visuals with their textured GLB workbench, converted to native Unity assets without another package. Optimized 1,948,936 to 60,000 triangles, retained CraftingStation/3 recipes and prefab references, added simple colliders, and kept a backup placeholder prefab. 4 scene/reference tests and 2 workbench PlayMode tests pass; Game view screenshot confirms the new art and E prompt. See `docs/WorkbenchIntegration.md` for files, checks and limits.

## Ship module art replacement — 2026-11-06
Six supplied Tripo models were converted into native Unity meshes/materials with three LODs. The corrected Hull Module_1 file is used. Hull and engine placeholders, cargo props and pickups now use the supplied art; nose/fin/tank join crafting and building. Existing cockpit remains available. See docs/ShipModuleIntegration.md for exact triangle counts, files, validation and limits.

## Hangar surface and terminal replacement — 2026-11-06
Imported Wall Panel, Floor Tile and Inspection Terminal with 3 LODs and URP PBR. Replaced hangar surfaces using shared combined mesh chunks and preserved the collision shell, airlock and terminal interactions. See docs/HangarModelIntegration.md for assets, triangle budgets, layout and validation.
