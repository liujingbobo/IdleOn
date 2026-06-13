using System.Collections.Generic;
using UnityEngine;
using IdleOn.Save;
using IdleOn.Core;
using IdleOn.Items;

namespace IdleOn.Inventory
{
    public class InventorySystem : MonoBehaviour
    {
        public static InventorySystem Instance { get; private set; }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private InventoryData Data => SaveManager.Instance?.CurrentSave?.Inventory;

        public bool TryAddItem(string itemId, int quantity = 1)
        {
            var data = Data;
            if (data == null) return false;

            bool success = data.AddItem(itemId, quantity);
            if (success)
                GameEvents.RaiseInventoryChanged();
            return success;
        }

        public bool RemoveItem(string itemId, int quantity = 1)
        {
            var data = Data;
            if (data == null) return false;

            bool success = data.RemoveItem(itemId, quantity);
            if (success)
                GameEvents.RaiseInventoryChanged();
            return success;
        }

        public int  GetQuantity(string itemId)               => Data?.GetQuantity(itemId) ?? 0;
        public bool HasItem(string itemId, int quantity = 1) => Data?.HasItem(itemId, quantity) ?? false;

        public IReadOnlyList<InventorySlotData> GetSlots()   => Data?.Slots ?? System.Array.Empty<InventorySlotData>();
        public int GetCapacity()                             => Data?.Capacity ?? 0;
    }
}
