using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using IdleOn.Core;
using IdleOn.Save;
using IdleOn.Talents;

namespace IdleOn.UI
{
    public class TalentWindow : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject      windowPanel;
        [SerializeField] private Transform       slotContainer;
        [SerializeField] private TalentSlotUI    slotPrefab;
        [SerializeField] private TMP_Text        pointsText;
        [SerializeField] private TalentInfoPanel infoPanel;

        [Header("Assign Mode")]
        [SerializeField] private Button      assignModeButton;
        [SerializeField] private TMP_Text    assignModeButtonText;
        [SerializeField] private DragHandler dragHandler;

        [Header("Debug")]
        [SerializeField] private bool    enableDebugKey = true;
        [SerializeField] private KeyCode debugOpenKey   = KeyCode.T;

        private readonly List<TalentSlotUI> _slots = new List<TalentSlotUI>();
        private bool _assignMode;

        void Awake()
        {
            windowPanel.SetActive(false);
            GameEvents.OnTalentChanged += RefreshAll;
        }

        void OnDestroy() => GameEvents.OnTalentChanged -= RefreshAll;

        void Start() => PopulateSlots();

        void Update()
        {
            if (enableDebugKey && Input.GetKeyDown(debugOpenKey))
                Toggle();
        }

        public void Open()
        {
            windowPanel.SetActive(true);
            RefreshAll();
        }

        public void Close()
        {
            SetAssignMode(false);
            windowPanel.SetActive(false);
            infoPanel?.Hide();
        }

        public void Toggle()
        {
            if (windowPanel.activeSelf) Close();
            else Open();
        }

        public void ToggleAssignMode() => SetAssignMode(!_assignMode);

        private void SetAssignMode(bool on)
        {
            _assignMode = on;
            if (assignModeButtonText != null)
                assignModeButtonText.text = on ? "Assign: ON" : "Assign Skills";
            foreach (var slot in _slots)
                slot.SetAssignMode(on);
        }

        private void PopulateSlots()
        {
            var db = GameDatabase.Instance?.Talents;
            if (db == null) return;

            foreach (var def in db.Talents)
            {
                if (def == null) continue;
                var slot = Instantiate(slotPrefab, slotContainer);
                slot.Initialize(def, OnSlotClicked, dragHandler);
                _slots.Add(slot);
            }
        }

        private void OnSlotClicked(TalentDefinition def) => infoPanel?.Show(def);

        private void RefreshAll()
        {
            if (!windowPanel.activeSelf) return;

            if (pointsText != null)
            {
                int pts = SaveManager.Instance?.CurrentSave?.TalentPoints ?? 0;
                pointsText.text = pts.ToString();
            }

            foreach (var slot in _slots) slot.Refresh();
            infoPanel?.Refresh();
        }
    }
}
