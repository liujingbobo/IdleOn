using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using IdleOn.Core;
using IdleOn.Characters;
using IdleOn.Enemies;
using IdleOn.Skills;
using IdleOn.Talents;
using IdleOn.World;

namespace IdleOn.Combat
{
    public enum CombatState { Idle, Seeking, Moving, Attacking, ManualMove, ManualAttack }

    [RequireComponent(typeof(PlayerStats))]
    public class PlayerCombatController : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private float attackRange            = 1.5f;
        [SerializeField] private float attackCooldown         = 1f;
        [SerializeField] private bool  startAutoCombatOnPlay  = true;

        [Header("References")]
        [SerializeField] private EnemySpawner enemySpawner;

        [Header("Drop Pickup")]
        [SerializeField] private LayerMask dropLayerMask;

        [Header("Fireball")]
        [SerializeField] private FireballProjectile fireballProjectilePrefab;
        [SerializeField] private float fireballSpawnOffset = 0.5f;
        [Tooltip("Projectile spawn height above lane.groundY (tiles). Keep <= 1 so it still hits 1-tile-high enemies.")]
        [SerializeField] private float projectileHeight     = 0.6f;

        [Header("Ground Detection")]
        [SerializeField] private LayerMask groundLayerMask;
        [SerializeField] private float     maxGroundSearchDistance = 2.5f;

        [Header("Debug")]
        [SerializeField] private bool debugCombatState;
        [SerializeField] private bool debugTargets;
        [SerializeField] private bool debugMovement;
        [SerializeField] private bool debugGroundResolve;

        private PlayerStats    _stats;
        private Rigidbody2D    _rb;
        private SpriteRenderer _spriteRenderer;
        private EnemyController _currentTarget;
        private float          _attackTimer;
        private readonly Dictionary<string, float> _skillReadyTimes = new Dictionary<string, float>();
        private Vector2        _manualMoveTarget;
        private EnemyController _manualAttackTarget;
        private bool           _resumeAutoCombat;
        private float          _desiredVelocityX;

        public bool        IsAutoCombatActive { get; private set; }
        public CombatState State              { get; private set; } = CombatState.Idle;
        public Transform FacingTarget
        {
            get
            {
                if (State == CombatState.ManualAttack && IsValidTarget(_manualAttackTarget))
                    return _manualAttackTarget.transform;
                if (State == CombatState.Attacking && IsValidTarget(_currentTarget))
                    return _currentTarget.transform;
                return null;
            }
        }

        void Awake()
        {
            _stats = GetComponent<PlayerStats>();
            _rb    = GetComponent<Rigidbody2D>();

            var sprite = transform.Find("Sprite");
            if (sprite != null) _spriteRenderer = sprite.GetComponent<SpriteRenderer>();
        }

        void Start()
        {
            if (startAutoCombatOnPlay)
                SetAutoCombat(true);
        }

        public void SetAutoCombat(bool active)
        {
            IsAutoCombatActive = active;
            if (!active)
            {
                SetState(CombatState.Idle, "auto combat off");
                SetDesiredVelocityX(0f, "auto combat off");
                _currentTarget = null;
                LogTarget("Current target cleared by auto combat off", null);
            }
            GameEvents.RaiseAutoCombatChanged(active);
        }

        // Read-only cooldown progress for UI: 0 = just cast, 1 = ready. Never written to by UI.
        public float GetSkillCooldownProgress01(string skillId)
        {
            var skill = GameDatabase.Instance?.Skills?.GetSkill(skillId);
            if (skill == null) return 1f;
            if (!_skillReadyTimes.TryGetValue(skill.SkillId, out float readyAt)) return 1f;
            float remaining = readyAt - Time.time;
            if (remaining <= 0f) return 1f;
            return Mathf.Clamp01(1f - remaining / Mathf.Max(0.0001f, skill.Cooldown));
        }

        public bool TryCastSkill(string skillId)
        {
            if (string.IsNullOrEmpty(skillId)) return false;

            var skill = GameDatabase.Instance?.Skills?.GetSkill(skillId);
            if (skill == null)
            {
                Debug.LogWarning($"[PlayerCombatController] Unknown skill id '{skillId}'.");
                return false;
            }

            if (skill.SkillId != "fireball")
            {
                Debug.LogWarning($"[PlayerCombatController] Skill '{skill.SkillId}' is not implemented.");
                return false;
            }

            if (!IsSkillUnlocked(skill))
            {
                Debug.Log($"[PlayerCombatController] Fireball is locked.");
                return false;
            }

            if (_skillReadyTimes.TryGetValue(skill.SkillId, out float readyAt) && Time.time < readyAt)
            {
                Debug.Log($"[PlayerCombatController] Fireball is on cooldown.");
                return false;
            }

            if (!_stats.SpendMP(skill.MpCost))
            {
                Debug.Log("[PlayerCombatController] Not enough MP to cast Fireball.");
                return false;
            }

            _skillReadyTimes[skill.SkillId] = Time.time + Mathf.Max(0f, skill.Cooldown);

            float damage = skill.BaseDamage + (TalentSystem.Instance?.GetFireballDamageBonus() ?? 0f);
            SpawnFireballProjectile(damage);
            Debug.Log($"[PlayerCombatController] Cast Fireball for {Mathf.RoundToInt(damage)} damage.");
            return true;
        }

        private void SpawnFireballProjectile(float damage)
        {
            if (fireballProjectilePrefab == null)
            {
                Debug.LogWarning("[PlayerCombatController] fireballProjectilePrefab not assigned.");
                return;
            }

            bool facingLeft = _spriteRenderer != null && _spriteRenderer.flipX;
            Vector2 direction = facingLeft ? Vector2.left : Vector2.right;

            // Phase 1: spawn height is lane-based (groundY + projectileHeight), not pivot-based,
            // so it stays horizontal on the lane and still hits 1-tile-high enemies after the
            // feet-root change. X keeps the forward muzzle offset from the player.
            var   lane    = GroundLane.Current;
            float spawnX  = transform.position.x + direction.x * fireballSpawnOffset;
            float spawnY  = lane != null ? lane.GroundY + projectileHeight : transform.position.y;
            Vector3 spawnPos = new Vector3(spawnX, spawnY, 0f);

            FireballProjectile projectile = Instantiate(fireballProjectilePrefab, spawnPos, Quaternion.identity);
            projectile.Init(direction, damage);
        }

        void Update()
        {
            _attackTimer -= Time.deltaTime;

            // Drop pickup takes priority over all other LMB input
            if (Input.GetMouseButton(0) && TryPickUpDrop())
            {
                SetDesiredVelocityX(0f, "drop pickup");
                return;
            }

            if (Input.GetMouseButtonDown(0))
                HandleClick();

            switch (State)
            {
                case CombatState.Idle:
                case CombatState.Seeking:
                    if (IsAutoCombatActive) SeekTarget();
                    break;
                case CombatState.Moving:
                    MoveToTarget();
                    break;
                case CombatState.Attacking:
                    TryAttack();
                    break;
                case CombatState.ManualMove:
                    UpdateManualMove();
                    break;
                case CombatState.ManualAttack:
                    UpdateManualAttack();
                    break;
            }
        }

        void FixedUpdate()
        {
            if (_rb == null) return;

            // Phase 1: Kinematic, no-gravity, single-lane movement. Drive X only and force the
            // feet (root) Y onto the lane. No velocity writes — MovePosition is the only mover.
            var lane = GroundLane.Current;
            Vector2 p = _rb.position;
            p.x += _desiredVelocityX * Time.fixedDeltaTime;
            if (lane != null)
            {
                p.x = lane.ClampX(p.x);
                p.y = lane.GroundY;
            }
            _rb.MovePosition(p);
        }

        // ── Drop pickup ──────────────────────────────────────────────────────

        private bool TryPickUpDrop()
        {
            if (Camera.main == null || DropManager.Instance == null) return false;

            Vector2 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            var hit = Physics2D.OverlapPoint(worldPos, dropLayerMask);
            if (hit == null) return false;

            var drop = hit.GetComponent<WorldDrop>();
            if (drop == null) return false;

            DropManager.Instance.Collect(drop);
            return true;
        }

        // ── Click handling ───────────────────────────────────────────────────

        private void HandleClick()
        {
            if (Camera.main == null) return;
            Vector2 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            foreach (var col in Physics2D.OverlapPointAll(worldPos))
            {
                var enemy = col.GetComponent<EnemyController>();
                if (IsValidTarget(enemy))
                {
                    _resumeAutoCombat   = IsAutoCombatActive;
                    _manualAttackTarget = enemy;
                    LogTarget("Manual target assigned from click", enemy);
                    SetState(CombatState.ManualAttack, "clicked enemy");
                    return;
                }
                LogTarget("Clicked collider rejected as target", enemy);
            }

            // Phase 1: click resolves to an X on the current lane only — clicked Y is ignored,
            // X is clamped to the lane bounds. (Old ground raycast resolve kept below, unused.)
            var lane = GroundLane.Current;
            if (lane != null)
            {
                _resumeAutoCombat = IsAutoCombatActive;
                float targetX     = lane.ClampX(worldPos.x);
                _manualMoveTarget = new Vector2(targetX, lane.GroundY);
                SetState(CombatState.ManualMove, "clicked lane");
            }
        }

        private bool TryResolveMoveTarget(Vector2 clickWorldPos, out Vector2 targetWorldPos)
        {
            var hit = Physics2D.OverlapPoint(clickWorldPos, groundLayerMask);
            LogGround($"click={clickWorldPos} directHit={(hit != null)} collider={(hit != null ? hit.name : "none")}");
            if (hit != null)
            {
                targetWorldPos = new Vector2(clickWorldPos.x, hit.bounds.max.y);
                LogGround($"resolved direct target={targetWorldPos}");
                return true;
            }

            var hit2D = Physics2D.Raycast(clickWorldPos, Vector2.down, maxGroundSearchDistance, groundLayerMask);
            LogGround($"raycastHit={(hit2D.collider != null)} collider={(hit2D.collider != null ? hit2D.collider.name : "none")}");
            if (hit2D.collider != null)
            {
                targetWorldPos = new Vector2(clickWorldPos.x, hit2D.point.y);
                LogGround($"resolved raycast target={targetWorldPos}");
                return true;
            }

            targetWorldPos = default;
            LogGround("resolve failed: no direct ground hit and no downward raycast hit");
            return false;
        }

        // ── States ───────────────────────────────────────────────────────────

        private void UpdateManualMove()
        {
            float distX = Mathf.Abs(transform.position.x - _manualMoveTarget.x);
            if (distX < 0.05f)
            {
                ResumeAutoCombatOrIdle("manual move reached target");
                return;
            }
            float direction = Mathf.Sign(_manualMoveTarget.x - transform.position.x);
            SetDesiredVelocityX(direction * _stats.FinalStats.MoveSpeed, "UpdateManualMove");
        }

        private void UpdateManualAttack()
        {
            if (!IsValidTarget(_manualAttackTarget))
            {
                LogTarget("Manual target rejected/cleared in UpdateManualAttack", _manualAttackTarget);
                ResumeAutoCombatOrIdle("manual target invalid");
                return;
            }

            float dist = Vector2.Distance(transform.position, _manualAttackTarget.transform.position);
            LogTargetFrame("UpdateManualAttack", _manualAttackTarget, dist);
            if (dist > attackRange)
            {
                float direction = Mathf.Sign(_manualAttackTarget.transform.position.x - transform.position.x);
                SetDesiredVelocityX(direction * _stats.FinalStats.MoveSpeed, "UpdateManualAttack");
                return;
            }

            SetDesiredVelocityX(0f, "manual attack in range");
            float damage = Random.Range(_stats.FinalStats.ATKMin, _stats.FinalStats.ATKMax);
            _manualAttackTarget.TakeDamage(damage);
            _attackTimer        = attackCooldown;
            LogTarget("Manual target cleared after attack", _manualAttackTarget);
            _manualAttackTarget = null;
            SetState(_resumeAutoCombat ? CombatState.Seeking : CombatState.Idle, "manual attack complete");
        }

        private void SeekTarget()
        {
            if (enemySpawner == null) return;
            var previous = _currentTarget;
            _currentTarget = enemySpawner.GetNearestEnemy(transform.position);
            if (!IsValidTarget(_currentTarget))
            {
                LogTarget("Current target rejected by SeekTarget", _currentTarget);
                _currentTarget = null;
            }
            if (previous != _currentTarget)
                LogTarget(previous == null ? "Current target assigned by SeekTarget" : "Current target switched by SeekTarget", _currentTarget);
            SetState(_currentTarget != null ? CombatState.Moving : CombatState.Idle, "seek target result");
        }

        // A target is only valid while it is non-null, active, alive, and not in its death state.
        // Prevents the player from chasing/attacking/facing a dead enemy during its death-clip delay.
        public static bool IsValidTarget(EnemyController enemy)
        {
            return enemy != null
                && enemy.gameObject.activeInHierarchy
                && enemy.IsAlive
                && enemy.State != EnemyState.Dead;
        }

        private bool IsSkillUnlocked(SkillDefinition skill)
        {
            if (skill == null) return false;
            if (string.IsNullOrEmpty(skill.RequiredTalentId)) return true;

            int level = TalentSystem.Instance != null ? TalentSystem.Instance.GetLevel(skill.RequiredTalentId) : 0;
            return level >= skill.RequiredTalentLevel;
        }

        private void MoveToTarget()
        {
            if (!IsValidTarget(_currentTarget))
            {
                LogTarget("Current target rejected/cleared in MoveToTarget", _currentTarget);
                ResumeAutoCombatOrIdle("move target invalid");
                return;
            }

            Vector2 myPos     = transform.position;
            Vector2 targetPos = _currentTarget.transform.position;
            float   dist      = Vector2.Distance(myPos, targetPos);
            LogTargetFrame("MoveToTarget", _currentTarget, dist);

            if (dist <= attackRange)
            {
                SetDesiredVelocityX(0f, "target in range");
                SetState(CombatState.Attacking, "target in range");
                return;
            }

            float direction = Mathf.Sign(targetPos.x - myPos.x);
            SetDesiredVelocityX(direction * _stats.FinalStats.MoveSpeed, "MoveToTarget");
        }

        private void TryAttack()
        {
            if (!IsValidTarget(_currentTarget))
            {
                LogTarget("Current target rejected/cleared in TryAttack", _currentTarget);
                ResumeAutoCombatOrIdle("attack target invalid");
                return;
            }

            Vector2 myPos     = transform.position;
            Vector2 targetPos = _currentTarget.transform.position;

            float dist = Vector2.Distance(myPos, targetPos);
            if (dist > attackRange) { SetState(CombatState.Moving, "target moved out of range"); return; }

            if (_attackTimer > 0f) return;
            _attackTimer = attackCooldown;
            SetDesiredVelocityX(0f, "TryAttack succeeded");

            float damage = Random.Range(_stats.FinalStats.ATKMin, _stats.FinalStats.ATKMax);
            _currentTarget.TakeDamage(damage);
            LogTarget($"TryAttack succeeded damage={damage:0.##}", _currentTarget);
            if (!IsValidTarget(_currentTarget))
                ResumeAutoCombatOrIdle("attack killed target");
        }

        private void ResumeAutoCombatOrIdle(string reason)
        {
            SetDesiredVelocityX(0f, reason);

            if (!IsValidTarget(_currentTarget))
            {
                LogTarget("Current target cleared by resume", _currentTarget);
                _currentTarget = null;
            }

            if (!IsValidTarget(_manualAttackTarget))
            {
                LogTarget("Manual target cleared by resume", _manualAttackTarget);
                _manualAttackTarget = null;
            }

            SetState(IsAutoCombatActive ? CombatState.Seeking : CombatState.Idle, reason);
        }

        private void SetState(CombatState nextState, string reason)
        {
            if (State == nextState) return;
            LogState($"State {State} -> {nextState} ({reason})");
            State = nextState;
        }

        private void LogState(string message)
        {
            if (debugCombatState)
                Debug.Log($"[PlayerCombatController] {message}", this);
        }

        private void LogTarget(string message, EnemyController enemy)
        {
            if (!debugTargets) return;
            Debug.Log($"[PlayerCombatController] {message}: {DescribeTarget(enemy)}", this);
        }

        private void LogTargetFrame(string context, EnemyController enemy, float dist)
        {
            if (!debugTargets) return;
            Vector3 targetPos = enemy != null ? enemy.transform.position : Vector3.zero;
            Debug.Log($"[PlayerCombatController] {context}: playerPos={transform.position} targetPos={targetPos} dist={dist:0.###} range={attackRange:0.###} {DescribeTarget(enemy)}", this);
        }

        private void LogGround(string message)
        {
            if (debugGroundResolve)
                Debug.Log($"[PlayerCombatController] TryResolveMoveTarget {message}", this);
        }

        private void SetDesiredVelocityX(float velocityX, string context)
        {
            if (Mathf.Approximately(_desiredVelocityX, velocityX)) return;

            float oldVelocityX = _desiredVelocityX;
            _desiredVelocityX = velocityX;

            if (debugMovement)
            {
                Vector2 rbVelocity = _rb != null ? _rb.linearVelocity : Vector2.zero;
                Debug.Log($"[PlayerCombatController] {context} desiredVelocityX {oldVelocityX:0.###}->{velocityX:0.###} rbVel={rbVelocity} state={State}", this);
            }
        }

        private static string DescribeTarget(EnemyController enemy)
        {
            if (enemy == null) return "target=null";
            return $"target={enemy.name} alive={enemy.IsAlive} state={enemy.State} active={enemy.gameObject.activeInHierarchy} pos={enemy.transform.position}";
        }
    }
}
