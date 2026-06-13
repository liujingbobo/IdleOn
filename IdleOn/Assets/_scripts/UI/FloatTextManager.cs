using System.Collections.Generic;
using UnityEngine;

namespace IdleOn.UI
{
    public class FloatTextManager : MonoBehaviour
    {
        public static FloatTextManager Instance { get; private set; }

        [SerializeField] private FloatText prefab;
        [SerializeField] private int initialPoolSize = 10;

        private readonly Queue<FloatText> _pool = new Queue<FloatText>();

        private static readonly Color ColorPhysical = new Color(1f,   0.55f, 0.1f);
        private static readonly Color ColorMagic    = new Color(0.3f, 0.6f,  1f);
        private static readonly Color ColorHeal     = new Color(0.2f, 0.85f, 0.2f);

        private const float BaseFontSize     = 2.5f;
        private const float CriticalFontSize = 4f;

        void Awake()
        {
            Instance = this;
            for (int i = 0; i < initialPoolSize; i++)
                _pool.Enqueue(CreateInstance());
        }

        public static void Show(string text, Vector3 worldPos, FloatTextType type, bool isCritical = false)
        {
            if (Instance == null) return;

            Color color = type == FloatTextType.Magic ? ColorMagic
                        : type == FloatTextType.Heal  ? ColorHeal
                        : ColorPhysical;

            float fontSize = isCritical ? CriticalFontSize : BaseFontSize;

            var ft = Instance.GetFromPool();
            ft.transform.position = worldPos;
            ft.gameObject.SetActive(true);
            ft.Spawn(text, color, fontSize, Instance.Return);
        }

        private void Return(FloatText ft)
        {
            ft.gameObject.SetActive(false);
            _pool.Enqueue(ft);
        }

        private FloatText GetFromPool()
        {
            if (_pool.Count > 0)
                return _pool.Dequeue();
            return CreateInstance();
        }

        private FloatText CreateInstance()
        {
            var ft = Instantiate(prefab, transform);
            ft.gameObject.SetActive(false);
            return ft;
        }
    }
}
