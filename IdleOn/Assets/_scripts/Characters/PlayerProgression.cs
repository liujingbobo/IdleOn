using UnityEngine;
using IdleOn.Core;

namespace IdleOn.Characters
{
    public class PlayerProgression : MonoBehaviour
    {
        public float TotalExp  { get; private set; }
        public int   Coins     { get; private set; }

        void OnEnable()
        {
            GameEvents.OnEnemyKilled += HandleEnemyKilled;
        }

        void OnDisable()
        {
            GameEvents.OnEnemyKilled -= HandleEnemyKilled;
        }

        private void HandleEnemyKilled(string enemyId, float xp, int coins)
        {
            TotalExp += xp;
            Coins    += coins;
            GameEvents.RaisePlayerExpGained(xp);
            GameEvents.RaisePlayerCoinsChanged(Coins);
            Debug.Log($"[Progression] +{xp} EXP  +{coins} Coins  |  Total: {TotalExp} EXP  {Coins} Coins");
        }
    }
}
