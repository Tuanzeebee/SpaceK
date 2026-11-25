# Workshop art pass two — 2026-11-06

Build on the saved foundation (24941b1, input preload follow-up 3b6d2f2), Unity 6000.3.28f1, URP 17.3. Retain Main startup, existing gameplay and user Tripo art. No paid purchases, Blender install or replacement controller.

Art direction follows the user's industrial rocket workshop reference: dark navy/charcoal gloves and equipment, amber fabric/safety accents, worn alloy structures, clear daylight entering a large hangar opening and warm work lights. Existing navy/cyan HUD remains. Palette: shadow #111B24, steel #3E4C57, paint #263D51, fabric #B95C22, pale alloy #B8C3CB, signal amber #E8A637, cyan #3BC4D3.

Use a verified CC0 rigged FPS arms source (WRAD ARMS by wriks, official public GitHub source and license), restyled as work gloves/sleeves with PBR materials. This is an original low-poly source, not a photoreal scanned hand. Create editable native Generic Animator clips for hold/place/rotate, pose fingers around the driver and held part, and connect animation to actual successful gameplay events. A separate first-person rendering layer/camera hides the arms in third person and menus; it never changes the one shared player motor or interaction ray.

Choose an electric fastening driver. Model its housing, grip, battery, chuck, bit, vents and status light; spin the bit during successful installation. Show the selected component held in the left hand while building and let place/rotation clips move the gloved rig. The tool is presentation for the current instant-install building rules; do not introduce a second cost, welding minigame or asynchronous construction transaction.

Create reusable cabinet, wheeled trolley and rack prefabs, populate the workbench area with existing module/crate models, and author bolt heads, piping/cable runs, structural I-beams, roof panels/skylights, industrial lamps and a large sliding hangar entrance. Compound authored geometry should have manufactured edges and a consistent finish, not plain template cubes. Combine static details by material; keep simple bounds colliders only where needed. Keep assembly bay and interaction approaches clear. Create an exterior apron behind the new port-side opening so the entrance is physically traversable.

Implementation plan:

1. Import and inspect the pinned arms FBX/rig, verify bone weights and license. Add gameplay presentation events with regression tests proving successful placement/removal/rotation and modal safety.
2. In parallel, author Editor geometry/environment builders in separate files; no concurrent Editor mutations. Parent integrates and checks prefab references, doors, clearances and lighting.
3. Author the first-person rig/tool/Animator/held-part renderer and attach it to Engineer prefab/scene. Retain existing camera collision behavior and Input System controls.
4. Run EditMode/PlayMode suites and new presentation checks. Capture normal gameplay, gloved/tool close-up and an active install/rotate frame with advancing game frames. Review visual consistency, lighting and hand silhouettes; tune at least once. Save all assets/scenes, restore background settings and stop Play.

Validation also covers existing interaction/crafting/building and Main build order. Report actual source licenses, packages, visual limitations and any remaining risks. No standalone performance certification is implied by Editor tests.
