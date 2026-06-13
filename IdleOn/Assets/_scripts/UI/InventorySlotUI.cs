using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using IdleOn.Items;
using IdleOn.Core;
using IdleOn.Equipment;

namespace IdleOn.UI
{
    public class InventorySlotUI : MonoBehaviour,
        IPointerClickHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
    {
        [SerializeField] private Image    itemIcon;
        [SerializeField] private TMP_Text stackCount;

        private ItemInfoPanel     _itemInfoPanel;
        private DragHandler       _dragHandler;
        private InventorySlotData _slotData;
        private bool              _isEmpty = true;

        private static Sprite _placeholder;
        private static Sprite Placeholder
        {
            get
            {
                if (_placeholder != null) return _placeholder;
                var tex = new Texture2D(2, 2);
                var c   = new Color(0.35f, 0.35f, 0.35f, 1f);
                tex.SetPixels(new[] { c, c, c, c });
                tex.Apply();
                _placeholder = Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f));
                return _placeholder;
            }
        }

        public void Initialize(DragHandler dragHandler, ItemInfoPanel itemInfoPanel)
        {
            _dragHandler   = dragHandler;
            _itemInfoPanel = itemInfoPanel;
        }

        // ── Display ──────────────────────────────────────────────────────────

        public void Populate(InventorySlotData slot, Sprite icon)
        {
            _slotData        = slot;
            _isEmpty         = false;
            itemIcon.sprite  = icon != null ? icon : Placeholder;
            itemIcon.enabled = true;

            if (slot.Quantity > 1)
            {
                stackCount.text    = "x" + slot.Quantity;
                stackCount.enabled = true;
            }
            else
            {
                stackCount.enabled = false;
            }
        }

        public void SetEmpty()
        {
            _slotData          = null;
            _isEmpty           = true;
            itemIcon.enabled   = false;
            stackCount.enabled = false;
        }

        // ── Click → ItemInfo ─────────────────────────────────────────────────

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_isEmpty || _slotData == null)
            {
                _itemInfoPanel?.Hide();
                return;
            }

            var def = GameDatabase.Instance?.Items?.GetItem(_slotData.ItemId);
            if (def != null)
                _itemInfoPanel?.Show(def, _slotData.Quantity, (RectTransform)transform);
            else
                _itemInfoPanel?.Hide();
        }

        // ── Drag ─────────────────────────────────────────────────────────────

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_isEmpty || _slotData == null)
            {
                eventData.pointerDrag = null;
                return;
            }

            var def = GameDatabase.Instance?.Items?.GetItem(_slotData.ItemId);
            if (def == null || def.ItemType != ItemType.Equipment)
            {
                eventData.pointerDrag = null;
                return;
            }

            _itemInfoPanel?.Hide();
            _dragHandler?.BeginDrag(_slotData.ItemId, def.Icon);
        }

        public void OnDrag(PointerEventData eventData)
        {
            _dragHandler?.UpdatePosition(eventData.position);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            _dragHandler?.EndDrag();
        }

        // ── Drop → Unequip ───────────────────────────────────────────────────

        public void OnDrop(PointerEventData eventData)
        {
            if (_dragHandler == null || !_dragHandler.IsDragging) return;
            if (_dragHandler.Source != DragSource.EquipmentSlot) return;

            EquipmentSystem.Instance?.Unequip(_dragHandler.FromSlot);
        }
    }
}
