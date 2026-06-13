using System;
using UnityEngine;

namespace IdleOn.Items
{
    [Serializable]
    public class DropEntry
    {
        public DropType       DropType;
        public ItemDefinition ItemDefinition;  // used when DropType == Item
        public CurrencyType   CurrencyType;    // used when DropType == Currency
        [Range(0f, 1f)]
        public float          DropChance   = 1f;
        public int            MinQuantity  = 1;
        public int            MaxQuantity  = 1;
    }
}
