using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IdleOn.UI
{
    public class EarningSlotUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text amountText;
        [SerializeField] private Sprite coin;
        [SerializeField] private Sprite essence;
        [SerializeField] private Image icon;
        
        public void SetData(string label, long amount)
        {
            if (amountText != null)
                amountText.text = $"{label} x{amount}";

            if (label != string.Empty)
            {
                if (icon != null)
                {
                    if (label.ToLower().Contains("Gold"))
                        icon.sprite = coin;
                    else if (label.ToLower().Contains("Slime Essence"))
                        icon.sprite = essence;
                }
            }
        }
    }
}
