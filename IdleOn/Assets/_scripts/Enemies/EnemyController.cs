using System;
using UnityEngine;
using IdleOn.Core;
using IdleOn.Characters;
using IdleOn.UI;
using IdleOn.Loot;
using IdleOn.World;

namespace IdleOn.Enemies
{
    public enum EnemyState { Patrol, Combat, Dead }

    [RequireComponent(typeof(HealthComponent))]
    public class EnemyController : MonoBehaviour
    {
        [Header("Definition")]
        [SerializeField] private EnemyDefinition definition;

        [Header("Patrol")]
        [SerializeField] private float patrolLeftX  = 0f;
        [SerializeField] private float patrolRightX = 0f;
        [SerializeField] private float patrolSpeed  = 1.5f;

        [Header("Combat")]
        [SerializeField] private float attackRange      = 1.2f;
        [SerializeField] private float attackCooldown   = 2f;
        [SerializeField] private float attackDamageMin  = 5f;
        [SerializeField] private float attackDamageMax  = 10f;
        [SerializeField] private float combatForgetTime = 4f;

        private HealthComponent _health;
        private HealthComponent _playerHealth;
        private Transform       _playerTransform;
        private SpriteRenderer  _spriteRenderer;

        private EnemyState _state;
        private float      _patrolDir;
        private float      _attackTimer;
        private float      _timeSinceLastHit;

        public bool           IsAlive    => _health != null && _health.IsAlive;
        public EnemyState     State      => _state;
        public EnemyDefinition Definition => definition;

        public event Action<EnemyController> OnKilled;

        void Awake()
        {
            _health = GetComponent<HealthComponent>();

            var spriteChild = transform.Find("Sprite");
            if (spriteChild != null)
                _spriteRenderer = spriteChild.GetComponent<SpriteRenderer>();
        }

        void Start()
        {
            var player = PlayerStats.Instance;
            if (player != null)
            {
                _playerTransform = player.transform;
                _playerHealth    = player.GetComponent<HealthComponent>();
            }

            if (patrolLeftX >= patrolRightX)
            {
                patrolLeftX  = transform.position.x - 2f;
                patrolRightX = transform.position.x + 2f;
            }
        }

        void OnEnable()
        {
            if (_health == null) return;
            _health.Initialize(definition != null ? definition.MaxHP : 1f);
            _health.OnDied += HandleDied;

            _state            = EnemyState.Patrol;
            _patrolDir        = 1f;
            _attackTimer      = attackCooldown;
            _timeSinceLastHit = 0f;
        }

        void OnDisable()
        {
            if (_health != null)
                _health.OnDied -= HandleDied;
        }

        void Update()
        {
            switch (_state)
            {
                case EnemyState.Patrol: UpdatePatrol(); break;
                case EnemyState.Combat: UpdateCombat(); break;
                case EnemyState.Dead:                   break;
            }
        }

        // ── Patrol ──────────────────────────────────────────────────────────

        private void UpdatePatrol()
        {
            float newX = transform.position.x + _patrolDir * patrolSpeed * Time.deltaTime;
            transform.position = new Vector3(newX, transform.position.y, transform.position.z);

            FlipSprite(_patrolDir);

            if (_patrolDir > 0f && transform.position.x >= patrolRightX)
                _patrolDir = -1f;
            else if (_patrolDir < 0f && transform.position.x <= patrolLeftX)
                _patrolDir = 1f;
        }

        // ── Combat ───────────────────────────────────────────────────────────

        private void UpdateCombat()
        {
            if (_playerTransform == null) { _state = EnemyState.Patrol; return; }

            _attackTimer      -= Time.deltaTime;
            _timeSinceLastHit += Time.deltaTime;

            if (_timeSinceLastHit >= combatForgetTime)
            {
                _state = EnemyState.Patrol;
                return;
            }

            float dist = Vector2.Distance(transform.position, _playerTransform.position);

            if (dist <= attackRange)
                TryAttackPlayer();
            else
                MoveTowardPlayer();
        }

        private void TryAttackPlayer()
        {
            if (_attackTimer > 0f) return;

            _attackTimer = attackCooldown;
            float dmg = UnityEngine.Random.Range(attackDamageMin, attackDamageMax);
            _playerHealth?.TakeDamage(dmg);
            FloatTextManager.Show(
                Mathf.RoundToInt(dmg).ToString(),
                _playerTransform.position + Vector3.up * 0.8f,
                FloatTextType.Physical);
        }

        private void MoveTowardPlayer()
        {
            float dir  = _playerTransform.position.x > transform.position.x ? 1f : -1f;
            float newX = transform.position.x + dir * patrolSpeed * Time.deltaTime;
            transform.position = new Vector3(newX, transform.position.y, transform.position.z);
            FlipSprite(dir);
        }

        // ── Shared ───────────────────────────────────────────────────────────

        private void FlipSprite(float dir)
        {
            if (_spriteRenderer != null)
                _spriteRenderer.flipX = dir < 0f;
        }

        public void TakeDamage(float amount, bool isCritical = false)
        {
            if (!IsAlive) return;

            _health?.TakeDamage(amount);
            FloatTextManager.Show(
                Mathf.RoundToInt(amount).ToString(),
                transform.position + Vector3.up * 0.8f,
                FloatTextType.Physical,
                isCritical);

            if (_state != EnemyState.Dead)
            {
                _state            = EnemyState.Combat;
                _timeSinceLastHit = 0f;
            }
        }

        private void HandleDied()
        {
            _state = EnemyState.Dead;

            float xp = definition != null ? definition.XPReward : 0f;
            GameEvents.RaiseEnemyKilled(definition != null ? definition.EnemyId : string.Empty, xp);

            // ── Loot debug ───────────────────────────────────────────────────
            if (definition == null)
            {
                Debug.LogWarning($"[EnemyController] {name}: definition is null — no loot.", this);
            }
            else if (definition.LootTable == null)
            {
                Debug.LogWarning($"[EnemyController] {name}: definition '{definition.EnemyId}' has no LootTable assigned.", this);
            }
            else if (DropManager.Instance == null)
            {
                Debug.LogWarning($"[EnemyController] {name}: DropManager.Instance is null — is DropManager in the scene?");
            }
            else
            {
                LootResult result = LootEvaluator.Evaluate(definition.LootTable);
                Debug.Log($"[EnemyController] {name}: LootResult has {result.Entries.Count} entries (IsEmpty={result.IsEmpty}).");
                DropManager.Instance.Spawn(result, transform.position);
            }
            // ─────────────────────────────────────────────────────────────────

            OnKilled?.Invoke(this);
            gameObject.SetActive(false);
        }
    }
}
