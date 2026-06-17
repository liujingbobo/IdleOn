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

        [Header("Debug")]
        [SerializeField] private bool debugRespawn;

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

        public EnemyController GetNearestValidEnemy(Vector2 position)
        {
            EnemyController nearest = null;
            float minDist = float.MaxValue;

            foreach (var slot in _slots)
            {
                var enemy = slot.Enemy;
                if (enemy == null || !enemy.gameObject.activeInHierarchy || !enemy.IsAlive || enemy.State == EnemyState.Dead)
                    continue;

                float dist = Vector2.Distance(position, enemy.transform.position);
                if (dist < minDist) { minDist = dist; nearest = enemy; }
            }

            return nearest;
        }

        private IEnumerator Respawn(SpawnSlot slot)
        {
            yield return new WaitForSeconds(respawnDelay);
            Vector3 oldPos = slot.Enemy.transform.position;
            Vector3 newPos = slot.SpawnPoint.position;
            if (debugRespawn)
            {
                var rb = slot.Enemy.GetComponent<Rigidbody2D>();
                Vector2 velocity = rb != null ? rb.linearVelocity : Vector2.zero;
                Debug.Log($"[EnemySpawner] Respawn {slot.Enemy.name} old={oldPos} new={newPos} rbVel={velocity} state={slot.Enemy.State}", this);
            }
            // Temporary. Dynamic Rigidbody2D + direct transform.position can fight physics.
            // Future fix: MovePosition/velocity in FixedUpdate.
            slot.Enemy.transform.position = slot.SpawnPoint.position;
            slot.Enemy.gameObject.SetActive(true);
        }
    }
}
