using NUnit.Framework;
using SpaceK.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SpaceK.Tests
{
    public class WorkshopArtTests
    {
        [Test] public void EngineerContainsWeightedGlovesAndEditableActionClips()
        {
            var player = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Player/Engineer.prefab");
            var hands = player.GetComponentInChildren<WorkshopHandsPresenter>(true); Assert.That(hands, Is.Not.Null);
            var skin = hands.GetComponentInChildren<SkinnedMeshRenderer>();
            Assert.That(skin.bones.Length, Is.EqualTo(skin.sharedMesh.bindposes.Length));
            Assert.That(skin.bones.Length, Is.GreaterThan(30)); Assert.That(skin.sharedMesh.subMeshCount, Is.EqualTo(2));
            Assert.That(hands.GetComponentsInChildren<Collider>(true), Is.Empty);
            Assert.That(hands.HandsCamera.GetComponent<AudioListener>(), Is.Null);
            Assert.That(hands.Building.View.cullingMask & hands.HandsCamera.cullingMask, Is.Zero);
            foreach (string name in new[] { "Idle", "Carry", "Install", "Rotate", "Pickup", "Remove" })
            {
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/_Project/Animations/WorkshopHands/" + name + ".anim");
                Assert.That(clip, Is.Not.Null, name);
                Assert.That(AnimationUtility.GetCurveBindings(clip).Length, Is.GreaterThan(100), name + " must animate the rig.");
            }
        }
        [Test] public void WorkshopPrefabsUseBoundedNativeGeometryAndKeepTheAssemblyAreaClear()
        {
            foreach (string name in new[] { "Tool Cabinet", "Service Trolley", "Module Storage Rack", "Port Sliding Hangar Door" })
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Environment/Workshop/" + name + ".prefab");
                Assert.That(prefab, Is.Not.Null, name); Assert.That(prefab.GetComponentsInChildren<MeshCollider>(true), Is.Empty);
                foreach (var filter in prefab.GetComponentsInChildren<MeshFilter>(true))
                    Assert.That(filter.sharedMesh.vertexCount, Is.LessThanOrEqualTo(35000), name);
            }
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/SpacecraftSimulator_Test.unity");
            var root = GameObject.Find("Workshop Art Pass"); Assert.That(root, Is.Not.Null);
            var clearBay = new Bounds(new Vector3(5, 1.5f, 3), new Vector3(11.8f, 2.8f, 13.8f));
            foreach (var collider in root.GetComponentsInChildren<Collider>())
                if (collider.enabled) Assert.That(collider.bounds.Intersects(clearBay), Is.False, collider.name + " blocks the assembly area.");
        }
    }
}
