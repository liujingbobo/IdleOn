using System.Collections;
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

        [Header("HUD Fade")]
        [SerializeField] private CanvasGroup hudCanvasGroup;
        [SerializeField, Min(0f)] private float hudFadeDuration = 0.16f;

        [Header("Typewriter")]
        [SerializeField, Min(1f)] private float charactersPerSecond = 45f;

        [Header("Portrait Enter")]
        [SerializeField] private RectTransform portraitRect;
        [SerializeField] private float portraitEnterOffset = 120f;
        [SerializeField, Min(0f)] private float portraitEnterDuration = 0.18f;

        private bool _subscribed;
        private bool _warnedMissing;
        private bool _isTyping;
        private Coroutine _typewriterRoutine;
        private Coroutine _hudFadeRoutine;
        private Coroutine _portraitEnterRoutine;
        private int _typewriterVersion;
        private int _hudFadeVersion;
        private int _portraitEnterVersion;
        private Vector2 _portraitFinalPosition;
        private bool _portraitPositionCached;

        void Awake()
        {
            if (portraitRect == null && dialoguePortrait != null)
                portraitRect = dialoguePortrait.rectTransform;
            CachePortraitFinalPosition();
            if (hudCanvasGroup != null)
                hudCanvasGroup.alpha = 1f;

            Hide();
            if (fullScreenButton != null)
                fullScreenButton.onClick.AddListener(OnFullScreenClicked);
        }

        // Subscribe in both OnEnable and Start: OnEnable covers re-enable; Start covers the case
        // where DialogueSystem's Awake ran after ours (Instance was null during our OnEnable).
        void OnEnable() => TrySubscribe(false);
        void Start()    => TrySubscribe(true);
        void OnDisable()
        {
            Unsubscribe();
            ResetAnimations();
        }

        void OnDestroy()
        {
            if (fullScreenButton != null)
                fullScreenButton.onClick.RemoveListener(OnFullScreenClicked);
        }

        private void TrySubscribe(bool warnIfMissing)
        {
            if (_subscribed) return;

            DialogueSystem sys = DialogueSystem.Instance;
            if (sys == null)
            {
                if (warnIfMissing && !_warnedMissing)
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
            RefreshLine();
            FadeHudTo(0f);
            PlayPortraitEnter();
        }

        private void HandleLineChanged() => RefreshLine();

        private void HandleEnded()
        {
            CancelTypewriter();
            RestorePortraitPosition();
            Hide();
            FadeHudTo(1f);
        }

        private void OnFullScreenClicked()
        {
            if (_isTyping)
            {
                CompleteTypewriter();
                return;
            }

            DialogueSystem sys = DialogueSystem.Instance;
            if (sys != null) sys.Advance();
        }

        private void RefreshLine()
        {
            DialogueSystem sys = DialogueSystem.Instance;
            if (sys == null) return;

            if (speakerNameText != null)
                speakerNameText.text = sys.CurrentSpeakerName ?? string.Empty;
            if (dialogueText != null)
                dialogueText.text = sys.CurrentText ?? string.Empty;
            if (dialoguePortrait != null)
            {
                Sprite portrait = sys.CurrentPortrait;
                dialoguePortrait.sprite = portrait;
                dialoguePortrait.enabled = portrait != null;
            }

            StartTypewriter(sys.CurrentText ?? string.Empty);
        }

        private void Show()
        {
            if (windowRoot != null) windowRoot.SetActive(true);
        }

        private void Hide()
        {
            if (windowRoot != null) windowRoot.SetActive(false);
        }

        private void StartTypewriter(string text)
        {
            CancelTypewriter();
            if (dialogueText == null) return;

            dialogueText.text = text;
            dialogueText.maxVisibleCharacters = 0;
            dialogueText.ForceMeshUpdate();

            int characterCount = dialogueText.textInfo.characterCount;
            if (characterCount <= 0 || charactersPerSecond <= 0f)
            {
                CompleteTypewriter();
                return;
            }

            _isTyping = true;
            int operation = _typewriterVersion;
            _typewriterRoutine = StartCoroutine(TypeText(operation, characterCount));
        }

        private IEnumerator TypeText(int operation, int characterCount)
        {
            float visibleCharacters = 0f;

            while (visibleCharacters < characterCount)
            {
                if (operation != _typewriterVersion)
                    yield break;

                visibleCharacters += charactersPerSecond * Time.unscaledDeltaTime;
                dialogueText.maxVisibleCharacters =
                    Mathf.Min(characterCount, Mathf.FloorToInt(visibleCharacters));
                yield return null;
            }

            if (operation != _typewriterVersion)
                yield break;

            dialogueText.maxVisibleCharacters = characterCount;
            _isTyping = false;
            _typewriterRoutine = null;
        }

        private void CompleteTypewriter()
        {
            CancelTypewriter();
            if (dialogueText != null)
                dialogueText.maxVisibleCharacters = int.MaxValue;
        }

        private void CancelTypewriter()
        {
            _typewriterVersion++;
            _isTyping = false;
            if (_typewriterRoutine != null)
            {
                StopCoroutine(_typewriterRoutine);
                _typewriterRoutine = null;
            }
        }

        private void FadeHudTo(float targetAlpha)
        {
            if (hudCanvasGroup == null) return;

            _hudFadeVersion++;
            if (_hudFadeRoutine != null)
            {
                StopCoroutine(_hudFadeRoutine);
                _hudFadeRoutine = null;
            }

            int operation = _hudFadeVersion;
            float startAlpha = hudCanvasGroup.alpha;
            if (hudFadeDuration <= 0f)
            {
                hudCanvasGroup.alpha = targetAlpha;
                return;
            }

            _hudFadeRoutine = StartCoroutine(
                FadeHud(operation, startAlpha, targetAlpha));
        }

        private IEnumerator FadeHud(int operation, float startAlpha, float targetAlpha)
        {
            float elapsed = 0f;

            while (elapsed < hudFadeDuration)
            {
                if (operation != _hudFadeVersion)
                    yield break;

                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / hudFadeDuration);
                hudCanvasGroup.alpha = Mathf.LerpUnclamped(
                    startAlpha,
                    targetAlpha,
                    Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }

            if (operation != _hudFadeVersion)
                yield break;

            hudCanvasGroup.alpha = targetAlpha;
            _hudFadeRoutine = null;
        }

        private void PlayPortraitEnter()
        {
            if (portraitRect == null) return;

            CachePortraitFinalPosition();
            _portraitEnterVersion++;
            if (_portraitEnterRoutine != null)
            {
                StopCoroutine(_portraitEnterRoutine);
                _portraitEnterRoutine = null;
            }

            Vector2 startPosition =
                _portraitFinalPosition + Vector2.right * portraitEnterOffset;
            portraitRect.anchoredPosition = startPosition;

            if (portraitEnterDuration <= 0f)
            {
                portraitRect.anchoredPosition = _portraitFinalPosition;
                return;
            }

            int operation = _portraitEnterVersion;
            _portraitEnterRoutine = StartCoroutine(
                MovePortrait(operation, startPosition, _portraitFinalPosition));
        }

        private IEnumerator MovePortrait(
            int operation,
            Vector2 startPosition,
            Vector2 targetPosition)
        {
            float elapsed = 0f;

            while (elapsed < portraitEnterDuration)
            {
                if (operation != _portraitEnterVersion)
                    yield break;

                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / portraitEnterDuration);
                portraitRect.anchoredPosition = Vector2.LerpUnclamped(
                    startPosition,
                    targetPosition,
                    Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }

            if (operation != _portraitEnterVersion)
                yield break;

            portraitRect.anchoredPosition = targetPosition;
            _portraitEnterRoutine = null;
        }

        private void CachePortraitFinalPosition()
        {
            if (_portraitPositionCached || portraitRect == null) return;

            _portraitFinalPosition = portraitRect.anchoredPosition;
            _portraitPositionCached = true;
        }

        private void RestorePortraitPosition()
        {
            _portraitEnterVersion++;
            if (_portraitEnterRoutine != null)
            {
                StopCoroutine(_portraitEnterRoutine);
                _portraitEnterRoutine = null;
            }

            if (_portraitPositionCached && portraitRect != null)
                portraitRect.anchoredPosition = _portraitFinalPosition;
        }

        private void ResetAnimations()
        {
            CancelTypewriter();
            RestorePortraitPosition();

            _hudFadeVersion++;
            if (_hudFadeRoutine != null)
            {
                StopCoroutine(_hudFadeRoutine);
                _hudFadeRoutine = null;
            }

            if (hudCanvasGroup != null)
                hudCanvasGroup.alpha = 1f;
        }
    }
}
