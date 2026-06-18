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
    public class SkillSlotUI : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private Image    iconImage;
        [SerializeField] private Image    emptyOverlay;
        [SerializeField] private TMP_Text slotLabel;

        private PlayerCombatController _combatController;
        private int         _slotIndex;
        private string      _assignedSkillId;

        public void Initialize(int slotIndex, PlayerCombatController combatController)
        {
            _slotIndex        = slotIndex;
            _combatController = combatController;

            if (slotLabel != null)
                slotLabel.text = (slotIndex + 1).ToString();

            SyncFromSave();
        }

        // Re-reads this slot's assigned skill from save data (e.g. after auto-equip).
        public void SyncFromSave()
        {
            var save = SaveManager.Instance?.CurrentSave;
            if (save != null && _slotIndex < save.HotbarSkillIds.Count)
                _assignedSkillId = save.HotbarSkillIds[_slotIndex];

            Refresh();
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
