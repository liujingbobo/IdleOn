using UnityEngine;
using IdleOn.Core;
using IdleOn.Equipment;
using IdleOn.Vault;

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

        public StatSheet FinalStats => _finalStats;

        void Awake()
        {
            Instance = this;
            _health  = GetComponent<HealthComponent>();
            Recalculate();
        }

        public void Recalculate()
        {
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

            if (!_initialized)
            {
                _health?.Initialize(_finalStats.MaxHP);
                _initialized = true;
            }
            else
            {
                _health?.UpdateMaxHP(_finalStats.MaxHP);
            }
        }

        public bool SpendMP(float amount)
        {
            // TODO: track current MP
            return true;
        }
    }
}
