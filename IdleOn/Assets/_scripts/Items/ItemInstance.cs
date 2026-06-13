namespace IdleOn.Items
{
    // Runtime representation of a stack of one item type.
    // Serialized form is InventorySlotData — convert with ToSlotData().
    public class ItemInstance
    {
        public ItemDefinition Definition { get; }
        public int            Quantity   { get; private set; }

        public ItemInstance(ItemDefinition definition, int quantity)
        {
            Definition = definition;
            Quantity   = quantity;
        }

        public void Add(int amount)    => Quantity += amount;
        public bool CanRemove(int amount) => Quantity >= amount;
        public void Remove(int amount) => Quantity -= amount;

        public InventorySlotData ToSlotData() =>
            new InventorySlotData(Definition.ItemId, Quantity);
    }
}
