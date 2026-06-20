using System;
using UnityEngine;
using IdleOn.Core;

namespace IdleOn.World
{
    // Single-scene multi-map driver. One persistent Player plus a shared GroundLane / Canvas / systems;
    // each map's portals, enemies, and NPCs live under a map-root GameObject. Reacts to MapSystem's
    // OnMapChanged: activates only the current map root, moves the player to that map's spawn, and marks
    // every configured map's MapProgress unlocked so PortalInteractable.TravelTo succeeds. PortalGate
    // (quest-driven) remains the real gate on each portal. Does not modify MapSystem / PortalGate /
    // PortalInteractable / WorldInteractable.
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
            var ms = MapSystem.Instance;

            // Pre-unlock every configured map's progress so portals can travel; the quest-driven
            // PortalGate is what actually decides whether each portal is usable.
            if (ms != null)
            {
                foreach (var e in maps)
                {
                    if (e == null || string.IsNullOrEmpty(e.MapId)) continue;
                    var p = ms.GetProgress(e.MapId);
                    if (p != null) p.IsUnlocked = true;
                }
            }

            // Activate only the current map root.
            foreach (var e in maps)
            {
                if (e == null || e.Root == null) continue;
                e.Root.SetActive(e.MapId == mapId);
            }

            // Move the persistent player to the current map's spawn — near the portal back to the
            // previous map if one exists in the destination root, otherwise the configured default.
            if (player == null) return;

            Vector2? spawnPos = null;
            string previousMapId = ms?.PreviousMapId;
            if (!string.IsNullOrEmpty(previousMapId))
            {
                foreach (var e in maps)
                {
                    if (e == null || e.MapId != mapId || e.Root == null) continue;
                    var portal = FindBackPortal(e.Root, previousMapId);
                    if (portal != null) spawnPos = SpawnNearPortal(portal);
                    break;
                }
            }

            if (spawnPos == null)
            {
                foreach (var e in maps)
                {
                    if (e == null || e.MapId != mapId) continue;
                    spawnPos = e.PlayerSpawn;
                    break;
                }
            }

            if (spawnPos != null)
            {
                var pos = player.position;
                pos.x = spawnPos.Value.x;
                pos.y = spawnPos.Value.y;
                player.position = pos;
            }
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
