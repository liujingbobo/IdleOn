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

        [Header("Shared")]
        [SerializeField] private ItemInfoPanel itemInfoPanel;
        [SerializeField] private DragHandler   dragHandler;

        private InventorySlotUI[] _inventorySlots;

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

        private void ToggleWindow()
        {
            bool open = !windowPanel.activeSelf;
            windowPanel.SetActive(open);

            if (open)
            {
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
            var occupied = inv.GetSlots();
            int total    = Mathf.Min(_inventorySlots.Length, inv.GetCapacity());

            for (int i = 0; i < total; i++)
            {
                if (i < occupied.Count)
                {
                    var slotData = occupied[i];
                    _inventorySlots[i].Populate(slotData, items?.GetItem(slotData.ItemId)?.Icon);
                }
                else
                {
                    _inventorySlots[i].SetEmpty();
                }
            }

            for (int i = total; i < _inventorySlots.Length; i++)
                _inventorySlots[i].SetEmpty();
        }

        private void RefreshEquipment()
        {
            if (!windowPanel.activeSelf) return;
            foreach (var slot in equipmentSlots)
                slot.Refresh();
        }
    }
}
