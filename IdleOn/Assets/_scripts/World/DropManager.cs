using System.Collections.Generic;
using UnityEngine;
using IdleOn.Loot;
using IdleOn.Items;
using IdleOn.Inventory;
using IdleOn.Core;
using IdleOn.Vault;

namespace IdleOn.World
{
    public class DropManager : MonoBehaviour
    {
        public static DropManager Instance { get; private set; }

        [Header("References")]
        [SerializeField] private WorldDrop dropPrefab;

        [Header("Pool")]
        [SerializeField] private int preWarmCount = 10;

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

            foreach (var entry in result.Entries)
            {
                var drop = GetFromPool();
                var icon = ResolveIcon(entry);
                drop.Setup(entry, icon);
                drop.transform.position = new Vector3(
                    origin.x + Random.Range(-0.6f, 0.6f),
                    origin.y + Random.Range(0f,    0.4f),
                    0f);
                drop.gameObject.SetActive(true);
            }
        }

        // ── Collection ───────────────────────────────────────────────────────

        public void Collect(WorldDrop drop)
        {
            if (drop == null || !drop.CanBeCollected) return;

            var entry = drop.Entry;

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
            }
            else
            {
                if (CurrencySystem.Instance == null) return;
                long amount = entry.Quantity;
                var vault = VaultSystem.Instance;
                if (vault != null) amount = Mathf.RoundToInt(amount * vault.GetCurrencyMultiplier());
                CurrencySystem.Instance.Add(entry.CurrencyType, amount);
            }

            ReturnToPool(drop);
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
