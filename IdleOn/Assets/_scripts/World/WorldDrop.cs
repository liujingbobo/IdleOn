using UnityEngine;
using IdleOn.Loot;

namespace IdleOn.World
{
    [RequireComponent(typeof(CircleCollider2D))]
    public class WorldDrop : MonoBehaviour
    {
        [Header("Visual Size")]
        [SerializeField, Min(1f)] private float targetPixelWidth = 32f;
        [SerializeField, Min(1f)] private float referencePixelsPerUnit = 100f;

        private LootResultEntry _entry;
        private SpriteRenderer  _spriteRenderer;
        private Transform       _visualTransform;
        private Vector3         _initialVisualScale;
        private float           _collectionCooldown;

        public LootResultEntry Entry          => _entry;
        public bool            CanBeCollected => _collectionCooldown <= 0f;

        void Awake()
        {
            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (_spriteRenderer == null)
            {
                Debug.LogWarning("[WorldDrop] No SpriteRenderer found in children — icon will never display.", this);
                return;
            }

            if (_spriteRenderer.transform == transform)
            {
                Debug.LogWarning("[WorldDrop] SpriteRenderer must be on a visual child; root scaling is disabled.", this);
                return;
            }

            _visualTransform    = _spriteRenderer.transform;
            _initialVisualScale = _visualTransform.localScale;
        }

        void Update()
        {
            if (_collectionCooldown > 0f)
                _collectionCooldown -= Time.deltaTime;
        }

        public void Setup(LootResultEntry entry, Sprite icon)
        {
            _entry              = entry;
            _collectionCooldown = 0f;

            if (_spriteRenderer == null)
            {
                Debug.LogWarning("[WorldDrop] Setup: _spriteRenderer is null — cannot display icon.", this);
                RestoreVisualScale();
                return;
            }

            if (icon == null)
                Debug.LogWarning($"[WorldDrop] Setup: icon is null for '{entry?.ItemId ?? entry?.CurrencyType.ToString()}' — sprite will be blank.", this);

            _spriteRenderer.sprite  = icon;
            _spriteRenderer.enabled = true;
            ApplyVisualScale(icon);
        }

        private void ApplyVisualScale(Sprite sprite)
        {
            if (_visualTransform == null || sprite == null)
            {
                RestoreVisualScale();
                return;
            }

            float baselineWidth =
                sprite.bounds.size.x * Mathf.Abs(_initialVisualScale.x);
            if (baselineWidth <= Mathf.Epsilon || referencePixelsPerUnit <= 0f)
            {
                RestoreVisualScale();
                return;
            }

            float targetWorldWidth = targetPixelWidth / referencePixelsPerUnit;
            float scaleMultiplier  = targetWorldWidth / baselineWidth;
            _visualTransform.localScale = _initialVisualScale * scaleMultiplier;
        }

        private void RestoreVisualScale()
        {
            if (_visualTransform != null)
                _visualTransform.localScale = _initialVisualScale;
        }

        // Called by DropManager when collection fails (e.g. inventory full).
        // Suppresses re-attempts for a short window to avoid per-frame spam.
        public void OnCollectionFailed()
        {
            _collectionCooldown = 0.5f;
        }
    }
}
