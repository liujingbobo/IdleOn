using UnityEngine;

namespace IdleOn.Enemies
{
    /// <summary>
    /// Presentation-only driver. Reads <see cref="EnemyController.State"/> and movement intent,
    /// then sets Animator parameters on the Sprite child. Position delta is only used for facing
    /// and as a fallback if the controller is unavailable. Contains no gameplay logic.
    /// </summary>
    [RequireComponent(typeof(EnemyController))]
    public class EnemyAnimatorDriver : MonoBehaviour
    {
        [Header("References (auto-found on the 'Sprite' child if left empty)")]
        [SerializeField] private Animator animator;
        [SerializeField] private SpriteRenderer spriteRenderer;

        [Header("Config")]
        [Tooltip("Minimum speed (units/sec) to count as moving. Frame-rate independent.")]
        [SerializeField] private float moveSpeedThreshold = 0.05f;
        [Tooltip("Enable if the sprite art faces LEFT by default (flips the facing logic). No rotate/scale.")]
        [SerializeField] private bool invertFacing = false;

        [Header("Debug")]
        [SerializeField] private bool debugFacing;

        private EnemyController _controller;
        private Vector3 _lastPosition;

        private static readonly int IsMovingHash    = Animator.StringToHash("IsMoving");
        private static readonly int IsAttackingHash = Animator.StringToHash("IsAttacking");
        private static readonly int IsDeadHash      = Animator.StringToHash("IsDead");
        private static readonly int HurtHash        = Animator.StringToHash("Hurt");

        void Awake()
        {
            _controller = GetComponent<EnemyController>();

            var sprite = transform.Find("Sprite");
            if (animator == null && sprite != null)       animator       = sprite.GetComponent<Animator>();
            if (spriteRenderer == null && sprite != null) spriteRenderer = sprite.GetComponent<SpriteRenderer>();

            _lastPosition = transform.position;
        }

        // Reset the baseline so a pooled respawn (repositioned + re-enabled) does not
        // register a one-frame movement spike.
        void OnEnable()
        {
            _lastPosition = transform.position;
        }

        void Update()
        {
            if (animator == null) return;

            Vector3 pos      = transform.position;
            float   dx       = pos.x - _lastPosition.x;
            float   moved    = (pos - _lastPosition).magnitude;
            _lastPosition    = pos;

            float   speed    = Time.deltaTime > 0f ? moved / Time.deltaTime : 0f;
            EnemyState state = _controller.State;

            if (state == EnemyState.Dead)
            {
                animator.SetBool(IsDeadHash, true);
                animator.SetBool(IsMovingHash, false);
                animator.SetBool(IsAttackingHash, false);
                return;   // no facing changes while dead
            }

            animator.SetBool(IsDeadHash, false);

            bool moving = _controller != null
                ? _controller.IsMoving
                : speed > moveSpeedThreshold;
            // Patrol always moves; in Combat, standing still means attacking in range.
            bool attacking = !moving && state == EnemyState.Combat;

            animator.SetBool(IsMovingHash, moving);
            animator.SetBool(IsAttackingHash, attacking);

            // In combat, face the player even while stationary. Otherwise face travel direction.
            Transform playerTarget = _controller.PlayerTarget;
            if (state == EnemyState.Combat && playerTarget != null)
                ApplyFacing(playerTarget.position.x - transform.position.x, speed, state, "target");
            else if (moving)
                ApplyFacing(dx, speed, state, "movement");
        }

        // Called by EnemyController on non-lethal damage. AnyState->Hurt is gated on IsDead==false
        // in SlimeAnimator.controller, so this is a no-op once the slime is dead.
        public void PlayHurt()
        {
            if (animator != null)
                animator.SetTrigger(HurtHash);
        }

        private void ApplyFacing(float dx, float speed, EnemyState state, string source)
        {
            if (spriteRenderer == null || Mathf.Abs(dx) <= 0.0001f) return;

            bool oldFlip = spriteRenderer.flipX;
            bool newFlip = (dx < 0f) ^ invertFacing;
            if (oldFlip == newFlip) return;

            if (debugFacing)
                Debug.Log($"[EnemyAnimatorDriver] {name}: flipX {oldFlip}->{newFlip} source={source} dx={dx:0.####} speed={speed:0.###} state={state}", this);
            spriteRenderer.flipX = newFlip;
        }
    }
}
