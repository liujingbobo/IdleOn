using UnityEngine;

namespace IdleOn.Characters
{
    /// <summary>
    /// Presentation-only correction for center-pivot, tightly cropped animation frames.
    /// Keeps the rendered sprite bottom aligned to the feet-root without moving gameplay,
    /// physics, or collider transforms.
    /// </summary>
    [DisallowMultipleComponent]
    public class SpriteFeetAligner : MonoBehaviour
    {
        [Header("References (auto-found on the 'Sprite' child if left empty)")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Transform visualTransform;

        [Header("Alignment")]
        [SerializeField] private float groundVisualOffset;

        private float _baseLocalX;
        private float _baseLocalZ;

        void Awake()
        {
            ResolveReferences();
            CacheBasePosition();
        }

        void OnEnable()
        {
            ResolveReferences();
            CacheBasePosition();
        }

        void LateUpdate()
        {
            AlignFeet();
        }

        private void ResolveReferences()
        {
            if (visualTransform == null)
                visualTransform = transform.Find("Sprite");

            if (spriteRenderer == null && visualTransform != null)
                spriteRenderer = visualTransform.GetComponent<SpriteRenderer>();
        }

        private void CacheBasePosition()
        {
            if (visualTransform == null) return;

            Vector3 localPosition = visualTransform.localPosition;
            _baseLocalX = localPosition.x;
            _baseLocalZ = localPosition.z;
        }

        private void AlignFeet()
        {
            if (spriteRenderer == null ||
                visualTransform == null ||
                spriteRenderer.sprite == null ||
                visualTransform == transform)
            {
                return;
            }

            float desiredBottomY = transform.position.y + groundVisualOffset;
            float worldDeltaY = desiredBottomY - spriteRenderer.bounds.min.y;

            Transform parent = visualTransform.parent;
            float localDeltaY = parent != null
                ? parent.InverseTransformVector(Vector3.up * worldDeltaY).y
                : worldDeltaY;

            Vector3 localPosition = visualTransform.localPosition;
            localPosition.x = _baseLocalX;
            localPosition.y += localDeltaY;
            localPosition.z = _baseLocalZ;
            visualTransform.localPosition = localPosition;
        }
    }
}
