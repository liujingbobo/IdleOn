using UnityEngine;
using IdleOn.Core;
using IdleOn.Inventory;
using IdleOn.Items;

namespace IdleOn.UI
{
    public class InventoryUI : MonoBehaviour
    {
        [SerializeField] private GameObject   panel;
        [SerializeField] private ItemDatabase itemDatabase;

        private InventorySlotUI[] _slots;

        void Awake()
        {
            _slots = panel.GetComponentsInChildren<InventorySlotUI>(true);
            panel.SetActive(false);
            GameEvents.OnInventoryChanged += Refresh;
        }

        void OnDestroy()
        {
            GameEvents.OnInventoryChanged -= Refresh;
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Tab))
                TogglePanel();
        }

        private void TogglePanel()
        {
            bool open = !panel.activeSelf;
            panel.SetActive(open);
            if (open) Refresh();
        }

        private void Refresh()
        {
            if (!panel.activeSelf) return;

            var inv      = InventorySystem.Instance;
            if (inv == null) return;

            var occupied = inv.GetSlots();
            int capacity = inv.GetCapacity();
            int total    = Mathf.Min(_slots.Length, capacity);

            for (int i = 0; i < total; i++)
            {
                if (i < occupied.Count)
                {
                    var slotData = occupied[i];
                    var def      = itemDatabase != null ? itemDatabase.GetItem(slotData.ItemId) : null;
                    _slots[i].Populate(slotData, def?.Icon);
                }
                else
                {
                    _slots[i].SetEmpty();
                }
            }

            for (int i = total; i < _slots.Length; i++)
                _slots[i].SetEmpty();
        }
    }
}
