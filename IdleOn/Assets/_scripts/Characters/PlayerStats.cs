using UnityEngine;
using IdleOn.Core;

namespace IdleOn.Characters
{
    [RequireComponent(typeof(HealthComponent))]
    public class PlayerStats : MonoBehaviour
    {
        public static PlayerStats Instance { get; private set; }

        [Header("Base Stats")]
        [SerializeField] private StatSheet baseStats = new StatSheet();

        private StatSheet _finalStats = new StatSheet();
        public StatSheet FinalStats => _finalStats;

        private HealthComponent _health;

        void Awake()
        {
            Instance = this;
            _health = GetComponent<HealthComponent>();
            Recalculate();
        }

        public void Recalculate()
        {
            // TODO: add bonuses from Equipment, Talent, and Vault systems
            _finalStats = new StatSheet
            {
                STR = baseStats.STR,
                AGI = baseStats.AGI,
                WIS = baseStats.WIS,
                LUK = baseStats.LUK,
                MaxHP = baseStats.MaxHP,
                MaxMP = baseStats.MaxMP,
                ATKMin = baseStats.ATKMin,
                ATKMax = baseStats.ATKMax,
                DEF = baseStats.DEF,
                ACC = baseStats.ACC,
                CRITChance = baseStats.CRITChance,
                MoveSpeed = baseStats.MoveSpeed
            };

            _health?.Initialize(_finalStats.MaxHP);
        }

        public bool SpendMP(float amount)
        {
            // TODO: track current MP
            return true;
        }
    }
}
