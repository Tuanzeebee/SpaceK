using UnityEngine;

namespace SpaceK.Building
{
    public static class ModuleAttachment
    {
        public static bool TryPose(PlacedShipModule support, BuildModuleDefinition part, Vector3 hitPoint,
            Vector3 hitNormal, int quarterTurns, float heightGrid, out Vector3 position, out Quaternion rotation, int mountSideTurns = 0)
        {
            position = default; rotation = Quaternion.identity;
            var box = support.GetComponent<BoxCollider>();
            if (box == null || support.Definition == null) return false;
            var root = support.transform;
            if (part.Mount == ModuleMount.Radial)
            {
                if (!support.Definition.AcceptsSideAttachments || Mathf.Abs(hitNormal.y) > .7f) return false;
                Vector3 normal = root.InverseTransformDirection(hitNormal);
                Vector3 face = Mathf.Abs(normal.x) > Mathf.Abs(normal.z)
                    ? Vector3.right * Mathf.Sign(normal.x) : Vector3.forward * Mathf.Sign(normal.z);
                face = GridPlacement.Rotation(mountSideTurns) * face;
                float extent = Mathf.Abs(face.x) * box.size.x * .5f + Mathf.Abs(face.z) * box.size.z * .5f;
                Vector3 localHit = root.InverseTransformPoint(hitPoint) - box.center;
                float y = Mathf.Round(localHit.y / heightGrid) * heightGrid;
                float clearance = (box.size.y - part.Size.y) * .5f;
                if (clearance < 0) return false;
                y = Mathf.Clamp(y, -clearance, clearance);
                // Radial prefabs extend along local +Z; their -Z edge is the mounting edge.
                position = root.TransformPoint(box.center + Vector3.up * y + face * (extent + part.Size.z * .5f - part.AttachmentInset));
                rotation = root.rotation * Quaternion.LookRotation(face, Vector3.up) * Quaternion.Euler(0, (quarterTurns % 2) * 180, 0);
                return true;
            }
            if (!support.Definition.SupportsStacking) return false;
            float sign = hitNormal.y < -.7f ? -1 : 1;
            position = root.TransformPoint(box.center + Vector3.up * (box.size.y * .5f * sign)) + Vector3.up * (part.Size.y * .5f * sign);
            rotation = root.rotation * GridPlacement.Rotation(quarterTurns);
            return GridPlacement.FitsOnSurface(box, position, part.Size, rotation);
        }
    }
}
