using SpaceK.Inventory;
using UnityEngine;

namespace SpaceK.Building
{
    public enum ModuleMount { Stack, Radial }
    [CreateAssetMenu(menuName = "SpaceK/Ship Module")]
    public sealed class BuildModuleDefinition : ScriptableObject
    {
        public string DisplayName;
        public GameObject Prefab;
        public ItemAmount Cost;
        public Vector3 Size = Vector3.one;
        public Vector3 SocketStep = Vector3.one;
        public ModuleMount Mount;
        public bool AcceptsSideAttachments;
        public bool SupportsStacking = true;
        [Min(0)] public float AttachmentInset = .005f;
        public bool IsValid => Prefab != null && Size.x > 0 && Size.y > 0 && Size.z > 0 && Cost.Item != null && Cost.Amount > 0;
    }
    public static class GridPlacement
    {
        public static Vector3 Snap(Vector3 point, float grid)
        {
            if (grid <= 0 || float.IsNaN(grid) || float.IsInfinity(grid)) throw new System.ArgumentOutOfRangeException(nameof(grid));
            return new Vector3(Mathf.Round(point.x / grid) * grid, point.y, Mathf.Round(point.z / grid) * grid);
        }
        public static Quaternion Rotation(int quarterTurns) => Quaternion.Euler(0, (quarterTurns % 4) * 90, 0);
        public static bool FitsOnSurface(BoxCollider surface, Vector3 center, Vector3 size, Quaternion rotation)
        {
            if (surface == null) return false;
            Vector3 half = size * .5f;
            Vector3 min = surface.center - surface.size * .5f, max = surface.center + surface.size * .5f;
            for (int x = -1; x <= 1; x += 2)
                for (int z = -1; z <= 1; z += 2)
                {
                    Vector3 corner = surface.transform.InverseTransformPoint(center + rotation * new Vector3(x * half.x, 0, z * half.z));
                    if (corner.x < min.x - .001f || corner.x > max.x + .001f || corner.z < min.z - .001f || corner.z > max.z + .001f) return false;
                }
            return true;
        }
    }
}
