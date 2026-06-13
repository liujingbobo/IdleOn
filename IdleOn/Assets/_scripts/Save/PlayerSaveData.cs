using System;
using System.Collections.Generic;
using IdleOn.Items;

namespace IdleOn.Save
{
    [Serializable]
    public class EquipmentSlotEntry
    {
        public string SlotName;
        public string ItemId;

        public EquipmentSlotEntry() { }
        public EquipmentSlotEntry(string slotName, string itemId)
        {
            SlotName = slotName;
            ItemId   = itemId;
        }
    }

    [Serializable]
    public class PlayerSaveData
    {
        // Progression
        public int   Level = 1;
        public float Exp   = 0f;

        // Currency (not stored in inventory)
        public long SilverCoins = 0;
        public long GoldCoins   = 0;

        // Inventory
        public InventoryData Inventory = new InventoryData(20);

        // Equipment: one entry per filled slot (SlotName → ItemId)
        public List<EquipmentSlotEntry> EquippedItems = new List<EquipmentSlotEntry>();

        // Location — used for offline progression (implemented later)
        public string CurrentMapId   = "town";
        public string LastLogoutTime = string.Empty;

        // Talent / Vault / Quest data added when those systems are implemented
    }
}
