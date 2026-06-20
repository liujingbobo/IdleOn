using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using IdleOn.Core;
using IdleOn.Quests;

namespace IdleOn.UI
{
    // Read-only, always-visible tutorial quest tracker.
    public class QuestWindowUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text descriptionText;

        void OnEnable()
        {
            GameEvents.OnQuestChanged += HandleQuestChanged;
            GameEvents.OnQuestCompleted += HandleQuestCompleted;
            GameEvents.OnQuestProgressChanged += Refresh;
            GameEvents.OnPersistentProgressLoaded += Refresh;
            Refresh();
        }

        void OnDisable()
        {
            GameEvents.OnQuestChanged -= HandleQuestChanged;
            GameEvents.OnQuestCompleted -= HandleQuestCompleted;
            GameEvents.OnQuestProgressChanged -= Refresh;
            GameEvents.OnPersistentProgressLoaded -= Refresh;
        }

        private void HandleQuestChanged(string questId) => Refresh();
        private void HandleQuestCompleted(string questId) => Refresh();

        public void Refresh()
        {
            var questSystem = QuestSystem.Instance;
            var quest = questSystem?.ActiveQuestDefinition;

            if (quest == null)
            {
                if (questSystem != null && questSystem.IsCompleted("q12"))
                {
                    SetText("Tutorial Complete", "Keep exploring.");
                }
                return;
            }

            if (nameText != null)
                nameText.text = quest.Title;

            if (descriptionText == null) return;

            IReadOnlyList<int> counts = questSystem.ActiveObjectiveCounts;
            var builder = new StringBuilder();
            for (int i = 0; i < quest.Objectives.Count; i++)
            {
                if (i > 0) builder.AppendLine();

                var objective = quest.Objectives[i];
                int current = counts != null && i < counts.Count ? counts[i] : 0;
                builder.Append(GetObjectiveLabel(objective));
                builder.Append(": ");
                builder.Append(current);
                builder.Append('/');
                builder.Append(objective.RequiredCount);
            }

            descriptionText.text = builder.ToString();
        }

        private void SetText(string title, string description)
        {
            if (nameText != null) nameText.text = title;
            if (descriptionText != null) descriptionText.text = description;
        }

        private static string GetObjectiveLabel(QuestObjective objective)
        {
            switch (objective.Type)
            {
                case QuestObjectiveType.GroundMove:
                    return "Move";
                case QuestObjectiveType.MapEntered:
                    return "Go to " + ReadableId(objective.TargetId);
                case QuestObjectiveType.EnemyKilled:
                    return objective.TargetId == "slime"
                        ? "Slimes defeated"
                        : "Defeat " + ReadableId(objective.TargetId);
                case QuestObjectiveType.ItemCollected:
                    return objective.TargetId == "slime_essence"
                        ? "Slime Essence"
                        : "Collect " + ReadableId(objective.TargetId);
                case QuestObjectiveType.ItemCrafted:
                    return objective.TargetId == "slime_sword"
                        ? "Craft Slime Sword"
                        : "Craft " + ReadableId(objective.TargetId);
                case QuestObjectiveType.ItemEquipped:
                    return objective.TargetId == "slime_sword"
                        ? "Equip Slime Sword"
                        : "Equip " + ReadableId(objective.TargetId);
                case QuestObjectiveType.TalentUpgraded:
                    return objective.TargetId == "fireball_training"
                        ? "Upgrade Fireball Training"
                        : "Upgrade " + ReadableId(objective.TargetId);
                case QuestObjectiveType.DialogueEnded:
                    return objective.TargetId != null && objective.TargetId.StartsWith("chief_")
                        ? "Talk to Chief"
                        : "Talk to " + ReadableId(objective.TargetId);
                default:
                    return ReadableId(objective.TargetId);
            }
        }

        private static string ReadableId(string id)
        {
            if (string.IsNullOrEmpty(id)) return "Objective";
            string[] words = id.Replace('-', '_').Split('_');
            for (int i = 0; i < words.Length; i++)
            {
                if (string.IsNullOrEmpty(words[i])) continue;
                words[i] = char.ToUpperInvariant(words[i][0]) + words[i].Substring(1);
            }
            return string.Join(" ", words);
        }
    }
}
