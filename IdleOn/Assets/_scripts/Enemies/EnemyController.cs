using System;
using System.Collections;
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

        [Header("Death")]
        [Tooltip("Seconds the dead enemy stays visible (to play its Dead clip) before being pooled.")]
        [SerializeField] private float deathDisableDelay = 0.5f;

        [Header("Debug")]
        [SerializeField] private bool debugLifecycle;
        [SerializeField] private bool debugMovement;

        private HealthComponent _health;
        private HealthComponent _playerHealth;
        private Transform       _playerTransform;
        private Rigidbody2D     _rb;

        private EnemyState _state;
        private float      _patrolDir;
        private float      _attackTimer;
        private float      _timeSinceLastHit;
        private float      _desiredVelocityX;

        public bool           IsAlive    => _health != null && _health.IsAlive;
        public bool           IsMoving   => _state != EnemyState.Dead &&
                                            Mathf.Abs(_desiredVelocityX) > 0.0001f;
        public EnemyState     State      => _state;
        public EnemyDefinition Definition => definition;
        public Transform      PlayerTarget => _playerTransform;

        public event Action<EnemyController> OnKilled;

        void Awake()
        {
            _health = GetComponent<HealthComponent>();
            _rb     = GetComponent<Rigidbody2D>();
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

            // Phase 1: keep patrol inside the single lane so the enemy never walks off its bounds.
            var lane = GroundLane.Current;
            if (lane != null)
            {
                patrolLeftX  = Mathf.Max(patrolLeftX,  lane.MinX);
                patrolRightX = Mathf.Min(patrolRightX, lane.MaxX);
            }
        }

        void OnEnable()
        {
            // Re-enable physics in case this is a pooled respawn that was frozen on death.
            bool simulatedBefore = _rb != null && _rb.simulated;
            if (_rb != null) _rb.simulated = true;
            SetDesiredVelocityX(0f, "OnEnable");

            if (_health == null) return;
            _health.Initialize(definition != null ? definition.MaxHP : 1f);
            _health.OnDied += HandleDied;

            _state            = EnemyState.Patrol;
            _patrolDir        = 1f;
            _attackTimer      = attackCooldown;
            _timeSinceLastHit = 0f;

            LogLifecycle($"OnEnable id={EnemyIdForLog()} stateReset={_state} pos={transform.position} rbSimBefore={simulatedBefore} rbSimAfter={(_rb != null && _rb.simulated)}");
        }

        void OnDisable()
        {
            SetDesiredVelocityX(0f, "OnDisable");
            if (_health != null)
                _health.OnDied -= HandleDied;
        }

        void FixedUpdate()
        {
            if (_rb == null || !_rb.simulated) return;

            // Phase 1: Kinematic, no-gravity, single-lane movement. Drive X only and force the
            // feet (root) Y onto the lane. No velocity writes — MovePosition is the only mover.
            var   lane = GroundLane.Current;
            float vx   = _state == EnemyState.Dead ? 0f : _desiredVelocityX;
            Vector2 p  = _rb.position;
            p.x += vx * Time.fixedDeltaTime;
            if (lane != null)
            {
                p.x = lane.ClampX(p.x);
                p.y = lane.GroundY;
            }
            _rb.MovePosition(p);
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
            SetDesiredVelocityX(_patrolDir * patrolSpeed, "UpdatePatrol");

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
            {
                SetDesiredVelocityX(0f, "attack range");
                TryAttackPlayer();
            }
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
            SetDesiredVelocityX(dir * patrolSpeed, "MoveTowardPlayer");
        }

        // ── Shared ───────────────────────────────────────────────────────────

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

        public void TakeMagicDamage(float amount, bool isCritical = false)
        {
            if (!IsAlive) return;

            _health?.TakeDamage(amount);
            FloatTextManager.Show(
                Mathf.RoundToInt(amount).ToString(),
                transform.position + Vector3.up * 0.8f,
                FloatTextType.Magic,
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
            bool simulatedBefore = _rb != null && _rb.simulated;

            // Freeze the dead body so it stops colliding with / pushing the player while its
            // death clip plays out (re-enabled on pooled respawn in OnEnable). Loot/XP/OnKilled
            // timing below is unchanged.
            SetDesiredVelocityX(0f, "HandleDied");
            if (_rb != null) _rb.simulated = false;
            LogLifecycle($"HandleDied id={EnemyIdForLog()} state={_state} pos={transform.position} rbSimBefore={simulatedBefore} rbSimAfter={(_rb != null && _rb.simulated)} deathDelay={deathDisableDelay:0.###}");

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
            StartCoroutine(DisableAfterDeath());
        }

        // Keep the GameObject active briefly so the Dead clip can play before it returns to
        // the pool. Loot, XP and OnKilled already fired above — this only delays deactivation.
        private IEnumerator DisableAfterDeath()
        {
            if (deathDisableDelay > 0f)
                yield return new WaitForSeconds(deathDisableDelay);

            gameObject.SetActive(false);
        }

        private void LogLifecycle(string message)
        {
            if (debugLifecycle)
                Debug.Log($"[EnemyController] {name}: {message}", this);
        }

        private string EnemyIdForLog()
        {
            return definition != null ? definition.EnemyId : "null";
        }

        private void SetDesiredVelocityX(float velocityX, string context)
        {
            if (Mathf.Approximately(_desiredVelocityX, velocityX)) return;

            float oldVelocityX = _desiredVelocityX;
            _desiredVelocityX = velocityX;

            if (debugMovement)
            {
                Vector2 rbVelocity = _rb != null ? _rb.linearVelocity : Vector2.zero;
                Debug.Log($"[EnemyController] {name}: {context} desiredVelocityX {oldVelocityX:0.###}->{velocityX:0.###} rbVel={rbVelocity} state={_state}", this);
            }
        }
    }
}
