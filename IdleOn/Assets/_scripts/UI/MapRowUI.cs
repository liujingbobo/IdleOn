using UnityEngine;
using UnityEngine.UI;
using TMPro;
using IdleOn.World;

namespace IdleOn.UI
{
    public class MapRowUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text   mapNameText;
        [SerializeField] private TMP_Text   objectiveText;
        [SerializeField] private Button     travelButton;
        [SerializeField] private TMP_Text   travelButtonText;
        [SerializeField] private GameObject lockIcon;
        [SerializeField] private Image      rowBackground;

        [Header("Colors")]
        [SerializeField] private Color currentMapColor = new Color(0.22f, 0.55f, 0.22f, 1f);
        [SerializeField] private Color defaultColor    = new Color(0.12f, 0.12f, 0.14f, 1f);
        [SerializeField] private Color completeColor   = new Color(0.12f, 0.20f, 0.35f, 1f);

        private MapDefinition _def;

        public void Initialize(MapDefinition def)
        {
            _def = def;
            if (mapNameText != null) mapNameText.text = def.DisplayName;
            travelButton.onClick.AddListener(OnTravelClicked);
            Refresh();
        }

        public void Refresh()
        {
            if (_def == null || MapSystem.Instance == null) return;

            var  prog       = MapSystem.Instance.GetProgress(_def.MapId);
            bool isCurrent  = MapSystem.Instance.CurrentMapId == _def.MapId;
            bool isUnlocked = prog != null && prog.IsUnlocked;

            // Unlock requirement display now lives on the Portal (PortalGate) — this row just shows
            // identity + travel state.
            objectiveText.text = isUnlocked ? "" : "Locked";

            travelButton.gameObject.SetActive(isUnlocked);
            travelButton.interactable = !isCurrent;
            if (travelButtonText != null)
                travelButtonText.text = isCurrent ? "Here" : "Travel";

            if (lockIcon != null) lockIcon.SetActive(!isUnlocked);

            if (rowBackground != null)
                rowBackground.color = isCurrent ? currentMapColor : defaultColor;
        }

        private void OnTravelClicked()
        {
            MapSystem.Instance?.TravelTo(_def.MapId);
        }
    }
}
