using System;
using IdleOn.Items;

namespace IdleOn.Core
{
    public static class GameEvents
    {
        // Combat
        public static event Action<bool>          OnAutoCombatChanged;
        public static event Action<string, float> OnEnemyKilled;       // enemyId, xpReward

        // Player HP
        public static event Action<float, float>  OnPlayerHPChanged;   // current, max

        // Progression
        public static event Action<float>         OnPlayerExpGained;   // amount gained this kill

        // Inventory
        public static event Action                OnInventoryChanged;
        public static event Action                OnInventoryFull;

        // Currency
        public static event Action<CurrencyType, long> OnCurrencyChanged; // type, new total

        // Equipment
        public static event Action OnEquipmentChanged;

        public static void RaiseAutoCombatChanged(bool active)                          => OnAutoCombatChanged?.Invoke(active);
        public static void RaiseEnemyKilled(string enemyId, float xp)                  => OnEnemyKilled?.Invoke(enemyId, xp);
        public static void RaisePlayerHPChanged(float current, float max)              => OnPlayerHPChanged?.Invoke(current, max);
        public static void RaisePlayerExpGained(float amount)                          => OnPlayerExpGained?.Invoke(amount);
        public static void RaiseInventoryChanged()                                     => OnInventoryChanged?.Invoke();
        public static void RaiseInventoryFull()                                        => OnInventoryFull?.Invoke();
        public static void RaiseCurrencyChanged(CurrencyType type, long newTotal)      => OnCurrencyChanged?.Invoke(type, newTotal);
        public static void RaiseEquipmentChanged()                                     => OnEquipmentChanged?.Invoke();
    }
}
