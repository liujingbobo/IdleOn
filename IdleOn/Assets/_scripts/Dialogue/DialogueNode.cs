using System;
using System.Collections.Generic;
using UnityEngine;

namespace IdleOn.Dialogue
{
    [Serializable]
    public class DialogueNode
    {
        public string NodeId;
        public string SpeakerNameOverride;
        public Sprite Portrait;
        public string Text;
        public List<DialogueChoice> Choices = new List<DialogueChoice>();
        public string NextNodeId;
        public bool EndDialogue;

        public bool HasChoices => Choices != null && Choices.Count > 0;
    }
}
