using System.Collections.Generic;
using UnityEngine;
using IdleOn.Core;

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
            _kills.TryGetValue(enemyId, out int count);
            _kills[enemyId] = count + 1;
        }

        public int GetKillCount(string enemyId)
        {
            if (string.IsNullOrEmpty(enemyId)) return 0;
            return _kills.TryGetValue(enemyId, out int count) ? count : 0;
        }
    }
}
