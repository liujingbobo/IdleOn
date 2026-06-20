using System.Collections.Generic;
using UnityEngine;
using IdleOn.Core;
using IdleOn.Characters;
using IdleOn.Save;

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
        public QuestDefinition ActiveQuestDefinition => _active;
        public IReadOnlyList<int> ActiveObjectiveCounts => _progress?.Counts;

        private QuestDefinition _active;
        private QuestProgress   _progress;
        private readonly HashSet<string> _completed = new HashSet<string>();
        private bool _stateImported;
        private bool _isImporting;

        // Source of completed-quest state for gates (PortalGate etc). Read-only — gates pull, never push.
        public bool IsCompleted(string questId)
            => !string.IsNullOrEmpty(questId) && _completed.Contains(questId);

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
            if (!_stateImported)
                StartQuest(database.StartQuest);
        }

        public QuestSaveData ExportState()
        {
            var data = new QuestSaveData
            {
                ActiveQuestId = _active != null ? _active.QuestId : string.Empty
            };

            if (database != null)
            {
                foreach (var quest in database.Quests)
                    if (quest != null && _completed.Contains(quest.QuestId))
                        data.CompletedQuestIds.Add(quest.QuestId);
            }

            if (_progress != null)
                foreach (int count in _progress.Counts)
                    data.ActiveObjectiveCounts.Add(count);

            return data;
        }

        public void ImportState(QuestSaveData data)
        {
            _stateImported = true;
            _isImporting = true;
            _completed.Clear();
            _active = null;
            _progress = null;

            if (database == null)
            {
                Debug.LogWarning("[QuestSystem] Cannot import state without a QuestDatabase.");
                _isImporting = false;
                return;
            }

            if (data != null && data.CompletedQuestIds != null)
            {
                foreach (string questId in data.CompletedQuestIds)
                    if (database.Get(questId) != null)
                        _completed.Add(questId);
            }

            QuestDefinition activeDef = data != null
                ? database.Get(data.ActiveQuestId)
                : null;

            if (activeDef != null && !_completed.Contains(activeDef.QuestId))
            {
                StartQuest(activeDef);
                var savedCounts = data.ActiveObjectiveCounts;
                if (savedCounts != null)
                {
                    int count = Mathf.Min(savedCounts.Count, activeDef.Objectives.Count);
                    for (int i = 0; i < count; i++)
                        _progress.Counts[i] = Mathf.Clamp(
                            savedCounts[i], 0, activeDef.Objectives[i].RequiredCount);
                }
            }
            else if (!_completed.Contains("q12"))
            {
                StartQuest(database.StartQuest);
            }
            else
            {
                GameEvents.RaiseQuestChanged(null);
            }

            _isImporting = false;
            PersistState();
            if (_active != null)
                GameEvents.RaiseQuestChanged(_active.QuestId);
        }

        // ── Quest lifecycle ─────────────────────────────────────────────────
        private void StartQuest(QuestDefinition def)
        {
            _active   = def;
            _progress = def != null ? new QuestProgress(def.QuestId, def.Objectives.Count) : null;
            if (_active == null) return;

            Debug.Log($"[QuestSystem] Quest started: {_active.QuestId} — {_active.Title}");
            GameEvents.RaiseQuestChanged(_active.QuestId);
            if (!_isImporting) PersistState();
        }

        private void Complete()
        {
            _progress.Completed = true;
            var def = _active;
            _completed.Add(def.QuestId);
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
                PersistState();
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

            if (!changed) return;

            GameEvents.RaiseQuestProgressChanged();
            if (AllObjectivesDone())
                Complete();
            else
                PersistState();
        }

        private bool AllObjectivesDone()
        {
            var objs = _active.Objectives;
            for (int i = 0; i < objs.Count; i++)
                if (_progress.Counts[i] < objs[i].RequiredCount) return false;
            return true;
        }

        private void PersistState()
        {
            var save = SaveManager.Instance?.CurrentSave;
            if (save != null)
                save.Quest = ExportState();
        }
    }
}
