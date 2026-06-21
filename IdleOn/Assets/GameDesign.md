# IdleOn-Inspired Demo

## Frozen Vertical Slice (2026-06-20)

The demo scope is frozen. The Q1–Q12 tutorial vertical slice is complete; subsequent work is polish, bug fixing, QA, and documentation unless a scope change is explicitly approved. Map4, Map5, and the boss are not part of the current demo.

The current tutorial alternates Chief dialogue quests (Q5/Q7/Q10/Q12) with action quests (Q6/Q8/Q9/Q11). During action quests, the Chief gives hints only; those dialogue IDs are not quest objective targets. Q12 ends the tutorial, after which the Chief plays random daily dialogue without advancing quests.

The always-visible top-right Quest Window shows the active quest title and live objective progress, restores after save/load, and displays `Tutorial Complete` / `Keep exploring.` after Q12. The old `ObjectiveHelper` top-map-objective UI is disabled.

Tutorial persistence includes active quest, completed quests, active objective counts, unlocked feature flags, global enemy kill counts, and current map ID. Old saves normalize missing fields, imports do not replay quest rewards, and `EnemyKillTracker` uses saved per-character counts.

Normal flow is **two Unity scenes**: `BootScene` (entry/menu, build index 0) and `TestCombat` (the only gameplay scene, build index 1). `TestCombat` is a single gameplay scene with four map roots: grassland_1, grassland_2, town, and grassland_3. Portals are travel-only (`destinationMapId`), while destination `MapDefinition` assets own unlock requirements: none, q1, q3, and q5 respectively. Craft unlocks at Q7; Talents and AutoCombat at Q10; Vault and Map at Q12. AutoCombat starts off and is hidden before Q10.

The final release gate is one uninterrupted human Q1–Q12 playthrough with real save/quit/reload checkpoints. `.claude/HANDOFF_CURRENT_STATE.md` is the authoritative test route and current debt list.

Implementation paths, field tables, component names, and code-level detail live in `CLAUDE.md`, not here. This document describes design intent and player-observable behavior only.

---

### Current movement and loot polish

Player Run and Slime Move animation state is stable during FixedUpdate-driven movement. Player and enemy roots remain feet-roots fixed to the lane; a presentation-only frame alignment pass adjusts only the Sprite visual child so differently cropped center-pivot frames remain grounded.

World drops have no gravity and spawn directly on the active ground lane. One entry appears at the source X. Multiple entries preserve loot order and are arranged symmetrically around that X at 0.4-unit spacing; near a lane edge, the whole group shifts inward together so spacing is retained. Drop artwork is normalized to a visible width of `0.32` world units without changing the root, collider, pooling, collection, or fly-to-HUD behavior.

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

## Implementation Status

| System | Status |
|---|---|
| Combat (click-to-move, auto-combat, enemy AI) | Implemented |
| Loot drop pipeline | Implemented |
| Inventory (fixed-slot, 20 slots/page, pagination, drag-and-drop UI) | Implemented |
| Equipment (8 slots, stat bonuses, drag-and-drop) | Implemented |
| Currency (Silver, Gold, wallet system) | Implemented |
| Crafting (recipes, ingredient check, crafting window) | Implemented |
| Vault upgrades (Bigger Damage, Monster Tax, Natural Talent) | Implemented |
| Main HUD (HP/MP/XP bars, currency, window buttons) | Implemented |
| Player level-up (XP curve, HUD update, talent point grant) | Implemented |
| Save/Load (Account save: Vault + multi-character, JSON to disk) | Implemented |
| Character Select / Startup Menu | Implemented |
| Talent system (grid UI, upgrades, skill hotbar assignment) | Implemented |
| Player / Enemy animations | Implemented |
| Click-to-move on a single ground lane | Implemented |
| Same-lane walk-to-interact (crafting stations, portals, NPC dialogue) | Implemented |
| Skill casting (Fireball — MP cost, damage, cooldown) | Implemented |
| Quest system (Q1–Q12 linear chain, gates HUD features) | Implemented and persisted |
| Map system / area progression (multi-map single-scene, destination-MapDef-gated portals) | Implemented |
| Offline progression | Not implemented |
| Settings | Not implemented |

**Bugfix pass (2026-06-20):** Grassland3 enemies respawn locally for grinding; enemy/slime movement speed reduced 60%; portal travel spawns the player near the destination map's portal back to the source map when one exists, falling back to default spawn otherwise. See `.claude/HANDOFF_CURRENT_STATE.md` for detail. Next work is polish/regression/full Q1–Q12 manual run only, unless redirected. Do not implement Map4/Map5/boss yet.

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

The player and monsters stand on top of floor/platform tiles arranged in a horizontal layout.

---

# Combat

## Player Input

### Click to Move

The player moves along a single ground lane. Clicking anywhere walks the player to that X position within the lane bounds — the clicked Y is ignored, and there is no vertical movement or jump on a single lane.

Click priority order per LMB click:

1. UI element → do nothing
2. World drop (held LMB) → collect it
3. Enemy collider hit → attack that enemy
4. Interactable (crafting station, portal, NPC) → walk to it, then activate
5. Otherwise → move to the clicked X on the current lane

### Click to Attack (persistent target)

If the player clicks a monster, the player moves to attack range next to it, then keeps attacking on the normal attack cooldown until it dies. Manual clicks and Auto Combat share the same persistent target/attack loop.

* Clicking the **same** monster again while already moving to/attacking it is a no-op.
* Clicking a **different** monster switches target; the attack cooldown carries over unchanged (no free attack from switching).
* When the target dies: Auto Combat off → idle; Auto Combat on → search for another target.

### Auto Combat

When auto combat is enabled:

* Player repeatedly finds the nearest monster
* Moves to attack range next to that monster
* Attacks on the normal cooldown until it dies
* Finds the next target after the kill
* Continues indefinitely; idles if no valid target exists

### Auto Combat Interrupts

If auto combat is enabled and the player clicks a monster, the player switches to that monster, kills it, then resumes auto search.

If auto combat is enabled and the player clicks the floor, the current target is cancelled, the player moves to the clicked position, then resumes auto search on arrival.

### Drop Pickup

Hold left mouse button over a world drop to collect it. Drop pickup input takes priority over movement and attack.

### Interactables (walk-to-interact)

Same-lane interactable objects: crafting stations, portals, and NPCs. Click one → the player walks to it along the current lane → it activates when the player is close enough.

* Clicking the ground cancels a pending interaction; clicking a different interactable replaces it; starting a manual attack on an enemy cancels it; an interactable removed before arrival cancels safely.
* Interactable objects never physically block the player, enemies, projectiles, drops, or movement.
* **Crafting station:** walk up → opens the Crafting window through the HUD (open-only; stays open if Crafting is already open).
* **Portal:** walk up → travels to its destination map if valid/unlocked; otherwise nothing happens.
* **NPC:** walk up → starts dialogue.

**Not implemented yet:** multi-lane interactions, ladders/portals as lane connections, pathfinding between platforms.

## Movement

The player and enemies move along a single ground lane — no gravity, no jumping. Click-to-move ignores the clicked Y and clamps the clicked X to the lane bounds. Enemies patrol and chase within the same lane bounds. Projectiles (Fireball) travel horizontally on the lane.

Run/Move animation follows the character controller's actual horizontal movement intent, so rendering frames that fall between physics ticks do not briefly return to Idle. Stopping or entering attack range clears movement intent and returns to the appropriate Idle/Attack animation.

Player and enemy roots remain feet-roots fixed to the lane. A presentation-only frame alignment pass adjusts only the Sprite visual child after each sprite-frame update, keeping differently cropped center-pivot frames grounded without moving gameplay roots or colliders.

**Not implemented yet:** multiple platforms/lanes, ladders, portals, jumping, pathfinding. Future cross-platform travel uses explicit ladder/portal transitions (a same-lane walk-to-interact step first), never physics jumping.

## Enemy Behavior

Enemies patrol between two points at their spawn position.

When hit or when the player enters their attack range, enemies enter Combat state and chase the player.

Enemies return to Patrol after a cooldown with no hit received.

Demo slimes patrol/chase at a reduced speed (60% slower than the original tuning). Player movement speed, attack cooldown, damage, HP, and rewards are unchanged.

Grassland3's pre-placed slimes respawn a few seconds after death, for demo grinding (q6: kill 5 slimes + collect 5 slime essence). Grassland2's single tutorial slime does not respawn — kill once, stays dead, no infinite grind there.

## Damage Feedback

All damage (player → enemy, enemy → player) shows a floating damage number.

Physical damage: orange. Magic damage: blue. Heal: green. Critical: larger text.

Taking non-fatal damage plays a Hurt animation on the player or the slime. A fatal hit always plays the Death animation instead — Death never gets interrupted or overridden by a Hurt trigger.

## Death and Respawn

When the player dies: movement and combat input stop, the Death animation plays, the player respawns at Town's default spawn point (not at a portal/back-spawn point), HP is restored, and normal control resumes.

When a slime dies: the Death animation plays before it disappears, then any loot drops once. Grassland 3's slimes respawn after a short delay for grinding; Grassland 2's tutorial slime does not respawn.

## Skills

### Fireball

* Costs MP
* Deals magic damage
* Small area attack
* Implemented: click hotbar slot to cast, costs MP, deals damage to current/nearest valid enemy, enters cooldown.
* Still missing (polish, not core logic): final icon asset, a visual/audio effect on cast or hit, and explicit feedback when MP is insufficient.

### Arcane Power (planned, not implemented)

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

Inventory is fixed-slot: each slot is a real, stable position, including empty ones. Same item stacks into its existing slot indefinitely; new items take the first empty slot. Removing/equipping an item clears that slot in place — later items never shift to fill the gap.

Default capacity: 20 slots.

Capacity can be increased by using an Inventory Expansion consumable item, or by leveling the Inventory Expansion talent (+20 slots per level, max level 5). Both sources only append empty slots — existing items keep their slot positions. Total capacity = base capacity (consumable-driven, persisted) + talent bonus (computed live from talent level).

Inventory data is saved as part of the player save file. Old saves load safely — existing items keep their slot order, then empty slots are appended up to capacity; no items are lost or reordered.

## Inventory UI

Press Tab to open/close the inventory panel.

Pagination: 20 fixed slots per page. Paging never moves data, only changes which 20-slot window is displayed.

- 1 page total → page buttons hidden.
- First page (more pages exist) → Next button only.
- Middle page → Prev and Next both shown.
- Last page → Prev button only.

Each slot shows:
* Item icon (or grey placeholder if no icon assigned)
* Stack count when quantity > 1

Empty slots (including empty slots within capacity, and slots beyond capacity) show an empty dark frame.

Drag-and-drop is implemented for equipping/unequipping. No drag-and-drop slot-to-slot item move yet.

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

No direct use. Used in quests and crafting.

Examples:

* Slime Gel
* Slime Essence

---

# Currency

Currency is separate from inventory items.

Two currencies:

* Silver Coins — common, dropped by enemies
* Gold Coins — rare, dropped by enemies or quest rewards

Currency is stored as numeric values in player save data, not as inventory items.

Currency drops appear as world drops (same pickup flow as items). Collecting a currency drop delivers it directly to the wallet.

---

# Loot

All loot sources (enemies, chests, trees) use the same pipeline: evaluate the source's loot table → spawn world drops → player collects (item to inventory, or currency to wallet).

Loot tables are reusable — multiple enemies can share one table. Each entry defines drop type (item or currency), which item/currency, drop chance, and min/max quantity. Multiple entries can drop from one kill, evaluated independently.

World drops are pooled and stay in the world indefinitely until collected — no despawn timer.

World drops have no gravity and spawn directly on the active ground lane. One entry appears at the source X. Multiple entries preserve loot order and are arranged symmetrically around that X at 0.4-unit spacing; near a lane edge, the whole group shifts inward together so spacing is retained.

Drop artwork is normalized to a visible width of 32 pixels at the map's 100 pixels/unit reference (`0.32` world units). Only the Sprite visual child is scaled; the WorldDrop root, collider, pooling, collection, and fly-to-HUD behavior are unchanged.

If inventory is full on item collect attempt: the drop stays in the world, an inventory-full notification fires, and a short cooldown suppresses repeated attempts.

---

# Player Level-Up

Killing enemies awards XP. When accumulated XP reaches the threshold for the current level, the player levels up.

- Level display in the HUD updates immediately
- The XP bar resets and refills toward the next level's threshold, carrying over any excess XP from the kill that triggered the level-up
- The player can gain multiple levels from a single kill if XP is large enough
- XP text shows `current / required XP`

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

---

# Map / Area Progression

Four map roots in one scene: Grassland 1, Grassland 2, Town, Grassland 3. Travel between them is data-only — same scene, no scene loads.

Each portal is travel-only; whether it's usable depends entirely on the destination map's own unlock requirement (a quest, an enemy kill count, or none). A locked portal is dimmed; an unlocked one is fully visible.

Traveling through a portal back toward a map you came from spawns you near the portal that leads back, instead of always at that map's default spawn point.

The Map Window (M key or Map HUD button) shows each map point: current, unlocked, or locked, with travel by clicking an unlocked point.

See `.claude/HANDOFF_CURRENT_STATE.md` for the exact per-map unlock table.

---

# Quest System

A linear Q1–Q12 tutorial chain — one active quest at a time, no branching or concurrent quests. See `.claude/HANDOFF_CURRENT_STATE.md` for the exact objective/reward table and manual test route.

Quest completion can award Exp and/or unlock a HUD feature (Craft/Talents/AutoCombat/Vault/Map). Inventory is never gated.

Map/portal unlocks are driven by the destination map's own requirements (quest id and/or enemy kill count), not by the quest system directly.

---

# Talent System

Players gain Talent Points when leveling up. Open the Talent Window with T or the Talent HUD button.

The Talent Window shows a grid of square talent slots. Clicking a slot selects it and shows details in the info panel on the right. The info panel has an Upgrade button that spends one talent point to level up the selected talent (max level 5).

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
| Inventory Expansion | Inventory capacity +20 slots | Passive |

## Skill Hotbar

3 slots at the bottom-center of the screen. Drag an unlocked skill talent from the Talent Window in Assign mode to assign it to a slot.

Each slot shows the skill's icon when assigned, a radial cooldown fill while on cooldown, and a tooltip on hover (name, description, MP cost, cooldown, required talent level).

Clicking a slot casts the assigned skill — costs MP, applies damage, starts the cooldown.

## Planned (not implemented)

* Arcane Power skill
* Magic Damage talent
* Fireball Cooldown talent
* Mana Regeneration talent

---

# Upgrade Vault

Account-wide permanent upgrades. Uses Coins.

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

Not implemented.

---

# Save System

`AccountSaveData` is the top-level save file — one file per account, holding account-shared data and all characters as siblings:

* `Version`
* `Vault` — account-shared, used by all characters
* `Players` — one entry per character
* `CurrentPlayerId`

Vault upgrades are shared account-wide; per-character progress (level, EXP, talent points, coins, inventory, equipment, talent levels, hotbar, map progress, quest progress, last logout time) is isolated per character.

Quest progress is implemented and persisted for the frozen Q1–Q12 tutorial.

Implementation detail (serialization format, save file path, init lifecycle) lives in `CLAUDE.md`.

## BootScene / StartMenu UI

* **New Save** → opens Character Select with an empty list.
* **Load Save** → opens Character Select with existing characters; a zero-character account is valid.
* **Create New Character** → adds a character row, stays on Character Select.
* **Character row's Select button** is the only way to choose a character and enter the gameplay scene — no separate Continue step.

Not yet implemented: character deletion, rename/text input, multiple account save slots, UI polish.

---

# Crafting

## Recipes

Each recipe defines a result item and quantity, a list of required ingredients, and an optional required level (not enforced yet).

## Craft Logic

1. Check all ingredient quantities in inventory.
2. Attempt to add result to inventory first — if full, abort without consuming anything.
3. Remove ingredients from inventory.

## Current Recipes

* Slime Sword — Slime Essence → Weapon
* Slime Armor — Slime Gel → Armor
* Basic Hat — Slime Gel → Hat

## Crafting Window

Left side: result item icon/name/description, the materials list, and a Craft button. Right side: the full recipe list, craftable recipes shown before uncraftable ones.

* No recipe selected: item info and materials are empty, Craft button is disabled.
* Recipe selected: shows the result item's icon/name/description, lists each required material with owned/required count, colored green when you have enough and red when you don't. Craft button is enabled only when every material requirement is met.
* Each recipe row in the list visually distinguishes craftable from uncraftable.
* Crafting (or any inventory change while the window is open) refreshes everything live.

---

# UI

## Main HUD

Always-visible HUD anchored to the bottom of the screen.

**Character panel** (bottom-left):
* Name (static text — player naming isn't implemented yet)
* Level
* HP bar, MP bar, XP bar — all live
* Silver and Gold coin display — live

**Button bar** (bottom strip):
* Auto Combat toggle
* Inv — opens/closes Inventory window
* Craft — opens/closes Crafting window
* Vault — opens/closes Vault window
* Talent — opens/closes Talent window
* Map — opens/closes Map window
* Quest / Settings — placeholder buttons, not yet implemented (the Quest Window itself is separate and already implemented)

Each window-toggle button's state mirrors whether its window is currently open: closed = clickable, open/current = greyed out.

## Window Flow

The HUD enforces one-window-at-a-time for the windows it manages (Inv/Craft/Vault/Talent/Map): clicking a different window's button closes whatever is open and opens the new one; clicking the currently-open window's button closes it.

Temporary debug keys remain active: Tab (Inventory), C (Crafting), V (Vault), T (Talent), M (Map).

## Windows

| Window | Status | Open via |
|---|---|---|
| Inventory + Equipment | Implemented | Tab key or Inv button |
| Crafting | Implemented | C key or Craft button |
| Vault | Implemented | V key or Vault button |
| Talents | Implemented | T key or Talent button |
| Quests (always-visible Quest Window) | Implemented | always visible |
| Map | Implemented | M key or Map button |
| Offline Rewards popup | Not implemented | — |
| Settings | Not implemented | Settings button (logs placeholder) |
