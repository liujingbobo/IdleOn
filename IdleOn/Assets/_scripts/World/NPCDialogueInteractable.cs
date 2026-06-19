using UnityEngine;
using IdleOn.Dialogue;
using IdleOn.Quests;

namespace IdleOn.World
{
    // Walk-up NPC. Uses the WorldInteractable flow (player walks to InteractionX on the lane, then
    // Interact fires on arrival) to start a DialogueDefinition through DialogueSystem. No quest logic —
    // dialogue completion is handled elsewhere via GameEvents.OnDialogueEnded(dialogueId).
    //
    // Optional per-quest dialogue: if a QuestDialogueSelector is assigned and it returns a dialogue for
    // the active quest, that wins; otherwise the fixed `dialogue` field is used (legacy behavior).
    public class NPCDialogueInteractable : WorldInteractable
    {
        [SerializeField] private DialogueDefinition dialogue;
        [SerializeField] private string npcId;
        [SerializeField] private QuestDialogueSelector questDialogueSelector;   // optional

        public string NpcId => npcId;

        public override void Interact(GameObject player)
        {
            DialogueDefinition toStart = dialogue;
            if (questDialogueSelector != null)
            {
                var questDialogue = questDialogueSelector.GetDialogueForActiveQuest();
                if (questDialogue != null) toStart = questDialogue;
            }

            if (toStart == null)
            {
                Debug.LogWarning("[NPCDialogue] dialogue not set — nothing to start.", this);
                return;
            }
            if (DialogueSystem.Instance == null)
            {
                Debug.LogWarning("[NPCDialogue] DialogueSystem.Instance is null — cannot start dialogue.", this);
                return;
            }
            DialogueSystem.Instance.StartDialogue(toStart);
        }
    }
}
