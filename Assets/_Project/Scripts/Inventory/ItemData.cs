using System;
using UnityEngine;

namespace SpaceK.Inventory
{
    [CreateAssetMenu(menuName = "SpaceK/Item")]
    public sealed class ItemData : ScriptableObject
    {
        public string Id;
        public string DisplayName;
        public Sprite Icon;
        [TextArea] public string Description;
        [Min(1)] public int MaxStack = 50;
        public bool Stackable => MaxStack > 1;
        void OnValidate() { MaxStack = Mathf.Max(1, MaxStack); }
    }

    [Serializable]
    public struct ItemAmount
    {
        public ItemData Item;
        [Min(1)] public int Amount;
        public ItemAmount(ItemData item, int amount) { Item = item; Amount = amount; }
    }
}
