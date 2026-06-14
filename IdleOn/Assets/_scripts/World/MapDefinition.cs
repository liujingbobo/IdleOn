using UnityEngine;

namespace IdleOn.World
{
    [CreateAssetMenu(fileName = "MapDef_", menuName = "IdleOn/Map Definition")]
    public class MapDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string MapId;
        public string DisplayName;

        [Header("Objective")]
        public string ObjectiveEnemyId;   // empty = any enemy counts
        public int    KillObjective;
        public string ObjectiveLabel;     // e.g. "Kill Slimes"

        [Header("Reward")]
        public long   SilverReward;
        public string UnlocksMapId;       // empty = no unlock — end of content
    }
}
