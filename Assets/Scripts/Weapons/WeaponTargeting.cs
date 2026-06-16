using System.Collections.Generic;
using Enemies;
using UnityEngine;

namespace Weapons
{
    public static class WeaponTargeting
    {
        public const float EnemyBodyRadius = 0.3f;

        public static EnemyHealth FindNearest(Vector2 origin, float maxRange)
        {
            return FindNearest(origin, maxRange, null);
        }

        public static EnemyHealth FindNearest(Vector2 origin, float maxRange, HashSet<EnemyHealth> excluded)
        {
            IReadOnlyList<EnemyHealth> alive = EnemyHealth.AliveEnemies;
            EnemyHealth best = null;
            float bestSqr = (maxRange + EnemyBodyRadius) * (maxRange + EnemyBodyRadius);

            for (int i = 0; i < alive.Count; i++)
            {
                EnemyHealth enemy = alive[i];
                if (enemy == null || !enemy.IsAlive || (excluded != null && excluded.Contains(enemy)))
                {
                    continue;
                }

                float sqr = ((Vector2)enemy.transform.position - origin).sqrMagnitude;
                if (sqr <= bestSqr)
                {
                    bestSqr = sqr;
                    best = enemy;
                }
            }

            return best;
        }

        public static EnemyHealth FindFarthest(Vector2 origin, float maxRange)
        {
            IReadOnlyList<EnemyHealth> alive = EnemyHealth.AliveEnemies;
            EnemyHealth best = null;
            float maxSqr = (maxRange + EnemyBodyRadius) * (maxRange + EnemyBodyRadius);
            float bestSqr = -1f;

            for (int i = 0; i < alive.Count; i++)
            {
                EnemyHealth enemy = alive[i];
                if (enemy == null || !enemy.IsAlive)
                {
                    continue;
                }

                float sqr = ((Vector2)enemy.transform.position - origin).sqrMagnitude;
                if (sqr <= maxSqr && sqr > bestSqr)
                {
                    bestSqr = sqr;
                    best = enemy;
                }
            }

            return best;
        }

        public static int CollectInRadius(Vector2 origin, float radius, List<EnemyHealth> results)
        {
            results.Clear();
            IReadOnlyList<EnemyHealth> alive = EnemyHealth.AliveEnemies;
            float maxSqr = (radius + EnemyBodyRadius) * (radius + EnemyBodyRadius);

            for (int i = 0; i < alive.Count; i++)
            {
                EnemyHealth enemy = alive[i];
                if (enemy == null || !enemy.IsAlive)
                {
                    continue;
                }

                if (((Vector2)enemy.transform.position - origin).sqrMagnitude <= maxSqr)
                {
                    results.Add(enemy);
                }
            }

            return results.Count;
        }

        public static int CollectInArc(Vector2 origin, Vector2 direction, float range, float arcAngleDegrees, List<EnemyHealth> results)
        {
            results.Clear();
            if (direction.sqrMagnitude < 0.0001f)
            {
                direction = Vector2.right;
            }

            direction.Normalize();
            IReadOnlyList<EnemyHealth> alive = EnemyHealth.AliveEnemies;
            float maxSqr = (range + EnemyBodyRadius) * (range + EnemyBodyRadius);
            float minDot = Mathf.Cos(Mathf.Clamp(arcAngleDegrees, 1f, 360f) * 0.5f * Mathf.Deg2Rad);

            for (int i = 0; i < alive.Count; i++)
            {
                EnemyHealth enemy = alive[i];
                if (enemy == null || !enemy.IsAlive)
                {
                    continue;
                }

                Vector2 toEnemy = (Vector2)enemy.transform.position - origin;
                float sqr = toEnemy.sqrMagnitude;
                if (sqr > maxSqr)
                {
                    continue;
                }

                if (arcAngleDegrees >= 360f || sqr < 0.01f || Vector2.Dot(toEnemy.normalized, direction) >= minDot)
                {
                    results.Add(enemy);
                }
            }

            return results.Count;
        }

        public static int CollectInLine(Vector2 origin, Vector2 direction, float length, float width, List<EnemyHealth> results)
        {
            results.Clear();
            if (direction.sqrMagnitude < 0.0001f)
            {
                direction = Vector2.right;
            }

            direction.Normalize();
            IReadOnlyList<EnemyHealth> alive = EnemyHealth.AliveEnemies;
            float halfWidth = width * 0.5f + EnemyBodyRadius;

            for (int i = 0; i < alive.Count; i++)
            {
                EnemyHealth enemy = alive[i];
                if (enemy == null || !enemy.IsAlive)
                {
                    continue;
                }

                Vector2 toEnemy = (Vector2)enemy.transform.position - origin;
                float along = Vector2.Dot(toEnemy, direction);
                if (along < -EnemyBodyRadius || along > length + EnemyBodyRadius)
                {
                    continue;
                }

                float perpendicular = Mathf.Abs(toEnemy.x * direction.y - toEnemy.y * direction.x);
                if (perpendicular <= halfWidth)
                {
                    results.Add(enemy);
                }
            }

            return results.Count;
        }
    }
}
