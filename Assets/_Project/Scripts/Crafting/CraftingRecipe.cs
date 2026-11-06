using SpaceK.Inventory;
using UnityEngine;

namespace SpaceK.Crafting
{
    [CreateAssetMenu(menuName = "SpaceK/Crafting Recipe")]
    public sealed class CraftingRecipe : ScriptableObject
    {
        public string DisplayName;
        public ItemAmount[] Materials = System.Array.Empty<ItemAmount>();
        [Min(0)] public float Duration = 3;
        public ItemAmount Output;
        public bool IsValid => Output.Item != null && Output.Amount > 0 && Materials != null && Materials.Length > 0
            && !float.IsNaN(Duration) && !float.IsInfinity(Duration) && Duration >= 0;
    }
}
