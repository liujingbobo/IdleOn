using System.Collections.Generic;
using UnityEngine;
using IdleOn.Items;

namespace IdleOn.Crafting
{
    [CreateAssetMenu(fileName = "NewRecipe", menuName = "IdleOn/Craft Recipe")]
    public class CraftRecipeDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string RecipeId;

        [Header("Result")]
        public ItemDefinition ResultItem;
        public int ResultQuantity = 1;

        [Header("Ingredients")]
        // Note: the result item must not appear as one of its own ingredients.
        public List<CraftIngredient> RequiredIngredients = new List<CraftIngredient>();

        [Header("Requirements (placeholder — not enforced)")]
        public int RequiredLevel = 1;
    }
}
