using System.Collections.Generic;
using UnityEngine;

namespace IdleOn.Skills
{
    [CreateAssetMenu(fileName = "SkillDatabase", menuName = "IdleOn/Skill Database")]
    public class SkillDatabase : ScriptableObject
    {
        [SerializeField] private List<SkillDefinition> skills = new List<SkillDefinition>();

        public IReadOnlyList<SkillDefinition> Skills => skills;

        public SkillDefinition GetSkill(string skillId)
        {
            if (string.IsNullOrEmpty(skillId)) return null;
            foreach (var s in skills)
                if (s != null && s.SkillId == skillId) return s;
            return null;
        }
    }
}
