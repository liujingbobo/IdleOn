using UnityEngine;
using IdleOn.Core;
using IdleOn.Quests;

namespace IdleOn.UI
{
    // Generic scene-object gate. Lives on an always-active GameObject (never on target itself —
    // a script that disables its own GameObject can't re-enable it later). SetActive(true/false)
    // on target mirrors FeatureUnlockSystem state, same pattern as HUDFeatureGate.
    public class FeatureGatedGameObject : MonoBehaviour
    {
        [SerializeField] private FeatureFlags feature;
        [SerializeField] private GameObject target;

        void OnEnable()
        {
            GameEvents.OnFeaturesChanged          += Refresh;
            GameEvents.OnPersistentProgressLoaded += Refresh;
            Refresh();
        }

        void OnDisable()
        {
            GameEvents.OnFeaturesChanged          -= Refresh;
            GameEvents.OnPersistentProgressLoaded -= Refresh;
        }

        void Start() => Refresh();

        private void Refresh()
        {
            if (target == null) return;

            var fu = FeatureUnlockSystem.Instance;
            bool unlocked = fu != null && fu.IsUnlocked(feature);
            target.SetActive(unlocked);
        }
    }
}
