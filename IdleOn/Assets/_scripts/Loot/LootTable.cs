using System.Collections.Generic;
using UnityEngine;
using IdleOn.Items;

namespace IdleOn.Loot
{
    [CreateAssetMenu(fileName = "LootTable", menuName = "IdleOn/Loot Table")]
    public class LootTable : ScriptableObject
    {
        [SerializeField] private List<DropEntry> entries = new List<DropEntry>();
        public IReadOnlyList<DropEntry> Entries => entries;

#if UNITY_EDITOR
        private void OnValidate()
        {
            for (int i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                if (e == null) continue;
                if (e.DropType == DropType.Item && e.ItemDefinition == null)
                    Debug.LogWarning($"[LootTable] {name}: entry [{i}] is Item but has no ItemDefinition assigned.", this);
                if (e.MinQuantity > e.MaxQuantity)
                    Debug.LogWarning($"[LootTable] {name}: entry [{i}] MinQuantity > MaxQuantity.", this);
            }
        }
#endif
    }
}
