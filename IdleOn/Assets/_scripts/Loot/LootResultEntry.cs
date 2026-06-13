using System;
using IdleOn.Items;

namespace IdleOn.Loot
{
    [Serializable]
    public class LootResultEntry
    {
        public DropType      DropType;
        public string        ItemId;
        public CurrencyType  CurrencyType;
        public int           Quantity;

        public static LootResultEntry ForItem(string itemId, int qty) =>
            new LootResultEntry { DropType = DropType.Item, ItemId = itemId, Quantity = qty };

        public static LootResultEntry ForCurrency(CurrencyType type, int qty) =>
            new LootResultEntry { DropType = DropType.Currency, CurrencyType = type, Quantity = qty };
    }
}
