using System;
using IdleOn.Vault;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace IdleOn.UI
{
    public class VaultSlotUI : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private Image      iconImage;
        [SerializeField] private TMP_Text   levelText;
        [SerializeField] private GameObject selectedIndicator;

        private VaultUpgradeDefinition         _def;
        private Action<VaultUpgradeDefinition> _onClicked;

        public void Initialize(VaultUpgradeDefinition def, Action<VaultUpgradeDefinition> onClicked)
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

            int level = VaultSystem.Instance?.GetLevel(_def) ?? 0;
            if (levelText != null)
                levelText.text = $"{level}/{_def.MaxLevel}";
        }

        public void SetSelected(bool selected) => selectedIndicator?.SetActive(selected);

        public void OnPointerClick(PointerEventData eventData) => _onClicked?.Invoke(_def);
    }
}
