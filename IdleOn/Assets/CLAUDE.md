# CLAUDE.md

## Project Overview

This project is a Unity take-home assessment inspired by IdleOn.

The goal is to build a small but polished vertical slice.

Focus on:

* Clean architecture
* Modular systems
* Gameplay progression
* UX polish

Do not attempt to recreate the full IdleOn game.

---

## Scope Rules

Only implement features listed in GameDesign.md.

Do not add extra systems unless explicitly requested.

Do not implement:

* Multiplayer
* Backend server
* Login system
* Cloud save
* Additional classes
* Complex pathfinding
* Additional maps
* Advanced AI

Prefer a complete polished feature over many unfinished features.

---

## Technology Rules

Use:

* Unity 6
* C#
* ScriptableObjects
* Unity UI (uGUI)
* TextMeshPro
* JSON Save

Avoid:

* Odin
* UniRx
* Zenject
* VContainer
* Third-party frameworks

Keep the project easy to clone and run.

---

## Architecture Principles

Use modular systems.

Separate:

* Data
* Logic
* UI

Avoid large God classes.

Prefer:

* ScriptableObjects for configuration
* Events for communication
* Small focused classes

Never use FindObjectOfType.

Use instead:

* Direct [SerializeField] references
* Static Instance properties for true singletons (e.g. PlayerStats.Instance)
* Dependency injection via the spawner or initializer

Every time you change a system, update ALL related systems. Never leave a half-broken dependency.

---

## Folder Structure

```
Assets/_scripts/
  Core/
  Characters/
  Combat/
  Enemies/
  Loot/
  World/
  Inventory/
  Save/
  Equipment/
  Talents/
  Vault/
  Quests/
  UI/
```

### Namespaces

| Folder     | Namespace          |
|------------|--------------------|
| Core       | IdleOn.Core        |
| Characters | IdleOn.Characters  |
| Combat     | IdleOn.Combat      |
| Enemies    | IdleOn.Enemies     |
| Loot       | IdleOn.Loot        |
| World      | IdleOn.World       |
| Inventory  | IdleOn.Inventory   |
| Save       | IdleOn.Save        |
| UI         | IdleOn.UI          |

---

## Save System

`GameBootstrap` runs at [DefaultExecutionOrder(-100)] and calls `SaveManager.CreateNewSave()` in Awake.

Every Play session starts with a fresh in-memory save. No auto-load from disk on startup yet.

`SaveManager.SaveToDisk()` and `LoadFromDisk()` are implemented but NOT called automatically.

Systems that need save data must use this pattern in Start():

```csharp
void Start()
{
    if (SaveManager.Instance.IsLoaded)
        Initialize();
    else
        SaveManager.OnSaveLoaded += Initialize;
}
```

`PlayerSaveData` owns:
- Level, Exp
- SilverCoins, GoldCoins (long)
- InventoryData (20 slots default)
- EquippedItems (List<EquipmentSlotEntry>)
- VaultData (VaultSaveData — upgrade levels by upgradeId string)
- CurrentMapId, LastLogoutTime

---

## Loot System

### Pipeline

Every loot source (enemy, chest, tree, etc.) uses the same two lines:

```csharp
LootResult result = LootEvaluator.Evaluate(definition.LootTable, luckMultiplier);
DropManager.Instance.Spawn(result, transform.position);
```

Never instantiate WorldDrop directly. Always go through DropManager.

### LootTable

`LootTable` is a ScriptableObject (Create → IdleOn → Loot Table).

`EnemyDefinition.LootTable` is a reference to a LootTable asset — NOT an inline list.

Each DropEntry rolls independently. Multiple entries can drop from one kill.

### WorldDrop

WorldDrop prefab is on the "Drop" physics layer (index 8).

WorldDrop is passive — no self-input logic. All pickup input lives in PlayerCombatController.

No auto-pickup. No despawn timer.

### Pickup

PlayerCombatController checks the Drop layer mask on every LMB hold frame.

If a WorldDrop collider is found: call `DropManager.Instance.Collect(drop)` and consume the input frame.

Drop pickup takes priority over movement and attack input.

`dropLayerMask` on PlayerCombatController must be set to the "Drop" layer (index 8, mask = 256).

### Currency drops

Currency goes through the WorldDrop pipeline like items. On collect, CurrencySystem.Add() is called. Currency collection always succeeds (no wallet-full state).

### Inventory full

On item collect failure: `WorldDrop.OnCollectionFailed()` sets a 0.5s cooldown. `GameEvents.RaiseInventoryFull()` fires. The drop stays in the world.

---

## Inventory System

`InventorySystem` is the only entry point for reading and writing inventory data.

UI must read from `InventorySystem.Instance` only — never from `SaveManager` or `InventoryData` directly.

Key methods:
- `TryAddItem(itemId, qty) → bool`
- `RemoveItem(itemId, qty) → bool`
- `GetSlots() → IReadOnlyList<InventorySlotData>`
- `GetCapacity() → int`

`CurrencySystem` is the only entry point for reading and writing currency.

Key methods:
- `Add(CurrencyType, long amount)`
- `Spend(CurrencyType, long amount) → bool`
- `GetAmount(CurrencyType) → long`

---

## Inventory UI

Tab key opens/closes the inventory panel.

`InventoryUI` subscribes to `GameEvents.OnInventoryChanged`.

`Refresh()` only runs when the panel is active.

`InventoryUI` reads from `InventorySystem.Instance` only.

`InventoryUI` needs a `[SerializeField] ItemDatabase itemDatabase` reference to resolve icons.

If `ItemDefinition.Icon` is null, a grey placeholder sprite is generated at runtime.

Stack count is hidden when quantity == 1.

---

## GameEvents

Current events:

```csharp
// Combat
OnAutoCombatChanged   Action<bool>
OnEnemyKilled         Action<string, float>   // enemyId, xp

// Player HP
OnPlayerHPChanged     Action<float, float>    // current, max

// Progression
OnPlayerExpGained     Action<float>

// Inventory
OnInventoryChanged    Action
OnInventoryFull       Action

// Currency
OnCurrencyChanged     Action<CurrencyType, long>  // type, new total

// Equipment
OnEquipmentChanged    Action

// Vault
OnVaultChanged        Action
```

---

## UI Systems

### Float Text

Use `FloatTextManager.Show(text, worldPos, FloatTextType, isCritical)` to display damage numbers.

Types: `Physical` (orange), `Magic` (blue), `Heal` (green).

`isCritical = true` increases font size.

FloatTextManager uses an object pool — do not instantiate FloatText directly.

### Inventory UI (ItemWindow)

`ItemWindow` component lives on the **Canvas** GameObject itself (not a child).

Press **Tab** to open/close (debug key in ItemWindow.Update).

Public API: `Open()`, `Close()`, `Toggle()` — call these from MainHUD buttons.

### Crafting Window

`CraftingWindow` component lives on `Canvas/CraftingWindow` child.

Press **C** to open/close (temporary debug key — `enableDebugKey` bool on component).

Public API: `Open()`, `Close()`, `Toggle()`.

Two-panel layout: right panel = recipe list, left panel = recipe detail with craft button.

Subscribes to `OnInventoryChanged` to refresh affordability dots.

Add result item first before consuming ingredients. If inventory full, `RaiseInventoryFull()` fires and nothing is consumed.

### Vault Window

`VaultWindow` component lives on `Canvas/VaultWindow` child.

Press **V** to open/close (temporary debug key — `enableDebugKey` bool on component).

Public API: `Open()`, `Close()`, `Toggle()`.

Subscribes to `OnVaultChanged` and `OnCurrencyChanged` (Silver only).

### Main HUD

`MainHUD` component lives on `Canvas/MainHUD` child.

Persistent HUD — always visible. No open/close.

**Character panel** (bottom-left): Name/Level, HP bar, MP bar, XP bar, Silver, Gold.
**Button bar** (bottom strip): Auto Combat toggle, Inv, Craft, Vault, Talent, Quest, Map, Settings.

Reads from:
- `GameEvents.OnPlayerHPChanged` → HP bar
- `GameEvents.OnCurrencyChanged` → Silver/Gold text
- `GameEvents.OnPlayerExpGained` → XP bar (reads `PlayerProgression.TotalExp`)
- `GameEvents.OnAutoCombatChanged` → Auto button label
- `GameEvents.OnEquipmentChanged` → refresh MP bar (MaxMP only — no current-MP tracking yet)

Window buttons call `Toggle()` on their respective window components.

Placeholder buttons (Talent, Quest, Map, Settings) call `Debug.Log(...)` only.

Requires `[SerializeField]` references to: `PlayerCombatController`, `ItemWindow`, `CraftingWindow`, `VaultWindow`, `PlayerProgression`.

**MP bar is a known placeholder** — shows MaxMP/MaxMP (always full) until a current-MP system is built.

**XP bar** uses a serialized `xpPerLevel` float (default 100) as cap — not tied to a real level-up formula yet.

Player name is hardcoded as "Hero" — no Name field in SaveData yet.

---

## Combat Rules

### Map Visual

The combat map is tile/grid-based, similar to IdleOn.

Monsters and the player stand on top of floor/platform tiles.

### Player Input

1. Click floor tile → player moves to that position.
2. Click monster → player moves to attack range next to that monster, then performs one attack.
3. Auto combat ON → player repeatedly finds a monster, moves to attack range, attacks, and continues.
4. Auto combat ON + click monster → interrupt current action, move to clicked monster, attack once, then resume auto combat.
5. Auto combat ON + click floor → interrupt current action, move to that floor position, then resume auto combat.

LMB hold over a WorldDrop (Drop layer) → collect drop. This check runs before movement/attack.

### Movement Implementation

Use simple direct 2D movement toward a target position on the floor/platform.

Do not implement A* pathfinding.

Direct Vector2.MoveTowards is sufficient for the first implementation.

---

## Inventory System

Inventory is slot-based. Each slot holds one item type. Same item stacks into the same slot. Stack size is unlimited.

Capacity = total slot count. Capacity can be increased at runtime (e.g. by an Inventory Expansion consumable).

Inventory data must be serializable for save/load.

Do not store currency in inventory slots. Currency is separate data.

## Item Types

Items are divided into three categories:

* Equipment
* Consumable
* Material

Equipment slots: Hat, Weapon, Armor, Accessory, Pants, Shoes, Ring1, Ring2.

Consumables must expose an `Apply(PlayerStats)` method (or equivalent interface) even if the implementation is empty, so future effects can be added without architecture changes.

Materials are inert data — used for quests and future crafting only.

## Currency

Currency is NOT stored as inventory items.

Two currencies:

* Silver Coins
* Gold Coins

Store currency as plain numeric fields in player save data.

Enemy drops support both item drops and currency drops via the same drop table.

## Drop System

Each source (enemy, chest, tree, etc.) holds a `LootTable` reference — not an inline DropEntry list.

Each `DropEntry` defines:

* Drop type: Item or Currency
* `ItemDefinition` reference (if Item)
* Currency type: Silver or Gold (if Currency)
* Drop chance (0–1)
* Min and Max quantity

Evaluate each entry independently on kill. Multiple entries can drop in one kill.

## Data Rules

Use ScriptableObjects for:

* ItemDefinition (Equipment, Consumable, Material)
* LootTable (shared, reusable drop tables)
* EnemyDefinition (stats + LootTable reference — NOT inline DropEntry list)
* Talents
* Vault Upgrades
* Quests
* MapDefinition
* MapRegistry
* ItemDatabase (master item registry)
* CurrencyDatabase (master currency registry)

Do not hardcode game data into UI scripts.

---

## Code Style

Use clear names.

Examples:

* PlayerCombatController
* EnemySpawner
* InventorySystem
* CurrencySystem
* DropManager
* LootEvaluator
* TalentSystem
* VaultSystem
* QuestSystem
* SaveManager

Keep methods small.

Keep systems independent.

---

## Git Commit Style

Examples:

* Initial Unity project setup
* Create combat loop
* Implement inventory system
* Add equipment system
* Implement talent upgrades
* Add vault progression
* Implement offline rewards
* Polish UI

Avoid:

* fix
* update
* test
* aaa

---

## Crafting System

Data: `CraftRecipeDefinition` (ScriptableObject) + `CraftIngredient` (serializable struct) + `CraftingDatabase` (ScriptableObject list).

Logic: `CraftingSystem` MonoBehaviour singleton. `CanCraft(recipe)` checks quantities. `Craft(recipe)` adds result first — if inventory full, abort before consuming ingredients.

`CraftingDatabase` is assigned to `GameDatabase.Crafting`.

Do NOT add: crafting levels, crafting time, auto-craft, or unlock conditions.

---

## Vault System

Data: `VaultUpgradeDefinition` (ScriptableObject) + `VaultSaveData` (serializable) + `VaultDatabase` (ScriptableObject list).

Logic: `VaultSystem` MonoBehaviour singleton. Upgrades cost Silver. `Upgrade(def)` spends Silver, increments level, calls `PlayerStats.Recalculate()` for BiggerDamage.

`VaultDatabase` is assigned to `GameDatabase.Vault`.

Three upgrade types: `BiggerDamage`, `MonsterTax`, `NaturalTalent`.

- `BiggerDamage` bonus is applied inside `PlayerStats.Recalculate()` after equipment pass.
- `MonsterTax` multiplier is applied inside `DropManager.Collect()` for currency drops only.
- `NaturalTalent` only stores level and exposes `GetTalentPointBonus()` — no TalentSystem wired yet.

Cost formula: `floor(BaseCost × CostGrowthRate ^ currentLevel)`.

Do NOT add: new upgrade types, prestige resets, or cross-character sharing without explicit request.

---

## Protected Systems — Do Not Modify Without Explicit Instruction

These systems are complete and stable. Do not refactor, rename, or add to them unless directly asked:

- `InventorySystem`, `CurrencySystem` — inventory and wallet logic
- `EquipmentSystem` — equip/unequip logic
- `DropManager` — loot collection pipeline
- `SaveManager`, `GameBootstrap` — save lifecycle
- `CraftingSystem` — crafting logic
- `VaultSystem` — vault upgrade logic
- `PlayerStats.Recalculate()` — stat pipeline (only extend at the end with new bonuses)
- `ItemWindow`, `CraftingWindow`, `VaultWindow` — window UI logic (buttons and events are wired)

---

## Known Limitations / TODOs

- **MP bar** in MainHUD shows MaxMP/MaxMP (always full). No current-MP system exists yet. When MP spending is implemented, add `OnPlayerMPChanged(float current, float max)` to GameEvents and wire MainHUD.
- **XP bar** uses hardcoded `xpPerLevel` float on MainHUD. PlayerProgression has no level-up formula yet. When leveling is implemented, PlayerProgression should expose `GetLevel()` and `GetExpForNextLevel()`.
- **Level** in HUD reads `SaveManager.CurrentSave.Level` (stays 1). Level-up logic needs to be added to PlayerProgression.
- **Player name** is hardcoded "Hero". Add a `PlayerName` field to PlayerSaveData when character creation is built.
- **Debug keys** C (Crafting), V (Vault), Tab (Inventory) are still active. They are guarded by `enableDebugKey` bools on each window. Remove or disable them once MainHUD buttons are the only entry point.
- **Talent / Quest / Map / Settings** buttons log placeholder messages. These systems are not implemented.
- **NaturalTalent** vault upgrade stores a level and exposes `GetTalentPointBonus()` but is not wired to any TalentSystem.
- **Save/Load** is not triggered automatically. `SaveToDisk()` and `LoadFromDisk()` exist but are never called. Every Play session starts fresh.
- **Multiple windows** can be open simultaneously. No WindowManager exists. Add one only if needed.

---

## Working Instructions

Before implementing any feature:

1. Read GameDesign.md.
2. Follow existing architecture.
3. Only modify files related to the requested feature.
4. Do not create unrelated systems.
5. Keep implementations simple and maintainable.

When unsure, choose the simpler solution.
