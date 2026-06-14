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
| Save/Load (JSON to disk) | Scaffolded — not yet triggered automatically |
| Talent system | Not implemented |
| Quest system | Not implemented |
| Map system / travel | Not implemented |
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
| Map | Not implemented | Map button (logs placeholder) |
| Offline Rewards popup | Not implemented | — |

---

# Recommended Next Features

Priority order based on completeness of the core loop:

1. **Player Level-Up** — add level-up logic to `PlayerProgression`. Fire an `OnLevelChanged` event. Wire to HUD level display and XP bar cap. Grant talent points on level-up (feeds NaturalTalent vault bonus).

2. **Save/Load Trigger** — call `SaveToDisk()` on a zone transition or on application quit. Call `LoadFromDisk()` at startup if a save file exists (add a "Continue" vs "New Game" choice).

3. **Talent System** — add `TalentSystem`, `TalentDefinition` ScriptableObjects, and `TalentWindow` UI. Wire `GetTalentPointBonus()` from VaultSystem to the talent point pool.

4. **Quest System** — add `QuestSystem`, `QuestDefinition` ScriptableObjects (Kill 10 Slimes, Kill 20 Slimes), and `QuestWindow` UI with progress tracking.

5. **Skills (Fireball, Arcane Power)** — add MP spending, skill cooldowns, and skill buttons to MainHUD. Requires current-MP tracking and `OnPlayerMPChanged` event.

6. **Offline Progression** — save logout time on quit. On next load, calculate EXP/coins/materials earned while away and display a popup.
