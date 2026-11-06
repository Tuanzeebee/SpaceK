# Spacecraft Simulator Foundation

## Intent and constraints
Implement the user's seven phases in the existing SpaceK checkout. The actual project uses Unity 6000.3.28f1, URP 17.3.0, Input System 1.20.1, uGUI 2.0.0 and Test Framework 1.6.0. Preserve pre-existing modifications and SampleScene. No new project, renderer migration, paid assets or unverified binary downloads.

## Approaches
Use the existing Input System with one CharacterController motor and Cinemachine 3 cameras. Importing two separate Starter Asset controllers adds duplicate movement/input ownership. Importing the replacement Asset Store controller requires its non-standard license and account access; defer model/animation assets until that is selected. Hand-writing a full camera stack would duplicate Cinemachine; use its blending and third-person collision instead. Use primitive placeholders rather than installing ProBuilder for a single greybox.

## Design decisions
- Modules under Assets/_Project: Player, Camera, Interaction, Inventory, Crafting, Building, UI, editor tooling and tests. A runtime asmdef plus editor and test asmdefs keep editor code out of builds.
- InputActionAsset with keyboard/mouse and gamepad bindings. Input reader owns a private instance. Shared modal state suppresses world actions while inventory/crafting is open. Escape closes modal/building before releasing the cursor.
- One CharacterController and player identity across V switches. Configurable movement, gravity, jump, crouch with overhead clearance and optional Animator parameters. Mouse orbit independent of body in third person. Cinemachine blends between first and third person; collision excludes player/ghosts.
- Camera raycast finds IInteractable through collider parents, blocked by nearer solid geometry. Door toggles, pickup transfers only when inventory can accept, workbench opens crafting within range, terminal displays hangar status.
- ItemData, CraftingRecipe and BuildModuleDefinition are ScriptableObjects. Inventory has bounded slots, stack splitting, ID-based search and atomic mutations. Duplicate recipe material IDs are cumulatively validated. Crafting validates ingredients/output capacity at start and commits both in one exchange after elapsed duration. Cancellation consumes nothing. If inventory changes before completion, the exchange is revalidated and a failed craft ends without partial consumption. One active job per station; ingredients are not reserved across stations.
- Placement selects module data, uses camera ray onto a floor/build surface, snaps to a grid, rotates by 90 degrees, checks bounds and overlap, consumes inventory only on valid placement. Ghost has no gameplay collider or behaviour. Assemblies parent to a ship root; attachment/load/flight simulation are future work.
- uGUI Canvas with responsive anchors/scaler, TextMeshPro, navy industrial palette, inventory/crafting buttons, station progress and placement feedback. Cursor unlocks for panels, movement/look stop, gravity continues.
- Editor generator creates persistent assets, reusable prefabs and SpacecraftSimulator_Test.unity without replacing existing content. Generated scene opens in Editor and is appended to build settings without removing existing entries.

## Validation
EditMode tests cover stacking/capacity/invalid quantities, duplicate materials, timed craft/cancel/refund and grid/rotation rules. PlayMode tests cover movement/jump/crouch, V identity preservation, blocking raycasts, interactables, modal input and placement. Verify advancing frames, scene/prefab references, URP, dependency resolution, compile errors and runtime Console. Capture the actual Game view and report limits honestly.

## Non-goals
Persistence, spacecraft flight/physics, networking, production art, humanoid animation without a rig, mobile touch controls and fully validated gamepad UX are outside this foundation.
