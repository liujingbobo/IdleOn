using UnityEngine;

namespace IdleOn.VFX
{
    // Simplest sprite-frame animation player. Attach to a GameObject with a SpriteRenderer,
    // drag frames into the array, set fps. For quick pixel-effect previews.
    [RequireComponent(typeof(SpriteRenderer))]
    public class SpriteFramePlayer : MonoBehaviour
    {
        [SerializeField] private Sprite[] frames;
        [SerializeField] private float    fps             = 12f;
        [SerializeField] private bool     loop            = true;
        [SerializeField] private bool     destroyOnFinish = false; // only used when loop = false

        private SpriteRenderer _renderer;
        private float          _timer;
        private int            _index;

        void Awake() => _renderer = GetComponent<SpriteRenderer>();

        void OnEnable()
        {
            _timer = 0f;
            _index = 0;
            if (frames != null && frames.Length > 0)
                _renderer.sprite = frames[0];
        }

        void Update()
        {
            if (frames == null || frames.Length == 0 || fps <= 0f) return;

            _timer += Time.deltaTime;
            float frameTime = 1f / fps;
            if (_timer < frameTime) return;

            _timer -= frameTime;
            _index++;

            if (_index >= frames.Length)
            {
                if (loop)
                {
                    _index = 0;
                }
                else
                {
                    _index = frames.Length - 1;
                    _renderer.sprite = frames[_index];
                    if (destroyOnFinish) Destroy(gameObject);
                    else enabled = false;
                    return;
                }
            }

            _renderer.sprite = frames[_index];
        }
    }
}
