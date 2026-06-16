using System.Collections.Generic;
using Enemies;
using UnityEngine;
using Weapons.Effects;

namespace Weapons.Data
{
    [CreateAssetMenu(fileName = "ThrustMeleeWeaponAttack", menuName = "Game/Weapons/Attack/Melee Thrust")]
    public class ThrustMeleeWeaponAttackBehaviour : WeaponAttackBehaviour
    {
        [SerializeField, Min(1)] private int damage = 8;
        [SerializeField, Min(0.05f)] private float attackCooldown = 0.55f;
        [SerializeField, Min(0.1f)] private float attackRange = 1.5f;
        [SerializeField, Min(0.05f)] private float thrustWidth = 0.55f;
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
            float width = thrustWidth * context.ScaleArea(1f);

            WeaponTargeting.CollectInLine(context.Position, direction, range, width, Targets);

            int scaledDamage = context.ScaleDamage(damage);
            for (int i = 0; i < Targets.Count; i++)
            {
                if (context.DealDamage(Targets[i], scaledDamage, effectColor))
                {
                    CombatEffects.SpawnImpactBurst(Targets[i].transform.position, effectColor, 0.4f);
                }
            }

            context.PlayAttackAnimation();
            context.PlayAttackPunch(direction);

            Vector3 start = (Vector3)context.Position + (Vector3)(direction * 0.35f);
            Vector3 end = (Vector3)context.Position + (Vector3)(direction * range);
            CombatEffects.SpawnLine(new[] { start, end }, effectColor, width * 0.4f, 0.12f);
        }
    }
}
