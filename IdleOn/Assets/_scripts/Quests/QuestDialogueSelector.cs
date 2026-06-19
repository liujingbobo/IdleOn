using System;
using System.Collections.Generic;
using UnityEngine;
using IdleOn.Dialogue;

namespace IdleOn.Quests
{
    // Maps the currently-active quest id to the DialogueDefinition an NPC should start. Lives next to
    // NPCDialogueInteractable; the interactable asks it for a dialogue and falls back to its own fixed
    // dialogue when no entry matches. Keeps all quest→dialogue routing here — no quest logic in
    // DialogueSystem or NPCDialogueInteractable.
    public class QuestDialogueSelector : MonoBehaviour
    {
        [Serializable]
        public class Entry
        {
            public string             QuestId;
            public DialogueDefinition Dialogue;
        }

        [SerializeField] private List<Entry> entries = new List<Entry>();
        [SerializeField] private List<DialogueDefinition> postTutorialDialogues = new List<DialogueDefinition>();

        // Dialogue mapped to the active quest, or null when no entry matches (caller should fall back).
        public DialogueDefinition GetDialogueForActiveQuest()
        {
            var qs = QuestSystem.Instance;
            if (qs == null) return null;

            string active = qs.ActiveQuestId;
            if (!string.IsNullOrEmpty(active))
            {
                foreach (var e in entries)
                    if (e != null && e.Dialogue != null && e.QuestId == active)
                        return e.Dialogue;

                return null;
            }

            if (qs.IsCompleted("q12") && postTutorialDialogues != null && postTutorialDialogues.Count > 0)
            {
                int start = UnityEngine.Random.Range(0, postTutorialDialogues.Count);
                for (int i = 0; i < postTutorialDialogues.Count; i++)
                {
                    var dialogue = postTutorialDialogues[(start + i) % postTutorialDialogues.Count];
                    if (dialogue != null) return dialogue;
                }
            }

            return null;
        }
    }
}
