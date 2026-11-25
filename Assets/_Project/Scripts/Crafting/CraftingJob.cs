using SpaceK.Inventory;

namespace SpaceK.Crafting
{
    public sealed class CraftingJob
    {
        InventoryModel inventory;
        ItemAmount[] materials;
        ItemAmount[] output;
        float duration, elapsed;
        public bool IsRunning { get; private set; }
        public bool Succeeded { get; private set; }
        public string Message { get; private set; } = "Select a recipe";
        public float Progress => duration <= 0 ? (Succeeded ? 1 : 0) : UnityEngine.Mathf.Clamp01(elapsed / duration);

        public bool Start(CraftingRecipe recipe, InventoryModel target)
        {
            if (IsRunning || recipe == null || !recipe.IsValid || target == null) return false;
            var products = new[] { recipe.Output };
            if (!target.CanExchange(recipe.Materials, products)) { Message = "Missing materials or output space"; return false; }
            inventory = target; materials = (ItemAmount[])recipe.Materials.Clone(); output = products;
            duration = recipe.Duration; elapsed = 0; Succeeded = false; IsRunning = true; Message = "Fabricating";
            Tick(0);
            return true;
        }
        public void Tick(float seconds)
        {
            if (!IsRunning || seconds < 0 || float.IsNaN(seconds) || float.IsInfinity(seconds)) return;
            elapsed += seconds;
            if (elapsed < duration) return;
            // Commit once on completion; cancel and failed revalidation never consume materials.
            Succeeded = inventory.TryExchange(materials, output);
            IsRunning = false;
            Message = Succeeded ? "Fabrication complete" : "Inventory changed: craft cancelled without cost";
        }
        public void Cancel()
        {
            if (!IsRunning) return;
            IsRunning = false; Message = "Cancelled; materials retained";
        }
    }
}
