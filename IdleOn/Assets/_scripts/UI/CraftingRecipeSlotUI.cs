using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using IdleOn.Crafting;

namespace IdleOn.UI
{
    public class CraftingRecipeSlotUI : MonoBehaviour
    {
        [SerializeField] private Image    itemIcon;
        [SerializeField] private TMP_Text itemName;
        [SerializeField] private Image    canCraftDot;
        [SerializeField] private Button   button;
        [SerializeField] private Image    background;

        [Header("Colors")]
        [SerializeField] private Color selectedColor   = new Color(0.3f, 0.5f, 0.8f, 1f);
        [SerializeField] private Color normalColor     = new Color(0.15f, 0.15f, 0.15f, 1f);
        [SerializeField] private Color canCraftColor   = Color.green;
        [SerializeField] private Color cannotCraftColor = Color.grey;

        public CraftRecipeDefinition Recipe { get; private set; }
        private Action<CraftRecipeDefinition> _onSelected;

        private static Sprite _greyPlaceholder;

        public void Initialize(CraftRecipeDefinition recipe, Action<CraftRecipeDefinition> onSelected)
        {
            Recipe      = recipe;
            _onSelected = onSelected;
            button.onClick.AddListener(() => _onSelected?.Invoke(Recipe));
            Refresh();
        }

        public void Refresh()
        {
            if (Recipe == null) return;

            bool canCraft = CraftingSystem.Instance != null && CraftingSystem.Instance.CanCraft(Recipe);
            canCraftDot.color = canCraft ? canCraftColor : cannotCraftColor;

            var icon = Recipe.ResultItem?.Icon;
            itemIcon.sprite = icon != null ? icon : GetPlaceholder();
            itemIcon.color  = icon != null ? Color.white : new Color(0.4f, 0.4f, 0.4f);
            itemName.text   = Recipe.ResultItem?.DisplayName ?? Recipe.RecipeId;
        }

        public void SetSelected(bool selected)
        {
            if (background != null)
                background.color = selected ? selectedColor : normalColor;
        }

        private static Sprite GetPlaceholder()
        {
            if (_greyPlaceholder != null) return _greyPlaceholder;
            var tex    = new Texture2D(32, 32);
            var pixels = new Color[32 * 32];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color(0.4f, 0.4f, 0.4f);
            tex.SetPixels(pixels);
            tex.Apply();
            _greyPlaceholder = Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f));
            return _greyPlaceholder;
        }
    }
}
