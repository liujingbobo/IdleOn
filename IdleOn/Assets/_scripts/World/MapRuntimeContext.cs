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

        void OnEnable()
        {
            Current = this;
        }

        // OnDisable also runs on destroy, so this covers both disabled and destroyed.
        void OnDisable()
        {
            if (Current == this) Current = null;
        }
    }
}
