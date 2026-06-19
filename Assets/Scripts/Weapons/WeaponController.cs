using System;
using UnityEngine;
using Weapons.Data;
using Weapons.Effects;

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

        private const float AttackPunchDuration = 0.12f;
        private const float AttackPunchDistance = 0.22f;

        private Transform _owner;
        private Sprite _defaultSprite;
        private Sprite[] _attackAnimationSprites;
        private int _attackAnimationFrameIndex;
        private float _attackAnimationFrameDuration;
        private float _nextAttackAnimationFrameTime;
        private float _nextAttackTime;
        private float _hideHitIndicatorTime;
        private bool _isPlayingAttackAnimation;

        private int _lastHitCount;
        private int _stageIndex;
        private float _bonusDamageMultiplier = 1f;
        private float _bonusCooldownMultiplier = 1f;
        private Vector3 _baseLocalPosition;
        private Vector2 _punchDirection;
        private float _punchEndTime;
        private object _behaviourState;

        public WeaponDefinition Definition => definition;
        public Transform Owner => _owner;
        public int LastHitCount => _lastHitCount;
        public int StageIndex => _stageIndex;
        public WeaponStage CurrentStage => definition != null ? definition.GetStage(_stageIndex) : null;
        public float BonusDamageMultiplier => _bonusDamageMultiplier;
        public float BonusCooldownMultiplier => _bonusCooldownMultiplier;

        public event Action<WeaponController> Evolved;

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

            _baseLocalPosition = transform.localPosition;
            ApplyStageVisual(_stageIndex);
            ResolveHitIndicator();
        }

        private void OnEnable()
        {
            ResetRuntimeState();
        }

        public void ApplyDefinition(WeaponDefinition newDefinition)
        {
            definition = newDefinition;
            _lastHitCount = 0;
            _stageIndex = 0;
            ApplyStageVisual(0);
        }

        public void BindOwner(Transform owner)
        {
            _owner = owner;
            ApplyOwnerSorting();
        }

        public void SetBaseLocalPosition(Vector3 localPosition)
        {
            _baseLocalPosition = localPosition;
            transform.localPosition = localPosition;
        }

        public T GetOrCreateState<T>() where T : class, new()
        {
            if (_behaviourState is T existing)
            {
                return existing;
            }

            T state = new();
            _behaviourState = state;
            return state;
        }

        public void AddDamageBoost(float multiplier)
        {
            _bonusDamageMultiplier *= Mathf.Max(0.01f, multiplier);
        }

        public void AddCooldownBoost(float multiplier)
        {
            _bonusCooldownMultiplier *= Mathf.Clamp(multiplier, 0.1f, 10f);
        }

        public void RestoreState(int stageIndex, int lastHitCount)
        {
            _lastHitCount = lastHitCount;
            _stageIndex = stageIndex;
            ApplyStageVisual(_stageIndex);
        }

        public void NotifyLastHit()
        {
            _lastHitCount++;

            if (definition == null)
            {
                return;
            }

            int targetStage = definition.GetStageIndexForLastHits(_lastHitCount);
            if (targetStage > _stageIndex)
            {
                Evolve(targetStage);
            }
        }

        public bool TryUseAttack(float time, float cooldown)
        {
            if (time < _nextAttackTime)
            {
                return false;
            }

            _nextAttackTime = time + Mathf.Max(0f, cooldown);
            if (definition != null)
                AudioManager.PlayWeaponSFX(definition.SoundCategory);
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

        public void PlayAttackPunch(Vector2 worldDirection, float time)
        {
            if (worldDirection.sqrMagnitude < 0.0001f)
            {
                return;
            }

            _punchDirection = worldDirection.normalized;
            _punchEndTime = time + AttackPunchDuration;

            if (spriteRenderer != null && Mathf.Abs(worldDirection.x) > 0.01f)
            {
                spriteRenderer.flipX = worldDirection.x < 0f;
            }
        }

        public void TickAnimation(float time)
        {
            TickHitIndicator(time);
            TickPunch(time);

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

        private void Evolve(int stageIndex)
        {
            _stageIndex = stageIndex;
            ApplyStageVisual(stageIndex);

            Color glow = new(1f, 0.93f, 0.45f);
            CombatEffects.SpawnShockwave(transform.position, glow, 1.4f, 0.45f);
            CombatEffects.SpawnImpactBurst(transform.position, glow, 1.2f);

            string stageName = CurrentStage != null && !string.IsNullOrEmpty(CurrentStage.StageName)
                ? CurrentStage.StageName
                : definition.DisplayName;
            CombatEffects.SpawnText(transform.position + Vector3.up * 0.8f, $"{stageName}!", glow, 3.5f);

            Evolved?.Invoke(this);
        }

        private void ApplyStageVisual(int stageIndex)
        {
            WeaponStage stage = definition != null ? definition.GetStage(stageIndex) : null;
            if (stage == null || stage.Sprite == null || spriteRenderer == null)
            {
                return;
            }

            spriteRenderer.sprite = stage.Sprite;
            _defaultSprite = stage.Sprite;
        }

        private void ResetRuntimeState()
        {
            _nextAttackTime = 0f;
            StopAttackAnimation();
        }

        private void TickPunch(float time)
        {
            if (_punchEndTime <= 0f)
            {
                return;
            }

            float remaining = _punchEndTime - time;
            if (remaining <= 0f)
            {
                transform.localPosition = _baseLocalPosition;
                _punchEndTime = 0f;
                return;
            }

            float strength = remaining / AttackPunchDuration;
            transform.localPosition = _baseLocalPosition + (Vector3)(_punchDirection * (AttackPunchDistance * strength));
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
