using UnityEngine;
using IdleOn.Core;
using IdleOn.World;

namespace IdleOn.Quests
{
    // Drives a portal's hidden / locked / unlocked state from quest + feature state WITHOUT putting any
    // quest logic into PortalInteractable. Never deactivates the portal root — only toggles the visual,
    // the collider, and the PortalInteractable component:
    //   Hidden   (!revealed): visual off, collider off, portal off.
    //   Locked   (revealed, not unlocked): visual on, collider off, portal off  → no travel.
    //   Unlocked: visual on, collider on, portal on  → existing PortalInteractable travel works.
    public class PortalGate : MonoBehaviour
    {
        [SerializeField] private GameObject         visual;
        [SerializeField] private PortalInteractable portal;
        [SerializeField] private Collider2D         portalCollider;

        [Header("Gate conditions (empty = no condition)")]
        [SerializeField] private string       revealQuestId;
        [SerializeField] private string       unlockQuestId;
        [SerializeField] private FeatureFlags requiredFeature = FeatureFlags.None;

        void OnEnable()
        {
            GameEvents.OnQuestCompleted  += HandleQuestCompleted;
            GameEvents.OnFeaturesChanged += Evaluate;
            Evaluate();
        }

        void OnDisable()
        {
            GameEvents.OnQuestCompleted  -= HandleQuestCompleted;
            GameEvents.OnFeaturesChanged -= Evaluate;
        }

        // Re-evaluate after all Awakes have run (QuestSystem/FeatureUnlockSystem instances ready).
        void Start() => Evaluate();

        private void HandleQuestCompleted(string questId) => Evaluate();

        private void Evaluate()
        {
            bool revealed = string.IsNullOrEmpty(revealQuestId) || IsQuestDone(revealQuestId);
            bool unlocked = revealed
                         && (string.IsNullOrEmpty(unlockQuestId) || IsQuestDone(unlockQuestId))
                         && (requiredFeature == FeatureFlags.None || IsFeatureUnlocked(requiredFeature));

            if (visual != null)         visual.SetActive(revealed);
            if (portalCollider != null) portalCollider.enabled = unlocked;
            if (portal != null)         portal.enabled = unlocked;
        }

        private bool IsQuestDone(string questId)
        {
            var qs = QuestSystem.Instance;
            return qs != null && qs.IsCompleted(questId);
        }

        private bool IsFeatureUnlocked(FeatureFlags feature)
        {
            var fu = FeatureUnlockSystem.Instance;
            return fu != null && fu.IsUnlocked(feature);
        }
    }
}
