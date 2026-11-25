using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace SpaceK.Tests
{
    public class ImportedModuleTests
    {
        [Test] public void FinLongEdgeFacesTheHullAndSmallTipFacesOutward()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Modules/Stabilizer Fin.prefab");
            foreach (var lod in prefab.GetComponent<LODGroup>().GetLODs())
            {
                var filter = lod.renderers[0].GetComponent<MeshFilter>();
                var points = System.Array.ConvertAll(filter.sharedMesh.vertices,
                    p => prefab.transform.InverseTransformPoint(filter.transform.TransformPoint(p)));
                float rootMin = float.MaxValue, rootMax = float.MinValue, tipMin = float.MaxValue, tipMax = float.MinValue;
                foreach (var point in points)
                {
                    if (point.z < -.24f) { rootMin = Mathf.Min(rootMin, point.y); rootMax = Mathf.Max(rootMax, point.y); }
                    if (point.z > .24f) { tipMin = Mathf.Min(tipMin, point.y); tipMax = Mathf.Max(tipMax, point.y); }
                }
                Assert.That(rootMax - rootMin, Is.GreaterThan(.7f), "The long edge must be at the -Z hull mount.");
                Assert.That(tipMax - tipMin, Is.LessThan(.3f), "The small end must extend outward along +Z.");
            }
        }
        [TestCase("Hull Module", "Modules/Hull Module")]
        [TestCase("Nose Cone", "Modules/Nose Cone")]
        [TestCase("Engine Module", "Modules/Thruster Module")]
        [TestCase("Stabilizer Fin", "Modules/Stabilizer Fin")]
        [TestCase("Fuel Tank", "Modules/Fuel Tank")]
        [TestCase("Material Crate", "Environment/Material Crate")]
        [TestCase("Wall Panel", "Environment/Wall Panel")]
        [TestCase("Floor Tile", "Environment/Floor Tile")]
        [TestCase("Inspection Terminal", "Interactables/Terminal")]
        public void ImportedModelHasBoundedCollisionAndDecreasingLods(string model, string prefabName)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/" + prefabName + ".prefab");
            Assert.That(prefab, Is.Not.Null);
            var collider = prefab.GetComponent<BoxCollider>(); Assert.That(collider, Is.Not.Null);
            var group = prefab.GetComponent<LODGroup>(); Assert.That(group, Is.Not.Null);
            var lods = group.GetLODs(); Assert.That(lods.Length, Is.EqualTo(3));
            long previous = long.MaxValue;
            for (int i = 0; i < lods.Length; i++)
            {
                Assert.That(lods[i].renderers.Length, Is.EqualTo(1));
                var renderer = lods[i].renderers[0]; var mesh = renderer.GetComponent<MeshFilter>().sharedMesh;
                Assert.That(mesh, Is.Not.Null); long triangles = (long)mesh.GetIndexCount(0) / 3;
                Assert.That(triangles, Is.InRange(1L, 35000L)); Assert.That(triangles, Is.LessThan(previous)); previous = triangles;
                Assert.That(renderer.sharedMaterial.shader.name, Is.EqualTo("Universal Render Pipeline/Lit"));
                foreach (var property in new[] { "_BaseMap", "_BumpMap", "_MetallicGlossMap", "_OcclusionMap" })
                    Assert.That(renderer.sharedMaterial.GetTexture(property), Is.Not.Null, model + property);
                if (i == 0)
                {
                    // Prefab asset colliders do not belong to a live physics scene.
                    Assert.That((renderer.bounds.center - collider.transform.TransformPoint(collider.center)).magnitude, Is.LessThan(.002f), model + " collision center");
                    Assert.That((renderer.bounds.size - collider.size).magnitude, Is.LessThan(.002f), model + " collision bounds");
                }
            }
            Assert.That(prefab.GetComponentInChildren<MeshCollider>(), Is.Null, "Detailed art uses inexpensive box collision.");
        }
    }
}
