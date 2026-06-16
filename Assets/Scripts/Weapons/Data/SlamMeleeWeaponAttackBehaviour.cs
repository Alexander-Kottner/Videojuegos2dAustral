using System.Collections.Generic;
using Enemies;
using UnityEngine;
using Weapons.Effects;

namespace Weapons.Data
{
    [CreateAssetMenu(fileName = "SlamMeleeWeaponAttack", menuName = "Game/Weapons/Attack/Melee Slam")]
    public class SlamMeleeWeaponAttackBehaviour : WeaponAttackBehaviour
    {
        [SerializeField, Min(1)] private int damage = 24;
        [SerializeField, Min(0.05f)] private float attackCooldown = 1.8f;
        [SerializeField, Min(0.1f)] private float slamRadius = 2.2f;
        [SerializeField] private Color effectColor = new(1f, 0.85f, 0.35f);

        private static readonly List<EnemyHealth> Targets = new();

        public override void Tick(WeaponRuntimeContext context)
        {
            if (!context.HasOwner)
            {
                return;
            }

            float radius = context.ScaleArea(slamRadius);
            EnemyHealth nearest = WeaponTargeting.FindNearest(context.Position, radius);
            if (nearest == null || !context.TryUseAttack(context.ScaleCooldown(attackCooldown)))
            {
                return;
            }

            WeaponTargeting.CollectInRadius(context.Position, radius, Targets);

            int scaledDamage = context.ScaleDamage(damage);
            for (int i = 0; i < Targets.Count; i++)
            {
                if (context.DealDamage(Targets[i], scaledDamage, effectColor))
                {
                    CombatEffects.SpawnImpactBurst(Targets[i].transform.position, effectColor, 0.6f);
                }
            }

            Vector2 direction = ((Vector2)nearest.transform.position - context.Position).normalized;
            context.PlayAttackAnimation();
            context.PlayAttackPunch(direction);
            CombatEffects.SpawnShockwave(context.Position, effectColor, radius, 0.35f);
        }
    }
}
