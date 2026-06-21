using System;
using UnityEngine;
using IdleOn.Core;

namespace IdleOn.World
{
    // Single-scene hybrid map driver. A MapDefinition prefab is instantiated when assigned; maps
    // without one continue using their existing baked scene roots. One persistent Player plus the
    // shared GroundLane / Canvas / systems remain outside map content. PortalGate (quest-driven)
    // remains the real destination gate.
    public class MapContentController : MonoBehaviour
    {
        [Serializable]
        public class MapEntry
        {
            public string     MapId;
            public GameObject Root;
            public Vector2    PlayerSpawn = new Vector2(0f, -2f);
        }

        [SerializeField] private MapEntry[] maps = new MapEntry[0];
        [SerializeField] private Transform  player;

        private string     _activeMapId;
        private GameObject _activeContentRoot;
        private GameObject _activePrefabInstance;

        void OnEnable()  => GameEvents.OnMapChanged += HandleMapChanged;
        void OnDisable() => GameEvents.OnMapChanged -= HandleMapChanged;

        void Start()
        {
            // If MapSystem initialized before this component enabled, sync to the current map now.
            var ms = MapSystem.Instance;
            if (ms != null && !string.IsNullOrEmpty(ms.CurrentMapId))
                HandleMapChanged(ms.CurrentMapId);
        }

        private void HandleMapChanged(string mapId)
        {
            var mapSystem = MapSystem.Instance;
            var entry = FindEntry(mapId);
            if (entry == null)
            {
                Debug.LogWarning($"[MapContentController] No map entry configured for '{mapId}'.", this);
                return;
            }

            // Keep the legacy MapProgress travel flags available. PortalGate remains the real
            // destination-requirement gate and MapSystem remains unchanged.
            if (mapSystem != null)
            {
                foreach (var configuredEntry in maps)
                {
                    if (configuredEntry == null || string.IsNullOrEmpty(configuredEntry.MapId)) continue;
                    var progress = mapSystem.GetProgress(configuredEntry.MapId);
                    if (progress != null) progress.IsUnlocked = true;
                }
            }

            // Repeated initialization/map events for the same destination reuse the current content.
            if (_activeMapId != mapId || _activeContentRoot == null)
                SwitchContent(entry);
            else if (!_activeContentRoot.activeSelf)
                _activeContentRoot.SetActive(true);

            PlacePlayer(entry, mapSystem);
        }

        private void SwitchContent(MapEntry entry)
        {
            ClearActiveMapRuntimeObjects();
            UnloadActivePrefab();

            // Baked roots are fallback content and are never destroyed.
            foreach (var configuredEntry in maps)
            {
                if (configuredEntry == null || configuredEntry.Root == null) continue;
                configuredEntry.Root.SetActive(false);
            }

            GameObject contentRoot = null;
            var mapDef = GameDatabase.Instance?.Maps?.GetMap(entry.MapId);
            if (mapDef != null && mapDef.MapPrefab != null)
            {
                try
                {
                    _activePrefabInstance = Instantiate(mapDef.MapPrefab);
                    if (_activePrefabInstance != null)
                    {
                        _activePrefabInstance.SetActive(true);
                        contentRoot = _activePrefabInstance;
                    }
                    else
                    {
                        Debug.LogWarning(
                            $"[MapContentController] Prefab instantiation returned null for " +
                            $"'{entry.MapId}'; using baked fallback.",
                            this);
                    }
                }
                catch (Exception exception)
                {
                    Debug.LogWarning(
                        $"[MapContentController] Failed to instantiate prefab for '{entry.MapId}'; " +
                        $"using baked fallback. {exception.Message}",
                        this);
                    _activePrefabInstance = null;
                }
            }

            if (contentRoot == null)
            {
                if (entry.Root != null)
                {
                    entry.Root.SetActive(true);
                    contentRoot = entry.Root;
                }
                else
                {
                    Debug.LogWarning(
                        $"[MapContentController] Map '{entry.MapId}' has neither a usable prefab " +
                        "nor a baked fallback root.",
                        this);
                }
            }

            _activeMapId = contentRoot != null ? entry.MapId : null;
            _activeContentRoot = contentRoot;
        }

        private void ClearActiveMapRuntimeObjects()
        {
            if (_activeContentRoot == null) return;

            var context = _activeContentRoot.GetComponent<MapRuntimeContext>();
            if (context == null) return;

            context.ClearDrops();
            context.ClearProjectiles();
        }

        private void UnloadActivePrefab()
        {
            if (_activePrefabInstance == null) return;

            // Destroy is deferred in Play mode. Disable first so the source map stops participating
            // before the destination map activates in the same frame.
            _activePrefabInstance.SetActive(false);
            Destroy(_activePrefabInstance);
            _activePrefabInstance = null;
            _activeContentRoot = null;
            _activeMapId = null;
        }

        private void PlacePlayer(MapEntry entry, MapSystem mapSystem)
        {
            if (player == null || _activeContentRoot == null) return;

            Vector2? spawnPos = null;
            string previousMapId = mapSystem?.PreviousMapId;
            if (!string.IsNullOrEmpty(previousMapId))
            {
                var portal = FindBackPortal(_activeContentRoot, previousMapId);
                if (portal != null) spawnPos = SpawnNearPortal(portal);
            }

            if (spawnPos == null)
                spawnPos = entry.PlayerSpawn;

            var pos = player.position;
            pos.x = spawnPos.Value.x;
            pos.y = spawnPos.Value.y;
            player.position = pos;
        }

        private MapEntry FindEntry(string mapId)
        {
            foreach (var entry in maps)
                if (entry != null && entry.MapId == mapId)
                    return entry;
            return null;
        }

        // Searches the destination root for a PortalInteractable whose own DestinationMapId points
        // back to the map we just came from. PortalInteractable stays travel-only — this only reads
        // its existing DestinationMapId, never adds fields to it.
        private static PortalInteractable FindBackPortal(GameObject root, string previousMapId)
        {
            foreach (var portal in root.GetComponentsInChildren<PortalInteractable>(true))
                if (portal.DestinationMapId == previousMapId) return portal;
            return null;
        }

        // Small offset toward the map's interior (away from the edge the portal sits on) so the
        // player doesn't spawn exactly on top of the portal collider. Clamped to the lane bounds.
        private static Vector2 SpawnNearPortal(PortalInteractable portal)
        {
            const float offset = 1.5f;
            float px = portal.transform.position.x;
            float candidate = Mathf.Approximately(px, 0f) ? px + offset : px - Mathf.Sign(px) * offset;

            var lane = GroundLane.Current;
            float x = lane != null ? lane.ClampX(candidate) : candidate;
            float y = lane != null ? lane.GroundY : portal.transform.position.y;
            return new Vector2(x, y);
        }
    }
}
