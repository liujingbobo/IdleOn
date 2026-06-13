using System;
using UnityEngine;
using TMPro;

namespace IdleOn.UI
{
    public class FloatText : MonoBehaviour
    {
        private TMP_Text _label;
        private float    _elapsed;
        private float    _duration   = 0.8f;
        private float    _floatSpeed = 1.5f;
        private Action<FloatText> _onComplete;

        void Awake()
        {
            _label = GetComponent<TMP_Text>();
            var mr = GetComponent<MeshRenderer>();
            if (mr != null) mr.sortingOrder = 10;
        }

        public void Spawn(string text, Color color, float fontSize, Action<FloatText> onComplete)
        {
            _label.text     = text;
            _label.color    = color;
            _label.fontSize = fontSize;
            _elapsed        = 0f;
            _onComplete     = onComplete;
        }

        void Update()
        {
            _elapsed += Time.deltaTime;
            float t = _elapsed / _duration;

            transform.position += Vector3.up * _floatSpeed * Time.deltaTime;

            // Full opacity for first 40%, then fade out
            float alpha = t < 0.4f ? 1f : 1f - ((t - 0.4f) / 0.6f);
            var c = _label.color;
            c.a = Mathf.Clamp01(alpha);
            _label.color = c;

            if (_elapsed >= _duration)
                _onComplete?.Invoke(this);
        }
    }
}
