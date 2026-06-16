using System.Collections.Generic;
using Enemies;
using UnityEngine;
using Weapons.Effects;

namespace Weapons.Data
{
    [CreateAssetMenu(fileName = "BeamWeaponAttack", menuName = "Game/Weapons/Attack/Beam")]
    public class BeamWeaponAttackBehaviour : WeaponAttackBehaviour
    {
        [SerializeField, Min(1)] private int damage = 16;
        [SerializeField, Min(0.05f)] private float attackCooldown = 1.2f;
        [SerializeField, Min(0.5f)] private float attackRange = 9f;
        [SerializeField, Min(0.05f)] private float beamWidth = 0.7f;
        [SerializeField] private Color effectColor = new(0.8f, 0.5f, 1f);

        private static readonly List<EnemyHealth> Targets = new();

        public override void Tick(WeaponRuntimeContext context)
        {
            if (!context.HasOwner)
            {
                return;
            }

            float range = context.ScaleRange(attackRange);
            EnemyHealth target = WeaponTargeting.FindFarthest(context.Position, range);
            if (target == null || !context.TryUseAttack(context.ScaleCooldown(attackCooldown)))
            {
                return;
            }

            Vector2 direction = ((Vector2)target.transform.position - context.Position).normalized;
            float width = beamWidth * context.ScaleArea(1f);

            WeaponTargeting.CollectInLine(context.Position, direction, range, width, Targets);

            int scaledDamage = context.ScaleDamage(damage);
            for (int i = 0; i < Targets.Count; i++)
            {
                if (context.DealDamage(Targets[i], scaledDamage, effectColor))
                {
                    CombatEffects.SpawnImpactBurst(Targets[i].transform.position, effectColor, 0.5f);
                }
            }

            context.PlayAttackAnimation();
            context.PlayAttackPunch(direction);

            Vector3 start = (Vector3)context.Position + (Vector3)(direction * 0.35f);
            Vector3 end = (Vector3)context.Position + (Vector3)(direction * range);
            CombatEffects.SpawnBeam(start, end, effectColor, width * 0.5f);
        }
    }
}
