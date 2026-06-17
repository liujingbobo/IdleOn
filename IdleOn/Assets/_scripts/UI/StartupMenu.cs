using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using IdleOn.Save;

namespace IdleOn.UI
{
    /// <summary>
    /// Minimal programmer-art startup flow: Main Menu (New / Load) → Character Select.
    /// Builds its own overlay Canvas at runtime and freezes gameplay (Time.timeScale = 0)
    /// until a character is selected. SelectCharacter() fires SaveManager.OnSaveLoaded, at
    /// which point the existing gameplay systems initialise normally; then time is restored.
    /// Add this component to one empty GameObject in the scene — it builds the rest itself.
    /// </summary>
    public class StartupMenu : MonoBehaviour
    {
        private GameObject    _mainPanel;
        private GameObject    _charPanel;
        private Transform     _charListRoot;
        private TMP_FontAsset _font;

        void Awake()
        {
            Time.timeScale = 0f;                       // freeze gameplay until selection
            _font = TMP_Settings.defaultFontAsset;

            BuildCanvas();
            _mainPanel = BuildMainPanel();
            _charPanel = BuildCharacterPanel();
            ShowMain();
        }

        // ── Canvas / EventSystem ────────────────────────────────────────────

        private void BuildCanvas()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5000;                // above the gameplay HUD

            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode        = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            gameObject.AddComponent<GraphicRaycaster>();

            // Reuse the existing EventSystem if present; otherwise create one at root
            // (NOT under this object, so it survives when the menu is destroyed).
            if (EventSystem.current == null)
            {
                var es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                es.transform.SetParent(null, false);
            }
        }

        // ── Panels ──────────────────────────────────────────────────────────

        private GameObject BuildMainPanel()
        {
            var panel = CreateFullscreen("MainMenuPanel");
            AddText(panel.transform, "IdleOn — Demo", 46, FontStyles.Bold);
            AddText(panel.transform, "Save / Load", 22);
            AddButton(panel.transform, "New Save", OnNewSave);

            var load = AddButton(panel.transform, "Load Save", OnLoadSave);
            load.interactable = SaveManager.Instance != null && SaveManager.Instance.HasSaveFile();
            return panel;
        }

        private GameObject BuildCharacterPanel()
        {
            var panel = CreateFullscreen("CharacterSelectPanel");
            AddText(panel.transform, "Select Character", 40, FontStyles.Bold);

            var listGo = new GameObject("CharList", typeof(RectTransform));
            listGo.transform.SetParent(panel.transform, false);
            var vlg = listGo.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment       = TextAnchor.UpperCenter;
            vlg.spacing              = 8;
            vlg.childForceExpandWidth = false;
            vlg.childForceExpandHeight = false;
            var le = listGo.AddComponent<LayoutElement>();
            le.preferredHeight = 340; le.preferredWidth = 360;
            _charListRoot = listGo.transform;

            AddButton(panel.transform, "Create New Character", OnCreateCharacter);
            panel.SetActive(false);
            return panel;
        }

        // ── Flow handlers ───────────────────────────────────────────────────

        private void OnNewSave()
        {
            SaveManager.Instance.CreateNewAccount();
            SaveManager.Instance.SaveAccountToDisk();   // create account_save.json immediately
            ShowCharacterSelect();
        }

        private void OnLoadSave()
        {
            if (!SaveManager.Instance.LoadAccountFromDisk())
                SaveManager.Instance.CreateNewAccount(); // safety fallback
            ShowCharacterSelect();
        }

        private void OnCreateCharacter()
        {
            SaveManager.Instance.CreateNewCharacter(null);
            SaveManager.Instance.SaveAccountToDisk();
            RefreshCharacterList();
        }

        private void OnSelectCharacter(string playerId)
        {
            if (SaveManager.Instance.SelectCharacter(playerId))
                EnterGameplay();
        }

        private void EnterGameplay()
        {
            SaveManager.Instance.SaveAccountToDisk();   // persist the selection
            Time.timeScale = 1f;
            Destroy(gameObject);                         // remove the overlay canvas
        }

        // ── View switching ──────────────────────────────────────────────────

        private void ShowMain()
        {
            _mainPanel.SetActive(true);
            _charPanel.SetActive(false);
        }

        private void ShowCharacterSelect()
        {
            _mainPanel.SetActive(false);
            _charPanel.SetActive(true);
            RefreshCharacterList();
        }

        private void RefreshCharacterList()
        {
            for (int i = _charListRoot.childCount - 1; i >= 0; i--)
                Destroy(_charListRoot.GetChild(i).gameObject);

            var acc = SaveManager.Instance.CurrentAccount;
            if (acc == null) return;

            if (acc.Players.Count == 0)
            {
                AddText(_charListRoot, "(no characters yet)", 20);
                return;
            }

            foreach (var p in acc.Players)
            {
                string id = p.PlayerId;
                AddButton(_charListRoot, $"{p.PlayerName}   (Lv.{p.Level})", () => OnSelectCharacter(id));
            }
        }

        // ── UI building helpers (programmer art) ────────────────────────────

        private GameObject CreateFullscreen(string name)
        {
            var go = new GameObject(name, typeof(Image));
            go.transform.SetParent(transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            go.GetComponent<Image>().color = new Color(0.05f, 0.06f, 0.09f, 0.98f);

            var vlg = go.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment        = TextAnchor.MiddleCenter;
            vlg.spacing               = 16;
            vlg.childForceExpandWidth  = false;
            vlg.childForceExpandHeight = false;
            return go;
        }

        private TMP_Text AddText(Transform parent, string txt, float size, FontStyles style = FontStyles.Normal)
        {
            var go = new GameObject("Text", typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<TextMeshProUGUI>();
            if (_font != null) t.font = _font;
            t.text      = txt;
            t.fontSize  = size;
            t.fontStyle = style;
            t.alignment = TextAlignmentOptions.Center;
            t.color     = Color.white;
            var le = go.AddComponent<LayoutElement>();
            le.preferredWidth = 420; le.preferredHeight = size + 14;
            return t;
        }

        private Button AddButton(Transform parent, string label, Action onClick)
        {
            var go = new GameObject("Button", typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = new Color(0.22f, 0.25f, 0.32f, 1f);
            var le = go.AddComponent<LayoutElement>();
            le.preferredWidth = 360; le.preferredHeight = 56;

            var t  = AddText(go.transform, label, 26);
            var rt = t.rectTransform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

            var btn = go.GetComponent<Button>();
            btn.onClick.AddListener(() => onClick());
            return btn;
        }
    }
}
