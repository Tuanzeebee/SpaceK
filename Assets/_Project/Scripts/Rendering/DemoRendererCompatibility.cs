using UnityEngine;

namespace SpaceK.Rendering
{
    // Work around a reproduced 6000.3.28f1 native GPU Resident Drawer crash without editing the user's URP assets.
    [ExecuteAlways]
    public sealed class DemoRendererCompatibility : MonoBehaviour
    {
        void OnEnable() { Apply(); }
        void OnTransformChildrenChanged() { Apply(); }
        void Apply()
        {
            var properties = new MaterialPropertyBlock();
            foreach (var renderer in GetComponentsInChildren<MeshRenderer>(true))
            {
                renderer.GetPropertyBlock(properties);
                // A non-empty property block selects the conventional renderer path, locally.
                properties.SetFloat("_SpaceKConventionalRendering", 1);
                renderer.SetPropertyBlock(properties);
                properties.Clear();
            }
        }
    }
}
