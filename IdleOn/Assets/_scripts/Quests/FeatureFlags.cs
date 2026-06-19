using System;

namespace IdleOn.Quests
{
    // Demo feature gates. Inventory is always available and intentionally not listed.
    [Flags]
    public enum FeatureFlags
    {
        None       = 0,
        AutoCombat = 1 << 0,
        Craft      = 1 << 1,
        Talents    = 1 << 2,
        Vault      = 1 << 3,
        Map        = 1 << 4
    }
}
