using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SpaceK.Building;
using SpaceK.CameraSystem;
using SpaceK.Interaction;
using SpaceK.Player;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace SpaceK.Editor
{
    public static class WorkshopHandsBuilder
    {
        const string Art = "Assets/_Project/Art/Characters/WorkshopHands";
        const string PrefabPath = "Assets/_Project/Prefabs/Player/Workshop Hands.prefab";
        const string Animations = "Assets/_Project/Animations/WorkshopHands";
        static Transform Bone(GameObject rig, string name) => rig.GetComponentsInChildren<Transform>().Single(t => t.name == name);

        public static void Build()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Build workshop hands in Edit Mode.");
            Directory.CreateDirectory(Animations); AssetDatabase.Refresh(); WorkshopGeometry.EnsureFolders();
            var importer = (ModelImporter)AssetImporter.GetAtPath(Art + "/arms.fbx");
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.optimizeGameObjects = false; importer.importAnimation = false; importer.isReadable = true;
            importer.materialImportMode = ModelImporterMaterialImportMode.None; importer.SaveAndReimport();
            int layer = ViewmodelLayer();
            var root = new GameObject("Workshop Hands");
            try
            {
                var camera = new GameObject("Hands Camera", typeof(UnityEngine.Camera)).GetComponent<UnityEngine.Camera>();
                camera.transform.SetParent(root.transform, false); camera.fieldOfView = 58; camera.nearClipPlane = .01f; camera.farClipPlane = 3;
                camera.cullingMask = 1 << layer; camera.enabled = false; camera.allowHDR = true;
                camera.GetUniversalAdditionalCameraData().renderType = CameraRenderType.Overlay;
                var motion = new GameObject("Motion").transform; motion.SetParent(camera.transform, false);
                var pose = new GameObject("Pose").transform; pose.SetParent(motion, false);
                var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Art + "/arms.fbx"), pose);
                model.name = "Rig"; model.transform.localScale = Vector3.one * .08f;
                model.transform.localRotation = Quaternion.Euler(0, 180, 0); model.transform.localPosition = new Vector3(0, -.22f, .04f);
                Restyle(model);
                var right = Bone(model, "wrist.r"); var left = Bone(model, "wrist.l");
                Pose(model, root.transform, true, 0, WorkshopAction.Idle);
                var toolSocket = new GameObject("Driver Grip").transform;
                toolSocket.position = right.position + new Vector3(-.035f, .075f, .035f); toolSocket.rotation = Quaternion.Euler(0, -18, 0);
                toolSocket.SetParent(right, true); toolSocket.localScale = Vector3.one / right.lossyScale.x;
                var tool = Driver(toolSocket);
                var carry = new GameObject("Component Grip").transform;
                carry.position = left.position + new Vector3(.065f, .055f, .035f); carry.rotation = Quaternion.Euler(0, -12, -8);
                carry.SetParent(left, true); carry.localScale = Vector3.one / left.lossyScale.x;
                var animator = root.AddComponent<Animator>(); animator.applyRootMotion = false; animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.runtimeAnimatorController = Controller(model, root, pose);
                var presenter = root.AddComponent<WorkshopHandsPresenter>();
                presenter.HandsCamera = camera; presenter.Animator = animator; presenter.MotionRoot = motion; presenter.CarrySocket = carry;
                presenter.ToolBit = tool.transform.Find("Spinning Bit"); presenter.ToolLight = tool.GetComponentInChildren<Light>();
                Pose(model, root.transform, false, 0, WorkshopAction.Idle);
                foreach (var transform in root.GetComponentsInChildren<Transform>(true)) transform.gameObject.layer = layer;
                foreach (var renderer in root.GetComponentsInChildren<Renderer>(true)) { renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false; }
                root.AddComponent<SpaceK.Rendering.DemoRendererCompatibility>();
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally { Object.DestroyImmediate(root); }
            InstallOnEngineer(layer);
            AssetDatabase.SaveAssets();
        }

        static int ViewmodelLayer()
        {
            int layer = LayerMask.NameToLayer("WorkshopHands"); if (layer >= 0) return layer;
            var settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers = settings.FindProperty("layers");
            for (int i = 31; i >= 8; i--)
                if (string.IsNullOrEmpty(layers.GetArrayElementAtIndex(i).stringValue))
                { layers.GetArrayElementAtIndex(i).stringValue = "WorkshopHands"; settings.ApplyModifiedProperties(); return i; }
            throw new InvalidOperationException("No spare layer for first-person hands.");
        }
        static void InstallOnEngineer(int layer)
        {
            const string path = "Assets/_Project/Prefabs/Player/Engineer.prefab";
            var engineer = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var building = engineer.GetComponent<BuildingController>();
                var old = engineer.GetComponentInChildren<WorkshopHandsPresenter>(true);
                if (old != null) Object.DestroyImmediate(old.gameObject);
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath), building.View.transform);
                instance.transform.localPosition = Vector3.zero; instance.transform.localRotation = Quaternion.identity;
                var presenter = instance.GetComponent<WorkshopHandsPresenter>();
                presenter.Building = building; presenter.CameraRig = engineer.GetComponent<PlayerCameraRig>();
                presenter.Motor = engineer.GetComponent<PlayerMotor>(); presenter.Interactor = engineer.GetComponent<PlayerInteractor>();
                building.View.cullingMask &= ~(1 << layer);
                var stack = building.View.GetUniversalAdditionalCameraData().cameraStack;
                stack.RemoveAll(c => c == null); stack.Add(presenter.HandsCamera);
                PrefabUtility.RecordPrefabInstancePropertyModifications(presenter);
                PrefabUtility.SaveAsPrefabAsset(engineer, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(engineer); }
        }
        static void Restyle(GameObject rig)
        {
            var skin = rig.GetComponentInChildren<SkinnedMeshRenderer>(); var original = skin.sharedMesh;
            var mesh = Object.Instantiate(original); mesh.name = "Workshop Gloved Arms";
            var gloves = new List<int>(); var fabric = new List<int>(); var weights = mesh.boneWeights;
            bool Gloved(int index)
            {
                var w = weights[index]; float hand = 0;
                int[] ids = { w.boneIndex0, w.boneIndex1, w.boneIndex2, w.boneIndex3 };
                float[] values = { w.weight0, w.weight1, w.weight2, w.weight3 };
                for (int i = 0; i < 4; i++) if (skin.bones[ids[i]].name.StartsWith("finger") || skin.bones[ids[i]].name.StartsWith("wrist")) hand += values[i];
                return hand > .3f;
            }
            var triangles = mesh.triangles;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                var list = Gloved(triangles[i]) || Gloved(triangles[i + 1]) || Gloved(triangles[i + 2]) ? gloves : fabric;
                list.Add(triangles[i]); list.Add(triangles[i + 1]); list.Add(triangles[i + 2]);
            }
            mesh.subMeshCount = 2; mesh.SetTriangles(gloves, 0); mesh.SetTriangles(fabric, 1); mesh.RecalculateTangents();
            string path = Art + "/Gloved Arms.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null) { EditorUtility.CopySerialized(mesh, existing); Object.DestroyImmediate(mesh); mesh = existing; }
            else AssetDatabase.CreateAsset(mesh, path);
            skin.sharedMesh = mesh; skin.updateWhenOffscreen = true;
            skin.sharedMaterials = new[] { WorkshopGeometry.Material("Workshop Glove Rubber", new Color(.05f, .06f, .065f), .03f, .23f), WorkshopGeometry.Material("Workshop Sleeve Fabric", new Color(.58f, .24f, .075f), 0, .15f) };
            skin.sharedMaterials[0].SetColor("_BaseColor", new Color(.05f, .06f, .065f));
            skin.sharedMaterials[1].SetColor("_BaseColor", new Color(.58f, .24f, .075f));
            var weave = FabricNormal();
            foreach (var material in skin.sharedMaterials)
            {
                material.SetTexture("_BumpMap", weave); material.EnableKeyword("_NORMALMAP"); material.SetFloat("_BumpScale", .3f);
                material.SetTextureScale("_BumpMap", new Vector2(4, 4)); EditorUtility.SetDirty(material);
            }
            var padding = WorkshopGeometry.Material("Glove Reinforcement", new Color(.085f, .10f, .11f), .08f, .28f);
            var band = WorkshopGeometry.Material("Reflective Cuff", new Color(.68f, .71f, .69f), .12f, .36f);
            foreach (string side in new[] { "l", "r" })
            {
                var wrist = Bone(rig, "wrist." + side); float scale = wrist.lossyScale.x;
                WorkshopGeometry.Box("Padded Knuckles", wrist, new Vector3(side == "r" ? -.0024f : .0024f, .0064f, 0), new Vector3(.0018f, .0045f, .0065f), padding, .0005f);
                WorkshopGeometry.Cylinder("Cuff Band", wrist, new Vector3(0, -.0012f, 0), .045f / scale, .025f / scale, band, 16);
                WorkshopGeometry.Cylinder("Glove Wrist Seal", wrist, new Vector3(0, -.0002f, 0), .046f / scale, .015f / scale, padding, 16);
            }
        }
        static Texture2D FabricNormal()
        {
            const string path = Art + "/Glove Weave Normal.png";
            if (!File.Exists(path))
            {
                var texture = new Texture2D(128, 128, TextureFormat.RGB24, false, true);
                var pixels = new Color[128 * 128];
                for (int y = 0; y < 128; y++) for (int x = 0; x < 128; x++)
                {
                    float nx = Mathf.Sin(x * Mathf.PI * .5f) * .12f, ny = Mathf.Sin(y * Mathf.PI * .5f) * .12f;
                    pixels[y * 128 + x] = new Color(nx * .5f + .5f, ny * .5f + .5f, Mathf.Sqrt(1 - nx * nx - ny * ny) * .5f + .5f);
                }
                texture.SetPixels(pixels); texture.Apply(); File.WriteAllBytes(path, texture.EncodeToPNG()); Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            }
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.NormalMap; importer.sRGBTexture = false; importer.mipmapEnabled = true;
            importer.wrapMode = TextureWrapMode.Repeat; importer.filterMode = FilterMode.Trilinear; importer.anisoLevel = 4; importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
        static GameObject Driver(Transform parent)
        {
            var tool = new GameObject("Electric Fastening Driver"); tool.transform.SetParent(parent, false);
            var paint = WorkshopGeometry.Material("Driver Ochre", new Color(.65f, .29f, .055f), .15f, .34f);
            var rubber = WorkshopGeometry.Material("Driver Rubber", new Color(.025f, .035f, .04f), .05f, .25f);
            var metal = WorkshopGeometry.Material("Driver Steel", new Color(.28f, .32f, .35f), .85f, .43f);
            WorkshopGeometry.Box("Motor Housing", tool.transform, Vector3.zero, new Vector3(.10f, .08f, .14f), paint, .014f);
            WorkshopGeometry.Box("Rear Cap", tool.transform, new Vector3(0, 0, -.077f), new Vector3(.089f, .07f, .025f), rubber, .008f);
            WorkshopGeometry.Box("Rubber Handle", tool.transform, new Vector3(0, -.075f, -.015f), new Vector3(.052f, .11f, .055f), rubber, .009f);
            WorkshopGeometry.Box("Battery Pack", tool.transform, new Vector3(0, -.14f, -.015f), new Vector3(.087f, .04f, .075f), paint, .007f);
            WorkshopGeometry.Box("Battery Foot", tool.transform, new Vector3(0, -.163f, -.015f), new Vector3(.09f, .014f, .08f), rubber, .003f);
            WorkshopGeometry.Box("Trigger", tool.transform, new Vector3(0, -.045f, .021f), new Vector3(.034f, .03f, .018f), paint, .004f);
            for (int side = -1; side <= 1; side += 2)
            {
                for (int i = 0; i < 5; i++) WorkshopGeometry.Box("Motor Vent", tool.transform, new Vector3(side * .0505f, -.015f + i * .007f, -.034f), new Vector3(.002f, .003f, .025f), rubber, .0006f);
                WorkshopGeometry.Box("Side Plate", tool.transform, new Vector3(side * .051f, .012f, .015f), new Vector3(.003f, .02f, .047f), rubber, .001f);
            }
            var collar = WorkshopGeometry.Cylinder("Torque Collar", tool.transform, new Vector3(0, 0, .086f), .034f, .036f, rubber, 24); collar.transform.localRotation = Quaternion.Euler(90, 0, 0);
            var chuck = WorkshopGeometry.Cylinder("Chuck", tool.transform, new Vector3(0, 0, .116f), .021f, .03f, metal, 20); chuck.transform.localRotation = Quaternion.Euler(90, 0, 0);
            var bit = new GameObject("Spinning Bit").transform; bit.SetParent(tool.transform, false); bit.localPosition = new Vector3(0, 0, .151f);
            var shaft = WorkshopGeometry.Cylinder("Hex Bit", bit, Vector3.zero, .0045f, .047f, metal, 6); shaft.transform.localRotation = Quaternion.Euler(90, 0, 0);
            var light = new GameObject("Task LED").AddComponent<Light>(); light.transform.SetParent(tool.transform, false); light.transform.localPosition = new Vector3(0, -.025f, .10f);
            light.type = LightType.Point; light.color = new Color(.7f, .87f, 1); light.intensity = .45f; light.range = .7f; light.enabled = false;
            return tool;
        }
        static void Solve(Transform upper, Transform lower, Transform wrist, Vector3 target, Vector3 pole)
        {
            Vector3 origin = upper.position; float a = Vector3.Distance(origin, lower.position), b = Vector3.Distance(lower.position, wrist.position);
            Vector3 axis = (target - origin).normalized; float distance = Mathf.Clamp(Vector3.Distance(origin, target), Mathf.Abs(a - b) + .001f, a + b - .001f);
            float x = (a * a - b * b + distance * distance) / (2 * distance);
            Vector3 bend = Vector3.ProjectOnPlane(pole - origin, axis).normalized;
            Vector3 elbow = origin + axis * x + bend * Mathf.Sqrt(Mathf.Max(0, a * a - x * x));
            upper.rotation = Quaternion.FromToRotation(lower.position - origin, elbow - origin) * upper.rotation;
            lower.rotation = Quaternion.FromToRotation(wrist.position - lower.position, target - lower.position) * lower.rotation;
        }
        static void Pose(GameObject rig, Transform root, bool carrying, float phase, WorkshopAction action)
        {
            float pulse = Mathf.Sin(phase * Mathf.PI);
            Vector3 right = new(.18f, -.15f, .43f), left = carrying ? new(-.16f, -.15f, .45f) : new(-.21f, -.28f, .32f);
            if (action == WorkshopAction.Install || action == WorkshopAction.Remove) { right += new Vector3(-.025f, .025f, .10f) * pulse; left += Vector3.forward * .06f * pulse; }
            if (action == WorkshopAction.Pickup) left += new Vector3(.04f, .05f, .12f) * pulse;
            float breathe = action == WorkshopAction.Idle ? Mathf.Sin(phase * Mathf.PI * 2) * .003f : 0;
            right.y += breathe; left.y += breathe;
            foreach (string side in new[] { "l", "r" })
            {
                var wrist = Bone(rig, "wrist." + side);
                var target = root.TransformPoint(side == "r" ? right : left);
                Solve(Bone(rig, "bicep." + side), Bone(rig, "forearm." + side), wrist, target, root.TransformPoint(new Vector3(side == "r" ? .48f : -.48f, -.45f, .08f)));
                wrist.rotation = root.rotation * Quaternion.LookRotation(Vector3.up, Vector3.forward);
                if (action == WorkshopAction.Rotate && side == "l") wrist.rotation = root.rotation * Quaternion.Euler(0, pulse * 55, pulse * -12) * Quaternion.LookRotation(Vector3.up, Vector3.forward);
                if ((action == WorkshopAction.Install || action == WorkshopAction.Remove) && side == "r") wrist.rotation *= Quaternion.Euler(pulse * -9, 0, 0);
            }
        }
        static RuntimeAnimatorController Controller(GameObject rig, GameObject root, Transform pose)
        {
            var fingers = rig.GetComponentsInChildren<Transform>().Where(t => t.name.StartsWith("finger") && !t.name.EndsWith("_end")).ToArray();
            var fingerRest = fingers.ToDictionary(t => t, t => t.localRotation);
            var bones = rig.GetComponentsInChildren<SkinnedMeshRenderer>().SelectMany(s => s.bones).Distinct().ToArray();
            var rest = bones.ToDictionary(t => t, t => t.localRotation);
            AnimationClip Clip(string name, bool carrying, WorkshopAction action, bool loop)
            {
                var clip = new AnimationClip { name = name, frameRate = 30 };
                var samples = bones.ToDictionary(t => t, t => new List<Quaternion>());
                float length = loop ? 4 : .65f;
                const int steps = 16;
                for (int sample = 0; sample <= steps; sample++)
                {
                    foreach (var bone in bones) bone.localRotation = rest[bone];
                    foreach (var finger in fingers)
                    {
                        string n = finger.name; float curl = n.Contains("thumb") ? 12 : n.Contains("1.") ? 25 : n.Contains("2.") ? 48 : 24;
                        if (n.Contains("index") && n.EndsWith(".r")) curl *= .55f;
                        if (!carrying && n.EndsWith(".l")) curl *= .35f;
                        if (action == WorkshopAction.Install && n.EndsWith(".l")) curl *= 1 - .7f * Mathf.Sin(sample / (float)steps * Mathf.PI);
                        finger.localRotation = fingerRest[finger] * Quaternion.Euler(0, 0, curl * (n.EndsWith(".r") ? -1 : 1));
                    }
                    Pose(rig, root.transform, carrying, sample / (float)steps, action);
                    foreach (var bone in bones) samples[bone].Add(bone.localRotation);
                }
                foreach (var bone in bones)
                {
                    string path = AnimationUtility.CalculateTransformPath(bone, root.transform);
                    for (int axis = 0; axis < 4; axis++)
                    {
                        var keys = new Keyframe[steps + 1];
                        for (int sample = 0; sample <= steps; sample++) keys[sample] = new Keyframe(sample * length / steps, samples[bone][sample][axis]);
                        AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalRotation." + "xyzw"[axis]), new AnimationCurve(keys));
                    }
                }
                clip.EnsureQuaternionContinuity(); var settings = AnimationUtility.GetAnimationClipSettings(clip); settings.loopTime = loop; AnimationUtility.SetAnimationClipSettings(clip, settings);
                string assetPath = Animations + "/" + name + ".anim";
                var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
                if (existing != null) { EditorUtility.CopySerialized(clip, existing); Object.DestroyImmediate(clip); return existing; }
                AssetDatabase.CreateAsset(clip, assetPath); return clip;
            }
            var clips = new[] { Clip("Idle", false, WorkshopAction.Idle, true), Clip("Carry", true, WorkshopAction.Idle, true), Clip("Install", true, WorkshopAction.Install, false), Clip("Rotate", true, WorkshopAction.Rotate, false), Clip("Pickup", false, WorkshopAction.Pickup, false), Clip("Remove", true, WorkshopAction.Remove, false) };
            string controllerPath = Animations + "/Workshop Hands.controller";
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (controller == null) controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            var machine = controller.layers[0].stateMachine;
            foreach (var state in machine.states) machine.RemoveState(state.state);
            controller.parameters = Array.Empty<AnimatorControllerParameter>(); controller.AddParameter("Carrying", AnimatorControllerParameterType.Bool);
            var states = clips.Select(c => { var s = machine.AddState(c.name); s.motion = c; s.writeDefaultValues = true; return s; }).ToArray();
            machine.defaultState = states[0];
            for (int i = 0; i < states.Length; i++)
                for (int destination = 0; destination < 2; destination++)
                {
                    if (i == destination) continue;
                    var transition = states[i].AddTransition(states[destination]); transition.duration = .12f;
                    transition.hasExitTime = i >= 2; transition.exitTime = 1;
                    transition.AddCondition(destination == 1 ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0, "Carrying");
                }
            EditorUtility.SetDirty(controller); return controller;
        }
    }
}
