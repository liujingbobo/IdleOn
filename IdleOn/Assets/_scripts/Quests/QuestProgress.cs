namespace IdleOn.Quests
{
    // Runtime progress for one active quest (Phase A — in-memory only; serialization is Phase B).
    public class QuestProgress
    {
        public readonly string QuestId;
        public readonly int[]  Counts;   // one slot per objective
        public bool Completed;

        public QuestProgress(string questId, int objectiveCount)
        {
            QuestId = questId;
            Counts  = new int[objectiveCount];
        }
    }
}
