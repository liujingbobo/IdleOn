using System;
using IdleOn.Items;

namespace IdleOn.Core
{
    public static class GameEvents
    {
        // Combat
        public static event Action<bool>          OnAutoCombatChanged;
        public static event Action<string, float> OnEnemyKilled;       // enemyId, xpReward

        // Player HP
        public static event Action<float, float>  OnPlayerHPChanged;   // current, max
        public static event Action<float, float>  OnPlayerMPChanged;   // current, max

        // Progression
        public static event Action<float>         OnPlayerExpGained;    // amount gained this kill
        public static event Action<int>           OnPlayerLevelChanged; // new level

        // Inventory
        public static event Action                OnInventoryChanged;
        public static event Action                OnInventoryFull;

        // Currency
        public static event Action<CurrencyType, long> OnCurrencyChanged; // type, new total

        // Equipment
        public static event Action OnEquipmentChanged;

        // Vault
        public static event Action OnVaultChanged;

        // Talents
        public static event Action OnTalentChanged;

        // Skill Hotbar
        public static event Action OnHotbarChanged;

        // Map
        public static event Action<string>   OnMapChanged;               // newMapId
        public static event Action<int, int> OnMapObjectiveProgress;     // current, required
        public static event Action<string>   OnMapObjectiveCompleted;    // completedMapId

        // Dialogue
        public static event Action<string>   OnDialogueEnded;            // dialogueId — fires once when a dialogue fully ends

        // Quest hooks (raisers added incrementally; OnGroundMoveCompleted wired in Phase A,
        // the item/craft/equip/talent raisers wired later with Q6–Q9).
        public static event Action               OnGroundMoveCompleted;
        public static event Action<string, int>  OnItemCollected;        // itemId, qty
        public static event Action<string>       OnItemCrafted;          // itemId
        public static event Action<string>       OnItemEquipped;         // itemId
        public static event Action<string>       OnTalentUpgraded;       // talentId

        // Quest / feature state
        public static event Action<string>       OnQuestChanged;         // new active questId
        public static event Action<string>       OnQuestCompleted;       // completed questId
        public static event Action               OnFeaturesChanged;
        public static event Action               OnPersistentProgressLoaded;

        public static void RaiseAutoCombatChanged(bool active)                          => OnAutoCombatChanged?.Invoke(active);
        public static void RaiseEnemyKilled(string enemyId, float xp)                  => OnEnemyKilled?.Invoke(enemyId, xp);
        public static void RaisePlayerHPChanged(float current, float max)              => OnPlayerHPChanged?.Invoke(current, max);
        public static void RaisePlayerMPChanged(float current, float max)              => OnPlayerMPChanged?.Invoke(current, max);
        public static void RaisePlayerExpGained(float amount)                          => OnPlayerExpGained?.Invoke(amount);
        public static void RaisePlayerLevelChanged(int newLevel)                       => OnPlayerLevelChanged?.Invoke(newLevel);
        public static void RaiseInventoryChanged()                                     => OnInventoryChanged?.Invoke();
        public static void RaiseInventoryFull()                                        => OnInventoryFull?.Invoke();
        public static void RaiseCurrencyChanged(CurrencyType type, long newTotal)      => OnCurrencyChanged?.Invoke(type, newTotal);
        public static void RaiseEquipmentChanged()                                     => OnEquipmentChanged?.Invoke();
        public static void RaiseVaultChanged()                                         => OnVaultChanged?.Invoke();
        public static void RaiseTalentChanged()                                     => OnTalentChanged?.Invoke();
        public static void RaiseHotbarChanged()                                     => OnHotbarChanged?.Invoke();
        public static void RaiseMapChanged(string mapId)                                => OnMapChanged?.Invoke(mapId);
        public static void RaiseMapObjectiveProgress(int current, int required)         => OnMapObjectiveProgress?.Invoke(current, required);
        public static void RaiseMapObjectiveCompleted(string mapId)                     => OnMapObjectiveCompleted?.Invoke(mapId);
        public static void RaiseDialogueEnded(string dialogueId)                         => OnDialogueEnded?.Invoke(dialogueId);
        public static void RaiseGroundMoveCompleted()                                    => OnGroundMoveCompleted?.Invoke();
        public static void RaiseItemCollected(string itemId, int qty)                    => OnItemCollected?.Invoke(itemId, qty);
        public static void RaiseItemCrafted(string itemId)                               => OnItemCrafted?.Invoke(itemId);
        public static void RaiseItemEquipped(string itemId)                              => OnItemEquipped?.Invoke(itemId);
        public static void RaiseTalentUpgraded(string talentId)                          => OnTalentUpgraded?.Invoke(talentId);
        public static void RaiseQuestChanged(string questId)                             => OnQuestChanged?.Invoke(questId);
        public static void RaiseQuestCompleted(string questId)                           => OnQuestCompleted?.Invoke(questId);
        public static void RaiseFeaturesChanged()                                        => OnFeaturesChanged?.Invoke();
        public static void RaisePersistentProgressLoaded()                               => OnPersistentProgressLoaded?.Invoke();
    }
}
