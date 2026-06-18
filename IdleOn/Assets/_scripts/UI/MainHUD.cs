using UnityEngine;
using UnityEngine.UI;
using TMPro;
using IdleOn.Core;
using IdleOn.Items;
using IdleOn.Inventory;
using IdleOn.Characters;
using IdleOn.Combat;

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
        [SerializeField] private TextMeshProUGUI silverText;
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

        [Header("Progression")]
        [SerializeField] private PlayerProgression playerProgression;

        [Header("Button Bar")]
        [SerializeField] private Button invButton;
        [SerializeField] private Button craftButton;
        [SerializeField] private Button vaultButton;
        [SerializeField] private Button talentButtonRef;
        [SerializeField] private Button mapButton;

        private enum WindowType { None, Inventory, Crafting, Vault, Talent, Map }
        private WindowType _currentWindow = WindowType.None;

        void Awake()
        {
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
            RefreshButtonStates();
        }

        public void RefreshAll()
        {
            RefreshNameLevel();
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
            hpSlider.value = max > 0f ? current / max : 0f;
            hpText.text    = $"{Mathf.CeilToInt(current)}/{Mathf.CeilToInt(max)}";
        }

        private void OnMPChanged(float current, float max)
        {
            mpSlider.value = max > 0f ? current / max : 0f;
            mpText.text    = $"{Mathf.CeilToInt(current)}/{Mathf.CeilToInt(max)}";
        }

        private void OnCurrencyChanged(CurrencyType type, long newTotal)
        {
            if (type == CurrencyType.Silver)
                silverText.text = $"Silver: {newTotal}";
            else
                goldText.text   = $"Gold: {newTotal}";
        }

        private void OnExpGained(float delta)          => RefreshXP();
        private void OnLevelChanged(int newLevel)      { RefreshNameLevel(); RefreshXP(); }

        private void OnAutoCombatChanged(bool active)
        {
            if (autoCombatButtonText != null)
                autoCombatButtonText.text = active ? "Auto: ON" : "Auto: OFF";
        }

        private void OnEquipmentChanged() => RefreshMP();
        private void OnTalentChanged()     => RefreshMP();

        // ── Refresh helpers ──────────────────────────────────────────────────

        private void RefreshNameLevel()
        {
            int level = playerProgression != null ? playerProgression.Level : 1;
            nameLevelText.text = $"Lv. {level}";
        }

        private void RefreshMP()
        {
            var stats = PlayerStats.Instance;
            float currentMP = stats != null ? stats.CurrentMP : 0f;
            float maxMP     = stats != null ? stats.FinalStats.MaxMP : 0f;
            mpSlider.value = maxMP > 0f ? currentMP / maxMP : 0f;
            mpText.text    = $"{Mathf.CeilToInt(currentMP)}/{Mathf.CeilToInt(maxMP)}";
        }

        private void RefreshXP()
        {
            if (playerProgression == null) return;
            float current = playerProgression.CurrentExp;
            float cap     = playerProgression.ExpForNextLevel(playerProgression.Level);
            xpSlider.value = cap > 0f ? Mathf.Clamp01(current / cap) : 0f;
            xpText.text    = $"{Mathf.FloorToInt(current)}/{cap}";
        }

        private void RefreshCurrency()
        {
            var cs = CurrencySystem.Instance;
            if (cs == null) return;
            silverText.text = $"Silver: {cs.GetAmount(CurrencyType.Silver)}";
            goldText.text   = $"Gold: {cs.GetAmount(CurrencyType.Gold)}";
        }

        private void RefreshAutoCombat()
        {
            bool active = combatController != null && combatController.IsAutoCombatActive;
            if (autoCombatButtonText != null)
                autoCombatButtonText.text = active ? "Auto: ON" : "Auto: OFF";
        }

        private void RefreshButtonStates()
        {
            if (invButton != null)        invButton.interactable        = !IsWindowOpen(WindowType.Inventory);
            if (craftButton != null)      craftButton.interactable      = !IsWindowOpen(WindowType.Crafting);
            if (vaultButton != null)      vaultButton.interactable      = !IsWindowOpen(WindowType.Vault);
            if (talentButtonRef != null)  talentButtonRef.interactable  = !IsWindowOpen(WindowType.Talent);
            if (mapButton != null)        mapButton.interactable        = !IsWindowOpen(WindowType.Map);
        }
    }
}
