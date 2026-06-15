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

        void Awake()
        {
            GameEvents.OnPlayerHPChanged    += OnHPChanged;
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

        public void RefreshAll()
        {
            RefreshNameLevel();
            RefreshMP();
            RefreshXP();
            RefreshCurrency();
            RefreshAutoCombat();
        }

        // ── Button callbacks (wired in Inspector via Button.onClick) ─────────

        public void OnInventoryButtonClicked()   => itemWindow?.Toggle();
        public void OnCraftButtonClicked()        => craftingWindow?.Toggle();
        public void OnVaultButtonClicked()        => vaultWindow?.Toggle();

        public void OnAutoCombatButtonClicked()
        {
            if (combatController != null)
                combatController.SetAutoCombat(!combatController.IsAutoCombatActive);
        }

        public void OnTalentButtonClicked()   => talentWindow?.Toggle();
        public void OnQuestButtonClicked()    => Debug.Log("[MainHUD] Quest not implemented yet.");
        public void OnMapButtonClicked()      => mapWindow?.Toggle();
        public void OnSettingsButtonClicked() => Debug.Log("[MainHUD] Settings not implemented yet.");

        // ── Event handlers ───────────────────────────────────────────────────

        private void OnHPChanged(float current, float max)
        {
            hpSlider.value = max > 0f ? current / max : 0f;
            hpText.text    = $"{Mathf.CeilToInt(current)}/{Mathf.CeilToInt(max)}";
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
            nameLevelText.text = $"Hero  Lv.{level}";
        }

        private void RefreshMP()
        {
            float maxMP = PlayerStats.Instance != null ? PlayerStats.Instance.FinalStats.MaxMP : 0f;
            mpSlider.value = 1f;
            mpText.text    = $"{Mathf.CeilToInt(maxMP)}/{Mathf.CeilToInt(maxMP)}";
        }

        private void RefreshXP()
        {
            if (playerProgression == null) return;
            float current = playerProgression.CurrentExp;
            float cap     = playerProgression.ExpForNextLevel(playerProgression.Level);
            xpSlider.value = cap > 0f ? Mathf.Clamp01(current / cap) : 0f;
            xpText.text    = $"{Mathf.FloorToInt(current)}/{cap} XP";
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
    }
}
