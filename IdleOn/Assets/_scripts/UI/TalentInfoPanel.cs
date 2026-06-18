using UnityEngine;
using UnityEngine.UI;
using TMPro;
using IdleOn.Core;
using IdleOn.Talents;

namespace IdleOn.UI
{
    public class TalentInfoPanel : MonoBehaviour
    {
        [Header("Panel Root")]
        [SerializeField] private GameObject panel;

        [Header("Talent Info")]
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text descriptionText;

        [Header("Level Bar")]
        [SerializeField] private Image    levelFiller;
        [SerializeField] private TMP_Text levelText;

        [Header("Upgrade")]
        [SerializeField] private Button upgradeButton;

        private TalentDefinition _talent;

        void Awake()
        {
            panel.SetActive(false);
            GameEvents.OnTalentChanged += Refresh;
            upgradeButton?.onClick.AddListener(OnUpgradeClicked);
        }

        void OnDestroy()
        {
            GameEvents.OnTalentChanged -= Refresh;
            upgradeButton?.onClick.RemoveListener(OnUpgradeClicked);
        }

        public void Show(TalentDefinition def)
        {
            _talent = def;
            panel.SetActive(true);
            Refresh();
        }

        public void Hide()
        {
            panel.SetActive(false);
            _talent = null;
            if (upgradeButton != null) upgradeButton.interactable = false;
        }

        public void Refresh()
        {
            if (_talent == null || !panel.activeSelf) return;

            int level = TalentSystem.Instance?.GetLevel(_talent.TalentId) ?? 0;
            bool maxed = level >= _talent.MaxLevel;

            nameText.text        = _talent.DisplayName;
            descriptionText.text = _talent.Description;

            if (levelFiller != null)
                levelFiller.fillAmount = _talent.MaxLevel > 0 ? (float)level / _talent.MaxLevel : 0f;

            if (levelText != null)
                levelText.text = maxed ? "Max" : $"Lv. {level} / {_talent.MaxLevel}";

            if (upgradeButton != null)
                upgradeButton.interactable = TalentSystem.Instance != null
                    && TalentSystem.Instance.CanUpgrade(_talent);
        }

        private void OnUpgradeClicked() => TalentSystem.Instance?.Upgrade(_talent);
    }
}
