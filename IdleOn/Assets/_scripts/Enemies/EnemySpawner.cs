using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace IdleOn.Enemies
{
    public class EnemySpawner : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private EnemyController enemyPrefab;
        [SerializeField] private Transform[] spawnPoints;
        [SerializeField] private float respawnDelay = 3f;

        private class SpawnSlot
        {
            public Transform SpawnPoint;
            public EnemyController Enemy;
        }

        private readonly List<SpawnSlot> _slots = new List<SpawnSlot>();

        void Start()
        {
            foreach (var point in spawnPoints)
            {
                var enemy = Instantiate(enemyPrefab, point.position, Quaternion.identity);
                var slot  = new SpawnSlot { SpawnPoint = point, Enemy = enemy };
                enemy.OnKilled += _ => StartCoroutine(Respawn(slot));
                _slots.Add(slot);
            }
        }

        public EnemyController GetNearestEnemy(Vector2 position)
        {
            EnemyController nearest = null;
            float minDist = float.MaxValue;

            foreach (var slot in _slots)
            {
                if (slot.Enemy == null || !slot.Enemy.IsAlive) continue;
                float dist = Vector2.Distance(position, slot.Enemy.transform.position);
                if (dist < minDist) { minDist = dist; nearest = slot.Enemy; }
            }

            return nearest;
        }

        private IEnumerator Respawn(SpawnSlot slot)
        {
            yield return new WaitForSeconds(respawnDelay);
            slot.Enemy.transform.position = slot.SpawnPoint.position;
            slot.Enemy.gameObject.SetActive(true);
        }
    }
}
