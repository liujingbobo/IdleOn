using UnityEngine;

namespace IdleOn.World
{
    [CreateAssetMenu(fileName = "MapDef_", menuName = "IdleOn/Map Definition")]
    public class MapDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string MapId;
        public string DisplayName;

        [Header("Content")]
        public GameObject MapPrefab;

        [Header("Unlock Requirements (all optional, ANDed)")]
        public string UnlockQuestId;             // empty = no quest requirement
        public string UnlockEnemyId;              // empty = no kill requirement
        public int    UnlockKillCount;
        public string UnlockRequirementLabel;     // e.g. "Slime" — used in portal requirement text
    }
}
