using System.Collections.Generic;
using UnityEngine;

namespace IdleOn.Enemies
{
    // Central discovery for currently active enemy instances. Spawners and map-local
    // respawners remain responsible only for creating/enabling those instances.
    public static class EnemyTargetRegistry
    {
        private static readonly HashSet<EnemyController> Enemies =
            new HashSet<EnemyController>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            Enemies.Clear();
        }

        public static void Register(EnemyController enemy)
        {
            if (enemy != null)
                Enemies.Add(enemy);
        }

        public static void Unregister(EnemyController enemy)
        {
            if (enemy != null)
                Enemies.Remove(enemy);
        }

        public static EnemyController GetNearestValidEnemy(Vector2 position)
        {
            EnemyController nearest = null;
            float nearestSqrDistance = float.MaxValue;
            List<EnemyController> staleEntries = null;

            foreach (EnemyController enemy in Enemies)
            {
                if (enemy == null)
                {
                    if (staleEntries == null)
                        staleEntries = new List<EnemyController>();
                    staleEntries.Add(enemy);
                    continue;
                }

                if (!IsValidTarget(enemy))
                    continue;

                float sqrDistance =
                    ((Vector2)enemy.transform.position - position).sqrMagnitude;
                if (sqrDistance >= nearestSqrDistance)
                    continue;

                nearest = enemy;
                nearestSqrDistance = sqrDistance;
            }

            if (staleEntries != null)
            {
                foreach (EnemyController staleEntry in staleEntries)
                    Enemies.Remove(staleEntry);
            }

            return nearest;
        }

        private static bool IsValidTarget(EnemyController enemy)
        {
            return enemy.gameObject.activeInHierarchy
                && enemy.IsAlive
                && enemy.State != EnemyState.Dead;
        }
    }
}
