using System;
using System.Collections.Generic;
using UnityEngine;

namespace IdleOn.Dialogue
{
    [CreateAssetMenu(fileName = "DialogueDef_", menuName = "IdleOn/Dialogue Definition")]
    public class DialogueDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string DialogueId;
        public string SpeakerName;
        public string PortraitKey;

        [Header("Nodes")]
        public List<DialogueNode> Nodes = new List<DialogueNode>();

        public DialogueNode GetFirstNode()
        {
            if (Nodes == null) return null;

            foreach (DialogueNode node in Nodes)
            {
                if (node != null)
                    return node;
            }

            return null;
        }

        public DialogueNode FindNode(string nodeId)
        {
            if (string.IsNullOrEmpty(nodeId) || Nodes == null)
                return null;

            foreach (DialogueNode node in Nodes)
            {
                if (node != null && string.Equals(node.NodeId, nodeId, StringComparison.Ordinal))
                    return node;
            }

            return null;
        }
    }
}
