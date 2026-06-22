using System;
using System.Collections.Generic;
using UnityEngine;
using IdleOn.Combat;
using IdleOn.Save;
using IdleOn.Quests;
using IdleOn.Items;
using IdleOn.Inventory;
using IdleOn.UI;

namespace IdleOn.World
{
    // Minimal offline earnings: snapshots logout state on save, grants a one-time reward on the
    // next character load if the tutorial is complete, AutoCombat was on, and the last map qualifies.
    public class OfflineEarningsSystem : MonoBehaviour
    {
        public static OfflineEarningsSystem Instance { get; private set; }

        [SerializeField] private PlayerCombatController combatController;
        [SerializeField] private OfflineEarningsWindowUI windowUI;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        void OnEnable()  => SaveManager.OnSaveLoaded += HandleSaveLoaded;
        void OnDisable() => SaveManager.OnSaveLoaded -= HandleSaveLoaded;
        void OnDestroy() { if (Instance == this) Instance = null; }

        // Called by SaveManager right before serializing — mirrors the Quest/FeatureUnlock export pattern.
        public void RecordLogoutSnapshot()
        {
            var save = SaveManager.Instance != null ? SaveManager.Instance.CurrentSave : null;
            if (save == null) return;

            save.LastLogoutUtcTicks      = DateTime.UtcNow.Ticks;
            save.LastOfflineMapId        = MapSystem.Instance != null ? MapSystem.Instance.CurrentMapId : string.Empty;
            save.WasAutoCombatOnAtLogout = combatController != null && combatController.IsAutoCombatActive;
        }

        private void HandleSaveLoaded()
        {
            var save = SaveManager.Instance != null ? SaveManager.Instance.CurrentSave : null;
            if (save == null || save.LastLogoutUtcTicks <= 0) return;

            long ticksElapsed = DateTime.UtcNow.Ticks - save.LastLogoutUtcTicks;
            long offlineSeconds = Math.Max(1L, ticksElapsed / TimeSpan.TicksPerSecond);

            // Consume the snapshot immediately so a second load can't grant the same reward twice.
            string map = save.LastOfflineMapId;
            bool autoCombatWasOn = save.WasAutoCombatOnAtLogout;
            save.LastLogoutUtcTicks      = 0;
            save.LastOfflineMapId        = string.Empty;
            save.WasAutoCombatOnAtLogout = false;

            if (!autoCombatWasOn) return;
            if (QuestSystem.Instance == null || !QuestSystem.Instance.IsCompleted("q12")) return;

            long gold, essence;
            switch (map)
            {
                case "grassland_2": gold = offlineSeconds * 50;  essence = offlineSeconds * 2; break;
                case "grassland_3": gold = offlineSeconds * 120; essence = offlineSeconds * 5; break;
                default: return;
            }

            CurrencySystem.Instance?.Add(CurrencyType.Gold, gold);
            InventorySystem.Instance?.TryAddItem("slime_essence", (int)essence);

            windowUI?.Show(new List<(string Label, long Amount)>
            {
                ("Gold", gold),
                ("Slime Essence", essence)
            });
        }
    }
}
