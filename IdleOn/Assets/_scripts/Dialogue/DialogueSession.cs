using System;
using System.Collections.Generic;

namespace IdleOn.Dialogue
{
    public class DialogueSession
    {
        private static readonly IReadOnlyList<DialogueChoice> EmptyChoices =
            Array.Empty<DialogueChoice>();

        private DialogueDefinition _definition;
        private DialogueNode _currentNode;

        public IDialogueConditionResolver ConditionResolver { get; set; }
        public IDialogueEffectRunner EffectRunner { get; set; }

        public bool IsActive => _definition != null && _currentNode != null;
        public DialogueDefinition Definition => _definition;
        public DialogueNode CurrentNode => _currentNode;
        public IReadOnlyList<DialogueChoice> CurrentChoices =>
            _currentNode != null && _currentNode.Choices != null
                ? _currentNode.Choices
                : EmptyChoices;

        public bool Start(DialogueDefinition definition)
        {
            End();

            if (definition == null)
                return false;

            DialogueNode firstNode = definition.GetFirstNode();
            if (firstNode == null)
                return false;

            _definition = definition;
            _currentNode = firstNode;
            return true;
        }

        public bool Continue()
        {
            if (!IsActive)
                return false;

            // A node with choices cannot advance until the player selects one.
            if (_currentNode.HasChoices)
                return false;

            if (_currentNode.EndDialogue)
            {
                End();
                return true;
            }

            return MoveToNodeOrEnd(_currentNode.NextNodeId);
        }

        public bool Choose(int index)
        {
            if (!IsActive || !_currentNode.HasChoices)
                return false;

            if (index < 0 || index >= _currentNode.Choices.Count)
                return false;

            DialogueChoice choice = _currentNode.Choices[index];
            if (choice == null)
                return false;

            if (ConditionResolver != null && !ConditionResolver.CanUseChoice(choice))
                return false;

            EffectRunner?.RunChoiceEffect(choice);

            if (choice.EndsDialogue)
            {
                End();
                return true;
            }

            return MoveToNodeOrEnd(choice.TargetNodeId);
        }

        public void End()
        {
            _currentNode = null;
            _definition = null;
        }

        private bool MoveToNodeOrEnd(string nodeId)
        {
            DialogueNode nextNode = _definition?.FindNode(nodeId);
            if (nextNode == null)
            {
                End();
                return true;
            }

            _currentNode = nextNode;
            return true;
        }
    }
}
