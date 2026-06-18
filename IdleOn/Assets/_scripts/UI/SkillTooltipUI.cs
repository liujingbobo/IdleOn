using UnityEngine;
using TMPro;
using IdleOn.Skills;

namespace IdleOn.UI
{
    public class SkillTooltipUI : MonoBehaviour
    {
        public static SkillTooltipUI Instance { get; private set; }

        [SerializeField] private GameObject      panel;
        [SerializeField] private TMP_Text        nameText;
        [SerializeField] private TMP_Text        descriptionText;
        [SerializeField] private TMP_Text        mpCostText;
        [SerializeField] private TMP_Text        cooldownText;
        [SerializeField] private TMP_Text        talentRequirementText;

        void Awake()
        {
            Instance = this;
            if (panel != null)
                panel.SetActive(false);
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void Show(SkillDefinition skill)
        {
            if (skill == null || panel == null) return;

            if (nameText != null)        nameText.text        = skill.DisplayName;
            if (descriptionText != null) descriptionText.text  = skill.Description;
            if (mpCostText != null)      mpCostText.text       = $"MP: {skill.MpCost:0.#}";
            if (cooldownText != null)    cooldownText.text     = $"Cooldown: {skill.Cooldown:0.#}s";
            if (talentRequirementText != null)
                talentRequirementText.text = string.IsNullOrEmpty(skill.RequiredTalentId)
                    ? string.Empty
                    : $"Requires talent Lv.{skill.RequiredTalentLevel}";

            panel.SetActive(true);
        }

        public void Hide()
        {
            if (panel != null)
                panel.SetActive(false);
        }
    }
}
