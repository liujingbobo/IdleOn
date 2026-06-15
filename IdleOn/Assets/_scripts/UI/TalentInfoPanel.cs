using UnityEngine;
using UnityEngine.UI;
using TMPro;
using IdleOn.Core;
using IdleOn.Talents;
using IdleOn.Skills;

namespace IdleOn.UI
{
    public class TalentInfoPanel : MonoBehaviour
    {
        [Header("Panel Root")]
        [SerializeField] private GameObject panel;

        [Header("Talent Info")]
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private TMP_Text currentEffectText;
        [SerializeField] private TMP_Text nextEffectText;

        [Header("Upgrade")]
        [SerializeField] private Button   upgradeButton;

        [Header("Skill Section")]
        [SerializeField] private GameObject skillSection;
        [SerializeField] private TMP_Text   skillNameText;
        [SerializeField] private TMP_Text   skillStatusText;
        [SerializeField] private Image      skillIconImage;

        private TalentDefinition _talent;
        private SkillDefinition  _linkedSkill;

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
            _talent      = def;
            _linkedSkill = FindLinkedSkill(def);
            panel.SetActive(true);
            Refresh();
        }

        public void Hide()
        {
            panel.SetActive(false);
            _talent      = null;
            _linkedSkill = null;
            if (upgradeButton != null) upgradeButton.interactable = false;
        }

        public void Refresh()
        {
            if (_talent == null || !panel.activeSelf) return;

            int level = TalentSystem.Instance?.GetLevel(_talent.TalentId) ?? 0;

            nameText.text        = _talent.DisplayName;
            levelText.text       = $"Lv.{level} / {_talent.MaxLevel}";
            descriptionText.text = _talent.Description;

            currentEffectText.text = level > 0
                ? $"Now: {_talent.GetEffectText(level)}"
                : "Now: —";

            nextEffectText.text = level < _talent.MaxLevel
                ? $"Next: {_talent.GetEffectText(level + 1)}"
                : "— Maxed —";

            if (upgradeButton != null)
                upgradeButton.interactable = TalentSystem.Instance != null
                    && TalentSystem.Instance.CanUpgrade(_talent);

            RefreshSkillSection(level);
        }

        private void OnUpgradeClicked() => TalentSystem.Instance?.Upgrade(_talent);

        private void RefreshSkillSection(int talentLevel)
        {
            if (_linkedSkill == null)
            {
                skillSection?.SetActive(false);
                return;
            }

            skillSection?.SetActive(true);

            if (skillNameText != null)
                skillNameText.text = $"Skill: {_linkedSkill.DisplayName}";

            bool unlocked = talentLevel >= _linkedSkill.RequiredTalentLevel;

            if (skillStatusText != null)
                skillStatusText.text = unlocked
                    ? "Unlocked  (drag slot in Assign mode)"
                    : $"Locked  (reach Lv.{_linkedSkill.RequiredTalentLevel} to unlock)";

            if (skillIconImage != null)
            {
                skillIconImage.sprite = _linkedSkill.Icon;
                skillIconImage.color  = unlocked ? Color.white : new Color(0.3f, 0.3f, 0.3f, 1f);
            }
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
