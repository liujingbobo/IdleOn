using UnityEngine;

namespace IdleOn.Skills
{
    [CreateAssetMenu(fileName = "SkillDef_", menuName = "IdleOn/Skill Definition")]
    public class SkillDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string SkillId;
        public string DisplayName;
        [TextArea(1, 2)]
        public string Description;
        public Sprite Icon;

        [Header("Usage")]
        public float MpCost;
        public float Cooldown;
        public float BaseDamage;

        [Header("Unlock Requirement")]
        public string RequiredTalentId;
        public int    RequiredTalentLevel = 1;
    }
}
