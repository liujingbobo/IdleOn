using UnityEngine;
using IdleOn.Core;

namespace IdleOn.Quests
{
    // In-memory feature unlock state (Phase A — NOT persisted yet). Quests call Unlock(); UI gating
    // (HUDFeatureGate) is planned for a later phase and listens to GameEvents.OnFeaturesChanged.
    public class FeatureUnlockSystem : MonoBehaviour
    {
        public static FeatureUnlockSystem Instance { get; private set; }

        public FeatureFlags Unlocked { get; private set; } = FeatureFlags.None;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public bool IsUnlocked(FeatureFlags feature)
            => feature != FeatureFlags.None && (Unlocked & feature) == feature;

        public void Unlock(FeatureFlags feature)
        {
            if (feature == FeatureFlags.None) return;
            if ((Unlocked & feature) == feature) return;   // already unlocked

            Unlocked |= feature;
            Debug.Log($"[FeatureUnlock] Unlocked: {feature} | now: {Unlocked}");
            GameEvents.RaiseFeaturesChanged();
        }
    }
}
