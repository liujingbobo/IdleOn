using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using IdleOn.Core;
using IdleOn.Talents;
using IdleOn.Skills;

namespace IdleOn.UI
{
    public class TalentSlotUI : MonoBehaviour,
        IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private Image    iconImage;
        [SerializeField] private TMP_Text levelText;

        private TalentDefinition         _def;
        private Action<TalentDefinition> _onClicked;
        private SkillDefinition          _linkedSkill;
        private DragHandler              _dragHandler;
        private bool                     _isAssignMode;
        private CanvasGroup              _canvasGroup;

        public void Initialize(TalentDefinition def,
                               Action<TalentDefinition> onClicked,
                               DragHandler dragHandler)
        {
            _def         = def;
            _onClicked   = onClicked;
            _dragHandler = dragHandler;
            _linkedSkill = FindLinkedSkill(def);

            var existingCg = GetComponent<CanvasGroup>();
            _canvasGroup = existingCg ? existingCg : gameObject.AddComponent<CanvasGroup>();

            if (iconImage != null)
            {
                iconImage.sprite = def.Icon;
                iconImage.color  = def.Icon != null ? Color.white : new Color(0.4f, 0.4f, 0.4f, 1f);
            }

            Refresh();
        }

        public void SetAssignMode(bool on)
        {
            _isAssignMode = on;
            Refresh();
        }

        public void Refresh()
        {
            if (_def == null) return;

            int level = TalentSystem.Instance?.GetLevel(_def.TalentId) ?? 0;
            if (levelText != null)
                levelText.text = $"{level}/{_def.MaxLevel}";

            ApplyVisuals(level);
        }

        private void ApplyVisuals(int level)
        {
            if (_canvasGroup == null) return;

            if (!_isAssignMode)
            {
                _canvasGroup.alpha          = 1f;
                _canvasGroup.blocksRaycasts = true;
                return;
            }

            bool isSkillTalent = _linkedSkill != null;
            bool isUnlocked    = isSkillTalent && level >= _linkedSkill.RequiredTalentLevel;

            if (!isSkillTalent)
            {
                _canvasGroup.alpha          = 0.35f;
                _canvasGroup.blocksRaycasts = false;
            }
            else if (!isUnlocked)
            {
                _canvasGroup.alpha          = 0.5f;
                _canvasGroup.blocksRaycasts = true;
            }
            else
            {
                _canvasGroup.alpha          = 1f;
                _canvasGroup.blocksRaycasts = true;
            }
        }

        // ── Click (normal mode: select talent; assign mode: allowed for skill slots) ──

        public void OnPointerClick(PointerEventData eventData)
        {
            _onClicked?.Invoke(_def);
        }

        // ── Drag (assign mode, unlocked skill talents only) ───────────────────

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!_isAssignMode || _linkedSkill == null || _dragHandler == null)
            {
                eventData.pointerDrag = null;
                return;
            }

            int level = TalentSystem.Instance?.GetLevel(_def.TalentId) ?? 0;
            if (level < _linkedSkill.RequiredTalentLevel)
            {
                eventData.pointerDrag = null;
                return;
            }

            _dragHandler.BeginDrag(_linkedSkill.SkillId, _linkedSkill.Icon, DragSource.SkillPanel);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_dragHandler != null && _dragHandler.IsDragging)
                _dragHandler.UpdatePosition(eventData.position);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (_dragHandler != null && _dragHandler.IsDragging)
                _dragHandler.EndDrag();
        }

        private SkillDefinition FindLinkedSkill(TalentDefinition def)
        {
            var db = GameDatabase.Instance?.Skills;
            if (db == null) return null;
            foreach (var skill in db.Skills)
                if (skill != null && skill.RequiredTalentId == def.TalentId)
                    return skill;
            return null;
        }
    }
}
