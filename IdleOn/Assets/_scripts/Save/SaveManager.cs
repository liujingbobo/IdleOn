using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using IdleOn.Vault;
using IdleOn.Quests;
using IdleOn.World;
using IdleOn.Core;
using IdleOn.Items;
using IdleOn.Equipment;
using IdleOn.Talents;

namespace IdleOn.Save
{
    // Runs before default-order components (e.g. StartupMenu) so Instance is ready early,
    // but after GameBootstrap (-100).
    [DefaultExecutionOrder(-50)]
    public class SaveManager : MonoBehaviour
    {
        public static SaveManager Instance { get; private set; }

        // Top-level account save (shared vault + character list).
        public AccountSaveData CurrentAccount { get; private set; }

        // The currently selected character. Existing systems read this — name unchanged.
        public PlayerSaveData CurrentSave { get; private set; }

        // Account-shared vault data.
        public VaultSaveData CurrentVault => CurrentAccount?.Vault;

        // True once a character has been selected (gameplay may initialise).
        public bool IsLoaded { get; private set; }

        public static event Action OnSaveLoaded;

        private const string SaveFileName = "account_save.json";

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // ── Save-on-quit ────────────────────────────────────────────────────
        void OnApplicationQuit()                 => AutoSave();
        void OnApplicationPause(bool paused)     { if (paused) AutoSave(); }
        private void AutoSave()                  { if (IsLoaded) SaveAccountToDisk(); }

        // ── Account lifecycle ───────────────────────────────────────────────

        public bool HasSaveFile() => File.Exists(GetSaveFilePath());

        public void CreateNewAccount()
        {
            CurrentAccount = new AccountSaveData();
            CurrentSave    = null;
            IsLoaded       = false;
        }

        public bool LoadAccountFromDisk()
        {
            string path = GetSaveFilePath();
            if (!File.Exists(path)) return false;

            try
            {
                string json = File.ReadAllText(path);
                var acc = JsonUtility.FromJson<AccountSaveData>(json);
                if (acc == null) return false;

                // Defensive defaults for forward/backward tolerance.
                if (acc.Vault   == null) acc.Vault   = new VaultSaveData();
                if (acc.Players == null) acc.Players = new List<PlayerSaveData>();
                foreach (var player in acc.Players)
                    NormalizePlayerSave(player);
                if (acc.Version < 2) acc.Version = 2;

                CurrentAccount = acc;
                CurrentSave    = null;
                IsLoaded       = false;   // selection happens via SelectCharacter
                Debug.Log($"[SaveManager] Loaded account ← {path} ({acc.Players.Count} characters)");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveManager] Failed to load account: {e.Message}");
                return false;
            }
        }

        public void SaveAccountToDisk()
        {
            if (CurrentAccount == null) return;
            if (CurrentSave != null)
            {
                CurrentAccount.CurrentPlayerId = CurrentSave.PlayerId;
                if (QuestSystem.Instance != null)
                    CurrentSave.Quest = QuestSystem.Instance.ExportState();
                if (FeatureUnlockSystem.Instance != null)
                    CurrentSave.UnlockedFeatureFlags = FeatureUnlockSystem.Instance.ExportState();
                if (EnemyKillTracker.Instance != null)
                    CurrentSave.EnemyKillCounts = EnemyKillTracker.Instance.ExportState();
                if (OfflineEarningsSystem.Instance != null)
                    OfflineEarningsSystem.Instance.RecordLogoutSnapshot();
            }

            string json = JsonUtility.ToJson(CurrentAccount, prettyPrint: true);
            File.WriteAllText(GetSaveFilePath(), json);
            Debug.Log($"[SaveManager] Saved account → {GetSaveFilePath()}");
        }

        // ── Character lifecycle ─────────────────────────────────────────────

        public PlayerSaveData CreateNewCharacter(string name)
        {
            if (CurrentAccount == null) CreateNewAccount();

            string finalName = string.IsNullOrEmpty(name)
                ? "Hero " + (CurrentAccount.Players.Count + 1)
                : name;

            var p = new PlayerSaveData
            {
                PlayerId   = Guid.NewGuid().ToString("N"),
                PlayerName = finalName,
            };
            CurrentAccount.Players.Add(p);
            return p;
        }

        // Sets the active character and notifies systems via OnSaveLoaded.
        public bool SelectCharacter(string playerId)
        {
            var p = CurrentAccount?.FindPlayer(playerId);
            if (p == null) return false;

            CurrentSave = p;
            CurrentAccount.CurrentPlayerId = p.PlayerId;
            IsLoaded = true;

            NormalizePlayerSave(CurrentSave);
            QuestSystem.Instance?.ImportState(CurrentSave.Quest);
            FeatureUnlockSystem.Instance?.ImportState(CurrentSave.UnlockedFeatureFlags);
            EnemyKillTracker.Instance?.ImportState(CurrentSave.EnemyKillCounts);

            OnSaveLoaded?.Invoke();
            GameEvents.RaisePersistentProgressLoaded();
            return true;
        }

        private static void NormalizePlayerSave(PlayerSaveData player)
        {
            if (player == null) return;

            if (player.Inventory == null) player.Inventory = new InventoryData(20);
            if (player.Equipment == null) player.Equipment = new EquipmentData();
            if (player.MapProgress == null) player.MapProgress = new List<MapProgressData>();
            if (player.TalentData == null) player.TalentData = new List<TalentSaveData>();
            if (player.HotbarSkillIds == null)
                player.HotbarSkillIds = new List<string> { "", "", "" };

            if (player.Quest == null) player.Quest = new QuestSaveData();
            if (player.Quest.CompletedQuestIds == null)
                player.Quest.CompletedQuestIds = new List<string>();
            if (player.Quest.ActiveObjectiveCounts == null)
                player.Quest.ActiveObjectiveCounts = new List<int>();
            if (player.EnemyKillCounts == null)
                player.EnemyKillCounts = new List<EnemyKillSaveData>();
        }

        public string GetSaveFilePath() => Path.Combine(Application.persistentDataPath, SaveFileName);
        public void   DeleteSaveFile()  { string p = GetSaveFilePath(); if (File.Exists(p)) File.Delete(p); }
    }
}
