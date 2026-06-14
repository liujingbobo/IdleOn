using System;
using UnityEngine;
using IdleOn.Items;

namespace IdleOn.Crafting
{
    [Serializable]
    public class CraftIngredient
    {
        public ItemDefinition Item;
        public int Quantity = 1;
    }
}
