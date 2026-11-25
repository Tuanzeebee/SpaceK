using UnityEngine;

namespace SpaceK.Inventory
{
    public sealed class InventoryComponent : MonoBehaviour
    {
        [Min(1)] public int Slots = 24;
        public ItemAmount[] StartingItems = System.Array.Empty<ItemAmount>();
        InventoryModel model;
        public InventoryModel Model
        {
            get
            {
                if (model != null) return model;
                model = new InventoryModel(Slots);
                foreach (var entry in StartingItems)
                    if (!model.TryAdd(entry.Item, entry.Amount)) Debug.LogWarning("Starting inventory entry does not fit.", this);
                return model;
            }
        }
        void Awake() { _ = Model; }
    }
}
