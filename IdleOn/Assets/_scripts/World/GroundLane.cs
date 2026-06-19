using UnityEngine;

namespace IdleOn.World
{
    // Phase 1 single-lane ground reference. Characters' feet (root) are forced to GroundY,
    // and X is clamped to [MinX, MaxX]. No gravity, no multi-lane graph yet — one lane per scene.
    public class GroundLane : MonoBehaviour
    {
        public static GroundLane Current { get; private set; }

        [SerializeField] private float groundY = -2f;
        [SerializeField] private float minX    = -10f;
        [SerializeField] private float maxX    =  10f;

        public float GroundY => groundY;
        public float MinX    => minX;
        public float MaxX    => maxX;

        public float ClampX(float x) => Mathf.Clamp(x, minX, maxX);

        void OnEnable()
        {
            if (Current != null && Current != this) { Destroy(this); return; }
            Current = this;
        }

        // OnDisable also runs on destroy, so this covers both disabled and destroyed.
        void OnDisable()
        {
            if (Current == this) Current = null;
        }
    }
}
