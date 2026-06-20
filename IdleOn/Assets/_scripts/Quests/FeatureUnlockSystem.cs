using UnityEngine;
using IdleOn.Core;
using IdleOn.Save;

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

        public int ExportState() => (int)Unlocked;

        public void ImportState(int flags)
        {
            Unlocked = (FeatureFlags)flags;
            MirrorToSave();
            GameEvents.RaiseFeaturesChanged();
        }

        public void Unlock(FeatureFlags feature)
        {
            if (feature == FeatureFlags.None) return;
            if ((Unlocked & feature) == feature) return;   // already unlocked

            Unlocked |= feature;
            MirrorToSave();
            Debug.Log($"[FeatureUnlock] Unlocked: {feature} | now: {Unlocked}");
            GameEvents.RaiseFeaturesChanged();
        }

        private void MirrorToSave()
        {
            var save = SaveManager.Instance?.CurrentSave;
            if (save != null)
                save.UnlockedFeatureFlags = ExportState();
        }
    }
}
