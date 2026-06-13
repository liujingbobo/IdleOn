using System;
using System.Collections.Generic;
using UnityEngine;
using IdleOn.Items;

namespace IdleOn.Equipment
{
    [Serializable]
    public class EquipmentSlotEntry
    {
        public EquipmentSlot Slot;
        public string        ItemId;

        public EquipmentSlotEntry() { }
        public EquipmentSlotEntry(EquipmentSlot slot, string itemId)
        {
            Slot   = slot;
            ItemId = itemId;
        }
    }

    [Serializable]
    public class EquipmentData
    {
        [SerializeField] private List<EquipmentSlotEntry> slots = new List<EquipmentSlotEntry>();

        public IReadOnlyList<EquipmentSlotEntry> AllEquipped => slots;

        public string Get(EquipmentSlot slot)
        {
            foreach (var e in slots)
                if (e.Slot == slot) return e.ItemId;
            return null;
        }

        public void Set(EquipmentSlot slot, string itemId)
        {
            foreach (var e in slots)
            {
                if (e.Slot != slot) continue;
                e.ItemId = itemId;
                return;
            }
            slots.Add(new EquipmentSlotEntry(slot, itemId));
        }

        public void Clear(EquipmentSlot slot)
        {
            slots.RemoveAll(e => e.Slot == slot);
        }

        public bool IsOccupied(EquipmentSlot slot) => Get(slot) != null;
    }
}
