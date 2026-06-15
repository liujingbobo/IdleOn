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
| Save/Load (JSON to disk) | Scaffolded — not yet triggered automatically |
| Talent system | Not implemented |
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

If the player clicks a floor tile, the player moves to that position.

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

Talent points accumulate but cannot be spent yet — TalentSystem is not implemented.

## TODOs

- Implement **TalentSystem** and **TalentWindow** to let the player spend accumulated talent points
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

Players gain Talent Points when leveling up.

## Basic Talents

* Max HP
* Max MP
* Defense
* Move Speed
* Wisdom
* AFK Gains

## Mage Talents I

* Fireball Damage
* Magic Damage

## Mage Talents II

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

## What is Saved

* Level
* EXP
* Silver Coins
* Gold Coins
* Inventory (slot list)
* Equipped items (slot name → item id pairs)
* Talent Levels
* Vault Levels
* Quest Progress
* Last Logout Time
* Current Map Id

## Implementation Notes

* `SaveManager` singleton with `DontDestroyOnLoad`
* JSON serialization via `JsonUtility`
* Save file at `Application.persistentDataPath/player_save.json`
* Each Play session starts with `CreateNewSave()` (fresh in-memory data)
* `SaveToDisk()` and `LoadFromDisk()` are implemented but not yet auto-called
* Systems initialize only after `SaveManager.OnSaveLoaded` fires

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
| Talents | Not implemented | Talent button (logs placeholder) |
| Quests | Not implemented | Quest button (logs placeholder) |
| Map | Implemented | M key or Map button |
| Offline Rewards popup | Not implemented | — |

---

# Recommended Next Features

Priority order based on completeness of the core loop:

1. **Save/Load Trigger** — call `SaveToDisk()` on application quit. Call `LoadFromDisk()` at startup if a save file exists (add a "Continue" vs "New Game" choice). Without this, level, talent points, and map progress reset every session.

2. **Talent System** — add `TalentSystem`, `TalentDefinition` ScriptableObjects, and `TalentWindow` UI. Talent points already accumulate in `PlayerSaveData.TalentPoints` — the system just needs to read and spend from that pool.

3. **Quest System** — add `QuestSystem`, `QuestDefinition` ScriptableObjects (Kill 10 Slimes, Kill 20 Slimes), and `QuestWindow` UI with progress tracking.

4. **Skills (Fireball, Arcane Power)** — add MP spending, skill cooldowns, and skill buttons to MainHUD. Requires current-MP tracking and `OnPlayerMPChanged` event.

5. **Offline Progression** — save logout time on quit. On next load, calculate EXP/coins/materials earned while away and display a popup.
