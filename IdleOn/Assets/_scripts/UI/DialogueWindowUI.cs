using UnityEngine;
using UnityEngine.UI;
using TMPro;
using IdleOn.Dialogue;

namespace IdleOn.UI
{
    // Minimal linear dialogue window. Lives on an always-active object (the Canvas) so it can
    // receive DialogueSystem events while the window root itself is hidden. No choices, no quest
    // logic — just show/refresh/hide driven by DialogueSystem and a full-screen click to advance.
    public class DialogueWindowUI : MonoBehaviour
    {
        [SerializeField] private GameObject windowRoot;
        [SerializeField] private TMP_Text   speakerNameText;
        [SerializeField] private TMP_Text   dialogueText;
        [SerializeField] private Button     fullScreenButton;
        [SerializeField] private Image dialoguePortrait;

        private bool _subscribed;
        private bool _warnedMissing;

        void Awake()
        {
            Hide();
            if (fullScreenButton != null)
                fullScreenButton.onClick.AddListener(OnFullScreenClicked);
        }

        // Subscribe in both OnEnable and Start: OnEnable covers re-enable; Start covers the case
        // where DialogueSystem's Awake ran after ours (Instance was null during our OnEnable).
        void OnEnable() => TrySubscribe();
        void Start()    => TrySubscribe();
        void OnDisable() => Unsubscribe();

        void OnDestroy()
        {
            if (fullScreenButton != null)
                fullScreenButton.onClick.RemoveListener(OnFullScreenClicked);
        }

        private void TrySubscribe()
        {
            if (_subscribed) return;

            DialogueSystem sys = DialogueSystem.Instance;
            if (sys == null)
            {
                if (!_warnedMissing)
                {
                    Debug.LogWarning("[DialogueWindowUI] No DialogueSystem.Instance found — window stays hidden.", this);
                    _warnedMissing = true;
                }
                return;
            }

            sys.OnDialogueStarted     += HandleStarted;
            sys.OnDialogueLineChanged += HandleLineChanged;
            sys.OnDialogueEnded       += HandleEnded;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;

            DialogueSystem sys = DialogueSystem.Instance;
            if (sys != null)
            {
                sys.OnDialogueStarted     -= HandleStarted;
                sys.OnDialogueLineChanged -= HandleLineChanged;
                sys.OnDialogueEnded       -= HandleEnded;
            }
            _subscribed = false;
        }

        private void HandleStarted(DialogueDefinition def)
        {
            Show();
            Refresh();
        }

        private void HandleLineChanged() => Refresh();

        private void HandleEnded() => Hide();

        private void OnFullScreenClicked()
        {
            DialogueSystem sys = DialogueSystem.Instance;
            if (sys != null) sys.Advance();
        }

        private void Refresh()
        {
            DialogueSystem sys = DialogueSystem.Instance;
            if (sys == null) return;

            if (speakerNameText != null)
                speakerNameText.text = sys.CurrentSpeakerName ?? string.Empty;
            if (dialogueText != null)
                dialogueText.text = sys.CurrentText ?? string.Empty;
        }

        private void Show()
        {
            if (windowRoot != null) windowRoot.SetActive(true);
        }

        private void Hide()
        {
            if (windowRoot != null) windowRoot.SetActive(false);
        }
    }
}
