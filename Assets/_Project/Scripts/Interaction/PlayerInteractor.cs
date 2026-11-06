using SpaceK.Inventory;
using SpaceK.Player;
using SpaceK.UI;
using UnityEngine;

namespace SpaceK.Interaction
{
    public sealed class PlayerInteractor : MonoBehaviour
    {
        public UnityEngine.Camera View;
        public PlayerInputReader Input;
        public InventoryComponent Inventory;
        public SimulatorHUD HUD;
        [Min(.5f)] public float Range = 3.2f;
        public LayerMask Mask = ~4;
        public IInteractable Current { get; private set; }
        void Update()
        {
            Current = null;
            Vector3 playerCenter = transform.position + Vector3.up;
            float cameraOffset = Vector3.Distance(View.transform.position, playerCenter);
            if (Input.WorldEnabled && !HUD.Building.IsPlacing && Physics.Raycast(View.ViewportPointToRay(new Vector3(.5f, .5f)), out var hit, Range + cameraOffset, Mask, QueryTriggerInteraction.Ignore)
                && Vector3.SqrMagnitude(hit.point - playerCenter) <= Range * Range)
            {
                var context = new InteractionContext(transform, Inventory.Model, HUD);
                foreach (var component in hit.collider.GetComponentsInParent<MonoBehaviour>())
                    if (component is IInteractable interactable && interactable.CanInteract(context)) { Current = interactable; break; }
                if (Current != null && Input.Pressed("Interact")) Current.Interact(context);
            }
            HUD.SetPrompt(Current == null ? "" : "[E]  " + Current.Prompt);
        }
    }
}
