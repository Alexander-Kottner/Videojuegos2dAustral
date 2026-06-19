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
        [SerializeField] private Sprite[] attackAnimationSprites;
        [SerializeField, Min(0.01f)] private float attackAnimationFrameDuration = 0.08f;

        private Rigidbody2D _rigidbody2D;
        private float _nextRetargetTime;
        private float _nextAttackTime;
        private float _attackReadyTime;
        private bool _isSettlingAttack;
        private Sprite _defaultSprite;
        private Sprite[] _attackAnimationSprites;
        private int _attackAnimationFrameIndex;
        private float _attackAnimationFrameDuration;
        private float _nextAttackAnimationFrameTime;
        private bool _isPlayingAttackAnimation;

        private Vector3 _baseScale = Vector3.one;
        private bool _baseScaleCaptured;

        public EnemyDefinition Definition => definition;
        public Rigidbody2D Rigidbody2D => _rigidbody2D;
        public Transform Target => targetOverride;
        public float SpeedMultiplier { get; set; } = 1f;
        public float DamageMultiplier { get; set; } = 1f;

        private void Awake()
        {
            _rigidbody2D = GetComponent<Rigidbody2D>();
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            if (spriteRenderer != null)
            {
                _defaultSprite = spriteRenderer.sprite;
            }
        }

        private void Update()
        {
            RefreshTarget();
            TickAttack();
            TickAttackAnimation();
        }

        private void OnEnable()
        {
            _nextAttackTime = 0f;
            ResetAttackSettle();
            StopAttackAnimation();
        }

        private void FixedUpdate()
        {
            TickMovement();
        }

        public void SetTarget(Transform newTarget)
        {
            targetOverride = newTarget;
        }

        public void ApplyVisualOverrides(Color tint, float scaleMultiplier)
        {
            CaptureBaseScale();

            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
            }

            if (spriteRenderer != null)
            {
                spriteRenderer.color = tint;
            }

            transform.localScale = _baseScale * Mathf.Max(0.05f, scaleMultiplier);
        }

        private void CaptureBaseScale()
        {
            if (_baseScaleCaptured)
            {
                return;
            }

            _baseScale = transform.localScale;
            _baseScaleCaptured = true;
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

        public bool TryUseAttack(float time, float cooldown)
        {
            if (time < _nextAttackTime)
            {
                return false;
            }

            _nextAttackTime = time + Mathf.Max(0f, cooldown);
            AudioManager.PlayEnemyAttack();
            return true;
        }

        public bool HasSettledAttack(float time, bool isInRange, float settlingDelay)
        {
            if (!isInRange)
            {
                ResetAttackSettle();
                return false;
            }

            if (!_isSettlingAttack)
            {
                _isSettlingAttack = true;
                _attackReadyTime = time + Mathf.Max(0f, settlingDelay);
            }

            return time >= _attackReadyTime;
        }

        public void PlayAttackAnimation(float time)
        {
            if (spriteRenderer == null || attackAnimationSprites == null || attackAnimationSprites.Length == 0)
            {
                return;
            }

            _attackAnimationSprites = attackAnimationSprites;
            _attackAnimationFrameIndex = 0;
            _attackAnimationFrameDuration = Mathf.Max(0.01f, attackAnimationFrameDuration);
            _nextAttackAnimationFrameTime = time + _attackAnimationFrameDuration;
            _isPlayingAttackAnimation = true;

            if (_attackAnimationSprites[0] != null)
            {
                spriteRenderer.sprite = _attackAnimationSprites[0];
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

        private void TickAttack()
        {
            if (definition == null || definition.AttackBehaviour == null)
            {
                return;
            }

            definition.AttackBehaviour.Tick(BuildContext(Time.deltaTime));
        }

        private void TickAttackAnimation()
        {
            if (!_isPlayingAttackAnimation || spriteRenderer == null || Time.time < _nextAttackAnimationFrameTime)
            {
                return;
            }

            _attackAnimationFrameIndex++;

            if (_attackAnimationSprites == null || _attackAnimationFrameIndex >= _attackAnimationSprites.Length)
            {
                StopAttackAnimation();
                return;
            }

            if (_attackAnimationSprites[_attackAnimationFrameIndex] != null)
            {
                spriteRenderer.sprite = _attackAnimationSprites[_attackAnimationFrameIndex];
            }

            _nextAttackAnimationFrameTime = Time.time + _attackAnimationFrameDuration;
        }

        private void ResetAttackSettle()
        {
            _isSettlingAttack = false;
            _attackReadyTime = 0f;
        }

        private void StopAttackAnimation()
        {
            _isPlayingAttackAnimation = false;
            _attackAnimationSprites = null;
            _attackAnimationFrameIndex = 0;

            if (spriteRenderer != null && _defaultSprite != null)
            {
                spriteRenderer.sprite = _defaultSprite;
            }
        }

        private EnemyRuntimeContext BuildContext(float deltaTime)
        {
            return new EnemyRuntimeContext(this, transform, _rigidbody2D, targetOverride, deltaTime, Time.time);
        }
    }
}
