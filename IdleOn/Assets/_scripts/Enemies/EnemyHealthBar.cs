using UnityEngine;
using IdleOn.Characters;

namespace IdleOn.Enemies
{
    // Reusable always-visible world-space HP bar. SpriteRenderer-based (no uGUI Canvas) and
    // wired entirely per-prefab — never spawned at runtime. Add alongside HealthComponent on
    // any enemy root; sizes itself from widthSource and reads HealthComponent events only.
    public class EnemyHealthBar : MonoBehaviour
    {
        [Header("References (auto-found on this GameObject if left empty)")]
        [SerializeField] private HealthComponent health;
        [Tooltip("Used to size the bar to the enemy's hitbox width. Falls back to fallbackWidth if unset.")]
        [SerializeField] private Collider2D widthSource;

        [Header("Bar Visuals")]
        [SerializeField] private Transform barRoot;
        [SerializeField] private SpriteRenderer backgroundRenderer;
        [SerializeField] private SpriteRenderer fillRenderer;

        [Header("Layout")]
        [SerializeField] private float heightAboveCollider = 0.25f;
        [SerializeField] private float barHeight = 0.08f;
        [SerializeField] private float fallbackWidth = 0.5f;

        private float _barWidth;

        void Awake()
        {
            if (health == null)      health      = GetComponent<HealthComponent>();
            if (widthSource == null) widthSource  = GetComponent<Collider2D>();

            _barWidth = widthSource != null ? widthSource.bounds.size.x : fallbackWidth;

            ApplySize(backgroundRenderer, _barWidth, barHeight);
            PositionAboveHead();
        }

        void OnEnable()
        {
            if (health == null) return;

            health.OnHPChanged += HandleHPChanged;
            health.OnDied      += HandleDied;

            if (barRoot != null) barRoot.gameObject.SetActive(true);
            HandleHPChanged(health.CurrentHP, health.MaxHP);
        }

        void OnDisable()
        {
            if (health == null) return;

            health.OnHPChanged -= HandleHPChanged;
            health.OnDied      -= HandleDied;
        }

        private void PositionAboveHead()
        {
            if (barRoot == null) return;

            float topY = widthSource != null
                ? widthSource.bounds.max.y - transform.position.y
                : 1f;

            Vector3 pos = barRoot.localPosition;
            pos.y = topY + heightAboveCollider;
            barRoot.localPosition = pos;
        }

        private void HandleHPChanged(float current, float max)
        {
            float pct = max > 0f ? Mathf.Clamp01(current / max) : 0f;
            SetFillPercent(pct);
        }

        private void HandleDied()
        {
            SetFillPercent(0f);
            if (barRoot != null) barRoot.gameObject.SetActive(false);
        }

        private void SetFillPercent(float pct)
        {
            if (fillRenderer == null) return;

            float width = _barWidth * pct;
            ApplySize(fillRenderer, width, barHeight);

            // Anchor the left edge; only the right edge moves as HP depletes.
            float leftEdgeX = -_barWidth * 0.5f;
            Vector3 pos = fillRenderer.transform.localPosition;
            pos.x = leftEdgeX + width * 0.5f;
            fillRenderer.transform.localPosition = pos;
        }

        private static void ApplySize(SpriteRenderer renderer, float targetWidth, float targetHeight)
        {
            if (renderer == null || renderer.sprite == null) return;

            Vector2 native = renderer.sprite.bounds.size;
            Vector3 scale  = renderer.transform.localScale;
            scale.x = native.x > 0f ? targetWidth  / native.x : scale.x;
            scale.y = native.y > 0f ? targetHeight / native.y : scale.y;
            renderer.transform.localScale = scale;
        }
    }
}
