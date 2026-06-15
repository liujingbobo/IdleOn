using System.Collections.Generic;
using UnityEngine;

namespace IdleOn.Talents
{
    [CreateAssetMenu(fileName = "TalentDatabase", menuName = "IdleOn/Talent Database")]
    public class TalentDatabase : ScriptableObject
    {
        [SerializeField] private List<TalentDefinition> talents = new List<TalentDefinition>();

        public IReadOnlyList<TalentDefinition> Talents => talents;

        public TalentDefinition GetTalent(string talentId)
        {
            foreach (var t in talents)
                if (t != null && t.TalentId == talentId) return t;
            return null;
        }
    }
}
