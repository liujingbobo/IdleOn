using System;
using UnityEngine;

namespace IdleOn.Dialogue
{
    public class DialogueSystem : MonoBehaviour
    {
        public static DialogueSystem Instance { get; private set; }

        private readonly DialogueSession _session = new DialogueSession();

        public event Action<DialogueDefinition> OnDialogueStarted;
        public event Action<DialogueNode> OnNodeChanged;
        public event Action OnDialogueEnded;

        public DialogueSession Session => _session;
        public bool IsActive => _session.IsActive;
        public DialogueNode CurrentNode => _session.CurrentNode;

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
            if (!_session.Start(definition))
            {
                if (wasActive)
                    OnDialogueEnded?.Invoke();
                return false;
            }

            OnDialogueStarted?.Invoke(definition);
            OnNodeChanged?.Invoke(_session.CurrentNode);
            return true;
        }

        public bool Continue()
        {
            return Advance(_session.Continue);
        }

        public bool Choose(int index)
        {
            return Advance(() => _session.Choose(index));
        }

        public void EndDialogue()
        {
            if (!_session.IsActive)
                return;

            _session.End();
            OnDialogueEnded?.Invoke();
        }

        private bool Advance(Func<bool> action)
        {
            if (!_session.IsActive)
                return false;

            DialogueNode previousNode = _session.CurrentNode;
            bool advanced = action();
            if (!advanced)
                return false;

            if (!_session.IsActive)
            {
                OnDialogueEnded?.Invoke();
            }
            else if (_session.CurrentNode != previousNode)
            {
                OnNodeChanged?.Invoke(_session.CurrentNode);
            }

            return true;
        }
    }
}
