using UnityEngine;
using IdleOn.Loot;

namespace IdleOn.World
{
    [RequireComponent(typeof(CircleCollider2D))]
    public class WorldDrop : MonoBehaviour
    {
        private LootResultEntry _entry;
        private SpriteRenderer  _spriteRenderer;
        private float           _collectionCooldown;

        public LootResultEntry Entry          => _entry;
        public bool            CanBeCollected => _collectionCooldown <= 0f;

        void Awake()
        {
            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        void Update()
        {
            if (_collectionCooldown > 0f)
                _collectionCooldown -= Time.deltaTime;
        }

        public void Setup(LootResultEntry entry, Sprite icon)
        {
            _entry              = entry;
            _collectionCooldown = 0f;

            if (_spriteRenderer != null)
                _spriteRenderer.sprite = icon;
        }

        // Called by DropManager when collection fails (e.g. inventory full).
        // Suppresses re-attempts for a short window to avoid per-frame spam.
        public void OnCollectionFailed()
        {
            _collectionCooldown = 0.5f;
        }
    }
}
