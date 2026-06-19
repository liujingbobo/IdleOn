using UnityEngine;
using IdleOn.Enemies;

namespace IdleOn.Combat
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class FireballProjectile : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float speed         = 8f;
        [SerializeField] private float maxDistance    = 7f;
        [SerializeField] private float maxLifetime     = 2f;

        [Header("Spawn Scale")]
        [SerializeField] private float startScale      = 0.25f;
        [SerializeField] private float fullScale       = 1f;
        [SerializeField] private float scaleUpDuration  = 0.08f;

        [Header("Hit")]
        [SerializeField] private float hitAnimDuration = 0.3f;

        [Header("References")]
        [SerializeField] private Animator       animator;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Collider2D     hitCollider;

        private static readonly int HitHash = Animator.StringToHash("Hit");

        private Vector2 _direction;
        private float   _damage;
        private Vector3 _spawnPos;
        private float   _spawnTime;
        private bool    _hasHit;

        void Awake()
        {
            if (animator == null)       animator       = GetComponent<Animator>();
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            if (hitCollider == null)    hitCollider    = GetComponent<Collider2D>();
        }

        public void Init(Vector2 direction, float damage)
        {
            _direction = direction.normalized;
            _damage    = damage;
            _spawnPos  = transform.position;
            _spawnTime = Time.time;

            transform.localScale = Vector3.one * startScale;

            if (spriteRenderer != null)
                spriteRenderer.flipX = _direction.x < 0f;
        }

        void Update()
        {
            if (_hasHit) return;

            float age = Time.time - _spawnTime;

            if (age < scaleUpDuration)
            {
                float t = age / scaleUpDuration;
                float s = Mathf.Lerp(startScale, fullScale, t);
                transform.localScale = Vector3.one * s;
            }
            else if (transform.localScale.x != fullScale)
            {
                transform.localScale = Vector3.one * fullScale;
            }

            transform.position += (Vector3)(_direction * speed * Time.deltaTime);

            float traveled = Vector3.Distance(_spawnPos, transform.position);
            if (traveled >= maxDistance || age >= maxLifetime)
            {
                Destroy(gameObject);
            }
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (_hasHit) return;

            var enemy = other.GetComponent<EnemyController>() ?? other.GetComponentInParent<EnemyController>();
            if (enemy == null) return;
            if (!PlayerCombatController.IsValidTarget(enemy)) return;

            _hasHit = true;

            enemy.TakeMagicDamage(_damage);

            if (hitCollider != null) hitCollider.enabled = false;

            if (animator != null) animator.SetTrigger(HitHash);

            Destroy(gameObject, hitAnimDuration);
        }
    }
}
