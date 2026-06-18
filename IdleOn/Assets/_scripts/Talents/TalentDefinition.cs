using UnityEngine;

namespace IdleOn.Talents
{
    [CreateAssetMenu(fileName = "TalentDef_", menuName = "IdleOn/Talent Definition")]
    public class TalentDefinition : ScriptableObject
    {
        [Header("Visuals")]
        public Sprite Icon;

        [Header("Identity")]
        public string TalentId;
        public string DisplayName;
        [TextArea(1, 2)]
        public string Description;
        public int    MaxLevel = 5;

        [Header("Stat Bonuses per Level")]
        public float ATKMinPerLevel;
        public float ATKMaxPerLevel;
        public float MaxHPPerLevel;
        public float MoveSpeedPerLevel;
        public float MaxMPPerLevel;

        [Header("Non-Stat Bonuses per Level")]
        public float CurrencyMultiplierPerLevel; // e.g. 0.05 = +5% per level
        public float FireballDamagePerLevel;     // reserved for Phase 2
        public float InventorySlotsPerLevel;     // e.g. 20 = +20 inventory slots per level

        // Returns cumulative effect at given level. If level=0, shows per-level preview.
        public string GetEffectText(int level)
        {
            bool preview = level <= 0;
            int  lv      = preview ? 1 : level;

            var sb = new System.Text.StringBuilder();

            if (ATKMinPerLevel != 0 || ATKMaxPerLevel != 0)
                sb.Append($"ATK +{Mathf.RoundToInt(lv * ATKMinPerLevel)}/+{Mathf.RoundToInt(lv * ATKMaxPerLevel)}  ");
            if (MaxHPPerLevel != 0)
                sb.Append($"MaxHP +{Mathf.RoundToInt(lv * MaxHPPerLevel)}  ");
            if (MoveSpeedPerLevel != 0)
                sb.Append($"Speed +{lv * MoveSpeedPerLevel:F1}  ");
            if (MaxMPPerLevel != 0)
                sb.Append($"MaxMP +{Mathf.RoundToInt(lv * MaxMPPerLevel)}  ");
            if (CurrencyMultiplierPerLevel != 0)
                sb.Append($"Silver +{Mathf.RoundToInt(lv * CurrencyMultiplierPerLevel * 100)}%  ");
            if (FireballDamagePerLevel != 0)
                sb.Append($"Fireball +{Mathf.RoundToInt(lv * FireballDamagePerLevel)}  ");
            if (InventorySlotsPerLevel != 0)
                sb.Append($"Inventory +{Mathf.RoundToInt(lv * InventorySlotsPerLevel)} slots  ");

            string result = sb.Length > 0 ? sb.ToString().TrimEnd() : "—";
            return preview ? result + " /lv" : result;
        }
    }
}
