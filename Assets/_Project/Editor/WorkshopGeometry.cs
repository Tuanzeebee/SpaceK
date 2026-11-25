using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SpaceK.Editor
{
    /// <summary>Native shared workshop meshes. Geometry has no implicit physics.</summary>
    public static class WorkshopGeometry
    {
        public const string ArtPath = "Assets/_Project/Art/Workshop";
        static readonly Dictionary<string, Mesh> Cache = new();

        public static void EnsureFolders()
        {
            foreach (string folder in new[] { ArtPath, ArtPath + "/Meshes", ArtPath + "/Materials", "Assets/_Project/Prefabs/Environment/Workshop" })
                if (!AssetDatabase.IsValidFolder(folder))
                {
                    string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
                    if (!AssetDatabase.IsValidFolder(parent)) Directory.CreateDirectory(parent);
                    AssetDatabase.Refresh();
                    AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
                }
        }

        public static Material Material(string name, Color color, float metallic = .3f, float smoothness = .35f)
        {
            EnsureFolders();
            string path = ArtPath + "/Materials/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) throw new InvalidOperationException("Workshop geometry requires the existing URP Lit shader.");
            material = new Material(shader) { name = name };
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Smoothness", smoothness);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        public static GameObject Box(string name, Transform parent, Vector3 localPosition, Vector3 size, Material material, float bevel = .015f)
        {
            if (size.x <= 0 || size.y <= 0 || size.z <= 0) throw new ArgumentOutOfRangeException(nameof(size));
            bevel = Mathf.Clamp(bevel, 0, Mathf.Min(size.x, Mathf.Min(size.y, size.z)) * .45f);
            string key = "Box_" + Number(size.x) + "_" + Number(size.y) + "_" + Number(size.z) + "_" + Number(bevel);
            return MeshObject(name, parent, localPosition, Shared(key, () => BeveledBox(size, bevel)), material);
        }

        public static GameObject Cylinder(string name, Transform parent, Vector3 localPosition, float radius, float height, Material material, int sides = 16)
        {
            if (radius <= 0 || height <= 0) throw new ArgumentOutOfRangeException(nameof(radius));
            sides = Mathf.Clamp(sides, 6, 64);
            string key = "Cylinder_" + Number(radius) + "_" + Number(height) + "_" + sides;
            return MeshObject(name, parent, localPosition, Shared(key, () => CylinderMesh(radius, height, sides)), material);
        }

        static string Number(float value) => value.ToString("0.#####", CultureInfo.InvariantCulture).Replace('.', 'p');
        static Mesh Shared(string key, Func<Mesh> create)
        {
            if (Cache.TryGetValue(key, out var cached) && cached != null) return cached;
            EnsureFolders();
            string path = ArtPath + "/Meshes/" + key + ".asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh == null) { mesh = create(); mesh.name = key; AssetDatabase.CreateAsset(mesh, path); }
            Cache[key] = mesh;
            return mesh;
        }

        static GameObject MeshObject(string name, Transform parent, Vector3 position, Mesh mesh, Material material)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(parent, false); go.transform.localPosition = position;
            go.GetComponent<MeshFilter>().sharedMesh = mesh; go.GetComponent<MeshRenderer>().sharedMaterial = material;
            return go;
        }

        static Mesh BeveledBox(Vector3 size, float bevel)
        {
            var vertices = new List<Vector3>(); var triangles = new List<int>(); var uv = new List<Vector2>();
            Vector3 half = size * .5f, inset = half - Vector3.one * bevel;
            void Face(Vector3 normal, params Vector3[] points)
            {
                if (Vector3.Dot(Vector3.Cross(points[1] - points[0], points[2] - points[0]), normal) < 0) Array.Reverse(points);
                int start = vertices.Count;
                foreach (var point in points) { vertices.Add(point); uv.Add(new Vector2(point.x + point.z, point.y + point.z)); }
                for (int i = 1; i < points.Length - 1; i++) { triangles.Add(start); triangles.Add(start + i); triangles.Add(start + i + 1); }
            }
            for (int axis = 0; axis < 3; axis++)
                foreach (int sign in new[] { -1, 1 })
                {
                    int u = (axis + 1) % 3, v = (axis + 2) % 3;
                    var points = new Vector3[4]; int index = 0;
                    foreach (var pair in new[] { new Vector2(-1, -1), new Vector2(1, -1), new Vector2(1, 1), new Vector2(-1, 1) })
                    { Vector3 p = Vector3.zero; p[axis] = half[axis] * sign; p[u] = inset[u] * pair.x; p[v] = inset[v] * pair.y; points[index++] = p; }
                    Vector3 n = Vector3.zero; n[axis] = sign; Face(n, points);
                }
            if (bevel > 0)
            {
                for (int axis = 0; axis < 3; axis++)
                    foreach (int su in new[] { -1, 1 }) foreach (int sv in new[] { -1, 1 })
                    {
                        int u = (axis + 1) % 3, v = (axis + 2) % 3;
                        Vector3 a = Vector3.zero, b = Vector3.zero;
                        a[u] = half[u] * su; a[v] = inset[v] * sv;
                        b[u] = inset[u] * su; b[v] = half[v] * sv;
                        var points = new[] { a, b, b, a }; points[0][axis] = -inset[axis]; points[1][axis] = -inset[axis]; points[2][axis] = inset[axis]; points[3][axis] = inset[axis];
                        Vector3 n = Vector3.zero; n[u] = su; n[v] = sv; Face(n, points);
                    }
                foreach (int x in new[] { -1, 1 }) foreach (int y in new[] { -1, 1 }) foreach (int z in new[] { -1, 1 })
                {
                    Vector3 signs = new(x, y, z), p = Vector3.Scale(inset, signs);
                    var points = new[] { p + new Vector3(x * bevel, 0, 0), p + new Vector3(0, y * bevel, 0), p + new Vector3(0, 0, z * bevel) };
                    Face(signs, points);
                }
            }
            var mesh = new Mesh(); mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0); mesh.SetUVs(0, uv); mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
        }

        static Mesh CylinderMesh(float radius, float height, int sides)
        {
            var vertices = new List<Vector3>(); var triangles = new List<int>(); var uv = new List<Vector2>();
            for (int i = 0; i <= sides; i++)
            {
                float angle = i * Mathf.PI * 2 / sides;
                for (int j = 0; j < 2; j++) { vertices.Add(new Vector3(Mathf.Cos(angle) * radius, (j - .5f) * height, Mathf.Sin(angle) * radius)); uv.Add(new Vector2(i / (float)sides, j)); }
            }
            for (int i = 0; i < sides; i++) { int a = i * 2; triangles.AddRange(new[] { a, a + 1, a + 3, a, a + 3, a + 2 }); }
            foreach (int sign in new[] { -1, 1 })
            {
                int center = vertices.Count; vertices.Add(new Vector3(0, height * .5f * sign, 0)); uv.Add(new Vector2(.5f, .5f));
                for (int i = 0; i <= sides; i++)
                { float angle = i * Mathf.PI * 2 / sides; Vector3 p = new(Mathf.Cos(angle) * radius, height * .5f * sign, Mathf.Sin(angle) * radius); vertices.Add(p); uv.Add(new Vector2(p.x / radius, p.z / radius) * .5f + Vector2.one * .5f); }
                for (int i = 0; i < sides; i++) triangles.AddRange(sign > 0 ? new[] { center, center + i + 2, center + i + 1 } : new[] { center, center + i + 1, center + i + 2 });
            }
            var mesh = new Mesh(); mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0); mesh.SetUVs(0, uv); mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
        }

        /// <summary>Combine only authored detail under this root, before nesting imported prefabs.</summary>
        public static void CombineStatic(Transform root, string assetName)
        {
            var sources = root.GetComponentsInChildren<MeshFilter>().Where(f => f.sharedMesh != null && f.GetComponent<MeshRenderer>() != null).ToArray();
            foreach (var materialGroup in sources.GroupBy(f => f.GetComponent<MeshRenderer>().sharedMaterial))
            {
                var batch = new List<CombineInstance>(); int vertices = 0, chunk = 0;
                void Flush()
                {
                    if (batch.Count == 0) return;
                    var mesh = new Mesh { name = assetName + "_" + materialGroup.Key.name + "_" + chunk++ };
                    mesh.CombineMeshes(batch.ToArray(), true, true); mesh.RecalculateBounds();
                    if (mesh.vertexCount > 35000) throw new InvalidOperationException("Workshop detail mesh exceeded 35k vertices.");
                    string path = ArtPath + "/Meshes/" + mesh.name + ".asset";
                    var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                    if (existing != null) { EditorUtility.CopySerialized(mesh, existing); UnityEngine.Object.DestroyImmediate(mesh); mesh = existing; EditorUtility.SetDirty(mesh); }
                    else AssetDatabase.CreateAsset(mesh, path);
                    MeshObject("Batched " + materialGroup.Key.name, root, Vector3.zero, mesh, materialGroup.Key);
                    batch.Clear(); vertices = 0;
                }
                foreach (var source in materialGroup)
                {
                    if (vertices + source.sharedMesh.vertexCount > 34000) Flush();
                    batch.Add(new CombineInstance { mesh = source.sharedMesh, transform = root.worldToLocalMatrix * source.transform.localToWorldMatrix }); vertices += source.sharedMesh.vertexCount;
                }
                Flush();
            }
            foreach (var source in sources)
            {
                UnityEngine.Object.DestroyImmediate(source.GetComponent<MeshRenderer>()); UnityEngine.Object.DestroyImmediate(source);
            }
            foreach (var transform in root.GetComponentsInChildren<Transform>().Reverse())
                if (transform != root && transform.childCount == 0 && transform.GetComponents<Component>().Length == 1) UnityEngine.Object.DestroyImmediate(transform.gameObject);
        }
    }
}

