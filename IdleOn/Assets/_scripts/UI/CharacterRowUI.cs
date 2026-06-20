using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using IdleOn.Save;

namespace IdleOn.UI
{
    public class CharacterRowUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private Button   selectButton;
        [SerializeField] private Button   deleteButton; // not wired — SaveManager has no delete API yet

        private Action _onSelect;

        public void Bind(PlayerSaveData player, Action onSelect)
        {
            if (nameText != null)  nameText.text  = player.PlayerName;
            if (levelText != null) levelText.text = $"Lv. {player.Level}";

            _onSelect = onSelect;
            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(() => _onSelect?.Invoke());

            if (deleteButton != null) deleteButton.gameObject.SetActive(false);
        }
    }
}
