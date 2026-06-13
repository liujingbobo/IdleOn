using System.Collections.Generic;
using UnityEngine;

namespace IdleOn.Items
{
    [CreateAssetMenu(fileName = "ItemDatabase", menuName = "IdleOn/Item Database")]
    public class ItemDatabase : ScriptableObject
    {
        [SerializeField] private List<ItemDefinition> allItems = new List<ItemDefinition>();

        public IReadOnlyList<ItemDefinition> AllItems => allItems;

        public ItemDefinition GetItem(string itemId)
        {
            foreach (var item in allItems)
                if (item != null && item.ItemId == itemId) return item;
            return null;
        }

        public bool HasItem(string itemId) => GetItem(itemId) != null;

#if UNITY_EDITOR
        private void OnValidate()
        {
            var seen = new HashSet<string>();
            foreach (var item in allItems)
            {
                if (item == null || string.IsNullOrEmpty(item.ItemId)) continue;
                if (!seen.Add(item.ItemId))
                    Debug.LogWarning($"[ItemDatabase] Duplicate ItemId: '{item.ItemId}'", this);
            }
        }
#endif
    }
}
