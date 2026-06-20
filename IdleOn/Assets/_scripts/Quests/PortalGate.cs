using UnityEngine;
using TMPro;
using IdleOn.Core;
using IdleOn.World;

namespace IdleOn.Quests
{
    // Drives a portal's active/inactive state from its DESTINATION map's unlock requirements.
    // Portal itself only stores destinationMapId (PortalInteractable). The destination MapDefinition
    // is the single source of truth for what unlocks it (UnlockQuestId / UnlockEnemyId+UnlockKillCount).
    // Never deactivates the portal root — only alpha, collider, and PortalInteractable.enabled:
    //   Unlocked: alpha 1, collider on, portal on  → travel works.
    //   Locked:   alpha 0.5, collider off, portal off → no travel. Requirement text shown only if
    //             locked by a kill requirement (quest-only lock shows no text, just half alpha).
    public class PortalGate : MonoBehaviour
    {
        [SerializeField] private GameObject         visual;
        [SerializeField] private PortalInteractable portal;
        [SerializeField] private Collider2D         portalCollider;
        [SerializeField] private TMP_Text           requirementText; // optional, hidden when not needed

        private SpriteRenderer _visualRenderer;

        void Awake()
        {
            if (visual != null) _visualRenderer = visual.GetComponent<SpriteRenderer>();
        }

        void OnEnable()
        {
            GameEvents.OnQuestCompleted  += HandleQuestCompleted;
            GameEvents.OnFeaturesChanged += Evaluate;
            GameEvents.OnPersistentProgressLoaded += Evaluate;
            Evaluate();
        }

        void OnDisable()
        {
            GameEvents.OnQuestCompleted  -= HandleQuestCompleted;
            GameEvents.OnFeaturesChanged -= Evaluate;
            GameEvents.OnPersistentProgressLoaded -= Evaluate;
        }

        // Re-evaluate after all Awakes have run (QuestSystem/EnemyKillTracker instances ready).
        void Start() => Evaluate();

        private void HandleQuestCompleted(string questId) => Evaluate();

        private void Evaluate()
        {
            var destMap = GetDestinationMap();

            bool questOk = destMap == null || string.IsNullOrEmpty(destMap.UnlockQuestId)
                           || IsQuestDone(destMap.UnlockQuestId);
            bool killOk  = destMap == null || string.IsNullOrEmpty(destMap.UnlockEnemyId)
                           || GetKillCount(destMap.UnlockEnemyId) >= destMap.UnlockKillCount;
            bool unlocked = questOk && killOk;

            if (_visualRenderer != null)
            {
                var c = _visualRenderer.color;
                c.a = unlocked ? 1f : 0.5f;
                _visualRenderer.color = c;
            }
            if (portalCollider != null) portalCollider.enabled = unlocked;
            if (portal != null)         portal.enabled = unlocked;

            // MapSystem.TravelTo still gates on MapProgressData.IsUnlocked (legacy flag from the old
            // auto-unlock-next-map model). Keep it in sync with this portal's destination-MapDef
            // evaluation so clicking an unlocked portal actually travels. MapSystem.cs itself is untouched.
            if (unlocked && destMap != null)
            {
                var prog = MapSystem.Instance?.GetProgress(destMap.MapId);
                if (prog != null) prog.IsUnlocked = true;
            }

            if (requirementText != null)
            {
                bool showKillReq = !unlocked && !killOk && destMap != null;
                requirementText.gameObject.SetActive(showKillReq);
                if (showKillReq)
                {
                    int current = GetKillCount(destMap.UnlockEnemyId);
                    string label = string.IsNullOrEmpty(destMap.UnlockRequirementLabel)
                        ? destMap.UnlockEnemyId
                        : destMap.UnlockRequirementLabel;
                    requirementText.text = $"{current}/{destMap.UnlockKillCount} × {label}";
                }
            }
        }

        private MapDefinition GetDestinationMap()
        {
            if (portal == null || string.IsNullOrEmpty(portal.DestinationMapId)) return null;
            return GameDatabase.Instance?.Maps?.GetMap(portal.DestinationMapId);
        }

        private bool IsQuestDone(string questId)
        {
            var qs = QuestSystem.Instance;
            return qs != null && qs.IsCompleted(questId);
        }

        private int GetKillCount(string enemyId)
        {
            var tracker = EnemyKillTracker.Instance;
            return tracker != null ? tracker.GetKillCount(enemyId) : 0;
        }
    }
}
