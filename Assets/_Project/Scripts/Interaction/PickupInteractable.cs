using SpaceK.Inventory;
using UnityEngine;

namespace SpaceK.Interaction
{
    public sealed class PickupInteractable : MonoBehaviour, IInteractable
    {
        public ItemAmount Content;
        bool collected;
        public bool Collected => collected;
        public string Prompt => Content.Item == null ? "Unconfigured pickup" : "Collect " + Content.Item.DisplayName + " x" + Content.Amount;
        public bool CanInteract(InteractionContext context) => !collected && Content.Item != null;
        public void Interact(InteractionContext context)
        {
            if (collected) return;
            if (!context.Inventory.TryAdd(Content.Item, Content.Amount)) { context.HUD?.Notify("Inventory full"); return; }
            collected = true; gameObject.SetActive(false); Destroy(gameObject);
        }
    }
}
