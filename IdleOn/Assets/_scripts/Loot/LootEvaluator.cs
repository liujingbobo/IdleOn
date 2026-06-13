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

            Debug.Log($"[LootEvaluator] Evaluating '{table.name}' ({table.Entries.Count} entries, luck={luckMultiplier}).");

            foreach (var entry in table.Entries)
            {
                if (entry == null) { Debug.LogWarning("[LootEvaluator] Null entry skipped."); continue; }

                float roll            = Random.value;
                float effectiveChance = Mathf.Clamp01(entry.DropChance * luckMultiplier);

                if (entry.DropType == DropType.Item)
                {
                    string itemName = entry.ItemDefinition != null ? entry.ItemDefinition.ItemId : "NULL";
                    if (roll > effectiveChance)
                    {
                        Debug.Log($"[LootEvaluator]   Item '{itemName}': roll {roll:F2} > chance {effectiveChance:F2} — MISS.");
                        continue;
                    }
                    if (entry.ItemDefinition == null)
                    {
                        Debug.LogWarning($"[LootEvaluator]   Item entry passed roll but ItemDefinition is null — skipped.");
                        continue;
                    }
                    int qty = Random.Range(entry.MinQuantity, entry.MaxQuantity + 1);
                    Debug.Log($"[LootEvaluator]   Item '{itemName}': roll {roll:F2} <= chance {effectiveChance:F2} — HIT x{qty}.");
                    result.Entries.Add(LootResultEntry.ForItem(entry.ItemDefinition.ItemId, qty));
                }
                else
                {
                    if (roll > effectiveChance)
                    {
                        Debug.Log($"[LootEvaluator]   Currency '{entry.CurrencyType}': roll {roll:F2} > chance {effectiveChance:F2} — MISS.");
                        continue;
                    }
                    int qty = Random.Range(entry.MinQuantity, entry.MaxQuantity + 1);
                    Debug.Log($"[LootEvaluator]   Currency '{entry.CurrencyType}': roll {roll:F2} <= chance {effectiveChance:F2} — HIT x{qty}.");
                    result.Entries.Add(LootResultEntry.ForCurrency(entry.CurrencyType, qty));
                }
            }

            return result;
        }
    }
}
