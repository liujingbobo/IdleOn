using System;
using System.Collections.Generic;
using UnityEngine;

namespace IdleOn.Items
{
    [Serializable]
    public class InventoryData
    {
        [SerializeField] private int capacity = 20;
        [SerializeField] private List<InventorySlotData> slots = new List<InventorySlotData>();

        public int Capacity  => capacity;
        public int UsedSlots => slots.Count;
        public IReadOnlyList<InventorySlotData> Slots => slots;

        public InventoryData() { }
        public InventoryData(int capacity) { this.capacity = capacity; }

        // Returns false if inventory is full and item has no existing slot.
        public bool AddItem(string itemId, int quantity = 1)
        {
            var slot = FindSlot(itemId);
            if (slot != null)
            {
                slot.Quantity += quantity;
                return true;
            }

            if (slots.Count >= capacity) return false;

            slots.Add(new InventorySlotData(itemId, quantity));
            return true;
        }

        // Returns false if slot not found or quantity insufficient.
        public bool RemoveItem(string itemId, int quantity = 1)
        {
            var slot = FindSlot(itemId);
            if (slot == null || slot.Quantity < quantity) return false;

            slot.Quantity -= quantity;
            if (slot.Quantity <= 0) slots.Remove(slot);
            return true;
        }

        public int  GetQuantity(string itemId)               => FindSlot(itemId)?.Quantity ?? 0;
        public bool HasItem(string itemId, int quantity = 1) => GetQuantity(itemId) >= quantity;
        public void ExpandCapacity(int additionalSlots)      => capacity += additionalSlots;

        private InventorySlotData FindSlot(string itemId)
        {
            foreach (var slot in slots)
                if (slot.ItemId == itemId) return slot;
            return null;
        }
    }
}
