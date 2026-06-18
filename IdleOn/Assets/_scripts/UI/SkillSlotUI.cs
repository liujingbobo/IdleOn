using System.Collections;
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
    public class SkillSlotUI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        private const float TooltipHoverDelay = 1f;

        [SerializeField] private Image    iconImage;
        [SerializeField] private Image    iconFrontImage;
        [SerializeField] private Image    emptyOverlay;
        [SerializeField] private TMP_Text slotLabel;

        private PlayerCombatController _combatController;
        private SkillDefinition        _assignedSkill;
        private int                    _slotIndex;
        private string                 _assignedSkillId;
        private Coroutine              _hoverCoroutine;

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

        void Update()
        {
            if (_assignedSkill == null || iconFrontImage == null || _combatController == null) return;
            iconFrontImage.fillAmount = _combatController.GetSkillCooldownProgress01(_assignedSkillId);
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

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_assignedSkill == null) return;
            if (_hoverCoroutine != null) StopCoroutine(_hoverCoroutine);
            _hoverCoroutine = StartCoroutine(HoverAndShowTooltip());
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_hoverCoroutine != null)
            {
                StopCoroutine(_hoverCoroutine);
                _hoverCoroutine = null;
            }
            SkillTooltipUI.Instance?.Hide();
        }

        private IEnumerator HoverAndShowTooltip()
        {
            yield return new WaitForSeconds(TooltipHoverDelay);
            SkillTooltipUI.Instance?.Show(_assignedSkill);
            _hoverCoroutine = null;
        }

        private void Refresh()
        {
            _assignedSkill = GameDatabase.Instance?.Skills?.GetSkill(_assignedSkillId);
            bool hasIcon = _assignedSkill != null && _assignedSkill.Icon != null;

            if (iconImage != null)
            {
                iconImage.sprite  = hasIcon ? _assignedSkill.Icon : null;
                iconImage.enabled = hasIcon;
            }

            if (iconFrontImage != null)
            {
                iconFrontImage.sprite     = hasIcon ? _assignedSkill.Icon : null;
                iconFrontImage.enabled    = hasIcon;
                iconFrontImage.fillAmount = 1f;
            }

            if (emptyOverlay != null)
                emptyOverlay.gameObject.SetActive(_assignedSkill == null);
        }
    }
}
