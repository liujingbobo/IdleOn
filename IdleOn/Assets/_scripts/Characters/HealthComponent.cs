using System;
using UnityEngine;

namespace IdleOn.Characters
{
    public class HealthComponent : MonoBehaviour
    {
        public float MaxHP { get; private set; }
        public float CurrentHP { get; private set; }
        public bool IsAlive => CurrentHP > 0f;

        public event Action OnDied;
        public event Action<float, float> OnHPChanged;

        public void Initialize(float maxHP)
        {
            MaxHP = maxHP;
            CurrentHP = maxHP;
            OnHPChanged?.Invoke(CurrentHP, MaxHP);
        }

        public void TakeDamage(float amount)
        {
            if (!IsAlive) return;

            // TODO: apply DEF reduction
            CurrentHP = Mathf.Max(0f, CurrentHP - amount);
            OnHPChanged?.Invoke(CurrentHP, MaxHP);

            if (CurrentHP <= 0f)
                OnDied?.Invoke();
        }

        public void Heal(float amount)
        {
            if (!IsAlive) return;
            CurrentHP = Mathf.Min(MaxHP, CurrentHP + amount);
            OnHPChanged?.Invoke(CurrentHP, MaxHP);
        }
    }
}
