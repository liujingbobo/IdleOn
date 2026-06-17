using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using IdleOn.Vault;

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
            if (CurrentSave != null) CurrentAccount.CurrentPlayerId = CurrentSave.PlayerId;

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
            OnSaveLoaded?.Invoke();
            return true;
        }

        public string GetSaveFilePath() => Path.Combine(Application.persistentDataPath, SaveFileName);
        public void   DeleteSaveFile()  { string p = GetSaveFilePath(); if (File.Exists(p)) File.Delete(p); }
    }
}
