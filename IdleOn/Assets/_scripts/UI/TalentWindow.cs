using System.Collections.Generic;
using UnityEngine;
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

        [Header("Motion (optional)")]
        [SerializeField] private UIWindowMotion motion;

        [Header("Debug")]
        [SerializeField] private bool    enableDebugKey = true;
        [SerializeField] private KeyCode debugOpenKey   = KeyCode.T;

        private readonly List<TalentSlotUI> _slots = new List<TalentSlotUI>();
        private readonly Dictionary<TalentDefinition, TalentSlotUI> _slotsByDef = new Dictionary<TalentDefinition, TalentSlotUI>();
        private TalentSlotUI _selectedSlot;

        public bool IsOpen => motion != null ? motion.IsOpen : windowPanel.activeSelf;

        void Awake()
        {
            if (motion != null) motion.SetClosedImmediate();
            else                windowPanel.SetActive(false);
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
            if (motion != null) motion.PlayOpen();
            else                windowPanel.SetActive(true);
            ClearSelection();
            RefreshAll();
        }

        public void Close()
        {
            if (motion != null) motion.PlayClose();
            else                windowPanel.SetActive(false);
            ClearSelection();
        }

        public void Toggle()
        {
            if (IsOpen) Close();
            else Open();
        }

        private void ClearSelection()
        {
            _selectedSlot?.SetSelected(false);
            _selectedSlot = null;
            infoPanel?.Hide();
        }

        private void PopulateSlots()
        {
            var db = GameDatabase.Instance?.Talents;
            if (db == null) return;

            foreach (var def in db.Talents)
            {
                if (def == null) continue;
                var slot = Instantiate(slotPrefab, slotContainer);
                slot.Initialize(def, OnSlotClicked);
                _slots.Add(slot);
                _slotsByDef[def] = slot;
            }
        }

        private void OnSlotClicked(TalentDefinition def)
        {
            _selectedSlot?.SetSelected(false);
            _selectedSlot = _slotsByDef.TryGetValue(def, out var slot) ? slot : null;
            _selectedSlot?.SetSelected(true);

            infoPanel?.Show(def);
        }

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
