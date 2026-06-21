using System.Collections.Generic;
using UnityEngine;
using IdleOn.Loot;
using IdleOn.Items;
using IdleOn.Inventory;
using IdleOn.Core;
using IdleOn.Vault;
using IdleOn.Talents;
using IdleOn.UI;

namespace IdleOn.World
{
    public class DropManager : MonoBehaviour
    {
        public static DropManager Instance { get; private set; }

        [Header("References")]
        [SerializeField] private WorldDrop dropPrefab;
        [SerializeField] private PickupFlyEffectController pickupFlyEffect;

        [Header("Pool")]
        [SerializeField] private int preWarmCount = 10;

        [Header("Spawn Layout")]
        [SerializeField, Min(0f)] private float dropSpacing = 0.4f;

        private readonly Queue<WorldDrop> _pool = new Queue<WorldDrop>();

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        void Start()
        {
            for (int i = 0; i < preWarmCount; i++)
                _pool.Enqueue(CreateInstance());
        }

        // ── Spawning ─────────────────────────────────────────────────────────

        public void Spawn(LootResult result, Vector2 origin)
        {
            if (result == null || result.IsEmpty) return;

            int count = result.Entries.Count;
            var lane  = GroundLane.Current;
            float groundY = lane != null ? lane.GroundY : origin.y;
            float halfSpan = (count - 1) * dropSpacing * 0.5f;
            float centerX = origin.x;

            if (lane != null)
            {
                float laneHalfWidth = (lane.MaxX - lane.MinX) * 0.5f;
                centerX = halfSpan <= laneHalfWidth
                    ? Mathf.Clamp(centerX, lane.MinX + halfSpan, lane.MaxX - halfSpan)
                    : (lane.MinX + lane.MaxX) * 0.5f;
            }

            for (int index = 0; index < count; index++)
            {
                var entry = result.Entries[index];
                var drop = GetFromPool();
                var icon = ResolveIcon(entry);
                drop.Setup(entry, icon);
                float offsetX = (index - (count - 1) * 0.5f) * dropSpacing;
                drop.transform.position = new Vector3(centerX + offsetX, groundY, 0f);
                drop.gameObject.SetActive(true);
            }
        }

        // ── Collection ───────────────────────────────────────────────────────

        public void Collect(WorldDrop drop)
        {
            if (drop == null || !drop.CanBeCollected) return;

            var entry = drop.Entry;
            Vector3 worldPosition = drop.transform.position;
            Sprite icon = ResolveIcon(entry);
            bool collectionSucceeded = false;

            if (entry.DropType == DropType.Item)
            {
                if (InventorySystem.Instance == null) return;
                bool success = InventorySystem.Instance.TryAddItem(entry.ItemId, entry.Quantity);
                if (!success)
                {
                    drop.OnCollectionFailed();
                    GameEvents.RaiseInventoryFull();
                    return;
                }
                GameEvents.RaiseItemCollected(entry.ItemId, entry.Quantity);
                collectionSucceeded = true;
            }
            else
            {
                if (CurrencySystem.Instance == null) return;
                long amount = entry.Quantity;
                var vault = VaultSystem.Instance;
                if (vault != null) amount = Mathf.RoundToInt(amount * vault.GetCurrencyMultiplier());
                var talent = TalentSystem.Instance;
                if (talent != null) amount = Mathf.RoundToInt(amount * (1f + talent.GetCurrencyMultiplierBonus()));
                long previousAmount = CurrencySystem.Instance.GetAmount(entry.CurrencyType);
                CurrencySystem.Instance.Add(entry.CurrencyType, amount);
                collectionSucceeded = CurrencySystem.Instance.GetAmount(entry.CurrencyType) > previousAmount;
            }

            ReturnToPool(drop);

            if (!collectionSucceeded)
                return;

            if (pickupFlyEffect == null)
            {
                Debug.LogWarning("[DropManager] PickupFlyEffectController is not assigned; pickup visual skipped.", this);
                return;
            }

            if (entry.DropType == DropType.Item)
                pickupFlyEffect.PlayItem(icon, worldPosition);
            else
                pickupFlyEffect.PlayCurrency(icon, worldPosition, entry.CurrencyType);
        }

        // ── Pool ─────────────────────────────────────────────────────────────

        private WorldDrop GetFromPool()
        {
            while (_pool.Count > 0)
            {
                var drop = _pool.Dequeue();
                if (drop != null) return drop;
            }
            return CreateInstance();
        }

        private void ReturnToPool(WorldDrop drop)
        {
            drop.gameObject.SetActive(false);
            _pool.Enqueue(drop);
        }

        private WorldDrop CreateInstance()
        {
            var instance = Instantiate(dropPrefab, transform);
            instance.gameObject.SetActive(false);
            return instance;
        }

        // ── Icon resolution ──────────────────────────────────────────────────

        private Sprite ResolveIcon(LootResultEntry entry)
        {
            var db = GameDatabase.Instance;
            if (db == null) return null;

            if (entry.DropType == DropType.Item)
                return db.Items != null ? db.Items.GetItem(entry.ItemId)?.Icon : null;
            else
                return db.Currency != null ? db.Currency.GetCurrency(entry.CurrencyType)?.Icon : null;
        }
    }
}
