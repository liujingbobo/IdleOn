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

        // Source map of the most recent explicit TravelTo, for MapContentController's
        // spawn-near-back-portal logic. Empty/null on fresh load/teleport/debug-open — no prior
        // travel happened, so the default spawn is used instead.
        public string PreviousMapId { get; private set; }

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

            // Fresh/loaded session has no prior in-session travel — never carry over a stale
            // PreviousMapId from an earlier character/session (would only matter if the new
            // CurrentMapId happens to have a portal back to that stale id, but it's still wrong).
            PreviousMapId = null;

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

            PreviousMapId = CurrentMapId;
            CurrentMapId  = mapId;

            var save = SaveManager.Instance?.CurrentSave;
            if (save != null) save.CurrentMapId = mapId;

            GameEvents.RaiseMapChanged(mapId);
        }

        // Kill counting per current map is kept (read by MapRowUI), but no longer grants rewards or
        // auto-unlocks the next map — portal unlocking now lives entirely in PortalGate, driven by the
        // destination MapDefinition's own unlock requirements. See PortalGate.cs.
        private void HandleEnemyKilled(string enemyId, float xp)
        {
            var mapDef = CurrentMapDef;
            if (mapDef == null) return;

            var prog = GetProgress(CurrentMapId);
            if (prog == null) return;

            prog.KillCount++;
        }
    }
}
