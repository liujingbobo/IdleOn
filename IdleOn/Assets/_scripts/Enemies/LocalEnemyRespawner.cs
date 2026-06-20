using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using IdleOn.World;

namespace IdleOn.Enemies
{
    // Minimal map-root-local respawn: re-enables an already-placed child EnemyController at its
    // original position after a delay. Does not instantiate new enemies and does not use the
    // global EnemySpawner (which manages its own spawned slots and would duplicate/replace the
    // hand-placed enemies on this root). Attach to a single map root (e.g. Map_grassland_3).
    public class LocalEnemyRespawner : MonoBehaviour
    {
        [SerializeField] private float respawnDelay = 3f;

        private readonly List<EnemyController> _enemies = new List<EnemyController>();
        private readonly Dictionary<EnemyController, Vector3> _spawnPositions = new Dictionary<EnemyController, Vector3>();

        void Awake()
        {
            foreach (var enemy in GetComponentsInChildren<EnemyController>(true))
            {
                _enemies.Add(enemy);
                _spawnPositions[enemy] = enemy.transform.position;
                enemy.OnKilled += HandleKilled;
            }
        }

        void OnDestroy()
        {
            foreach (var enemy in _enemies)
                if (enemy != null) enemy.OnKilled -= HandleKilled;
        }

        private void HandleKilled(EnemyController enemy)
        {
            if (!_spawnPositions.TryGetValue(enemy, out var spawnPos)) return;
            StartCoroutine(Respawn(enemy, spawnPos));
        }

        private IEnumerator Respawn(EnemyController enemy, Vector3 spawnPos)
        {
            yield return new WaitForSeconds(respawnDelay);
            if (enemy == null) yield break;

            var lane = GroundLane.Current;
            if (lane != null) spawnPos.y = lane.GroundY;

            enemy.transform.position = spawnPos;
            enemy.gameObject.SetActive(true);
        }
    }
}
