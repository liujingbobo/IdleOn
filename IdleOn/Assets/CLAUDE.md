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

## Save System (Account-based — updated 2026-06-16)

`AccountSaveData` is now the top-level save file (`account_save.json` at `Application.persistentDataPath`). Old `player_save.json` is ignored.

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

Simple programmer-art `StartupMenu` freezes gameplay on Play until menu/character selection completes.

- Main Menu: **New Save**, **Load Save** (disabled if `account_save.json` doesn't exist).
- Character Select: lists existing characters + **Create New Character**. Names auto-generated (Hero 1, Hero 2, ...).
- No character deletion, no text input, no multiple account save slots, no UI polish yet.

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

### Fixed-slot model (updated 2026-06-17)

`InventoryData.Slots` is a **fixed-size list**, not a dense occupied-only list. Every index in `Slots` is a real slot position, including empty ones.

- `InventorySlotData.IsEmpty` (`string.IsNullOrEmpty(ItemId) || Quantity <= 0`) and `Clear()` (`ItemId = null`, `Quantity = 0`) mark/clear a slot without removing its list entry.
- `InventoryData.EnsureSlots(capacity)` appends empty slots until `Slots.Count >= capacity`. Never removes/shrinks the list — if capacity ever decreases, extra slots are left in place and simply ignored by display/index bounds checks.
- `AddItem`: stacks into the first existing non-empty slot with a matching `ItemId`; otherwise scans for the first empty slot within `capacity` and places the item there. Existing items are never moved.
- `RemoveItem`: still **itemId-based** (finds the first matching non-empty slot), decrements, and calls `Clear()` on that slot when quantity hits 0. The slot stays in the list — later slots never shift.
- `InventorySystem.Data` getter calls `EnsureSlots(capacity)` on every access. This both pads old dense saves (existing items keep their list position starting at slot 0; empty slots are appended after them) and grows the list whenever capacity increases.
- **Known limitation:** `RemoveItem`/`Equip` operate on itemId, not a specific slot index. If the same item ever exists in more than one slot, only the first match is affected. See "Known Limitations" below for the planned `RemoveItemAt`/`EquipFromSlot`/`MoveItem` follow-up.

### Inventory Expansion talent (implemented 2026-06-17)

`TalentDefinition.InventorySlotsPerLevel` (new field, mirrors the other PerLevel fields) — set to `20` on `TalentDef_InventoryExpansion.asset` (`TalentId = "inventory_expansion"`, `MaxLevel = 5`). `TalentSystem.GetInventorySlotBonus() → int` sums `level × InventorySlotsPerLevel` across all talents.

`InventorySystem.GetCapacity()` = `InventoryData.Capacity` (base, persisted — grows only via the Inventory Expansion **consumable**) **+** `TalentSystem.GetInventorySlotBonus()`. The talent bonus is computed live every call and is never written into `InventoryData.Capacity` — keeps persisted base capacity and derived talent bonus separate.

`InventoryData.AddItem(itemId, totalCapacity, quantity=1)` takes the effective capacity as a parameter (instead of reading its own field) so the talent bonus actually unlocks placeable/empty slots, not just page display. `InventorySystem.TryAddItem`/`Data` getter pass `GetCapacity()` (base+bonus) into `AddItem`/`EnsureSlots`.

`TalentSystem.Upgrade()` fires `GameEvents.RaiseInventoryChanged()` (in addition to `OnTalentChanged`) whenever the upgraded talent has `InventorySlotsPerLevel != 0` — `ItemWindow` is already subscribed to `OnInventoryChanged` and only acts if the panel is open, so pagination/page-buttons refresh live if `InventoryPanel` is open during the upgrade, and pick up the new capacity next time it's opened otherwise. No new event was added.

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

**Pagination (added 2026-06-17):** 20 fixed `InventorySlotUI` slots are reused across pages — no slots are created/destroyed. Visible slot `i` on page `p` maps to global slot index `p * 20 + i`, read directly from `InventorySystem.GetSlots()` (the fixed-slot list) — empty global indices show an empty slot UI.

- `NextPage()` / `PrevPage()` increment/decrement `_currentPage`, clamp against `totalPages = Ceil(capacity / 20)`, then refresh — page switching never moves or mutates inventory data, only changes which slice is displayed.
- Page button visibility: 1 page → both hidden; first page → Next only; middle page → Prev + Next; last page → Prev only.
- `_currentPage` resets to 0 on `Open()`/`ToggleWindow()` open branch, and is re-clamped every refresh in case capacity shrinks/grows.
- `CloseButton` wired to `ItemWindow.Close()`.

**Equipment placeholders (added 2026-06-17):** each `EquipmentSlotUI` has a `[SerializeField] GameObject placeholder` child. `Refresh()` shows the placeholder and hides the icon when the slot has no equipped item with an icon; shows the icon and hides the placeholder when equipped. Unequipping restores the placeholder automatically (same `Refresh()` call, driven by `OnEquipmentChanged`). New nested equipment slot UI (`Left`/`Middle`/`Right` groups) is wired into `ItemWindow.equipmentSlots`; old inactive flat equipment slots were left in place, untouched.

### Crafting Window (redesigned 2026-06-18)

`CraftingWindow` component lives on `Canvas/CraftingWindow` child. `windowPanel` field is wired to `BookUI` (the manually-redesigned book-style panel root) — this was previously unwired/null and would have NPE'd on `IsOpen`/`Open()`/`Close()`.

Press **C** to open/close (temporary debug key — `enableDebugKey` bool on component).

Public API: `Open()`, `Close()`, `Toggle()`.

**Layout (manually redesigned, code adapted to it — do not redesign further without instruction):**
- **Left (`LeftPanel`, `CraftingDetailPanel` component):**
  - `ItemInfo` — icon (nested `IconBackground/Icon`, frame stays on the outer `IconBackground`), name, description. All cleared/hidden when no recipe is selected.
  - `Materials` — scrollable list of one reusable row per required ingredient.
  - `CraftButton` — single button, position is user-controlled in the scene.
- **Right (`ScrollView/Viewport/Content`)** — recipe list, one reusable row per recipe.

**No selection on open:** `CraftingDetailPanel.Show(null)` runs from `Awake()` (not `Start()` — the window starts inactive, so `Start()` would be deferred until first `Open()`, leaving stale placeholder text/icon visible for a frame). Empty state: icon hidden, name/description empty, materials list empty, `CraftButton.interactable = false`.

**Recipe row (`CraftingRecipeSlotUI`, prefab `Assets/_assets/Prefabs/UI/CraftingRecipeSlot.prefab`):** one prefab, reused for every recipe. `itemIcon` is wired to the nested `IconBackground/Icon` (fixed this session — was incorrectly wired to the outer frame `IconBackground`). New fields `craftableSprite`/`notCraftableSprite` — `Refresh()` swaps `background.sprite` based on `CraftingSystem.CanCraft(recipe)`. `canCraftDot` tint logic is unchanged (both indicators coexist).

**Recipe list sort:** `CraftingWindow.PopulateRecipeList()` sorts craftable recipes first, then uncraftable (stable, preserves DB order within each group). Re-sorts and rebuilds (via `RefreshAllSlots()`) on `OnInventoryChanged` while the window is open, so crafting/consuming materials live-reorders the list. Old instantiated slots are destroyed by reference (tracked in `_slots`), not by iterating `transform` children — `Destroy()` is deferred in Play mode, so trusting live child count mid-frame double-instantiates.

**Material row (`IngredientRowUI`, prefab `Assets/_assets/Prefabs/UI/IngredientRow.prefab`):** one prefab, reused for every ingredient. Count text format is exactly `{owned}/{required}` (no spaces). `ColorEnough` = `RGBA(0.361,0.459,0.173,1)` (green), `ColorMissing` = `RGBA(0.761,0.294,0.282,1)` (red) — matched exactly to the sample rows the user hand-placed before this session, not arbitrary colors.

**Demo rows removed:** the hand-placed example rows the user used to show the desired look (`RecipeRow-Enough`/`RecipesRow-NotEnough` on the left, `CraftingRecipeSlot-CanCraft`/`CraftingRecipeSlot-CanNotCraft` on the right) were converted into the two prefabs above (copying their sprites/colors/structure) and then removed from the scene — they are not part of the runtime flow.

Subscribes to `OnInventoryChanged` to refresh row backgrounds, material counts/colors, recipe order, and `CraftButton` state together (`CraftingDetailPanel.Refresh()` + `CraftingWindow.RefreshAllSlots()`).

Add result item first before consuming ingredients. If inventory full, the failure is logged (`Debug.LogWarning`, no dedicated status UI text exists in the new layout) and nothing is consumed.

**Not verified live:** the "enough materials" / craftable path (green count, craftable sprite, enabled CraftButton, successful craft) was verified by code symmetry only — the test scene's player/inventory had 0 capacity (uninitialized in that session's Play context), not a bug in this change. Re-verify with a fully initialized player/inventory session (see Known Limitations).

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

**XP bar** uses `PlayerProgression.ExpForNextLevel(Level)` as cap — tied to the real level-up formula.

Player name is still hardcoded as "Hero" in save data — the Name text object in the scene is static/manual, MainHUD does not write to it.

---

## Movement Model — Phase 1 (single-lane, no-gravity, feet-root, 2026-06-19)

**This supersedes the old Dynamic-Rigidbody / gravity / `transform.position`-write movement described in the older "Movement Implementation", "Ground Detection", and 2026-06-16 physics session notes below.** Those sections are kept for history but no longer describe runtime behavior.

- **Single lane, no gravity.** The current scene has exactly one ground lane. Player and enemies do **not** fall — they are driven deterministically.
- **`GroundLane` (`_scripts/World/GroundLane.cs`)** is the single source of truth for the lane. `GroundLane.Current` (static) exposes `GroundY`, `MinX`, `MaxX`, and `ClampX(x)`. One instance per scene; it registers in `OnEnable` and clears `Current` in `OnDisable` (covers both disable and destroy). Current scene values: `GroundY = -2.0`, `MinX = -9.5`, `MaxX = 10.5` (Tilemap worldX [-10, 11], 0.5 inset).
- **Rigidbody2D = Kinematic, `gravityScale = 0`** on both Player (scene instance) and `Slime.prefab`. Movement uses `Rigidbody2D.MovePosition` in `FixedUpdate` only — **no velocity writes, no `transform.position` writes for movement** (don't mix paradigms).
- **Root = feet / ground-contact point.** Player root.y = `-2.0` (was center; relocated to feet). `Slime.prefab` root was already feet. Each `FixedUpdate` forces `position.y = GroundLane.GroundY` and X-only horizontal motion; X is clamped to `[MinX, MaxX]`.
- **Pivots:** do **not** change imported sprite pivots. Use the `Sprite` child `localPosition` offset to keep the visual where it was (Player Sprite child localY = `+0.41` to preserve world position after the root moved down 0.5; Slime Sprite child localY `+0.152` unchanged).
- **Click-to-move:** ignores clicked Y entirely; resolves to `GroundLane.ClampX(clickWorldPos.x)` on the lane. (The old `TryResolveMoveTarget` / `groundLayerMask` / `maxGroundSearchDistance` path is no longer used for movement; fields left in place, harmless.)
- **Enemy patrol/chase** stays on the lane: patrol bounds are clamped to `[MinX, MaxX]` in `Start`; Y is forced to `GroundY` every `FixedUpdate`.
- **Enemy hitbox** must be **at least 1 tile tall measured from the feet upward** (Slime BoxCollider2D: offset.y `0.5`, size.y `1` → feet → +1 tile). This guarantees low projectiles connect.
- **`EnemySpawner`** spawns and respawns enemies on `GroundLane.GroundY` (`LaneSpawnPos`) — no gravity to settle them.
- **Fireball / projectiles:** spawn height = `GroundLane.GroundY + projectileHeight` (`projectileHeight` field on `PlayerCombatController`, default `0.6`). Keep `projectileHeight <= 1` so it still hits 1-tile-high enemies. Direction is horizontal only (`Vector2.left/right` from facing) — projectiles stay on the lane. `FireballProjectile.prefab` is already Kinematic + trigger; trigger hits on Kinematic enemies are verified working.
- **Not implemented yet:** multi-lane, ladders, portals, jumping, BFS/pathfinding, lane graph. Single flat lane only.
- **Future interactables / multi-platform:** use a same-lane *walk-to-interact* model first (walk to target X on the current lane, then act); add cross-lane transitions (ladders/portals) as explicit steps later — never physics jumping.

---

## WorldInteractable — Phase 2 (same-lane walk-to-interact, 2026-06-19)

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

---

## Combat Rules

### Map Visual

The combat map is tile/grid-based, similar to IdleOn.

Monsters and the player stand on top of floor/platform tiles.

### Player Input

LMB hold over a WorldDrop (Drop layer) → collect drop. This check runs before all other input.

Single LMB click — priority order:
1. `EventSystem.current.IsPointerOverGameObject()` → do nothing (UI absorbs click)
2. `Physics2D.OverlapPointAll(worldPos)` — scan hits for `EnemyController` → ManualAttack
3. `TryResolveMoveTarget(worldPos, out target)` — ground layer hit → ManualMove
4. Otherwise → do nothing

Auto combat behaviors:
- Auto combat ON → player repeatedly seeks nearest monster, moves to attack range, attacks, continues.
- Auto combat ON + click monster → interrupt current action, ManualAttack, then resume auto combat.
- Auto combat ON + click floor → interrupt current action, ManualMove, then resume auto combat.

### Ground Detection (TryResolveMoveTarget)

`TryResolveMoveTarget(Vector2 clickWorldPos, out Vector2 targetWorldPos)` in `PlayerCombatController`:
1. `Physics2D.OverlapPoint(clickWorldPos, groundLayerMask)` — direct collider hit → `targetWorldPos = (click.x, hit.bounds.max.y)`
2. `Physics2D.Raycast(clickWorldPos, Vector2.down, maxGroundSearchDistance, groundLayerMask)` — downward cast → `targetWorldPos = hit.point`
3. Both fail → return false → do nothing

`maxGroundSearchDistance` defaults to 2.5 units (serialized, tweakable in Inspector).

Current movement stores `_manualMoveTarget = new Vector2(groundTarget.x, transform.position.y)` — resolved X from ground, current player Y preserved. Future pathfinding replaces only this assignment.

### Required Inspector Setup

`PlayerCombatController` ground-detection fields:
- `groundLayerMask` — **must be assigned** to the Ground physics layer. If left at 0 (Nothing), `TryResolveMoveTarget` always returns false and click-to-move silently fails for all floor clicks.
- `maxGroundSearchDistance` — default 2.5 units; raise if ground collider is unusually far below typical click positions.

Floor/platform Tilemap requirements:
- `TilemapCollider2D` must be present on the Tilemap GameObject.
- `CompositeCollider2D` geometry type must be **Polygons** (not Outlines). Outlines mode creates edge-only shapes with no interior — `Physics2D.OverlapPoint` never detects a point inside it.
- The Tilemap GameObject **Layer** must match `groundLayerMask` (e.g. Layer "Ground").

`dropLayerMask` on `PlayerCombatController` must remain set to the "Drop" layer (index 8, mask = 256).

### Movement Implementation

`Vector2.MoveTowards` toward `_manualMoveTarget`. No A* pathfinding.

`TryResolveMoveTarget` is the only method to replace when pathfinding is added — the state machine and `UpdateManualMove` stay unchanged.

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
GetFireballDamageBonus()      // reserved — Fireball casting not yet implemented
GetInventorySlotBonus()       // int — used by InventorySystem.GetCapacity(), not a PlayerStats field
```

### Skill Hotbar

`SkillHotbarUI` lives on `Canvas/MainHUD/SkillHotbar` (HLG, 3 slots, bottom-center, anchored above the button bar).

Each `SkillSlotUI`:
- `IDropHandler` — accepts drops where `DragHandler.Source == DragSource.SkillPanel`
- On drop: calls `AssignSkill(skillId)` → updates `PlayerSaveData.HotbarSkillIds[slotIndex]` and icon
- `IPointerClickHandler` — logs placeholder (skill casting not yet implemented)

### DragHandler Changes

`DragSource` enum gained a third value: `SkillPanel`.

`DragHandler.BeginDrag(string id, Sprite icon, DragSource source)` overload — enables drag icon always (even if icon is null), sets source to SkillPanel.

### Skill-to-Talent Link (data-driven)

`SkillDefinition.RequiredTalentId` links a skill to the talent that unlocks it.
Both `TalentSlotUI` and `TalentInfoPanel` search `GameDatabase.Instance.Skills` at initialize time to find the linked skill. No hardcoded IDs.

Currently only `SkillDef_Fireball` is defined (`RequiredTalentId = "fireball_training"`, `RequiredTalentLevel = 1`).

### Rules

- `TalentSystem` IS a singleton. Access via `TalentSystem.Instance`.
- `TalentDatabase` and `SkillDatabase` are both assigned to `GameDatabase` asset.
- Do NOT add skill casting, MP spending, or projectile logic until Phase 2B.
- `TalentDefinition.Icon` sprites are not yet assigned in the asset files — slots show grey placeholder. Assign sprites in the Inspector when art is ready.

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
- `MapSystem` — kill tracking, travel, objective completion
- `PlayerStats.Recalculate()` — stat pipeline (only extend at the end with new bonuses)
- `ItemWindow`, `CraftingWindow`, `VaultWindow`, `MapWindow`, `ObjectiveHelperUI` — window UI logic (buttons and events are wired). `CraftingWindow`/`CraftingDetailPanel`/`CraftingRecipeSlotUI`/`IngredientRowUI` were intentionally modified 2026-06-18 under explicit instruction to adapt to a manual UI redesign — still protected going forward.
- `TalentWindow`, `TalentInfoPanel`, `SkillHotbarUI`, `SkillSlotUI` — talent and hotbar UI logic (wired and verified)

---

## Known Limitations / TODOs

- **MP bar** in MainHUD now shows real current MP (`OnPlayerMPChanged` wired). No longer a placeholder.
- **Player name** is hardcoded "Hero" in save data; the Name text object in MainHUD's CharacterPanel is static/manual, not script-driven. Add a `PlayerName` field to PlayerSaveData when character creation is built.
- **Debug keys** T (Talent), C (Crafting), V (Vault), Tab (Inventory) are still active. They are guarded by `enableDebugKey` bools on each window. Remove or disable them once MainHUD buttons are the only entry point.
- **Quest / Settings** buttons still log placeholder messages. These systems are not implemented.
- **Skill casting** (Fireball) is implemented — click hotbar slot → `PlayerCombatController.TryCastSkill`, MP cost, cooldown (`GetSkillCooldownProgress01` drives the radial fill on `SkillSlotUI`), hover tooltip. Arcane Power and other skills still not implemented.
- **Talent / Skill icons** — `TalentDefinition.Icon` and `SkillDefinition.Icon` sprites are not assigned in the ScriptableObject assets. Slots show a grey placeholder. Assign sprites in the Inspector when art is ready.
- **NaturalTalent** vault upgrade is wired: each level-up grants `1 + GetTalentPointBonus()` talent points. TalentSystem and TalentWindow are implemented and can now spend these points.
- **Save/Load** ✅ implemented (Account-based, see "Save System" above). `account_save.json` persists Vault (shared) + Players (per-character) + CurrentPlayerId. Old `player_save.json` ignored — no migration. Save-on-quit only (no autosave timer).
- **Multiple windows** can be open simultaneously. No WindowManager exists. Add one only if needed.
- **Click-to-move Y** — `_manualMoveTarget` uses `transform.position.y` (current player Y), not the resolved ground Y from `TryResolveMoveTarget`. The player never moves vertically. Replace the assignment inside `HandleClick` when vertical movement or pathfinding is needed.
- **groundLayerMask** on `PlayerCombatController` must be assigned in the Inspector. If it is 0 (Nothing), all floor clicks silently do nothing. Floor Tilemap must also have `CompositeCollider2D` geometry set to **Polygons** — Outlines mode is incompatible with `Physics2D.OverlapPoint`.
- **Player / Enemy animations** — no Animator components or animation clips exist yet. `PlayerCombatController.State` (CombatState enum) is the intended driver for player animation transitions when added.
- **RemoveItem/Equip are itemId-based, not slot-index-based** (since the 2026-06-17 fixed-slot refactor). If the same item exists in multiple slots, only the first match is affected. Future task: add `RemoveItemAt(slotIndex)`, `EquipFromSlot(slotIndex)`, `MoveItem(fromSlot, toSlot)` for exact-slot drag/drop and equip.
- **Full QA pass still needed** across save/load, inventory, talents, hotbar, crafting, and MainHUD together in one play session — recent sessions verified each system individually (often via reflection/test scripts), not as a full end-to-end playthrough.
- **Fireball needs a polish pass:** final icon asset, visual feedback (projectile or hit effect — currently damage is applied directly with no visual), and feedback when MP is insufficient to cast (currently silently does nothing).
- **MainHUD visual polish** may still be needed (colors/spacing/art), but the layout itself is now manually controlled in the scene — do not move/resize CharacterPanel/ButtonBar without instruction.
- **CraftingWindow craftable path** (green/enough materials, successful craft, sort moving a recipe to the front) should be manually re-tested in a fully initialized player/inventory session — see "Crafting Window" above for why it wasn't exercised live this session.

---

## Working Instructions

Before implementing any feature:

1. Read GameDesign.md.
2. Follow existing architecture.
3. Only modify files related to the requested feature.
4. Do not create unrelated systems.
5. Keep implementations simple and maintainable.

When unsure, choose the simpler solution.

---

# Session Update — 2026-06-16

## Animation System

Player and Slime use **Animator + AnimationClips that animate `SpriteRenderer.sprite`** (frame-by-frame sprite swap). No code swaps sprites.

- Assets: `_assets/Animations/Player/` (`Player_Idle`, `Player_Run`, `Player_Attack`, `PlayerAnimator.controller`) and `_assets/Animations/Enemies/` (`Slime_Idle`, `Slime_Move`, `Slime_Attack`, `Slime_Dead`, `SlimeAnimator.controller`).
- The **Animator lives on the `Sprite` child** (same GameObject as the SpriteRenderer); the driver lives on the root.
- `PlayerAnimatorDriver` (`_scripts/Characters/`) reads `PlayerCombatController.State` + per-frame position delta → sets `IsMoving` / `IsAttacking`. States: **Idle / Run / Attack**.
- `EnemyAnimatorDriver` (`_scripts/Enemies/`) reads `EnemyController.State` + position delta → sets `IsMoving` / `IsAttacking` / `IsDead`. States: **Idle / Move / Attack / Dead**.
- Movement detection is velocity-based (`moveSpeedThreshold`, units/sec) so it is frame-rate independent.
- **Animation drivers own visual facing.** `EnemyController` no longer flips the sprite (its old `FlipSprite` was removed).
- **`invertFacing` (bool, inspector)** on both drivers: current player/slime art faces **left** by default, so the Player scene instance and the Slime prefab use `invertFacing = true`. Facing = `flipX = (movingLeft) ^ invertFacing`. Do **not** rotate/scale sprites to fix facing.
- **Slime death clip** is visible because `EnemyController.HandleDied()` delays the pooled `SetActive(false)` by `deathDisableDelay` (~0.5s, matches the 6-frame Dead clip). Loot/XP/`OnKilled` timing is unchanged.

## Dynamic Rigidbody2D

- Player and Slime are now **Dynamic** Rigidbody2D (changed this session) to support future maps with vertical platforms / gravity / stairs. **Keep Freeze Rotation Z enabled.**
- Movement is still the original simple `transform.position` writes (`PlayerCombatController`: `MoveToTarget`/`UpdateManualMove`/`UpdateManualAttack`; `EnemyController`: `UpdatePatrol`/`MoveTowardPlayer`). This is acceptable **temporarily**.
- **Future task:** move Dynamic-body movement to `Rigidbody2D.MovePosition` (or velocity) in `FixedUpdate`. Direct transform writes fight the physics solver. **Do not refactor movement unless explicitly requested.**

## Stale / Dead Target Handling (kill-jitter fix)

`PlayerCombatController.IsValidTarget(enemy)` = non-null **and** `activeInHierarchy` **and** `IsAlive` **and** `State != EnemyState.Dead`. Used in `HandleClick`, `MoveToTarget`, `TryAttack`, `UpdateManualAttack`, and after `SeekTarget`. The player clears `_currentTarget` / `_manualAttackTarget` on death, auto-combat reacquires a live target, manual returns to Idle/Auto. `EnemyController` sets `Rigidbody2D.simulated = false` during the death delay and restores it in `OnEnable` (pooled respawn).

## Physics Layers & Collision Matrix

Goal: Player/Enemy colliders stay **solid and query-detectable** (clicking/targeting) but do **not** physically collide with each other (prevents Dynamic-body depenetration jitter).

- Physics layers (`TagManager.asset`): **`Player` = 6**, **`Ground` = 7**, **`Drop` = 8**, **`Enemy` = 9** (Player/Enemy added this session; Ground/Drop pre-existing).
- Assignments: Player GameObject (scene) → layer **Player**; `Slime.prefab` root → layer **Enemy** (colliders are on the roots); Ground Tilemap → **Ground** (already was); WorldDrop → **Drop** (unchanged).
- `Physics2DSettings.asset` layer collision matrix:
  - Player ↔ Ground: **enabled**
  - Enemy ↔ Ground: **enabled**
  - Player ↔ Enemy: **disabled**
  - Enemy ↔ Enemy: **disabled**
  - Enemy ↔ Drop: **disabled**
  - Player ↔ Drop: **enabled** (unchanged — pickup uses an `OverlapPoint` query, not physical contact)
- Colliders remain **non-trigger**. `Physics2D.OverlapPointAll` / `OverlapPoint` are queries and ignore the collision matrix, so click-targeting and drop pickup are unaffected (`m_QueriesHitTriggers` / `m_QueriesStartInColliders` are on).

> ⚠️ Editing `ProjectSettings/*.asset` (TagManager, Physics2DSettings) needs care: do it in **edit mode** only, and prefer a single mechanism (SerializedObject or direct file edit, not both interleaved). `TagManager.asset` holds **both** physics `layers:` and `m_SortingLayers:` — never overwrite one and lose the other. `uniqueID` in `m_SortingLayers` is **unsigned** (use SerializedProperty `longValue`, not `intValue`). Sorting layers in use: Default, Background, Floor, Enemy, Player, FloatText.

## Ground / Tilemap & Click-to-Move (still current)

- Ground Tilemap: `TilemapCollider2D` + `CompositeCollider2D`; geometry should stay **Polygons** (Outlines mode breaks `OverlapPoint`).
- `PlayerCombatController.groundLayerMask` must be the Ground layer; if left Nothing/0, all floor clicks silently do nothing.
- Click priority: UI → does nothing; enemy collider → attack; direct ground hit → move; empty space within `maxGroundSearchDistance` above ground → downward raycast finds surface → move; sky/background with no ground below → does nothing.

## Known Limitations (still open — do not lose these)

- **Save/Load** is still not auto-triggered; every play session starts fresh.
- **Skill casting** not implemented; hotbar assignment exists but clicking a slot is a placeholder.
- **MP / current-MP** system is still a placeholder (MP bar shows MaxMP/MaxMP).
- **Talent / Skill icon sprites** still unassigned on some SO assets (grey placeholder).
- **Quest system** not implemented.
- **Offline progression** not implemented.
- **No WindowManager** — multiple windows can be open at once.
- **Dynamic Rigidbody2D movement** should eventually move off direct `transform.position` (see above).

## Next Task (superseded — see below)

**Move Dynamic Rigidbody2D movement to `Rigidbody2D.MovePosition` / velocity in `FixedUpdate`** (player + enemy). The Physics2D layer separation already removed the player↔enemy jitter; this is the long-term correctness fix. The previously-planned "Player/Enemy physical collision via layers" task is **complete this session**.

---

# Session Update — Account Save + Character Select (2026-06-16)

## Completed this session

- **Account save architecture**: `AccountSaveData` is the new top-level save file. Contains `Version`, `Vault` (VaultSaveData, account-shared), `Players` (List<PlayerSaveData>), `CurrentPlayerId`. Vault and Players are siblings.
- **PlayerSaveData**: `VaultData` removed; `PlayerId` and `PlayerName` added. All other per-character data (level/exp/talents/hotbar/inventory/equipment/currency/map progress) unchanged.
- **SaveManager**: rewritten to be account-based. Exposes `CurrentAccount`, `CurrentSave` (name kept for compatibility), `CurrentVault`. New save file `account_save.json`; old `player_save.json` ignored. Supports account load/create/save, character create/select, save-on-quit.
- **VaultSystem**: now reads `SaveManager.Instance.CurrentVault` instead of `CurrentSave.VaultData`. Vault upgrades are shared across all characters — verified Hero 2 sees Hero 1's vault upgrades.
- **StartupMenu**: new programmer-art UI. Freezes gameplay until menu/character selection. Main Menu (New Save / Load Save, Load disabled if no save file), Character Select (existing characters + Create New Character, auto-named Hero N). No deletion, no text input, no multiple save slots, no polish yet.

## Verified behavior

- Fresh Play → Main Menu shown.
- New Save → creates `account_save.json`.
- Create/select character → enters gameplay.
- Restart Play → Load Save enabled.
- Load → restores characters; Hero 1 and Hero 2 are separate `PlayerSaveData` entries.
- Hero 1 progress persists; Hero 2 does not overwrite Hero 1.
- Vault upgrades persist as shared account data.
- Console has no project errors.

## Known limitations / later work

- Save/load migration is minimal; old `player_save.json` ignored.
- Character deletion not implemented.
- Character rename / text input not implemented.
- Multiple account save slots not implemented.
- Offline progression not implemented.
- Cloud save not implemented.
- Startup UI is programmer-art only; MainMenu/CharacterSelect polish later.
- Skill casting still not implemented.

## Next Task

**Implement minimal Fireball skill casting from assigned hotbar slot.**

Scope:
- Click hotbar Fireball slot to cast.
- Require Fireball assigned through Talent assign mode.
- Consume MP if current MP exists, or implement minimal current MP if needed.
- Apply cooldown.
- Damage current/nearest valid enemy.
- Use `TalentSystem.GetFireballDamageBonus()`.
- No projectile art/VFX yet unless trivial.
- Do not implement complex projectile collisions unless explicitly approved.

---

# Session Update — Inventory Pagination, Equipment Placeholders, Fixed-Slot Refactor (2026-06-17)

## Completed this session

- **InventoryPanel pagination** — `ItemWindow` reuses its existing 20 `InventorySlotUI` slots across pages instead of creating more. `NextPageBtn`/`PrevPageBtn` wired to `NextPage()`/`PrevPage()`. Button visibility: 1 page → both hidden; first page → Next only; middle → both; last page → Prev only. `CloseButton` wired to `Close()`. See "Inventory UI (ItemWindow)" above.
- **Equipment placeholder icons** — `EquipmentSlotUI.placeholder` shows when a slot has no equipped item, hides when equipped; restored on unequip. New nested equipment slot UI wired into `ItemWindow.equipmentSlots`; old inactive flat slots left alone. See "Equipment placeholders" above.
- **Fixed-slot inventory refactor** — `InventoryData.Slots` changed from a dense occupied-only list to a fixed-size list where every index is a real slot (including empty ones). Removing/equipping an item clears that slot in place; later items never shift. `AddItem` stacks into a matching slot first, then fills the first empty slot. `EnsureSlots(capacity)` pads old dense saves and any future capacity increase without reordering or losing items. See "Fixed-slot model" above.

## Verified

- Compile clean (console: 0 errors/warnings) after each change.
- Play-mode smoke test: `ItemWindow.Open()/NextPage()/PrevPage()/Close()` run without exceptions; panel opens/closes correctly.
- Reflection test against a live `InventoryData` instance: simulated old dense save (5 items, no padding) → `EnsureSlots(20)` padded slots 5–19 empty, items stayed at original indices; `RemoveItem` on a middle slot cleared only that slot, later items did not shift; `AddItem` of a new item landed exactly in the freed slot; `AddItem` of an existing item stacked instead of using a new slot; filling to capacity then adding one more correctly returned `false` with no data loss.

## Known limitations / next task

- `RemoveItem`/`Equip` are itemId-based, not slot-index-based (see "Known Limitations / TODOs" above for the planned `RemoveItemAt`/`EquipFromSlot`/`MoveItem` follow-up).
- Inventory Expansion talent (+20 capacity per level) is **not implemented** — flagged as a gap, not built, per explicit scope instruction this session.

## Next Task

**Implement minimal Fireball skill casting from assigned hotbar slot** (unchanged from previous session — see task above).

---

# Session Update — Inventory Expansion Talent (2026-06-17)

## Completed this session

- **Inventory Expansion talent** — new `TalentDefinition.InventorySlotsPerLevel` field; `TalentDef_InventoryExpansion.asset` (`TalentId = "inventory_expansion"`, MaxLevel 5, +20/level) created and registered in `TalentDatabase.asset` (7th entry). `TalentSystem.GetInventorySlotBonus()` added. `InventorySystem.GetCapacity()` now returns base persisted capacity + live talent bonus. `InventoryData.AddItem` takes an explicit `totalCapacity` param so the bonus actually unlocks placeable slots (not just page display). `TalentSystem.Upgrade()` fires `OnInventoryChanged` when the upgraded talent affects capacity, so `ItemWindow` (already subscribed) refreshes pagination live if open. See "Inventory Expansion talent" above.

## Verified

- Compile clean (console: 0 errors/warnings).
- Reflection test in Play mode against live `TalentSystem`/`InventoryData` instances (no save data touched): bonus at level 0/1/2 = 0/20/40; effective capacity 20→60 at level 2; `AddItem` placed an item into slot 21+ (beyond base 20) only once the bonus made it available; `EnsureSlots(60)` grew `Slots.Count` to exactly 60 with no item movement.

---

# Session Update — Fireball Casting, Hotbar Wiring, MainHUD Rewire (2026-06-18)

## Completed this session

- **Fireball skill casting** — `PlayerCombatController.GetSkillCooldownProgress01(skillId)` added (read-only cooldown progress, 0=just cast, 1=ready). `TryCastSkill` and `_skillReadyTimes` unchanged/untouched.
- **SkillSlotUI rewritten** — IconBG/IconFront show/hide based on whether the assigned skill has an icon, radial cooldown fill via `GetSkillCooldownProgress01`, click-to-cast, hover (1s delay) shows new `SkillTooltipUI` (`Canvas/MainHUD/SkillTooltip`) with name/description/MP/cooldown/talent requirement.
- **SkillHotbarUI.slots[] scene wiring fixed** — `slots[1]`/`slots[2]` were `null` in the scene (only slot 0 ever initialized). Wired to `"SkillSlot1 (1)"`/`"SkillSlot1 (2)"`. No script change.
- **MainHUD rewired to match manually-redesigned CharacterPanel/ButtonBar** — old inactive CharacterPanel rows (NameLevelText/HPBarRow/MPBarRow/XPBarRow/CurrencyRow) were dead; real display is now `StatusGroup/HPBarRow|MPBarRow|EXPBarRow`, `LevelGroup/Text (TMP)`, `Name`. Rewired `MainHUD.cs` serialized fields to the new objects. Level text now `"Lv. {level}"` (was `"Hero  Lv.{level}"` — Name object is separate/static now). HP/MP/EXP text now exact `"cur/max"` (dropped `" XP"` suffix). Added `invButton/craftButton/vaultButton/talentButtonRef/mapButton` + `RefreshButtonStates()` so ButtonBar button `interactable` mirrors real window `IsOpen` state (closed=true/open=false), polled in `Update()` so a window closed via its own CloseButton still un-sticks its button. Fixed copy-paste label bug: MP/EXP rows in StatusGroup both said "HP" — corrected to "MP"/"EXP".

## Verified

- Compile clean throughout.
- Play-mode reflection tests: all 3 hotbar slots now `Initialize()` correctly (previously slots 1/2 silently never ran); cooldown fill, empty-slot icon hiding, tooltip show/hide all confirmed.
- MainHUD: `Lv. 1`, `50/50` (MP), `10/100` (EXP) — exact format confirmed. Button interactable correctly reflects real `.IsOpen` (including windows that started open from a prior scene state) and self-corrects when a window is closed via its own CloseButton (not through MainHUD).

## Known gaps / next task

- Quest and Settings systems still unimplemented (placeholder log only, buttons inactive in scene — no icon assets).
- Skill casting only exists for Fireball; no other skills defined yet.
- No task explicitly queued — pick from "Known Limitations / TODOs" above (e.g. Arcane Power skill, slot-index-based inventory ops, Rigidbody2D MovePosition migration) when ready.

---

# Session Update — CraftingWindow UI Redesign Adaptation (2026-06-18)

## Completed this session

- **Audited and adapted `CraftingWindow` to a manually-redesigned scene hierarchy** (`ItemInfo`/`Materials`/`CraftButton` on the left, sorted recipe list on the right) without altering the redesign itself. See "Crafting Window (redesigned 2026-06-18)" above for full detail.
- `CraftingWindow.cs` — fixed unwired `windowPanel` (was `NULL`, would NPE); removed auto-select-first-recipe on open; recipe list now sorts craftable-first; fixed a Destroy-is-deferred duplicate-row bug by tracking instantiated slots instead of trusting live `transform.childCount`.
- `CraftingDetailPanel.cs` — rewritten for the new `ItemInfo` layout (no more `emptyState`/`detailState` GameObjects); dropped the unused `statusLabel` (craft-fail now logs a warning instead); empty-state init moved to `Awake()` so it's guaranteed set before the panel is ever shown.
- `CraftingRecipeSlotUI.cs` — added `craftableSprite`/`notCraftableSprite`, swaps `background.sprite` by craftable state in `Refresh()`.
- `IngredientRowUI.cs` — `ColorEnough`/`ColorMissing` matched exactly to the user's sample rows; count format fixed to `owned/required` (no spaces).
- Rebuilt `Assets/_assets/Prefabs/UI/CraftingRecipeSlot.prefab` (`itemIcon` fixed to point at the nested `Icon`, was wrongly on the outer frame; added the two new sprite fields) and `Assets/_assets/Prefabs/UI/IngredientRow.prefab` (rebuilt from the user's sample row to match its structure/sprites, now carries `IngredientRowUI`). Demo rows removed from the scene.

## Verified

- Compile clean (console: 0 errors after each change).
- Play-mode: window opens with no selection (icon hidden, text empty, materials empty, CraftButton disabled), no duplicate/leftover rows, selecting a recipe populates `ItemInfo`/`Materials` correctly, insufficient-material row shows exact red `0/5`, uncraftable sprite, disabled CraftButton.
- **Not verified live:** the craftable/enough-materials path — test scene's inventory had 0 capacity in that Play session (separate, unrelated init gap). Verified by code symmetry only.

## Next task

Re-run the CraftingWindow craftable path in a fully initialized player session, then pick up Fireball polish (icon, hit feedback, MP-insufficient feedback) or the full cross-system QA pass — see Known Limitations above.

---

# Session Update — Phase 1 Movement Refactor (single-lane, no-gravity, feet-root, 2026-06-19)

## Completed & verified

- **Single-lane no-gravity feet-root movement** implemented and play-verified. See "Movement Model — Phase 1" near the top of this file for the full spec.
- New `GroundLane` component (`GroundY`/`MinX`/`MaxX`/`ClampX`, static `Current`, registers `OnEnable` / clears `OnDisable`). One `GroundLane` object added to the `TestCombat` scene (`GroundY=-2.0`, `MinX=-9.5`, `MaxX=10.5`).
- Player (scene) + `Slime.prefab` → **Kinematic, gravityScale 0**. Player root relocated to feet (root.y `-1.5`→`-2.0`, capsule offset.y `0`→`+0.5`, Sprite child localY `-0.09`→`+0.41` to preserve the visual — no visible jump). **Imported sprite pivots not touched.**
- `PlayerCombatController` / `EnemyController` `FixedUpdate` now use `MovePosition`, X-only, Y forced to `GroundLane.GroundY`. Click-to-move ignores clicked Y and clamps X to lane. Enemy patrol clamped to lane bounds. `EnemySpawner` spawns/respawns on `GroundLane.GroundY`.
- Fireball spawn height = `GroundLane.GroundY + projectileHeight` (default `0.6`), horizontal; verified it kills a 1-tile-high Kinematic Slime and that the kill respawns on `GroundY`.

## Verified (play-mode)

Compile clean (0 errors). Player feet rest at `-2.0` (no fall, no visual jump); Y stays locked when displaced; X clamps to lane; Slime on `-2.0` Kinematic with hitbox feet→+1 tile; spawn/respawn on `GroundY`; Fireball spawns at `(-1.4)` and damages/kills Kinematic Slimes (Kinematic trigger confirmed); `GroundLane.Current` set when active, cleared on disable/destroy, re-set on re-enable.

## Scope

- **No changes** to UI/HUD/SkillHotbar, inventory, crafting, talents, save/load, or item/equipment data. Only `GroundLane.cs` (new), `PlayerCombatController.cs`, `EnemyController.cs`, `EnemySpawner.cs`, the Player scene instance, `Slime.prefab` RB, and the `TestCombat` scene `GroundLane` object.
- `Enemy.prefab` (broken stub, unreferenced) and `FireballProjectile.prefab` (already Kinematic) untouched. Layer collision matrix unchanged.

## Known polish notes (not bugs, deferred)

- **Camera follow** may need a dedicated follow-target child: after the player root moved center→feet, a camera that tracks the player transform sits ~0.5 lower.
- **Enemy attack float text** spawns at `playerPos + 0.8` (`EnemyController`); relative to the new feet-root it appears ~0.5 lower — may need an offset tweak.
- Now-unused (left in place, harmless): `TryResolveMoveTarget`, `groundLayerMask`, `maxGroundSearchDistance` on `PlayerCombatController`.

## Not implemented yet

Multi-lane, ladders, portals, jumping, BFS/pathfinding, lane graph. Future interactables should use same-lane walk-to-interact first.

---

# Session Update — Phase 2 WorldInteractable (same-lane walk-to-interact, 2026-06-19)

## Completed & verified

- **Same-lane WorldInteractable system** implemented and play-verified. Full spec in "WorldInteractable — Phase 2" near the top of this file.
- New `_scripts/World/`: `IWorldInteractable`, `WorldInteractable` (abstract base), `CraftingStationInteractable`, `PortalInteractable`.
- `MainHUD.OpenCraftingWindow()` added — public **open-only** entry reusing central window switching (no toggle, keeps button sprites synced). No MainHUD redesign.
- `PlayerCombatController` click handling extended: WorldInteractable checked after enemy / before ground move; `_pendingInteractable` with walk-to-interact, range trigger, and cancel rules (ground click, other interactable, manual attack, destroyed/disabled).
- **Scene placeholders added to `TestCombat`:** `CraftingStation` (x=5, trigger collider, wired to MainHUD) and `Portal` (x=-5, trigger collider, `destinationMapId = grassland_2`). Both collider-only (invisible) for now.

## Verified (play-mode)

Compile clean (0 errors). Walked player 4.0→4.66 (within 0.4 of station x=5) → `Interact` fired once, pending cleared. With Inventory open, station interact **closed Inventory and opened Crafting** via MainHUD; second interact left Crafting **open** (open-only, no toggle); craft button sprite = on-sprite. Portal `Interact` with locked dest → safe no-op; after unlocking `grassland_2` → `CurrentMapId = grassland_2`. Both interactable colliders `isTrigger = true` (no physical block). Enemy melee/combat and lane Y-lock still intact.

## Scope

- **No changes** to UI layouts, inventory/talent/crafting data, save/load, combat damage, Fireball, SkillHotbar, or player/enemy prefab layout. Edited only `MainHUD.cs` (one new public method) + `PlayerCombatController.cs` (click/pending logic) + 4 new World scripts + 2 new scene objects.

## Known next steps

- Add **visuals** for interactables (currently invisible collider-only placeholders).
- Add `NPCDialogueInteractable` once a dialogue system exists.
- Later: support **multi-lane** interactions (walk to a lane connector / ladder / portal first, then to the interactable on the target lane).
