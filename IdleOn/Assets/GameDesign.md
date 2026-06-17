# IdleOn-Inspired Demo

## Implementation Status

| System | Status |
|---|---|
| Combat (click-to-move, auto-combat, enemy AI) | Implemented |
| Loot drop pipeline (WorldDrop, DropManager, LootEvaluator) | Implemented |
| Inventory (slot-based, 20 slots, drag-and-drop UI) | Implemented |
| Equipment (8 slots, stat bonuses, drag-and-drop) | Implemented |
| Currency (Silver, Gold, wallet system) | Implemented |
| Crafting (recipes, ingredient check, crafting window) | Implemented |
| Vault upgrades (Bigger Damage, Monster Tax, Natural Talent) | Implemented |
| Main HUD (HP/MP/XP bars, currency, window buttons) | Implemented |
| Player level-up (XP curve, HUD update, talent point grant) | Implemented |
| Save/Load (Account save: Vault + multi-character, JSON to disk) | Implemented |
| Character Select / Startup Menu (programmer-art) | Implemented |
| Talent system (grid UI, upgrades, skill hotbar assignment) | Implemented |
| Player / Enemy animations (Animator + sprite-swap clips, driver components) | Implemented |
| Player/Enemy physical collision separation (Physics2D layer matrix) | Implemented |
| Click-to-move ground filtering (groundLayerMask) | Implemented |
| Skill casting (Fireball — MP cost, damage, cooldown) | Not implemented |
| Quest system | Not implemented |
| Map system / area progression (3 Grassland maps, ObjectiveHelper, MapWindow) | Implemented |
| Offline progression | Not implemented |
| Settings | Not implemented |

---

## Goal

Build a polished vertical slice inspired by the early-game experience of IdleOn.

Focus on:

* Combat
* Character Progression
* Equipment
* Talents
* Vault Upgrades
* Quests
* Offline Progression

This is not a full IdleOn clone.

---

# Core Gameplay Loop

Kill Monsters

↓

Gain EXP and Coins

↓

Level Up

↓

Gain Talent Points

↓

Upgrade Talents

↓

Get Better Equipment

↓

Become Stronger

↓

Upgrade Vault

↓

Repeat

---

# Maps

## Town

Contains:

* Quest NPC
* Upgrade Vault
* Map Travel Button

## Grassland

Contains:

* Monster Spawner
* Auto Combat
* Loot Drops

The combat map is tile/grid-based visually, similar to IdleOn.

The player and monsters stand on top of floor/platform tiles.

Floor tiles are arranged in a horizontal platform layout.

---

# Combat

## Player Input

### Click to Move

Click priority order per LMB click:

1. UI element → do nothing (EventSystem absorbs the click)
2. Enemy collider hit → attack that enemy (see Click to Attack)
3. Direct hit on a ground collider → move to that X position on the floor
4. Empty space within 2.5 units above a ground collider → move (downward raycast finds the floor surface)
5. Sky, background, or empty space with no ground below within 2.5 units → do nothing

Clicking just above the floor surface works. Clicking the sky or background does nothing.

### Click to Attack

If the player clicks a monster, the player moves to attack range next to that monster, then performs one attack.

### Auto Combat

When auto combat is enabled:

* Player repeatedly finds the nearest monster
* Moves to attack range next to that monster
* Attacks
* Finds the next target after the kill
* Continues indefinitely

### Auto Combat Interrupts

If auto combat is enabled and the player clicks a monster:

* Interrupt the current auto-combat action
* Move to the clicked monster
* Perform one attack
* Resume auto combat

If auto combat is enabled and the player clicks the floor:

* Interrupt the current action
* Move to the clicked floor position
* Resume auto combat

### Drop Pickup

Hold left mouse button over a world drop to collect it.

Drop pickup input takes priority over movement and attack.

## Movement

Use simple direct 2D movement toward the target position.

Use Vector2.MoveTowards — no A* pathfinding required.

## Enemy Behavior

Enemies patrol between two points at their spawn position.

When hit or when the player enters their attack range, enemies enter Combat state and chase the player.

Enemies return to Patrol after a cooldown with no hit received (combatForgetTime).

## Damage Feedback

All damage (player → enemy, enemy → player) shows a floating damage number via FloatTextManager.

Physical damage: orange. Magic damage: blue. Heal: green. Critical: larger text.

## Skills

### Fireball

* Costs MP
* Deals magic damage
* Small area attack

### Arcane Power

* Costs MP
* Increases attack damage
* Duration: 10 seconds

---

# Player Stats

## Primary Stats

### STR

Increases physical damage.

### AGI

Increases accuracy and critical chance.

### WIS

Increases mana and magic damage.

### LUK

Increases drop rate and critical damage.

---

## Secondary Stats

### ATK

Attack Damage (Min ~ Max)

### HP

Health

### MP

Mana

### DEF

Defense

### ACC

Accuracy

### CRIT

Critical Chance

---

# Inventory

Inventory is slot-based. Each slot holds one item type and stacks indefinitely.

Default capacity: 20 slots.

Capacity can be increased by using an Inventory Expansion consumable item.

Inventory data is saved as part of the player save file.

## Inventory UI

Press Tab to open/close the inventory panel.

Displays 20 slots in a 4×5 grid.

Each slot shows:
* Item icon (or grey placeholder if no icon assigned)
* Stack count when quantity > 1

Empty slots show an empty dark frame.

No drag and drop yet. No item interaction yet.

---

# Items

## Equipment

Provides stat bonuses when equipped.

Equipment Slots:

* Hat
* Weapon
* Armor
* Accessory
* Pants
* Shoes
* Ring1
* Ring2

## Consumables

Used from inventory. Effect is applied immediately.

* Health Potion — restores HP
* Mana Potion — restores MP
* Inventory Expansion — adds slots to inventory

## Materials

No direct use. Used in quests and future crafting.

Examples:

* Slime Gel

---

# Currency

Currency is separate from inventory items.

Two currencies:

* Silver Coins — common, dropped by enemies
* Gold Coins — rare, dropped by enemies or quest rewards

Currency is stored as numeric values in player save data, not as inventory items.

Currency drops appear as world drops (same pickup flow as items). Collecting a currency drop delivers it directly to the wallet.

---

# Equipment

Equipment provides stat bonuses when equipped.

Slots: Hat, Weapon, Armor, Accessory, Pants, Shoes, Ring1, Ring2.

---

# Loot

## Drop Pipeline

All loot sources (enemies, chests, trees, fishing spots, quest rewards) use the same pipeline:

1. `LootEvaluator.Evaluate(lootTable, luckMultiplier)` → produces a `LootResult`
2. `DropManager.Instance.Spawn(result, position)` → spawns WorldDrop objects
3. Player holds LMB over drop → `DropManager.Collect()` → item to inventory or currency to wallet

## LootTable

Each source holds a `LootTable` ScriptableObject reference (not an inline list).

LootTables are reusable — multiple enemies can share one table.

Each DropEntry defines:

* Drop type: Item or Currency
* Which item or which currency
* Drop chance (0.0 – 1.0)
* Min and Max quantity

Multiple entries can drop from one kill. Each entry rolls independently.

## WorldDrop

World drops are pooled objects (DropManager owns the pool).

Drops stay in the world indefinitely until collected — no despawn timer.

Currency drops use the same WorldDrop prefab but deliver to wallet on collect.

## Inventory Full

If inventory is full on item collect attempt:

* Drop stays in the world
* OnInventoryFull event fires
* A 0.5s cooldown suppresses repeated attempts

---

# Player Level-Up

## Current Behavior

Killing enemies awards XP. When accumulated XP reaches the threshold for the current level, the player levels up.

- Level display in the HUD updates immediately (e.g. `Hero Lv.1` → `Hero Lv.2`)
- The XP bar resets and refills toward the next level's threshold, carrying over any excess XP from the kill that triggered the level-up
- The player can gain multiple levels from a single kill if XP is large enough
- XP text shows `current / required XP` (e.g. `75/100 XP`)

## XP Curve

| Level | XP to next level |
|---|---|
| 1 | 100 |
| 2 | 120 |
| 3 | 144 |
| 5 | 207 |
| 10 | 516 |

Formula: `floor(100 × 1.2 ^ (level − 1))`

## Talent Points

Each level-up grants talent points:

- Base: **1 point per level**
- With NaturalTalent vault upgrade: **1 + upgrade level** points per level

Example: NaturalTalent at level 2 → 3 points per level-up.

Talent points accumulate and can be spent in the Talent Window (T key or Talent HUD button).

## TODOs

- Add a visual/audio cue on level-up (flash, sound, particle) for game feel
- Wire **SaveToDisk** so level and talent points persist across sessions
- Consider a **level-up popup** showing what was gained (talent points, new stat unlocks)

---

# Map / Area Progression

## Current Behavior

Three Grassland areas provide the core progression ladder for the demo. Each area has a kill objective. Completing it grants Silver and unlocks the next area.

### Area Loop

```
Spawn in Grassland 1
  → Kill Slimes (auto combat)
  → ObjectiveHelper tracks progress live at top of screen
  → After 10 kills: +50 Silver, Grassland 2 unlocked
  → Press M → MapWindow opens → click Travel to Grassland 2
  → After 15 kills: +100 Silver, Grassland 3 unlocked
  → Travel to Grassland 3
  → After 20 kills: +150 Silver, "Demo Complete" message
```

### Maps

| Map | Kill Objective | Silver Reward | Unlocks |
|---|---|---|---|
| Grassland 1 | 10 Slimes | 50 | Grassland 2 |
| Grassland 2 | 15 Slimes | 100 | Grassland 3 |
| Grassland 3 | 20 Slimes | 150 | — (Demo Complete) |

All three maps reuse the same Slime enemy and the same spawner. Travel is data-only — no scene change.

### ObjectiveHelper HUD

A compact always-visible strip anchored to the top-center of the screen (640×58px).

- **Line 1 (bold, white):** `Grassland 1  ·  Kill Slimes:  3 / 10`
- **Line 2 (gold):** `Reward: 50 Silver + Grassland 2 Unlock`

On completion: `✓ Grassland 1 — Complete!` / `Grassland 2 Unlocked!  +50 Silver`

On Grassland 3 completion: `✓ Grassland 3 — All areas cleared!` / `Demo Complete — Thanks for playing!`

### MapWindow

Opened via M key or Map HUD button. Shows one row per area:

- **Current area:** green tint, "Here" button (disabled)
- **Unlocked & not current:** dark tint, "Travel" button enabled
- **Locked:** shows `???`, no button

### Reviewer Impact

A reviewer can complete the full area loop (Grassland 1 → 2 → 3) in about 5 minutes without any explanation, demonstrating: kill-to-reward feedback, progression gating, and clear end state.

## TODOs

- Add a second enemy type for Grassland 2/3 (stronger variant) so each area feels distinct
- Add a visual/audio effect on map completion (flash, fanfare)
- Show silver reward popup on completion rather than just updating the wallet counter
- Allow EnemySpawner to reconfigure enemy type per map (`MapDefinition.EnemyDefinition` field already exists)

---

# Quest System

## Quest 1

Kill 10 Slimes

Rewards:

* Coins
* EXP

## Quest 2

Kill 20 Slimes

Rewards:

* Equipment

---

# Talent System

Players gain Talent Points when leveling up. Open the Talent Window with T or the Talent HUD button.

## Current Behavior

The Talent Window shows a grid of square talent slots (72×72px, 3-column grid). Clicking a slot selects it and shows details in the info panel on the right. The info panel has an Upgrade button that spends one talent point to level up the selected talent (max level 5).

**Normal mode:** click any slot to view and upgrade it.

**Assign mode** (toggle button in title bar): passive talent slots dim and become unclickable. Skill talent slots that are unlocked can be dragged to the 3-slot Skill Hotbar at the bottom of the screen. Locked skill slots are dimmed but not draggable.

## Implemented Talents

| Talent | Effect per Level | Type |
|---|---|---|
| Power Strike | ATK Min +1, ATK Max +2 | Passive |
| Thick Skin | Max HP +5 | Passive |
| Swift Feet | Move Speed +0.1 | Passive |
| Lucky Hunter | Silver drop +5% | Passive |
| Mana Training | Max MP +5 | Passive |
| Fireball Training | Fireball damage +1 | Skill unlock |

## Skill Hotbar

3 slots at the bottom-center of the screen (above the HUD button bar). Drag an unlocked skill talent from the Talent Window in Assign mode to assign it to a slot. Clicking a hotbar slot is a placeholder — skill casting is not yet implemented.

## TODOs (Phase 2B)

- Implement Fireball casting: MP cost, cooldown timer, projectile/area damage
- Add MP spending and `OnPlayerMPChanged` event to drain and refill the MP bar
- Add Arcane Power skill
- Assign icon sprites to TalentDefinition and SkillDefinition ScriptableObject assets

## Mage Talents I (planned)

* Fireball Damage ✓ (implemented as Fireball Training)
* Magic Damage

## Mage Talents II (planned)

* Fireball Cooldown
* Mana Regeneration

---

# Upgrade Vault

Account-wide permanent upgrades.

Uses Coins.

Available Upgrades:

### Bigger Damage

Permanent Damage Increase

### Monster Tax

Permanent Coin Gain Increase

### Natural Talent

Permanent Talent Point Bonus

---

# Offline Progression

When the player exits while auto-combat is active:

Save:

* Logout Time
* Current Zone

Next login:

Calculate:

* EXP Earned
* Coins Earned
* Materials Earned

Display rewards in a popup.

---

# Save System

## Account Save Architecture

`AccountSaveData` is the top-level save file — one file per account, holding account-shared data and all characters as siblings:

* `Version` (int)
* `Vault` (VaultSaveData) — account-shared, used by all characters
* `Players` (List<PlayerSaveData>) — one entry per character
* `CurrentPlayerId` (string)

Vault upgrades are shared account-wide; per-character progress is isolated.

## What is Saved per Character (PlayerSaveData)

* PlayerId, PlayerName
* Level, EXP, Talent Points
* Silver Coins, Gold Coins
* Inventory (slot list)
* Equipped items
* Talent Levels
* Hotbar skill assignments
* Map Progress / Current Map Id
* Last Logout Time

Vault Levels are **not** per-character — they live on `AccountSaveData.Vault` and apply to every character on the account.

Quest Progress is not yet implemented (no Quest system).

## Implementation Notes

* `SaveManager` singleton with `DontDestroyOnLoad`
* JSON serialization via `JsonUtility`
* Save file at `Application.persistentDataPath/account_save.json` (old `player_save.json` ignored, no migration)
* `SaveManager` exposes `CurrentAccount`, `CurrentSave` (selected character), `CurrentVault` (shared vault)
* Account/character flow: create or load account → create or select character → `OnSaveLoaded` fires → systems initialize
* Save-on-quit via `OnApplicationQuit` / `OnApplicationPause`
* Systems initialize only after `SaveManager.OnSaveLoaded` fires

## Startup / Character Select UI

Programmer-art `StartupMenu` freezes gameplay until a character is selected.

* Main Menu: New Save, Load Save (disabled without an existing save file)
* Character Select: lists existing characters, Create New Character (auto-named Hero 1 / Hero 2 / ...)
* Not yet implemented: character deletion, rename/text input, multiple account save slots, UI polish

---

# Crafting

## Recipes

Recipes are ScriptableObjects (`CraftRecipeDefinition`) stored in a `CraftingDatabase` assigned to `GameDatabase`.

Each recipe defines:
* Result item and quantity
* List of required ingredients (ItemDefinition + quantity)
* Optional required level (not enforced yet)

## Craft Logic

1. Check all ingredient quantities in inventory.
2. Attempt to add result to inventory first — if full, abort without consuming anything.
3. Remove ingredients from inventory.

## Current Recipes

* Slime Armor — 5× Slime Gel → Armor
* Basic Hat — 3× Slime Gel → Hat

---

# UI

## Main HUD

Always-visible HUD anchored to the bottom of the screen.

**Character panel** (bottom-left):
* Name placeholder ("Hero") + Level
* HP bar — live via `OnPlayerHPChanged`
* MP bar — shows MaxMP/MaxMP (placeholder until current-MP tracking exists)
* XP bar — fills toward `Level × xpPerLevel` via `OnPlayerExpGained`
* Silver and Gold coin display — live via `OnCurrencyChanged`

**Button bar** (bottom strip):
* Auto Combat toggle — calls `PlayerCombatController.SetAutoCombat()`
* Inv — opens/closes Inventory window
* Craft — opens/closes Crafting window
* Vault — opens/closes Vault window
* Talent / Quest / Map / Settings — placeholder buttons (log only)

## Window Flow

All windows expose `Open()`, `Close()`, `Toggle()`. MainHUD buttons call `Toggle()`.

Multiple windows can be open simultaneously. No window manager.

Temporary debug keys remain active:
* **Tab** — Inventory window
* **C** — Crafting window
* **V** — Vault window

## Windows

| Window | Status | Open via |
|---|---|---|
| Inventory + Equipment | Implemented | Tab key or Inv button |
| Crafting | Implemented | C key or Craft button |
| Vault | Implemented | V key or Vault button |
| Talents | Implemented | T key or Talent button |
| Quests | Not implemented | Quest button (logs placeholder) |
| Map | Implemented | M key or Map button |
| Offline Rewards popup | Not implemented | — |

---

# Recommended Next Features

Priority order based on completeness of the core loop:

1. **Save/Load Trigger** — ✅ Done. Account-based save/load with character select is implemented (see Save System above).

2. **Skill Casting (Phase 2B)** — Fireball is assigned to the hotbar but does nothing when clicked. Implement: MP spending, `OnPlayerMPChanged` event, cooldown timer on `SkillSlotUI`, projectile or area damage via `FloatTextManager`. Arcane Power can follow the same pattern. **This is the current next task.**

3. **Quest System** — add `QuestSystem`, `QuestDefinition` ScriptableObjects (Kill 10 Slimes, Kill 20 Slimes), and `QuestWindow` UI with progress tracking.

4. **Offline Progression** — save logout time on quit. On next load, calculate EXP/coins/materials earned while away and display a popup.

5. **Talent / Skill Icons** — assign `Icon` sprites to all six `TalentDefinition` assets and `SkillDef_Fireball` in the Inspector. The slot grid already renders them; they just show grey until sprites are set.

6. **Player / Enemy Animations** — ✅ Done (see Session Update 2026-06-16 below).

---

# Session Update — 2026-06-16

## Completed this session

- **Player / Enemy animations** — Animator + AnimationClips that animate `SpriteRenderer.sprite`.
  - Player: Idle / Run / Attack. Slime: Idle / Move / Attack / Dead.
  - `PlayerAnimatorDriver` (reads `PlayerCombatController.State` + position delta) and `EnemyAnimatorDriver` (reads `EnemyController.State` + position delta) drive the Animator and **own visual facing**.
- **Facing bug fixed** — current player/slime art faces **left** by default. Drivers added an inspector `invertFacing` bool with XOR flip logic. Player scene instance and Slime prefab use `invertFacing = true`. `EnemyController` no longer flips the sprite.
- **Slime death animation** — visible because `EnemyController` delays the pooled `SetActive(false)` briefly (`deathDisableDelay`, ~0.5s).
- **Dynamic Rigidbody2D** — Player and Slime switched to Dynamic (for future vertical platforms / gravity / stairs). Freeze Rotation Z stays on. Movement still uses simple `transform.position` for now (temporary).
- **Kill jitter — fixed** — root cause was Player↔Enemy physical collision while both are Dynamic.
  - `PlayerCombatController.IsValidTarget()` rejects null / inactive / not-alive / `EnemyState.Dead` targets and reacquires a live one.
  - `EnemyController` freezes `Rigidbody2D.simulated=false` during the death delay, restored on pooled respawn.
  - **Physics2D layer separation** (the real fix): Player and Enemy on dedicated layers with collision disabled (see CLAUDE.md "Physics Layers & Collision Matrix").

## Implementation status (delta)
- Player/Enemy animations: **implemented**
- Talent grid + skill hotbar assignment: implemented
- Skill casting: **not implemented**
- Click-to-move ground filtering: implemented (layer collision now configured)
- Player/Enemy physical collision separation: **implemented** (Physics2D layers)
- Save/Load trigger: later / final
- Quest system: later
- Offline progression: later

## Next Task (superseded — see below)

**Move Dynamic Rigidbody2D movement off direct `transform.position`** — convert `PlayerCombatController` and `EnemyController` movement to `Rigidbody2D.MovePosition` (or velocity) in `FixedUpdate`. Direct transform writes on a Dynamic body fight the physics solver; the layer-collision fix removes the player↔enemy jitter, but this is the proper long-term fix. **Do not refactor movement unless explicitly requested.**

> Note: the previously-planned "Fix Player/Enemy physical collision via Physics2D layers" was **completed this session**. The checklist below is kept as its regression test.

### Regression / verification checklist (collision separation)
- [ ] Player stands on ground.
- [ ] Slime stands on ground.
- [ ] Player and slime do not physically push each other.
- [ ] Enemy click still attacks (`Physics2D.OverlapPointAll` still detects enemies).
- [ ] Ground click still moves.
- [ ] Killing a slime does not cause stuck / flipping jitter.
- [ ] Slime respawns and moves normally.
- [ ] Console has no errors.

---

# Session Update — Account Save + Character Select (2026-06-16)

## Completed this session

- **Account Save** — `AccountSaveData` is now the top-level save file: `Version`, `Vault` (shared), `Players` (per-character list), `CurrentPlayerId`.
- **PlayerSaveData** — `VaultData` removed (vault is account-shared now); `PlayerId` and `PlayerName` added.
- **SaveManager** — account-based: `CurrentAccount`, `CurrentSave` (selected character), `CurrentVault`. File renamed to `account_save.json`; old `player_save.json` ignored.
- **VaultSystem** — reads `CurrentVault` instead of `CurrentSave.VaultData`. Verified shared across characters (Hero 2 sees Hero 1's vault upgrades).
- **StartupMenu** — programmer-art Main Menu (New Save / Load Save) + Character Select (existing characters + Create New Character, auto-named Hero N). Gameplay frozen until selection.

## Verified

Fresh Play → Main Menu; New Save creates `account_save.json`; create/select character enters gameplay; restart enables Load Save; load restores characters as separate `PlayerSaveData` entries; Hero 1 progress persists and is not overwritten by Hero 2; vault upgrades shared; no console errors.

## Known limitations

No character deletion, no rename/text input, no multiple account save slots, no offline progression, no cloud save, no startup UI polish, minimal save migration (old `player_save.json` ignored).

## Next Task

**Implement minimal Fireball skill casting from assigned hotbar slot.**

- Click hotbar Fireball slot to cast.
- Require Fireball assigned through Talent assign mode.
- Consume MP if current MP exists, or implement minimal current MP if needed.
- Apply cooldown.
- Damage current/nearest valid enemy.
- Use `TalentSystem.GetFireballDamageBonus()`.
- No projectile art/VFX yet unless trivial.
- Do not implement complex projectile collisions unless explicitly approved.
