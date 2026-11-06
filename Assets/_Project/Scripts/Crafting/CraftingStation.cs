using SpaceK.Interaction;
using UnityEngine;

namespace SpaceK.Crafting
{
    public sealed class CraftingStation : MonoBehaviour, IInteractable
    {
        public string StationName = "Fabrication workbench";
        public CraftingRecipe[] Recipes = System.Array.Empty<CraftingRecipe>();
        [Min(1)] public float UseRange = 4;
        public CraftingJob Job { get; } = new();
        public string Prompt => "Use " + StationName;
        public bool CanInteract(InteractionContext context) => Vector3.Distance(context.Player.position, transform.position) <= UseRange;
        public void Interact(InteractionContext context) { context.HUD?.OpenCrafting(this); }
        void Update() { Job.Tick(Time.deltaTime); }
        void OnDisable() { Job.Cancel(); }
    }
}
