using UnityEngine;
using IdleOn.Core;
using IdleOn.Equipment;
using IdleOn.Vault;
using IdleOn.Talents;

namespace IdleOn.Characters
{
    [RequireComponent(typeof(HealthComponent))]
    public class PlayerStats : MonoBehaviour
    {
        public static PlayerStats Instance { get; private set; }

        [Header("Base Stats")]
        [SerializeField] private StatSheet baseStats = new StatSheet();

        private StatSheet       _finalStats  = new StatSheet();
        private HealthComponent _health;
        private bool            _initialized;
        private float           _currentMP;

        public StatSheet FinalStats => _finalStats;
        public float     CurrentMP  => _currentMP;
        public float     CurrentHP  => _health != null ? _health.CurrentHP : 0f;
        public float     MaxHP      => _health != null ? _health.MaxHP     : _finalStats.MaxHP;

        void Awake()
        {
            Instance = this;
            _health  = GetComponent<HealthComponent>();
            // Subscribe before Recalculate() — its first-time _health.Initialize fires OnHPChanged,
            // which we forward so the HUD receives the initial HP.
            _health.OnHPChanged += HandleHPChanged;
            Recalculate();
        }

        void OnDestroy()
        {
            if (_health != null) _health.OnHPChanged -= HandleHPChanged;
        }

        private void HandleHPChanged(float current, float max)
            => GameEvents.RaisePlayerHPChanged(current, max);

        public void Recalculate()
        {
            float oldMaxMP = _finalStats.MaxMP;

            _finalStats = new StatSheet
            {
                STR        = baseStats.STR,
                AGI        = baseStats.AGI,
                WIS        = baseStats.WIS,
                LUK        = baseStats.LUK,
                MaxHP      = baseStats.MaxHP,
                MaxMP      = baseStats.MaxMP,
                ATKMin     = baseStats.ATKMin,
                ATKMax     = baseStats.ATKMax,
                DEF        = baseStats.DEF,
                ACC        = baseStats.ACC,
                CRITChance = baseStats.CRITChance,
                MoveSpeed  = baseStats.MoveSpeed
            };

            // Sum equipment bonuses
            var equipment = EquipmentSystem.Instance;
            if (equipment != null)
            {
                var db = GameDatabase.Instance?.Items;
                foreach (var entry in equipment.GetAllEquipped())
                {
                    var def = db?.GetItem(entry.ItemId);
                    if (def == null) continue;

                    var b = def.StatBonuses;
                    _finalStats.STR        += b.STR;
                    _finalStats.AGI        += b.AGI;
                    _finalStats.WIS        += b.WIS;
                    _finalStats.LUK        += b.LUK;
                    _finalStats.MaxHP      += b.MaxHP;
                    _finalStats.MaxMP      += b.MaxMP;
                    _finalStats.ATKMin     += b.ATKMin;
                    _finalStats.ATKMax     += b.ATKMax;
                    _finalStats.DEF        += b.DEF;
                    _finalStats.ACC        += b.ACC;
                    _finalStats.CRITChance += b.CRITChance;
                    _finalStats.MoveSpeed  += b.MoveSpeed;
                }
            }

            // Apply vault attack bonuses
            var vault = VaultSystem.Instance;
            if (vault != null)
            {
                _finalStats.ATKMin += vault.GetATKMinBonus();
                _finalStats.ATKMax += vault.GetATKMaxBonus();
            }

            // Apply talent bonuses
            var talents = TalentSystem.Instance;
            if (talents != null)
            {
                _finalStats.ATKMin    += talents.GetATKMinBonus();
                _finalStats.ATKMax    += talents.GetATKMaxBonus();
                _finalStats.MaxHP     += talents.GetMaxHPBonus();
                _finalStats.MoveSpeed += talents.GetMoveSpeedBonus();
                _finalStats.MaxMP     += talents.GetMaxMPBonus();
            }

            if (!_initialized)
            {
                _health?.Initialize(_finalStats.MaxHP);
                _currentMP = _finalStats.MaxMP;
                _initialized = true;
            }
            else
            {
                _health?.UpdateMaxHP(_finalStats.MaxHP);
                float ratio = oldMaxMP > 0f ? _currentMP / oldMaxMP : 1f;
                _currentMP = Mathf.Clamp(_finalStats.MaxMP * ratio, 0f, _finalStats.MaxMP);
            }

            GameEvents.RaisePlayerMPChanged(_currentMP, _finalStats.MaxMP);
        }

        public bool SpendMP(float amount)
        {
            if (amount <= 0f) return true;
            if (_currentMP < amount) return false;

            _currentMP = Mathf.Max(0f, _currentMP - amount);
            GameEvents.RaisePlayerMPChanged(_currentMP, _finalStats.MaxMP);
            return true;
        }
    }
}
