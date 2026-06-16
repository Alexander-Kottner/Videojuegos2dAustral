using System.Collections.Generic;
using Enemies;
using UnityEngine;
using Weapons.Effects;

namespace Weapons.Data
{
    [CreateAssetMenu(fileName = "ChainLightningWeaponAttack", menuName = "Game/Weapons/Attack/Chain Lightning")]
    public class ChainLightningWeaponAttackBehaviour : WeaponAttackBehaviour
    {
        [SerializeField, Min(1)] private int damage = 14;
        [SerializeField, Min(0.05f)] private float attackCooldown = 1.6f;
        [SerializeField, Min(0.5f)] private float attackRange = 6.5f;
        [SerializeField, Min(1)] private int chainCount = 4;
        [SerializeField, Min(0.5f)] private float chainRange = 3f;
        [SerializeField, Range(0.1f, 1f)] private float damageFalloff = 0.85f;
        [SerializeField] private Color effectColor = new(0.5f, 0.8f, 1f);

        private static readonly HashSet<EnemyHealth> Visited = new();
        private static readonly List<Vector3> LinePoints = new();

        public override void Tick(WeaponRuntimeContext context)
        {
            if (!context.HasOwner)
            {
                return;
            }

            float range = context.ScaleRange(attackRange);
            EnemyHealth current = WeaponTargeting.FindNearest(context.Position, range);
            if (current == null || !context.TryUseAttack(context.ScaleCooldown(attackCooldown)))
            {
                return;
            }

            Visited.Clear();
            LinePoints.Clear();
            LinePoints.Add(context.Position);

            int hops = chainCount + context.ExtraProjectiles;
            float hopRange = context.ScaleArea(chainRange);
            float damageMultiplier = 1f;

            Vector2 direction = ((Vector2)current.transform.position - context.Position).normalized;
            context.PlayAttackAnimation();
            context.PlayAttackPunch(direction);

            for (int hop = 0; hop < hops && current != null; hop++)
            {
                Visited.Add(current);
                Vector3 targetPosition = current.transform.position;
                AddJitteredSegment(LinePoints[^1], targetPosition);

                int scaledDamage = Mathf.Max(1, Mathf.RoundToInt(context.ScaleDamage(damage) * damageMultiplier));
                context.DealDamage(current, scaledDamage, effectColor);
                CombatEffects.SpawnImpactBurst(targetPosition, effectColor, 0.5f);

                damageMultiplier *= damageFalloff;
                current = WeaponTargeting.FindNearest(targetPosition, hopRange, Visited);
            }

            CombatEffects.SpawnLine(LinePoints, Color.Lerp(effectColor, Color.white, 0.5f), 0.07f, 0.18f);
        }

        private static void AddJitteredSegment(Vector3 from, Vector3 to)
        {
            Vector3 delta = to - from;
            Vector3 perpendicular = new Vector3(-delta.y, delta.x, 0f).normalized;

            LinePoints.Add(from + delta * 0.33f + perpendicular * Random.Range(-0.25f, 0.25f));
            LinePoints.Add(from + delta * 0.66f + perpendicular * Random.Range(-0.25f, 0.25f));
            LinePoints.Add(to);
        }
    }
}
