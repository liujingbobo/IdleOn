using System.Collections;
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
        [Header("States")]
        [SerializeField] private GameObject emptyState;
        [SerializeField] private GameObject detailState;

        [Header("Result")]
        [SerializeField] private Image    resultIcon;
        [SerializeField] private TMP_Text resultName;

        [Header("Ingredients")]
        [SerializeField] private Transform       ingredientsContainer;
        [SerializeField] private IngredientRowUI ingredientRowPrefab;

        [Header("Craft")]
        [SerializeField] private Button   craftButton;
        [SerializeField] private TMP_Text statusLabel;

        private CraftRecipeDefinition    _recipe;
        private readonly List<IngredientRowUI> _rows = new List<IngredientRowUI>();
        private Coroutine _statusRoutine;

        void Awake()
        {
            emptyState.SetActive(true);
            detailState.SetActive(false);
            statusLabel.gameObject.SetActive(false);
            craftButton.onClick.AddListener(OnCraftClicked);
            GameEvents.OnInventoryChanged += OnInventoryChanged;
        }

        void OnDestroy()
        {
            GameEvents.OnInventoryChanged -= OnInventoryChanged;
        }

        public void Show(CraftRecipeDefinition recipe)
        {
            _recipe = recipe;
            emptyState.SetActive(recipe == null);
            detailState.SetActive(recipe != null);

            if (recipe == null) return;

            RebuildIngredientRows();
            Refresh();
        }

        public void Refresh()
        {
            if (_recipe == null) return;

            resultIcon.sprite = _recipe.ResultItem?.Icon;
            resultIcon.color  = _recipe.ResultItem?.Icon != null ? Color.white : new Color(0.4f, 0.4f, 0.4f);
            resultName.text   = _recipe.ResultItem?.DisplayName ?? _recipe.RecipeId;

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
                ShowStatus("Inventory full!");
        }

        private void ShowStatus(string message)
        {
            statusLabel.text = message;
            statusLabel.gameObject.SetActive(true);
            if (_statusRoutine != null) StopCoroutine(_statusRoutine);
            _statusRoutine = StartCoroutine(HideStatusAfterDelay(2f));
        }

        private IEnumerator HideStatusAfterDelay(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            statusLabel.gameObject.SetActive(false);
            _statusRoutine = null;
        }
    }
}
