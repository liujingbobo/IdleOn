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
        public IReadOnlyList<InventorySlotData> Slots => slots;

        public InventoryData() { }
        public InventoryData(int capacity) { this.capacity = capacity; }

        // Fixed-slot model: appends empty slots until Slots.Count >= capacity.
        // Never removes/shrinks — old dense saves keep existing items at their
        // current indices (slot 0..N-1) and just get empty slots appended.
        public void EnsureSlots(int requiredCount)
        {
            while (slots.Count < requiredCount)
                slots.Add(new InventorySlotData(null, 0));
        }

        // totalCapacity is the effective capacity (base Capacity + any external bonus,
        // e.g. talent-driven) — callers that have no bonus to apply should pass Capacity.
        // Returns false if inventory is full and item has no existing matching slot.
        public bool AddItem(string itemId, int totalCapacity, int quantity = 1)
        {
            EnsureSlots(totalCapacity);

            var slot = FindSlot(itemId);
            if (slot != null)
            {
                slot.Quantity += quantity;
                return true;
            }

            for (int i = 0; i < totalCapacity && i < slots.Count; i++)
            {
                if (!slots[i].IsEmpty) continue;
                slots[i].ItemId   = itemId;
                slots[i].Quantity = quantity;
                return true;
            }

            return false;
        }

        // Removes from the first matching slot. itemId-based, not source-slot-index-based —
        // TODO: callers (e.g. Equip) cannot yet target a specific slot when an item exists in multiple stacks.
        public bool RemoveItem(string itemId, int quantity = 1)
        {
            var slot = FindSlot(itemId);
            if (slot == null || slot.Quantity < quantity) return false;

            slot.Quantity -= quantity;
            if (slot.Quantity <= 0) slot.Clear();
            return true;
        }

        public int  GetQuantity(string itemId)               => FindSlot(itemId)?.Quantity ?? 0;
        public bool HasItem(string itemId, int quantity = 1) => GetQuantity(itemId) >= quantity;
        public void ExpandCapacity(int additionalSlots)      => capacity += additionalSlots;

        private InventorySlotData FindSlot(string itemId)
        {
            foreach (var slot in slots)
                if (!slot.IsEmpty && slot.ItemId == itemId) return slot;
            return null;
        }
    }
}
