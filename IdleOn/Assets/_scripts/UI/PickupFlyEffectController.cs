using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using IdleOn.Items;

namespace IdleOn.UI
{
    // Visual-only pickup feedback. Gameplay data is already committed before Play is called.
    [RequireComponent(typeof(Canvas))]
    public class PickupFlyEffectController : MonoBehaviour
    {
        [Header("Targets")]
        [SerializeField] private RectTransform inventoryTarget;
        [SerializeField] private RectTransform goldTarget;
        [SerializeField] private Camera worldCamera;

        [Header("Animation")]
        [SerializeField, Min(0.05f)] private float duration = 0.55f;
        [SerializeField, Min(0f)] private float arcHeight = 70f;
        [SerializeField] private Vector2 iconSize = new Vector2(24f, 24f);

        [Header("Arrival")]
        [SerializeField, Min(0f)] private float arrivalPause = 0.04f;
        [SerializeField, Min(0.01f)] private float popDuration = 0.06f;
        [SerializeField, Min(1f)] private float popScale = 1.2f;
        [SerializeField, Min(0.01f)] private float shrinkDuration = 0.1f;

        [Header("Pool")]
        [SerializeField, Min(0)] private int prewarmCount = 8;

        private readonly Queue<Image> _pool = new Queue<Image>();
        private readonly List<Image> _active = new List<Image>();
        private Canvas _canvas;
        private RectTransform _canvasRect;

        void Awake()
        {
            _canvas = GetComponent<Canvas>();
            _canvasRect = (RectTransform)transform;

            for (int i = 0; i < prewarmCount; i++)
                _pool.Enqueue(CreateImage());
        }

        void OnDisable()
        {
            StopAllCoroutines();
            for (int i = _active.Count - 1; i >= 0; i--)
                Release(_active[i]);
        }

        public void PlayItem(Sprite sprite, Vector3 worldPosition)
        {
            Play(sprite, worldPosition, inventoryTarget, "Inventory");
        }

        public void PlayCurrency(Sprite sprite, Vector3 worldPosition, CurrencyType currencyType)
        {
            if (currencyType != CurrencyType.Gold)
            {
                Debug.LogWarning(
                    $"[PickupFlyEffectController] Currency '{currencyType}' is not used by the Gold-only demo; visual skipped.",
                    this);
                return;
            }

            Play(sprite, worldPosition, goldTarget, "Gold");
        }

        private void Play(Sprite sprite, Vector3 worldPosition, RectTransform target, string targetLabel)
        {
            if (!isActiveAndEnabled)
                return;

            if (sprite == null)
            {
                Debug.LogWarning("[PickupFlyEffectController] Pickup sprite is missing; visual skipped.", this);
                return;
            }

            if (target == null || !target.gameObject.activeInHierarchy)
            {
                Debug.LogWarning(
                    $"[PickupFlyEffectController] {targetLabel} HUD target is missing or inactive; visual skipped.",
                    this);
                return;
            }

            Camera sourceCamera = worldCamera != null ? worldCamera : Camera.main;
            if (sourceCamera == null)
            {
                Debug.LogWarning("[PickupFlyEffectController] No world camera is available; visual skipped.", this);
                return;
            }

            Camera uiCamera = _canvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : _canvas.worldCamera;

            Vector2 startScreen = RectTransformUtility.WorldToScreenPoint(sourceCamera, worldPosition);
            Vector2 endScreen = RectTransformUtility.WorldToScreenPoint(uiCamera, target.position);

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _canvasRect, startScreen, uiCamera, out Vector2 start)
                || !RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _canvasRect, endScreen, uiCamera, out Vector2 end))
            {
                Debug.LogWarning("[PickupFlyEffectController] Canvas coordinate conversion failed; visual skipped.", this);
                return;
            }

            Image image = Acquire();
            image.sprite = sprite;
            image.color = Color.white;
            image.rectTransform.sizeDelta = iconSize;
            image.rectTransform.anchoredPosition = start;
            image.rectTransform.localScale = Vector3.one;
            image.gameObject.SetActive(true);
            image.transform.SetAsLastSibling();

            Vector2 control = new Vector2(
                Mathf.Lerp(start.x, end.x, 0.35f),
                Mathf.Max(start.y, end.y) + arcHeight);
            StartCoroutine(Animate(image, start, control, end));
        }

        private IEnumerator Animate(Image image, Vector2 start, Vector2 control, Vector2 end)
        {
            float elapsed = 0f;
            float safeDuration = Mathf.Max(0.05f, duration);

            while (elapsed < safeDuration && image != null)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / safeDuration);
                float oneMinusT = 1f - t;

                image.rectTransform.anchoredPosition =
                    oneMinusT * oneMinusT * start
                    + 2f * oneMinusT * t * control
                    + t * t * end;
                image.rectTransform.localScale = Vector3.one;
                image.color = Color.white;
                yield return null;
            }

            if (image == null) yield break;

            image.rectTransform.anchoredPosition = end;
            image.rectTransform.localScale = Vector3.one;
            image.color = Color.white;

            float pauseElapsed = 0f;
            while (pauseElapsed < arrivalPause && image != null)
            {
                pauseElapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            if (image == null) yield break;

            float popElapsed = 0f;
            float safePopDuration = Mathf.Max(0.01f, popDuration);
            while (popElapsed < safePopDuration && image != null)
            {
                popElapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(popElapsed / safePopDuration);
                float scale = Mathf.Lerp(1f, popScale, Mathf.SmoothStep(0f, 1f, t));
                image.rectTransform.localScale = Vector3.one * scale;
                image.color = Color.white;
                yield return null;
            }

            if (image == null) yield break;

            float shrinkElapsed = 0f;
            float safeShrinkDuration = Mathf.Max(0.01f, shrinkDuration);
            while (shrinkElapsed < safeShrinkDuration && image != null)
            {
                shrinkElapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(shrinkElapsed / safeShrinkDuration);
                float remaining = 1f - Mathf.SmoothStep(0f, 1f, t);
                image.rectTransform.localScale = Vector3.one * (popScale * remaining);

                Color color = Color.white;
                color.a = remaining;
                image.color = color;
                yield return null;
            }

            if (image != null) Release(image);
        }

        private Image Acquire()
        {
            Image image = _pool.Count > 0 ? _pool.Dequeue() : CreateImage();
            _active.Add(image);
            return image;
        }

        private void Release(Image image)
        {
            if (image == null) return;

            _active.Remove(image);
            image.gameObject.SetActive(false);
            image.sprite = null;
            image.color = Color.white;
            image.rectTransform.localScale = Vector3.one;
            _pool.Enqueue(image);
        }

        private Image CreateImage()
        {
            var go = new GameObject(
                "PickupFlyIcon",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            go.transform.SetParent(transform, false);

            var image = go.GetComponent<Image>();
            image.raycastTarget = false;
            image.preserveAspect = true;
            image.rectTransform.sizeDelta = iconSize;
            go.SetActive(false);
            return image;
        }
    }
}
