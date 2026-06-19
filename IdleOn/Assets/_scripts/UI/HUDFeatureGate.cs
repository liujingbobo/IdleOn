using UnityEngine;
using IdleOn.Core;
using IdleOn.Quests;

namespace IdleOn.UI
{
    // Shows / hides feature buttons from FeatureUnlockSystem state. Lives on an always-active object
    // (Canvas / MainHUD), never on the hidden buttons themselves. Inventory is always visible and not
    // handled here. Only SetActive on the button GameObjects — MainHUD's window-switching and button
    // sprite logic are untouched (they simply don't matter while a button is inactive).
    public class HUDFeatureGate : MonoBehaviour
    {
        [SerializeField] private GameObject autoCombatButtonObject;
        [SerializeField] private GameObject craftButtonObject;
        [SerializeField] private GameObject talentButtonObject;
        [SerializeField] private GameObject vaultButtonObject;
        [SerializeField] private GameObject mapButtonObject;

        void OnEnable()
        {
            GameEvents.OnFeaturesChanged += Refresh;
            Refresh();
        }

        void OnDisable()
        {
            GameEvents.OnFeaturesChanged -= Refresh;
        }

        void Start() => Refresh();

        private void Refresh()
        {
            Apply(autoCombatButtonObject, FeatureFlags.AutoCombat);
            Apply(craftButtonObject,      FeatureFlags.Craft);
            Apply(talentButtonObject,     FeatureFlags.Talents);
            Apply(vaultButtonObject,      FeatureFlags.Vault);
            Apply(mapButtonObject,        FeatureFlags.Map);
        }

        private void Apply(GameObject buttonObject, FeatureFlags feature)
        {
            if (buttonObject == null) return;
            var fu = FeatureUnlockSystem.Instance;
            bool unlocked = fu != null && fu.IsUnlocked(feature);
            buttonObject.SetActive(unlocked);
        }
    }
}
