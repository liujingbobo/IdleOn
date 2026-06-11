using UnityEngine;

namespace IdleOn.Enemies
{
    [CreateAssetMenu(fileName = "EnemyDefinition", menuName = "IdleOn/Enemy Definition")]
    public class EnemyDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string EnemyId;
        public string DisplayName;

        [Header("Stats")]
        public float MaxHP = 50f;
        public float ATK = 5f;
        public float DEF = 0f;
        public float MoveSpeed = 1.5f;

        [Header("Rewards")]
        public float XPReward = 10f;
        public int CoinReward = 3;
    }
}
