using System;
using System.Collections.Generic;

namespace IdleOn.Vault
{
    [Serializable]
    public class VaultSaveData
    {
        public List<VaultLevelEntry> Levels = new List<VaultLevelEntry>();

        public int GetLevel(string upgradeId)
        {
            foreach (var e in Levels)
                if (e.UpgradeId == upgradeId) return e.Level;
            return 0;
        }

        public void SetLevel(string upgradeId, int level)
        {
            foreach (var e in Levels)
            {
                if (e.UpgradeId == upgradeId) { e.Level = level; return; }
            }
            Levels.Add(new VaultLevelEntry { UpgradeId = upgradeId, Level = level });
        }
    }
}
