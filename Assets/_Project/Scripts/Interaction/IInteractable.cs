using SpaceK.Inventory;
using SpaceK.UI;
using UnityEngine;

namespace SpaceK.Interaction
{
    public readonly struct InteractionContext
    {
        public readonly Transform Player;
        public readonly InventoryModel Inventory;
        public readonly SimulatorHUD HUD;
        public InteractionContext(Transform player, InventoryModel inventory, SimulatorHUD hud) { Player = player; Inventory = inventory; HUD = hud; }
    }
    public interface IInteractable
    {
        string Prompt { get; }
        bool CanInteract(InteractionContext context);
        void Interact(InteractionContext context);
    }
}
