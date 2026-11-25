using System;
using System.IO;
using System.Linq;
using SpaceK.Building;
using SpaceK.CameraSystem;
using SpaceK.Crafting;
using SpaceK.Interaction;
using SpaceK.Inventory;
using SpaceK.Player;
using SpaceK.UI;
using TMPro;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

namespace SpaceK.Editor
{
    public static class FoundationSceneBuilder
    {
        const string Root = "Assets/_Project";
        public const string ScenePath = Root + "/Scenes/SpacecraftSimulator_Test.unity";
        static readonly Color Navy = new(.025f, .055f, .09f), PanelColor = new(.045f, .09f, .14f, .98f);
        static readonly Color Cyan = new(.25f, .88f, .93f), Muted = new(.55f, .69f, .76f), White = new(.88f, .94f, .97f);
        static Material metal, dark, floor, orange, cyan, glass;
        static TMP_FontAsset font;

        [MenuItem("SpaceK/Create Foundation Demo (preserve existing)")]
        public static void Create()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
            if (File.Exists(ScenePath)) throw new InvalidOperationException("Demo scene exists. Open it; generation never overwrites existing scenes.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save modified open scenes before generating the demo.");
            foreach (string folder in new[] { "Art", "Audio", "Animations", "Materials", "Prefabs/Player", "Prefabs/Environment", "Prefabs/Interactables", "Prefabs/Modules", "Prefabs/UI", "Scenes", "ScriptableObjects/Items", "ScriptableObjects/Recipes", "ScriptableObjects/Modules", "Settings" })
                Directory.CreateDirectory(Root + "/" + folder);
            AssetDatabase.Refresh();
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            if (font == null) throw new InvalidOperationException("Import TMP essentials non-interactively first with TMP_PackageResourceImporter.ImportResources(true,false,false).");
            metal = Material("Brushed Alloy", new(.22f, .31f, .38f), .65f);
            dark = Material("Navy Panels", new(.04f, .08f, .12f), .35f);
            floor = Material("Hangar Deck", new(.095f, .13f, .16f), .3f);
            orange = Material("Safety Amber", new(.95f, .43f, .12f), .3f);
            cyan = Material("Signal Cyan", new(.08f, .65f, .72f), .25f, true);
            glass = Material("Terminal Screen", new(.035f, .23f, .3f), .4f, true);
            var valid = Material("Ghost Valid", new(.08f, .75f, .61f, .42f), 0, true, true);
            var invalid = Material("Ghost Invalid", new(.95f, .19f, .14f, .42f), 0, true, true);
            var alloy = Item("alloy", "Titanium Alloy", "Structural feedstock for hulls and engines.", 50, new(.4f, .65f, .76f));
            var circuit = Item("circuit", "Control Circuit", "Avionics and fabrication electronics.", 30, Cyan);
            var hullItem = Item("hull-module", "Hull Module", "A fabricated structural module, ready to install.", 10, new(.75f, .8f, .85f));
            var engineItem = Item("engine-module", "Engine Module", "A modular single-bell propulsion unit.", 10, new(.96f, .5f, .18f));
            var cockpitItem = Item("cockpit-module", "Cockpit Module", "A pressurised control compartment placeholder.", 5, new(.24f, .85f, .75f));
            var hullPrefab = ModulePrefab("Hull Module", new(2, 1, 2), 0);
            var enginePrefab = ModulePrefab("Thruster Module", new(1, 1, 2), 1);
            var cockpitPrefab = ModulePrefab("Cockpit Module", new(2, 2, 2), 2);
            var modules = new[] { Module("Hull", hullPrefab, hullItem, new(2, 1, 2)), Module("Thruster", enginePrefab, engineItem, new(1, 1, 2)), Module("Cockpit", cockpitPrefab, cockpitItem, new(2, 2, 2)) };
            var recipes = new[] { Recipe("Hull Module", alloy, 4, circuit, 1, hullItem, 3), Recipe("Thruster Module", alloy, 6, circuit, 2, engineItem, 5), Recipe("Cockpit Module", alloy, 8, circuit, 3, cockpitItem, 6) };
            // Include imported modules while retaining the original selection order.
            modules = modules.Concat(AssetDatabase.FindAssets("t:BuildModuleDefinition", new[] { Root + "/ScriptableObjects/Modules" })
                .Select(g => AssetDatabase.LoadAssetAtPath<BuildModuleDefinition>(AssetDatabase.GUIDToAssetPath(g))).Where(m => m.IsValid && !modules.Contains(m))).ToArray();
            recipes = recipes.Concat(AssetDatabase.FindAssets("t:CraftingRecipe", new[] { Root + "/ScriptableObjects/Recipes" })
                .Select(g => AssetDatabase.LoadAssetAtPath<CraftingRecipe>(AssetDatabase.GUIDToAssetPath(g))).Where(r => r.IsValid && !recipes.Contains(r))).ToArray();
            var actions = InputActions();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "SpacecraftSimulator_Test";
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.32f, .4f, .48f);
            var earthSky = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/Earth Sky.mat");
            if (earthSky != null) RenderSettings.skybox = earthSky;
            RenderSettings.fog = true; RenderSettings.fogColor = Navy; RenderSettings.fogMode = FogMode.ExponentialSquared; RenderSettings.fogDensity = .009f;
            var sun = new GameObject("Hangar Key Light").AddComponent<Light>(); sun.type = LightType.Directional; sun.intensity = 1.4f; sun.color = new(.78f, .88f, 1); sun.transform.rotation = Quaternion.Euler(55, -25, 0); sun.shadows = LightShadows.Soft;
            BuildHangar();
            var assembly = new GameObject("Spacecraft Assembly").transform;
            var workbench = Workbench(recipes); workbench.transform.position = new(-4, 0, -3);
            var terminal = Terminal(); terminal.transform.position = new(4, 0, -6);
            Pickup(alloy, 12, new(-1.5f, .5f, -5)); Pickup(circuit, 4, new(1, .5f, -4.5f));
            var player = Player(actions, valid, invalid, modules);
            player.transform.position = new(0, .1f, -10);
            var hud = HUD();
            hud.Input = player.GetComponent<PlayerInputReader>(); hud.Inventory = player.GetComponent<InventoryComponent>(); hud.CameraRig = player.GetComponent<PlayerCameraRig>();
            hud.Building = player.GetComponent<BuildingController>(); hud.Stations = new[] { workbench.GetComponent<CraftingStation>() };
            player.GetComponent<InventoryComponent>().StartingItems = new[] { new ItemAmount(alloy, 20), new ItemAmount(circuit, 8), new ItemAmount(hullItem, 2), new ItemAmount(engineItem, 1) };
            player.GetComponent<InventoryComponent>().StartingItems = player.GetComponent<InventoryComponent>().StartingItems
                .Concat(modules.Skip(3).Select(m => new ItemAmount(m.Cost.Item, m.Cost.Item.Id == "stabilizer-fin" ? 4 : 1))).ToArray();
            var building = player.GetComponent<BuildingController>(); building.HUD = hud; building.AssemblyRoot = assembly;
            player.GetComponent<PlayerInteractor>().HUD = hud;
            foreach (var component in player.GetComponents<Component>()) PrefabUtility.RecordPrefabInstancePropertyModifications(component);
            var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            eventSystem.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            foreach (var root in scene.GetRootGameObjects()) root.AddComponent<SpaceK.Rendering.DemoRendererCompatibility>();
            EditorSceneManager.MarkSceneDirty(scene); AssetDatabase.SaveAssets();
            if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new IOException("Could not save demo scene.");
            var scenes = EditorBuildSettings.scenes.ToList();
            if (!scenes.Any(s => s.path == ScenePath)) { scenes.Add(new EditorBuildSettingsScene(ScenePath, true)); EditorBuildSettings.scenes = scenes.ToArray(); }
            Selection.activeGameObject = player;
            Debug.Log("[SpaceK] Foundation scene, prefabs, data, input and UI saved: " + ScenePath);
        }

        static Material Material(string name, Color color, float metallic, bool emission = false, bool transparent = false)
        {
            string path = Root + "/Materials/" + name + ".mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path); if (existing != null) return existing;
            var shader = Shader.Find("Universal Render Pipeline/Lit"); if (shader == null) throw new InvalidOperationException("URP Lit shader missing.");
            var material = new Material(shader) { name = name }; material.SetColor("_BaseColor", color); material.SetFloat("_Metallic", metallic); material.SetFloat("_Smoothness", .45f);
            if (emission) { material.EnableKeyword("_EMISSION"); material.SetColor("_EmissionColor", color * .65f); }
            if (transparent)
            {
                material.SetFloat("_Surface", 1); material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha); material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetFloat("_ZWrite", 0); material.SetOverrideTag("RenderType", "Transparent"); material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); material.renderQueue = 3000;
            }
            AssetDatabase.CreateAsset(material, path); return material;
        }
        static ItemData Item(string id, string name, string description, int stack, Color color)
        {
            var item = Asset<ItemData>(Root + "/ScriptableObjects/Items/" + id + ".asset");
            item.Id = id; item.DisplayName = name; item.Description = description; item.MaxStack = stack;
            string iconPath = Root + "/Art/" + id + "-icon.png";
            if (!File.Exists(iconPath))
            {
                var texture = new Texture2D(32, 32); var pixels = new Color[1024];
                for (int y = 0; y < 32; y++) for (int x = 0; x < 32; x++) pixels[y * 32 + x] = x < 3 || y < 3 || x > 28 || y > 28 ? Navy : color * (x > 7 && x < 24 && y > 7 && y < 24 ? 1 : .45f);
                texture.SetPixels(pixels); texture.Apply(); File.WriteAllBytes(iconPath, texture.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(iconPath);
                var importer = (TextureImporter)AssetImporter.GetAtPath(iconPath); importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single; importer.SaveAndReimport();
            }
            item.Icon = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath); EditorUtility.SetDirty(item); return item;
        }
        static T Asset<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path); if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(asset, path); return asset;
        }
        static CraftingRecipe Recipe(string name, ItemData a, int ac, ItemData b, int bc, ItemData output, float seconds)
        {
            string path = Root + "/ScriptableObjects/Recipes/" + name + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<CraftingRecipe>(path); if (existing != null) return existing;
            var recipe = Asset<CraftingRecipe>(path); recipe.DisplayName = name;
            recipe.Materials = new[] { new ItemAmount(a, ac), new ItemAmount(b, bc) }; recipe.Output = new ItemAmount(output, 1); recipe.Duration = seconds; EditorUtility.SetDirty(recipe); return recipe;
        }
        static BuildModuleDefinition Module(string name, GameObject prefab, ItemData cost, Vector3 size)
        {
            string path = Root + "/ScriptableObjects/Modules/" + name + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<BuildModuleDefinition>(path); if (existing != null) return existing;
            var module = Asset<BuildModuleDefinition>(path); module.DisplayName = name; module.Prefab = prefab; module.Cost = new ItemAmount(cost, 1); module.Size = size; module.SocketStep = size; EditorUtility.SetDirty(module); return module;
        }
        static GameObject Cube(string name, Vector3 position, Vector3 size, Material material, Transform parent = null, bool collider = true)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name; go.transform.SetParent(parent, false); go.transform.localPosition = position; go.transform.localScale = size; go.GetComponent<Renderer>().sharedMaterial = material;
            if (!collider) UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>()); return go;
        }
        static GameObject ModulePrefab(string name, Vector3 size, int kind)
        {
            string path = Root + "/Prefabs/Modules/" + name + ".prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path); if (existing != null) return existing;
            var root = new GameObject(name); root.AddComponent<BoxCollider>().size = size;
            Cube("Pressure Shell", Vector3.zero, size - Vector3.one * .05f, metal, root.transform, false);
            Cube("Service Stripe", new(0, size.y * .5f - .02f, 0), new(size.x * .8f, .02f, .15f), kind == 1 ? orange : cyan, root.transform, false);
            if (kind == 1) Cube("Engine Exhaust", new(0, 0, -size.z * .5f + .02f), new(size.x * .65f, size.y * .65f, .04f), cyan, root.transform, false);
            if (kind == 2) Cube("Canopy", new(0, size.y * .15f, size.z * .5f - .01f), new(size.x * .8f, size.y * .5f, .025f), glass, root.transform, false);
            root.AddComponent<SpaceK.Rendering.DemoRendererCompatibility>();
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path); UnityEngine.Object.DestroyImmediate(root); return prefab;
        }
        static void BuildHangar()
        {
            var hangar = new GameObject("Hangar 07").transform;
            Cube("Deck", new(0, -.25f, 0), new(32, .5f, 40), floor, hangar);
            Cube("Port Wall", new(-16, 4, 0), new(.5f, 8, 40), dark, hangar); Cube("Starboard Wall", new(16, 4, 0), new(.5f, 8, 40), dark, hangar);
            Cube("Front Bulkhead", new(0, 4, -20), new(32, 8, .5f), dark, hangar); Cube("Rear Bulkhead", new(0, 4, 20), new(32, 8, .5f), dark, hangar);
            for (int z = -18; z < 20; z += 6)
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    Cube("Structural Rib", new(side * 15.4f, 3.8f, z), new(.8f, 7.6f, .6f), metal, hangar);
                    Cube("Guide Light", new(side * 15.1f, 2.7f, z), new(.06f, 2.3f, .2f), cyan, hangar, false);
                }
                Cube("Overhead Beam", new(0, 8, z), new(31, .4f, .7f), metal, hangar);
                var light = new GameObject("Ceiling Worklight").AddComponent<Light>(); light.transform.SetParent(hangar); light.transform.position = new(0, 6.5f, z); light.type = LightType.Point; light.range = 13; light.intensity = 5; light.color = new(.65f, .83f, 1);
            }
            for (int x = -14; x <= 14; x += 2) Cube("Deck Seam", new(x, .003f, 0), new(.025f, .006f, 39), metal, hangar, false);
            var bay = Cube("Assembly Bay", new(5, .025f, 3), new(12, .05f, 14), metal, hangar); bay.AddComponent<PlacementSurface>();
            for (int x = -1; x <= 11; x++) Cube("Bay Grid", new(x, .054f, 3), new(.025f, .006f, 14), dark, hangar, false);
            for (int z = -4; z <= 10; z++) Cube("Bay Grid", new(5, .054f, z), new(12, .006f, .025f), dark, hangar, false);
            Cube("Bay Port Marking", new(-1.1f, .01f, 3), new(.12f, .02f, 14.3f), orange, hangar, false); Cube("Bay Starboard Marking", new(11.1f, .01f, 3), new(.12f, .02f, 14.3f), orange, hangar, false);
            Cube("Bay Near Marking", new(5, .01f, -4.1f), new(12.3f, .02f, .12f), orange, hangar, false); Cube("Bay Far Marking", new(5, .01f, 10.1f), new(12.3f, .02f, .12f), orange, hangar, false);
            Cube("Airlock Port", new(-9, 2, 12), new(14, 4, .5f), metal, hangar); Cube("Airlock Starboard", new(9, 2, 12), new(14, 4, .5f), metal, hangar); Cube("Airlock Header", new(0, 4.5f, 12), new(4, 1, .5f), dark, hangar);
            var door = new GameObject("Airlock Door"); door.transform.position = new(0, 0, 12);
            var panel = Cube("Sliding Panel", new(0, 1.75f, 0), new(3.8f, 3.5f, .3f), dark, door.transform);
            Cube("Window", new(0, .3f / 3.5f, -.17f / .3f), new(2.7f / 3.8f, .7f / 3.5f, .05f / .3f), glass, panel.transform, false);
            var interaction = door.AddComponent<DoorInteractable>(); interaction.Panel = panel.transform; interaction.OpenOffset = new(0, 3.7f, 0);
            PrefabUtility.SaveAsPrefabAssetAndConnect(door, Root + "/Prefabs/Interactables/Airlock.prefab", InteractionMode.AutomatedAction);
            WorldText("HANGAR 07", new(0, 5.7f, 19.65f), 2.1f, White, hangar);
            WorldText("SPACECRAFT ASSEMBLY / AUTHORIZED PERSONNEL", new(0, 4.7f, 19.6f), .46f, Cyan, hangar);
            WorldText("FABRICATION", new(-4, 2.6f, -2.3f), .34f, Cyan, hangar);
            WorldText("ASSEMBLY BAY", new(5, .15f, -4.3f), .45f, White, hangar).transform.rotation = Quaternion.Euler(90, 0, 0);
            var crate = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Prefabs/Environment/Material Crate.prefab");
            for (int i = 0; i < 4; i++)
            {
                if (crate != null)
                {
                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(crate, hangar);
                    instance.transform.position = new(-11 + i % 2 * .9f, .3f + i / 2 * .6f, 3);
                }
                else Cube("Cargo Crate", new(-11 + i % 2 * 1.8f, .65f + i / 2 * 1.3f, 3), new(1.5f, 1.3f, 1.5f), i % 2 == 0 ? orange : metal, hangar);
            }
            var surfaces = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Prefabs/Environment/Hangar Surfaces.prefab");
            if (surfaces != null)
            {
                PrefabUtility.InstantiatePrefab(surfaces, hangar);
                foreach (string name in new[] { "Deck", "Port Wall", "Starboard Wall", "Front Bulkhead", "Rear Bulkhead", "Airlock Port", "Airlock Starboard", "Airlock Header" })
                    hangar.Find(name).GetComponent<Renderer>().enabled = false;
                foreach (var renderer in hangar.GetComponentsInChildren<Renderer>())
                    if (renderer.name == "Deck Seam") renderer.enabled = false;
            }
        }
        static TextMeshPro WorldText(string value, Vector3 position, float size, Color color, Transform parent)
        {
            var go = new GameObject(value); go.transform.SetParent(parent, false); go.transform.position = position;
            var text = go.AddComponent<TextMeshPro>(); text.font = font; text.text = value; text.fontSize = size * 10; text.color = color; text.alignment = TextAlignmentOptions.Center; text.rectTransform.sizeDelta = new(28, 3); return text;
        }
        static GameObject Workbench(CraftingRecipe[] recipes)
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Prefabs/Interactables/Workbench.prefab");
            if (existing != null)
            {
                // Preserve imported art when rebuilding a missing demo scene.
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(existing);
                instance.GetComponent<CraftingStation>().Recipes = recipes;
                return instance;
            }
            var go = new GameObject("Fabrication Workbench");
            Cube("Cabinet", new(0, .5f, 0), new(2.4f, 1, 1.1f), dark, go.transform); Cube("Worktop", new(0, 1.05f, 0), new(2.6f, .15f, 1.3f), metal, go.transform);
            Cube("Fabricator", new(-.65f, 1.4f, .15f), new(.9f, .65f, .7f), orange, go.transform); Cube("Display", new(.6f, 1.45f, .3f), new(.75f, .5f, .1f), glass, go.transform);
            go.AddComponent<CraftingStation>().Recipes = recipes; PrefabUtility.SaveAsPrefabAssetAndConnect(go, Root + "/Prefabs/Interactables/Workbench.prefab", InteractionMode.AutomatedAction); return go;
        }
        static GameObject Terminal()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Prefabs/Interactables/Terminal.prefab");
            if (existing != null) return (GameObject)PrefabUtility.InstantiatePrefab(existing);
            var go = new GameObject("Computer Terminal"); Cube("Console", new(0, .65f, 0), new(1.2f, 1.3f, .7f), metal, go.transform); Cube("Screen", new(0, 1.65f, .05f), new(1.25f, .7f, .15f), glass, go.transform);
            go.AddComponent<TerminalInteractable>(); PrefabUtility.SaveAsPrefabAssetAndConnect(go, Root + "/Prefabs/Interactables/Terminal.prefab", InteractionMode.AutomatedAction); return go;
        }
        static void Pickup(ItemData item, int count, Vector3 position)
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Prefabs/Interactables/Pickup-" + item.Id + ".prefab");
            if (existing != null)
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(existing);
                instance.GetComponent<PickupInteractable>().Content = new ItemAmount(item, count);
                position.y = existing.GetComponent<BoxCollider>().size.y * .5f;
                instance.transform.position = position;
                return;
            }
            var go = Cube(item.DisplayName + " Supply", Vector3.zero, new(.7f, .6f, .7f), item.Id == "alloy" ? orange : cyan);
            go.AddComponent<PickupInteractable>().Content = new ItemAmount(item, count);
            PrefabUtility.SaveAsPrefabAssetAndConnect(go, Root + "/Prefabs/Interactables/Pickup-" + item.Id + ".prefab", InteractionMode.AutomatedAction); go.transform.position = position;
        }

        static InputActionAsset InputActions()
        {
            string path = Root + "/Settings/Spacecraft.inputactions";
            var existing = AssetDatabase.LoadAssetAtPath<InputActionAsset>(path); if (existing != null) return existing;
            var asset = ScriptableObject.CreateInstance<InputActionAsset>(); var map = asset.AddActionMap("Gameplay");
            var move = map.AddAction("Move", InputActionType.Value, expectedControlLayout: "Vector2");
            move.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s").With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d"); move.AddBinding("<Gamepad>/leftStick");
            var look = map.AddAction("Look", InputActionType.Value, expectedControlLayout: "Vector2"); look.AddBinding("<Mouse>/delta"); look.AddBinding("<Gamepad>/rightStick");
            Bind(map, "Jump", "<Keyboard>/space", "<Gamepad>/buttonSouth"); Bind(map, "Sprint", "<Keyboard>/leftShift", "<Gamepad>/leftStickPress"); Bind(map, "Walk", "<Keyboard>/leftAlt");
            Bind(map, "Crouch", "<Keyboard>/leftCtrl", "<Gamepad>/buttonEast"); Bind(map, "Interact", "<Keyboard>/e", "<Gamepad>/buttonWest"); Bind(map, "SwitchCamera", "<Keyboard>/v", "<Gamepad>/rightStickPress");
            Bind(map, "Inventory", "<Keyboard>/i", "<Gamepad>/select"); map.FindAction("Inventory").AddBinding("<Keyboard>/tab"); Bind(map, "Crafting", "<Keyboard>/c"); Bind(map, "Build", "<Keyboard>/b", "<Gamepad>/dpad/up");
            Bind(map, "Rotate", "<Keyboard>/r", "<Gamepad>/rightShoulder"); Bind(map, "NextModule", "<Keyboard>/q", "<Gamepad>/leftShoulder"); Bind(map, "Place", "<Mouse>/leftButton", "<Gamepad>/rightTrigger"); Bind(map, "Cancel", "<Mouse>/rightButton"); Bind(map, "Pause", "<Keyboard>/escape", "<Gamepad>/start");
            Bind(map, "RemoveModule", "<Keyboard>/x", "<Gamepad>/dpad/down"); Bind(map, "UndoBuild", "<Keyboard>/z", "<Gamepad>/dpad/left");
            Bind(map, "NextMountSide", "<Keyboard>/f", "<Gamepad>/dpad/right");
            File.WriteAllText(path, asset.ToJson()); UnityEngine.Object.DestroyImmediate(asset); AssetDatabase.ImportAsset(path); return AssetDatabase.LoadAssetAtPath<InputActionAsset>(path);
        }
        static void Bind(InputActionMap map, string name, params string[] bindings)
        {
            var action = map.AddAction(name, InputActionType.Button); foreach (string binding in bindings) action.AddBinding(binding);
        }
        static GameObject Player(InputActionAsset actions, Material valid, Material invalid, BuildModuleDefinition[] modules)
        {
            var player = new GameObject("Engineer"); player.layer = 2;
            var controller = player.AddComponent<CharacterController>(); controller.height = 1.8f; controller.radius = .32f; controller.center = Vector3.up * .9f; controller.stepOffset = .25f; controller.skinWidth = .025f;
            var input = player.AddComponent<PlayerInputReader>(); input.Actions = actions;
            var inventory = player.AddComponent<InventoryComponent>();
            var motor = player.AddComponent<PlayerMotor>(); motor.Input = input;
            var visual = new GameObject("Body Visual").transform; visual.SetParent(player.transform, false); motor.VisualRoot = visual;
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule); body.name = "Engineer Suit"; body.transform.SetParent(visual, false); body.transform.localPosition = new(0, .9f, 0); body.transform.localScale = new(.55f, .9f, .55f); body.GetComponent<Renderer>().sharedMaterial = orange; UnityEngine.Object.DestroyImmediate(body.GetComponent<Collider>()); body.layer = 2;
            var visor = Cube("Visor", new(0, 1.5f, .23f), new(.38f, .19f, .09f), glass, visual, false); visor.layer = 2;
            var aim = new GameObject("Camera Aim").transform; aim.SetParent(player.transform, false); aim.localPosition = new(0, 1.65f, 0);
            var cameraObject = new GameObject("Player Camera", typeof(UnityEngine.Camera), typeof(AudioListener), typeof(CinemachineBrain), typeof(CameraWallGuard)); cameraObject.tag = "MainCamera"; cameraObject.transform.SetParent(player.transform, false);
            var camera = cameraObject.GetComponent<UnityEngine.Camera>(); camera.nearClipPlane = .05f; camera.farClipPlane = 120; camera.fieldOfView = 70; camera.backgroundColor = Navy; camera.clearFlags = CameraClearFlags.SolidColor;
            var brain = cameraObject.GetComponent<CinemachineBrain>(); brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut, .3f); brain.UpdateMethod = CinemachineBrain.UpdateMethods.LateUpdate;
            cameraObject.GetComponent<CameraWallGuard>().Anchor = aim;
            var fp = VirtualCamera("First Person", player.transform, aim, 0); var tp = VirtualCamera("Third Person", player.transform, aim, 4);
            var rig = player.AddComponent<PlayerCameraRig>(); rig.Input = input; rig.Motor = motor; rig.AimTarget = aim; rig.FirstPersonCamera = fp; rig.ThirdPersonCamera = tp; rig.BodyRenderers = visual.GetComponentsInChildren<Renderer>(); motor.CameraRig = rig;
            var building = player.AddComponent<BuildingController>(); building.Input = input; building.Inventory = inventory; building.View = camera; building.Modules = modules; building.ValidGhostMaterial = valid; building.InvalidGhostMaterial = invalid;
            var interactor = player.AddComponent<PlayerInteractor>(); interactor.Input = input; interactor.Inventory = inventory; interactor.View = camera;
            PrefabUtility.SaveAsPrefabAssetAndConnect(player, Root + "/Prefabs/Player/Engineer.prefab", InteractionMode.AutomatedAction); return player;
        }
        static CinemachineCamera VirtualCamera(string name, Transform parent, Transform aim, float distance)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false);
            var camera = go.AddComponent<CinemachineCamera>(); camera.Follow = aim; camera.Lens.FieldOfView = 70; camera.Lens.NearClipPlane = .05f; camera.Lens.FarClipPlane = 120;
            var follow = go.AddComponent<CinemachineThirdPersonFollow>(); follow.Damping = Vector3.zero; follow.ShoulderOffset = Vector3.zero; follow.VerticalArmLength = 0; follow.CameraDistance = distance;
            follow.AvoidObstacles = new CinemachineThirdPersonFollow.ObstacleSettings { Enabled = true, CollisionFilter = ~4, IgnoreTag = "Player", CameraRadius = .2f, DampingIntoCollision = 0, DampingFromCollision = .15f }; return camera;
        }

        static SimulatorHUD HUD()
        {
            var go = new GameObject("Simulator HUD", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster), typeof(SimulatorHUD));
            go.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.GetComponent<UnityEngine.UI.CanvasScaler>(); scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new(1600, 900); scaler.matchWidthOrHeight = .5f;
            var root = go.transform; var hud = go.GetComponent<SimulatorHUD>();
            var header = Box("Header", root, new(0, 1), new(1, 1), new(20, -92), new(-20, -20), PanelColor);
            Label("Brand", header, "SPACEK  /  HANGAR 07", 26, White, new(20, 26), new(-560, -10));
            Label("Subtitle", header, "ENGINEERING SIMULATOR   /   FOUNDATION", 14, Muted, new(20, 4), new(-560, -45));
            hud.CameraMode = Label("Camera Mode", header, "CAM / FIRST PERSON   [V]", 18, Cyan, new(790, 15), new(-18, -15)); hud.CameraMode.alignment = TextAlignmentOptions.Right;
            var crosshair = Label("Reticle", root, "+", 26, Cyan, Vector2.zero, Vector2.zero); var crossRect = crosshair.rectTransform; crossRect.anchorMin = crossRect.anchorMax = crossRect.pivot = new(.5f, .5f); crossRect.sizeDelta = new(28, 28); crossRect.anchoredPosition = Vector2.zero; crosshair.alignment = TextAlignmentOptions.Center;
            hud.Prompt = FixedLabel("Interaction Prompt", root, "", new(.5f, .5f), new(0, -65), new(720, 55), 24, White); hud.Prompt.alignment = TextAlignmentOptions.Center;
            hud.Toast = FixedLabel("Notification", root, "Welcome engineer. Collect supplies, then visit fabrication.", new(.5f, 1), new(0, -125), new(1000, 45), 20, Cyan); hud.Toast.alignment = TextAlignmentOptions.Center;
            var footer = Box("Controls", root, new(0, 0), new(1, 0), new(20, 20), new(-20, 94), PanelColor);
            Label("Controls Text", footer, "WASD Move    SHIFT Sprint    ALT Walk    SPACE Jump    CTRL Crouch    E Interact\nV Camera    I / TAB Cargo    C Craft near workbench    B Assembly    ESC Cursor / Close", 17, Muted, new(20, 8), new(-20, -8));
            hud.InventoryPanel = Modal("Cargo Panel", root, "CARGO MANIFEST", "Inventory", hud);
            hud.InventorySummary = FixedLabel("Inventory Summary", hud.InventoryPanel.transform, "CARGO", new(.5f, 1), new(0, -85), new(630, 32), 18, Cyan);
            hud.InventoryContent = ScrollContent("Cargo List", hud.InventoryPanel.transform, new(24, 30), new(-24, -120));
            hud.CraftingPanel = Modal("Crafting Panel", root, "FABRICATION / WORKBENCH", "Crafting", hud);
            hud.RecipeContent = ScrollContent("Recipe List", hud.CraftingPanel.transform, new(24, 145), new(-24, -90));
            hud.CraftStatus = FixedLabel("Job Status", hud.CraftingPanel.transform, "Select a recipe", new(.5f, 0), new(0, 112), new(625, 36), 18, Cyan);
            hud.CraftProgress = Progress(hud.CraftingPanel.transform);
            hud.CancelCraftButton = Button("Cancel Fabrication", hud.CraftingPanel.transform, "CANCEL JOB", new(.5f, 0), new(0, 35), new(220, 40));
            hud.TerminalPanel = Modal("Terminal Panel", root, "HANGAR SYSTEMS / TERMINAL", "Terminal", hud);
            hud.TerminalText = Label("System Report", hud.TerminalPanel.transform, "", 25, Cyan, new(38, 35), new(-38, -115));
            hud.BuildPanel = Box("Building HUD", root, new(.5f, 0), new(.5f, 0), new(-440, 112), new(440, 223), PanelColor).gameObject;
            hud.BuildStatus = Label("Building Status", hud.BuildPanel.transform, "", 21, Cyan, new(24, 8), new(-24, -8)); hud.BuildStatus.alignment = TextAlignmentOptions.Center;
            hud.InventoryPanel.SetActive(false); hud.CraftingPanel.SetActive(false); hud.TerminalPanel.SetActive(false); hud.BuildPanel.SetActive(false);
            var template = Button("Cargo Recipe Row", root, "ITEM", new(.5f, .5f), Vector2.zero, new(600, 84));
            var rowLayout = template.gameObject.AddComponent<UnityEngine.UI.LayoutElement>(); rowLayout.minHeight = 84; rowLayout.preferredHeight = 84;
            template.GetComponentInChildren<TMP_Text>().rectTransform.offsetMin = new(80, 8);
            var icon = Box("Icon", template.transform, new(0, .5f), new(0, .5f), new(14, -25), new(64, 25), White); icon.GetComponent<UnityEngine.UI.Image>().preserveAspect = true;
            var rowPrefab = PrefabUtility.SaveAsPrefabAsset(template.gameObject, Root + "/Prefabs/UI/CargoRecipeRow.prefab"); hud.RowPrefab = rowPrefab.GetComponent<UnityEngine.UI.Button>(); UnityEngine.Object.DestroyImmediate(template.gameObject);
            return hud;
        }
        static GameObject Modal(string name, Transform root, string title, string unused, SimulatorHUD hud)
        {
            var panel = Box(name, root, new(.5f, .5f), new(.5f, .5f), new(-360, -280), new(360, 280), PanelColor);
            var stripe = Box("Accent", panel, new(0, 1), new(1, 1), new(0, -5), Vector2.zero, Cyan);
            FixedLabel("Title", panel, title, new(0, 1), new(32, -35), new(580, 45), 27, White).rectTransform.pivot = new(0, .5f);
            var close = Button("Close", panel, "X", new(1, 1), new(-32, -32), new(40, 40));
            UnityEditor.Events.UnityEventTools.AddPersistentListener(close.onClick, hud.ClosePanels); return panel.gameObject;
        }
        static RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max, Vector2 low, Vector2 high)
        {
            var go = new GameObject(name, typeof(RectTransform)); var rect = go.GetComponent<RectTransform>(); rect.SetParent(parent, false); rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = low; rect.offsetMax = high; return rect;
        }
        static RectTransform Box(string name, Transform parent, Vector2 min, Vector2 max, Vector2 low, Vector2 high, Color color)
        {
            var rect = Rect(name, parent, min, max, low, high); var image = rect.gameObject.AddComponent<UnityEngine.UI.Image>(); image.color = color; image.raycastTarget = name.Contains("Panel") || name == "Viewport"; return rect;
        }
        static TextMeshProUGUI Label(string name, Transform parent, string text, float size, Color color, Vector2 low, Vector2 high)
        {
            var rect = Rect(name, parent, Vector2.zero, Vector2.one, low, high); var label = rect.gameObject.AddComponent<TextMeshProUGUI>(); label.font = font; label.text = text; label.fontSize = size; label.color = color; label.raycastTarget = false; label.alignment = TextAlignmentOptions.MidlineLeft; return label;
        }
        static TextMeshProUGUI FixedLabel(string name, Transform parent, string text, Vector2 anchor, Vector2 position, Vector2 size, float fontSize, Color color)
        {
            var label = Label(name, parent, text, fontSize, color, Vector2.zero, Vector2.zero); var rect = label.rectTransform; rect.anchorMin = rect.anchorMax = anchor; rect.sizeDelta = size; rect.anchoredPosition = position; return label;
        }
        static UnityEngine.UI.Button Button(string name, Transform parent, string text, Vector2 anchor, Vector2 position, Vector2 size)
        {
            var rect = Box(name, parent, anchor, anchor, Vector2.zero, size, new(.09f, .18f, .24f)); rect.sizeDelta = size; rect.anchoredPosition = position;
            var image = rect.GetComponent<UnityEngine.UI.Image>(); image.raycastTarget = true;
            var button = rect.gameObject.AddComponent<UnityEngine.UI.Button>(); button.targetGraphic = image;
            var colors = button.colors; colors.highlightedColor = new(.4f, .83f, .87f); colors.pressedColor = new(.2f, .6f, .65f); colors.disabledColor = new(.65f, .7f, .75f); button.colors = colors;
            Label("Label", rect, text, 20, White, new(18, 7), new(-12, -7)); return button;
        }
        static RectTransform ScrollContent(string name, Transform parent, Vector2 low, Vector2 high)
        {
            var rect = Rect(name, parent, Vector2.zero, Vector2.one, low, high); var scroll = rect.gameObject.AddComponent<UnityEngine.UI.ScrollRect>(); scroll.horizontal = false; scroll.movementType = UnityEngine.UI.ScrollRect.MovementType.Clamped;
            var viewport = Box("Viewport", rect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new(1, 1, 1, .01f)); viewport.gameObject.AddComponent<UnityEngine.UI.Mask>().showMaskGraphic = false;
            var content = Rect("Content", viewport, new(0, 1), new(1, 1), Vector2.zero, Vector2.zero); content.pivot = new(.5f, 1);
            var layout = content.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>(); layout.spacing = 9; layout.childControlHeight = true; layout.childControlWidth = true; layout.childForceExpandHeight = false;
            var fit = content.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>(); fit.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize; scroll.viewport = viewport; scroll.content = content; return content;
        }
        static UnityEngine.UI.Slider Progress(Transform parent)
        {
            var rect = Box("Progress", parent, new(.5f, 0), new(.5f, 0), new(-310, 77), new(310, 90), Navy);
            var fill = Box("Fill", rect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Cyan);
            var slider = rect.gameObject.AddComponent<UnityEngine.UI.Slider>(); slider.fillRect = fill; slider.interactable = false; slider.transition = UnityEngine.UI.Selectable.Transition.None; return slider;
        }
    }
}
