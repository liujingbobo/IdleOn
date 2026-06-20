using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace IdleOn.UI
{
    public class UIButtonMotion : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerDownHandler,
        IPointerUpHandler,
        ICancelHandler
    {
        [SerializeField] private RectTransform visualTarget;
        [SerializeField] private Selectable selectable;

        [Header("Scale")]
        [SerializeField, Min(1f)] private float hoverScale = 1.06f;
        [SerializeField, Min(0f)] private float hoverDuration = 0.08f;
        [SerializeField, Range(0.01f, 1f)] private float pressScale = 0.96f;
        [SerializeField, Min(0f)] private float pressDuration = 0.05f;

        private Vector3 _initialScale;
        private Vector3 _transitionStart;
        private Vector3 _transitionTarget;
        private float _transitionDuration;
        private float _transitionElapsed;
        private bool _isHovering;
        private bool _isPressed;
        private bool _isTransitioning;
        private bool _initialized;

        private void Awake()
        {
            EnsureInitialized();
        }

        private void Update()
        {
            EnsureInitialized();

            if (!CanAnimate())
            {
                ResetInteractionState(true);
                return;
            }

            if (!_isTransitioning || visualTarget == null)
                return;

            _transitionElapsed += Time.unscaledDeltaTime;
            float duration = Mathf.Max(0.0001f, _transitionDuration);
            float t = Mathf.Clamp01(_transitionElapsed / duration);
            visualTarget.localScale = Vector3.LerpUnclamped(
                _transitionStart,
                _transitionTarget,
                Mathf.SmoothStep(0f, 1f, t));

            if (t >= 1f)
            {
                visualTarget.localScale = _transitionTarget;
                _isTransitioning = false;
            }
        }

        private void OnDisable()
        {
            ResetInteractionState(true);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!CanAnimate()) return;

            _isHovering = true;
            if (!_isPressed)
                BeginTransition(hoverScale, hoverDuration);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isHovering = false;
            _isPressed = false;
            BeginTransition(1f, hoverDuration);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!CanAnimate() || eventData.button != PointerEventData.InputButton.Left)
                return;

            _isPressed = true;
            BeginTransition(pressScale, pressDuration);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            _isPressed = false;
            if (!CanAnimate())
            {
                ResetInteractionState(true);
                return;
            }

            BeginTransition(_isHovering ? hoverScale : 1f, hoverDuration);
        }

        public void OnCancel(BaseEventData eventData)
        {
            ResetInteractionState(false);
        }

        private void EnsureInitialized()
        {
            if (_initialized) return;

            if (visualTarget == null)
                visualTarget = transform as RectTransform;
            if (selectable == null)
                selectable = GetComponent<Selectable>();

            if (visualTarget != null)
            {
                _initialScale = visualTarget.localScale;
                _transitionStart = _initialScale;
                _transitionTarget = _initialScale;
            }

            _initialized = true;
        }

        private bool CanAnimate()
        {
            return isActiveAndEnabled
                && visualTarget != null
                && (selectable == null || selectable.IsInteractable());
        }

        private void BeginTransition(float multiplier, float duration)
        {
            EnsureInitialized();
            if (visualTarget == null) return;

            _transitionStart = visualTarget.localScale;
            _transitionTarget = Vector3.Scale(_initialScale, Vector3.one * multiplier);
            _transitionDuration = duration;
            _transitionElapsed = 0f;
            _isTransitioning = true;

            if (duration <= 0f)
            {
                visualTarget.localScale = _transitionTarget;
                _isTransitioning = false;
            }
        }

        private void ResetInteractionState(bool immediate)
        {
            _isHovering = false;
            _isPressed = false;

            if (!_initialized || visualTarget == null)
                return;

            if (immediate)
            {
                visualTarget.localScale = _initialScale;
                _transitionStart = _initialScale;
                _transitionTarget = _initialScale;
                _transitionElapsed = 0f;
                _isTransitioning = false;
            }
            else
            {
                BeginTransition(1f, hoverDuration);
            }
        }
    }
}
