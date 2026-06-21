using System.Collections;
using UnityEngine;
using IdleOn.Combat;

namespace IdleOn.Characters
{
    /// <summary>
    /// Presentation-only driver. Reads <see cref="PlayerCombatController.State"/> and movement
    /// intent, then sets Animator parameters on the Sprite child. Position delta is only used
    /// for facing and as a fallback if the controller is unavailable.
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
        [SerializeField, Min(0f)] private float hitFlashDuration = 0.1f;

        [Header("Debug")]
        [SerializeField] private bool debugFacing;

        private PlayerCombatController _controller;
        private Vector3 _lastPosition;
        private Color _originalSpriteColor;
        private bool _spriteColorCached;
        private Coroutine _hitFlashRoutine;

        private static readonly int IsMovingHash    = Animator.StringToHash("IsMoving");
        private static readonly int IsAttackingHash = Animator.StringToHash("IsAttacking");
        private static readonly int IsDeadHash      = Animator.StringToHash("IsDead");
        private static readonly int HurtHash        = Animator.StringToHash("Hurt");

        public float FacingDirectionX
        {
            get
            {
                bool facingLeft = spriteRenderer != null &&
                                  (spriteRenderer.flipX ^ invertFacing);
                return facingLeft ? -1f : 1f;
            }
        }

        void Awake()
        {
            _controller = GetComponent<PlayerCombatController>();

            var sprite = transform.Find("Sprite");
            if (animator == null && sprite != null)       animator       = sprite.GetComponent<Animator>();
            if (spriteRenderer == null && sprite != null) spriteRenderer = sprite.GetComponent<SpriteRenderer>();

            if (spriteRenderer != null)
            {
                _originalSpriteColor = spriteRenderer.color;
                _spriteColorCached = true;
            }

            _lastPosition = transform.position;
        }

        void OnDisable()
        {
            StopHitFlash();
        }

        void Update()
        {
            if (animator == null) return;

            Vector3 pos       = transform.position;
            float   dx        = pos.x - _lastPosition.x;
            float   moved     = (pos - _lastPosition).magnitude;
            _lastPosition     = pos;
            float   speed     = Time.deltaTime > 0f ? moved / Time.deltaTime : 0f;

            if (_controller != null && _controller.IsDead)
            {
                StopHitFlash();
                animator.SetBool(IsDeadHash, true);
                animator.SetBool(IsMovingHash, false);
                animator.SetBool(IsAttackingHash, false);
                return;   // no facing changes while dead
            }

            animator.SetBool(IsDeadHash, false);

            bool        moving    = _controller != null
                ? _controller.IsMoving
                : speed > moveSpeedThreshold;
            CombatState state     = _controller.State;
            bool        attacking = !moving &&
                                    (state == CombatState.Attacking || state == CombatState.ManualAttack);

            animator.SetBool(IsMovingHash, moving);
            animator.SetBool(IsAttackingHash, attacking);

            // During attacks, face the target even while stationary. Otherwise face travel direction.
            Transform facingTarget = _controller.FacingTarget;
            if (facingTarget != null)
                ApplyFacing(facingTarget.position.x - transform.position.x, speed, state, "target");
            else if (moving)
                ApplyFacing(dx, speed, state, "movement");
        }

        // Called by PlayerCombatController on non-lethal damage. Animator's AnyState->Hurt
        // transition is gated on IsDead==false, so this is a no-op once the player is dead.
        public void PlayHurt()
        {
            if (animator != null)
                animator.SetTrigger(HurtHash);

            RestartHitFlash();
        }

        private void RestartHitFlash()
        {
            StopHitFlash();
            if (spriteRenderer == null || hitFlashDuration <= 0f) return;

            spriteRenderer.color = new Color(1f, 0f, 0f, _originalSpriteColor.a);
            _hitFlashRoutine = StartCoroutine(RestoreColorAfterDelay());
        }

        private IEnumerator RestoreColorAfterDelay()
        {
            yield return new WaitForSeconds(hitFlashDuration);
            _hitFlashRoutine = null;
            RestoreSpriteColor();
        }

        private void StopHitFlash()
        {
            if (_hitFlashRoutine != null)
            {
                StopCoroutine(_hitFlashRoutine);
                _hitFlashRoutine = null;
            }

            RestoreSpriteColor();
        }

        private void RestoreSpriteColor()
        {
            if (_spriteColorCached && spriteRenderer != null)
                spriteRenderer.color = _originalSpriteColor;
        }

        private void ApplyFacing(float dx, float speed, CombatState state, string source)
        {
            if (spriteRenderer == null || Mathf.Abs(dx) <= 0.0001f) return;

            bool oldFlip = spriteRenderer.flipX;
            bool newFlip = (dx < 0f) ^ invertFacing;
            if (oldFlip == newFlip) return;

            if (debugFacing)
                Debug.Log($"[PlayerAnimatorDriver] flipX {oldFlip}->{newFlip} source={source} dx={dx:0.####} speed={speed:0.###} state={state}", this);
            spriteRenderer.flipX = newFlip;
        }
    }
}
