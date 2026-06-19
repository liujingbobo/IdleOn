using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace IdleOn.UI
{
    [RequireComponent(typeof(Image))]
    public class UISpriteAnimator : MonoBehaviour
    {
        [SerializeField] private List<Sprite> frames = new List<Sprite>();
        [SerializeField, Min(0.01f)] private float framesPerSecond = 12f;
        [SerializeField] private bool loop = true;
        [SerializeField] private bool playOnEnable = true;

        private Image _image;
        private int _frameIndex;
        private float _timer;
        private bool _isPlaying;

        private void Awake()
        {
            _image = GetComponent<Image>();
        }

        private void OnEnable()
        {
            if (playOnEnable)
                Play();
        }

        private void Update()
        {
            if (!_isPlaying || frames.Count == 0)
                return;

            _timer += Time.unscaledDeltaTime;
            float frameDuration = 1f / framesPerSecond;

            while (_timer >= frameDuration)
            {
                _timer -= frameDuration;
                _frameIndex++;

                if (_frameIndex >= frames.Count)
                {
                    if (loop)
                        _frameIndex = 0;
                    else
                    {
                        _frameIndex = frames.Count - 1;
                        _isPlaying = false;
                    }
                }

                _image.sprite = frames[_frameIndex];
            }
        }

        public void Play()
        {
            if (frames.Count == 0)
                return;

            _isPlaying = true;
            _frameIndex = 0;
            _timer = 0f;
            _image.sprite = frames[0];
        }

        public void Pause()
        {
            _isPlaying = false;
        }
    }
}
