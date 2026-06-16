using UnityEngine;
using IdleOn.Combat;

namespace IdleOn.Characters
{
    /// <summary>
    /// Presentation-only driver. Reads <see cref="PlayerCombatController.State"/> and the
    /// per-frame position delta, then sets Animator parameters on the Sprite child.
    /// Contains no gameplay logic and never writes back to the combat controller.
    /// </summary>
    [RequireComponent(typeof(PlayerCombatController))]
    public class PlayerAnimatorDriver : MonoBehaviour
    {
        [Header("References (auto-found on the 'Sprite' child if left empty)")]
        [SerializeField] private Animator animator;
        [SerializeField] private SpriteRenderer spriteRenderer;

        [Header("Config")]
        [Tooltip("Minimum speed (units/sec) to count as moving. Frame-rate independent.")]
        [SerializeField] private float moveSpeedThreshold = 0.05f;
        [Tooltip("Enable if the sprite art faces LEFT by default (flips the facing logic). No rotate/scale.")]
        [SerializeField] private bool invertFacing = false;

        private PlayerCombatController _controller;
        private Vector3 _lastPosition;

        private static readonly int IsMovingHash   = Animator.StringToHash("IsMoving");
        private static readonly int IsAttackingHash = Animator.StringToHash("IsAttacking");

        void Awake()
        {
            _controller = GetComponent<PlayerCombatController>();

            var sprite = transform.Find("Sprite");
            if (animator == null && sprite != null)       animator       = sprite.GetComponent<Animator>();
            if (spriteRenderer == null && sprite != null) spriteRenderer = sprite.GetComponent<SpriteRenderer>();

            _lastPosition = transform.position;
        }

        void Update()
        {
            if (animator == null) return;

            Vector3 pos       = transform.position;
            float   dx        = pos.x - _lastPosition.x;
            float   moved     = (pos - _lastPosition).magnitude;
            _lastPosition     = pos;

            float       speed     = Time.deltaTime > 0f ? moved / Time.deltaTime : 0f;
            bool        moving    = speed > moveSpeedThreshold;
            CombatState state     = _controller.State;
            bool        attacking = !moving &&
                                    (state == CombatState.Attacking || state == CombatState.ManualAttack);

            animator.SetBool(IsMovingHash, moving);
            animator.SetBool(IsAttackingHash, attacking);

            // Face the direction of travel (presentation only; clips animate the sprite, not flipX).
            // invertFacing supports art whose default orientation faces left.
            if (spriteRenderer != null && moving && Mathf.Abs(dx) > 0.0001f)
                spriteRenderer.flipX = (dx < 0f) ^ invertFacing;
        }
    }
}
