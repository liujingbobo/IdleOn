using UnityEngine;
using UnityEngine.EventSystems;
using IdleOn.Skills;

namespace IdleOn.UI
{
    public class SkillDragSource : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private DragHandler dragHandler;

        public SkillDefinition Skill      { get; set; }
        public bool            IsUnlocked { get; set; }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!IsUnlocked || Skill == null)
            {
                eventData.pointerDrag = null;
                return;
            }
            dragHandler.BeginDrag(Skill.SkillId, Skill.Icon, DragSource.SkillPanel);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (dragHandler.IsDragging)
                dragHandler.UpdatePosition(eventData.position);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (dragHandler.IsDragging)
                dragHandler.EndDrag();
        }
    }
}
