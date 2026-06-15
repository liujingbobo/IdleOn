using System;
using System.Collections.Generic;
using UnityEngine;
using IdleOn.Core;
using IdleOn.Save;
using IdleOn.Characters;

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

            GameEvents.RaiseTalentChanged();
            Debug.Log($"[TalentSystem] {def.DisplayName} → Lv.{entry.Level} | Points left: {save.TalentPoints}");
        }

        // ── Stat bonus getters (used by PlayerStats.Recalculate) ────────────

        public float GetATKMinBonus()             => SumBonus(d => d.ATKMinPerLevel);
        public float GetATKMaxBonus()             => SumBonus(d => d.ATKMaxPerLevel);
        public float GetMaxHPBonus()              => SumBonus(d => d.MaxHPPerLevel);
        public float GetMoveSpeedBonus()          => SumBonus(d => d.MoveSpeedPerLevel);
        public float GetMaxMPBonus()              => SumBonus(d => d.MaxMPPerLevel);
        public float GetCurrencyMultiplierBonus() => SumBonus(d => d.CurrencyMultiplierPerLevel);
        public float GetFireballDamageBonus()     => SumBonus(d => d.FireballDamagePerLevel);

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
