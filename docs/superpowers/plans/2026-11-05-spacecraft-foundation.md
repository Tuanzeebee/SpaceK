# Spacecraft Foundation Implementation Plan

> Execute inline using superpowers:executing-plans; user already requested direct implementation in this connected Unity project.

**Goal:** Deliver a runnable hangar demo with one FP/TP player, interaction, inventory, timed crafting and modular placement.

**Architecture:** Data ScriptableObjects and focused runtime components connected through serialized references and local events. Editor-only generator creates scene/prefabs/data through Unity APIs.

**Tech Stack:** Existing Unity 6000.3.28f1 / URP 17.3.0 / Input System 1.20.1 / uGUI; add verified Cinemachine 3.1 release through UPM.

**Spec:** docs/superpowers/specs/2026-11-05-spacecraft-foundation-design.md

## Global Constraints
Preserve existing configuration changes, SampleScene, URP and input settings. Do not install duplicate controllers, paid assets or packages without a concrete use. Use the actual installed Editor. No project recreation.

## Review Focus
- Inventory capacity failures must not destroy pickups or consume partial material sets.
- Modal UI must suppress camera, movement and world interaction; Escape has predictable precedence.
- Camera self-collision, wall clipping and stand-up under low ceilings.
- Placement must reject player/walls/occupied cells and leave inventory unchanged on failure.
- Scene/prefab fields and all generated assets must survive reload.

## Tasks and progress
- [x] 1. Audit and verify official package metadata/license; install Cinemachine through Client API.
- [x] 2. Inventory/Crafting: write tests, demonstrate failure, implement ItemData, InventoryModel, InventoryComponent, CraftingRecipe, CraftingJob, CraftingStation.
- [x] 3. Player/Camera: InputReader, PlayerMotor, PlayerCameraRig, shared gameplay modal state and .inputactions; integration checks for movement and identity-preserving switch.
- [x] 4. Interaction: IInteractable, PlayerInteractor, Door/Pickup/Workbench/Terminal; verify occlusion/range and full inventory behaviour.
- [x] 5. Building: BuildModuleDefinition, grid rules, placement ghost/collision/cost and assembly root; test invalid placement and grid rotation.
- [x] 6. uGUI HUD and panels, Editor scene/prefab/data generator, hangar. Persist scene via Editor API and append build entry.
- [x] 7. Run full tests, verify live advancing gameplay and Console, inspect screenshots, independent review, fixes and user guide/phase report.

Task 7: 21 EditMode and 10 PlayMode tests passed; live frames, three screenshots and independent review completed. On 2026-11-06 approval service was available again: restored and saved original Editor shader/background settings, confirmed Play Mode off and scene clean, inspected Console (0 errors; 1 Unity AI account-network warning). Guide and model review updated. Native renderer mitigation and untested standalone build remain documented limitations; see docs/SpacecraftFoundation.md.

## Interfaces
InventoryModel: Count(ItemData), TryAdd(ItemData,int), TryRemove(ItemData,int), TryExchange(requirements, outputs), Changed event. CraftingJob: Start(recipe,inventory), Tick(seconds), Cancel(), Progress/IsRunning. InputReader exposes Move/Look/actions and GameplayBlocked. Interactor passes an InteractionContext with player inventory and UI to IInteractable. Building consumes ItemData from the same inventory.

## Verification
Use unity command test runner commands as discovered from the live catalog. Use edit/play tests for actual runtime types; no mocked inventory. Before completion, inspect Console and serialized scene references and capture screen-source Game view. Save test outputs under Logs and summarize exact outcomes in docs/SpacecraftFoundation.md.
