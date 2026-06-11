using System;
using UnityEngine;
using IdleOn.Core;
using IdleOn.Characters;

namespace IdleOn.Enemies
{
    [RequireComponent(typeof(HealthComponent))]
    public class EnemyController : MonoBehaviour
    {
        [SerializeField] private EnemyDefinition definition;

        private HealthComponent _health;

        public bool IsAlive => _health != null && _health.IsAlive;
        public EnemyDefinition Definition => definition;

        // EnemySpawner subscribes once at instantiation for respawn scheduling
        public event Action<EnemyController> OnKilled;

        void Awake()
        {
            _health = GetComponent<HealthComponent>();
        }

        void OnEnable()
        {
            if (_health == null) return;
            _health.Initialize(definition != null ? definition.MaxHP : 1f);
            _health.OnDied += HandleDied;
        }

        void OnDisable()
        {
            if (_health != null)
                _health.OnDied -= HandleDied;
        }

        public void TakeDamage(float amount)
        {
            _health?.TakeDamage(amount);
        }

        private void HandleDied()
        {
            float xp    = definition != null ? definition.XPReward  : 0f;
            int   coins = definition != null ? definition.CoinReward : 0;

            GameEvents.RaiseEnemyKilled(definition != null ? definition.EnemyId : string.Empty, xp, coins);
            OnKilled?.Invoke(this);
            gameObject.SetActive(false);
        }
    }
}
