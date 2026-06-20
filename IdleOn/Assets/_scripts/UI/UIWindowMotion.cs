using System.Collections;
using UnityEngine;

namespace IdleOn.UI
{
    public class UIWindowMotion : MonoBehaviour
    {
        public enum WindowState
        {
            Closed,
            Opening,
            Open,
            Closing
        }

        [Header("References")]
        [SerializeField] private GameObject windowRoot;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform scaleTarget;

        [Header("Open")]
        [SerializeField, Range(0.01f, 1f)] private float openStartScale = 0.94f;
        [SerializeField, Min(0.01f)] private float openDuration = 0.16f;

        [Header("Close")]
        [SerializeField, Range(0.01f, 1f)] private float closeEndScale = 0.96f;
        [SerializeField, Min(0.01f)] private float closeDuration = 0.12f;

        private Vector3 _initialScale;
        private Coroutine _animation;
        private int _operationVersion;
        private bool _initialized;
        private WindowState _state = WindowState.Closed;

        public WindowState State => _state;
        public bool IsOpen => _state == WindowState.Opening || _state == WindowState.Open;

        private void Awake()
        {
            EnsureInitialized();
        }

        private void OnDisable()
        {
            if (!_initialized) return;

            CancelCurrentAnimation();
            ApplyClosedVisuals();
            if (windowRoot != null && windowRoot != gameObject)
                windowRoot.SetActive(false);
            _state = WindowState.Closed;
        }

        public void SetClosedImmediate()
        {
            EnsureInitialized();
            CancelCurrentAnimation();
            ApplyClosedVisuals();
            if (windowRoot != null && windowRoot != gameObject)
                windowRoot.SetActive(false);
            _state = WindowState.Closed;
        }

        public void PlayOpen()
        {
            EnsureInitialized();
            if (!HasValidReferences()) return;
            if (_state == WindowState.Open || _state == WindowState.Opening) return;

            bool wasClosed = _state == WindowState.Closed || !windowRoot.activeSelf;
            int operation = BeginOperation(WindowState.Opening);

            windowRoot.SetActive(true);
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            if (wasClosed)
            {
                canvasGroup.alpha = 0f;
                scaleTarget.localScale = ScaledInitial(openStartScale);
            }

            _animation = StartCoroutine(Animate(
                operation,
                canvasGroup.alpha,
                1f,
                scaleTarget.localScale,
                _initialScale,
                openDuration,
                CompleteOpen));
        }

        public void PlayClose()
        {
            EnsureInitialized();
            if (!HasValidReferences()) return;
            if (_state == WindowState.Closed || _state == WindowState.Closing) return;

            int operation = BeginOperation(WindowState.Closing);
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            _animation = StartCoroutine(Animate(
                operation,
                canvasGroup.alpha,
                0f,
                scaleTarget.localScale,
                ScaledInitial(closeEndScale),
                closeDuration,
                CompleteClose));
        }

        private IEnumerator Animate(
            int operation,
            float startAlpha,
            float endAlpha,
            Vector3 startScale,
            Vector3 endScale,
            float duration,
            System.Action<int> onComplete)
        {
            float elapsed = 0f;
            float safeDuration = Mathf.Max(0.01f, duration);

            while (elapsed < safeDuration)
            {
                if (operation != _operationVersion)
                    yield break;

                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / safeDuration);
                float eased = Mathf.SmoothStep(0f, 1f, t);
                canvasGroup.alpha = Mathf.LerpUnclamped(startAlpha, endAlpha, eased);
                scaleTarget.localScale = Vector3.LerpUnclamped(startScale, endScale, eased);
                yield return null;
            }

            if (operation != _operationVersion)
                yield break;

            canvasGroup.alpha = endAlpha;
            scaleTarget.localScale = endScale;
            _animation = null;
            onComplete?.Invoke(operation);
        }

        private void CompleteOpen(int operation)
        {
            if (operation != _operationVersion) return;

            _state = WindowState.Open;
            canvasGroup.alpha = 1f;
            scaleTarget.localScale = _initialScale;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        private void CompleteClose(int operation)
        {
            if (operation != _operationVersion) return;

            _state = WindowState.Closed;
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            windowRoot.SetActive(false);
            scaleTarget.localScale = ScaledInitial(openStartScale);
        }

        private int BeginOperation(WindowState state)
        {
            CancelCurrentAnimation();
            _state = state;
            return _operationVersion;
        }

        private void CancelCurrentAnimation()
        {
            _operationVersion++;
            if (_animation != null)
            {
                StopCoroutine(_animation);
                _animation = null;
            }
        }

        private void EnsureInitialized()
        {
            if (_initialized) return;

            if (canvasGroup == null && windowRoot != null)
                canvasGroup = windowRoot.GetComponent<CanvasGroup>();
            if (scaleTarget != null)
                _initialScale = scaleTarget.localScale;

            _initialized = true;
        }

        private bool HasValidReferences()
        {
            if (windowRoot != null && canvasGroup != null && scaleTarget != null)
                return true;

            Debug.LogWarning("[UIWindowMotion] Missing windowRoot, CanvasGroup, or scaleTarget reference.", this);
            return false;
        }

        private Vector3 ScaledInitial(float multiplier)
        {
            return Vector3.Scale(_initialScale, Vector3.one * multiplier);
        }

        private void ApplyClosedVisuals()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            if (scaleTarget != null)
                scaleTarget.localScale = ScaledInitial(openStartScale);
        }
    }
}
