using System;
using System.Collections.Generic;
using IdleOn.Items;
using IdleOn.Equipment;
using IdleOn.Vault;
using IdleOn.World;
using IdleOn.Talents;

namespace IdleOn.Save
{
    [Serializable]
    public class PlayerSaveData
    {
        // Progression
        public int   Level        = 1;
        public float Exp          = 0f;
        public int   TalentPoints = 0;

        // Currency (not stored in inventory)
        public long SilverCoins = 0;
        public long GoldCoins   = 0;

        // Inventory
        public InventoryData  Inventory  = new InventoryData(20);

        // Equipment: one entry per occupied slot
        public EquipmentData  Equipment  = new EquipmentData();

        // Vault upgrades
        public VaultSaveData VaultData = new VaultSaveData();

        // Location — used for offline progression (implemented later)
        public string CurrentMapId   = "grassland_1";
        public string LastLogoutTime = string.Empty;

        // Map progression
        public List<MapProgressData> MapProgress = new List<MapProgressData>();

        // Talent levels
        public List<TalentSaveData> TalentData = new List<TalentSaveData>();

        // Skill hotbar (3 slots; empty string = unassigned)
        public List<string> HotbarSkillIds = new List<string> { "", "", "" };
    }
}
