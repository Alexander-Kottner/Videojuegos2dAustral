using TMPro;
using UnityEngine;

namespace Weapons.Effects
{
    [DisallowMultipleComponent]
    public class DamageNumber : MonoBehaviour
    {
        private const float Lifetime = 0.6f;
        private const float RiseSpeed = 1.1f;

        private TextMeshPro _text;
        private float _elapsed;
        private Color _color;

        public void Play(string text, Color color, float fontSize)
        {
            if (_text == null)
            {
                _text = GetComponent<TextMeshPro>();
                if (_text == null)
                {
                    _text = gameObject.AddComponent<TextMeshPro>();
                }

                _text.alignment = TextAlignmentOptions.Center;
                _text.textWrappingMode = TextWrappingModes.NoWrap;

                MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
                if (meshRenderer != null)
                {
                    meshRenderer.sortingLayerID = CombatEffects.SortingLayerId;
                    meshRenderer.sortingOrder = CombatEffects.SortingOrder + 20;
                }
            }

            _text.text = text;
            _text.fontSize = fontSize;
            _text.color = color;
            _color = color;
            _elapsed = 0f;
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(_elapsed / Lifetime);

            transform.position += Vector3.up * (RiseSpeed * Time.deltaTime);

            Color color = _color;
            color.a = _color.a * (1f - progress * progress);
            _text.color = color;

            if (progress >= 1f)
            {
                CombatEffects.Release(this);
            }
        }
    }
}
