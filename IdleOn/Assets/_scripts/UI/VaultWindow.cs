using System.Collections.Generic;
using UnityEngine;
using IdleOn.Core;
using IdleOn.Vault;

namespace IdleOn.UI
{
    public class VaultWindow : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject     windowPanel;
        [SerializeField] private Transform      slotContainer;
        [SerializeField] private VaultSlotUI    slotPrefab;
        [SerializeField] private VaultInfoPanel infoPanel;

        [Header("Motion (optional)")]
        [SerializeField] private UIWindowMotion motion;

        [Header("Debug")]
        [SerializeField] private bool    enableDebugKey = true;
        [SerializeField] private KeyCode debugOpenKey   = KeyCode.V;

        private readonly List<VaultSlotUI> _slots = new List<VaultSlotUI>();
        private readonly Dictionary<VaultUpgradeDefinition, VaultSlotUI> _slotsByDef = new Dictionary<VaultUpgradeDefinition, VaultSlotUI>();
        private VaultSlotUI _selectedSlot;

        public bool IsOpen => motion != null ? motion.IsOpen : windowPanel.activeSelf;

        void Awake()
        {
            if (motion != null) motion.SetClosedImmediate();
            else                windowPanel.SetActive(false);
            GameEvents.OnVaultChanged += RefreshAll;
        }

        void OnDestroy() => GameEvents.OnVaultChanged -= RefreshAll;

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
            var db = GameDatabase.Instance?.Vault;
            if (db == null) return;

            foreach (var def in db.Upgrades)
            {
                if (def == null) continue;
                var slot = Instantiate(slotPrefab, slotContainer);
                slot.Initialize(def, OnSlotClicked);
                _slots.Add(slot);
                _slotsByDef[def] = slot;
            }
        }

        private void OnSlotClicked(VaultUpgradeDefinition def)
        {
            _selectedSlot?.SetSelected(false);
            _selectedSlot = _slotsByDef.TryGetValue(def, out var slot) ? slot : null;
            _selectedSlot?.SetSelected(true);

            infoPanel?.Show(def);
        }

        private void RefreshAll()
        {
            if (!windowPanel.activeSelf) return;

            foreach (var slot in _slots) slot.Refresh();
            infoPanel?.Refresh();
        }
    }
}
