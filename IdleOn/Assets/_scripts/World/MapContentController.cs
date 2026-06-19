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

            // Move the persistent player to the current map's spawn.
            if (player != null)
            {
                foreach (var e in maps)
                {
                    if (e == null || e.MapId != mapId) continue;
                    var pos = player.position;
                    pos.x = e.PlayerSpawn.x;
                    pos.y = e.PlayerSpawn.y;
                    player.position = pos;
                    break;
                }
            }
        }
    }
}
