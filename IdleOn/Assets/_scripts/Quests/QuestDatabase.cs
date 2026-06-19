using System.Collections.Generic;
using UnityEngine;

namespace IdleOn.Quests
{
    [CreateAssetMenu(fileName = "QuestDatabase", menuName = "IdleOn/Quest Database")]
    public class QuestDatabase : ScriptableObject
    {
        public List<QuestDefinition> Quests = new List<QuestDefinition>();
        public QuestDefinition StartQuest;   // the first quest (Q1)

        public QuestDefinition Get(string questId)
        {
            if (string.IsNullOrEmpty(questId)) return null;
            foreach (var q in Quests)
                if (q != null && q.QuestId == questId) return q;
            return null;
        }
    }
}
