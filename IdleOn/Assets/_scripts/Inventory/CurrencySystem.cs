using UnityEngine;
using IdleOn.Save;
using IdleOn.Core;
using IdleOn.Items;

namespace IdleOn.Inventory
{
    public class CurrencySystem : MonoBehaviour
    {
        public static CurrencySystem Instance { get; private set; }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private PlayerSaveData Save => SaveManager.Instance?.CurrentSave;

        // Silver is unused — demo tracks/saves Gold only. CurrencyType.Silver is kept only
        // for ordinal stability of existing serialized data (Gold must stay ordinal 1).
        public long GetAmount(CurrencyType type)
        {
            var save = Save;
            if (save == null || type != CurrencyType.Gold) return 0;
            return save.GoldCoins;
        }

        public void Add(CurrencyType type, long amount)
        {
            var save = Save;
            if (save == null || type != CurrencyType.Gold) return;

            save.GoldCoins += amount;

            GameEvents.RaiseCurrencyChanged(type, GetAmount(type));
        }

        public bool Spend(CurrencyType type, long amount)
        {
            var save = Save;
            if (save == null || type != CurrencyType.Gold || GetAmount(type) < amount) return false;

            save.GoldCoins -= amount;

            GameEvents.RaiseCurrencyChanged(type, GetAmount(type));
            return true;
        }
    }
}
