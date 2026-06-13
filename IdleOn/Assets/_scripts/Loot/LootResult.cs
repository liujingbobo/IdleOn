using System.Collections.Generic;

namespace IdleOn.Loot
{
    public class LootResult
    {
        public List<LootResultEntry> Entries = new List<LootResultEntry>();
        public bool IsEmpty => Entries.Count == 0;
    }
}
