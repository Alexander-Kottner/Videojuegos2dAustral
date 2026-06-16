using System.Collections.Generic;
using Enemies;
using UnityEngine;
using Weapons.Effects;

namespace Weapons.Data
{
    [CreateAssetMenu(fileName = "MeteorStrikeWeaponAttack", menuName = "Game/Weapons/Attack/Meteor Strike")]
    public class MeteorStrikeWeaponAttackBehaviour : WeaponAttackBehaviour
    {
        [SerializeField, Min(1)] private int damage = 35;
        [SerializeField, Min(0.05f)] private float attackCooldown = 2.6f;
        [SerializeField, Min(0.5f)] private float targetRange = 8f;
        [SerializeField, Min(1)] private int strikeCount = 2;
        [SerializeField, Min(0.2f)] private float strikeRadius = 1.6f;
        [SerializeField, Min(0.1f)] private float impactDelay = 0.8f;
        [SerializeField] private Color effectColor = new(0.55f, 0.8f, 1f);

        private class MeteorState
        {
            public readonly List<PendingStrike> Pending = new();
        }

        private struct PendingStrike
        {
            public Vector3 Position;
            public float ImpactTime;
            public int Damage;
            public float Radius;
        }

        private static readonly List<EnemyHealth> Targets = new();
        private static readonly List<EnemyHealth> HitBuffer = new();

        public override void Tick(WeaponRuntimeContext context)
        {
            if (!context.HasOwner || context.Weapon == null)
            {
                return;
            }

            MeteorState state = context.Weapon.GetOrCreateState<MeteorState>();
            TickPendingStrikes(context, state);

            float range = context.ScaleRange(targetRange);
            if (WeaponTargeting.CollectInRadius(context.Position, range, Targets) == 0)
            {
                return;
            }

            if (!context.TryUseAttack(context.ScaleCooldown(attackCooldown)))
            {
                return;
            }

            context.PlayAttackAnimation();

            int strikes = strikeCount + context.ExtraProjectiles;
            int scaledDamage = context.ScaleDamage(damage);
            float radius = context.ScaleArea(strikeRadius);

            for (int i = 0; i < strikes; i++)
            {
                EnemyHealth target = Targets[Random.Range(0, Targets.Count)];
                Vector3 position = target.transform.position + (Vector3)(Random.insideUnitCircle * 0.6f);

                state.Pending.Add(new PendingStrike
                {
                    Position = position,
                    ImpactTime = context.Time + impactDelay + i * 0.15f,
                    Damage = scaledDamage,
                    Radius = radius
                });

                Color warning = effectColor;
                warning.a = 0.55f;
                CombatEffects.SpawnFlash(CombatEffects.RingSprite, position, warning,
                    radius * 2.2f, radius * 0.7f, impactDelay + i * 0.15f);
            }
        }

        private void TickPendingStrikes(WeaponRuntimeContext context, MeteorState state)
        {
            for (int i = state.Pending.Count - 1; i >= 0; i--)
            {
                PendingStrike strike = state.Pending[i];
                if (context.Time < strike.ImpactTime)
                {
                    continue;
                }

                state.Pending.RemoveAt(i);

                CombatEffects.SpawnExplosion(strike.Position, effectColor, strike.Radius);
                WeaponTargeting.CollectInRadius(strike.Position, strike.Radius, HitBuffer);

                for (int j = 0; j < HitBuffer.Count; j++)
                {
                    context.DealDamage(HitBuffer[j], strike.Damage, effectColor);
                }
            }
        }
    }
}
