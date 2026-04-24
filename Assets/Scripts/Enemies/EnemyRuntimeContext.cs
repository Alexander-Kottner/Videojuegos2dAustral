using UnityEngine;

namespace Enemies
{
    public readonly struct EnemyRuntimeContext
    {
        public EnemyRuntimeContext(
            EnemyController controller,
            Transform transform,
            Rigidbody2D rigidbody2D,
            Transform target,
            float deltaTime,
            float time)
        {
            Controller = controller;
            Transform = transform;
            Rigidbody2D = rigidbody2D;
            Target = target;
            DeltaTime = deltaTime;
            Time = time;
        }

        public EnemyController Controller { get; }
        public Transform Transform { get; }
        public Rigidbody2D Rigidbody2D { get; }
        public Transform Target { get; }
        public float DeltaTime { get; }
        public float Time { get; }

        public bool HasTarget => Target != null;
        public Vector2 Position => Rigidbody2D != null ? Rigidbody2D.position : (Vector2)Transform.position;
        public Vector2 TargetPosition => HasTarget ? (Vector2)Target.position : Position;
    }
}
