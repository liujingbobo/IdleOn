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

        [SerializeField] private GameObject selectedGroup;
        
        [Header("Background Sprites")]
        [SerializeField] private Sprite craftableSprite;
        [SerializeField] private Sprite notCraftableSprite;

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

            if (background != null && craftableSprite != null && notCraftableSprite != null)
                background.sprite = canCraft ? craftableSprite : notCraftableSprite;

            var icon = Recipe.ResultItem?.Icon;
            itemIcon.sprite = icon != null ? icon : GetPlaceholder();
            itemIcon.color  = icon != null ? Color.white : new Color(0.4f, 0.4f, 0.4f);
            itemName.text   = Recipe.ResultItem?.DisplayName ?? Recipe.RecipeId;
        }

        public void SetSelected(bool selected)
        {
            if(selectedGroup != null)
                selectedGroup.SetActive(selected);
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
