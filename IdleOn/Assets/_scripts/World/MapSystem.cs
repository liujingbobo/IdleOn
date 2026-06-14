using System.Collections.Generic;
using UnityEngine;
using IdleOn.Core;
using IdleOn.Save;
using IdleOn.Items;
using IdleOn.Inventory;

namespace IdleOn.World
{
    public class MapSystem : MonoBehaviour
    {
        public static MapSystem Instance { get; private set; }

        public string        CurrentMapId  { get; private set; } = "grassland_1";
        public MapDefinition CurrentMapDef => GameDatabase.Instance?.Maps?.GetMap(CurrentMapId);

        private List<MapProgressData> _progress = new List<MapProgressData>();

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        void OnEnable()  => GameEvents.OnEnemyKilled += HandleEnemyKilled;

        void OnDisable()
        {
            GameEvents.OnEnemyKilled -= HandleEnemyKilled;
            SaveManager.OnSaveLoaded -= Initialize;
        }

        void Start()
        {
            if (SaveManager.Instance != null && SaveManager.Instance.IsLoaded)
                Initialize();
            else
                SaveManager.OnSaveLoaded += Initialize;
        }

        private void Initialize()
        {
            var save = SaveManager.Instance?.CurrentSave;
            if (save == null) return;

            _progress = save.MapProgress ?? new List<MapProgressData>();

            // Ensure every map in the database has a progress entry
            var db = GameDatabase.Instance?.Maps;
            if (db != null)
            {
                foreach (var mapDef in db.Maps)
                {
                    if (mapDef == null) continue;
                    if (GetProgress(mapDef.MapId) == null)
                        _progress.Add(new MapProgressData { MapId = mapDef.MapId });
                }
            }

            // grassland_1 is always unlocked
            var g1 = GetProgress("grassland_1");
            if (g1 != null) g1.IsUnlocked = true;

            string savedId = save.CurrentMapId;
            bool validId  = !string.IsNullOrEmpty(savedId) && GetProgress(savedId) != null;
            CurrentMapId  = validId ? savedId : "grassland_1";
            save.MapProgress = _progress;

            GameEvents.RaiseMapChanged(CurrentMapId);
        }

        public IReadOnlyList<MapProgressData> AllProgress => _progress;

        public MapProgressData GetProgress(string mapId)
        {
            foreach (var p in _progress)
                if (p.MapId == mapId) return p;
            return null;
        }

        public void TravelTo(string mapId)
        {
            var prog = GetProgress(mapId);
            if (prog == null || !prog.IsUnlocked || mapId == CurrentMapId) return;

            CurrentMapId = mapId;

            var save = SaveManager.Instance?.CurrentSave;
            if (save != null) save.CurrentMapId = mapId;

            GameEvents.RaiseMapChanged(mapId);
        }

        private void HandleEnemyKilled(string enemyId, float xp)
        {
            var mapDef = CurrentMapDef;
            if (mapDef == null) return;

            var prog = GetProgress(CurrentMapId);
            if (prog == null || prog.IsComplete) return;

            bool counts = string.IsNullOrEmpty(mapDef.ObjectiveEnemyId)
                       || mapDef.ObjectiveEnemyId == enemyId;
            if (!counts) return;

            prog.KillCount++;

            GameEvents.RaiseMapObjectiveProgress(prog.KillCount, mapDef.KillObjective);

            if (prog.KillCount >= mapDef.KillObjective)
                CompleteObjective(mapDef, prog);
        }

        private void CompleteObjective(MapDefinition mapDef, MapProgressData prog)
        {
            prog.IsComplete = true;

            if (!string.IsNullOrEmpty(mapDef.UnlocksMapId))
            {
                var next = GetProgress(mapDef.UnlocksMapId);
                if (next == null)
                {
                    next = new MapProgressData { MapId = mapDef.UnlocksMapId };
                    _progress.Add(next);
                }
                next.IsUnlocked = true;
            }

            if (mapDef.SilverReward > 0)
                CurrencySystem.Instance?.Add(CurrencyType.Silver, mapDef.SilverReward);

            GameEvents.RaiseMapObjectiveCompleted(mapDef.MapId);

            Debug.Log($"[MapSystem] {mapDef.DisplayName} complete! +{mapDef.SilverReward} Silver | Unlocks: {mapDef.UnlocksMapId}");
        }
    }
}
