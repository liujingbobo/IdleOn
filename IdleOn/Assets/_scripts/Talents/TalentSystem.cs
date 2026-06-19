using System;
using System.Collections.Generic;
using UnityEngine;
using IdleOn.Core;
using IdleOn.Save;
using IdleOn.Characters;
using IdleOn.Skills;

namespace IdleOn.Talents
{
    public class TalentSystem : MonoBehaviour
    {
        public static TalentSystem Instance { get; private set; }

        private List<TalentSaveData> _talents = new List<TalentSaveData>();

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        void OnDisable() => SaveManager.OnSaveLoaded -= Initialize;

        void Start()
        {
            if (SaveManager.Instance != null && SaveManager.Instance.IsLoaded)
                Initialize();
            else
                SaveManager.OnSaveLoaded += Initialize;
        }

        private void Initialize()
        {
            var save = SaveManager.Instance?.CurrentSave;
            if (save == null) return;

            _talents = save.TalentData ?? new List<TalentSaveData>();
            save.TalentData = _talents;
        }

        // ── Read ─────────────────────────────────────────────────────────────

        public int GetLevel(string talentId)
        {
            foreach (var t in _talents)
                if (t.TalentId == talentId) return t.Level;
            return 0;
        }

        public bool CanUpgrade(TalentDefinition def)
        {
            if (def == null) return false;
            var save = SaveManager.Instance?.CurrentSave;
            return save != null && save.TalentPoints > 0 && GetLevel(def.TalentId) < def.MaxLevel;
        }

        // ── Write ─────────────────────────────────────────────────────────────

        public void Upgrade(TalentDefinition def)
        {
            if (!CanUpgrade(def)) return;

            var entry = GetOrCreate(def.TalentId);
            entry.Level++;

            var save = SaveManager.Instance.CurrentSave;
            save.TalentPoints--;

            bool affectsStats = def.ATKMinPerLevel != 0 || def.ATKMaxPerLevel  != 0 ||
                                 def.MaxHPPerLevel  != 0 || def.MoveSpeedPerLevel != 0 ||
                                 def.MaxMPPerLevel  != 0;
            if (affectsStats)
                PlayerStats.Instance?.Recalculate();

            AutoEquipUnlockedSkill(def, entry.Level);

            if (def.InventorySlotsPerLevel != 0)
                GameEvents.RaiseInventoryChanged();

            GameEvents.RaiseTalentChanged();
            GameEvents.RaiseTalentUpgraded(def.TalentId);
            Debug.Log($"[TalentSystem] {def.DisplayName} → Lv.{entry.Level} | Points left: {save.TalentPoints}");
        }

        // ── Auto-equip skills unlocked by this talent ───────────────────────

        private void AutoEquipUnlockedSkill(TalentDefinition def, int newLevel)
        {
            var skillDb = GameDatabase.Instance?.Skills;
            if (skillDb == null) return;

            foreach (var skill in skillDb.Skills)
            {
                if (skill == null || skill.RequiredTalentId != def.TalentId) continue;
                if (newLevel < skill.RequiredTalentLevel) continue;

                EquipSkillToHotbar(skill.SkillId);
            }
        }

        private void EquipSkillToHotbar(string skillId)
        {
            var save = SaveManager.Instance?.CurrentSave;
            if (save == null) return;

            while (save.HotbarSkillIds.Count < 3)
                save.HotbarSkillIds.Add(string.Empty);

            if (save.HotbarSkillIds.Contains(skillId)) return;

            int emptyIndex = save.HotbarSkillIds.IndexOf(string.Empty);
            if (emptyIndex < 0)
            {
                Debug.LogWarning($"[TalentSystem] No empty hotbar slot to auto-equip skill '{skillId}'.");
                return;
            }

            save.HotbarSkillIds[emptyIndex] = skillId;
            GameEvents.RaiseHotbarChanged();
        }

        // ── Stat bonus getters (used by PlayerStats.Recalculate) ────────────

        public float GetATKMinBonus()             => SumBonus(d => d.ATKMinPerLevel);
        public float GetATKMaxBonus()             => SumBonus(d => d.ATKMaxPerLevel);
        public float GetMaxHPBonus()              => SumBonus(d => d.MaxHPPerLevel);
        public float GetMoveSpeedBonus()          => SumBonus(d => d.MoveSpeedPerLevel);
        public float GetMaxMPBonus()              => SumBonus(d => d.MaxMPPerLevel);
        public float GetCurrencyMultiplierBonus() => SumBonus(d => d.CurrencyMultiplierPerLevel);
        public float GetFireballDamageBonus()     => SumBonus(d => d.FireballDamagePerLevel);
        public int   GetInventorySlotBonus()      => Mathf.RoundToInt(SumBonus(d => d.InventorySlotsPerLevel));

        private float SumBonus(Func<TalentDefinition, float> selector)
        {
            float total = 0f;
            var db = GameDatabase.Instance?.Talents;
            if (db == null) return 0f;
            foreach (var def in db.Talents)
                if (def != null) total += GetLevel(def.TalentId) * selector(def);
            return total;
        }

        private TalentSaveData GetOrCreate(string talentId)
        {
            foreach (var t in _talents)
                if (t.TalentId == talentId) return t;
            var entry = new TalentSaveData { TalentId = talentId, Level = 0 };
            _talents.Add(entry);
            return entry;
        }
    }
}
