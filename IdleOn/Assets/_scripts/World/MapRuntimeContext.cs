using UnityEngine;

namespace IdleOn.World
{
    // Phase 1 foundation only — not wired into any system yet. Future map prefabs will carry
    // this component so spawned drops/projectiles/VFX can be parented under the active map's
    // runtime containers and cleaned up automatically when that map is unloaded/destroyed.
    // Mirrors GroundLane's self-registering Current pattern. Unused by DropManager,
    // PlayerCombatController, or any other system until that migration phase is approved.
    public class MapRuntimeContext : MonoBehaviour
    {
        public static MapRuntimeContext Current { get; private set; }

        [SerializeField] private Transform dropsRoot;
        [SerializeField] private Transform projectilesRoot;
        [SerializeField] private Transform vfxRoot;

        public Transform DropsRoot       => dropsRoot       != null ? dropsRoot       : transform;
        public Transform ProjectilesRoot => projectilesRoot != null ? projectilesRoot : transform;
        public Transform VFXRoot         => vfxRoot         != null ? vfxRoot         : transform;

        public void ClearDrops()
        {
            ClearRuntimeChildren(DropsRoot);
        }

        public void ClearProjectiles()
        {
            ClearRuntimeChildren(ProjectilesRoot);
        }

        void OnEnable()
        {
            Current = this;
        }

        // OnDisable also runs on destroy, so this covers both disabled and destroyed.
        void OnDisable()
        {
            if (Current == this) Current = null;
        }

        private static void ClearRuntimeChildren(Transform root)
        {
            if (root == null) return;

            for (int i = root.childCount - 1; i >= 0; i--)
            {
                var child = root.GetChild(i);
                if (child == null) continue;

                var drop = child.GetComponent<WorldDrop>();
                if (drop != null && DropManager.Instance != null)
                {
                    DropManager.Instance.RecycleForMapUnload(drop);
                    continue;
                }

                // Destroy is deferred in Play mode. Disable first so stale drops cannot still render
                // or receive clicks during the map-switch frame.
                child.gameObject.SetActive(false);
                Destroy(child.gameObject);
            }
        }
    }
}
