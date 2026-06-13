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
            if (_spriteRenderer == null)
                Debug.LogWarning("[WorldDrop] No SpriteRenderer found in children — icon will never display.", this);
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

            if (_spriteRenderer == null)
            {
                Debug.LogWarning("[WorldDrop] Setup: _spriteRenderer is null — cannot display icon.", this);
                return;
            }

            if (icon == null)
                Debug.LogWarning($"[WorldDrop] Setup: icon is null for '{entry?.ItemId ?? entry?.CurrencyType.ToString()}' — sprite will be blank.", this);

            _spriteRenderer.sprite  = icon;
            _spriteRenderer.enabled = true;
        }

        // Called by DropManager when collection fails (e.g. inventory full).
        // Suppresses re-attempts for a short window to avoid per-frame spam.
        public void OnCollectionFailed()
        {
            _collectionCooldown = 0.5f;
        }
    }
}
