# CLAUDE.md

## Frozen Demo State (2026-06-20)

The playable vertical slice is feature-frozen. Q1–Q12, the right-top Quest Window, tutorial persistence, four-map single-gameplay-scene travel, portal gating, and HUD feature unlocks are implemented. Until explicitly approved, future work is limited to polish, bug fixes, QA, and documentation. Do not implement Map4, Map5, or the boss.

Normal flow is **two Unity scenes**: `BootScene` (entry/menu, build index 0) → `TestCombat` (the only gameplay scene, build index 1). Within `TestCombat`, all four maps are roots in that one gameplay scene, switched by `MapContentController` — not separate Unity scenes.

Current tutorial rules:

* Q5/Q7/Q10/Q12 are Chief dialogue quests.
* Q6/Q8/Q9/Q11 are action quests. During those quests the Chief plays hint-only dialogue, which must never satisfy a quest objective.
* Q12 ends the chain. Afterwards the Chief selects random daily dialogue and cannot advance a quest.
* The always-visible Quest Window shows the active quest and live objective counts, restores after load, and shows `Tutorial Complete` / `Keep exploring.` after Q12.
* `Canvas/ObjectiveHelper` is disabled. The old top-map-objective flow is historical only.

Persistence now covers the active quest, completed quests, active objective counts, unlocked feature flags, per-character global enemy kill counts, and current map ID. It uses `TutorialProgressSaveData`, `QuestSaveData`, and `EnemyKillSaveData`; missing old-save fields are normalized, import does not replay completion rewards, and `EnemyKillTracker` is backed by saved counts. Existing `CurrentMapId` / `MapProgress` remains for travel compatibility.

`TestCombat` contains `Map_grassland_1`, `Map_grassland_2`, `Map_town`, and `Map_grassland_3`. `MapContentController` switches roots and moves the player to the matching spawn. `PortalInteractable` stores only `destinationMapId`; `PortalGate` reads requirements from the destination `MapDefinition`. Requirements are: grassland_1 none, grassland_2 q1, town q3, grassland_3 q5. Portal alpha is 1 when active and 0.5 when locked.

Inventory is always available. Q7 unlocks Craft, Q10 unlocks Talents and AutoCombat, and Q12 unlocks Vault and Map. AutoCombat starts off and stays hidden before Q10. Slime Essence, Slime Sword, and silver/currency visuals are assigned. Q11 directs the player to kill monsters, level up, earn a Talent Point, and then upgrade Fireball Training.

The remaining release gate is one uninterrupted human Q1–Q12 playthrough with real save/quit/reload checkpoints. Use `.claude/HANDOFF_CURRENT_STATE.md` for the authoritative route and current debt. Any contradictory implementation note below is historical and superseded by this section.

### Hurt/Death animation (player + slime)

Both Player and Slime have real Hurt and Death sprite animations — not freeze-based placeholders.

- **Player:** `HealthComponent.OnDamaged` (new event, fires on non-lethal `TakeDamage` only, sibling of the existing lethal-only `OnDied`) is consumed by `PlayerCombatController.HandleDamaged()`, which calls `PlayerAnimatorDriver.PlayHurt()` (`Animator.SetTrigger("Hurt")`). `PlayerAnimatorDriver` also reads `PlayerCombatController.IsDead` every frame to drive the Animator's `IsDead` bool. `PlayerAnimator.controller` has `Hurt`(trigger)/`IsDead`(bool) params, `Hurt`/`Death` states wired from `AnyState` (Death always takes priority; `AnyState→Hurt` is gated `IsDead==false` so Death overrides Hurt and Hurt can never fire after death). Clips: `Player_Hurt.anim` (6 frames, ADVENTURER `06-Hurt`), `Player_Death.anim` (9 frames, ADVENTURER `07-Dead`), both non-looping, 12fps. Existing `Idle`/`Run`/`Attack` states/transitions were not touched — the new wiring lives only in the controller's `AnyState` transition list.
- **Slime:** identical pattern. `EnemyController.HandleDamaged()` (subscribed to `HealthComponent.OnDamaged`) calls `EnemyAnimatorDriver.PlayHurt()`. `SlimeAnimator.controller` gained `Hurt`(trigger) + a `Hurt` state, `AnyState→Hurt` gated `IsDead==false`; the existing `AnyState→Dead` transition/`Slime_Dead.anim`/death flow are unchanged. Clip: `Slime_Hurt.anim` (6 frames, `SLIME04/04-Hurt`), non-looping, 12fps.
- **Death respawn (player) stays a separate, unconditional path:** `PlayerCombatController.DeathSequence()` waits `deathAnimationFallbackDuration` (1.2s — covers the 0.75s Death clip), then calls `MapSystem.Instance.RespawnAtDefault(deathRespawnMapId)` (`"town"`) and `HealthComponent.Revive()`. This does **not** read or clear `MapSystem.PreviousMapId` — death respawn is unrelated to the portal-return-spawn feature; `PreviousMapId` is left exactly as travel last set it.
- **Slime death stays exactly as before:** `EnemyController.HandleDied()` (lethal hit only) freezes physics, fires kill XP/loot/`OnKilled` once, then `DisableAfterDeath()` waits `deathDisableDelay` (0.5s) so `Slime_Dead.anim` plays before `SetActive(false)`. Loot only ever spawns from `HandleDied`, so Hurt animation work cannot duplicate drops. `LocalEnemyRespawner` (Grassland3 only) and the no-respawner Grassland2 tutorial slime are both unaffected.

### Current movement and loot presentation polish

* Player Run and Slime Move animation state follows each controller's read-only horizontal movement intent (`IsMoving`), rather than relying on an `Update`-frame position delta from `FixedUpdate` movement.
* `SpriteFeetAligner` is attached to the Player root and `Slime.prefab`. In `LateUpdate` it adjusts only the `Sprite` visual child's local Y so the current rendered frame's bottom remains aligned to the feet-root. Root transforms, Rigidbody2D, and colliders remain unchanged.
* World drops spawn directly on `GroundLane.GroundY`. Multiple loot entries preserve result order and use a centered, symmetric layout with `dropSpacing = 0.4`; near lane edges the whole layout shifts together instead of clamping individual drops into overlaps.
* WorldDrop sprite width normalization remains presentation-only and targets `0.32` world units while preserving the visual child's original scale ratio and signs.

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

## Save System (Account-based)

`AccountSaveData` is the top-level save file (`account_save.json` at `Application.persistentDataPath`). Old `player_save.json` is ignored.

```csharp
public class AccountSaveData
{
    public int Version;
    public VaultSaveData Vault;            // account-shared, sibling of Players
    public List<PlayerSaveData> Players;   // one per character, sibling of Vault
    public string CurrentPlayerId;
}
```

`VaultSaveData` does NOT contain the player list — vault and players are siblings under the account.

`SaveManager` exposes:
- `CurrentAccount` (AccountSaveData)
- `CurrentSave` (PlayerSaveData) — the selected/current character. **Name preserved intentionally** so existing systems (PlayerProgression, TalentSystem, MapSystem, etc.) keep working unchanged.
- `CurrentVault` (VaultSaveData) → `CurrentAccount.Vault`

Account/character lifecycle: `CreateNewAccount()`, `LoadAccountFromDisk()`, `SaveAccountToDisk()`, `CreateNewCharacter(name)`, `SelectCharacter(playerId)`. Save-on-quit via `OnApplicationQuit`/`OnApplicationPause`.

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

`PlayerSaveData` owns (per-character):
- PlayerId, PlayerName
- Level, Exp, TalentPoints
- SilverCoins, GoldCoins (long)
- InventoryData (20 slots default)
- EquipmentData
- CurrentMapId, LastLogoutTime
- MapProgress (List<MapProgressData>)
- TalentData (List<TalentSaveData> — talentId + level pairs)
- HotbarSkillIds (List<string> — 3 entries, skillId per slot, empty string = unassigned)

`VaultData` was **removed** from `PlayerSaveData` — vault now lives only on `AccountSaveData.Vault` and is shared across all characters on the account. `VaultSystem` reads `SaveManager.Instance.CurrentVault`, not `CurrentSave.VaultData`. Verified: Hero 2 sees vault upgrades created on Hero 1.

### Startup UI

The original programmer-art `StartupMenu` (runtime-generated Canvas/buttons) is bypassed/disabled in the normal flow. Kept only as the `TestCombat` direct-open fallback. Do not restore its runtime-generated UI path as the primary flow.

### BootScene / StartMenu

`BootScene` is the normal entry scene (build index 0), with a real, scene-/prefab-authored Canvas — not generated by code. `TestCombat` is build index 1, the only gameplay scene.

- **MainMenuPanel**: `New Save` / `Load Save` buttons (`Load Save` disabled if `account_save.json` doesn't exist).
- **CharacterSelectPanel**: character list (`CharListContainer` + `CharacterRowUI.prefab` rows) + `NewCharacterButton`.
- `New Save` → creates/initializes a new account, opens `CharacterSelectPanel` with an **empty** list. Does not enter `TestCombat`.
- `Load Save` → loads the account file, opens `CharacterSelectPanel` with existing characters (zero characters is valid).
- `NewCharacterButton` → creates a character row, stays on `CharacterSelectPanel`.
- `CharacterRowUI.SelectButton` is the **only** normal way to select a character and load `TestCombat` — there is no separate Continue button.
- `TestCombat` opened directly in the editor (no `BootScene`) still falls back safely to an unselected-character fresh state — this direct-open fallback still exists for editor testing.

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

`DropManager.Spawn()` places drops at `GroundLane.Current.GroundY` (falling back to the supplied origin Y if no lane exists). It does not add gravity, bounce, or random vertical offsets.

For multiple entries, spawn X is stable and symmetric around the death/origin X:

```csharp
offsetX = (index - (count - 1) * 0.5f) * dropSpacing;
```

`dropSpacing` defaults to `0.4`. Near `GroundLane.MinX` / `MaxX`, the entire group center is shifted so spacing and entry order are preserved; drops are not individually clamped into overlaps.

The WorldDrop root and collider are never scaled for artwork. `WorldDrop.Setup()` normalizes only the `SpriteRenderer` visual child to a target width of 32 pixels at 100 reference pixels/unit (`0.32` world units), recalculating after every pooled sprite change and preserving the visual child's original scale ratio and signs.

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

Currency is NOT stored as inventory items — see `CurrencySystem` below.

### Fixed-slot model

`InventoryData.Slots` is a **fixed-size list**, not a dense occupied-only list. Every index in `Slots` is a real slot position, including empty ones.

- `InventorySlotData.IsEmpty` (`string.IsNullOrEmpty(ItemId) || Quantity <= 0`) and `Clear()` (`ItemId = null`, `Quantity = 0`) mark/clear a slot without removing its list entry.
- `InventoryData.EnsureSlots(capacity)` appends empty slots until `Slots.Count >= capacity`. Never removes/shrinks the list — if capacity ever decreases, extra slots are left in place and simply ignored by display/index bounds checks.
- `AddItem`: stacks into the first existing non-empty slot with a matching `ItemId`; otherwise scans for the first empty slot within `capacity` and places the item there. Existing items are never moved.
- `RemoveItem`: still **itemId-based** (finds the first matching non-empty slot), decrements, and calls `Clear()` on that slot when quantity hits 0. The slot stays in the list — later slots never shift.
- `InventorySystem.Data` getter calls `EnsureSlots(capacity)` on every access. This both pads old dense saves (existing items keep their list position starting at slot 0; empty slots are appended after them) and grows the list whenever capacity increases.
- **Known limitation:** `RemoveItem`/`Equip` operate on itemId, not a specific slot index. If the same item ever exists in more than one slot, only the first match is affected. See "Known Limitations" below for the planned `RemoveItemAt`/`EquipFromSlot`/`MoveItem` follow-up.

### Inventory Expansion talent

`TalentDefinition.InventorySlotsPerLevel` (mirrors the other PerLevel fields) — set to `20` on `TalentDef_InventoryExpansion.asset` (`TalentId = "inventory_expansion"`, `MaxLevel = 5`). `TalentSystem.GetInventorySlotBonus() → int` sums `level × InventorySlotsPerLevel` across all talents.

`InventorySystem.GetCapacity()` = `InventoryData.Capacity` (base, persisted — grows only via the Inventory Expansion **consumable**) **+** `TalentSystem.GetInventorySlotBonus()`. The talent bonus is computed live every call and is never written into `InventoryData.Capacity` — keeps persisted base capacity and derived talent bonus separate.

`InventoryData.AddItem(itemId, totalCapacity, quantity=1)` takes the effective capacity as a parameter (instead of reading its own field) so the talent bonus actually unlocks placeable/empty slots, not just page display. `InventorySystem.TryAddItem`/`Data` getter pass `GetCapacity()` (base+bonus) into `AddItem`/`EnsureSlots`.

`TalentSystem.Upgrade()` fires `GameEvents.RaiseInventoryChanged()` (in addition to `OnTalentChanged`) whenever the upgraded talent has `InventorySlotsPerLevel != 0` — `ItemWindow` is already subscribed to `OnInventoryChanged` and only acts if the panel is open, so pagination/page-buttons refresh live if `InventoryPanel` is open during the upgrade, and pick up the new capacity next time it's opened otherwise. No new event was added.

### Currency

`CurrencySystem` is the only entry point for reading and writing currency.

Key methods:
- `Add(CurrencyType, long amount)`
- `Spend(CurrencyType, long amount) → bool`
- `GetAmount(CurrencyType) → long`

### Inventory rules

Inventory is slot-based. Each slot holds one item type. Same item stacks into the same slot. Stack size is unlimited.

Capacity = total slot count. Capacity can be increased at runtime (e.g. by an Inventory Expansion consumable).

Inventory data must be serializable for save/load.

Do not store currency in inventory slots. Currency is separate data.

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

// Talents
OnTalentChanged       Action                  // fires after every Upgrade(); refreshes TalentWindow + MainHUD MP bar

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

**Pagination:** 20 fixed `InventorySlotUI` slots are reused across pages — no slots are created/destroyed. Visible slot `i` on page `p` maps to global slot index `p * 20 + i`, read directly from `InventorySystem.GetSlots()` (the fixed-slot list) — empty global indices show an empty slot UI.

- `NextPage()` / `PrevPage()` increment/decrement `_currentPage`, clamp against `totalPages = Ceil(capacity / 20)`, then refresh — page switching never moves or mutates inventory data, only changes which slice is displayed.
- Page button visibility: 1 page → both hidden; first page → Next only; middle page → Prev + Next; last page → Prev only.
- `_currentPage` resets to 0 on `Open()`/`ToggleWindow()` open branch, and is re-clamped every refresh in case capacity shrinks/grows.
- `CloseButton` wired to `ItemWindow.Close()`.

**Equipment placeholders:** each `EquipmentSlotUI` has a `[SerializeField] GameObject placeholder` child. `Refresh()` shows the placeholder and hides the icon when the slot has no equipped item with an icon; shows the icon and hides the placeholder when equipped. Unequipping restores the placeholder automatically (same `Refresh()` call, driven by `OnEquipmentChanged`). Nested equipment slot UI (`Left`/`Middle`/`Right` groups) is wired into `ItemWindow.equipmentSlots`; old inactive flat equipment slots were left in place, untouched.

### Crafting Window

`CraftingWindow` component lives on `Canvas/CraftingWindow` child. `windowPanel` field is wired to `BookUI` (the manually-redesigned book-style panel root).

Press **C** to open/close (temporary debug key — `enableDebugKey` bool on component).

Public API: `Open()`, `Close()`, `Toggle()`.

**Layout (manually redesigned, code adapted to it — do not redesign further without instruction):**
- **Left (`LeftPanel`, `CraftingDetailPanel` component):**
  - `ItemInfo` — icon (nested `IconBackground/Icon`, frame stays on the outer `IconBackground`), name, description. All cleared/hidden when no recipe is selected.
  - `Materials` — scrollable list of one reusable row per required ingredient.
  - `CraftButton` — single button, position is user-controlled in the scene.
- **Right (`ScrollView/Viewport/Content`)** — recipe list, one reusable row per recipe.

**No selection on open:** `CraftingDetailPanel.Show(null)` runs from `Awake()` (not `Start()` — the window starts inactive, so `Start()` would be deferred until first `Open()`, leaving stale placeholder text/icon visible for a frame). Empty state: icon hidden, name/description empty, materials list empty, `CraftButton.interactable = false`.

**Recipe row (`CraftingRecipeSlotUI`, prefab `Assets/_assets/Prefabs/UI/CraftingRecipeSlot.prefab`):** one prefab, reused for every recipe. `itemIcon` is wired to the nested `IconBackground/Icon`. `craftableSprite`/`notCraftableSprite` — `Refresh()` swaps `background.sprite` based on `CraftingSystem.CanCraft(recipe)`. `canCraftDot` tint logic is unchanged (both indicators coexist).

**Recipe list sort:** `CraftingWindow.PopulateRecipeList()` sorts craftable recipes first, then uncraftable (stable, preserves DB order within each group). Re-sorts and rebuilds (via `RefreshAllSlots()`) on `OnInventoryChanged` while the window is open, so crafting/consuming materials live-reorders the list. Old instantiated slots are destroyed by reference (tracked in `_slots`), not by iterating `transform` children — `Destroy()` is deferred in Play mode, so trusting live child count mid-frame double-instantiates.

**Material row (`IngredientRowUI`, prefab `Assets/_assets/Prefabs/UI/IngredientRow.prefab`):** one prefab, reused for every ingredient. Count text format is exactly `{owned}/{required}` (no spaces). `ColorEnough` = `RGBA(0.361,0.459,0.173,1)` (green), `ColorMissing` = `RGBA(0.761,0.294,0.282,1)` (red).

Subscribes to `OnInventoryChanged` to refresh row backgrounds, material counts/colors, recipe order, and `CraftButton` state together (`CraftingDetailPanel.Refresh()` + `CraftingWindow.RefreshAllSlots()`).

Add result item first before consuming ingredients. If inventory full, the failure is logged (`Debug.LogWarning`, no dedicated status UI text exists in the new layout) and nothing is consumed.

**Not verified live:** the "enough materials" / craftable path was verified by code symmetry only in one earlier session (test scene's inventory had 0 capacity in that Play context). Re-verify with a fully initialized player/inventory session (see Known Limitations).

### Map Window

`MapWindow` component lives on `Canvas/MapWindow` child, `windowPanel` wired to `BookUI` (the manually-authored book-style panel root). Press **M** to open/close (debug key). Also opened via MainHUD Map button. Public API: `Open()`, `Close()`, `Toggle()`.

Display/travel logic lives in `MapWindowUI` (`_scripts/UI/MapWindowUI.cs`), a second component on the same `MapWindow` GameObject, wired via `MapWindow.mapWindowUI`. `MapWindow.RefreshRows()` calls `mapWindowUI.Refresh()` first, then a legacy per-row refresh (kept only as a fallback path — `PopulateRows()`/`MapRowUI` are unused while `mapWindowUI` is assigned).

`MapWindowUI.points[]` is a hand-wired array (one entry per hand-placed map point under `BookUI/WindowPanel/Map/`: `Grassland1..5`, `Town`), each holding `MapId`, `Button`, `Current` (GameObject), `Locked` (GameObject — dark overlay `Image`, `raycastTarget=false`, 12×12, centered).

State per point, driven by `Refresh()`: locked → `Button.interactable=false`, `Current` hidden, `Locked` shown. Unlocked+not current → `Button.interactable=true`, `Current` shown, `Locked` hidden. Unlocked+current → `Button.interactable=false`, `Current` shown, `Locked` hidden. Unlock check mirrors `PortalGate`: destination `MapDefinition.UnlockQuestId`/`UnlockEnemyId`+`UnlockKillCount`, plus a `HasMapContent` guard (looks for a `Map_{mapId}` root in the scene) so `grassland_4`/`grassland_5` (no map content yet) stay locked regardless of quest state. Refreshes on `OnMapChanged`, `OnQuestChanged`/`OnQuestCompleted`, `OnFeaturesChanged`, `OnPersistentProgressLoaded`, `OnEnemyKilled`, and `Awake`/`Start`.

Clicking an unlocked non-current point calls `MapSystem.Instance.TravelTo(mapId)`; clicking is otherwise a no-op (locked or already-current). Actual `TravelTo` succeeding additionally requires `MapProgressData.IsUnlocked` to be synced by `PortalGate`, which only happens once `MapSystem.Initialize()` has run against a loaded save (i.e. after character selection) — existing, untouched architecture.

Old per-row path (kept for reference, not currently wired): `PopulateRows()` instantiates one `MapRowUI` prefab (`Assets/_assets/Prefabs/UI/MapRowUI.prefab`) per `MapDefinition` into `rowContainer`; each row shows map name/objective text/Travel button; tint green=current, blue=complete, dark=default, hidden=locked.

### ObjectiveHelper

`ObjectiveHelperUI` still exists on `Canvas/ObjectiveHelper`, but that GameObject is disabled for the frozen demo. The old always-visible top objective strip is historical and must not drive tutorial progression.

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

**Rebuilt and implemented (2026-06-21):** `VaultSlotUI.cs` (slot icon/level/selection), `VaultInfoPanel.cs` (new — name/description/icon/level fill/current-vs-next effect text/Upgrade button), `VaultWindow.cs` rewrite to drive the new info panel, `VaultSlotUI.prefab`. Each `VaultUpgradeDefinition` now carries an `Icon` and `Description` field; vault icon art lives under `Assets/_assets/Art/Vault/`. All committed.

### Offline Earnings Window

`Canvas/OfflineEarnings` (`OfflineEarningsWindowUI` + `OfflineEarningsSystem`, `BookUI` reused as `windowPanel`/`motion`). Closed by default via `UIWindowMotion.SetClosedImmediate()` in `Awake()` — same convention as every other window.

`OfflineEarningsSystem.RecordLogoutSnapshot()` is called from `SaveManager.SaveAccountToDisk()` and snapshots `LastLogoutUtcTicks`/`LastOfflineMapId`/`WasAutoCombatOnAtLogout` onto `PlayerSaveData`. On `SaveManager.OnSaveLoaded`, `HandleSaveLoaded()` consumes (zeroes) the snapshot immediately — preventing duplicate grants — then grants rewards only if: tutorial complete (`QuestSystem.IsCompleted("q12")`), AutoCombat was on at logout, and the logged-out map was `grassland_2` or `grassland_3`. Reward formula: `offlineSeconds = max(1, elapsed)`; grassland_2 → `Gold = offlineSeconds*50`, `slime_essence = offlineSeconds*2`; grassland_3 → `Gold = offlineSeconds*120`, `slime_essence = offlineSeconds*5`. Rewards are added via `CurrencySystem`/`InventorySystem` before the popup (`EarningSlotUI` slots in `ContentRow/BackgroundGrid`) is shown.

Do NOT add EXP, additional maps, or repeat-grant logic to this system without explicit instruction.

### Dialogue Portraits

`DialogueNode` carries a per-node portrait `Sprite` (falls back to `DialogueDefinition.Portrait` if unset). `DialogueWindowUI` refreshes the portrait each time the current node changes, rendered into the existing `DialogueWindow` body image in `TestCombat`. All existing Chief dialogue nodes have the VillageChef portrait assigned.

### UI Animation Pass

`Inventory`/`Crafting`/`Dialogue` windows gained open/close motion (`ItemWindow.cs`, `CraftingWindow.cs`, `DialogueWindowUI.cs`), following the same `UIWindowMotion`/`UIButtonMotion` alpha/scale-tween pattern already used elsewhere. Vault/Map/Talent window motion was wired during the Vault rebuild pass above.

### Main HUD

`MainHUD` component lives on `Canvas/MainHUD` child.

Persistent HUD — always visible. No open/close.

**Character panel** (bottom-left, manually laid out — see `CharacterPanel/StatusGroup`, `/Name`, `/LevelGroup`): Name text (static, MainHUD never writes it), Level text (`"Lv. {level}"`), HP/MP/EXP sliders+text (`"cur/max"` format, no suffix).
**Button bar** (bottom strip): Auto Combat toggle, Inv, Craft, Vault, Talent, Quest, Map, Settings — all real `Button` components.

Reads from:
- `GameEvents.OnPlayerHPChanged` → HP slider+text
- `GameEvents.OnPlayerMPChanged` → MP slider+text (real current-MP, not a placeholder)
- `GameEvents.OnCurrencyChanged` → Silver/Gold text
- `GameEvents.OnPlayerExpGained` / `OnPlayerLevelChanged` → EXP slider+text + Level text
- `GameEvents.OnAutoCombatChanged` → Auto button label
- `GameEvents.OnEquipmentChanged` / `OnTalentChanged` → refresh MP bar (MaxMP changes)

Window buttons call `MainHUD.OnXButtonClicked()` → internal `ToggleWindow(WindowType)`, which calls `Open()`/`Close()` on the window components directly (not `window.Toggle()`).

**Button interactable state mirrors window open state** — `RefreshButtonStates()` sets `button.interactable = !window.IsOpen` for Inv/Craft/Vault/Talent/Map, called after every open/close/switch and polled every `Update()` (so closing a window via its own CloseButton — bypassing MainHUD — still un-sticks its ButtonBar button next frame). Reads the window's real `IsOpen`, not just the cached `_currentWindow`.

Placeholder buttons (Quest, Settings) still call `Debug.Log(...)` only — inactive in scene, no icon assets yet.

Requires `[SerializeField]` references to: `PlayerCombatController`, `ItemWindow`, `CraftingWindow`, `VaultWindow`, `MapWindow`, `TalentWindow`, `PlayerProgression`, plus `invButton/craftButton/vaultButton/talentButtonRef/mapButton` (Button refs for interactable sync).

**MP bar shows real current MP** — `PlayerStats.CurrentMP` / `FinalStats.MaxMP`. No longer a placeholder.

**CharacterPanel StatsGroup hover popup (implemented):** `Canvas/MainHUD/CharacterPanel/StatsGroup` holds a `CanvasGroup` (hidden by default — alpha 0, not interactable, no raycast block) and 12 TMP texts (STR/AGI/WIS/LUK/MAXHP/MAXMP/ATKMin/ATKMax/DEF/ACC/CRITChance/MoveSpeed) under `LeftGroup`/`RightGroup`. An `EventTrigger` on `CharacterPanel` (PointerEnter/PointerExit) calls `MainHUD.OnCharacterPanelPointerEnter`/`OnCharacterPanelPointerExit`, which fade `statsGroup.alpha` 0↔1 over `statsFadeDuration` (0.12s, `Time.unscaledDeltaTime`-driven coroutine) and refresh all 12 texts once on show from `PlayerStats.Instance.FinalStats` (`"Name: value"` format).

**XP bar** uses `PlayerProgression.ExpForNextLevel(Level)` as cap — tied to the real level-up formula.

Player name is still hardcoded as "Hero" in save data — the Name text object in the scene is static/manual, MainHUD does not write to it.

---

## Movement Model — Single-lane, no-gravity, feet-root

- **Single lane, no gravity.** The current scene has exactly one ground lane. Player and enemies do **not** fall — they are driven deterministically.
- **`GroundLane` (`_scripts/World/GroundLane.cs`)** is the single source of truth for the lane. `GroundLane.Current` (static) exposes `GroundY`, `MinX`, `MaxX`, and `ClampX(x)`. One instance per scene; it registers in `OnEnable` and clears `Current` in `OnDisable` (covers both disable and destroy). Current scene values: `GroundY = -2.0`, `MinX = -9.5`, `MaxX = 10.5` (Tilemap worldX [-10, 11], 0.5 inset).
- **Rigidbody2D = Kinematic, `gravityScale = 0`** on both Player (scene instance) and `Slime.prefab`. Movement uses `Rigidbody2D.MovePosition` in `FixedUpdate` only — **no velocity writes, no `transform.position` writes for movement** (don't mix paradigms).
- **Root = feet / ground-contact point.** Player root.y = `-2.0` (feet). `Slime.prefab` root is feet. Each `FixedUpdate` forces `position.y = GroundLane.GroundY` and X-only horizontal motion; X is clamped to `[MinX, MaxX]`.
- **Pivots / frame cropping:** do **not** change imported sprite pivots. Player and Slime use `SpriteFeetAligner` on the feet-root. After the Animator swaps the frame, `LateUpdate` adjusts only the visual `Sprite` child local Y until `SpriteRenderer.bounds.min.y` matches the root Y plus optional `groundVisualOffset` (currently `0`). It does not move the root, Rigidbody2D, or collider and does not accumulate drift.
- **Animation movement state:** `PlayerCombatController.IsMoving` and `EnemyController.IsMoving` expose read-only horizontal movement intent from `_desiredVelocityX`. Animator drivers use this as the authoritative Run/Move signal, avoiding `Update` / `FixedUpdate` sampling flicker. Position delta remains presentation-only for facing/fallback.
- **Click-to-move:** ignores clicked Y entirely; resolves to `GroundLane.ClampX(clickWorldPos.x)` on the lane.
- **Enemy patrol/chase** stays on the lane: patrol bounds are clamped to `[MinX, MaxX]` in `Start`; Y is forced to `GroundY` every `FixedUpdate`.
- **Enemy hitbox** must be **at least 1 tile tall measured from the feet upward** (Slime BoxCollider2D: offset.y `0.5`, size.y `1` → feet → +1 tile). This guarantees low projectiles connect.
- **`EnemySpawner`** spawns and respawns enemies on `GroundLane.GroundY` (`LaneSpawnPos`) — no gravity to settle them.
- **Fireball / projectiles:** spawn height = `GroundLane.GroundY + projectileHeight` (`projectileHeight` field on `PlayerCombatController`, default `0.6`). Keep `projectileHeight <= 1` so it still hits 1-tile-high enemies. Direction is horizontal only (`Vector2.left/right` from facing) — projectiles stay on the lane. `FireballProjectile.prefab` is Kinematic + trigger.
- **Not implemented yet:** multi-lane, ladders, portals, jumping, BFS/pathfinding, lane graph. Single flat lane only.
- **Future interactables / multi-platform:** use a same-lane *walk-to-interact* model first (walk to target X on the current lane, then act); add cross-lane transitions (ladders/portals) as explicit steps later — never physics jumping.

---

## WorldInteractable (same-lane walk-to-interact)

Same-lane interactable system: click a world object → player walks to it on the current lane → `Interact()` fires when in range. Same-lane only — no multi-lane pathfinding, ladders, portals-as-lane-connections, BFS, or jumping yet.

- **Contract (`_scripts/World/`):**
  - `IWorldInteractable` — `float InteractionX`, `float InteractionRange`, `bool CanInteract(GameObject player)`, `void Interact(GameObject player)`.
  - `WorldInteractable` — abstract `MonoBehaviour` base. `InteractionX => transform.position.x` (lane feet-root), `interactionRange = 0.4` (small but forgiving, serialized), `CanInteract => isActiveAndEnabled`, abstract `Interact`.
- **Click priority (in `PlayerCombatController.HandleClick`):** drop pickup → UI → **enemy** → **WorldInteractable** → plain ground move. Interactable is checked **after** enemy (enemy click always wins) and **before** ground movement.
  - Clicking an interactable stores `_pendingInteractable`, sets the manual-move target to `GroundLane.Current.ClampX(InteractionX)` on the lane, enters `ManualMove`.
  - On each `UpdateManualMove` frame: if within `InteractionRange` of `InteractionX`, calls `Interact(gameObject)` once (pending cleared **before** the call → no double-fire), then resumes auto-combat/idle. Never triggers immediately on click unless already in range.
  - **Plain ground click cancels** pending interactable. **Clicking another interactable replaces** it. **Starting a manual enemy attack cancels** it.
  - **Disabled/destroyed interactable cancels safely** (Unity-null + `isActiveAndEnabled` guard in `UpdateManualMove`).
- **Colliders:** interactables use **trigger / click colliders** — they must NOT physically block player, enemies, Fireball, drops, or ground movement. (Fireball still only damages `EnemyController`; it passes through interactable triggers.)
- **`CraftingStationInteractable`** — opens crafting via **`MainHUD.OpenCraftingWindow()`**, NOT `CraftingWindow.Open()` (avoids bypassing MainHUD window switching / desyncing HUD button sprites). Holds a serialized `MainHUD` reference; warns if unset.
- **`MainHUD.OpenCraftingWindow()`** — public, **open-only**: reuses central switching (closes the current MainHUD-managed window, opens Crafting, refreshes button sprites). It is **not** a toggle — if Crafting is already open it stays open. (Do not call `OnCraftButtonClicked()` from a station; that toggles and could close Crafting.)
- **`PortalInteractable`** — calls `MapSystem.Instance.TravelTo(destinationMapId)` when valid; logs a safe warning if `destinationMapId` is empty or `MapSystem.Instance` is null. Does not rewrite the map system. (`TravelTo` itself ignores locked/current destinations.)
- **`NPCDialogueInteractable`** — drives `DialogueSystem` for NPC talk targets (e.g. the Chief). Implemented and in use; do not re-flag as a future task.

---

## Combat Rules

### Map Visual

The combat map is tile/grid-based, similar to IdleOn.

Monsters and the player stand on top of floor/platform tiles.

### Player Input — persistent target model

LMB hold over a WorldDrop (Drop layer) → collect drop. This check runs before all other input.

Single LMB click — priority order: drop pickup → UI (`EventSystem.IsPointerOverGameObject`) → enemy → `WorldInteractable` → plain ground move.

**Enemy click and AutoCombat share one persistent `_currentTarget` + `Moving`→`Attacking` loop** (`PlayerCombatController`):
- Clicking an enemy sets `_currentTarget`; player walks into range, then attacks repeatedly on the existing attack cooldown until it dies, then idles (AutoCombat off) or auto-reseeks (AutoCombat on).
- Clicking the **same** enemy while already `Moving`/`Attacking` toward it is a no-op — no movement restart, no attack-timer reset, no extra attack.
- Clicking a **different** enemy switches `_currentTarget`; the attack cooldown timer is global and carries over unchanged (no free attack on switch).
- Clicking the ground clears `_currentTarget`, moves to the clicked X, then idles (AutoCombat off) or resumes `SeekTarget()` (AutoCombat on).
- Auto combat ON with no clicks → player repeatedly seeks nearest monster (`SeekTarget()`/`EnemySpawner.GetNearestEnemy`), moves to attack range, attacks, continues after each kill.

### AutoCombat Target Discovery (registry-based, 2026-06-20)

`EnemyTargetRegistry` (`_scripts/Enemies/EnemyTargetRegistry.cs`, new static class) — `EnemyController.OnEnable`/`OnDisable` register/unregister themselves. `GetNearestValidEnemy(position)` scans only currently-registered, active, alive, non-`Dead`-state enemies and returns the nearest, sweeping stale (destroyed) entries as it goes. `PlayerCombatController.SeekTarget()` calls this instead of any scene-wide find — fixes AutoCombat not finding enemies spawned/respawned (e.g. via `LocalEnemyRespawner`) after the player entered the map. Resets on every domain reload (`RuntimeInitializeOnLoadMethod`).

### Required Inspector Setup

`PlayerCombatController` ground-detection fields (legacy, see Known Limitations — current movement uses `GroundLane`, not raycast ground detection, but the fields remain in the Inspector and must stay harmless):
- `groundLayerMask`, `maxGroundSearchDistance` — unused by current movement; left in place, harmless.
- `dropLayerMask` must remain set to the "Drop" layer (index 8, mask = 256).

Floor/platform Tilemap requirements (still relevant for collider-based queries elsewhere):
- `TilemapCollider2D` must be present on the Tilemap GameObject.
- `CompositeCollider2D` geometry type must be **Polygons** (not Outlines). Outlines mode creates edge-only shapes with no interior — `Physics2D.OverlapPoint` never detects a point inside it.
- The Tilemap GameObject **Layer** must match `groundLayerMask` (e.g. Layer "Ground").

### Movement Implementation

`TryResolveMoveTarget` (legacy raycast-based ground resolution) is no longer the active movement path — see "Movement Model" above. It is the method to replace if/when pathfinding is added on top of the lane model.

---

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
| `MapDefinition.cs` | `_scripts/World/` | SO — MapId, DisplayName, ObjectiveEnemyId, KillObjective, ObjectiveLabel, SilverReward, UnlocksMapId, UnlockQuestId, UnlockEnemyId, UnlockKillCount, UnlockRequirementLabel |
| `MapDatabase.cs` | `_scripts/World/` | SO list — exposes `Maps` (IReadOnlyList) and `GetMap(mapId)` |
| `MapProgressData.cs` | `_scripts/World/` | `[Serializable]` — MapId, KillCount, IsComplete, IsUnlocked |
| `MapSystem.cs` | `_scripts/World/` | Singleton MonoBehaviour — kill tracking, travel, reward grant |
| `MapContentController.cs` | `_scripts/World/` | Listens for `OnMapChanged`, activates the current map root, moves the player to the matching spawn |
| `PortalGate.cs` | `_scripts/Quests/` | Pure evaluator — destination `MapDefinition` lookup → quest/kill-count check → drives portal alpha/collider/label |
| `EnemyKillTracker.cs` | `_scripts/World/` | Global `enemyId` → kill-count singleton, subscribes to `OnEnemyKilled`, persisted per character |
| `MapWindow.cs` / `MapWindowUI.cs` | `_scripts/UI/` | Popup window — current map UI, see "Map Window" above |
| `ObjectiveHelperUI.cs` | `_scripts/UI/` | Legacy top-strip objective UI; disabled in `TestCombat` |

### Assets

| Asset | Path |
|---|---|
| `MapDatabase.asset` | `_assets/ScriptableObjects/Maps/` |
| `MapDef_Grassland1/2/3.asset` | `_assets/ScriptableObjects/Maps/` |
| `MapRowUI.prefab` | `_assets/Prefabs/UI/` |
| `Portal.prefab` | `_assets/Prefabs/` |

`MapDatabase` is assigned to `GameDatabase.mapDatabase`.

### PlayerSaveData additions

```csharp
public string              CurrentMapId = "grassland_1";
public List<MapProgressData> MapProgress = new List<MapProgressData>();
```

### MapSystem Init Pattern

Same as PlayerProgression — checks `SaveManager.Instance.IsLoaded` in `Start()`, calls `Initialize()` directly if already loaded. `Initialize()` reads `save.MapProgress`, creates missing entries, forces `grassland_1.IsUnlocked = true`, fires `RaiseMapChanged`. Resets `PreviousMapId = null` on every fresh load/new-account.

### Architecture — destination-MapDef-driven unlocks

The destination `MapDefinition` is the sole source of truth for what unlocks a portal — not the portal itself and not `QuestSystem`/`MapSystem`.

- `MapDefinition.UnlockQuestId`/`UnlockEnemyId`/`UnlockKillCount`/`UnlockRequirementLabel` are all optional and ANDed.
- `PortalInteractable` stores only `destinationMapId` — no unlock fields live on the portal.
- `PortalGate` evaluates: destination map lookup via `GameDatabase.Instance.Maps.GetMap()` → `QuestSystem.IsCompleted` + `EnemyKillTracker.GetKillCount` → drives alpha (1.0/0.5), `Collider2D.enabled`, `PortalInteractable.enabled`, optional kill-requirement TMP label. Never deactivates the portal root.
- `PortalGate.Evaluate()` syncs `MapProgressData.IsUnlocked = true` on its destination once unlocked — `MapSystem.TravelTo` still gates on that legacy flag. `MapContentController` separately also force-sets this for every configured map on every map change — known redundancy between the two paths, **not yet cleaned up, not approved as a task** (see Known Limitations).

### Kill Counting

`MapSystem` subscribes to `OnEnemyKilled`. Counts kill if:
1. Current map's progress is not `IsComplete`.
2. `MapDefinition.ObjectiveEnemyId` is empty (any enemy) OR matches the fired `enemyId`.

On objective complete: sets `IsComplete=true`, unlocks next map (if `UnlocksMapId` set), grants silver via `CurrencySystem.Instance.Add(Silver, reward)`, fires `OnMapObjectiveCompleted`.

### Travel

`MapSystem.TravelTo(mapId)` — validates unlocked + not current, updates `save.CurrentMapId`, fires `OnMapChanged`. Also records `PreviousMapId = CurrentMapId` (the map being left) before updating `CurrentMapId`.

### Portal Return-Spawn

`MapSystem.PreviousMapId` — the source map of the most recent explicit `TravelTo`. Reset to `null` inside `Initialize()` on every fresh load/new-account, so a stale value from an earlier session/character never leaks into a fresh map activation.

`MapContentController.HandleMapChanged(mapId)`, after activating the destination root: if `PreviousMapId` is set, searches the destination root's `PortalInteractable`s for one whose `DestinationMapId == PreviousMapId`. If found, spawns the player at that portal's X offset 1.5 units toward the map interior (away from the edge the portal sits on), clamped to `GroundLane` bounds. If not found — or `PreviousMapId` is empty (fresh game, load, direct editor-open, debug travel) — falls back to the existing configured `MapEntry.PlayerSpawn` default. `PortalInteractable` is untouched by this: still travel-only, still stores only `destinationMapId`, no spawn/unlock fields added.

### Rules

- `MapSystem` IS a singleton. Access via `MapSystem.Instance`.
- `MapRowUI.Refresh()` null-checks `MapSystem.Instance` — safe to call before initialization.
- `ObjectiveEnemyId = "slime"` for all three Grassland maps (single enemy type for now).
- When adding a new map: add a `MapDefinition` asset, add it to `MapDatabase`, set `UnlocksMapId` on the previous last map.
- Do NOT add a WorldMap scene. Travel is data-only — same scene, same spawner.
- `TestCombat` holds one player + 4 map-root GameObjects (`Map_grassland_1/2`, `Map_town`, `Map_grassland_3`). Global `EnemySpawner` is disabled for this slice; each map root has its enemies pre-placed.

---

## Enemies

### Movement Speed

`EnemyController.patrolSpeed` (used for both patrol and player-chase) is the real speed source — `EnemyDefinition.MoveSpeed` is a separate, unused field, not wired to anything. `Assets/_assets/Prefabs/Enemies/Slime.prefab` → `patrolSpeed = 0.6` (reduced from 1.5). All scene slime instances inherit this from the prefab; no per-instance overrides exist or are needed.

### Grassland3 Local Respawn

`LocalEnemyRespawner` (`_scripts/Enemies/LocalEnemyRespawner.cs`) is attached only to `Map_grassland_3`. On `Awake()` it records each child `EnemyController`'s original position and subscribes to `OnKilled`; on death it waits `respawnDelay` (3s default) then repositions and re-`SetActive(true)`s the **same** enemy instance (no `Instantiate`). This is a **demo/local-only respawn mechanism** — it has no spawn-point pooling, scaling, or difficulty curve, and is not the final/general spawner architecture. `Map_grassland_2` has no respawner — its one tutorial slime stays dead after the kill, by design.

---

## Talent System

### Files

| File | Location | Role |
|---|---|---|
| `TalentDefinition.cs` | `_scripts/Talents/` | SO — TalentId, DisplayName, Description, MaxLevel, stat bonuses per level, `Icon` (Sprite), `GetEffectText(level)` |
| `TalentDatabase.cs` | `_scripts/Talents/` | SO list — exposes `Talents` (IReadOnlyList) |
| `TalentSaveData.cs` | `_scripts/Talents/` | `[Serializable]` — TalentId + Level |
| `TalentSystem.cs` | `_scripts/Talents/` | Singleton MonoBehaviour — `GetLevel()`, `CanUpgrade()`, `Upgrade()`, stat bonus getters |
| `SkillDefinition.cs` | `_scripts/Skills/` | SO — SkillId, DisplayName, Description, Icon, MpCost, Cooldown, BaseDamage, RequiredTalentId, RequiredTalentLevel |
| `SkillDatabase.cs` | `_scripts/Skills/` | SO list — exposes `Skills` (IReadOnlyList) and `GetSkill(skillId)` |
| `TalentSlotUI.cs` | `_scripts/UI/` | Square 72×72 slot — icon + level text, click to select, drag in assign mode |
| `TalentWindow.cs` | `_scripts/UI/` | Popup window — grid of TalentSlotUI, assign mode toggle, delegates to TalentInfoPanel |
| `TalentInfoPanel.cs` | `_scripts/UI/` | Read-only detail panel + Upgrade button for the currently selected talent |
| `SkillSlotUI.cs` | `_scripts/UI/` | One hotbar slot — receives drag-drop from TalentSlotUI, stores skillId |
| `SkillHotbarUI.cs` | `_scripts/UI/` | 3-slot hotbar at bottom-center — initializes slots from `HotbarSkillIds` |

### Assets

| Asset | Path |
|---|---|
| `TalentDatabase.asset` | `_assets/ScriptableObjects/Talents/` |
| `TalentDef_*.asset` (×7) | `_assets/ScriptableObjects/Talents/` |
| `SkillDatabase.asset` | `_assets/ScriptableObjects/Skills/` |
| `SkillDef_Fireball.asset` | `_assets/ScriptableObjects/Skills/` |
| `TalentSlotUI.prefab` | `_assets/Prefabs/UI/` |

### TalentWindow Layout

```
TalentWindow (TalentWindow component)
  └── WindowPanel [620×500] (Image, VLG)
        ├── TitleBar [44px] (HLG) — TitleText, PointsText, AssignModeBtn, CloseBtn
        └── ContentRow (HLG)
              ├── TalentGrid (GridLayoutGroup, 3 cols, 72×72 cells)
              └── TalentInfoPanel (TalentInfoPanel component)
                    └── InfoContent (VLG) — NameText, LevelText, Description,
                                           CurrentEffect, NextEffect, UpgradeBtn,
                                           SkillSection (shown only for skill talents)
```

Press **T** to open/close (debug key on TalentWindow component).

### TalentSlotUI Prefab Structure

```
TalentSlotUI [72×72] (Image dark bg, TalentSlotUI component)
  ├── Icon (Image — TalentDefinition.Icon; grey placeholder if null)
  └── LevelText (TMP — "0/5" at bottom)
```

No upgrade button on the slot. Clicking the slot selects it and updates TalentInfoPanel.

### Normal Mode vs Assign Mode

**Normal mode (Assign OFF):**
- Click any slot → TalentInfoPanel shows talent details + Upgrade button
- No dragging

**Assign mode (Assign ON — toggle button in TitleBar):**

| Slot type | State | alpha | blocksRaycasts | Draggable |
|---|---|---|---|---|
| Passive talent | — | 0.35 | false | no |
| Skill talent | locked (level < RequiredTalentLevel) | 0.50 | true | no |
| Skill talent | unlocked | 1.00 | true | yes |

Drag from TalentSlotUI → drop on SkillSlotUI in the hotbar to assign the skill.

Closing TalentWindow always resets assign mode to OFF.

### Upgrade Button

Upgrade button lives in TalentInfoPanel (bottom of InfoContent). It is:
- Visible whenever InfoContent is active (a talent is selected)
- Interactable only when: `TalentSystem.CanUpgrade(talent)` → points > 0 AND level < MaxLevel
- On click: calls `TalentSystem.Instance.Upgrade(_talent)` → fires `OnTalentChanged` → all slots + InfoPanel refresh

### TalentSystem Stat Getters (used by PlayerStats.Recalculate)

```csharp
GetATKMinBonus()              // sum of ATKMinPerLevel × level across all talents
GetATKMaxBonus()
GetMaxHPBonus()
GetMoveSpeedBonus()
GetMaxMPBonus()
GetCurrencyMultiplierBonus()
GetFireballDamageBonus()
GetInventorySlotBonus()       // int — used by InventorySystem.GetCapacity(), not a PlayerStats field
```

### Skill Hotbar

`SkillHotbarUI` lives on `Canvas/MainHUD/SkillHotbar` (HLG, 3 slots, bottom-center, anchored above the button bar).

Each `SkillSlotUI`:
- `IDropHandler` — accepts drops where `DragHandler.Source == DragSource.SkillPanel`
- On drop: calls `AssignSkill(skillId)` → updates `PlayerSaveData.HotbarSkillIds[slotIndex]` and icon
- `IPointerClickHandler` — casts the assigned skill via `PlayerCombatController.TryCastSkill`

`SkillSlotUI` IconBG/IconFront show/hide based on whether the assigned skill has an icon; radial cooldown fill via `PlayerCombatController.GetSkillCooldownProgress01(skillId)`; hover (1s delay) shows `SkillTooltipUI` (`Canvas/MainHUD/SkillTooltip`) with name/description/MP/cooldown/talent requirement.

### DragHandler Changes

`DragSource` enum gained a third value: `SkillPanel`.

`DragHandler.BeginDrag(string id, Sprite icon, DragSource source)` overload — enables drag icon always (even if icon is null), sets source to SkillPanel.

### Skill-to-Talent Link (data-driven)

`SkillDefinition.RequiredTalentId` links a skill to the talent that unlocks it.
Both `TalentSlotUI` and `TalentInfoPanel` search `GameDatabase.Instance.Skills` at initialize time to find the linked skill. No hardcoded IDs.

Currently only `SkillDef_Fireball` is defined (`RequiredTalentId = "fireball_training"`, `RequiredTalentLevel = 1`).

### Fireball (implemented)

Click the hotbar slot to cast. Costs MP, deals magic damage to the current/nearest valid enemy, then enters cooldown. Damage includes `TalentSystem.GetFireballDamageBonus()`.

### Rules

- `TalentSystem` IS a singleton. Access via `TalentSystem.Instance`.
- `TalentDatabase` and `SkillDatabase` are both assigned to `GameDatabase` asset.
- Do NOT add skill casting, MP spending, or projectile logic beyond Fireball until a second skill is explicitly requested.
- `TalentDefinition.Icon` sprites are not yet assigned in the asset files — slots show grey placeholder. Assign sprites in the Inspector when art is ready.

---

## Physics Layers & Collision Matrix

Goal: Player/Enemy colliders stay **solid and query-detectable** (clicking/targeting) but do **not** physically collide with each other (prevents Dynamic-body depenetration jitter — historical; player/enemy are now Kinematic under the lane model, but the layer separation below remains the active configuration).

- Physics layers (`TagManager.asset`): **`Player` = 6**, **`Ground` = 7**, **`Drop` = 8**, **`Enemy` = 9**.
- Assignments: Player GameObject (scene) → layer **Player**; `Slime.prefab` root → layer **Enemy** (colliders are on the roots); Ground Tilemap → **Ground**; WorldDrop → **Drop**.
- `Physics2DSettings.asset` layer collision matrix:
  - Player ↔ Ground: **enabled**
  - Enemy ↔ Ground: **enabled**
  - Player ↔ Enemy: **disabled**
  - Enemy ↔ Enemy: **disabled**
  - Enemy ↔ Drop: **disabled**
  - Player ↔ Drop: **enabled** (pickup uses an `OverlapPoint` query, not physical contact)
- Colliders remain **non-trigger**. `Physics2D.OverlapPointAll` / `OverlapPoint` are queries and ignore the collision matrix, so click-targeting and drop pickup are unaffected (`m_QueriesHitTriggers` / `m_QueriesStartInColliders` are on).

> ⚠️ Editing `ProjectSettings/*.asset` (TagManager, Physics2DSettings) needs care: do it in **edit mode** only, and prefer a single mechanism (SerializedObject or direct file edit, not both interleaved). `TagManager.asset` holds **both** physics `layers:` and `m_SortingLayers:` — never overwrite one and lose the other. `uniqueID` in `m_SortingLayers` is **unsigned** (use SerializedProperty `longValue`, not `intValue`). Sorting layers in use: Default, Background, Floor, Enemy, Player, FloatText.

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
- `TalentSystem` — talent upgrade logic and stat bonus getters
- `MapSystem` — kill tracking, travel, objective completion. Note: `PortalGate`/`MapContentController` read/write `MapProgressData.IsUnlocked` via `GetProgress()` only — `MapSystem.cs` itself is untouched.
- `PlayerStats.Recalculate()` — stat pipeline (only extend at the end with new bonuses)
- `ItemWindow`, `CraftingWindow`, `VaultWindow`, `MapWindow` — window UI logic (buttons and events are wired). `CraftingWindow`/`CraftingDetailPanel`/`CraftingRecipeSlotUI`/`IngredientRowUI` were intentionally modified once under explicit instruction to adapt to a manual UI redesign — still protected going forward.
- `ObjectiveHelperUI` — disabled (not deleted) in-scene for the quest demo; still protected if re-enabled.
- Player/Slime Hurt/Death animation wiring — real clips exist for both; never fake death with a freeze, never let Hurt override Death, never route player death respawn through `PreviousMapId`/back-portal spawn.
- `TalentWindow`, `TalentInfoPanel`, `SkillHotbarUI`, `SkillSlotUI` — talent and hotbar UI logic (wired and verified)
- `QuestSystem`, `MapSystem` (again, emphasized) — never add branching, multi-active-quest, or auto-unlock-next-map logic. Unlocks are destination-MapDef-driven only (`PortalGate`), not `QuestSystem`/`MapSystem`-driven.
- `PortalInteractable` — travel-only. Never give it unlock-requirement fields, and never give it spawn-position fields — `destinationMapId` only.
- `SaveManager` architecture, `DialogueSystem` core, `MainHUD` window-toggle logic — do not rewrite.
- BootScene menu flow / scene-authored UI — do not restore the runtime-generated `StartupMenu` UI path as the primary flow; do not redesign UI unless asked.
- `ObjectiveHelperUI` — do not reactivate.
- Map4, Map5, boss — do not implement.

---

## Known Limitations / TODOs

Unapproved polish/debt — do not promote any of these to an active task without explicit instruction:

- **Camera follow** may need a dedicated follow-target child: after the player root moved center→feet under the lane movement model, a camera that tracks the player transform sits ~0.5 lower than before.
- **Enemy attack float text** spawns at `playerPos + 0.8` (`EnemyController`); relative to the feet-root it appears ~0.5 lower than intended — may need an offset tweak.
- **PortalGate / MapContentController redundancy:** both write `MapProgressData.IsUnlocked` for the same maps — known overlap, not cleaned up, not currently causing a bug.

Other open items:

- **MP bar** in MainHUD shows real current MP (`OnPlayerMPChanged` wired). No longer a placeholder.
- **Player name** is hardcoded "Hero" in save data; the Name text object in MainHUD's CharacterPanel is static/manual, not script-driven. Add a `PlayerName` field to PlayerSaveData when character creation is built.
- **Debug keys** T (Talent), C (Crafting), V (Vault), Tab (Inventory), M (Map) are still active. They are guarded by `enableDebugKey` bools on each window. Remove or disable them once MainHUD buttons are the only entry point.
- **Quest / Settings HUD buttons** still log placeholder messages. The tutorial `QuestSystem` and always-visible Quest Window are implemented independently; Settings remains unimplemented.
- **Talent / Skill icons** — `TalentDefinition.Icon` and `SkillDefinition.Icon` sprites are not assigned in the ScriptableObject assets. Slots show a grey placeholder. Assign sprites in the Inspector when art is ready.
- **Multiple windows** can be open simultaneously. No WindowManager exists. Add one only if needed.
- **Click-to-move Y** — under the lane model, Y is always `GroundLane.GroundY`; there is no vertical movement and no pathfinding.
- **`groundLayerMask`/`maxGroundSearchDistance`** on `PlayerCombatController` are legacy fields from the pre-lane raycast movement, currently unused but left in place — harmless.
- **Player / Enemy animations** are implemented (Animator + sprite-swap clips), including real Hurt and Death clips for both Player and Slime (see "Hurt/Death animation" above) — no animation is faked with a freeze. Driver components own facing and read controller movement intent; `SpriteFeetAligner` keeps differently cropped frames grounded without touching gameplay transforms.
- **RemoveItem/Equip are itemId-based, not slot-index-based.** If the same item exists in multiple slots, only the first match is affected. Future task: add `RemoveItemAt(slotIndex)`, `EquipFromSlot(slotIndex)`, `MoveItem(fromSlot, toSlot)` for exact-slot drag/drop and equip.
- **Full QA pass still needed** across save/load, inventory, talents, hotbar, crafting, and MainHUD together in one play session — the authoritative manual route lives in `.claude/HANDOFF_CURRENT_STATE.md`.
- **Fireball needs a polish pass:** final icon asset, visual feedback (projectile or hit effect — currently damage is applied directly with no visual), and feedback when MP is insufficient to cast (currently silently does nothing).
- **MainHUD visual polish** may still be needed (colors/spacing/art), but the layout itself is manually controlled in the scene — do not move/resize CharacterPanel/ButtonBar without instruction.
- **CraftingWindow craftable path** (green/enough materials, successful craft, sort moving a recipe to the front) should be manually re-tested in a fully initialized player/inventory session.

---

## Working Instructions

Before implementing any feature:

1. Read GameDesign.md.
2. Follow existing architecture.
3. Only modify files related to the requested feature.
4. Do not create unrelated systems.
5. Keep implementations simple and maintainable.

When unsure, choose the simpler solution.
