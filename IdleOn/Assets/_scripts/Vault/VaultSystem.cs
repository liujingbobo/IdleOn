using UnityEngine;
using IdleOn.Core;
using IdleOn.Inventory;
using IdleOn.Items;
using IdleOn.Save;
using IdleOn.Characters;

namespace IdleOn.Vault
{
    public class VaultSystem : MonoBehaviour
    {
        public static VaultSystem Instance { get; private set; }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private VaultDatabase DB   => GameDatabase.Instance?.Vault;
        private VaultSaveData Save => SaveManager.Instance?.CurrentVault;

        public int GetLevel(VaultUpgradeDefinition def)
        {
            if (def == null) return 0;
            return Save?.GetLevel(def.UpgradeId) ?? 0;
        }

        public long GetCost(VaultUpgradeDefinition def)
        {
            if (def == null) return 0;
            return def.GetCost(GetLevel(def));
        }

        public bool CanUpgrade(VaultUpgradeDefinition def)
        {
            if (def == null || Save == null) return false;
            if (GetLevel(def) >= def.MaxLevel) return false;
            return CurrencySystem.Instance?.GetAmount(CurrencyType.Gold) >= GetCost(def);
        }

        public bool Upgrade(VaultUpgradeDefinition def)
        {
            if (!CanUpgrade(def)) return false;
            if (!CurrencySystem.Instance.Spend(CurrencyType.Gold, GetCost(def))) return false;

            Save.SetLevel(def.UpgradeId, GetLevel(def) + 1);

            if (def.UpgradeType == VaultUpgradeType.BiggerDamage)
                PlayerStats.Instance?.Recalculate();

            GameEvents.RaiseVaultChanged();
            return true;
        }

        // ── Bonus getters used by other systems ──────────────────────────────

        public float GetATKMinBonus()
        {
            var def = GetDefByType(VaultUpgradeType.BiggerDamage);
            return def != null ? GetLevel(def) * def.BonusPerLevel : 0f;
        }

        public float GetATKMaxBonus()
        {
            var def = GetDefByType(VaultUpgradeType.BiggerDamage);
            return def != null ? GetLevel(def) * def.BonusPerLevel2 : 0f;
        }

        public float GetCurrencyMultiplier()
        {
            var def = GetDefByType(VaultUpgradeType.MonsterTax);
            return def != null ? 1f + GetLevel(def) * def.BonusPerLevel : 1f;
        }

        public int GetTalentPointBonus()
        {
            var def = GetDefByType(VaultUpgradeType.NaturalTalent);
            return def != null ? (int)(GetLevel(def) * def.BonusPerLevel) : 0;
        }

        private VaultUpgradeDefinition GetDefByType(VaultUpgradeType type)
        {
            var db = DB;
            if (db == null) return null;
            foreach (var def in db.Upgrades)
                if (def != null && def.UpgradeType == type) return def;
            return null;
        }
    }
}
