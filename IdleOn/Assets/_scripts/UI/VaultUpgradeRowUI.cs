using UnityEngine;
using UnityEngine.UI;
using TMPro;
using IdleOn.Vault;

namespace IdleOn.UI
{
    public class VaultUpgradeRowUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text upgradeName;
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private TMP_Text effectText;
        [SerializeField] private TMP_Text costText;
        [SerializeField] private Button   upgradeButton;

        private VaultUpgradeDefinition _def;

        public void Initialize(VaultUpgradeDefinition def)
        {
            _def = def;
            upgradeName.text = def.DisplayName;
            upgradeButton.onClick.AddListener(OnUpgradeClicked);
            Refresh();
        }

        public void Refresh()
        {
            if (_def == null) return;
            var vs = VaultSystem.Instance;
            if (vs == null) return;

            int  level      = vs.GetLevel(_def);
            bool maxed      = level >= _def.MaxLevel;
            bool canUpgrade = vs.CanUpgrade(_def);

            levelText.text  = $"Lv. {level} / {_def.MaxLevel}";
            effectText.text = _def.GetCurrentEffectText(level);
            costText.text   = maxed ? "MAX" : $"{vs.GetCost(_def)} Gold";

            upgradeButton.interactable = canUpgrade;
        }

        private void OnUpgradeClicked()
        {
            VaultSystem.Instance?.Upgrade(_def);
        }
    }
}
