using System.Collections.Generic;
using Enemies;
using UnityEngine;
using Weapons.Effects;

namespace Weapons.Data
{
    [CreateAssetMenu(fileName = "ArcMeleeWeaponAttack", menuName = "Game/Weapons/Attack/Melee Arc")]
    public class ArcMeleeWeaponAttackBehaviour : WeaponAttackBehaviour
    {
        [SerializeField, Min(1)] private int damage = 10;
        [SerializeField, Min(0.05f)] private float attackCooldown = 0.8f;
        [SerializeField, Min(0.1f)] private float attackRange = 1.6f;
        [SerializeField, Range(10f, 360f)] private float arcAngle = 120f;
        [SerializeField, Min(0)] private int maxTargets;
        [SerializeField] private Color effectColor = Color.white;

        private static readonly List<EnemyHealth> Targets = new();

        public override void Tick(WeaponRuntimeContext context)
        {
            if (!context.HasOwner)
            {
                return;
            }

            float range = context.ScaleRange(attackRange);
            EnemyHealth nearest = WeaponTargeting.FindNearest(context.Position, range);
            if (nearest == null || !context.TryUseAttack(context.ScaleCooldown(attackCooldown)))
            {
                return;
            }

            Vector2 direction = ((Vector2)nearest.transform.position - context.Position).normalized;
            float arc = Mathf.Min(360f, arcAngle * context.ScaleArea(1f));

            WeaponTargeting.CollectInArc(context.Position, direction, range, arc, Targets);

            int scaledDamage = context.ScaleDamage(damage);
            int hits = maxTargets > 0 ? Mathf.Min(maxTargets, Targets.Count) : Targets.Count;

            for (int i = 0; i < hits; i++)
            {
                if (context.DealDamage(Targets[i], scaledDamage, effectColor))
                {
                    CombatEffects.SpawnImpactBurst(Targets[i].transform.position, effectColor);
                }
            }

            context.PlayAttackAnimation();
            context.PlayAttackPunch(direction);
            CombatEffects.SpawnSlashArc(context.Position, direction, range, arc, effectColor);
        }
    }
}
