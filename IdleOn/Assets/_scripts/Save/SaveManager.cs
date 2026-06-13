using System;
using System.IO;
using UnityEngine;

namespace IdleOn.Save
{
    public class SaveManager : MonoBehaviour
    {
        public static SaveManager Instance { get; private set; }

        public PlayerSaveData CurrentSave { get; private set; }
        public bool           IsLoaded    { get; private set; }

        public static event Action OnSaveLoaded;

        private const string SaveFileName = "player_save.json";

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

        // ── Runtime entry point ─────────────────────────────────────────────
        // Called by GameBootstrap. Creates a fresh in-memory save for this session.

        public void CreateNewSave()
        {
            CurrentSave = new PlayerSaveData();
            IsLoaded    = true;
            OnSaveLoaded?.Invoke();
        }

        // ── JSON methods ────────────────────────────────────────────────────
        // Implemented but NOT called automatically.
        // Call SaveToDisk() from a save-trigger (e.g. on logout, on zone transition).
        // Call LoadFromDisk() when you want to restore a persisted session.

        public void SaveToDisk()
        {
            string json = JsonUtility.ToJson(CurrentSave, prettyPrint: true);
            File.WriteAllText(GetSaveFilePath(), json);
            Debug.Log($"[SaveManager] Saved → {GetSaveFilePath()}");
        }

        public bool LoadFromDisk()
        {
            string path = GetSaveFilePath();
            if (!File.Exists(path)) return false;

            string json = File.ReadAllText(path);
            CurrentSave = JsonUtility.FromJson<PlayerSaveData>(json);
            IsLoaded    = true;
            OnSaveLoaded?.Invoke();
            Debug.Log($"[SaveManager] Loaded ← {path}");
            return true;
        }

        public bool   HasSaveFile()    => File.Exists(GetSaveFilePath());
        public void   DeleteSaveFile() { string p = GetSaveFilePath(); if (File.Exists(p)) File.Delete(p); }
        public string GetSaveFilePath() => Path.Combine(Application.persistentDataPath, SaveFileName);
    }
}
