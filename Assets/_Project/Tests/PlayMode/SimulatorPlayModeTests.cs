using System.Collections;
using NUnit.Framework;
using SpaceK.Building;
using SpaceK.Crafting;
using SpaceK.Interaction;
using SpaceK.Inventory;
using SpaceK.Player;
using SpaceK.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SpaceK.Tests
{
    public class SimulatorPlayModeTests
    {
        Keyboard keyboard;
        PlayerMotor motor;
        SimulatorHUD hud;
        InputSettings.BackgroundBehavior previousBackground;
        InputSettings.EditorInputBehaviorInPlayMode previousEditorInput;
        float previousCaptureDelta;
        [UnitySetUp] public IEnumerator Setup()
        {
            previousBackground = InputSystem.settings.backgroundBehavior;
            previousEditorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
            previousCaptureDelta = Time.captureDeltaTime;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            Time.captureDeltaTime = 1f / 60;
            yield return SceneManager.LoadSceneAsync("SpacecraftSimulator_Test");
            keyboard = InputSystem.AddDevice<Keyboard>(); motor = Object.FindFirstObjectByType<PlayerMotor>(); hud = Object.FindFirstObjectByType<SimulatorHUD>();
            yield return null; yield return new WaitForSeconds(.2f); motor.Input.SetCursor(false);
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard);
            hud?.ClosePanels();
            InputSystem.settings.backgroundBehavior = previousBackground;
            InputSystem.settings.editorInputBehaviorInPlayMode = previousEditorInput;
            Time.captureDeltaTime = previousCaptureDelta;
            yield return null;
        }
        void Keys(params Key[] keys) { InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys)); }
        void Teleport(Vector3 position)
        {
            var controller = motor.GetComponent<CharacterController>(); controller.enabled = false; motor.transform.position = position; controller.enabled = true; Physics.SyncTransforms();
        }
        IEnumerator Aim(Vector3 point)
        {
            motor.CameraRig.enabled = false;
            motor.CameraRig.AimTarget.position = motor.transform.position + Vector3.up * 1.65f;
            motor.CameraRig.AimTarget.rotation = Quaternion.LookRotation(point - motor.CameraRig.AimTarget.position);
            yield return null; yield return null; yield return null;
        }
        [UnityTest] public IEnumerator WasdAndVPreserveOnePlayerAndContinueMovement()
        {
            int identity = motor.GetInstanceID(); var start = motor.transform.position; Keys(Key.W); yield return new WaitForSeconds(.35f);
            Assert.That(motor.transform.position.z - start.z, Is.GreaterThan(.6f));
            Keys(Key.W, Key.V); yield return null; yield return null;
            Assert.That(motor.CameraRig.ThirdPerson, Is.True); Assert.That(motor.GetInstanceID(), Is.EqualTo(identity));
            var duringSwitch = motor.transform.position; Keys(Key.W); yield return new WaitForSeconds(.15f); Keys(); yield return null;
            Assert.That(motor.transform.position.z, Is.GreaterThan(duringSwitch.z));
            Assert.That(Object.FindObjectsByType<PlayerMotor>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
        }
        [UnityTest] public IEnumerator WorkshopHandsAnimateSuccessfulBuildAndRespectCameraAndMenus()
        {
            var presentation = System.Array.Find(motor.GetComponentsInChildren<MonoBehaviour>(true), p => p.GetType().Name == "WorkshopHandsPresenter");
            Assert.That(presentation, Is.Not.Null, "Engineer needs the rigged workshop hands presentation.");
            var type = presentation.GetType();
            var camera = (UnityEngine.Camera)type.GetField("HandsCamera").GetValue(presentation);
            var animator = (Animator)type.GetField("Animator").GetValue(presentation);
            var skin = presentation.GetComponentInChildren<SkinnedMeshRenderer>(true);
            Assert.That(skin.bones.Length, Is.GreaterThan(30)); Assert.That(animator.runtimeAnimatorController, Is.Not.Null);
            Assert.That(camera.enabled, Is.True);
            motor.CameraRig.ToggleMode(); yield return null; Assert.That(camera.enabled, Is.False);
            motor.CameraRig.ToggleMode(); yield return null; Assert.That(camera.enabled, Is.True);
            Teleport(new(5, .1f, -3)); yield return Aim(new(5, .05f, 1));
            hud.Building.Begin(); yield return null;
            var wrist = System.Array.Find(skin.bones, b => b.name == "wrist.r"); Vector3 restingWrist = wrist.position;
            Assert.That(hud.Building.TryPlace(), Is.True); yield return new WaitForSeconds(.2f);
            Assert.That(type.GetProperty("LastAction").GetValue(presentation).ToString(), Is.EqualTo("Install"));
            Assert.That(Vector3.Distance(wrist.position, restingWrist), Is.GreaterThan(.005f), "The rig must actually animate, not just set a state name.");
            int installed = (int)type.GetProperty("ActionRevision").GetValue(presentation);
            // Modal UI must neither render the viewmodel nor execute placement/rotation presentation.
            hud.OpenInventory(); yield return null; Assert.That(camera.enabled, Is.False);
            Keys(Key.R); yield return null; yield return null;
            Assert.That((int)type.GetProperty("ActionRevision").GetValue(presentation), Is.EqualTo(installed));
            Keys(); hud.ClosePanels(); yield return null; Assert.That(camera.enabled, Is.True);
            hud.Building.Begin(); Keys(Key.R); yield return null; yield return null;
            Assert.That(type.GetProperty("LastAction").GetValue(presentation).ToString(), Is.EqualTo("Rotate"));
            Assert.That((int)type.GetProperty("ActionRevision").GetValue(presentation), Is.GreaterThan(installed));
        }
        [UnityTest] public IEnumerator WorkshopEntranceIsTraversableAndDoorControlClosesItsPanel()
        {
            var door = Object.FindFirstObjectByType<WorkshopHangarDoor>(); Assert.That(door, Is.Not.Null);
            Assert.That(door.IsOpen, Is.True);
            Teleport(new(-14, .1f, -4)); motor.CameraRig.enabled = false;
            typeof(SpaceK.CameraSystem.PlayerCameraRig).GetProperty("Yaw").SetValue(motor.CameraRig, 270f);
            Keys(Key.W); yield return new WaitForSeconds(.9f); Keys(); yield return null;
            Assert.That(motor.transform.position.x, Is.LessThan(-16.5f), "The imported port wall must not leave an invisible collider across the doorway.");
            Assert.That(motor.transform.position.y, Is.GreaterThan(-.1f), "The exterior apron must support the player.");
            Teleport(door.transform.position + new Vector3(2, -1.2f, 0)); yield return Aim(door.GetComponent<BoxCollider>().bounds.center);
            Assert.That(motor.GetComponent<PlayerInteractor>().Current, Is.SameAs(door));
            Keys(Key.E); yield return null; Keys(); yield return new WaitForSeconds(3.2f);
            Assert.That(door.IsOpen, Is.False);
            Assert.That(Vector3.Distance(door.Panel.localPosition, door.ClosedLocalPosition), Is.LessThan(.03f));
            Assert.That(Physics.Raycast(new Vector3(-14, 1, -4), Vector3.left, out var hit, 4), Is.True);
            Assert.That(hit.collider.transform, Is.EqualTo(door.Panel));
        }
        [UnityTest] public IEnumerator JumpAndCrouchRespectGroundAndOverheadClearance()
        {
            Assert.That(motor.IsGrounded, Is.True); float y = motor.transform.position.y;
            Keys(Key.Space); yield return new WaitForSeconds(.1f); Assert.That(motor.transform.position.y, Is.GreaterThan(y + .1f));
            Keys(); yield return new WaitForSeconds(1);
            Keys(Key.LeftCtrl); yield return null; yield return null; Assert.That(motor.IsCrouched, Is.True);
            var roof = GameObject.CreatePrimitive(PrimitiveType.Cube); roof.GetComponent<Renderer>().enabled = false; roof.transform.position = motor.transform.position + Vector3.up * 1.55f; roof.transform.localScale = new(2, .2f, 2); Physics.SyncTransforms();
            Keys(); yield return null; yield return null; Assert.That(motor.IsCrouched, Is.True);
            Object.Destroy(roof); yield return null; yield return null; Assert.That(motor.IsCrouched, Is.False);
        }
        [UnityTest] public IEnumerator InventoryModalSuppressesWorldActionsAndEscapeClosesIt()
        {
            Keys(Key.I); yield return null; yield return null; Assert.That(hud.ModalOpen, Is.True);
            var position = motor.transform.position; float yaw = motor.CameraRig.Yaw;
            Keys(Key.W, Key.V, Key.B, Key.E); yield return new WaitForSeconds(.2f);
            Assert.That(Vector3.Distance(position, motor.transform.position), Is.LessThan(.04f)); Assert.That(motor.CameraRig.ThirdPerson, Is.False);
            Assert.That(hud.Building.IsPlacing, Is.False); Assert.That(motor.CameraRig.Yaw, Is.EqualTo(yaw));
            Keys(Key.Escape); yield return null; yield return null; Assert.That(hud.ModalOpen, Is.False); Assert.That(motor.Input.GameplayBlocked, Is.False);
        }
        [UnityTest] public IEnumerator CameraRaycastFindsPickupAndNearerWallBlocksIt()
        {
            var pickup = Object.FindFirstObjectByType<PickupInteractable>(); Teleport(pickup.transform.position + new Vector3(0, -.4f, -2));
            yield return Aim(pickup.transform.position);
            var interactor = motor.GetComponent<PlayerInteractor>(); Assert.That(interactor.Current, Is.SameAs(pickup));
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube); wall.GetComponent<Renderer>().enabled = false; wall.transform.position = Vector3.Lerp(interactor.View.transform.position, pickup.transform.position, .5f); wall.transform.localScale = new(2, 2, .1f); Physics.SyncTransforms();
            yield return null; Assert.That(interactor.Current, Is.Null); Object.Destroy(wall);
        }
        [UnityTest] public IEnumerator ThirdPersonInteractionUsesPlayerReachRatherThanCameraDistance()
        {
            var pickup = Object.FindFirstObjectByType<PickupInteractable>();
            Teleport(pickup.transform.position + new Vector3(0, -.4f, -2)); motor.CameraRig.ToggleMode();
            yield return Aim(pickup.transform.position); yield return new WaitForSeconds(.4f);
            Assert.That(motor.GetComponent<PlayerInteractor>().Current, Is.SameAs(pickup));
        }
        [UnityTest] public IEnumerator PickupDoorAndWorkbenchWorkThroughInteraction()
        {
            var pickup = Object.FindFirstObjectByType<PickupInteractable>(); int before = hud.Inventory.Model.Count(pickup.Content.Item); var item = pickup.Content.Item; int amount = pickup.Content.Amount;
            Teleport(pickup.transform.position + new Vector3(0, -.4f, -2)); yield return Aim(pickup.transform.position);
            Keys(Key.E); yield return null; yield return null; Assert.That(hud.Inventory.Model.Count(item), Is.EqualTo(before + amount)); Keys();
            var door = Object.FindFirstObjectByType<DoorInteractable>(); Teleport(new(0, .1f, 9.5f)); yield return Aim(door.Panel.position);
            Keys(Key.E); yield return null; yield return null; Assert.That(door.IsOpen, Is.True); Keys();
            var station = Object.FindFirstObjectByType<CraftingStation>(); Teleport(station.transform.position + new Vector3(0, .1f, -2)); yield return Aim(station.GetComponentInChildren<Collider>().bounds.center);
            Keys(Key.E); yield return null; yield return null; Assert.That(hud.CraftingPanel.activeSelf, Is.True);
        }
        [UnityTest] public IEnumerator FullCargoDoesNotDestroyPickupAndTerminalOpensPanel()
        {
            var pickup = Object.FindFirstObjectByType<PickupInteractable>(); var full = new InventoryModel(1); full.TryAdd(pickup.Content.Item, pickup.Content.Item.MaxStack);
            pickup.Interact(new InteractionContext(motor.transform, full, hud)); yield return null;
            Assert.That(pickup != null && pickup.gameObject.activeSelf, Is.True);
            var interaction = motor.GetComponent<PlayerInteractor>();
            var hands = motor.GetComponentInChildren<WorkshopHandsPresenter>(); int revision = hands.ActionRevision;
            var fullCargo = new GameObject("Full pickup cargo").AddComponent<InventoryComponent>();
            while (fullCargo.Model.TryAdd(pickup.Content.Item, pickup.Content.Item.MaxStack)) { }
            interaction.Inventory = fullCargo;
            Teleport(pickup.transform.position + new Vector3(0, -.4f, -2)); yield return Aim(pickup.transform.position);
            Keys(Key.E); yield return null; yield return null; Keys(); yield return null;
            Assert.That(hands.ActionRevision, Is.EqualTo(revision), "Failed pickup must not play a successful collection animation.");
            interaction.Inventory = hud.Inventory; Object.Destroy(fullCargo.gameObject);
            var terminal = Object.FindFirstObjectByType<TerminalInteractable>();
            Teleport(terminal.transform.position + new Vector3(0, .1f, -2)); yield return Aim(terminal.GetComponentInChildren<Collider>().bounds.center);
            Assert.That(motor.GetComponent<PlayerInteractor>().Current, Is.SameAs(terminal));
            Keys(Key.E); yield return null; yield return null;
            Assert.That(hud.TerminalPanel.activeSelf, Is.True); Assert.That(hud.TerminalText.text, Does.Contain("HANGAR"));
        }
        [UnityTest] public IEnumerator BuildingGhostUsesOnlyHighestDetailLod()
        {
            var example = new GameObject("LOD test module"); example.transform.position = Vector3.one * 100;
            var definition = Object.Instantiate(hud.Building.Selected);
            try
            {
                var mesh = hud.Building.Selected.Prefab.GetComponentInChildren<MeshFilter>().sharedMesh;
                var renderers = new Renderer[3];
                for (int i = 0; i < 3; i++)
                {
                    var child = new GameObject("LOD" + i, typeof(MeshFilter), typeof(MeshRenderer)); child.transform.SetParent(example.transform, false);
                    child.GetComponent<MeshFilter>().sharedMesh = mesh; renderers[i] = child.GetComponent<Renderer>();
                }
                example.AddComponent<LODGroup>().SetLODs(new[] { new LOD(.6f, new[] { renderers[0] }), new LOD(.2f, new[] { renderers[1] }), new LOD(.01f, new[] { renderers[2] }) });
                example.AddComponent<SpaceK.Rendering.DemoRendererCompatibility>();
                definition.Prefab = example; hud.Building.Modules = new[] { definition }; hud.Building.Select(0); hud.Building.Begin();
                Assert.That(GameObject.Find("Module Ghost").GetComponentsInChildren<MeshFilter>(true).Length, Is.EqualTo(1), "Ghost must not draw all three LODs on top of each other.");
            }
            finally { hud.Building.Cancel(); Object.Destroy(example); Object.Destroy(definition); }
            yield return null;
        }
        [UnityTest] public IEnumerator ImportedModulesCanBeSelectedPreviewedAndPlaced()
        {
            Teleport(new(5, .1f, -3)); yield return Aim(new(5, .06f, 1));
            var building = hud.Building; int verified = 0;
            for (int i = 0; i < building.Modules.Length; i++)
            {
                var definition = building.Modules[i]; if (definition.Prefab.GetComponent<LODGroup>() == null) continue;
                if (hud.Inventory.Model.Count(definition.Cost.Item) == 0) Assert.That(hud.Inventory.Model.TryAdd(definition.Cost.Item, 1), Is.True);
                building.Select(i); building.Begin(); yield return null; building.EvaluatePlacement();
                Assert.That(building.PlacementValid, Is.True, definition.DisplayName + ": " + building.Feedback);
                Assert.That(GameObject.Find("Module Ghost").GetComponentsInChildren<MeshFilter>(true).Length, Is.EqualTo(1));
                int before = hud.Inventory.Model.Count(definition.Cost.Item);
                Assert.That(building.TryPlace(), Is.True, definition.DisplayName);
                Assert.That(hud.Inventory.Model.Count(definition.Cost.Item), Is.EqualTo(before - 1));
                var placed = building.AssemblyRoot.GetChild(building.AssemblyRoot.childCount - 1);
                Assert.That(placed.GetComponent<LODGroup>().GetLODs().Length, Is.EqualTo(3));
                Object.Destroy(placed.gameObject); building.Cancel(); verified++; yield return null;
            }
            Assert.That(verified, Is.EqualTo(5));
        }
        [UnityTest] public IEnumerator BuildingSnapsPlacesConsumesOnceAndRejectsOverlap()
        {
            Teleport(new(5, .1f, -3)); yield return Aim(new(5, .06f, 1));
            var building = hud.Building; building.Begin(); building.EvaluatePlacement();
            Assert.That(building.PlacementValid, Is.True, building.Feedback); int count = hud.Inventory.Model.Count(building.Selected.Cost.Item);
            Assert.That(building.TryPlace(), Is.True); Assert.That(hud.Inventory.Model.Count(building.Selected.Cost.Item), Is.EqualTo(count - 1));
            var placed = building.AssemblyRoot.GetChild(0); Assert.That(placed.position.x, Is.EqualTo(Mathf.Round(placed.position.x)).Within(.001f));
            var blocker = new GameObject("Occupied Space"); var blockerCollider = blocker.AddComponent<BoxCollider>();
            blocker.transform.position = placed.position + new Vector3(.7f, 1.1f, 0); blockerCollider.size = new(.5f, 2, 2); Physics.SyncTransforms();
            building.EvaluatePlacement(); Assert.That(building.PlacementValid, Is.False);
            Assert.That(building.TryPlace(), Is.False); Assert.That(hud.Inventory.Model.Count(building.Selected.Cost.Item), Is.EqualTo(count - 1));
            Object.Destroy(blocker);
            building.Cancel(); Assert.That(building.IsPlacing, Is.False); yield return null;
        }
        [UnityTest] public IEnumerator FinAttachesToHullSideAndFacesOutward()
        {
            var building = hud.Building;
            building.Select(System.Array.FindIndex(building.Modules, m => m.name == "Hull"));
            Teleport(new(5, .1f, -3)); yield return Aim(new(5, .05f, 1));
            building.Begin(); Assert.That(building.TryPlace(), Is.True);
            var hull = building.AssemblyRoot.GetChild(0);
            building.Select(System.Array.FindIndex(building.Modules, m => m.name == "Stabilizer Fin"));
            yield return Aim(hull.position + new Vector3(0, -.2f, -.6f));
            building.EvaluatePlacement(); Assert.That(building.PlacementValid, Is.True, building.Feedback);
            var preview = GameObject.Find("Module Ghost").transform;
            Vector3 mountPosition = preview.position, facing = preview.forward;
            Keys(Key.R); yield return null; Keys(); yield return null;
            Assert.That(Vector3.Distance(preview.position, mountPosition), Is.LessThan(.001f), "R must rotate the fin at the same connector.");
            Assert.That(Vector3.Dot(preview.forward, facing), Is.LessThan(-.99f), "R must reverse the fin's orientation.");
            Keys(Key.R); yield return null; Keys(); yield return null;
            Assert.That(Vector3.Dot(preview.forward, facing), Is.GreaterThan(.99f));
            Assert.That(building.TryPlace(), Is.True);
            var fin = building.AssemblyRoot.GetChild(1);
            Assert.That(fin.position.z, Is.LessThan(hull.position.z - .6f));
            Assert.That(Vector3.Dot(fin.forward, Vector3.back), Is.GreaterThan(.99f));
            var hullBounds = hull.GetComponent<BoxCollider>().bounds;
            var finBounds = fin.GetComponent<BoxCollider>().bounds;
            Assert.That(finBounds.max.z, Is.EqualTo(hullBounds.min.z).Within(.025f), "Fin root must meet hull side.");
            for (int side = 1; side < 4; side++)
            {
                Keys(Key.F); yield return null; Keys(); yield return null;
                Assert.That(building.TryPlace(), Is.True, "F must allow all four mounting sides while the front fin is under the crosshair: " + building.Feedback);
                Assert.That(building.AssemblyRoot.GetChild(side + 1).position.y, Is.EqualTo(fin.position.y).Within(.001f));
            }
            Assert.That(building.TryRemove(hull.GetComponent<PlacedShipModule>()), Is.False, "Remove attached fins before their support.");
            for (int side = 0; side < 4; side++) Assert.That(building.UndoLastPlacement(), Is.True);
            Assert.That(hud.Inventory.Model.Count(building.Selected.Cost.Item), Is.EqualTo(4));
            Assert.That(building.TryRemove(hull.GetComponent<PlacedShipModule>()), Is.True);
        }
        [UnityTest] public IEnumerator HighModuleCanBeExtendedByAimingAtItsSide()
        {
            var building = hud.Building;
            var definition = System.Array.Find(building.Modules, m => m.name == "Hull");
            var upper = Object.Instantiate(definition.Prefab, new Vector3(5, 4.5f, 8.5f), Quaternion.identity, building.AssemblyRoot);
            upper.AddComponent<PlacedShipModule>().Definition = definition;
            building.Select(System.Array.FindIndex(building.Modules, m => m.name == "Nose Cone"));
            Teleport(new(5, .1f, -3)); yield return Aim(new(5, 5.2f, 7.9f));
            building.Begin(); building.EvaluatePlacement();
            Assert.That(building.PlacementValid, Is.True, building.Feedback);
            Assert.That(building.TryPlace(), Is.True);
            var nose = building.AssemblyRoot.GetChild(1);
            Assert.That(nose.position.x, Is.EqualTo(upper.transform.position.x).Within(.001f));
            Assert.That(nose.position.z, Is.EqualTo(upper.transform.position.z).Within(.001f));
            Assert.That(nose.GetComponent<BoxCollider>().bounds.min.y, Is.EqualTo(upper.GetComponent<BoxCollider>().bounds.max.y).Within(.025f));
        }
        [UnityTest] public IEnumerator UndoPlacementRefundsExactlyOnceThroughKeyboard()
        {
            var building = hud.Building; Teleport(new(5, .1f, -3)); yield return Aim(new(5, .05f, 1));
            building.Begin(); int before = hud.Inventory.Model.Count(building.Selected.Cost.Item);
            Assert.That(building.TryPlace(), Is.True); var placed = building.AssemblyRoot.GetChild(0).gameObject;
            Keys(Key.Z); yield return null; yield return null;
            Assert.That(placed == null || !placed.activeSelf, Is.True, "Z must undo the last installation.");
            Assert.That(hud.Inventory.Model.Count(building.Selected.Cost.Item), Is.EqualTo(before));
            Keys(); yield return null; Keys(Key.Z); yield return null; yield return null;
            Assert.That(hud.Inventory.Model.Count(building.Selected.Cost.Item), Is.EqualTo(before));
        }
        [UnityTest] public IEnumerator AimedRemovalKeepsModuleWhenRefundCannotFit()
        {
            var building = hud.Building; Teleport(new(5, .1f, -3)); yield return Aim(new(5, .05f, 1));
            building.Begin(); Assert.That(building.TryPlace(), Is.True); var placed = building.AssemblyRoot.GetChild(0).gameObject;
            yield return Aim(placed.transform.position);
            var fullCargo = new GameObject("Full refund cargo").AddComponent<InventoryComponent>();
            fullCargo.Slots = 1;
            var filler = hud.Inventory.StartingItems[0].Item;
            while (fullCargo.Model.TryAdd(filler, filler.MaxStack)) { }
            Assert.That(fullCargo.Model.Stacks.Count, Is.EqualTo(fullCargo.Model.SlotCapacity));
            int filled = fullCargo.Model.Count(filler);
            var original = building.Inventory; building.Inventory = fullCargo;
            Keys(Key.X); yield return null; yield return null;
            Assert.That(placed != null && placed.activeSelf, Is.True);
            Assert.That(fullCargo.Model.Count(filler), Is.EqualTo(filled));
            building.Inventory = original; Keys(); yield return null; Keys(Key.X); yield return null; yield return null;
            Assert.That(placed == null || !placed.activeSelf, Is.True, "X must remove an aimed module once cargo can receive its refund.");
            Object.Destroy(fullCargo.gameObject);
        }
        [UnityTest] public IEnumerator ThirdPersonCameraCollisionKeepsCameraInFrontOfWall()
        {
            Teleport(new(0, .1f, -17.5f)); motor.CameraRig.ToggleMode();
            yield return new WaitForSeconds(.6f);
            var view = motor.GetComponent<PlayerInteractor>().View;
            Assert.That(view.transform.position.z, Is.GreaterThan(-19.65f)); Assert.That(view.transform.position.z, Is.LessThan(motor.transform.position.z));
            int frame = Time.frameCount; yield return null; Assert.That(Time.frameCount, Is.GreaterThan(frame));
        }
        [UnityTest] public IEnumerator WorkbenchRecipeButtonProducesItemAfterItsDuration()
        {
            var station = Object.FindFirstObjectByType<CraftingStation>();
            Teleport(station.transform.position + new Vector3(0, .1f, -2)); hud.OpenCrafting(station); yield return null;
            var recipe = station.Recipes[0]; int outputBefore = hud.Inventory.Model.Count(recipe.Output.Item);
            hud.RecipeContent.GetChild(0).GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            Assert.That(station.Job.IsRunning, Is.True); Assert.That(hud.Inventory.Model.Count(recipe.Output.Item), Is.EqualTo(outputBefore));
            yield return new WaitForSeconds(recipe.Duration + .1f);
            Assert.That(station.Job.Succeeded, Is.True); Assert.That(hud.Inventory.Model.Count(recipe.Output.Item), Is.EqualTo(outputBefore + 1));
        }
    }
}


