using Enemies;
using UnityEngine;

namespace Weapons.Data
{
    [CreateAssetMenu(fileName = "MeleeWeaponAttack", menuName = "Game/Weapons/Attack/Melee")]
    public class MeleeWeaponAttackBehaviour : WeaponAttackBehaviour
    {
        [SerializeField, Min(1)] private int damage = 10;
        [SerializeField, Min(0f)] private float attackRange = 1f;
        [SerializeField, Min(0.01f)] private float attackCooldown = 0.5f;
        [SerializeField] private LayerMask targetLayers = ~0;

        public override void Tick(WeaponRuntimeContext context)
        {
            if (!context.HasOwner)
            {
                return;
            }

            EnemyHealth target = FindTarget(context.Position, context.ScaleRange(attackRange));
            if (target == null || !context.TryUseAttack(context.ScaleCooldown(attackCooldown)))
            {
                return;
            }

            context.PlayAttackAnimation();
            context.PlayHitIndicator(target.transform);
            context.DealDamage(target, context.ScaleDamage(damage), Color.white);
        }

        private EnemyHealth FindTarget(Vector2 position, float range)
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(position, range, targetLayers);

            for (int i = 0; i < hits.Length; i++)
            {
                EnemyHealth target = hits[i].GetComponentInParent<EnemyHealth>();
                if (target != null && target.IsAlive)
                {
                    return target;
                }
            }

            return null;
        }
    }
}
