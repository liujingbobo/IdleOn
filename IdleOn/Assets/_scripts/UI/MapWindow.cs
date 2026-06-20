using System.Collections.Generic;
using UnityEngine;
using IdleOn.Core;

namespace IdleOn.UI
{
    public class MapWindow : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject windowPanel;
        [SerializeField] private Transform  rowContainer;
        [SerializeField] private MapRowUI   rowPrefab;
        [SerializeField] private MapWindowUI mapWindowUI;

        [Header("Debug")]
        [SerializeField] private bool    enableDebugKey = true;
        [SerializeField] private KeyCode debugOpenKey   = KeyCode.M;

        private readonly List<MapRowUI> _rows = new List<MapRowUI>();

        public bool IsOpen => windowPanel != null && windowPanel.activeSelf;

        void Awake()
        {
            if (windowPanel != null)
                windowPanel.SetActive(false);
            else
                Debug.LogWarning("[MapWindow] Window panel is not assigned.");

            GameEvents.OnMapChanged            += OnMapChanged;
            GameEvents.OnMapObjectiveCompleted += OnObjectiveCompleted;
        }

        void OnDestroy()
        {
            GameEvents.OnMapChanged            -= OnMapChanged;
            GameEvents.OnMapObjectiveCompleted -= OnObjectiveCompleted;
        }

        void Start()
        {
            if (mapWindowUI != null)
                mapWindowUI.Refresh();
            else
                PopulateRows();
        }

        void Update()
        {
            if (enableDebugKey && Input.GetKeyDown(debugOpenKey))
                Toggle();
        }

        public void Open()
        {
            if (windowPanel == null) return;
            windowPanel.SetActive(true);
            RefreshRows();
        }

        public void Close()
        {
            if (windowPanel != null)
                windowPanel.SetActive(false);
        }

        public void Toggle()
        {
            if (windowPanel == null) return;
            if (windowPanel.activeSelf) Close();
            else Open();
        }

        private void PopulateRows()
        {
            var db = GameDatabase.Instance?.Maps;
            if (db == null || rowContainer == null || rowPrefab == null) return;

            foreach (var mapDef in db.Maps)
            {
                if (mapDef == null) continue;
                var row = Instantiate(rowPrefab, rowContainer);
                row.Initialize(mapDef);
                _rows.Add(row);
            }
        }

        private void RefreshRows()
        {
            if (mapWindowUI != null)
                mapWindowUI.Refresh();

            foreach (var row in _rows) row.Refresh();
        }

        private void OnMapChanged(string mapId)
        {
            if (windowPanel.activeSelf) RefreshRows();
        }

        private void OnObjectiveCompleted(string mapId)
        {
            if (windowPanel.activeSelf) RefreshRows();
        }
    }
}
