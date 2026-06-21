using System;
using IdleOn.Talents;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace IdleOn.UI
{
    public class VaultSlotUI : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private Image      iconImage;
        [SerializeField] private TMP_Text   levelText;
        [SerializeField] private GameObject selectedIndicator;
        public void OnPointerClick(PointerEventData eventData)
        {
            throw new NotImplementedException();
        }
    }
}