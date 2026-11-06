using UnityEngine;

namespace SpaceK.CameraSystem
{
    // The final blended camera also needs collision: clear endpoints do not imply a clear blend path.
    [DefaultExecutionOrder(10000)]
    [RequireComponent(typeof(UnityEngine.Camera))]
    public sealed class CameraWallGuard : MonoBehaviour
    {
        public Transform Anchor;
        public LayerMask CollisionMask = ~4;
        public float MinimumRadius = .16f;
        UnityEngine.Camera output;
        void Awake() { output = GetComponent<UnityEngine.Camera>(); }
        void LateUpdate()
        {
            if (Anchor == null) return;
            Vector3 offset = transform.position - Anchor.position;
            float distance = offset.magnitude;
            float nearHeight = output.nearClipPlane * Mathf.Tan(output.fieldOfView * .5f * Mathf.Deg2Rad);
            float radius = Mathf.Max(MinimumRadius, nearHeight * Mathf.Sqrt(1 + output.aspect * output.aspect));
            if (distance > .001f && Physics.SphereCast(Anchor.position, radius, offset / distance, out var hit, distance,
                CollisionMask, QueryTriggerInteraction.Ignore))
                transform.position = Anchor.position + offset.normalized * Mathf.Max(0, hit.distance - .025f);
        }
    }
}
