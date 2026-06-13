using UnityEngine;
using UnityEngine.UI;
using TMPro;
using IdleOn.Items;

namespace IdleOn.UI
{
    public class InventorySlotUI : MonoBehaviour
    {
        [SerializeField] private Image    itemIcon;
        [SerializeField] private TMP_Text stackCount;

        private static Sprite _placeholder;

        private static Sprite Placeholder
        {
            get
            {
                if (_placeholder != null) return _placeholder;
                var tex = new Texture2D(2, 2);
                var c   = new Color(0.35f, 0.35f, 0.35f, 1f);
                tex.SetPixels(new[] { c, c, c, c });
                tex.Apply();
                _placeholder = Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f));
                return _placeholder;
            }
        }

        public void Populate(InventorySlotData slot, Sprite icon)
        {
            itemIcon.sprite  = icon != null ? icon : Placeholder;
            itemIcon.enabled = true;

            if (slot.Quantity > 1)
            {
                stackCount.text    = "x" + slot.Quantity;
                stackCount.enabled = true;
            }
            else
            {
                stackCount.enabled = false;
            }
        }

        public void SetEmpty()
        {
            itemIcon.enabled   = false;
            stackCount.enabled = false;
        }
    }
}
