using System;
using IdleOn.Items;
using IdleOn.Equipment;

namespace IdleOn.Save
{
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
        public InventoryData  Inventory  = new InventoryData(20);

        // Equipment: one entry per occupied slot
        public EquipmentData  Equipment  = new EquipmentData();

        // Location — used for offline progression (implemented later)
        public string CurrentMapId   = "town";
        public string LastLogoutTime = string.Empty;

        // Talent / Vault / Quest data added when those systems are implemented
    }
}
