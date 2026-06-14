using System.Collections.Generic;
using UnityEngine;

namespace IdleOn.World
{
    [CreateAssetMenu(fileName = "MapDatabase", menuName = "IdleOn/Map Database")]
    public class MapDatabase : ScriptableObject
    {
        [SerializeField] private List<MapDefinition> maps = new List<MapDefinition>();

        public IReadOnlyList<MapDefinition> Maps => maps;

        public MapDefinition GetMap(string mapId)
        {
            foreach (var m in maps)
                if (m != null && m.MapId == mapId) return m;
            return null;
        }
    }
}
