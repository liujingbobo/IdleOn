using UnityEngine;
using IdleOn.Core;
using IdleOn.Inventory;

namespace IdleOn.Crafting
{
    public class CraftingSystem : MonoBehaviour
    {
        public static CraftingSystem Instance { get; private set; }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public bool CanCraft(CraftRecipeDefinition recipe)
        {
            if (recipe == null) return false;
            var inv = InventorySystem.Instance;
            if (inv == null) return false;

            foreach (var ingredient in recipe.RequiredIngredients)
            {
                if (ingredient.Item == null) continue;
                if (inv.GetQuantity(ingredient.Item.ItemId) < ingredient.Quantity)
                    return false;
            }
            return true;
        }

        // Note: recipes must not use their own result item as an ingredient.
        public bool Craft(CraftRecipeDefinition recipe)
        {
            if (recipe == null || recipe.ResultItem == null) return false;
            if (!CanCraft(recipe)) return false;

            var inv = InventorySystem.Instance;

            // Add result first — if inventory is full, abort before consuming anything.
            if (!inv.TryAddItem(recipe.ResultItem.ItemId, recipe.ResultQuantity))
            {
                GameEvents.RaiseInventoryFull();
                return false;
            }

            foreach (var ingredient in recipe.RequiredIngredients)
            {
                if (ingredient.Item == null) continue;
                inv.RemoveItem(ingredient.Item.ItemId, ingredient.Quantity);
            }

            return true;
        }
    }
}
