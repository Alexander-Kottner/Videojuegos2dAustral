using Enemies;
using UnityEngine;
using Weapons.Effects;

namespace Weapons.Data
{
    [CreateAssetMenu(fileName = "ProjectileWeaponAttack", menuName = "Game/Weapons/Attack/Projectile")]
    public class ProjectileWeaponAttackBehaviour : WeaponAttackBehaviour
    {
        [SerializeField, Min(1)] private int damage = 8;
        [SerializeField, Min(0.05f)] private float attackCooldown = 1f;
        [SerializeField, Min(0.5f)] private float attackRange = 7f;
        [SerializeField, Min(0.5f)] private float projectileSpeed = 11f;
        [SerializeField, Min(1)] private int projectileCount = 1;
        [SerializeField, Min(0f)] private float spreadAngle = 12f;
        [SerializeField, Min(0)] private int pierces;
        [SerializeField, Min(0)] private int bounces;
        [SerializeField] private bool returnToOwner;
        [SerializeField] private bool homing;
        [SerializeField] private float spinSpeed;
        [SerializeField, Min(0f)] private float explodeRadius;
        [SerializeField, Min(0.05f)] private float projectileScale = 0.7f;
        [SerializeField, Min(0.02f)] private float hitRadius = 0.18f;
        [SerializeField] private bool emitTrail;
        [SerializeField] private Color effectColor = Color.white;

        public override void Tick(WeaponRuntimeContext context)
        {
            if (!context.HasOwner)
            {
                return;
            }

            float range = context.ScaleRange(attackRange);
            EnemyHealth target = WeaponTargeting.FindNearest(context.Position, range);
            if (target == null || !context.TryUseAttack(context.ScaleCooldown(attackCooldown)))
            {
                return;
            }

            Vector2 baseDirection = ((Vector2)target.transform.position - context.Position).normalized;
            int count = projectileCount + context.ExtraProjectiles;
            int scaledDamage = context.ScaleDamage(damage);
            float scale = projectileScale * context.ScaleArea(1f);
            Sprite sprite = context.Stage?.Sprite;

            for (int i = 0; i < count; i++)
            {
                float offset = count > 1 ? (i - (count - 1) * 0.5f) * spreadAngle : 0f;
                Vector2 direction = Quaternion.Euler(0f, 0f, offset) * baseDirection;

                WeaponProjectile projectile = CombatEffects.GetProjectile();
                projectile.Launch(new WeaponProjectile.LaunchConfig
                {
                    Source = context.Weapon,
                    Sprite = sprite,
                    SpriteTint = Color.white,
                    Color = effectColor,
                    Origin = (Vector3)context.Position + (Vector3)(direction * 0.4f),
                    Direction = direction,
                    Speed = projectileSpeed,
                    MaxDistance = range,
                    Damage = scaledDamage,
                    Pierces = pierces,
                    Bounces = bounces,
                    ReturnToOwner = returnToOwner,
                    Homing = homing,
                    SpinSpeed = spinSpeed,
                    HitRadius = hitRadius * context.ScaleArea(1f),
                    ExplodeRadius = explodeRadius > 0f ? context.ScaleArea(explodeRadius) : 0f,
                    Scale = scale,
                    EmitTrail = emitTrail
                });
            }

            context.PlayAttackAnimation();
            context.PlayAttackPunch(baseDirection);
        }
    }
}
