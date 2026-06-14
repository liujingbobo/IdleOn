using UnityEngine;

namespace IdleOn.Vault
{
    [CreateAssetMenu(fileName = "NewVaultUpgrade", menuName = "IdleOn/Vault Upgrade")]
    public class VaultUpgradeDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string           UpgradeId;
        public string           DisplayName;
        public VaultUpgradeType UpgradeType;

        [Header("Progression")]
        public int   MaxLevel       = 10;
        public long  BaseCost       = 100;
        public float CostGrowthRate = 1.5f;

        [Header("Bonus Per Level")]
        public float BonusPerLevel  = 1f;  // ATKMin for BiggerDamage; 0.05 for MonsterTax; 1 for NaturalTalent
        public float BonusPerLevel2 = 0f;  // ATKMax for BiggerDamage (unused by other types)

        public long GetCost(int currentLevel)
            => (long)(BaseCost * Mathf.Pow(CostGrowthRate, currentLevel));

        public string GetCurrentEffectText(int level)
        {
            if (level == 0) return "No bonus yet";
            switch (UpgradeType)
            {
                case VaultUpgradeType.BiggerDamage:
                    return $"ATK +{level * BonusPerLevel:0.#} ~ +{level * BonusPerLevel2:0.#}";
                case VaultUpgradeType.MonsterTax:
                    return $"+{level * BonusPerLevel * 100f:0}% coin gain";
                case VaultUpgradeType.NaturalTalent:
                    return $"+{(int)(level * BonusPerLevel)} talent pts";
                default:
                    return $"+{level * BonusPerLevel:0.#}";
            }
        }
    }
}
