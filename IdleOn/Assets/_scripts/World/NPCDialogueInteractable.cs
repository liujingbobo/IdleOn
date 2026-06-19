using UnityEngine;
using IdleOn.Dialogue;

namespace IdleOn.World
{
    // Walk-up NPC. Uses the WorldInteractable flow (player walks to InteractionX on the lane, then
    // Interact fires on arrival) to start a DialogueDefinition through DialogueSystem. No quest logic —
    // dialogue completion is handled elsewhere via GameEvents.OnDialogueEnded(dialogueId).
    public class NPCDialogueInteractable : WorldInteractable
    {
        [SerializeField] private DialogueDefinition dialogue;
        [SerializeField] private string npcId;

        public string NpcId => npcId;

        public override void Interact(GameObject player)
        {
            if (dialogue == null)
            {
                Debug.LogWarning("[NPCDialogue] dialogue not set — nothing to start.", this);
                return;
            }
            if (DialogueSystem.Instance == null)
            {
                Debug.LogWarning("[NPCDialogue] DialogueSystem.Instance is null — cannot start dialogue.", this);
                return;
            }
            DialogueSystem.Instance.StartDialogue(dialogue);
        }
    }
}
