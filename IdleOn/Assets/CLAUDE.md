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
* Crafting
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
```

---

## UI Systems

### Float Text

Use `FloatTextManager.Show(text, worldPos, FloatTextType, isCritical)` to display damage numbers.

Types: `Physical` (orange), `Magic` (blue), `Heal` (green).

`isCritical = true` increases font size.

FloatTextManager uses an object pool — do not instantiate FloatText directly.

### Inventory UI

Press Tab to open/close. 20 slots displayed in a 4×5 grid. Reads from InventorySystem.

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

## Working Instructions

Before implementing any feature:

1. Read GameDesign.md.
2. Follow existing architecture.
3. Only modify files related to the requested feature.
4. Do not create unrelated systems.
5. Keep implementations simple and maintainable.

When unsure, choose the simpler solution.
