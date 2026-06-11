using UnityEngine;
using IdleOn.Core;
using IdleOn.Characters;
using IdleOn.Enemies;

namespace IdleOn.Combat
{
    public enum CombatState { Idle, Seeking, Moving, Attacking }

    [RequireComponent(typeof(PlayerStats))]
    public class PlayerCombatController : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private float attackRange = 1.5f;
        [SerializeField] private float attackCooldown = 1f;
        [SerializeField] private bool startAutoCombatOnPlay = true;

        [Header("References")]
        [SerializeField] private EnemySpawner enemySpawner;

        private PlayerStats _stats;
        private EnemyController _currentTarget;
        private float _attackTimer;

        public bool IsAutoCombatActive { get; private set; }
        public CombatState State { get; private set; } = CombatState.Idle;

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
                State = CombatState.Idle;
                _currentTarget = null;
            }
            GameEvents.RaiseAutoCombatChanged(active);
        }

        void Update()
        {
            if (!IsAutoCombatActive) return;
            _attackTimer -= Time.deltaTime;

            switch (State)
            {
                case CombatState.Idle:
                case CombatState.Seeking:
                    SeekTarget();
                    break;
                case CombatState.Moving:
                    MoveToTarget();
                    break;
                case CombatState.Attacking:
                    TryAttack();
                    break;
            }
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
