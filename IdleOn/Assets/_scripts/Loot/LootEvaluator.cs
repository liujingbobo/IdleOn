using UnityEngine;
using IdleOn.Items;

namespace IdleOn.Loot
{
    public static class LootEvaluator
    {
        public static LootResult Evaluate(LootTable table, float luckMultiplier = 1f)
        {
            var result = new LootResult();
            if (table == null) return result;

            foreach (var entry in table.Entries)
            {
                if (entry == null) continue;

                float effectiveChance = Mathf.Clamp01(entry.DropChance * luckMultiplier);
                if (Random.value > effectiveChance) continue;

                int qty = Random.Range(entry.MinQuantity, entry.MaxQuantity + 1);
                if (qty <= 0) continue;

                if (entry.DropType == DropType.Item)
                {
                    if (entry.ItemDefinition == null) continue;
                    result.Entries.Add(LootResultEntry.ForItem(entry.ItemDefinition.ItemId, qty));
                }
                else
                {
                    result.Entries.Add(LootResultEntry.ForCurrency(entry.CurrencyType, qty));
                }
            }

            return result;
        }
    }
}
