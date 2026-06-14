using System;
using System.Collections.Generic;
using UnityEngine;

namespace Weapons.Effects
{
    /// <summary>
    /// Runtime factory and pool for lightweight 2D combat visuals. Sprites are generated
    /// procedurally once and shared, effect instances are pooled per component type.
    /// </summary>
    public static class CombatEffects
    {
        private const int MaxActiveDamageNumbers = 48;

        private static Transform _root;
        private static readonly Dictionary<Type, Stack<Component>> Pools = new();
        private static readonly Dictionary<int, Sprite> WedgeSprites = new();

        private static Sprite _circleSprite;
        private static Sprite _softCircleSprite;
        private static Sprite _ringSprite;
        private static Sprite _sparkSprite;

        private static int _sortingLayerId;
        private static int _sortingOrder = 10;
        private static int _activeDamageNumbers;

        public static int SortingLayerId => _sortingLayerId;
        public static int SortingOrder => _sortingOrder;

        public static void ConfigureSorting(int sortingLayerId, int sortingOrder)
        {
            _sortingLayerId = sortingLayerId;
            _sortingOrder = sortingOrder;
        }

        public static Sprite CircleSprite => _circleSprite != null ? _circleSprite : _circleSprite = CreateCircleSprite(48, false);
        public static Sprite SoftCircleSprite => _softCircleSprite != null ? _softCircleSprite : _softCircleSprite = CreateCircleSprite(48, true);
        public static Sprite RingSprite => _ringSprite != null ? _ringSprite : _ringSprite = CreateRingSprite(64, 0.12f);
        public static Sprite SparkSprite => _sparkSprite != null ? _sparkSprite : _sparkSprite = CreateSparkSprite(32);

        public static Sprite GetWedgeSprite(float arcAngleDegrees)
        {
            int bucket = Mathf.Clamp(Mathf.RoundToInt(arcAngleDegrees / 15f) * 15, 15, 360);
            if (!WedgeSprites.TryGetValue(bucket, out Sprite sprite) || sprite == null)
            {
                sprite = CreateWedgeSprite(96, bucket);
                WedgeSprites[bucket] = sprite;
            }

            return sprite;
        }

        public static EffectFlash SpawnFlash(
            Sprite sprite,
            Vector3 position,
            Color color,
            float startScale,
            float endScale,
            float duration,
            float rotationDegrees = 0f,
            float rotationSpeed = 0f,
            Vector2 velocity = default,
            int sortingOrderOffset = 0)
        {
            EffectFlash flash = Get<EffectFlash>();
            flash.transform.position = position;
            flash.Play(sprite, color, startScale, endScale, duration, rotationDegrees, rotationSpeed, velocity, sortingOrderOffset);
            return flash;
        }

        public static void SpawnSlashArc(Vector3 origin, Vector2 direction, float range, float arcAngleDegrees, Color color)
        {
            float rotation = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            Sprite wedge = arcAngleDegrees >= 350f ? CircleSprite : GetWedgeSprite(arcAngleDegrees);
            Color faded = color;
            faded.a *= 0.45f;
            SpawnFlash(wedge, origin, faded, range * 1.5f, range * 2.1f, 0.16f, rotation);
        }

        public static void SpawnImpactBurst(Vector3 position, Color color, float scale = 0.55f)
        {
            SpawnFlash(SparkSprite, position, color, scale * 0.5f, scale, 0.16f, UnityEngine.Random.Range(0f, 360f));
        }

        public static void SpawnShockwave(Vector3 position, Color color, float radius, float duration = 0.3f)
        {
            SpawnFlash(RingSprite, position, color, radius * 0.4f, radius * 2f, duration);
        }

        public static void SpawnExplosion(Vector3 position, Color color, float radius)
        {
            Color core = Color.Lerp(color, Color.white, 0.4f);
            SpawnFlash(SoftCircleSprite, position, core, radius * 0.6f, radius * 2f, 0.22f);
            SpawnShockwave(position, color, radius, 0.26f);
        }

        public static LineFlash SpawnLine(IReadOnlyList<Vector3> points, Color color, float width, float duration)
        {
            LineFlash line = Get<LineFlash>();
            line.Play(points, color, width, duration);
            return line;
        }

        public static void SpawnBeam(Vector3 from, Vector3 to, Color color, float width, float duration = 0.14f)
        {
            Color glow = color;
            glow.a *= 0.35f;
            SpawnLine(new[] { from, to }, glow, width * 2.2f, duration + 0.05f);
            SpawnLine(new[] { from, to }, Color.Lerp(color, Color.white, 0.6f), width * 0.7f, duration);
        }

        public static void SpawnDamageNumber(Vector3 position, int amount, Color color)
        {
            SpawnText(position + (Vector3)(UnityEngine.Random.insideUnitCircle * 0.25f) + Vector3.up * 0.35f,
                amount.ToString(), color, 3f);
        }

        public static void SpawnText(Vector3 position, string text, Color color, float fontSize)
        {
            if (_activeDamageNumbers >= MaxActiveDamageNumbers)
            {
                return;
            }

            DamageNumber number = Get<DamageNumber>();
            number.transform.position = position;
            number.Play(text, color, fontSize);
            _activeDamageNumbers++;
        }

        public static WeaponProjectile GetProjectile()
        {
            return Get<WeaponProjectile>();
        }

        public static void Release(Component effect)
        {
            if (effect == null)
            {
                return;
            }

            if (effect is DamageNumber)
            {
                _activeDamageNumbers = Mathf.Max(0, _activeDamageNumbers - 1);
            }

            effect.gameObject.SetActive(false);
            Type type = effect.GetType();
            if (!Pools.TryGetValue(type, out Stack<Component> pool))
            {
                pool = new Stack<Component>();
                Pools[type] = pool;
            }

            pool.Push(effect);
        }

        private static T Get<T>() where T : Component
        {
            Type type = typeof(T);
            if (Pools.TryGetValue(type, out Stack<Component> pool))
            {
                while (pool.Count > 0)
                {
                    Component pooled = pool.Pop();
                    if (pooled != null)
                    {
                        pooled.gameObject.SetActive(true);
                        return (T)pooled;
                    }
                }
            }

            GameObject go = new(type.Name);
            go.transform.SetParent(EnsureRoot(), false);
            return go.AddComponent<T>();
        }

        private static Transform EnsureRoot()
        {
            if (_root == null)
            {
                Pools.Clear();
                _activeDamageNumbers = 0;
                _root = new GameObject("CombatEffects (Runtime)").transform;
            }

            return _root;
        }

        private static Sprite CreateCircleSprite(int size, bool soft)
        {
            return CreateSprite(size, (x, y) =>
            {
                float distance = Distance01(x, y, size);
                if (distance > 1f)
                {
                    return 0f;
                }

                return soft ? Mathf.Pow(1f - distance, 1.6f) : (distance > 0.92f ? (1f - distance) / 0.08f : 1f);
            });
        }

        private static Sprite CreateRingSprite(int size, float thickness)
        {
            return CreateSprite(size, (x, y) =>
            {
                float distance = Distance01(x, y, size);
                float delta = Mathf.Abs(distance - (1f - thickness));
                return delta > thickness ? 0f : 1f - delta / thickness;
            });
        }

        private static Sprite CreateSparkSprite(int size)
        {
            return CreateSprite(size, (x, y) =>
            {
                float cx = (x + 0.5f) / size * 2f - 1f;
                float cy = (y + 0.5f) / size * 2f - 1f;
                float star = Mathf.Min(Mathf.Abs(cx) + Mathf.Abs(cy) * 4f, Mathf.Abs(cy) + Mathf.Abs(cx) * 4f);
                return Mathf.Clamp01(1f - star);
            });
        }

        private static Sprite CreateWedgeSprite(int size, float arcAngleDegrees)
        {
            float halfArcRadians = Mathf.Clamp(arcAngleDegrees, 1f, 360f) * 0.5f * Mathf.Deg2Rad;
            return CreateSprite(size, (x, y) =>
            {
                float cx = (x + 0.5f) / size * 2f - 1f;
                float cy = (y + 0.5f) / size * 2f - 1f;
                float distance = Mathf.Sqrt(cx * cx + cy * cy);
                if (distance > 1f || distance < 0.05f)
                {
                    return 0f;
                }

                float angle = Mathf.Abs(Mathf.Atan2(cy, cx));
                if (angle > halfArcRadians)
                {
                    return 0f;
                }

                float edgeFade = Mathf.Clamp01((halfArcRadians - angle) / Mathf.Max(0.05f, halfArcRadians * 0.25f));
                float radialFade = Mathf.Clamp01((1f - distance) / 0.25f);
                return Mathf.Min(edgeFade, radialFade) * Mathf.Clamp01(distance * 2.5f);
            });
        }

        private static float Distance01(int x, int y, int size)
        {
            float cx = (x + 0.5f) / size * 2f - 1f;
            float cy = (y + 0.5f) / size * 2f - 1f;
            return Mathf.Sqrt(cx * cx + cy * cy);
        }

        private static Sprite CreateSprite(int size, Func<int, int, float> alphaAt)
        {
            Texture2D texture = new(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };

            Color32[] pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    byte alpha = (byte)(Mathf.Clamp01(alphaAt(x, y)) * 255f);
                    pixels[y * size + x] = new Color32(255, 255, 255, alpha);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);

            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }
    }
}
