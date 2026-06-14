using System.Collections.Generic;
using Enemies;
using UnityEngine;
using Weapons.Effects;

namespace Weapons.Data
{
    [CreateAssetMenu(fileName = "OrbitWeaponAttack", menuName = "Game/Weapons/Attack/Orbit")]
    public class OrbitWeaponAttackBehaviour : WeaponAttackBehaviour
    {
        [SerializeField, Min(1)] private int damage = 9;
        [SerializeField, Min(0.05f)] private float hitInterval = 0.5f;
        [SerializeField, Min(0.2f)] private float orbitRadius = 1.9f;
        [SerializeField] private float rotationSpeed = 220f;
        [SerializeField, Min(1)] private int orbCount = 1;
        [SerializeField, Min(0.1f)] private float orbScale = 0.8f;
        [SerializeField] private Color effectColor = Color.white;

        private class OrbitState
        {
            public readonly List<SpriteRenderer> Orbs = new();
            public readonly Dictionary<EnemyHealth, float> NextHitTime = new();
            public readonly List<EnemyHealth> PruneBuffer = new();
            public float Angle;
        }

        public override void Tick(WeaponRuntimeContext context)
        {
            if (!context.HasOwner || context.Weapon == null)
            {
                return;
            }

            OrbitState state = context.Weapon.GetOrCreateState<OrbitState>();
            int desiredOrbs = orbCount + context.ExtraProjectiles;
            SyncOrbs(context, state, desiredOrbs);

            state.Angle += rotationSpeed * context.DeltaTime;

            float radius = context.ScaleRange(orbitRadius);
            float scale = orbScale * context.ScaleArea(1f);
            int scaledDamage = context.ScaleDamage(damage);
            float interval = context.ScaleCooldown(hitInterval);

            for (int i = 0; i < state.Orbs.Count; i++)
            {
                SpriteRenderer orb = state.Orbs[i];
                if (orb == null)
                {
                    continue;
                }

                float angle = (state.Angle + i * (360f / state.Orbs.Count)) * Mathf.Deg2Rad;
                Vector3 position = (Vector3)context.Position + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius;
                orb.transform.position = position;
                orb.transform.localScale = Vector3.one * scale;
                orb.transform.Rotate(0f, 0f, 360f * context.DeltaTime);

                Sprite stageSprite = context.Stage?.Sprite;
                if (stageSprite != null && orb.sprite != stageSprite)
                {
                    orb.sprite = stageSprite;
                }

                DamageNearby(context, state, position, scale * 0.6f, scaledDamage, interval);
            }

            PruneHitTimes(state);
        }

        private void DamageNearby(WeaponRuntimeContext context, OrbitState state, Vector3 orbPosition, float hitRadius, int scaledDamage, float interval)
        {
            IReadOnlyList<EnemyHealth> alive = EnemyHealth.AliveEnemies;
            float radius = hitRadius + WeaponTargeting.EnemyBodyRadius;
            float radiusSqr = radius * radius;

            for (int i = alive.Count - 1; i >= 0; i--)
            {
                if (i >= alive.Count)
                {
                    continue;
                }

                EnemyHealth enemy = alive[i];
                if (enemy == null || !enemy.IsAlive)
                {
                    continue;
                }

                if (((Vector2)enemy.transform.position - (Vector2)orbPosition).sqrMagnitude > radiusSqr)
                {
                    continue;
                }

                if (state.NextHitTime.TryGetValue(enemy, out float nextTime) && context.Time < nextTime)
                {
                    continue;
                }

                state.NextHitTime[enemy] = context.Time + interval;
                if (context.DealDamage(enemy, scaledDamage, effectColor))
                {
                    CombatEffects.SpawnImpactBurst(enemy.transform.position, effectColor, 0.45f);
                }
            }
        }

        private static void SyncOrbs(WeaponRuntimeContext context, OrbitState state, int desiredCount)
        {
            for (int i = state.Orbs.Count - 1; i >= 0; i--)
            {
                if (state.Orbs[i] == null)
                {
                    state.Orbs.RemoveAt(i);
                }
            }

            while (state.Orbs.Count > desiredCount)
            {
                SpriteRenderer orb = state.Orbs[^1];
                state.Orbs.RemoveAt(state.Orbs.Count - 1);
                if (orb != null)
                {
                    Object.Destroy(orb.gameObject);
                }
            }

            SpriteRenderer weaponRenderer = context.Weapon.GetComponentInChildren<SpriteRenderer>();

            while (state.Orbs.Count < desiredCount)
            {
                GameObject orbObject = new("OrbitOrb");
                orbObject.transform.SetParent(context.Weapon.transform, false);

                SpriteRenderer renderer = orbObject.AddComponent<SpriteRenderer>();
                if (weaponRenderer != null)
                {
                    renderer.sharedMaterial = weaponRenderer.sharedMaterial;
                    renderer.sortingLayerID = weaponRenderer.sortingLayerID;
                    renderer.sortingOrder = weaponRenderer.sortingOrder + 1;
                    renderer.sprite = weaponRenderer.sprite;
                }

                state.Orbs.Add(renderer);
            }
        }

        private static void PruneHitTimes(OrbitState state)
        {
            if (state.NextHitTime.Count < 64)
            {
                return;
            }

            state.PruneBuffer.Clear();
            foreach (KeyValuePair<EnemyHealth, float> pair in state.NextHitTime)
            {
                if (pair.Key == null || !pair.Key.IsAlive)
                {
                    state.PruneBuffer.Add(pair.Key);
                }
            }

            for (int i = 0; i < state.PruneBuffer.Count; i++)
            {
                state.NextHitTime.Remove(state.PruneBuffer[i]);
            }
        }
    }
}
