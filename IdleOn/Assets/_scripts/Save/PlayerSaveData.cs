using System;
using System.Collections.Generic;
using IdleOn.Items;
using IdleOn.Equipment;
using IdleOn.World;
using IdleOn.Talents;

namespace IdleOn.Save
{
    [Serializable]
    public class PlayerSaveData
    {
        // Identity (per-character)
        public string PlayerId   = string.Empty;
        public string PlayerName = "Hero";

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

        // (Vault upgrades moved to AccountSaveData.Vault — vault is account-shared.)

        // Location — used for offline progression (implemented later)
        public string CurrentMapId   = "grassland_1";
        public string LastLogoutTime = string.Empty;

        // Map progression
        public List<MapProgressData> MapProgress = new List<MapProgressData>();

        // Tutorial vertical-slice progression
        public QuestSaveData Quest = new QuestSaveData();
        public int UnlockedFeatureFlags = 0;
        public List<EnemyKillSaveData> EnemyKillCounts = new List<EnemyKillSaveData>();

        // Talent levels
        public List<TalentSaveData> TalentData = new List<TalentSaveData>();

        // Skill hotbar (3 slots; empty string = unassigned)
        public List<string> HotbarSkillIds = new List<string> { "", "", "" };
    }
}
