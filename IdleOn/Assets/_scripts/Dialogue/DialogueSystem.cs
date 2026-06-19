using System;
using UnityEngine;
using IdleOn.Core;

namespace IdleOn.Dialogue
{
    public class DialogueSystem : MonoBehaviour
    {
        public static DialogueSystem Instance { get; private set; }

        private readonly DialogueSession _session = new DialogueSession();

        public event Action<DialogueDefinition> OnDialogueStarted;
        public event Action<DialogueNode> OnNodeChanged;
        // Linear, UI-facing line signal: fires for the first line after StartDialogue and after
        // each successful Advance/Continue that changes the current line. Does NOT fire on end.
        public event Action OnDialogueLineChanged;
        public event Action OnDialogueEnded;

        public DialogueSession Session => _session;
        public bool IsActive => _session.IsActive;
        public DialogueNode CurrentNode => _session.CurrentNode;

        // Simple linear API for the future DialogueWindow. SpeakerNameOverride on the node wins,
        // otherwise the definition's SpeakerName. Null when no dialogue is active.
        public string CurrentSpeakerName
        {
            get
            {
                DialogueNode node = _session.CurrentNode;
                if (node == null) return null;
                return !string.IsNullOrEmpty(node.SpeakerNameOverride)
                    ? node.SpeakerNameOverride
                    : _session.Definition?.SpeakerName;
            }
        }

        public string CurrentText => _session.CurrentNode?.Text;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void SetResolvers(
            IDialogueConditionResolver conditionResolver,
            IDialogueEffectRunner effectRunner)
        {
            _session.ConditionResolver = conditionResolver;
            _session.EffectRunner = effectRunner;
        }

        public bool StartDialogue(DialogueDefinition definition)
        {
            bool wasActive = _session.IsActive;
            string previousId = _session.Definition?.DialogueId;
            if (!_session.Start(definition))
            {
                // Starting failed (null/empty definition). If a prior dialogue was active, Start()
                // already ended it internally — surface that single end here.
                if (wasActive)
                    RaiseEnded(previousId);
                return false;
            }

            OnDialogueStarted?.Invoke(definition);
            OnNodeChanged?.Invoke(_session.CurrentNode);
            OnDialogueLineChanged?.Invoke();
            return true;
        }

        // Linear alias for the UI — advance to the next line, or end on the last line.
        public bool Advance()
        {
            return Continue();
        }

        public bool Continue()
        {
            return AdvanceInternal(_session.Continue);
        }

        public bool Choose(int index)
        {
            return AdvanceInternal(() => _session.Choose(index));
        }

        public void EndDialogue()
        {
            if (!_session.IsActive)
                return;

            string dialogueId = _session.Definition?.DialogueId;   // capture before clearing
            _session.End();
            RaiseEnded(dialogueId);
        }

        private bool AdvanceInternal(Func<bool> action)
        {
            if (!_session.IsActive)
                return false;

            DialogueNode previousNode = _session.CurrentNode;
            string dialogueId = _session.Definition?.DialogueId;   // capture before action() may clear the session
            bool advanced = action();
            if (!advanced)
                return false;

            if (!_session.IsActive)
            {
                RaiseEnded(dialogueId);
            }
            else if (_session.CurrentNode != previousNode)
            {
                OnNodeChanged?.Invoke(_session.CurrentNode);
                OnDialogueLineChanged?.Invoke();
            }

            return true;
        }

        // Single end point: fires the local event and the global GameEvents end signal exactly once.
        private void RaiseEnded(string dialogueId)
        {
            OnDialogueEnded?.Invoke();
            GameEvents.RaiseDialogueEnded(dialogueId);
        }
    }
}
