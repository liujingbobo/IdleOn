using UnityEngine;
using IdleOn.Core;
using IdleOn.Save;
using IdleOn.Vault;

namespace IdleOn.Characters
{
    public class PlayerProgression : MonoBehaviour
    {
        [Header("XP Curve")]
        [SerializeField] private float _baseExp    = 100f;
        [SerializeField] private float _growthRate = 1.2f;

        public int   Level        { get; private set; } = 1;
        public float CurrentExp   { get; private set; } = 0f;
        public int   TalentPoints { get; private set; } = 0;

        void Start()
        {
            if (SaveManager.Instance != null && SaveManager.Instance.IsLoaded)
                Initialize();
            else
                SaveManager.OnSaveLoaded += Initialize;
        }

        void OnEnable()  => GameEvents.OnEnemyKilled += HandleEnemyKilled;

        void OnDisable()
        {
            GameEvents.OnEnemyKilled -= HandleEnemyKilled;
            SaveManager.OnSaveLoaded -= Initialize;
        }

        private void Initialize()
        {
            var save = SaveManager.Instance?.CurrentSave;
            if (save == null) return;
            Level        = save.Level;
            CurrentExp   = save.Exp;
            TalentPoints = save.TalentPoints;
        }

        // XP required to advance from `level` to `level + 1`.
        public int ExpForNextLevel(int level)
            => Mathf.FloorToInt(_baseExp * Mathf.Pow(_growthRate, level - 1));

        private void HandleEnemyKilled(string enemyId, float xp) => AwardExp(xp);

        // Public EXP grant — reused by kill rewards and quest rewards. Runs the same level-up loop.
        public void AwardExp(float xp)
        {
            CurrentExp += xp;

            while (CurrentExp >= ExpForNextLevel(Level))
            {
                CurrentExp -= ExpForNextLevel(Level);
                Level++;

                int bonus = VaultSystem.Instance != null ? VaultSystem.Instance.GetTalentPointBonus() : 0;
                TalentPoints += 1 + bonus;

                var save = SaveManager.Instance?.CurrentSave;
                if (save != null)
                {
                    save.Level        = Level;
                    save.TalentPoints = TalentPoints;
                }

                GameEvents.RaisePlayerLevelChanged(Level);
                Debug.Log($"[Progression] LEVEL UP → Lv.{Level} | TalentPoints: {TalentPoints}");
            }

            var s = SaveManager.Instance?.CurrentSave;
            if (s != null)
                s.Exp = CurrentExp;

            GameEvents.RaisePlayerExpGained(xp);
            Debug.Log($"[Progression] +{xp} EXP | {CurrentExp}/{ExpForNextLevel(Level)} | Lv.{Level}");
        }
    }
}
