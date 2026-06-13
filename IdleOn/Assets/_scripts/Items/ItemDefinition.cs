using UnityEngine;
using IdleOn.Core;

namespace IdleOn.Items
{
    [CreateAssetMenu(fileName = "NewItem", menuName = "IdleOn/Item Definition")]
    public class ItemDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string   ItemId;
        public string   DisplayName;
        [TextArea(2, 4)]
        public string   Description;
        public ItemType ItemType;
        public Sprite   Icon;
        public int      SellValue;

        [Header("Rarity")]
        public ItemRarity Rarity = ItemRarity.Common;

        [Header("Equipment")]
        public EquipmentSlot EquipmentSlot;
        public StatSheet     StatBonuses;       // applied when equipped; placeholder for now

        [Header("Consumable")]
        public ConsumableType ConsumableType;
        // Apply(PlayerStats) effect implementation deferred — architecture hook only
    }
}
