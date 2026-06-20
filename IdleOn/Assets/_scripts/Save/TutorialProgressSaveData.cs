using System;
using System.Collections.Generic;

namespace IdleOn.Save
{
    [Serializable]
    public class QuestSaveData
    {
        public string ActiveQuestId = string.Empty;
        public List<string> CompletedQuestIds = new List<string>();
        public List<int> ActiveObjectiveCounts = new List<int>();
    }

    [Serializable]
    public class EnemyKillSaveData
    {
        public string EnemyId = string.Empty;
        public int Count;
    }
}
