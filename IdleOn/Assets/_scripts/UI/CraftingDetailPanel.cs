using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using IdleOn.Crafting;
using IdleOn.Core;

namespace IdleOn.UI
{
    public class CraftingDetailPanel : MonoBehaviour
    {
        [Header("Item Info")]
        [SerializeField] private Image    itemInfoIcon;
        [SerializeField] private TMP_Text itemInfoName;
        [SerializeField] private TMP_Text itemInfoDescription;

        [Header("Ingredients")]
        [SerializeField] private Transform       ingredientsContainer;
        [SerializeField] private IngredientRowUI ingredientRowPrefab;

        [Header("Craft")]
        [SerializeField] private Button craftButton;

        private CraftRecipeDefinition _recipe;
        private readonly List<IngredientRowUI> _rows = new List<IngredientRowUI>();

        void Awake()
        {
            // Demo rows are hand-placed in the scene for art reference; clear them before runtime rows take over.
            foreach (Transform child in ingredientsContainer)
                Destroy(child.gameObject);

            craftButton.onClick.AddListener(OnCraftClicked);
            GameEvents.OnInventoryChanged += OnInventoryChanged;

            // Awake runs even while the window starts inactive; Start would be deferred until first activation,
            // leaving stale placeholder text/icon visible for a frame after the first Open().
            Show(null);
        }

        void OnDestroy()
        {
            GameEvents.OnInventoryChanged -= OnInventoryChanged;
        }

        public void Show(CraftRecipeDefinition recipe)
        {
            _recipe = recipe;

            if (recipe == null)
            {
                itemInfoIcon.sprite      = null;
                itemInfoIcon.enabled     = false;
                itemInfoName.text        = string.Empty;
                itemInfoDescription.text = string.Empty;

                foreach (var row in _rows)
                    Destroy(row.gameObject);
                _rows.Clear();

                craftButton.interactable = false;
                return;
            }

            itemInfoIcon.enabled = true;
            RebuildIngredientRows();
            Refresh();
        }

        public void Refresh()
        {
            if (_recipe == null) return;

            var resultItem = _recipe.ResultItem;
            itemInfoIcon.sprite      = resultItem?.Icon;
            itemInfoIcon.color       = resultItem?.Icon != null ? Color.white : new Color(0.4f, 0.4f, 0.4f);
            itemInfoName.text        = resultItem?.DisplayName ?? _recipe.RecipeId;
            itemInfoDescription.text = resultItem?.Description ?? string.Empty;

            for (int i = 0; i < _rows.Count; i++)
                _rows[i].Populate(_recipe.RequiredIngredients[i]);

            craftButton.interactable = CraftingSystem.Instance != null && CraftingSystem.Instance.CanCraft(_recipe);
        }

        private void RebuildIngredientRows()
        {
            foreach (var row in _rows)
                Destroy(row.gameObject);
            _rows.Clear();

            if (_recipe?.RequiredIngredients == null) return;

            foreach (var ingredient in _recipe.RequiredIngredients)
            {
                var row = Instantiate(ingredientRowPrefab, ingredientsContainer);
                row.Populate(ingredient);
                _rows.Add(row);
            }
        }

        private void OnInventoryChanged()
        {
            if (!gameObject.activeInHierarchy) return;
            Refresh();
        }

        private void OnCraftClicked()
        {
            if (_recipe == null) return;
            if (!CraftingSystem.Instance.Craft(_recipe))
                Debug.LogWarning("[CraftingDetailPanel] Inventory full!");
        }
    }
}
