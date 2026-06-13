using UnityEngine;
using IdleOn.Core;
using IdleOn.Characters;
using IdleOn.Enemies;
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

        private PlayerStats    _stats;
        private EnemyController _currentTarget;
        private float          _attackTimer;
        private Vector2        _manualMoveTarget;
        private EnemyController _manualAttackTarget;
        private bool           _resumeAutoCombat;

        public bool        IsAutoCombatActive { get; private set; }
        public CombatState State              { get; private set; } = CombatState.Idle;

        void Awake()
        {
            _stats = GetComponent<PlayerStats>();
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
                State          = CombatState.Idle;
                _currentTarget = null;
            }
            GameEvents.RaiseAutoCombatChanged(active);
        }

        void Update()
        {
            _attackTimer -= Time.deltaTime;

            // Drop pickup takes priority over all other LMB input
            if (Input.GetMouseButton(0) && TryPickUpDrop()) return;

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

            var hit = Physics2D.OverlapPoint(worldPos);
            if (hit != null)
            {
                var enemy = hit.GetComponent<EnemyController>();
                if (enemy != null && enemy.IsAlive)
                {
                    _resumeAutoCombat   = IsAutoCombatActive;
                    _manualAttackTarget = enemy;
                    State               = CombatState.ManualAttack;
                    return;
                }
            }

            _resumeAutoCombat = IsAutoCombatActive;
            _manualMoveTarget = new Vector2(worldPos.x, transform.position.y);
            State             = CombatState.ManualMove;
        }

        // ── States ───────────────────────────────────────────────────────────

        private void UpdateManualMove()
        {
            float dist = Vector2.Distance(transform.position, _manualMoveTarget);
            if (dist < 0.05f)
            {
                State = _resumeAutoCombat ? CombatState.Seeking : CombatState.Idle;
                return;
            }
            transform.position = Vector2.MoveTowards(
                transform.position, _manualMoveTarget, _stats.FinalStats.MoveSpeed * Time.deltaTime);
        }

        private void UpdateManualAttack()
        {
            if (_manualAttackTarget == null || !_manualAttackTarget.IsAlive)
            {
                _manualAttackTarget = null;
                State = _resumeAutoCombat ? CombatState.Seeking : CombatState.Idle;
                return;
            }

            float dist = Vector2.Distance(transform.position, _manualAttackTarget.transform.position);
            if (dist > attackRange)
            {
                transform.position = Vector2.MoveTowards(
                    transform.position, _manualAttackTarget.transform.position,
                    _stats.FinalStats.MoveSpeed * Time.deltaTime);
                return;
            }

            float damage = Random.Range(_stats.FinalStats.ATKMin, _stats.FinalStats.ATKMax);
            _manualAttackTarget.TakeDamage(damage);
            _attackTimer        = attackCooldown;
            _manualAttackTarget = null;
            State = _resumeAutoCombat ? CombatState.Seeking : CombatState.Idle;
        }

        private void SeekTarget()
        {
            if (enemySpawner == null) return;
            _currentTarget = enemySpawner.GetNearestEnemy(transform.position);
            State = _currentTarget != null ? CombatState.Moving : CombatState.Idle;
        }

        private void MoveToTarget()
        {
            if (_currentTarget == null || !_currentTarget.IsAlive) { State = CombatState.Seeking; return; }

            Vector2 myPos     = transform.position;
            Vector2 targetPos = _currentTarget.transform.position;
            float   dist      = Vector2.Distance(myPos, targetPos);

            if (dist <= attackRange) { State = CombatState.Attacking; return; }

            transform.position = Vector2.MoveTowards(myPos, targetPos, _stats.FinalStats.MoveSpeed * Time.deltaTime);
        }

        private void TryAttack()
        {
            if (_currentTarget == null || !_currentTarget.IsAlive) { State = CombatState.Seeking; return; }

            Vector2 myPos     = transform.position;
            Vector2 targetPos = _currentTarget.transform.position;

            if (Vector2.Distance(myPos, targetPos) > attackRange) { State = CombatState.Moving; return; }

            if (_attackTimer > 0f) return;
            _attackTimer = attackCooldown;

            float damage = Random.Range(_stats.FinalStats.ATKMin, _stats.FinalStats.ATKMax);
            _currentTarget.TakeDamage(damage);
        }
    }
}
