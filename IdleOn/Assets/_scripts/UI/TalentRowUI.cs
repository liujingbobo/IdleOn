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
    public class TalentRowUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private TMP_Text effectText;
        [SerializeField] private Button   upgradeButton;
        [SerializeField] private Image    rowBackground;

        [Header("Colors")]
        [SerializeField] private Color defaultColor = new Color(0.12f, 0.12f, 0.14f, 1f);
        [SerializeField] private Color maxedColor   = new Color(0.12f, 0.20f, 0.35f, 1f);

        private TalentDefinition         _def;
        private Action<TalentDefinition> _onClicked;
        private SkillDefinition          _linkedSkill;
        private DragHandler              _dragHandler;
        private bool                     _isAssignMode;
        private CanvasGroup              _canvasGroup;

        public void Initialize(TalentDefinition def,
                               Action<TalentDefinition> onClicked = null,
                               DragHandler dragHandler = null)
        {
            _def         = def;
            _onClicked   = onClicked;
            _dragHandler = dragHandler;
            _linkedSkill = FindLinkedSkill(def);
            var existingCg = gameObject.GetComponent<CanvasGroup>();
            _canvasGroup = existingCg ? existingCg : gameObject.AddComponent<CanvasGroup>();

            if (nameText != null) nameText.text = def.DisplayName;
            upgradeButton.onClick.AddListener(OnUpgradeClicked);

            if (rowBackground != null)
            {
                var trigger = rowBackground.gameObject.GetComponent<EventTrigger>()
                           ?? rowBackground.gameObject.AddComponent<EventTrigger>();
                var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
                entry.callback.AddListener((_) =>
                {
                    if (!_isAssignMode) _onClicked?.Invoke(_def);
                });
                trigger.triggers.Add(entry);
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
            if (_def == null || TalentSystem.Instance == null) return;

            int  level      = TalentSystem.Instance.GetLevel(_def.TalentId);
            bool maxed      = level >= _def.MaxLevel;
            bool canUpgrade = TalentSystem.Instance.CanUpgrade(_def);

            levelText.text  = $"Lv.{level}/{_def.MaxLevel}";
            effectText.text = _def.GetEffectText(level);
            upgradeButton.interactable = canUpgrade;

            if (rowBackground != null)
                rowBackground.color = maxed ? maxedColor : defaultColor;

            ApplyAssignModeVisuals(level);
        }

        private void ApplyAssignModeVisuals(int level)
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

        // ── Drag (assign mode only) ───────────────────────────────────────────

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

        private void OnUpgradeClicked() => TalentSystem.Instance?.Upgrade(_def);

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
