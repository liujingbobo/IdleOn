using UnityEngine;
using IdleOn.Characters;
using IdleOn.Combat;
using IdleOn.Enemies;

namespace IdleOn.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Systems")]
        public PlayerStats       PlayerStats;
        public PlayerProgression PlayerProgression;
        public PlayerCombatController CombatController;
        public EnemySpawner      EnemySpawner;

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }
}
