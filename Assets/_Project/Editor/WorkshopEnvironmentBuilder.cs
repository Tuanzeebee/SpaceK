using System;
using System.Collections.Generic;
using System.Linq;
using SpaceK.Interaction;
using SpaceK.Rendering;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace SpaceK.Editor
{
    public static class WorkshopEnvironmentBuilder
    {
        public const string RootName = "Workshop Art Pass";
        const string PrefabPath = "Assets/_Project/Prefabs/Environment/Workshop/";
        static Material paint, steel, alloy, rubber, amber, warm, cyan, concrete, earth;
        static int disabledWallChunks;

        [MenuItem("SpaceK/Workshop/Build Environment Art Pass")]
        public static void Build()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Exit Play Mode before authoring workshop assets.");
            var scene = SceneManager.GetActiveScene();
            if (scene.path != FoundationSceneBuilder.ScenePath) throw new InvalidOperationException("Open the foundation demo scene before building the workshop art pass.");
            var hangar = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "Hangar 07");
            if (hangar == null || hangar.transform.Find("Port Wall") == null) throw new InvalidOperationException("The foundation hangar was not found; no scene objects were changed.");
            WorkshopGeometry.EnsureFolders();
            Materials();
            var previous = scene.GetRootGameObjects().FirstOrDefault(g => g.name == RootName);
            if (previous != null) Object.DestroyImmediate(previous);
            var root = new GameObject(RootName).transform;
            OpenPort(hangar.transform, root);
            Place(Furniture("Tool Cabinet", Cabinet), root, new(-8, 0, -7), -12);
            Place(Furniture("Service Trolley", Trolley), root, new(-5.6f, 0, -5.8f), -18);
            Place(Furniture("Module Storage Rack", Rack), root, new(-12.4f, 0, 6.8f), -90);
            var structure = new GameObject("Structure and Roof").transform; structure.SetParent(root, false);
            Structure(structure); WorkshopGeometry.CombineStatic(structure, "HangarStructure");
            var services = new GameObject("Pipework and Cable Trays").transform; services.SetParent(root, false);
            Services(services); WorkshopGeometry.CombineStatic(services, "HangarServices");
            var bench = new GameObject("Workbench Detail Kit").transform; bench.SetParent(root, false); bench.localPosition = new(-4, 0, -3);
            BenchDetails(bench); WorkshopGeometry.CombineStatic(bench, "BenchDetailKit");
            var exterior = new GameObject("Exterior Apron").transform; exterior.SetParent(root, false);
            Exterior(exterior); WorkshopGeometry.CombineStatic(exterior, "ExteriorApron");
            var door = Furniture("Port Sliding Hangar Door", HangarDoor);
            Place(door, root, new(-16, 0, -4), 0);
            // Apply after all authored and nested renderer children exist.
            root.gameObject.AddComponent<DemoRendererCompatibility>();
            int meshCount = root.GetComponentsInChildren<MeshFilter>(true).Length;
            int largest = root.GetComponentsInChildren<MeshFilter>(true).Where(m => m.sharedMesh != null).Max(m => m.sharedMesh.vertexCount);
            if (root.GetComponentsInChildren<MeshCollider>(true).Length != 0) throw new InvalidOperationException("Workshop art must use bounds colliders only.");
            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Workshop] Authored environment. {meshCount} mesh filters, largest {largest} vertices; {root.GetComponentsInChildren<BoxCollider>(true).Length} box colliders; {disabledWallChunks} port wall renderer branches hidden. Active scene remains unsaved for review.");
        }

        static void Materials()
        {
            paint = WorkshopGeometry.Material("Workshop Navy Enamel", new(.149f, .239f, .318f), .55f, .38f);
            steel = WorkshopGeometry.Material("Workshop Dark Steel", new(.243f, .298f, .341f), .75f, .3f);
            alloy = WorkshopGeometry.Material("Workshop Machined Alloy", new(.722f, .765f, .796f), .85f, .48f);
            rubber = WorkshopGeometry.Material("Workshop Rubber", new(.027f, .039f, .047f), .05f, .24f);
            amber = WorkshopGeometry.Material("Workshop Safety Amber", new(.91f, .65f, .216f), .3f, .35f);
            concrete = WorkshopGeometry.Material("Apron Concrete", new(.31f, .35f, .36f), .08f, .22f);
            earth = WorkshopGeometry.Material("Exterior Weathered Ground", new(.38f, .36f, .30f), .02f, .18f);
            warm = WorkshopGeometry.Material("Warm Lamp Diffuser", new(1, .83f, .55f), .1f, .3f);
            warm.EnableKeyword("_EMISSION"); warm.SetColor("_EmissionColor", new Color(1, .73f, .37f) * 3); EditorUtility.SetDirty(warm);
            cyan = WorkshopGeometry.Material("Skylight Blue", new(.43f, .69f, .78f), .1f, .6f);
            cyan.EnableKeyword("_EMISSION"); cyan.SetColor("_EmissionColor", new Color(.35f, .62f, .73f) * .65f); EditorUtility.SetDirty(cyan);
        }

        static GameObject Box(string n, Transform p, Vector3 at, Vector3 size, Material m, float bevel = .015f) => WorkshopGeometry.Box(n, p, at, size, m, bevel);
        static GameObject Cyl(string n, Transform p, Vector3 at, float radius, float height, Material m, int sides = 16) => WorkshopGeometry.Cylinder(n, p, at, radius, height, m, sides);
        static void BoundsCollider(Transform parent, string name, Vector3 center, Vector3 size)
        {
            var go = new GameObject(name, typeof(BoxCollider)); go.transform.SetParent(parent, false); go.transform.localPosition = center; go.GetComponent<BoxCollider>().size = size;
        }
        static GameObject Furniture(string name, Action<Transform> build)
        {
            var go = new GameObject(name);
            try
            {
                build(go.transform);
                go.AddComponent<DemoRendererCompatibility>();
                return PrefabUtility.SaveAsPrefabAsset(go, PrefabPath + name + ".prefab");
            }
            finally { Object.DestroyImmediate(go); }
        }
        static void Place(GameObject prefab, Transform parent, Vector3 position, float yaw)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.transform.localPosition = position; instance.transform.localRotation = Quaternion.Euler(0, yaw, 0);
        }
        static void Bolt(Transform parent, Vector3 position, bool faceForward = true, float radius = .035f)
        {
            var bolt = Cyl("Hex fastener", parent, position, radius, .022f, alloy, 6);
            if (faceForward) bolt.transform.localRotation = Quaternion.Euler(90, 0, 0);
        }
        static void Label(Transform parent, string text, Vector3 position, float width, float height, float fontSize = 1)
        {
            var go = new GameObject(text); go.transform.SetParent(parent, false); go.transform.localPosition = position;
            var label = go.AddComponent<TextMeshPro>();
            label.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            label.text = text; label.fontSize = fontSize; label.color = new Color(.8f, .85f, .87f); label.alignment = TextAlignmentOptions.Center;
            label.rectTransform.sizeDelta = new Vector2(width, height);
            // Cabinet fronts face the player approaching from negative Z.
            go.transform.localRotation = Quaternion.identity;
        }

        static void Cabinet(Transform root)
        {
            Box("Folded cabinet shell", root, new(0, .98f, 0), new(2.1f, 1.8f, .8f), paint, .045f);
            Box("Rubber plinth", root, new(0, .12f, 0), new(2.02f, .16f, .76f), rubber);
            Box("Stainless work surface", root, new(0, 1.93f, 0), new(2.22f, .12f, .94f), alloy, .035f);
            Box("Toolboard back", root, new(0, 2.45f, .35f), new(2.15f, .95f, .09f), steel);
            for (int row = 0; row < 4; row++)
            {
                float y = .48f + row * .36f;
                Box("Recessed drawer reveal", root, new(-.36f, y, -.416f), new(1.25f, .34f, .035f), rubber);
                Box("Formed drawer front", root, new(-.36f, y, -.442f), new(1.19f, .29f, .055f), row == 3 ? amber : paint);
                Box("Extruded pull", root, new(-.36f, y + .065f, -.5f), new(.92f, .042f, .065f), alloy, .012f);
                Box("Drawer index", root, new(-.78f, y -.07f, -.474f), new(.18f, .045f, .008f), alloy, .002f);
            }
            Box("Tall locker reveal", root, new(.68f, .98f, -.416f), new(.62f, 1.42f, .035f), rubber);
            Box("Tall locker face", root, new(.68f, .98f, -.444f), new(.57f, 1.36f, .055f), paint);
            Box("Locker handle", root, new(.46f, 1.06f, -.505f), new(.05f, .3f, .07f), alloy);
            for (int i = 0; i < 5; i++) Box("Locker vent", root, new(.74f, 1.48f + i * .045f, -.48f), new(.28f, .018f, .01f), rubber, .003f);
            for (int x = 0; x < 12; x++) for (int y = 0; y < 4; y++)
                Box("Pegboard perforation", root, new(-.96f + x * .175f, 2.14f + y * .19f, .298f), new(.025f, .025f, .008f), rubber, .004f);
            for (int i = 0; i < 5; i++)
            {
                float x = -.75f + i * .32f;
                Box("Tool retaining hook", root, new(x, 2.56f, .23f), new(.045f, .08f, .12f), alloy);
                Box("Hanging tool handle", root, new(x, 2.3f, .19f), new(.075f, .32f, .08f), i % 2 == 0 ? amber : rubber);
                Box("Hanging tool shank", root, new(x, 2.55f, .19f), new(.04f, .2f, .04f), alloy);
                Box("Spanner jaw", root, new(x, 2.69f, .19f), new(.14f, .1f, .06f), alloy);
            }
            foreach (float x in new[] { -1f, 1f }) foreach (float y in new[] { .25f, 1.75f, 2.8f }) Bolt(root, new(x, y, -.412f));
            WorkshopGeometry.CombineStatic(root, "ToolCabinet");
            Label(root, "TOOL CONTROL  /  07", new(0, 2.86f, .285f), 1.8f, .12f, .7f);
            BoundsCollider(root, "Cabinet bounds", new(0, .97f, 0), new(2.15f, 1.94f, .94f));
        }

        static void Trolley(Transform root)
        {
            foreach (float x in new[] { -.65f, .65f }) foreach (float z in new[] { -.35f, .35f })
            {
                Box("Tubular upright", root, new(x, .66f, z), new(.055f, .88f, .055f), steel);
                Cyl("Swivel caster pin", root, new(x, .23f, z), .045f, .16f, alloy);
                Box("Caster fork", root, new(x, .16f, z), new(.07f, .16f, .19f), steel);
                var wheel = Cyl("Rubber caster wheel", root, new(x, .105f, z), .105f, .1f, rubber); wheel.transform.localRotation = Quaternion.Euler(0, 0, 90);
                var hub = Cyl("Wheel hub", root, new(x + Mathf.Sign(x) * .055f, .105f, z), .046f, .015f, alloy, 12); hub.transform.localRotation = Quaternion.Euler(0, 0, 90);
            }
            foreach (float y in new[] { .32f, 1.08f })
            {
                Box("Pressed tray", root, new(0, y, 0), new(1.5f, .055f, .86f), paint, .025f);
                foreach (float z in new[] { -.42f, .42f }) Box("Tray folded rim", root, new(0, y + .06f, z), new(1.5f, .13f, .035f), paint);
                foreach (float x in new[] { -.73f, .73f }) Box("Tray end rim", root, new(x, y + .06f, 0), new(.035f, .13f, .86f), paint);
            }
            foreach (float z in new[] { -.3f, .3f }) Box("Push handle riser", root, new(.86f, 1.13f, z), new(.045f, .35f, .045f), alloy);
            Box("Rubber push grip", root, new(.86f, 1.3f, 0), new(.075f, .07f, .66f), rubber);
            Box("Parts organizer", root, new(-.26f, 1.15f, .04f), new(.64f, .11f, .48f), amber);
            for (int i = 0; i < 3; i++) Box("Organizer divider", root, new(-.48f + i * .22f, 1.23f, .04f), new(.018f, .07f, .45f), steel, .003f);
            for (int i = 0; i < 9; i++) Bolt(root, new(-.48f + (i % 3) * .22f, 1.24f, -.08f + (i / 3) * .12f), false, .026f);
            Box("Battery case", root, new(.37f, 1.19f, .08f), new(.35f, .18f, .42f), rubber, .03f);
            Box("Battery latch", root, new(.37f, 1.29f, .08f), new(.13f, .035f, .15f), amber);
            WorkshopGeometry.CombineStatic(root, "ServiceTrolley");
            ImportedVisual(root, "Assets/_Project/Prefabs/Environment/Material Crate.prefab", new(0, .57f, .03f), .65f);
            BoundsCollider(root, "Trolley bounds", new(.05f, .65f, 0), new(1.85f, 1.3f, 1));
        }

        static void Rack(Transform root)
        {
            foreach (float x in new[] { -1.5f, 1.5f }) foreach (float z in new[] { -.63f, .63f })
            {
                Box("Steel angle upright", root, new(x, 1.75f, z), new(.1f, 3.5f, .1f), steel);
                Box("Anchored foot", root, new(x, .045f, z), new(.36f, .09f, .34f), alloy);
                for (int i = 0; i < 2; i++) Bolt(root, new(x -.1f + i * .2f, .1f, z), false);
                for (int i = 0; i < 12; i++) Box("Upright adjustment slot", root, new(x, .35f + i * .24f, z -.056f), new(.025f, .07f, .008f), rubber, .003f);
            }
            foreach (float y in new[] { .2f, 1.3f, 2.4f, 3.45f })
            {
                Box("Shelf deck", root, new(0, y, 0), new(3.1f, .075f, 1.35f), steel);
                foreach (float z in new[] { -.66f, .66f }) Box("Shelf load beam", root, new(0, y -.08f, z), new(3.13f, .18f, .08f), amber);
                for (int i = 0; i < 4; i++) Box("Load label", root, new(-1.15f + i * .76f, y -.07f, -.707f), new(.2f, .055f, .012f), alloy, .003f);
            }
            foreach (float x in new[] { -1.45f, 1.45f })
            {
                var cross = Box("Side diagonal brace", root, new(x, 1.75f, 0), new(.045f, 3.24f, .055f), alloy);
                cross.transform.localRotation = Quaternion.Euler(23, 0, 0);
            }
            WorkshopGeometry.CombineStatic(root, "ModuleRack");
            ImportedVisual(root, "Assets/_Project/Prefabs/Environment/Material Crate.prefab", new(-.77f, .52f, 0), .85f);
            ImportedVisual(root, "Assets/_Project/Prefabs/Environment/Material Crate.prefab", new(.72f, .52f, 0), .85f);
            ImportedVisual(root, "Assets/_Project/Prefabs/Modules/Thruster Module.prefab", new(-.78f, 1.7f, 0), .48f);
            ImportedVisual(root, "Assets/_Project/Prefabs/Modules/Fuel Tank.prefab", new(.73f, 1.8f, 0), .45f);
            ImportedVisual(root, "Assets/_Project/Prefabs/Modules/Nose Cone.prefab", new(-.7f, 2.8f, 0), .46f);
            ImportedVisual(root, "Assets/_Project/Prefabs/Modules/Stabilizer Fin.prefab", new(.7f, 2.8f, 0), .48f);
            BoundsCollider(root, "Rack bounds", new(0, 1.75f, 0), new(3.3f, 3.5f, 1.5f));
        }

        static void ImportedVisual(Transform root, string path, Vector3 position, float scale)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) { Debug.LogWarning("[Workshop] Optional display art missing: " + path); return; }
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root);
            instance.name = "Display / " + prefab.name; instance.transform.localPosition = position; instance.transform.localScale = Vector3.one * scale;
            foreach (var collider in instance.GetComponentsInChildren<Collider>(true)) { collider.enabled = false; PrefabUtility.RecordPrefabInstancePropertyModifications(collider); }
        }

        static void OpenPort(Transform hangar, Transform root)
        {
            disabledWallChunks = 0;
            var port = hangar.Find("Port Wall");
            foreach (var collider in port.GetComponents<Collider>()) { collider.enabled = false; PrefabUtility.RecordPrefabInstancePropertyModifications(collider); }
            // Identify complete imported LOD branches by their actual world-space pivots.
            foreach (var group in hangar.GetComponentsInChildren<LODGroup>(true))
            {
                Vector3 p = group.transform.position;
                if (Mathf.Abs(p.x + 15.8f) > .3f || Mathf.Abs(p.y) > .3f || p.z < -11.9f || p.z > 3.9f) continue;
                group.enabled = false; PrefabUtility.RecordPrefabInstancePropertyModifications(group);
                foreach (var renderer in group.GetComponentsInChildren<Renderer>(true)) { renderer.enabled = false; PrefabUtility.RecordPrefabInstancePropertyModifications(renderer); }
                disabledWallChunks++;
            }
            foreach (Transform child in hangar)
            {
                Vector3 p = child.position;
                if (p.x < -14.9f && p.x > -15.9f && p.z > -12.1f && p.z < 4 && p.y > .5f && p.y < 7.7f)
                {
                    foreach (var renderer in child.GetComponentsInChildren<Renderer>()) renderer.enabled = false;
                    foreach (var collider in child.GetComponentsInChildren<Collider>()) collider.enabled = false;
                }
            }
            BoundsCollider(root, "Port wall south of opening", new(-16, 4, -16), new(.5f, 8, 8));
            BoundsCollider(root, "Port wall north of opening", new(-16, 4, 12), new(.5f, 8, 16));
            BoundsCollider(root, "Port wall above opening", new(-16, 7.5f, -4), new(.5f, 1, 16));
            if (disabledWallChunks != 2) Debug.LogWarning($"[Workshop] Expected two imported port wall chunks; identified {disabledWallChunks}. Inspect aperture before saving.");
        }

        static void IColumn(Transform root, Vector3 position, float height)
        {
            var column = new GameObject("Bolted I section").transform; column.SetParent(root, false); column.localPosition = position;
            Box("Web", column, new(0, height * .5f, 0), new(.14f, height, .52f), steel);
            foreach (float z in new[] { -.3f, .3f }) Box("Rolled flange", column, new(0, height * .5f, z), new(.58f, height, .12f), steel);
            Box("Column baseplate", column, new(0, .08f, 0), new(.85f, .16f, .9f), alloy);
            foreach (float x in new[] { -.3f, .3f }) foreach (float z in new[] { -.32f, .32f }) Bolt(column, new(x, .17f, z), false, .055f);
            Box("Gusset front", column, new(0, .42f, -.2f), new(.38f, .5f, .08f), steel);
            Box("Amber collision guard", column, new(0, .55f, -.38f), new(.64f, .85f, .045f), amber);
            for (int i = 0; i < 4; i++)
            {
                var stripe = Box("Guard diagonal", column, new(-.22f + i * .14f, .55f, -.408f), new(.065f, .7f, .012f), rubber, .003f);
                stripe.transform.localRotation = Quaternion.Euler(0, 0, -16);
            }
        }

        static void Structure(Transform root)
        {
            foreach (float z in new[] { -17f, 7f, 17f }) IColumn(root, new(-14.85f, 0, z), 7.9f);
            foreach (float z in new[] { -17f, -7f, 7f, 17f }) IColumn(root, new(14.85f, 0, z), 7.9f);
            for (int z = -17; z <= 17; z += 8)
            {
                Box("Roof girder web", root, new(0, 7.91f, z), new(30.2f, .42f, .12f), steel);
                foreach (float y in new[] { 7.67f, 8.15f }) Box("Roof girder flange", root, new(0, y, z), new(30.2f, .09f, .46f), alloy);
                foreach (float x in new[] { -12f, -6f, 0f, 6f, 12f })
                {
                    Box("Girder stiffener", root, new(x, 7.91f, z), new(.1f, .42f, .43f), steel);
                    Bolt(root, new(x, 7.94f, z -.24f));
                }
            }
            // Thin roof sections stay entirely above 8.1m; strips have visible blue glazing and mullions.
            for (int z = -16; z <= 16; z += 8)
            {
                foreach (float x in new[] { -13f, -4f, 4f, 13f })
                {
                    float width = Mathf.Abs(x) > 10 ? 5.8f : 7.8f;
                    Box("Roof cassette", root, new(x, 8.24f, z), new(width, .18f, 7.95f), paint);
                    for (int rib = 0; rib < 5; rib++) Box("Roof standing seam", root, new(x - width * .42f + rib * width * .21f, 8.12f, z), new(.045f, .035f, 7.9f), steel, .006f);
                }
                foreach (float x in new[] { -8.8f, 8.8f })
                {
                    Box("Skylight glazing", root, new(x, 8.24f, z), new(2.4f, .11f, 7.9f), cyan);
                    foreach (float edge in new[] { -1.22f, 1.22f }) Box("Skylight side rail", root, new(x + edge, 8.17f, z), new(.08f, .12f, 7.96f), alloy);
                    for (int m = -3; m <= 3; m += 2) Box("Skylight cross mullion", root, new(x, 8.15f, z + m), new(2.5f, .13f, .065f), alloy);
                }
            }
            foreach (float x in new[] { -9f, 3f, 11f }) foreach (float z in new[] { -14f, -3f, 9f }) Lamp(root, new(x, 7.25f, z));
        }

        static void Lamp(Transform root, Vector3 position)
        {
            var fixture = new GameObject("Industrial pendant luminaire").transform; fixture.SetParent(root, false); fixture.localPosition = position;
            foreach (float x in new[] { -.8f, .8f }) Cyl("Suspension rod", fixture, new(x, .42f, 0), .018f, .8f, alloy, 8);
            Box("Cast lamp housing", fixture, Vector3.zero, new(2.05f, .2f, .56f), steel, .06f);
            Box("Warm prismatic diffuser", fixture, new(0, -.12f, 0), new(1.8f, .07f, .41f), warm, .025f);
            foreach (float z in new[] { -.27f, .27f }) Box("Lamp guard rail", fixture, new(0, -.18f, z), new(2, .035f, .035f), alloy, .009f);
            for (int i = -4; i <= 4; i++) Box("Wire guard rib", fixture, new(i * .22f, -.19f, 0), new(.018f, .025f, .53f), alloy, .005f);
            foreach (float x in new[] { -.96f, .96f }) Bolt(fixture, new(x, -.015f, -.29f), true, .03f);
            // Parent owns illumination; these are detailed emissive fixtures with no additional realtime lights.
        }

        static void Pipe(Transform root, Vector3 from, Vector3 to, float radius, Material material)
        {
            Vector3 delta = to - from;
            var pipe = Cyl("Service pipe", root, (from + to) * .5f, radius, delta.magnitude, material, 12);
            pipe.transform.localRotation = Quaternion.FromToRotation(Vector3.up, delta.normalized);
            int supports = Mathf.Max(1, Mathf.FloorToInt(delta.magnitude / 2.2f));
            for (int i = 0; i <= supports; i++)
            {
                Vector3 p = Vector3.Lerp(from, to, i / (float)supports);
                var collar = Cyl("Pipe union collar", root, p, radius * 1.4f, .1f, alloy, 12); collar.transform.localRotation = pipe.transform.localRotation;
                Box("Wall pipe saddle", root, p + new Vector3(-.1f, 0, 0), new(.11f, radius * 2.8f, .2f), steel);
            }
        }
        static void Services(Transform root)
        {
            foreach (float y in new[] { 2.8f, 3.25f })
            {
                Pipe(root, new(-15.12f, y, 5.2f), new(-15.12f, y, 18.2f), y < 3 ? .085f : .055f, y < 3 ? alloy : amber);
                Pipe(root, new(-15.12f, y, 18.2f), new(-15.12f, 7.5f, 18.2f), y < 3 ? .085f : .055f, y < 3 ? alloy : amber);
            }
            Pipe(root, new(-14.9f, 7.55f, -18), new(-14.9f, 7.55f, 18), .07f, steel);
            Pipe(root, new(-14.62f, 7.55f, -18), new(-14.62f, 7.55f, 18), .04f, rubber);
            foreach (float z in new[] { 8f, 16f })
            {
                Box("Cable tray channel", root, new(-14.88f, 5.5f, z), new(.48f, .08f, 7.5f), steel);
                foreach (float x in new[] { -15.08f, -14.68f }) Box("Cable tray raised edge", root, new(x, 5.6f, z), new(.04f, .2f, 7.5f), alloy);
                for (int i = -3; i <= 3; i++)
                {
                    Box("Tray rung", root, new(-14.88f, 5.52f, z + i), new(.43f, .035f, .05f), alloy);
                    Box("Tray wall bracket", root, new(-15.2f, 5.38f, z + i), new(.8f, .07f, .12f), steel);
                }
                for (int i = 0; i < 3; i++) Pipe(root, new(-15 + i * .12f, 5.59f, z - 3.7f), new(-15 + i * .12f, 5.59f, z + 3.7f), .025f, i == 0 ? amber : rubber);
            }
            Box("Service junction cabinet", root, new(-15.05f, 1.6f, 10.1f), new(.4f, 1.2f, .8f), paint, .04f);
            Box("Junction warning plate", root, new(-14.83f, 1.75f, 10.1f), new(.02f, .3f, .35f), amber);
            Pipe(root, new(-15.05f, 2.2f, 10.1f), new(-15.05f, 3.3f, 10.1f), .045f, rubber);
        }

        static void BenchDetails(Transform root)
        {
            Box("Bench rubber service mat", root, new(-.62f, 1.14f, -.12f), new(.76f, .025f, .56f), rubber, .018f);
            for (int i = 0; i < 5; i++) Box("Mat grip channel", root, new(-.9f + i * .14f, 1.156f, -.12f), new(.015f, .006f, .48f), steel, .002f);
            Box("Parts tray", root, new(.6f, 1.15f, -.4f), new(.52f, .07f, .28f), alloy);
            for (int i = 0; i < 6; i++) Bolt(root, new(.42f + i % 3 * .16f, 1.2f, -.46f + i / 3 * .13f), false, .026f);
            Box("Torque wrench grip", root, new(-.66f, 1.19f, -.26f), new(.09f, .07f, .32f), amber);
            Box("Torque wrench shaft", root, new(-.66f, 1.2f, .02f), new(.04f, .04f, .3f), alloy);
            Cyl("Torque wrench ratchet", root, new(-.66f, 1.2f, .2f), .07f, .045f, alloy, 12);
        }

        static void HangarDoor(Transform root)
        {
            foreach (float z in new[] { -8.4f, 8.4f })
            {
                Box("Door jamb", root, new(0, 3.7f, z), new(.85f, 7.4f, .68f), steel, .04f);
                Box("Door guide track", root, new(.49f, 4.1f, z), new(.1f, 8.2f, .2f), alloy);
                Box("Jamb warning guard", root, new(.47f, .7f, z), new(.06f, 1.4f, .52f), amber);
                for (int i = 0; i < 5; i++)
                {
                    var stripe = Box("Warning guard stripe", root, new(.51f, .18f + i * .28f, z), new(.025f, .12f, .52f), rubber, .004f);
                    stripe.transform.localRotation = Quaternion.Euler(18, 0, 0);
                }
                for (int i = 0; i < 6; i++)
                {
                    var bolt = Cyl("Jamb anchor", root, new(.47f, .35f + i * 1.2f, z), .065f, .035f, alloy, 6); bolt.transform.localRotation = Quaternion.Euler(0, 0, 90);
                }
            }
            Box("Door lintel", root, new(0, 7.47f, 0), new(.85f, .78f, 17.4f), steel, .04f);
            Box("Lintel amber identity strip", root, new(.44f, 7.48f, 0), new(.045f, .22f, 15.7f), amber);
            Box("Door threshold", root, new(0, .015f, 0), new(1.7f, .03f, 16), steel, .008f);
            for (int i = -7; i <= 7; i++) Box("Threshold traction", root, new(.35f, .033f, i), new(.8f, .009f, .12f), amber, .002f);
            WorkshopGeometry.CombineStatic(root, "PortDoorFrame");
            var panel = new GameObject("Retracting Hangar Panel").transform; panel.SetParent(root, false); panel.localPosition = new(0, 10.9f, 0);
            Box("Insulated sliding panel", panel, Vector3.zero, new(.27f, 6.96f, 15.8f), paint, .05f);
            for (int i = -7; i <= 7; i++)
            {
                Box("Door stiffening rib", panel, new(.17f, 0, i), new(.12f, 6.7f, .07f), steel);
                for (int row = -2; row <= 2; row++)
                {
                    var bolt = Cyl("Panel rivet", panel, new(.24f, row * 1.3f, i), .035f, .02f, alloy, 6); bolt.transform.localRotation = Quaternion.Euler(0, 0, 90);
                }
            }
            foreach (float y in new[] { -3.35f, 3.35f }) Box("Door edge beam", panel, new(.12f, y, 0), new(.28f, .18f, 15.7f), alloy);
            WorkshopGeometry.CombineStatic(panel, "PortSlidingPanel");
            panel.gameObject.AddComponent<BoxCollider>().size = new(.3f, 6.96f, 15.8f);
            var control = Box("Hangar Door Control", root, new(.8f, 1.3f, 8.7f), new(.38f, .64f, .34f), paint, .04f);
            control.AddComponent<BoxCollider>().size = new(.38f, .64f, .34f);
            Box("Control status indicator", control.transform, new(.205f, .17f, 0), new(.025f, .12f, .22f), cyan);
            var button = Cyl("Hangar open button", control.transform, new(.22f, -.1f, 0), .09f, .04f, amber, 16); button.transform.localRotation = Quaternion.Euler(0, 0, 90);
            var door = control.AddComponent<WorkshopHangarDoor>(); door.Panel = panel; door.ClosedLocalPosition = new(0, 3.5f, 0); door.OpenOffset = new(0, 7.4f, 0); door.StartsOpen = true;
            BoundsCollider(root, "South jamb bounds", new(0, 3.7f, -8.4f), new(.85f, 7.4f, .68f));
            BoundsCollider(root, "North jamb bounds", new(0, 3.7f, 8.4f), new(.85f, 7.4f, .68f));
        }

        static void Exterior(Transform root)
        {
            Box("Exterior concrete apron", root, new(-38, -.25f, 0), new(44, .5f, 50), concrete, .04f);
            BoundsCollider(root, "Walkable exterior apron", new(-38, -.25f, 0), new(44, .5f, 50));
            for (int x = -58; x <= -18; x += 5) Box("Concrete expansion joint", root, new(x, .006f, 0), new(.035f, .01f, 49.8f), rubber, .003f);
            for (int z = -20; z <= 20; z += 5) Box("Concrete expansion joint", root, new(-38, .006f, z), new(43.8f, .01f, .035f), rubber, .003f);
            foreach (float z in new[] { -11.3f, 3.3f })
                for (int x = -57; x <= -18; x += 4) Box("Apron guidance dash", root, new(x, .016f, z), new(2, .012f, .14f), amber, .003f);
            foreach (float z in new[] { -24f, 24f })
            {
                BoundsCollider(root, "Apron side perimeter bounds", new(-37.85f, 1.4f, z), new(43.7f, 2.8f, .16f));
                for (int x = -59; x <= -19; x += 5)
                {
                    Box("Fence stanchion", root, new(x, 1.4f, z), new(.09f, 2.8f, .09f), steel);
                    Box("Fence foot", root, new(x, .06f, z), new(.35f, .12f, .35f), alloy);
                    for (int wire = 0; wire < 6; wire++) Box("Perimeter cable", root, new(x + 2.5f, .4f + wire * .42f, z), new(5, .016f, .016f), steel, .003f);
                }
            }
            BoundsCollider(root, "Apron west perimeter bounds", new(-59.7f, 1.4f, 0), new(.16f, 2.8f, 48));
            for (int z = -24; z <= 24; z += 6)
            {
                Box("West fence stanchion", root, new(-59.7f, 1.4f, z), new(.1f, 2.8f, .1f), steel);
                Box("West fence anchor plate", root, new(-59.7f, .06f, z), new(.36f, .12f, .36f), alloy);
                Box("West guardrail post", root, new(-59.45f, .65f, z), new(.11f, 1.3f, .11f), steel);
            }
            foreach (float y in new[] { .55f, 1.18f }) Box("West safety guardrail", root, new(-59.45f, y, 0), new(.1f, .12f, 48), amber, .025f);
            foreach (float y in new[] { .4f, .82f, 1.24f, 1.66f, 2.08f, 2.5f }) Box("West perimeter cable", root, new(-59.7f, y, 0), new(.018f, .018f, 48), steel, .003f);
            TerrainBackdrop(root);
            foreach (float z in new[] { -13.4f, 5.4f })
            {
                Cyl("Exterior bollard", root, new(-18.5f, .48f, z), .12f, .96f, amber, 12);
                Cyl("Bollard rubber ring", root, new(-18.5f, .65f, z), .125f, .12f, rubber, 12);
                BoundsCollider(root, "Bollard bounds", new(-18.5f, .48f, z), new(.25f, .96f, .25f));
            }
        }

        static void TerrainBackdrop(Transform root)
        {
            // Continuous ground prevents gaps beneath the horizon; it is visual-only beyond the fenced apron.
            const int xSegments = 78, zSegments = 100;
            const string path = WorkshopGeometry.ArtPath + "/Meshes/ExteriorTerrainBackdrop.asset";
            var vertices = new Vector3[(xSegments + 1) * (zSegments + 1)];
            var uv = new Vector2[vertices.Length];
            var triangles = new int[xSegments * zSegments * 6];
            for (int zi = 0; zi <= zSegments; zi++)
                for (int xi = 0; xi <= xSegments; xi++)
                {
                    float x = Mathf.Lerp(-250, -16, xi / (float)xSegments);
                    float z = Mathf.Lerp(-200, 200, zi / (float)zSegments);
                    float distanceBlend = Mathf.SmoothStep(0, 1, Mathf.Clamp01((-x - 65) / 100));
                    float broadRidge = Mathf.PerlinNoise(11.3f + x * .012f, 29.7f + z * .013f);
                    float detail = Mathf.PerlinNoise(41.2f + x * .029f, 7.6f + z * .024f);
                    float height = -.4f + distanceBlend * (2 + broadRidge * 12 + detail * 2.5f);
                    int index = zi * (xSegments + 1) + xi;
                    vertices[index] = new Vector3(x, height, z); uv[index] = new Vector2(x, z) * .025f;
                }
            int next = 0;
            for (int zi = 0; zi < zSegments; zi++)
                for (int xi = 0; xi < xSegments; xi++)
                {
                    int a = zi * (xSegments + 1) + xi, b = a + xSegments + 1;
                    triangles[next++] = a; triangles[next++] = b; triangles[next++] = a + 1;
                    triangles[next++] = a + 1; triangles[next++] = b; triangles[next++] = b + 1;
                }
            var authored = new Mesh { name = "Exterior Terrain Backdrop" };
            authored.vertices = vertices; authored.uv = uv; authored.triangles = triangles;
            authored.RecalculateNormals(); authored.RecalculateTangents(); authored.RecalculateBounds();
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh == null) { mesh = authored; AssetDatabase.CreateAsset(mesh, path); }
            else { EditorUtility.CopySerialized(authored, mesh); Object.DestroyImmediate(authored); EditorUtility.SetDirty(mesh); }
            var terrain = new GameObject("Continuous exterior terrain", typeof(MeshFilter), typeof(MeshRenderer)); terrain.transform.SetParent(root, false);
            terrain.GetComponent<MeshFilter>().sharedMesh = mesh; terrain.GetComponent<MeshRenderer>().sharedMaterial = earth;
        }
    }
}
