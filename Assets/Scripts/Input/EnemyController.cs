using Enemies.Data;
using UnityEngine;

namespace Enemies
{
    [DisallowMultipleComponent]
    public class EnemyController : MonoBehaviour
    {
        [SerializeField] private EnemyDefinition definition;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Transform targetOverride;
        [SerializeField] private bool autoAcquireTarget = true;
        [SerializeField] private string targetTag = "Player";
        [SerializeField] private float retargetInterval = 0.5f;

        private Rigidbody2D _rigidbody2D;
        private float _nextRetargetTime;

        public EnemyDefinition Definition => definition;
        public Rigidbody2D Rigidbody2D => _rigidbody2D;
        public Transform Target => targetOverride;

        private void Awake()
        {
            _rigidbody2D = GetComponent<Rigidbody2D>();
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }
        }

        private void Update()
        {
            RefreshTarget();
        }

        private void FixedUpdate()
        {
            TickMovement();
        }

        public void SetTarget(Transform newTarget)
        {
            targetOverride = newTarget;
        }

        public bool TryAcquireTarget()
        {
            if (string.IsNullOrEmpty(targetTag))
            {
                return false;
            }

            try
            {
                GameObject targetObject = GameObject.FindGameObjectWithTag(targetTag);

                if (targetObject == null)
                {
                    return false;
                }

                targetOverride = targetObject.transform;
                return true;
            }
            catch (UnityException)
            {
                return false;
            }
        }

        public void Move(Vector2 velocity, float deltaTime)
        {
            if (_rigidbody2D != null)
            {
                _rigidbody2D.MovePosition(_rigidbody2D.position + velocity * deltaTime);
                return;
            }

            transform.position += (Vector3)(velocity * deltaTime);
        }

        public void FaceDirection(Vector2 direction)
        {
            if (Mathf.Approximately(direction.x, 0f))
            {
                return;
            }

            if (spriteRenderer != null)
            {
                spriteRenderer.flipX = direction.x < 0f;
            }
        }

        private void RefreshTarget()
        {
            if (!autoAcquireTarget || targetOverride != null || Time.time < _nextRetargetTime)
            {
                return;
            }

            _nextRetargetTime = Time.time + retargetInterval;
            TryAcquireTarget();
        }

        private void TickMovement()
        {
            if (definition == null || definition.MovementBehaviour == null)
            {
                return;
            }

            definition.MovementBehaviour.Tick(BuildContext(Time.fixedDeltaTime));
        }

        private EnemyRuntimeContext BuildContext(float deltaTime)
        {
            return new EnemyRuntimeContext(this, transform, _rigidbody2D, targetOverride, deltaTime, Time.time);
        }
    }
}
