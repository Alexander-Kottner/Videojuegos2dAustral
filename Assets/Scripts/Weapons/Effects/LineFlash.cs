using System.Collections.Generic;
using UnityEngine;

namespace Weapons.Effects
{
    [DisallowMultipleComponent]
    public class LineFlash : MonoBehaviour
    {
        private LineRenderer _line;
        private float _elapsed;
        private float _duration;
        private float _width;
        private Color _color;

        public void Play(IReadOnlyList<Vector3> points, Color color, float width, float duration)
        {
            EnsureLine();

            _line.positionCount = points.Count;
            for (int i = 0; i < points.Count; i++)
            {
                _line.SetPosition(i, points[i]);
            }

            _color = color;
            _width = Mathf.Max(0.005f, width);
            _duration = Mathf.Max(0.01f, duration);
            _elapsed = 0f;

            _line.startWidth = _width;
            _line.endWidth = _width;
            _line.startColor = color;
            _line.endColor = color;
            _line.enabled = true;
        }

        private void EnsureLine()
        {
            if (_line != null)
            {
                return;
            }

            _line = GetComponent<LineRenderer>();
            if (_line == null)
            {
                _line = gameObject.AddComponent<LineRenderer>();
            }

            _line.useWorldSpace = true;
            _line.numCapVertices = 2;
            _line.numCornerVertices = 2;
            _line.sortingLayerID = CombatEffects.SortingLayerId;
            _line.sortingOrder = CombatEffects.SortingOrder + 1;

            if (_line.sharedMaterial == null)
            {
                Shader shader = Shader.Find("Sprites/Default");
                if (shader != null)
                {
                    _line.sharedMaterial = new Material(shader);
                }
            }
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(_elapsed / _duration);

            Color color = _color;
            color.a = _color.a * (1f - progress);
            _line.startColor = color;
            _line.endColor = color;

            float width = _width * (1f - progress * 0.6f);
            _line.startWidth = width;
            _line.endWidth = width;

            if (progress >= 1f)
            {
                _line.enabled = false;
                CombatEffects.Release(this);
            }
        }
    }
}
