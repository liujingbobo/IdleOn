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

        public long GetAmount(CurrencyType type)
        {
            var save = Save;
            if (save == null) return 0;
            return type == CurrencyType.Silver ? save.SilverCoins : save.GoldCoins;
        }

        public void Add(CurrencyType type, long amount)
        {
            var save = Save;
            if (save == null) return;

            if (type == CurrencyType.Silver) save.SilverCoins += amount;
            else                             save.GoldCoins   += amount;

            GameEvents.RaiseCurrencyChanged(type, GetAmount(type));
        }

        public bool Spend(CurrencyType type, long amount)
        {
            var save = Save;
            if (save == null || GetAmount(type) < amount) return false;

            if (type == CurrencyType.Silver) save.SilverCoins -= amount;
            else                             save.GoldCoins   -= amount;

            GameEvents.RaiseCurrencyChanged(type, GetAmount(type));
            return true;
        }
    }
}
