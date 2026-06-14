using System.Collections.Generic;
using UnityEngine;

namespace IdleOn.Crafting
{
    [CreateAssetMenu(fileName = "CraftingDatabase", menuName = "IdleOn/Crafting Database")]
    public class CraftingDatabase : ScriptableObject
    {
        [SerializeField] private List<CraftRecipeDefinition> recipes = new List<CraftRecipeDefinition>();

        public IReadOnlyList<CraftRecipeDefinition> Recipes => recipes;
    }
}
