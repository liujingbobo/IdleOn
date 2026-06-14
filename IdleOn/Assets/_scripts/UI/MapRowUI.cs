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
            bool isComplete = prog != null && prog.IsComplete;

            // Objective text
            if (!isUnlocked)
            {
                objectiveText.text = "???";
            }
            else if (isComplete)
            {
                objectiveText.text = $"✓ {_def.ObjectiveLabel}  —  Complete";
            }
            else
            {
                int kills = prog?.KillCount ?? 0;
                objectiveText.text = $"{_def.ObjectiveLabel}  {kills} / {_def.KillObjective}";
            }

            // Travel button visibility and state
            travelButton.gameObject.SetActive(isUnlocked);
            travelButton.interactable = !isCurrent;
            if (travelButtonText != null)
                travelButtonText.text = isCurrent ? "Here" : "Travel";

            // Lock icon
            if (lockIcon != null) lockIcon.SetActive(!isUnlocked);

            // Row background tint
            if (rowBackground != null)
            {
                if (isCurrent)        rowBackground.color = currentMapColor;
                else if (isComplete)  rowBackground.color = completeColor;
                else                  rowBackground.color = defaultColor;
            }
        }

        private void OnTravelClicked()
        {
            MapSystem.Instance?.TravelTo(_def.MapId);
        }
    }
}
