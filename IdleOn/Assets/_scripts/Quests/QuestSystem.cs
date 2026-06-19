using UnityEngine;
using IdleOn.Core;
using IdleOn.Characters;

namespace IdleOn.Quests
{
    // Minimal linear main-quest driver (Phase A, in-memory). One active quest at a time. Listens to
    // GameEvents, advances objective counts, and on completion awards EXP, unlocks features, and chains
    // to NextQuestId. No save/load, no branching. No quest logic lives in any other system.
    public class QuestSystem : MonoBehaviour
    {
        public static QuestSystem Instance { get; private set; }

        [SerializeField] private QuestDatabase     database;
        [SerializeField] private PlayerProgression progression;   // for EXP rewards

        public string ActiveQuestId => _active != null ? _active.QuestId : null;

        private QuestDefinition _active;
        private QuestProgress   _progress;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void OnEnable()
        {
            GameEvents.OnGroundMoveCompleted += HandleGroundMove;
            GameEvents.OnMapChanged          += HandleMapEntered;
            GameEvents.OnEnemyKilled         += HandleEnemyKilled;
            GameEvents.OnItemCollected       += HandleItemCollected;
            GameEvents.OnItemCrafted         += HandleItemCrafted;
            GameEvents.OnItemEquipped        += HandleItemEquipped;
            GameEvents.OnTalentUpgraded      += HandleTalentUpgraded;
            GameEvents.OnDialogueEnded       += HandleDialogueEnded;
        }

        void OnDisable()
        {
            GameEvents.OnGroundMoveCompleted -= HandleGroundMove;
            GameEvents.OnMapChanged          -= HandleMapEntered;
            GameEvents.OnEnemyKilled         -= HandleEnemyKilled;
            GameEvents.OnItemCollected       -= HandleItemCollected;
            GameEvents.OnItemCrafted         -= HandleItemCrafted;
            GameEvents.OnItemEquipped        -= HandleItemEquipped;
            GameEvents.OnTalentUpgraded      -= HandleTalentUpgraded;
            GameEvents.OnDialogueEnded       -= HandleDialogueEnded;
        }

        void Start()
        {
            if (database == null) { Debug.LogWarning("[QuestSystem] No QuestDatabase assigned."); return; }
            StartQuest(database.StartQuest);
        }

        // ── Quest lifecycle ─────────────────────────────────────────────────
        private void StartQuest(QuestDefinition def)
        {
            _active   = def;
            _progress = def != null ? new QuestProgress(def.QuestId, def.Objectives.Count) : null;
            if (_active == null) return;

            Debug.Log($"[QuestSystem] Quest started: {_active.QuestId} — {_active.Title}");
            GameEvents.RaiseQuestChanged(_active.QuestId);
        }

        private void Complete()
        {
            _progress.Completed = true;
            var def = _active;
            Debug.Log($"[QuestSystem] Quest completed: {def.QuestId}");

            if (def.ExpReward > 0)
            {
                if (progression != null) progression.AwardExp(def.ExpReward);
                else Debug.LogWarning("[QuestSystem] No PlayerProgression assigned — EXP reward skipped.");
            }

            if (def.UnlocksFeatures != FeatureFlags.None)
                FeatureUnlockSystem.Instance?.Unlock(def.UnlocksFeatures);

            if (!string.IsNullOrEmpty(def.UnlocksMapId))
                Debug.Log($"[QuestSystem] (placeholder) unlock map/portal: {def.UnlocksMapId}");

            GameEvents.RaiseQuestCompleted(def.QuestId);

            var next = database.Get(def.NextQuestId);
            if (next != null)
            {
                StartQuest(next);
            }
            else
            {
                _active = null;
                _progress = null;
                Debug.Log("[QuestSystem] No next quest — chain complete.");
            }
        }

        // ── Event handlers → objective types ────────────────────────────────
        private void HandleGroundMove()                          => Advance(QuestObjectiveType.GroundMove, null);
        private void HandleMapEntered(string mapId)              => Advance(QuestObjectiveType.MapEntered, mapId);
        private void HandleEnemyKilled(string enemyId, float xp) => Advance(QuestObjectiveType.EnemyKilled, enemyId);
        private void HandleItemCollected(string itemId, int qty) => Advance(QuestObjectiveType.ItemCollected, itemId, qty);
        private void HandleItemCrafted(string itemId)            => Advance(QuestObjectiveType.ItemCrafted, itemId);
        private void HandleItemEquipped(string itemId)           => Advance(QuestObjectiveType.ItemEquipped, itemId);
        private void HandleTalentUpgraded(string talentId)       => Advance(QuestObjectiveType.TalentUpgraded, talentId);
        private void HandleDialogueEnded(string dialogueId)      => Advance(QuestObjectiveType.DialogueEnded, dialogueId);

        // ── Core advance ────────────────────────────────────────────────────
        private void Advance(QuestObjectiveType type, string targetId, int amount = 1)
        {
            if (_active == null || _progress == null || _progress.Completed) return;

            bool changed = false;
            var objs = _active.Objectives;
            for (int i = 0; i < objs.Count; i++)
            {
                var o = objs[i];
                if (o.Type != type) continue;
                if (!string.IsNullOrEmpty(o.TargetId) && o.TargetId != targetId) continue;
                if (_progress.Counts[i] >= o.RequiredCount) continue;

                _progress.Counts[i] += amount;
                if (_progress.Counts[i] > o.RequiredCount) _progress.Counts[i] = o.RequiredCount;
                changed = true;
            }

            if (changed && AllObjectivesDone())
                Complete();
        }

        private bool AllObjectivesDone()
        {
            var objs = _active.Objectives;
            for (int i = 0; i < objs.Count; i++)
                if (_progress.Counts[i] < objs[i].RequiredCount) return false;
            return true;
        }
    }
}
