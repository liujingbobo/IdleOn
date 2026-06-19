using System;
using System.Collections.Generic;

namespace IdleOn.Dialogue
{
    [Serializable]
    public class DialogueNode
    {
        public string NodeId;
        public string SpeakerNameOverride;
        public string Text;
        public List<DialogueChoice> Choices = new List<DialogueChoice>();
        public string NextNodeId;
        public bool EndDialogue;

        public bool HasChoices => Choices != null && Choices.Count > 0;
    }
}
