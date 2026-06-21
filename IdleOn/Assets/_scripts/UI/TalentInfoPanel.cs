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

        [Header("Effects")]
        [SerializeField] private TMP_Text   currentEffectText;
        [SerializeField] private GameObject currentSeparatorRoot;
        [SerializeField] private TMP_Text   nextEffectText;
        [SerializeField] private GameObject nextSeparatorRoot;

        [Header("Upgrade")]
        [SerializeField] private Button upgradeButton;

        private TalentDefinition _talent;

        void Awake()
        {
            ResolveEffectReferences();
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

            SetEffectVisible(
                currentEffectText,
                currentSeparatorRoot,
                level > 0,
                _talent.GetEffectText(level));

            SetEffectVisible(
                nextEffectText,
                nextSeparatorRoot,
                !maxed,
                _talent.GetEffectText(level + 1));

            if (upgradeButton != null)
                upgradeButton.interactable = TalentSystem.Instance != null
                    && TalentSystem.Instance.CanUpgrade(_talent);
        }

        private static void SetEffectVisible(
            TMP_Text text,
            GameObject separatorRoot,
            bool visible,
            string content)
        {
            if (text != null)
            {
                text.text = content;
                text.gameObject.SetActive(visible);
            }

            separatorRoot?.SetActive(visible);
        }

        private void ResolveEffectReferences()
        {
            if (currentEffectText == null || nextEffectText == null)
            {
                foreach (var text in GetComponentsInChildren<TMP_Text>(true))
                {
                    if (currentEffectText == null && text.gameObject.name == "CurrentEffect")
                        currentEffectText = text;
                    else if (nextEffectText == null && text.gameObject.name == "NextEffect")
                        nextEffectText = text;
                }
            }

            if (currentSeparatorRoot == null || nextSeparatorRoot == null)
            {
                foreach (var child in GetComponentsInChildren<Transform>(true))
                {
                    if (currentSeparatorRoot == null && child.name == "CurrentSeparater")
                        currentSeparatorRoot = child.gameObject;
                    else if (nextSeparatorRoot == null && child.name == "NextSeparater")
                        nextSeparatorRoot = child.gameObject;
                }
            }
        }

        private void OnUpgradeClicked() => TalentSystem.Instance?.Upgrade(_talent);
    }
}
