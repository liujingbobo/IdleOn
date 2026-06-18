using System;

namespace IdleOn.Items
{
    [Serializable]
    public class InventorySlotData
    {
        public string ItemId;
        public int    Quantity;

        public InventorySlotData(string itemId, int quantity)
        {
            ItemId   = itemId;
            Quantity = quantity;
        }

        public bool IsEmpty => string.IsNullOrEmpty(ItemId) || Quantity <= 0;

        public void Clear()
        {
            ItemId   = null;
            Quantity = 0;
        }
    }
}
