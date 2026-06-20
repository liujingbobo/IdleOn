using UnityEngine;
using UnityEngine.UI;
using TMPro;
using IdleOn.Core;
using IdleOn.Items;
using IdleOn.Inventory;
using IdleOn.Characters;
using IdleOn.Combat;
using IdleOn.Save;

namespace IdleOn.UI
{
    public class MainHUD : MonoBehaviour
    {
        [Header("Character Panel")]
        [SerializeField] private TextMeshProUGUI nameLevelText;
        [SerializeField] private Slider          hpSlider;
        [SerializeField] private TextMeshProUGUI hpText;
        [SerializeField] private Slider          mpSlider;
        [SerializeField] private TextMeshProUGUI mpText;
        [SerializeField] private Slider          xpSlider;
        [SerializeField] private TextMeshProUGUI xpText;
        [SerializeField] private TextMeshProUGUI goldText;

        [Header("Windows")]
        [SerializeField] private ItemWindow     itemWindow;
        [SerializeField] private CraftingWindow craftingWindow;
        [SerializeField] private VaultWindow    vaultWindow;
        [SerializeField] private MapWindow      mapWindow;
        [SerializeField] private TalentWindow   talentWindow;

        [Header("Combat")]
        [SerializeField] private PlayerCombatController combatController;
        [SerializeField] private TextMeshProUGUI         autoCombatButtonText;
        [SerializeField] private Button                  autoCombatButton;

        [Header("Progression")]
        [SerializeField] private PlayerProgression playerProgression;

        [Header("Button Bar")]
        [SerializeField] private Button invButton;
        [SerializeField] private Button craftButton;
        [SerializeField] private Button vaultButton;
        [SerializeField] private Button talentButtonRef;
        [SerializeField] private Button mapButton;

        [Header("Button State Sprites")]
        [SerializeField] private Sprite buttonOnSprite;
        [SerializeField] private Sprite buttonOffSprite;

        private enum WindowType { None, Inventory, Crafting, Vault, Talent, Map }
        private WindowType _currentWindow = WindowType.None;

        private bool _pendingRefresh;

        private Image _autoCombatBg;
        private Image _invButtonBg;
        private Image _craftButtonBg;
        private Image _vaultButtonBg;
        private Image _talentButtonBg;
        private Image _mapButtonBg;

        void Awake()
        {
            if (autoCombatButton != null) _autoCombatBg   = autoCombatButton.GetComponent<Image>();
            if (invButton != null)        _invButtonBg    = invButton.GetComponent<Image>();
            if (craftButton != null)      _craftButtonBg  = craftButton.GetComponent<Image>();
            if (vaultButton != null)      _vaultButtonBg  = vaultButton.GetComponent<Image>();
            if (talentButtonRef != null)  _talentButtonBg = talentButtonRef.GetComponent<Image>();
            if (mapButton != null)        _mapButtonBg    = mapButton.GetComponent<Image>();

            SaveManager.OnSaveLoaded         += OnSaveLoaded;
            GameEvents.OnPlayerHPChanged    += OnHPChanged;
            GameEvents.OnPlayerMPChanged    += OnMPChanged;
            GameEvents.OnCurrencyChanged    += OnCurrencyChanged;
            GameEvents.OnPlayerExpGained    += OnExpGained;
            GameEvents.OnAutoCombatChanged  += OnAutoCombatChanged;
            GameEvents.OnEquipmentChanged   += OnEquipmentChanged;
            GameEvents.OnPlayerLevelChanged += OnLevelChanged;
            GameEvents.OnTalentChanged      += OnTalentChanged;
        }

        void OnDestroy()
        {
            SaveManager.OnSaveLoaded         -= OnSaveLoaded;
            GameEvents.OnPlayerHPChanged    -= OnHPChanged;
            GameEvents.OnPlayerMPChanged    -= OnMPChanged;
            GameEvents.OnCurrencyChanged    -= OnCurrencyChanged;
            GameEvents.OnPlayerExpGained    -= OnExpGained;
            GameEvents.OnAutoCombatChanged  -= OnAutoCombatChanged;
            GameEvents.OnEquipmentChanged   -= OnEquipmentChanged;
            GameEvents.OnPlayerLevelChanged -= OnLevelChanged;
            GameEvents.OnTalentChanged      -= OnTalentChanged;
        }

        void Start()
        {
            RefreshAll();
        }

        void Update()
        {
            // OnSaveLoaded fires synchronously across all subscribers; defer one consume so
            // PlayerProgression/etc. have finished initializing before we read their values.
            if (_pendingRefresh) { _pendingRefresh = false; RefreshAll(); }
            RefreshButtonStates();
        }

        public void RefreshAll()
        {
            RefreshNameLevel();
            RefreshHP();
            RefreshMP();
            RefreshXP();
            RefreshCurrency();
            RefreshAutoCombat();
            RefreshButtonStates();
        }

        // ── Button callbacks (wired in Inspector via Button.onClick) ─────────

        public void OnInventoryButtonClicked()   => ToggleWindow(WindowType.Inventory);
        public void OnCraftButtonClicked()        => ToggleWindow(WindowType.Crafting);
        public void OnVaultButtonClicked()        => ToggleWindow(WindowType.Vault);

        public void OnAutoCombatButtonClicked()
        {
            if (combatController != null)
                combatController.SetAutoCombat(!combatController.IsAutoCombatActive);
        }

        public void OnTalentButtonClicked()   => ToggleWindow(WindowType.Talent);
        public void OnQuestButtonClicked()    => Debug.Log("[MainHUD] Quest not implemented yet.");
        public void OnMapButtonClicked()      => ToggleWindow(WindowType.Map);
        public void OnSettingsButtonClicked() => Debug.Log("[MainHUD] Settings not implemented yet.");

        // ── Window switching ─────────────────────────────────────────────────

        private void ToggleWindow(WindowType target)
        {
            if (IsWindowOpen(target))
            {
                CloseWindow(target);
                _currentWindow = WindowType.None;
                RefreshButtonStates();
                return;
            }

            if (_currentWindow != WindowType.None)
                CloseWindow(_currentWindow);

            OpenWindow(target);
            _currentWindow = target;
            RefreshButtonStates();
        }

        // Open-only entry for external callers (e.g. a walked-up CraftingStation). Reuses the central
        // switching: closes whatever MainHUD-managed window is open, opens Crafting, refreshes button
        // sprites. NOT a toggle — if Crafting is already open it stays open.
        public void OpenCraftingWindow()
        {
            if (IsWindowOpen(WindowType.Crafting)) { RefreshButtonStates(); return; }

            if (_currentWindow != WindowType.None)
                CloseWindow(_currentWindow);

            OpenWindow(WindowType.Crafting);
            _currentWindow = WindowType.Crafting;
            RefreshButtonStates();
        }

        private bool IsWindowOpen(WindowType type)
        {
            switch (type)
            {
                case WindowType.Inventory: return itemWindow != null && itemWindow.IsOpen;
                case WindowType.Crafting:  return craftingWindow != null && craftingWindow.IsOpen;
                case WindowType.Vault:     return vaultWindow != null && vaultWindow.IsOpen;
                case WindowType.Talent:    return talentWindow != null && talentWindow.IsOpen;
                case WindowType.Map:       return mapWindow != null && mapWindow.IsOpen;
                default:                   return false;
            }
        }

        private void OpenWindow(WindowType type)
        {
            switch (type)
            {
                case WindowType.Inventory: itemWindow?.Open();     break;
                case WindowType.Crafting:  craftingWindow?.Open();  break;
                case WindowType.Vault:     vaultWindow?.Open();     break;
                case WindowType.Talent:    talentWindow?.Open();    break;
                case WindowType.Map:       mapWindow?.Open();       break;
            }
        }

        private void CloseWindow(WindowType type)
        {
            switch (type)
            {
                case WindowType.Inventory: itemWindow?.Close();     break;
                case WindowType.Crafting:  craftingWindow?.Close();  break;
                case WindowType.Vault:     vaultWindow?.Close();     break;
                case WindowType.Talent:    talentWindow?.Close();    break;
                case WindowType.Map:       mapWindow?.Close();       break;
            }
        }

        // ── Event handlers ───────────────────────────────────────────────────

        private void OnHPChanged(float current, float max)
        {
            if (hpSlider != null) hpSlider.value = max > 0f ? current / max : 0f;
            if (hpText != null)   hpText.text    = $"{Mathf.CeilToInt(current)}/{Mathf.CeilToInt(max)}";
        }

        private void OnMPChanged(float current, float max)
        {
            if (mpSlider != null) mpSlider.value = max > 0f ? current / max : 0f;
            if (mpText != null)   mpText.text    = $"{Mathf.CeilToInt(current)}/{Mathf.CeilToInt(max)}";
        }

        private void OnCurrencyChanged(CurrencyType type, long newTotal)
        {
            if (type == CurrencyType.Gold && goldText != null)
                goldText.text = $"{newTotal}";
        }

        private void OnExpGained(float delta)          => RefreshXP();
        private void OnLevelChanged(int newLevel)      { RefreshNameLevel(); RefreshXP(); }

        private void OnAutoCombatChanged(bool active)
        {
            if (autoCombatButtonText != null)
                autoCombatButtonText.text = active ? "Auto: ON" : "Auto: OFF";
            if (_autoCombatBg != null)
                _autoCombatBg.sprite = active ? buttonOnSprite : buttonOffSprite;
        }

        private void OnEquipmentChanged() => RefreshMP();
        private void OnTalentChanged()     => RefreshMP();

        // Character just loaded/selected — defer full refresh so other systems finish initializing first.
        private void OnSaveLoaded() => _pendingRefresh = true;

        // ── Refresh helpers ──────────────────────────────────────────────────

        private void RefreshNameLevel()
        {
            if (nameLevelText == null) return;
            int level = playerProgression != null ? playerProgression.Level : 1;
            nameLevelText.text = $"Lv. {level}";
        }

        private void RefreshHP()
        {
            var stats = PlayerStats.Instance;
            float currentHP = stats != null ? stats.CurrentHP : 0f;
            float maxHP     = stats != null ? stats.MaxHP : 0f;
            if (hpSlider != null) hpSlider.value = maxHP > 0f ? currentHP / maxHP : 0f;
            if (hpText != null)   hpText.text    = $"{Mathf.CeilToInt(currentHP)}/{Mathf.CeilToInt(maxHP)}";
        }

        private void RefreshMP()
        {
            var stats = PlayerStats.Instance;
            float currentMP = stats != null ? stats.CurrentMP : 0f;
            float maxMP     = stats != null ? stats.FinalStats.MaxMP : 0f;
            if (mpSlider != null) mpSlider.value = maxMP > 0f ? currentMP / maxMP : 0f;
            if (mpText != null)   mpText.text    = $"{Mathf.CeilToInt(currentMP)}/{Mathf.CeilToInt(maxMP)}";
        }

        private void RefreshXP()
        {
            if (playerProgression == null) return;
            float current = playerProgression.CurrentExp;
            float cap     = playerProgression.ExpForNextLevel(playerProgression.Level);
            if (xpSlider != null) xpSlider.value = cap > 0f ? Mathf.Clamp01(current / cap) : 0f;
            if (xpText != null)   xpText.text    = $"{Mathf.FloorToInt(current)}/{cap}";
        }

        private void RefreshCurrency()
        {
            var cs = CurrencySystem.Instance;
            if (cs == null || goldText == null) return;
            goldText.text = $"{cs.GetAmount(CurrencyType.Gold)}";
        }

        private void RefreshAutoCombat()
        {
            bool active = combatController != null && combatController.IsAutoCombatActive;
            if (autoCombatButtonText != null)
                autoCombatButtonText.text = active ? "Auto: ON" : "Auto: OFF";
            if (_autoCombatBg != null)
                _autoCombatBg.sprite = active ? buttonOnSprite : buttonOffSprite;
        }

        private void RefreshButtonStates()
        {
            if (_invButtonBg != null)    _invButtonBg.sprite    = IsWindowOpen(WindowType.Inventory) ? buttonOnSprite : buttonOffSprite;
            if (_craftButtonBg != null)  _craftButtonBg.sprite  = IsWindowOpen(WindowType.Crafting)  ? buttonOnSprite : buttonOffSprite;
            if (_vaultButtonBg != null)  _vaultButtonBg.sprite  = IsWindowOpen(WindowType.Vault)     ? buttonOnSprite : buttonOffSprite;
            if (_talentButtonBg != null) _talentButtonBg.sprite = IsWindowOpen(WindowType.Talent)    ? buttonOnSprite : buttonOffSprite;
            if (_mapButtonBg != null)    _mapButtonBg.sprite    = IsWindowOpen(WindowType.Map)       ? buttonOnSprite : buttonOffSprite;
        }
    }
}
