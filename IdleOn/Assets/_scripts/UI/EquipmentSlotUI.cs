using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using IdleOn.Items;
using IdleOn.Equipment;
using IdleOn.Core;

namespace IdleOn.UI
{
    public class EquipmentSlotUI : MonoBehaviour, IPointerClickHandler, IDropHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] public EquipmentSlot targetSlot;
        [SerializeField] private Image        iconImage;

        private ItemInfoPanel _itemInfoPanel;
        private DragHandler   _dragHandler;

        public void Initialize(DragHandler dragHandler, ItemInfoPanel itemInfoPanel)
        {
            _dragHandler   = dragHandler;
            _itemInfoPanel = itemInfoPanel;
        }

        public void Refresh()
        {
            string itemId = EquipmentSystem.Instance?.GetEquipped(targetSlot);
            var    def    = itemId != null ? GameDatabase.Instance?.Items?.GetItem(itemId) : null;

            if (def?.Icon != null)
            {
                iconImage.sprite  = def.Icon;
                iconImage.enabled = true;
            }
            else
            {
                iconImage.enabled = false;
            }
        }

        // ── Click → ItemInfo ─────────────────────────────────────────────────

        public void OnPointerClick(PointerEventData eventData)
        {
            string itemId = EquipmentSystem.Instance?.GetEquipped(targetSlot);
            var    def    = itemId != null ? GameDatabase.Instance?.Items?.GetItem(itemId) : null;

            if (def != null)
                _itemInfoPanel?.Show(def, 0, (RectTransform)transform);
            else
                _itemInfoPanel?.Hide();
        }

        // ── Drop → Equip (inventory source only) ─────────────────────────────

        public void OnDrop(PointerEventData eventData)
        {
            string itemId = _dragHandler?.CurrentItemId;
            if (itemId == null) return;
            if (_dragHandler.Source != DragSource.Inventory) return;

            var def = GameDatabase.Instance?.Items?.GetItem(itemId);
            if (def == null || def.ItemType != ItemType.Equipment) return;
            if (def.EquipmentSlot != targetSlot) return;

            EquipmentSystem.Instance?.Equip(itemId);
        }

        // ── Drag → Unequip ───────────────────────────────────────────────────

        public void OnBeginDrag(PointerEventData eventData)
        {
            string itemId = EquipmentSystem.Instance?.GetEquipped(targetSlot);
            if (itemId == null) { eventData.pointerDrag = null; return; }

            var def = GameDatabase.Instance?.Items?.GetItem(itemId);
            _itemInfoPanel?.Hide();
            _dragHandler?.BeginDrag(itemId, def?.Icon, targetSlot);
        }

        public void OnDrag(PointerEventData eventData)
        {
            _dragHandler?.UpdatePosition(eventData.position);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            _dragHandler?.EndDrag();
        }
    }
}
