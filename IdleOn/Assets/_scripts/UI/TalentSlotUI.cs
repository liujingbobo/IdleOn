using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using IdleOn.Talents;

namespace IdleOn.UI
{
    public class TalentSlotUI : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private Image      iconImage;
        [SerializeField] private TMP_Text   levelText;
        [SerializeField] private GameObject selectedIndicator;

        private TalentDefinition         _def;
        private Action<TalentDefinition> _onClicked;

        public void Initialize(TalentDefinition def, Action<TalentDefinition> onClicked)
        {
            _def       = def;
            _onClicked = onClicked;

            if (iconImage != null)
            {
                iconImage.sprite = def.Icon;
                iconImage.color  = def.Icon != null ? Color.white : new Color(0.4f, 0.4f, 0.4f, 1f);
            }

            SetSelected(false);
            Refresh();
        }

        public void Refresh()
        {
            if (_def == null) return;

            int level = TalentSystem.Instance?.GetLevel(_def.TalentId) ?? 0;
            if (levelText != null)
                levelText.text = $"{level}/{_def.MaxLevel}";
        }

        public void SetSelected(bool selected) => selectedIndicator?.SetActive(selected);

        public void OnPointerClick(PointerEventData eventData) => _onClicked?.Invoke(_def);
    }
}
