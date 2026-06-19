using System;

namespace IdleOn.Dialogue
{
    [Serializable]
    public class DialogueChoice
    {
        public string Text;
        public string TargetNodeId;
        public bool EndsDialogue;

        // Reserved for future quest, inventory, or world-state integration.
        public string ConditionId;
        public string EffectId;
    }
}
