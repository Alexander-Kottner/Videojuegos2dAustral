using UnityEngine;
using Weapons.Data;

namespace Weapons
{
    [DisallowMultipleComponent]
    public class WeaponController : MonoBehaviour
    {
        [SerializeField] private WeaponDefinition definition;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Sprite[] attackAnimationSprites;
        [SerializeField, Min(0.01f)] private float attackAnimationFrameDuration = 0.08f;
        [SerializeField] private bool matchOwnerSorting = true;
        [SerializeField] private int sortingOrderOffset = 1;
        [SerializeField] private LineRenderer hitIndicator;
        [SerializeField] private bool autoCreateHitIndicator = true;
        [SerializeField, Min(0.01f)] private float hitIndicatorDuration = 0.12f;
        [SerializeField, Min(0f)] private float hitIndicatorArcHeight = 0.25f;
        [SerializeField, Min(0.001f)] private float hitIndicatorStartWidth = 0.04f;
        [SerializeField, Min(0.001f)] private float hitIndicatorEndWidth = 0.01f;
        [SerializeField] private Material hitIndicatorMaterial;

        private Transform _owner;
        private Sprite _defaultSprite;
        private Sprite[] _attackAnimationSprites;
        private int _attackAnimationFrameIndex;
        private float _attackAnimationFrameDuration;
        private float _nextAttackAnimationFrameTime;
        private float _nextAttackTime;
        private float _hideHitIndicatorTime;
        private bool _isPlayingAttackAnimation;

        public WeaponDefinition Definition => definition;
        public Transform Owner => _owner;

        private void Awake()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            if (spriteRenderer != null)
            {
                _defaultSprite = spriteRenderer.sprite;
            }

            ResolveHitIndicator();
        }

        private void OnEnable()
        {
            ResetRuntimeState();
        }

        public void BindOwner(Transform owner)
        {
            _owner = owner;
            ApplyOwnerSorting();
        }

        public bool TryUseAttack(float time, float cooldown)
        {
            if (time < _nextAttackTime)
            {
                return false;
            }

            _nextAttackTime = time + Mathf.Max(0f, cooldown);
            return true;
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

        public void TickAnimation(float time)
        {
            TickHitIndicator(time);

            if (!_isPlayingAttackAnimation || spriteRenderer == null || time < _nextAttackAnimationFrameTime)
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

            _nextAttackAnimationFrameTime = time + _attackAnimationFrameDuration;
        }

        public void PlayHitIndicator(Transform target, float time)
        {
            if (target == null)
            {
                return;
            }

            ResolveHitIndicator();
            if (hitIndicator == null)
            {
                return;
            }

            Vector3 start = transform.position;
            Vector3 end = target.position;
            Vector3 toTarget = end - start;
            Vector3 arcOffset = Vector3.zero;

            if (toTarget.sqrMagnitude > 0.0001f)
            {
                arcOffset = new Vector3(-toTarget.y, toTarget.x, 0f).normalized * hitIndicatorArcHeight;
            }

            hitIndicator.positionCount = 3;
            hitIndicator.useWorldSpace = true;
            hitIndicator.SetPosition(0, start);
            hitIndicator.SetPosition(1, (start + end) * 0.5f + arcOffset);
            hitIndicator.SetPosition(2, end);
            hitIndicator.enabled = true;
            _hideHitIndicatorTime = time + hitIndicatorDuration;
        }

        private void ResetRuntimeState()
        {
            _nextAttackTime = 0f;
            StopAttackAnimation();
        }

        private void ApplyOwnerSorting()
        {
            if (!matchOwnerSorting || _owner == null || spriteRenderer == null)
            {
                return;
            }

            SpriteRenderer ownerRenderer = _owner.GetComponentInChildren<SpriteRenderer>();
            if (ownerRenderer == null)
            {
                return;
            }

            spriteRenderer.sortingLayerID = ownerRenderer.sortingLayerID;
            spriteRenderer.sortingOrder = ownerRenderer.sortingOrder + sortingOrderOffset;

            if (hitIndicator != null)
            {
                hitIndicator.sortingLayerID = ownerRenderer.sortingLayerID;
                hitIndicator.sortingOrder = ownerRenderer.sortingOrder + sortingOrderOffset;
            }
        }

        private void ResolveHitIndicator()
        {
            if (hitIndicator == null)
            {
                hitIndicator = GetComponentInChildren<LineRenderer>();
            }

            if (hitIndicator == null && autoCreateHitIndicator)
            {
                hitIndicator = gameObject.AddComponent<LineRenderer>();
            }

            if (hitIndicator == null)
            {
                return;
            }

            hitIndicator.enabled = false;
            hitIndicator.positionCount = 3;
            hitIndicator.useWorldSpace = true;
            hitIndicator.startWidth = hitIndicatorStartWidth;
            hitIndicator.endWidth = hitIndicatorEndWidth;
            hitIndicator.numCapVertices = 2;

            if (hitIndicatorMaterial != null)
            {
                hitIndicator.sharedMaterial = hitIndicatorMaterial;
            }
            else if (hitIndicator.sharedMaterial == null)
            {
                Shader shader = Shader.Find("Sprites/Default");
                if (shader != null)
                {
                    hitIndicator.sharedMaterial = new Material(shader);
                }
            }
        }

        private void TickHitIndicator(float time)
        {
            if (hitIndicator != null && hitIndicator.enabled && time >= _hideHitIndicatorTime)
            {
                hitIndicator.enabled = false;
            }
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
    }
}
