using IdleOn.Core;
using IdleOn.Items;
using IdleOn.Vault;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IdleOn.UI
{
    public class VaultInfoPanel : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private TMP_Text   nameText;
        [SerializeField] private TMP_Text   descriptionText;
        [SerializeField] private Image      iconImage;
        [SerializeField] private Image      levelFiller;
        [SerializeField] private TMP_Text   levelText;
        [SerializeField] private TMP_Text   currentEffectText;
        [SerializeField] private GameObject currentSeparatorRoot;
        [SerializeField] private TMP_Text   nextEffectText;
        [SerializeField] private GameObject nextSeparatorRoot;
        [SerializeField] private Button     upgradeButton;
        [SerializeField] private TMP_Text   upgradeButtonLabel;

        private VaultUpgradeDefinition _def;

        void Awake()
        {
            panel.SetActive(false);
            GameEvents.OnVaultChanged    += Refresh;
            GameEvents.OnCurrencyChanged += OnCurrencyChanged;
            upgradeButton?.onClick.AddListener(OnUpgradeClicked);
        }

        void OnDestroy()
        {
            GameEvents.OnVaultChanged    -= Refresh;
            GameEvents.OnCurrencyChanged -= OnCurrencyChanged;
            upgradeButton?.onClick.RemoveListener(OnUpgradeClicked);
        }

        public void Show(VaultUpgradeDefinition def)
        {
            _def = def;
            panel.SetActive(true);
            Refresh();
        }

        public void Hide()
        {
            panel.SetActive(false);
            _def = null;
            if (upgradeButton != null) upgradeButton.interactable = false;
        }

        private void OnCurrencyChanged(CurrencyType type, long newTotal)
        {
            if (type == CurrencyType.Gold) Refresh();
        }

        public void Refresh()
        {
            if (_def == null || !panel.activeSelf) return;

            int  level = VaultSystem.Instance?.GetLevel(_def) ?? 0;
            bool maxed = level >= _def.MaxLevel;

            nameText.text        = _def.DisplayName;
            descriptionText.text = _def.Description; // VaultUpgradeDefinition has no Description field

            if (iconImage != null)
            {
                iconImage.sprite  = _def.Icon;
                iconImage.enabled = _def.Icon != null;
            }

            if (levelFiller != null)
                levelFiller.fillAmount = _def.MaxLevel > 0 ? (float)level / _def.MaxLevel : 0f;
            if (levelText != null)
                levelText.text = maxed ? "Max" : $"Lv.{level}/{_def.MaxLevel}";

            bool showCurrent = level > 0;
            bool showNext    = !maxed;

            currentSeparatorRoot?.SetActive(showCurrent);
            if (currentEffectText != null)
            {
                currentEffectText.gameObject.SetActive(showCurrent);
                if (showCurrent) currentEffectText.text = _def.GetCurrentEffectText(level);
            }

            nextSeparatorRoot?.SetActive(showNext);
            if (nextEffectText != null)
            {
                nextEffectText.gameObject.SetActive(showNext);
                if (showNext) nextEffectText.text = _def.GetCurrentEffectText(level + 1);
            }

            bool canUpgrade = !maxed && (VaultSystem.Instance?.CanUpgrade(_def) ?? false);
            if (upgradeButton != null) upgradeButton.interactable = canUpgrade;
            if (upgradeButtonLabel != null)
                upgradeButtonLabel.text = maxed ? "MAX" : $"Upgrade ({VaultSystem.Instance?.GetCost(_def) ?? 0} Gold)";
        }

        private void OnUpgradeClicked() => VaultSystem.Instance?.Upgrade(_def);
    }
}
