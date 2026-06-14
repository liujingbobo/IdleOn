using UnityEngine;
using UnityEngine.UI;
using TMPro;
using IdleOn.Crafting;
using IdleOn.Inventory;

namespace IdleOn.UI
{
    public class IngredientRowUI : MonoBehaviour
    {
        [SerializeField] private Image     ingredientIcon;
        [SerializeField] private TMP_Text  ingredientName;
        [SerializeField] private TMP_Text  ingredientCount;

        private static readonly Color ColorEnough  = Color.white;
        private static readonly Color ColorMissing = new Color(1f, 0.3f, 0.3f);

        public void Populate(CraftIngredient ingredient)
        {
            if (ingredient?.Item == null) { gameObject.SetActive(false); return; }
            gameObject.SetActive(true);

            int owned  = InventorySystem.Instance?.GetQuantity(ingredient.Item.ItemId) ?? 0;
            bool enough = owned >= ingredient.Quantity;

            ingredientIcon.sprite = ingredient.Item.Icon;
            ingredientIcon.color  = ingredient.Item.Icon != null ? Color.white : new Color(0.4f, 0.4f, 0.4f);
            ingredientName.text   = ingredient.Item.DisplayName;
            ingredientCount.text  = $"{owned} / {ingredient.Quantity}";
            ingredientCount.color = enough ? ColorEnough : ColorMissing;
        }
    }
}
