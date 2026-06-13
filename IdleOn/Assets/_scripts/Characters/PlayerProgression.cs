using UnityEngine;
using IdleOn.Core;

namespace IdleOn.Characters
{
    public class PlayerProgression : MonoBehaviour
    {
        public float TotalExp { get; private set; }

        void OnEnable()
        {
            GameEvents.OnEnemyKilled += HandleEnemyKilled;
        }

        void OnDisable()
        {
            GameEvents.OnEnemyKilled -= HandleEnemyKilled;
        }

        private void HandleEnemyKilled(string enemyId, float xp)
        {
            TotalExp += xp;
            GameEvents.RaisePlayerExpGained(xp);
            Debug.Log($"[Progression] +{xp} EXP  |  Total: {TotalExp} EXP");
        }
    }
}
