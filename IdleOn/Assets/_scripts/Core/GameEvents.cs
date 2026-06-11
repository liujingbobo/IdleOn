using System;

namespace IdleOn.Core
{
    public static class GameEvents
    {
        // Combat
        public static event Action<bool> OnAutoCombatChanged;
        public static event Action<string, float, int> OnEnemyKilled; // enemyId, xpReward, coinReward

        // Player HP
        public static event Action<float, float> OnPlayerHPChanged; // current, max

        // Progression
        public static event Action<float> OnPlayerExpGained;  // amount gained this kill
        public static event Action<int>   OnPlayerCoinsChanged; // new total

        public static void RaiseAutoCombatChanged(bool active)                     => OnAutoCombatChanged?.Invoke(active);
        public static void RaiseEnemyKilled(string enemyId, float xp, int coins)   => OnEnemyKilled?.Invoke(enemyId, xp, coins);
        public static void RaisePlayerHPChanged(float current, float max)          => OnPlayerHPChanged?.Invoke(current, max);
        public static void RaisePlayerExpGained(float amount)                      => OnPlayerExpGained?.Invoke(amount);
        public static void RaisePlayerCoinsChanged(int total)                      => OnPlayerCoinsChanged?.Invoke(total);
    }
}
