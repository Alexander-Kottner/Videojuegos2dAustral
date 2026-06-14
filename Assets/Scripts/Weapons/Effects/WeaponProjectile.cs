using System.Collections.Generic;
using Enemies;
using UnityEngine;

namespace Weapons.Effects
{
    [DisallowMultipleComponent]
    public class WeaponProjectile : MonoBehaviour
    {
        public struct LaunchConfig
        {
            public WeaponController Source;
            public Sprite Sprite;
            public Color SpriteTint;
            public Color Color;
            public Vector3 Origin;
            public Vector2 Direction;
            public float Speed;
            public float MaxDistance;
            public int Damage;
            public int Pierces;
            public int Bounces;
            public bool ReturnToOwner;
            public bool Homing;
            public float SpinSpeed;
            public float HitRadius;
            public float ExplodeRadius;
            public float Scale;
            public bool EmitTrail;
        }

        private const float MaxLifetime = 8f;
        private const float HomingTurnSpeed = 240f;
        private const float BounceSearchRadius = 5f;

        private static readonly List<EnemyHealth> HitBuffer = new();

        private readonly HashSet<EnemyHealth> _alreadyHit = new();

        private SpriteRenderer _renderer;
        private LaunchConfig _config;
        private Vector2 _direction;
        private float _traveled;
        private float _lifetime;
        private float _nextTrailTime;
        private int _piercesLeft;
        private int _bouncesLeft;
        private bool _returning;
        private EnemyHealth _homingTarget;

        public void Launch(in LaunchConfig config)
        {
            if (_renderer == null)
            {
                _renderer = GetComponent<SpriteRenderer>();
                if (_renderer == null)
                {
                    _renderer = gameObject.AddComponent<SpriteRenderer>();
                }
            }

            _config = config;
            _direction = config.Direction.sqrMagnitude > 0.0001f ? config.Direction.normalized : Vector2.right;
            _traveled = 0f;
            _lifetime = 0f;
            _nextTrailTime = 0f;
            _piercesLeft = config.Pierces;
            _bouncesLeft = config.Bounces;
            _returning = false;
            _homingTarget = null;
            _alreadyHit.Clear();

            _renderer.sprite = config.Sprite;
            _renderer.color = config.SpriteTint;
            _renderer.sortingLayerID = CombatEffects.SortingLayerId;
            _renderer.sortingOrder = CombatEffects.SortingOrder + 2;

            transform.position = config.Origin;
            transform.localScale = Vector3.one * Mathf.Max(0.05f, config.Scale);
            FaceDirection();
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            _lifetime += deltaTime;
            if (_lifetime > MaxLifetime)
            {
                Despawn();
                return;
            }

            if (_returning)
            {
                TickReturn(deltaTime);
            }
            else
            {
                TickFlight(deltaTime);
            }

            if (_config.SpinSpeed != 0f)
            {
                transform.Rotate(0f, 0f, _config.SpinSpeed * deltaTime);
            }

            if (_config.EmitTrail && Time.time >= _nextTrailTime)
            {
                _nextTrailTime = Time.time + 0.05f;
                Color trail = _config.Color;
                trail.a *= 0.5f;
                CombatEffects.SpawnFlash(CombatEffects.SoftCircleSprite, transform.position, trail,
                    _config.Scale * 0.5f, _config.Scale * 0.1f, 0.25f, sortingOrderOffset: -1);
            }

            CheckHits();
        }

        private void TickFlight(float deltaTime)
        {
            if (_config.Homing)
            {
                if (_homingTarget == null || !_homingTarget.IsAlive)
                {
                    _homingTarget = WeaponTargeting.FindNearest(transform.position, _config.MaxDistance, _alreadyHit);
                }

                if (_homingTarget != null)
                {
                    Vector2 toTarget = ((Vector2)_homingTarget.transform.position - (Vector2)transform.position).normalized;
                    float maxRadians = HomingTurnSpeed * Mathf.Deg2Rad * deltaTime;
                    _direction = Vector3.RotateTowards(_direction, toTarget, maxRadians, 0f).normalized;
                    FaceDirection();
                }
            }

            float step = _config.Speed * deltaTime;
            transform.position += (Vector3)(_direction * step);
            _traveled += step;

            if (_traveled < _config.MaxDistance)
            {
                return;
            }

            if (_config.ReturnToOwner && _config.Source != null)
            {
                _returning = true;
                _alreadyHit.Clear();
                return;
            }

            if (_config.ExplodeRadius > 0f)
            {
                Explode(transform.position);
                return;
            }

            Despawn();
        }

        private void TickReturn(float deltaTime)
        {
            if (_config.Source == null)
            {
                Despawn();
                return;
            }

            Vector3 ownerPosition = _config.Source.transform.position;
            Vector2 toOwner = ownerPosition - transform.position;
            if (toOwner.magnitude <= 0.45f)
            {
                Despawn();
                return;
            }

            _direction = toOwner.normalized;
            FaceDirection();
            transform.position += (Vector3)(_direction * (_config.Speed * 1.2f * deltaTime));
        }

        private void CheckHits()
        {
            IReadOnlyList<EnemyHealth> alive = EnemyHealth.AliveEnemies;
            float radius = _config.HitRadius + WeaponTargeting.EnemyBodyRadius;
            float radiusSqr = radius * radius;
            Vector2 position = transform.position;

            for (int i = alive.Count - 1; i >= 0; i--)
            {
                if (i >= alive.Count)
                {
                    continue;
                }

                EnemyHealth enemy = alive[i];
                if (enemy == null || !enemy.IsAlive || _alreadyHit.Contains(enemy))
                {
                    continue;
                }

                if (((Vector2)enemy.transform.position - position).sqrMagnitude > radiusSqr)
                {
                    continue;
                }

                _alreadyHit.Add(enemy);

                if (_config.ExplodeRadius > 0f)
                {
                    Explode(enemy.transform.position);
                    return;
                }

                CombatDamage.Apply(_config.Source, enemy, _config.Damage, _config.Color);
                CombatEffects.SpawnImpactBurst(enemy.transform.position, _config.Color);

                if (_piercesLeft > 0)
                {
                    _piercesLeft--;
                    continue;
                }

                if (_bouncesLeft > 0)
                {
                    EnemyHealth next = WeaponTargeting.FindNearest(transform.position, BounceSearchRadius, _alreadyHit);
                    if (next != null)
                    {
                        _bouncesLeft--;
                        _direction = ((Vector2)next.transform.position - position).normalized;
                        _traveled = 0f;
                        FaceDirection();
                        continue;
                    }
                }

                if (!_returning && _config.ReturnToOwner && _config.Source != null)
                {
                    _returning = true;
                    _alreadyHit.Clear();
                    _alreadyHit.Add(enemy);
                    return;
                }

                Despawn();
                return;
            }
        }

        private void Explode(Vector3 center)
        {
            CombatEffects.SpawnExplosion(center, _config.Color, _config.ExplodeRadius);
            WeaponTargeting.CollectInRadius(center, _config.ExplodeRadius, HitBuffer);

            for (int i = 0; i < HitBuffer.Count; i++)
            {
                CombatDamage.Apply(_config.Source, HitBuffer[i], _config.Damage, _config.Color);
            }

            Despawn();
        }

        private void FaceDirection()
        {
            if (_config.SpinSpeed == 0f)
            {
                float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0f, 0f, angle - 45f);
            }
        }

        private void Despawn()
        {
            _alreadyHit.Clear();
            CombatEffects.Release(this);
        }
    }
}
