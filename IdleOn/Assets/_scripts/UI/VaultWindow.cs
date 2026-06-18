using System.Collections.Generic;
using UnityEngine;
using IdleOn.Core;
using IdleOn.Items;

namespace IdleOn.UI
{
    public class VaultWindow : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject        windowPanel;
        [SerializeField] private Transform         rowContainer;
        [SerializeField] private VaultUpgradeRowUI rowPrefab;

        // TEMPORARY: debug key — remove once MainUI buttons call Open() / Toggle()
        [Header("Debug (remove once MainUI is wired)")]
        [SerializeField] private bool    enableDebugKey = true;
        [SerializeField] private KeyCode debugOpenKey   = KeyCode.V;

        private readonly List<VaultUpgradeRowUI> _rows = new List<VaultUpgradeRowUI>();

        public bool IsOpen => windowPanel.activeSelf;

        void Awake()
        {
            windowPanel.SetActive(false);
            GameEvents.OnVaultChanged    += RefreshAllRows;
            GameEvents.OnCurrencyChanged += OnCurrencyChanged;
        }

        void OnDestroy()
        {
            GameEvents.OnVaultChanged    -= RefreshAllRows;
            GameEvents.OnCurrencyChanged -= OnCurrencyChanged;
        }

        void Start()
        {
            PopulateRows();
        }

        void Update()
        {
            if (enableDebugKey && Input.GetKeyDown(debugOpenKey))
                Toggle();
        }

        public void Open()
        {
            windowPanel.SetActive(true);
            RefreshAllRows();
        }

        public void Close()
        {
            windowPanel.SetActive(false);
        }

        public void Toggle()
        {
            if (windowPanel.activeSelf) Close();
            else Open();
        }

        private void PopulateRows()
        {
            var db = GameDatabase.Instance?.Vault;
            if (db == null) return;

            foreach (var def in db.Upgrades)
            {
                if (def == null) continue;
                var row = Instantiate(rowPrefab, rowContainer);
                row.Initialize(def);
                _rows.Add(row);
            }
        }

        private void RefreshAllRows()
        {
            if (!windowPanel.activeSelf) return;
            foreach (var row in _rows)
                row.Refresh();
        }

        private void OnCurrencyChanged(CurrencyType type, long newTotal)
        {
            if (type == CurrencyType.Silver)
                RefreshAllRows();
        }
    }
}
