using UnityEngine;

namespace Weapons.Effects
{
    [DisallowMultipleComponent]
    public class EffectFlash : MonoBehaviour
    {
        private SpriteRenderer _renderer;
        private float _elapsed;
        private float _duration;
        private float _startScale;
        private float _endScale;
        private float _rotationSpeed;
        private Vector2 _velocity;
        private Color _color;

        public void Play(
            Sprite sprite,
            Color color,
            float startScale,
            float endScale,
            float duration,
            float rotationDegrees,
            float rotationSpeed,
            Vector2 velocity,
            int sortingOrderOffset)
        {
            if (_renderer == null)
            {
                _renderer = GetComponent<SpriteRenderer>();
                if (_renderer == null)
                {
                    _renderer = gameObject.AddComponent<SpriteRenderer>();
                }
            }

            _renderer.sprite = sprite;
            _renderer.color = color;
            _renderer.sortingLayerID = CombatEffects.SortingLayerId;
            _renderer.sortingOrder = CombatEffects.SortingOrder + sortingOrderOffset;

            _color = color;
            _startScale = startScale;
            _endScale = endScale;
            _duration = Mathf.Max(0.01f, duration);
            _rotationSpeed = rotationSpeed;
            _velocity = velocity;
            _elapsed = 0f;

            transform.rotation = Quaternion.Euler(0f, 0f, rotationDegrees);
            transform.localScale = Vector3.one * startScale;
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(_elapsed / _duration);

            float scale = Mathf.Lerp(_startScale, _endScale, 1f - (1f - progress) * (1f - progress));
            transform.localScale = Vector3.one * scale;

            if (_rotationSpeed != 0f)
            {
                transform.Rotate(0f, 0f, _rotationSpeed * Time.deltaTime);
            }

            if (_velocity != Vector2.zero)
            {
                transform.position += (Vector3)(_velocity * Time.deltaTime);
            }

            Color color = _color;
            color.a = _color.a * (1f - progress);
            _renderer.color = color;

            if (progress >= 1f)
            {
                CombatEffects.Release(this);
            }
        }
    }
}
