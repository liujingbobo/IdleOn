using UnityEngine;
using UnityEngine.UI;
using IdleOn.Items;

namespace IdleOn.UI
{
    public enum DragSource { Inventory, EquipmentSlot, SkillPanel }

    public class DragHandler : MonoBehaviour
    {
        [SerializeField] private Image dragIcon;

        private Canvas _canvas;

        public string        CurrentItemId { get; private set; }
        public DragSource    Source        { get; private set; }
        public EquipmentSlot FromSlot      { get; private set; }
        public bool          IsDragging    => CurrentItemId != null;

        void Start()
        {
            _canvas = GetComponentInParent<Canvas>();
            dragIcon.gameObject.SetActive(false);
        }

        public void BeginDrag(string itemId, Sprite icon)
        {
            Source           = DragSource.Inventory;
            FromSlot         = default;
            CurrentItemId    = itemId;
            dragIcon.sprite  = icon;
            dragIcon.enabled = icon != null;
            dragIcon.gameObject.SetActive(true);
        }

        public void BeginDrag(string itemId, Sprite icon, EquipmentSlot fromSlot)
        {
            Source           = DragSource.EquipmentSlot;
            FromSlot         = fromSlot;
            CurrentItemId    = itemId;
            dragIcon.sprite  = icon;
            dragIcon.enabled = icon != null;
            dragIcon.gameObject.SetActive(true);
        }

        public void BeginDrag(string id, Sprite icon, DragSource source)
        {
            Source           = source;
            FromSlot         = default;
            CurrentItemId    = id;
            dragIcon.sprite  = icon;
            dragIcon.enabled = true;
            dragIcon.gameObject.SetActive(true);
        }

        public void UpdatePosition(Vector2 screenPos)
        {
            if (_canvas == null) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                (RectTransform)_canvas.transform, screenPos,
                _canvas.worldCamera, out Vector2 local);
            ((RectTransform)dragIcon.transform).anchoredPosition = local;
        }

        public void EndDrag()
        {
            CurrentItemId = null;
            dragIcon.gameObject.SetActive(false);
        }

        public void Cancel() => EndDrag();
    }
}
