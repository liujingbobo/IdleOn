using System.Collections.Generic;
using UnityEngine;
using TMPro;
using IdleOn.Core;
using IdleOn.Save;

namespace IdleOn.UI
{
    public class TalentWindow : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject  windowPanel;
        [SerializeField] private Transform   rowContainer;
        [SerializeField] private TalentRowUI rowPrefab;
        [SerializeField] private TMP_Text    pointsText;

        [Header("Debug")]
        [SerializeField] private bool    enableDebugKey = true;
        [SerializeField] private KeyCode debugOpenKey   = KeyCode.T;

        private readonly List<TalentRowUI> _rows = new List<TalentRowUI>();

        void Awake()
        {
            windowPanel.SetActive(false);
            GameEvents.OnTalentChanged += RefreshAll;
        }

        void OnDestroy() => GameEvents.OnTalentChanged -= RefreshAll;

        void Start() => PopulateRows();

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

        public void Close()  => windowPanel.SetActive(false);

        public void Toggle()
        {
            if (windowPanel.activeSelf) Close();
            else Open();
        }

        private void PopulateRows()
        {
            var db = GameDatabase.Instance?.Talents;
            if (db == null) return;

            foreach (var def in db.Talents)
            {
                if (def == null) continue;
                var row = Instantiate(rowPrefab, rowContainer);
                row.Initialize(def);
                _rows.Add(row);
            }
        }

        private void RefreshAll()
        {
            if (!windowPanel.activeSelf) return;

            if (pointsText != null)
            {
                int pts = SaveManager.Instance?.CurrentSave?.TalentPoints ?? 0;
                pointsText.text = $"Points: {pts}";
            }

            foreach (var row in _rows) row.Refresh();
        }
    }
}
