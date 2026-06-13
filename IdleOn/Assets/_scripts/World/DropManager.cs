using System.Collections.Generic;
using UnityEngine;
using IdleOn.Loot;
using IdleOn.Items;
using IdleOn.Inventory;
using IdleOn.Core;

namespace IdleOn.World
{
    public class DropManager : MonoBehaviour
    {
        public static DropManager Instance { get; private set; }

        [Header("References")]
        [SerializeField] private WorldDrop        dropPrefab;
        [SerializeField] private ItemDatabase     itemDatabase;
        [SerializeField] private CurrencyDatabase currencyDatabase;

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
                var drop  = GetFromPool();
                var icon  = ResolveIcon(entry);
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
                CurrencySystem.Instance.Add(entry.CurrencyType, entry.Quantity);
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
            if (entry.DropType == DropType.Item)
            {
                var def = itemDatabase != null ? itemDatabase.GetItem(entry.ItemId) : null;
                return def != null ? def.Icon : null;
            }
            else
            {
                var def = currencyDatabase != null ? currencyDatabase.GetCurrency(entry.CurrencyType) : null;
                return def != null ? def.Icon : null;
            }
        }
    }
}
