using System;
using UnityEngine;
using UnityEngine.UI;
using IdleOn.Core;
using IdleOn.Quests;
using IdleOn.World;

namespace IdleOn.UI
{
    // Display/travel binding for the hand-authored map points in TestCombat.
    // Unlock requirements remain owned by the destination MapDefinition.
    public class MapWindowUI : MonoBehaviour
    {
        [Serializable]
        public class MapPoint
        {
            public string MapId;
            public Button Button;
            public GameObject Current;
            public GameObject Locked;

            [NonSerialized] public UnityEngine.Events.UnityAction ClickHandler;
        }

        [SerializeField] private MapPoint[] points = new MapPoint[0];

        void Awake()
        {
            foreach (var point in points)
            {
                if (point == null || point.Button == null) continue;
                MapPoint captured = point;
                point.ClickHandler = () => TravelTo(captured);
                point.Button.onClick.AddListener(point.ClickHandler);
            }
        }

        void OnEnable()
        {
            GameEvents.OnMapChanged += HandleMapChanged;
            GameEvents.OnQuestChanged += HandleQuestChanged;
            GameEvents.OnQuestCompleted += HandleQuestChanged;
            GameEvents.OnFeaturesChanged += Refresh;
            GameEvents.OnPersistentProgressLoaded += Refresh;
            GameEvents.OnEnemyKilled += HandleEnemyKilled;
            Refresh();
        }

        void Start() => Refresh();

        void OnDisable()
        {
            GameEvents.OnMapChanged -= HandleMapChanged;
            GameEvents.OnQuestChanged -= HandleQuestChanged;
            GameEvents.OnQuestCompleted -= HandleQuestChanged;
            GameEvents.OnFeaturesChanged -= Refresh;
            GameEvents.OnPersistentProgressLoaded -= Refresh;
            GameEvents.OnEnemyKilled -= HandleEnemyKilled;
        }

        void OnDestroy()
        {
            foreach (var point in points)
            {
                if (point?.Button == null || point.ClickHandler == null) continue;
                point.Button.onClick.RemoveListener(point.ClickHandler);
            }
        }

        public void Refresh()
        {
            string currentMapId = MapSystem.Instance?.CurrentMapId;

            foreach (var point in points)
            {
                if (point == null) continue;

                bool unlocked = IsUnlocked(point.MapId);
                bool isCurrent = unlocked && point.MapId == currentMapId;

                if (point.Button != null)
                    point.Button.interactable = unlocked && !isCurrent;
                if (point.Current != null)
                    point.Current.SetActive(isCurrent);
                if (point.Locked != null)
                    point.Locked.SetActive(!unlocked);
            }
        }

        private bool IsUnlocked(string mapId)
        {
            if (string.IsNullOrEmpty(mapId) || !HasMapContent(mapId))
                return false;

            if (mapId == "grassland_1")
                return true;

            var mapDef = GameDatabase.Instance?.Maps?.GetMap(mapId);
            if (mapDef == null)
                return false;

            bool questOk = string.IsNullOrEmpty(mapDef.UnlockQuestId)
                           || (QuestSystem.Instance != null
                               && QuestSystem.Instance.IsCompleted(mapDef.UnlockQuestId));
            bool killOk = string.IsNullOrEmpty(mapDef.UnlockEnemyId)
                          || (EnemyKillTracker.Instance != null
                              && EnemyKillTracker.Instance.GetKillCount(mapDef.UnlockEnemyId)
                              >= mapDef.UnlockKillCount);
            return questOk && killOk;
        }

        private static bool HasMapContent(string mapId)
        {
            var mapDef = GameDatabase.Instance?.Maps?.GetMap(mapId);
            if (mapDef != null && mapDef.MapPrefab != null)
                return true;

            string expectedRoot = "Map_" + mapId;
            var roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
            foreach (var root in roots)
                if (root != null && root.name == expectedRoot)
                    return true;
            return false;
        }

        private void TravelTo(MapPoint point)
        {
            if (point == null || !IsUnlocked(point.MapId))
            {
                Refresh();
                return;
            }

            var mapSystem = MapSystem.Instance;
            if (mapSystem == null)
            {
                Debug.LogWarning("[MapWindowUI] MapSystem is unavailable; travel was ignored.");
                Refresh();
                return;
            }

            if (mapSystem.CurrentMapId == point.MapId)
            {
                Refresh();
                return;
            }

            string previousMapId = mapSystem.CurrentMapId;
            mapSystem.TravelTo(point.MapId);
            if (mapSystem.CurrentMapId == previousMapId)
                Debug.LogWarning($"[MapWindowUI] Travel to '{point.MapId}' was rejected by MapSystem.");

            Refresh();
        }

        private void HandleMapChanged(string mapId) => Refresh();
        private void HandleQuestChanged(string questId) => Refresh();
        private void HandleEnemyKilled(string enemyId, float xp) => Refresh();
    }
}
