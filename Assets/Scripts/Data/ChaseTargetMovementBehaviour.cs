using Enemies;
using UnityEngine;

namespace Enemies.Data
{
    [CreateAssetMenu(fileName = "ChaseTargetMovement", menuName = "Game/Enemies/Movement/Chase Target")]
    public class ChaseTargetMovementBehaviour : EnemyMovementBehaviour
    {
        [SerializeField] private float moveSpeed = 2.5f;
        [SerializeField] private float stopDistance = 0.75f;

        public override void Tick(EnemyRuntimeContext context)
        {
            if (!context.HasTarget)
            {
                return;
            }

            Vector2 toTarget = context.TargetPosition - context.Position;
            float distance = toTarget.magnitude;

            if (distance <= stopDistance)
            {
                return;
            }

            Vector2 direction = toTarget / distance;
            context.Controller.Move(direction * moveSpeed, context.DeltaTime);
            context.Controller.FaceDirection(direction);
        }
    }
}
