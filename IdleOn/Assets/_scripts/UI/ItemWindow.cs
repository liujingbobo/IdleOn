using UnityEngine;
using UnityEngine.EventSystems;
using IdleOn.Core;
using IdleOn.Inventory;

namespace IdleOn.UI
{
    public class ItemWindow : MonoBehaviour
    {
        [Header("Window")]
        [SerializeField] private GameObject windowPanel;

        [Header("Equipment")]
        [SerializeField] private EquipmentSlotUI[] equipmentSlots;

        [Header("Inventory")]
        [SerializeField] private Transform inventoryPanel;

        [Header("Pagination")]
        [SerializeField] private GameObject nextPageButton;
        [SerializeField] private GameObject prevPageButton;
        private const int SlotsPerPage = 20;

        [Header("Shared")]
        [SerializeField] private ItemInfoPanel itemInfoPanel;
        [SerializeField] private DragHandler   dragHandler;

        private InventorySlotUI[] _inventorySlots;
        private int _currentPage;

        public bool IsOpen => windowPanel.activeSelf;

        void Awake()
        {
            windowPanel.SetActive(false);
            GameEvents.OnInventoryChanged += RefreshInventory;
            GameEvents.OnEquipmentChanged += RefreshEquipment;
        }

        void Start()
        {
            _inventorySlots = inventoryPanel.GetComponentsInChildren<InventorySlotUI>(true);

            foreach (var slot in _inventorySlots)
                slot.Initialize(dragHandler, itemInfoPanel);

            foreach (var slot in equipmentSlots)
                slot.Initialize(dragHandler, itemInfoPanel);
        }

        void OnDestroy()
        {
            GameEvents.OnInventoryChanged -= RefreshInventory;
            GameEvents.OnEquipmentChanged -= RefreshEquipment;
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Tab))
                ToggleWindow();

            if (windowPanel.activeSelf && Input.GetMouseButtonDown(0))
                if (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject())
                    itemInfoPanel.Hide();
        }

        // Called by MainHUD buttons.
        public void Open()   { windowPanel.SetActive(true);  _currentPage = 0; RefreshInventory(); RefreshEquipment(); }
        public void Close()  { windowPanel.SetActive(false); itemInfoPanel.Hide(); dragHandler.Cancel(); }
        public void Toggle() { if (windowPanel.activeSelf) Close(); else Open(); }

        // Called by NextPageBtn/PrevPageBtn onClick.
        public void NextPage() { _currentPage++; RefreshInventory(); }
        public void PrevPage() { _currentPage--; RefreshInventory(); }

        private void ToggleWindow()
        {
            bool open = !windowPanel.activeSelf;
            windowPanel.SetActive(open);

            if (open)
            {
                _currentPage = 0;
                RefreshInventory();
                RefreshEquipment();
            }
            else
            {
                itemInfoPanel.Hide();
                dragHandler.Cancel();
            }
        }

        private void RefreshInventory()
        {
            if (!windowPanel.activeSelf || _inventorySlots == null) return;

            var inv = InventorySystem.Instance;
            if (inv == null) return;

            var items    = GameDatabase.Instance?.Items;
            var allSlots = inv.GetSlots();
            int capacity = inv.GetCapacity();

            int totalPages = Mathf.Max(1, Mathf.CeilToInt(capacity / (float)SlotsPerPage));
            _currentPage = Mathf.Clamp(_currentPage, 0, totalPages - 1);

            int pageOffset = _currentPage * SlotsPerPage;

            for (int i = 0; i < _inventorySlots.Length; i++)
            {
                int globalIndex = pageOffset + i;

                if (globalIndex < capacity && globalIndex < allSlots.Count && !allSlots[globalIndex].IsEmpty)
                {
                    var slotData = allSlots[globalIndex];
                    _inventorySlots[i].Populate(slotData, items?.GetItem(slotData.ItemId)?.Icon);
                }
                else
                {
                    _inventorySlots[i].SetEmpty();
                }
            }

            if (prevPageButton != null) prevPageButton.SetActive(_currentPage > 0);
            if (nextPageButton != null) nextPageButton.SetActive(_currentPage < totalPages - 1);
        }

        private void RefreshEquipment()
        {
            if (!windowPanel.activeSelf) return;
            foreach (var slot in equipmentSlots)
                slot.Refresh();
        }
    }
}
