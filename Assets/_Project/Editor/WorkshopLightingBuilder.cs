using SpaceK.Building;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace SpaceK.Editor
{
    public static class WorkshopLightingBuilder
    {
        public static void Apply()
        {
            var building = Object.FindFirstObjectByType<BuildingController>();
            if (building == null || Application.isPlaying) throw new System.InvalidOperationException("Open the demo in Edit Mode.");
            var previous = GameObject.Find("Workshop Lighting");
            if (previous != null) Object.DestroyImmediate(previous);
            var root = new GameObject("Workshop Lighting");
            var volume = root.AddComponent<Volume>(); volume.isGlobal = true; volume.priority = 5;
            const string path = "Assets/_Project/Settings/Workshop Volume.asset";
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (profile == null) { profile = ScriptableObject.CreateInstance<VolumeProfile>(); AssetDatabase.CreateAsset(profile, path); }
            if (!profile.TryGet<Tonemapping>(out var tone)) tone = profile.Add<Tonemapping>(true);
            tone.mode.Override(TonemappingMode.ACES);
            if (!profile.TryGet<Bloom>(out var bloom)) bloom = profile.Add<Bloom>(true);
            bloom.intensity.Override(.18f); bloom.threshold.Override(1.15f); bloom.scatter.Override(.45f);
            if (!profile.TryGet<ColorAdjustments>(out var color)) color = profile.Add<ColorAdjustments>(true);
            color.contrast.Override(8); color.saturation.Override(-6); color.postExposure.Override(0);
            if (!profile.TryGet<Vignette>(out var vignette)) vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(.14f); vignette.smoothness.Override(.35f);
            EditorUtility.SetDirty(profile); volume.sharedProfile = profile;
            building.View.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            building.View.allowHDR = true;
            PrefabUtility.RecordPrefabInstancePropertyModifications(building.View.GetUniversalAdditionalCameraData());
            PrefabUtility.RecordPrefabInstancePropertyModifications(building.View);
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.37f, .45f, .54f);
            RenderSettings.ambientEquatorColor = new Color(.25f, .28f, .31f);
            RenderSettings.ambientGroundColor = new Color(.10f, .09f, .075f);
            RenderSettings.fogColor = new Color(.22f, .28f, .34f); RenderSettings.fogDensity = .004f;
            foreach (var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (light.name == "Hangar Key Light")
                { light.color = new Color(1, .93f, .81f); light.intensity = 1.3f; light.transform.rotation = Quaternion.Euler(40, 65, 0); }
                else if (light.name == "Ceiling Worklight")
                { light.color = new Color(1, .83f, .64f); light.intensity = 2.4f; light.range = 11; }
            }
            for (int i = 0; i < 3; i++)
            {
                var task = new GameObject("Workshop Task Fill").AddComponent<Light>(); task.transform.SetParent(root.transform, false);
                task.transform.position = new Vector3(-7 + i * 5, 5.5f, -4);
                task.type = LightType.Spot; task.range = 9; task.spotAngle = 90; task.innerSpotAngle = 65;
                task.transform.rotation = Quaternion.Euler(85, 0, 0); task.color = new Color(1, .85f, .68f); task.intensity = 5;
                task.shadows = LightShadows.None;
            }
            int handsLayer = LayerMask.NameToLayer("WorkshopHands");
            foreach (var probe in Object.FindObjectsByType<ReflectionProbe>(FindObjectsSortMode.None))
            { if (handsLayer >= 0) probe.cullingMask &= ~(1 << handsLayer); probe.intensity = .65f; }
            var structure = GameObject.Find("Workshop Art Pass/Structure and Roof");
            if (structure != null)
            {
                var glazing = WorkshopGeometry.Material("Workshop Skylight Glass", new Color(.43f, .69f, .78f, .17f), .05f, .7f);
                glazing.SetFloat("_Surface", 1); glazing.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); glazing.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                glazing.SetFloat("_ZWrite", 0); glazing.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); glazing.renderQueue = 3000;
                glazing.SetOverrideTag("RenderType", "Transparent"); glazing.SetShaderPassEnabled("ShadowCaster", false); EditorUtility.SetDirty(glazing);
                foreach (var renderer in structure.GetComponentsInChildren<MeshRenderer>())
                    if (renderer.sharedMaterial.name == "Skylight Blue") { renderer.sharedMaterial = glazing; renderer.shadowCastingMode = ShadowCastingMode.Off; }
            }
            var sourceDeck = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/HangarModels/Floor Tile/PBR.mat");
            const string deckPath = "Assets/_Project/Art/Workshop/Materials/Workshop Deck Finish.mat";
            var deck = AssetDatabase.LoadAssetAtPath<Material>(deckPath);
            if (deck == null) { deck = new Material(sourceDeck); AssetDatabase.CreateAsset(deck, deckPath); }
            deck.SetFloat("_Smoothness", .35f); deck.SetFloat("_BumpScale", .65f); deck.SetColor("_BaseColor", new Color(.85f, .85f, .85f)); EditorUtility.SetDirty(deck);
            foreach (var renderer in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
                if (renderer.sharedMaterial == sourceDeck) { renderer.sharedMaterial = deck; PrefabUtility.RecordPrefabInstancePropertyModifications(renderer); }
            AssetDatabase.SaveAssets();
        }
    }
}
