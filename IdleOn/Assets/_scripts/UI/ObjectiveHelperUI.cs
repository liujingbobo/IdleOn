using UnityEngine;
using TMPro;
using IdleOn.Core;
using IdleOn.World;

namespace IdleOn.UI
{
    // Map-level top-strip objective display. Disabled in TestCombat for the current demo —
    // portal unlock requirements are now shown on the Portal itself (see PortalGate), not here.
    // Kept compiling/minimal since MapDefinition no longer carries kill-objective/reward fields.
    public class ObjectiveHelperUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text mainText;
        [SerializeField] private TMP_Text rewardText;

        void Awake()
        {
            GameEvents.OnMapChanged += OnMapChanged;
        }

        void OnDestroy()
        {
            GameEvents.OnMapChanged -= OnMapChanged;
        }

        void Start() => RefreshFromCurrentMap();

        private void RefreshFromCurrentMap()
        {
            if (MapSystem.Instance == null) return;

            var mapDef = MapSystem.Instance.CurrentMapDef;
            if (mapDef == null)
            {
                if (mainText   != null) mainText.text   = "";
                if (rewardText != null) rewardText.text = "";
                return;
            }

            if (mainText != null) mainText.text = mapDef.DisplayName;
            if (rewardText != null) rewardText.text = "";
        }

        private void OnMapChanged(string mapId) => RefreshFromCurrentMap();
    }
}
