using System;

namespace IdleOn.World
{
    [Serializable]
    public class MapProgressData
    {
        public string MapId;
        public int    KillCount;
        public bool   IsComplete;
        public bool   IsUnlocked;
    }
}
