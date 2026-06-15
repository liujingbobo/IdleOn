using UnityEngine;
using UnityEngine.UI;
using TMPro;
using IdleOn.Talents;

namespace IdleOn.UI
{
    public class TalentRowUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private TMP_Text effectText;
        [SerializeField] private Button   upgradeButton;
        [SerializeField] private Image    rowBackground;

        [Header("Colors")]
        [SerializeField] private Color defaultColor = new Color(0.12f, 0.12f, 0.14f, 1f);
        [SerializeField] private Color maxedColor   = new Color(0.12f, 0.20f, 0.35f, 1f);

        private TalentDefinition _def;

        public void Initialize(TalentDefinition def)
        {
            _def = def;
            if (nameText != null) nameText.text = def.DisplayName;
            upgradeButton.onClick.AddListener(OnUpgradeClicked);
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
        }

        private void OnUpgradeClicked() => TalentSystem.Instance?.Upgrade(_def);
    }
}
