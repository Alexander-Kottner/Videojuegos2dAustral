using Enemies;
using UnityEngine;
using PlayerHealthComponent = global::Input.PlayerHealth;

namespace Enemies.Data
{
    [CreateAssetMenu(fileName = "MeleeEnemyAttack", menuName = "Game/Enemies/Attack/Melee")]
    public class MeleeEnemyAttackBehaviour : EnemyAttackBehaviour
    {
        [SerializeField, Min(1)] private int damage = 10;
        [SerializeField, Min(0f)] private float attackRange = 0.75f;
        [SerializeField, Min(0f)] private float settlingDelay = 0.8f;
        [SerializeField, Min(0.01f)] private float attackCooldown = 1f;

        public override void Tick(EnemyRuntimeContext context)
        {
            if (!context.HasTarget)
            {
                context.Controller.HasSettledAttack(context.Time, false, settlingDelay);
                return;
            }

            Vector2 toTarget = context.TargetPosition - context.Position;
            bool isInRange = toTarget.sqrMagnitude <= attackRange * attackRange;

            if (!context.Controller.HasSettledAttack(context.Time, isInRange, settlingDelay))
            {
                return;
            }

            if (!isInRange)
            {
                return;
            }

            PlayerHealthComponent targetHealth = context.Target.GetComponentInParent<PlayerHealthComponent>();
            if (targetHealth == null || !targetHealth.IsAlive)
            {
                context.Controller.HasSettledAttack(context.Time, false, settlingDelay);
                return;
            }

            context.Controller.FaceDirection(toTarget);

            if (!context.Controller.TryUseAttack(context.Time, attackCooldown))
            {
                return;
            }

            context.Controller.PlayAttackAnimation(context.Time);
            targetHealth.TakeDamage(damage);
        }
    }
}
