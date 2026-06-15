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
OnPlayerExpGained     Action<float>           // xp delta this kill
OnPlayerLevelChanged  Action<int>             // new level (fires before OnPlayerExpGained on the same kill)

// Inventory
OnInventoryChanged    Action
OnInventoryFull       Action

// Currency
OnCurrencyChanged     Action<CurrencyType, long>  // type, new total

// Equipment
OnEquipmentChanged    Action

// Vault
OnVaultChanged        Action

// Map
OnMapChanged              Action<string>       // newMapId — fires on travel and on Initialize
OnMapObjectiveProgress    Action<int, int>     // current kills, required kills
OnMapObjectiveCompleted   Action<string>       // completedMapId
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

### Map Window

`MapWindow` component lives on `Canvas/MapWindow` child. Inner `WindowPanel` is 490×260px, centered.

Press **M** to open/close (debug key). Also opened via MainHUD Map button.

Public API: `Open()`, `Close()`, `Toggle()`.

Populated once in `Start()` via `PopulateRows()` — instantiates one `MapRowUI` prefab per MapDefinition.

`RefreshRows()` is called on `Open()`, `OnMapChanged`, and `OnMapObjectiveCompleted` (only if window is active). Rows stay stale while the window is closed — this is intentional.

`MapRowUI` prefab is at `Assets/_assets/Prefabs/UI/MapRowUI.prefab`.

Each row shows: map name (bold, fixed 120px), objective text (flex), Travel button (76px, hidden when locked, disabled when current map).

Row background tint: green = current map, blue = complete, dark = default, hidden = locked.

### ObjectiveHelper

`ObjectiveHelperUI` component lives on `Canvas/ObjectiveHelper`. **Always visible — no open/close.**

Anchored top-center: 640×58px, 6px from top edge.

Two TMP text fields:
- `mainText` (bold, 15px): `"{MapName}  ·  {ObjectiveLabel}:  {kills} / {required}"`
- `rewardText` (gold, 11.5px): `"Reward: {silver} Silver + {nextMap} Unlock"`

On complete (no next map): shows `"✓ {MapName} — All areas cleared!"` / `"Demo Complete — Thanks for playing!"`

Subscribes to: `OnMapChanged`, `OnMapObjectiveProgress`, `OnMapObjectiveCompleted`.

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

**Map button** calls `mapWindow?.Toggle()`.

Placeholder buttons (Talent, Quest, Settings) still call `Debug.Log(...)` only.

Requires `[SerializeField]` references to: `PlayerCombatController`, `ItemWindow`, `CraftingWindow`, `VaultWindow`, `MapWindow`, `PlayerProgression`.

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

## Progression System

### PlayerProgression

`PlayerProgression` MonoBehaviour lives on the Player GameObject.

Public API (read-only properties):
- `Level` (int) — current player level, starts at 1
- `CurrentExp` (float) — XP accumulated within the current level
- `TalentPoints` (int) — unspent talent points (spent by TalentSystem when built)
- `ExpForNextLevel(int level) → int` — XP required to advance from `level` to `level + 1`

XP curve: `floor(100 × 1.2 ^ (level − 1))`
- Level 1→2: 100 XP | Level 2→3: 120 | Level 3→4: 144 | Level 10→11: 516
- Base (100) and growth rate (1.2) are `[SerializeField]` fields on the component — tweak in Inspector.

### Level-Up Data Flow

```
OnEnemyKilled fires
  → PlayerProgression.HandleEnemyKilled
      CurrentExp += xp
      while CurrentExp >= ExpForNextLevel(Level):
          CurrentExp -= ExpForNextLevel(Level)   // carry over remainder
          Level++
          TalentPoints += 1 + VaultSystem.GetTalentPointBonus()
          sync → SaveManager.CurrentSave (.Level, .TalentPoints)
          RaisePlayerLevelChanged(Level)         // HUD updates level text + resets XP cap
      sync → SaveManager.CurrentSave.Exp = CurrentExp
      RaisePlayerExpGained(xp)                  // HUD fills XP bar
```

`OnPlayerLevelChanged` always fires **before** `OnPlayerExpGained` within the same kill. MainHUD receives `OnPlayerLevelChanged` first (resets bar cap to new level's threshold), then `OnPlayerExpGained` (fills bar with carry-over remainder). This ordering prevents the bar from showing > 100% for one frame.

### Save Fields Used

| Field | Type | Meaning |
|---|---|---|
| `PlayerSaveData.Level` | int | Current level |
| `PlayerSaveData.Exp` | float | CurrentExp **within** the current level (not lifetime total) |
| `PlayerSaveData.TalentPoints` | int | Accumulated unspent talent points |

`PlayerProgression.Initialize()` reads all three from `SaveManager.CurrentSave` on Start.

### NaturalTalent Vault Interaction

On each level-up: `TalentPoints += 1 + VaultSystem.Instance.GetTalentPointBonus()`

`GetTalentPointBonus()` returns `NaturalTalent.level × BonusPerLevel` (BonusPerLevel defaults to 1 on the VaultUpgradeDefinition). At NaturalTalent level 0: +1/level. At level 3: +4/level.

Do NOT wire `TalentPoints` to anything else until TalentSystem is built.

### Rules

- `PlayerProgression` is NOT a singleton. MainHUD holds a `[SerializeField]` reference to it.
- Never read `TotalExp` from PlayerProgression — that property no longer exists. Use `CurrentExp`.
- `PlayerSaveData.Exp` stores within-level XP only, not lifetime XP.
- Never call `ExpForNextLevel` with level 0 — the formula is undefined for level < 1.
- The level-up `while` loop handles multi-level gains in a single kill correctly.

---

## Map System

### Files

| File | Location | Role |
|---|---|---|
| `MapDefinition.cs` | `_scripts/World/` | SO — MapId, DisplayName, ObjectiveEnemyId, KillObjective, ObjectiveLabel, SilverReward, UnlocksMapId |
| `MapDatabase.cs` | `_scripts/World/` | SO list — exposes `Maps` (IReadOnlyList) and `GetMap(mapId)` |
| `MapProgressData.cs` | `_scripts/World/` | `[Serializable]` — MapId, KillCount, IsComplete, IsUnlocked |
| `MapSystem.cs` | `_scripts/World/` | Singleton MonoBehaviour — kill tracking, travel, reward grant |
| `MapWindow.cs` | `_scripts/UI/` | Popup window — row population and refresh |
| `MapRowUI.cs` | `_scripts/UI/` | Single row component — wired to MapSystem.Instance at refresh time |
| `ObjectiveHelperUI.cs` | `_scripts/UI/` | Always-visible top-strip — live objective progress |

### Assets

| Asset | Path |
|---|---|
| `MapDatabase.asset` | `_assets/ScriptableObjects/Maps/` |
| `MapDef_Grassland1.asset` | `_assets/ScriptableObjects/Maps/` |
| `MapDef_Grassland2.asset` | `_assets/ScriptableObjects/Maps/` |
| `MapDef_Grassland3.asset` | `_assets/ScriptableObjects/Maps/` |
| `MapRowUI.prefab` | `_assets/Prefabs/UI/` |

`MapDatabase` is assigned to `GameDatabase.mapDatabase` (field added this session).

### PlayerSaveData additions

```csharp
public string              CurrentMapId = "grassland_1";   // was "town" — changed
public List<MapProgressData> MapProgress = new List<MapProgressData>();
```

### MapSystem Init Pattern

Same as PlayerProgression — checks `SaveManager.Instance.IsLoaded` in `Start()`, calls `Initialize()` directly if already loaded. `Initialize()` reads `save.MapProgress`, creates missing entries, forces `grassland_1.IsUnlocked = true`, fires `RaiseMapChanged`.

### Kill Counting

`MapSystem` subscribes to `OnEnemyKilled`. Counts kill if:
1. Current map's progress is not `IsComplete`.
2. `MapDefinition.ObjectiveEnemyId` is empty (any enemy) OR matches the fired `enemyId`.

On objective complete: sets `IsComplete=true`, unlocks next map (if `UnlocksMapId` set), grants silver via `CurrencySystem.Instance.Add(Silver, reward)`, fires `OnMapObjectiveCompleted`.

### Travel

`MapSystem.TravelTo(mapId)` — validates unlocked + not current, updates `save.CurrentMapId`, fires `OnMapChanged`.

### Rules

- `MapSystem` IS a singleton. Access via `MapSystem.Instance`.
- `MapRowUI.Refresh()` null-checks `MapSystem.Instance` — safe to call before initialization.
- `ObjectiveEnemyId = "slime"` for all three Grassland maps (single enemy type for now).
- When adding a new map: add a `MapDefinition` asset, add it to `MapDatabase`, set `UnlocksMapId` on the previous last map.
- Do NOT add a WorldMap scene. Travel is data-only — same scene, same spawner.

---

## Protected Systems — Do Not Modify Without Explicit Instruction

These systems are complete and stable. Do not refactor, rename, or add to them unless directly asked:

- `InventorySystem`, `CurrencySystem` — inventory and wallet logic
- `EquipmentSystem` — equip/unequip logic
- `DropManager` — loot collection pipeline
- `SaveManager`, `GameBootstrap` — save lifecycle
- `CraftingSystem` — crafting logic
- `VaultSystem` — vault upgrade logic
- `PlayerProgression` — level-up logic and talent point grant
- `MapSystem` — kill tracking, travel, objective completion
- `PlayerStats.Recalculate()` — stat pipeline (only extend at the end with new bonuses)
- `ItemWindow`, `CraftingWindow`, `VaultWindow`, `MapWindow`, `ObjectiveHelperUI` — window UI logic (buttons and events are wired)

---

## Known Limitations / TODOs

- **MP bar** in MainHUD shows MaxMP/MaxMP (always full). No current-MP system exists yet. When MP spending is implemented, add `OnPlayerMPChanged(float current, float max)` to GameEvents and wire MainHUD.
- **Player name** is hardcoded "Hero". Add a `PlayerName` field to PlayerSaveData when character creation is built.
- **Debug keys** C (Crafting), V (Vault), Tab (Inventory) are still active. They are guarded by `enableDebugKey` bools on each window. Remove or disable them once MainHUD buttons are the only entry point.
- **Talent / Quest / Settings** buttons log placeholder messages. These systems are not implemented. Map button is now wired to MapWindow.
- **TalentPoints** accumulate in `PlayerSaveData.TalentPoints` on every level-up (1 + NaturalTalent bonus). No TalentWindow or TalentSystem exists yet to spend them. Do not wire TalentPoints to anything until TalentSystem is built.
- **NaturalTalent** vault upgrade is now wired: each level-up grants `1 + GetTalentPointBonus()` talent points. The points accumulate but cannot be spent until TalentSystem is implemented.
- **Save/Load** is not triggered automatically. `SaveToDisk()` and `LoadFromDisk()` exist but are never called. Every Play session starts fresh — level and talent points reset on restart.
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
