using System;

namespace IdleOn.Quests
{
    public enum QuestObjectiveType
    {
        GroundMove,      // any successful manual ground move
        MapEntered,      // TargetId = mapId
        EnemyKilled,     // TargetId = enemyId ("" = any)
        ItemCollected,   // TargetId = itemId
        ItemCrafted,     // TargetId = itemId
        ItemEquipped,    // TargetId = itemId
        TalentUpgraded,  // TargetId = talentId
        DialogueEnded    // TargetId = dialogueId
    }

    [Serializable]
    public class QuestObjective
    {
        public QuestObjectiveType Type;
        public string TargetId      = string.Empty;   // empty = match any
        public int    RequiredCount = 1;
    }
}
