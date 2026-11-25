using System;
using System.Collections.Generic;

namespace SpaceK.Inventory
{
    public sealed class InventoryModel
    {
        readonly List<ItemAmount> stacks = new();
        readonly System.Collections.ObjectModel.ReadOnlyCollection<ItemAmount> view;
        public IReadOnlyList<ItemAmount> Stacks => view;
        public int SlotCapacity { get; }
        public event Action Changed;

        public InventoryModel(int slots = 24)
        {
            if (slots <= 0) throw new ArgumentOutOfRangeException(nameof(slots));
            SlotCapacity = slots;
            view = stacks.AsReadOnly();
        }

        public int Count(ItemData item) => item == null ? 0 : Count(item.Id);
        public int Count(string id)
        {
            long total = 0;
            foreach (var stack in stacks) if (stack.Item.Id == id) total += stack.Amount;
            return (int)Math.Min(total, int.MaxValue);
        }
        public bool TryAdd(ItemData item, int amount) => TryExchange(Array.Empty<ItemAmount>(), new[] { new ItemAmount(item, amount) });
        public bool TryRemove(ItemData item, int amount) => TryExchange(new[] { new ItemAmount(item, amount) }, Array.Empty<ItemAmount>());
        public bool CanExchange(IReadOnlyList<ItemAmount> cost, IReadOnlyList<ItemAmount> output) => Project(cost, output, out _);
        public bool TryExchange(IReadOnlyList<ItemAmount> cost, IReadOnlyList<ItemAmount> output)
        {
            if (!Project(cost, output, out var result)) return false;
            stacks.Clear();
            stacks.AddRange(result);
            Changed?.Invoke();
            return true;
        }

        bool Project(IReadOnlyList<ItemAmount> cost, IReadOnlyList<ItemAmount> output, out List<ItemAmount> result)
        {
            result = new List<ItemAmount>(stacks);
            if (cost == null || output == null) return false;
            foreach (var entry in cost)
            {
                if (!Valid(entry)) return false;
                int remaining = entry.Amount;
                for (int i = result.Count - 1; i >= 0 && remaining > 0; --i)
                {
                    var stack = result[i];
                    if (stack.Item.Id != entry.Item.Id) continue;
                    int taken = Math.Min(remaining, stack.Amount);
                    remaining -= taken;
                    stack.Amount -= taken;
                    if (stack.Amount == 0) result.RemoveAt(i); else result[i] = stack;
                }
                if (remaining != 0) return false;
            }
            foreach (var entry in output)
            {
                if (!Valid(entry)) return false;
                int remaining = entry.Amount;
                for (int i = 0; i < result.Count && remaining > 0; ++i)
                {
                    var stack = result[i];
                    if (stack.Item.Id != entry.Item.Id) continue;
                    int added = Math.Min(remaining, Math.Max(0, stack.Item.MaxStack - stack.Amount));
                    remaining -= added; stack.Amount += added; result[i] = stack;
                }
                while (remaining > 0)
                {
                    if (result.Count >= SlotCapacity) return false;
                    int added = Math.Min(remaining, entry.Item.MaxStack);
                    result.Add(new ItemAmount(entry.Item, added)); remaining -= added;
                }
            }
            return true;
        }
        static bool Valid(ItemAmount value) => value.Item != null && !string.IsNullOrWhiteSpace(value.Item.Id) && value.Amount > 0 && value.Item.MaxStack > 0;
    }
}
