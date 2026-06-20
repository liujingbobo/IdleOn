using System.Collections.Generic;
using UnityEngine;
using IdleOn.Core;
using IdleOn.Save;

namespace IdleOn.World
{
    // Temporary in-memory global kill counter for portal unlock requirements (PortalGate).
    // Not persisted — resets every session. TODO: move into PlayerSaveData as a global
    // enemy-kill-count dictionary once save/load support for this is requested.
    public class EnemyKillTracker : MonoBehaviour
    {
        public static EnemyKillTracker Instance { get; private set; }

        private readonly Dictionary<string, int> _kills = new Dictionary<string, int>();

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void OnEnable()  => GameEvents.OnEnemyKilled += HandleEnemyKilled;
        void OnDisable() => GameEvents.OnEnemyKilled -= HandleEnemyKilled;

        private void HandleEnemyKilled(string enemyId, float xp)
        {
            if (string.IsNullOrEmpty(enemyId)) return;
            _kills.TryGetValue(enemyId, out int count);
            _kills[enemyId] = count + 1;
            MirrorToSave();
        }

        public int GetKillCount(string enemyId)
        {
            if (string.IsNullOrEmpty(enemyId)) return 0;
            return _kills.TryGetValue(enemyId, out int count) ? count : 0;
        }

        public List<EnemyKillSaveData> ExportState()
        {
            var data = new List<EnemyKillSaveData>();
            foreach (var pair in _kills)
                data.Add(new EnemyKillSaveData { EnemyId = pair.Key, Count = pair.Value });
            data.Sort((a, b) => string.CompareOrdinal(a.EnemyId, b.EnemyId));
            return data;
        }

        public void ImportState(List<EnemyKillSaveData> data)
        {
            _kills.Clear();
            if (data != null)
            {
                foreach (var entry in data)
                {
                    if (entry == null || string.IsNullOrEmpty(entry.EnemyId)) continue;
                    int count = Mathf.Max(0, entry.Count);
                    _kills.TryGetValue(entry.EnemyId, out int existing);
                    _kills[entry.EnemyId] = existing + count;
                }
            }
            MirrorToSave();
        }

        private void MirrorToSave()
        {
            var save = SaveManager.Instance?.CurrentSave;
            if (save != null)
                save.EnemyKillCounts = ExportState();
        }
    }
}
