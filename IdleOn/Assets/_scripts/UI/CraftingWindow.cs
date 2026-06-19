using System.Collections.Generic;
using UnityEngine;
using IdleOn.Crafting;
using IdleOn.Core;

namespace IdleOn.UI
{
    public class CraftingWindow : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject        windowPanel;
        [SerializeField] private CraftingDetailPanel detailPanel;
        [SerializeField] private Transform         recipeListContent;
        [SerializeField] private CraftingRecipeSlotUI recipeSlotPrefab;

        // TEMPORARY: debug key for opening this window before MainUI is built.
        // Remove this block once MainUI buttons call Open() / Toggle() directly.
        [Header("Debug (remove once MainUI is wired)")]
        [SerializeField] private bool    enableDebugKey = true;
        [SerializeField] private KeyCode debugOpenKey   = KeyCode.C;

        private readonly List<CraftingRecipeSlotUI> _slots = new List<CraftingRecipeSlotUI>();
        private CraftRecipeDefinition _selectedRecipe;

        public bool IsOpen => windowPanel.activeSelf;

        void Awake()
        {
            windowPanel.SetActive(false);
            GameEvents.OnInventoryChanged += OnInventoryChanged;
        }

        void OnDestroy()
        {
            GameEvents.OnInventoryChanged -= OnInventoryChanged;
        }

        void Start()
        {
            PopulateRecipeList();
        }

        void Update()
        {
            if (enableDebugKey && Input.GetKeyDown(debugOpenKey))
                Toggle();
        }

        // Called by MainUI buttons.
        public void Open()
        {
            windowPanel.SetActive(true);
            RefreshAllSlots();
            if (_selectedRecipe != null)
                detailPanel.Refresh();
        }

        // Called by MainUI buttons or the window's own close button.
        public void Close()
        {
            windowPanel.SetActive(false);
        }

        // Called by MainUI buttons.
        public void Toggle()
        {
            if (windowPanel.activeSelf) Close();
            else Open();
        }

        private void PopulateRecipeList()
        {
            var db = GameDatabase.Instance?.Crafting;
            if (db == null) return;

            // Destroy() is deferred, so track our own instances instead of trusting transform.childCount mid-frame.
            foreach (var slot in _slots)
                Destroy(slot.gameObject);
            _slots.Clear();

            var recipes = new List<CraftRecipeDefinition>(db.Recipes);
            recipes.Sort((a, b) =>
            {
                bool canCraftA = CraftingSystem.Instance != null && CraftingSystem.Instance.CanCraft(a);
                bool canCraftB = CraftingSystem.Instance != null && CraftingSystem.Instance.CanCraft(b);
                return canCraftB.CompareTo(canCraftA);
            });

            foreach (var recipe in recipes)
            {
                var slot = Instantiate(recipeSlotPrefab, recipeListContent);
                slot.Initialize(recipe, OnRecipeSelected);
                _slots.Add(slot);
            }
        }

        private void OnRecipeSelected(CraftRecipeDefinition recipe)
        {
            _selectedRecipe = recipe;
            detailPanel.Show(recipe);

            foreach (var slot in _slots)
                slot.SetSelected(slot.Recipe == recipe);
        }

        private void OnInventoryChanged()
        {
            if (!windowPanel.activeSelf) return;
            RefreshAllSlots();
        }

        private void RefreshAllSlots()
        {
            // Crafting/inventory changes can flip craftable state, so re-sort craftable-first and reselect.
            PopulateRecipeList();
            foreach (var slot in _slots)
                slot.SetSelected(slot.Recipe == _selectedRecipe);
        }
    }
}
