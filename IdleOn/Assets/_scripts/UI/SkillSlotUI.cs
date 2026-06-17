using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using IdleOn.Combat;
using IdleOn.Core;
using IdleOn.Save;
using IdleOn.Skills;

namespace IdleOn.UI
{
    public class SkillSlotUI : MonoBehaviour, IDropHandler, IPointerClickHandler
    {
        [SerializeField] private Image    iconImage;
        [SerializeField] private Image    emptyOverlay;
        [SerializeField] private TMP_Text slotLabel;

        private DragHandler _dragHandler;
        private PlayerCombatController _combatController;
        private int         _slotIndex;
        private string      _assignedSkillId;

        public void Initialize(int slotIndex, DragHandler dragHandler, PlayerCombatController combatController)
        {
            _slotIndex        = slotIndex;
            _dragHandler      = dragHandler;
            _combatController = combatController;

            if (slotLabel != null)
                slotLabel.text = (slotIndex + 1).ToString();

            var save = SaveManager.Instance?.CurrentSave;
            if (save != null && slotIndex < save.HotbarSkillIds.Count)
                _assignedSkillId = save.HotbarSkillIds[slotIndex];

            Refresh();
        }

        public void OnDrop(PointerEventData eventData)
        {
            if (_dragHandler == null || !_dragHandler.IsDragging) return;
            if (_dragHandler.Source != DragSource.SkillPanel) return;

            AssignSkill(_dragHandler.CurrentItemId);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (string.IsNullOrEmpty(_assignedSkillId)) return;
            if (_combatController == null)
            {
                Debug.LogWarning($"[SkillSlotUI] Slot {_slotIndex + 1}: no PlayerCombatController assigned.");
                return;
            }

            _combatController.TryCastSkill(_assignedSkillId);
        }

        private void AssignSkill(string skillId)
        {
            _assignedSkillId = skillId;

            var save = SaveManager.Instance?.CurrentSave;
            if (save != null && _slotIndex < save.HotbarSkillIds.Count)
                save.HotbarSkillIds[_slotIndex] = skillId;

            Refresh();
        }

        private void Refresh()
        {
            var def = GameDatabase.Instance?.Skills?.GetSkill(_assignedSkillId);
            bool hasSkill = def != null;

            if (iconImage != null)
            {
                iconImage.sprite  = hasSkill ? def.Icon : null;
                iconImage.enabled = hasSkill;
            }

            if (emptyOverlay != null)
                emptyOverlay.gameObject.SetActive(!hasSkill);
        }
    }
}
