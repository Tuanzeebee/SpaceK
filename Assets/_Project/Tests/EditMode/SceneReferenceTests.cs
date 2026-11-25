using System.Linq;
using NUnit.Framework;
using SpaceK.Building;
using SpaceK.CameraSystem;
using SpaceK.Crafting;
using SpaceK.Editor;
using SpaceK.Interaction;
using SpaceK.Inventory;
using SpaceK.Player;
using SpaceK.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;

namespace SpaceK.Tests
{
    public class SceneReferenceTests
    {
        [OneTimeSetUp] public void OpenDemo() { EditorSceneManager.OpenScene(FoundationSceneBuilder.ScenePath); }
        [Test] public void DemoContainsOneSharedControllerAndValidReferences()
        {
            var motor = Object.FindObjectsByType<PlayerMotor>(FindObjectsSortMode.None).Single();
            Assert.That(motor.CameraRig, Is.Not.Null); Assert.That(motor.Input.Actions, Is.Not.Null);
            Assert.That(motor.GetComponents<CharacterController>().Length, Is.EqualTo(1));
            var camera = motor.CameraRig; Assert.That(camera.FirstPersonCamera, Is.Not.Null); Assert.That(camera.ThirdPersonCamera, Is.Not.Null); Assert.That(camera.AimTarget, Is.Not.Null);
            var interactor = motor.GetComponent<PlayerInteractor>(); Assert.That(interactor.Inventory, Is.Not.Null); Assert.That(interactor.HUD, Is.Not.Null); Assert.That(interactor.View, Is.Not.Null);
            var building = motor.GetComponent<BuildingController>(); Assert.That(building.AssemblyRoot, Is.Not.Null); Assert.That(building.HUD, Is.Not.Null);
            Assert.That(motor.GetComponent<InventoryComponent>().StartingItems.Length, Is.GreaterThan(0));
            Assert.That(building.Modules.Length, Is.EqualTo(6)); foreach (var module in building.Modules) Assert.That(module.IsValid, Is.True, module.name);
        }
        [Test] public void RuntimeAssetsAndPrefabsHaveNoMissingScripts()
        {
            foreach (var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects()) Check(root);
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/_Project/Prefabs" }))
                Check(AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid)));
        }
        static void Check(GameObject root)
        {
            foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                Assert.That(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject), Is.Zero, transform.name);
        }
        [Test] public void InputActionsAreCompleteAndAllRecipesReferenceItems()
        {
            var input = Object.FindFirstObjectByType<PlayerInputReader>();
            foreach (string name in new[] { "Move", "Look", "Jump", "Sprint", "Walk", "Crouch", "Interact", "SwitchCamera", "Inventory", "Crafting", "Build", "Rotate", "NextModule", "Place", "Cancel", "Pause", "RemoveModule", "UndoBuild", "NextMountSide" })
                Assert.That(input.Actions.FindAction("Gameplay/" + name), Is.Not.Null, name);
            foreach (var station in Object.FindObjectsByType<CraftingStation>(FindObjectsSortMode.None))
                foreach (var recipe in station.Recipes) { Assert.That(recipe.IsValid, Is.True); foreach (var material in recipe.Materials) Assert.That(material.Item, Is.Not.Null); }
            var ids = AssetDatabase.FindAssets("t:ItemData", new[] { "Assets/_Project" }).Select(g => AssetDatabase.LoadAssetAtPath<ItemData>(AssetDatabase.GUIDToAssetPath(g))).ToArray();
            Assert.That(ids.Select(i => i.Id).Distinct().Count(), Is.EqualTo(ids.Length)); Assert.That(ids.All(i => i.Icon != null), Is.True);
        }
        [Test] public void UrpAndUiInputArePreserved()
        {
            Assert.That(QualitySettings.renderPipeline ?? GraphicsSettings.defaultRenderPipeline, Is.Not.Null);
            Assert.That((QualitySettings.renderPipeline ?? GraphicsSettings.defaultRenderPipeline).GetType().Name, Does.Contain("Universal"));
            Assert.That(Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
            var hud = Object.FindFirstObjectByType<SimulatorHUD>(); Assert.That(hud.RowPrefab, Is.Not.Null); Assert.That(hud.CraftProgress, Is.Not.Null);
            Assert.That(hud.GetComponent<UnityEngine.UI.GraphicRaycaster>(), Is.Not.Null);
        }
    }
}
