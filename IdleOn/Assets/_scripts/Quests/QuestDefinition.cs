using System.Collections.Generic;
using UnityEngine;

namespace IdleOn.Quests
{
    [CreateAssetMenu(fileName = "QuestDef_", menuName = "IdleOn/Quest Definition")]
    public class QuestDefinition : ScriptableObject
    {
        public string QuestId;
        public string Title;

        [Tooltip("All objectives must reach RequiredCount for the quest to complete.")]
        public List<QuestObjective> Objectives = new List<QuestObjective>();

        [Header("Rewards / unlocks")]
        public int          ExpReward;
        public FeatureFlags UnlocksFeatures = FeatureFlags.None;

        [Tooltip("Phase A: logged as a placeholder (no MapSystem/PortalGate wiring yet).")]
        public string UnlocksMapId;

        [Tooltip("Quest to activate when this one completes. Empty = end of chain.")]
        public string NextQuestId;
    }
}
