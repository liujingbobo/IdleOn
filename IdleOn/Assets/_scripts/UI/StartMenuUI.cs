using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using IdleOn.Save;

namespace IdleOn.UI
{
    // Scene-authored Boot/StartMenu flow. This script only binds refs, fills data, and handles
    // buttons — all visuals (panels, backgrounds, buttons, row layout) are real scene/prefab
    // GameObjects edited in the Unity Editor. No runtime UI generation.
    //
    // New Save / Load Save both just open CharacterSelectPanel (account-level only, no character
    // chosen yet). NewCharacterButton only creates a row. Entering TestCombat happens exclusively
    // from a CharacterRowUI's own SelectButton — no separate Continue step.
    public class StartMenuUI : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject characterSelectPanel;

        [Header("Main Menu")]
        [SerializeField] private Button newSaveButton;
        [SerializeField] private Button loadSaveButton;

        [Header("Character Select")]
        [SerializeField] private Transform     charListContainer;
        [SerializeField] private CharacterRowUI characterRowPrefab;
        [SerializeField] private Button         newCharacterButton;

        [Header("Gameplay")]
        [SerializeField] private string gameplaySceneName = "TestCombat";

        private readonly List<CharacterRowUI> _rows = new List<CharacterRowUI>();

        void Awake()
        {
            newSaveButton.onClick.AddListener(OnNewSave);
            loadSaveButton.onClick.AddListener(OnLoadSave);
            newCharacterButton.onClick.AddListener(OnCreateCharacter);

            loadSaveButton.interactable = SaveManager.Instance != null && SaveManager.Instance.HasSaveFile();

            ShowMain();
        }

        private void ShowMain()
        {
            mainMenuPanel.SetActive(true);
            characterSelectPanel.SetActive(false);
        }

        private void ShowCharacterSelect()
        {
            mainMenuPanel.SetActive(false);
            characterSelectPanel.SetActive(true);
            RefreshCharacterList();
        }

        private void OnNewSave()
        {
            SaveManager.Instance.CreateNewAccount();
            SaveManager.Instance.SaveAccountToDisk();
            ShowCharacterSelect();
        }

        private void OnLoadSave()
        {
            if (!SaveManager.Instance.LoadAccountFromDisk())
                SaveManager.Instance.CreateNewAccount();
            ShowCharacterSelect();
        }

        private void OnCreateCharacter()
        {
            SaveManager.Instance.CreateNewCharacter(null);
            SaveManager.Instance.SaveAccountToDisk();
            RefreshCharacterList();
        }

        private void OnSelectCharacterRow(string playerId)
        {
            if (SaveManager.Instance.SelectCharacter(playerId))
                EnterGameplay();
        }

        private void RefreshCharacterList()
        {
            foreach (var row in _rows)
                if (row != null) Destroy(row.gameObject);
            _rows.Clear();

            var acc = SaveManager.Instance.CurrentAccount;
            if (acc == null || characterRowPrefab == null || charListContainer == null) return;

            foreach (var p in acc.Players)
            {
                var row = Instantiate(characterRowPrefab, charListContainer);
                string id = p.PlayerId;
                row.Bind(p, () => OnSelectCharacterRow(id));
                _rows.Add(row);
            }
        }

        private void EnterGameplay()
        {
            SaveManager.Instance.SaveAccountToDisk();
            SceneManager.LoadScene(gameplaySceneName);
        }
    }
}
