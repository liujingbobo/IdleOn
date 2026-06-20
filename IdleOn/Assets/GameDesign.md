# IdleOn-Inspired Demo

## Frozen Vertical Slice (2026-06-20)

The demo scope is frozen. The Q1–Q12 tutorial vertical slice is complete; subsequent work is polish, bug fixing, QA, and documentation unless a scope change is explicitly approved. Map4, Map5, and the boss are not part of the current demo.

The current tutorial alternates Chief dialogue quests (Q5/Q7/Q10/Q12) with action quests (Q6/Q8/Q9/Q11). During action quests, the Chief gives hints only; those dialogue IDs are not quest objective targets. Q12 ends the tutorial, after which the Chief plays random daily dialogue without advancing quests.

The always-visible top-right Quest Window shows the active quest title and live objective progress, restores after save/load, and displays `Tutorial Complete` / `Keep exploring.` after Q12. The old `ObjectiveHelper` top-map-objective UI is disabled.

Tutorial persistence includes active quest, completed quests, active objective counts, unlocked feature flags, global enemy kill counts, and current map ID. Old saves normalize missing fields, imports do not replay quest rewards, and `EnemyKillTracker` uses saved per-character counts.

Normal flow is **two Unity scenes**: `BootScene` (entry/menu, build index 0) and `TestCombat` (the only gameplay scene, build index 1). `TestCombat` is a single gameplay scene with four map roots: grassland_1, grassland_2, town, and grassland_3. Portals are travel-only (`destinationMapId`), while destination `MapDefinition` assets own unlock requirements: none, q1, q3, and q5 respectively. Craft unlocks at Q7; Talents and AutoCombat at Q10; Vault and Map at Q12. AutoCombat starts off and is hidden before Q10.

The final release gate is one uninterrupted human Q1–Q12 playthrough with real save/quit/reload checkpoints. `.claude/HANDOFF_CURRENT_STATE.md` is the authoritative test route. Older design sketches below are retained only as history where explicitly marked.

## Implementation Status

| System | Status |
|---|---|
| Combat (click-to-move, auto-combat, enemy AI) | Implemented |
| Loot drop pipeline (WorldDrop, DropManager, LootEvaluator) | Implemented |
| Inventory (fixed-slot, 20 slots/page, pagination, drag-and-drop UI) | Implemented |
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
| Skill casting (Fireball — MP cost, damage, cooldown) | Implemented |
| Quest system (Q1–Q12 linear chain, gates HUD features) | Implemented and persisted |
| Map system / area progression (multi-map single-scene, destination-MapDef-gated portals) | Implemented — superseded the old 3-Grassland/ObjectiveHelper/silver-reward model below, see "Quest/Portal/Map Architecture (2026-06-19)" |
| Offline progression | Not implemented |
| Settings | Not implemented |

**Bugfix pass complete (2026-06-20):** (A) Grassland3 enemies now respawn locally (demo-only respawn, not the general spawner architecture) — q6 grinding works; (B) enemy/slime movement speed reduced 60% (`patrolSpeed` 1.5→0.6 on `Slime.prefab`); (C) portal travel now spawns the player near the destination map's portal back to the source map when one exists, falling back to default spawn on fresh load/teleport/debug/no-match (`PortalInteractable` stays travel-only). See "Map / Area Progression" and "Enemy Behavior" below for detail. Next work is polish/regression/full Q1–Q12 manual run only, unless redirected. Do not implement Map4/Map5/boss yet.

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

> **Updated 2026-06-19 (Phase 1 movement refactor).** Movement is now single-lane, no-gravity, feet-root. Clicking resolves to an X on the current lane only — the clicked Y is ignored.

Click priority order per LMB click:

1. UI element → do nothing (EventSystem absorbs the click)
2. Enemy collider hit → attack that enemy (see Click to Attack)
3. Otherwise → move to the clicked X on the current lane. The clicked Y is ignored; X is clamped to the lane's `MinX`/`MaxX`. The player walks along the lane at `GroundLane.GroundY`.

There is no vertical movement on a single lane (no jump). Clicking anywhere horizontally walks the player to that X within the lane bounds.

### Click to Attack (updated 2026-06-20 — persistent target)

If the player clicks a monster, the player moves to attack range next to that monster, then **keeps attacking it on the normal attack cooldown until it dies** (no longer a single attack-and-stop). Manual clicks and Auto Combat share the same persistent target/attack loop.

* Clicking the **same** monster again while already moving to/attacking it is a no-op — it does not restart movement, reset the attack cooldown, or force an extra attack.
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

If auto combat is enabled and the player clicks a monster:

* Switch to that monster as the current target (no-op if it's already the current target)
* Move to it, attack on the normal cooldown until it dies
* Resume auto search after death

If auto combat is enabled and the player clicks the floor:

* Cancel the current target, move to the clicked floor position
* Resume auto target search after arrival

### Drop Pickup

Hold left mouse button over a world drop to collect it.

Drop pickup input takes priority over movement and attack.

### Interactables (walk-to-interact, Phase 2 — 2026-06-19)

Same-lane interactable objects (e.g. crafting stations, portals; later NPCs, chests, gathering nodes). Click one → the player walks to it along the current lane → it activates when the player is close enough.

- **Click priority:** drop pickup → UI → enemy → **interactable** → ground move. Interactables are checked *after* enemies (clicking an enemy still attacks) and *before* plain ground movement.
- Clicking an interactable walks the player to its interaction point on the current lane, then triggers it once the player is within its interaction range (small, ~0.4). It never triggers instantly unless the player is already in range.
- Clicking the ground cancels a pending interaction; clicking a different interactable replaces it; starting a manual attack on an enemy cancels it; an interactable that is removed before arrival cancels safely.
- Interactable objects use trigger/click colliders — they never physically block the player, enemies, projectiles, drops, or movement.
- **Crafting station:** walk up → opens the Crafting window through the HUD (open-only; closes any other open HUD window, stays open if Crafting is already open).
- **Portal:** walk up → travels to its destination map if that map is valid/unlocked; otherwise logs a warning. (Same-lane interaction only — portals are not yet lane connectors.)

**Not implemented yet:** multi-lane interactions, ladders/portals as lane connections, pathfinding between platforms.

## Movement

> **Updated 2026-06-19 (Phase 1).** Replaces the earlier gravity/`Vector2.MoveTowards` description.

Single-lane, no-gravity, feet-root model:

- **`GroundLane`** defines the current scene's lane: `GroundY`, `MinX`, `MaxX` (accessed via static `GroundLane.Current`). Current scene: `GroundY = -2.0`, `MinX = -9.5`, `MaxX = 10.5`.
- **Player and enemies use a Kinematic Rigidbody2D with `gravityScale = 0`** — they never fall. Movement is deterministic, driven by `Rigidbody2D.MovePosition` in `FixedUpdate` (X only; Y is forced to `GroundLane.GroundY`).
- **Root position = feet / ground-contact point.** The visible sprite is a child offset upward (imported sprite pivots are not changed).
- **Click-to-move** ignores the clicked Y and clamps the clicked X to `[MinX, MaxX]`.
- **Enemies** patrol and chase within the lane bounds, always at `GroundY`. Enemy hitboxes are at least 1 tile tall from the feet upward. The spawner places enemies on `GroundY`.
- **Projectiles (Fireball)** travel horizontally on the lane at `GroundY + projectileHeight` (default `0.6`, kept ≤ 1 tile so low shots still hit 1-tile-high enemies).

**Not implemented yet:** multiple platforms/lanes, ladders, portals, jumping, A*/BFS pathfinding. Future cross-platform travel uses explicit ladder/portal transitions (a same-lane walk-to-interact step first), never physics jumping.

## Enemy Behavior

Enemies patrol between two points at their spawn position.

When hit or when the player enters their attack range, enemies enter Combat state and chase the player.

Enemies return to Patrol after a cooldown with no hit received (combatForgetTime).

**Movement speed (reduced 2026-06-20):** demo slimes patrol/chase at `patrolSpeed = 0.6` (was `1.5`, a 60% reduction), set on `Slime.prefab`. Player movement speed, attack cooldown, damage, HP, and rewards are unchanged.

**Grassland3 local respawn (added 2026-06-20):** Grassland3's 5 pre-placed slimes respawn ~3s after death via `LocalEnemyRespawner`, a small map-root-local component (not the general `EnemySpawner`) — it just re-enables the same enemy at its original spot after a delay, for demo grinding (q6: kill 5 slimes + collect 5 slime_essence). Grassland2's single tutorial slime does **not** respawn — kill once, stays dead, no infinite grind there.

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

Inventory is **fixed-slot** (implemented 2026-06-17). Each slot is a real, stable position — including empty ones. Same item stacks into its existing slot indefinitely; new items take the first empty slot. Removing/equipping an item clears that slot in place — later items never shift to fill the gap.

Default capacity: 20 slots.

Capacity can be increased by using an Inventory Expansion consumable item, or by leveling the **Inventory Expansion talent** (implemented 2026-06-17, +20 slots per level, max level 5). Both sources only append empty slots — existing items keep their slot positions. Total capacity = base capacity (consumable-driven, persisted) + talent bonus (computed live from talent level, not persisted separately).

Inventory data is saved as part of the player save file. Old saves (pre-fixed-slot, dense occupied-only lists) load safely: existing items keep their slot order starting at slot 0, then empty slots are appended up to capacity — no items are lost or reordered.

## Inventory UI

Press Tab to open/close the inventory panel.

**Pagination (implemented 2026-06-17):** 20 fixed slots per page. Visible slot `i` on page `p` maps to global (real) slot index `p * 20 + i` — paging never moves data, only changes which 20-slot window is displayed.

- 1 page total → page buttons hidden.
- First page (more pages exist) → Next button only.
- Middle page → Prev and Next both shown.
- Last page → Prev button only.

Each slot shows:
* Item icon (or grey placeholder if no icon assigned)
* Stack count when quantity > 1

Empty slots (including empty slots within capacity, and slots beyond capacity) show an empty dark frame.

Drag-and-drop is implemented for equipping/unequipping. No drag-and-drop slot-to-slot item move yet — see Recommended Next Features.

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

> **Superseded 2026-06-19 — see "Quest/Portal/Map Architecture" session update near the bottom of this file.** Map unlocks are no longer kill-objective-per-map with auto-unlock-next; they're driven by each destination map's own `UnlockQuestId`/`UnlockEnemyId`/`UnlockKillCount` via `PortalGate`, tied into the Q1–Q12 quest chain. `ObjectiveHelper` is disabled (not deleted). The section below describes the original (now replaced) design intent; kept for history.

> **Portal return-spawn (added 2026-06-20):** traveling map A → map B now spawns the player near B's portal back to A, if one exists, instead of always using B's default spawn. `MapSystem.PreviousMapId` tracks the source map of the last `TravelTo`; `MapContentController` looks for a matching `PortalInteractable.DestinationMapId` in the destination root. Falls back to the default spawn on fresh game/load/direct editor-open/debug travel or when no matching back-portal exists. `PortalInteractable` is unchanged — travel-only, stores only `destinationMapId`.

## Current Behavior (superseded)

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

> **Implemented and superseded by the frozen Q1–Q12 specification above.** See `.claude/HANDOFF_CURRENT_STATE.md` for the exact current objective/reward table and manual route. The older Q1–Q10 summary below is retained as historical implementation context only.

- Historical implementation started as one active linear quest at a time with no branching or concurrent quests. The same model now persists as Q1–Q12.
- The original 10-quest sketch has been replaced by the Q1–Q12 route in `.claude/HANDOFF_CURRENT_STATE.md`.
- Quest completion can award Exp and/or unlock a HUD feature (Craft/Talents/AutoCombat/Vault/Map via `FeatureFlags`). Inventory is never gated.
- Map/portal unlocks are driven by the destination `MapDefinition`'s own requirements (quest id and/or enemy kill count), evaluated by `PortalGate` — not by `QuestSystem` itself.

## Original sketch (superseded, kept for history)

### Quest 1

Kill 10 Slimes

Rewards:

* Coins
* EXP

### Quest 2

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
| Inventory Expansion | Inventory capacity +20 slots | Passive |

## Skill Hotbar

3 slots at the bottom-center of the screen (above the HUD button bar). Drag an unlocked skill talent from the Talent Window in Assign mode to assign it to a slot.

Each slot shows two layered icon images, `IconBG` and `IconFront`:
- Empty slot: both hidden.
- Assigned skill: both show the skill's icon.
- `IconFront` is a Filled-type image used as a radial cooldown indicator — `fillAmount` is 0 right when the skill is cast and fills back up to 1 as the cooldown finishes (driven by `PlayerCombatController.GetSkillCooldownProgress01(skillId)`, read-only).

Clicking a slot casts the assigned skill (`PlayerCombatController.TryCastSkill`) — costs MP, applies damage, starts the cooldown.

Hovering a slot for 1 second shows a tooltip with the skill's name, description, MP cost, cooldown, and required talent level. The tooltip hides immediately on pointer exit.

## Fireball (implemented)

Click the hotbar slot to cast. Costs MP, deals magic damage to the current/nearest valid enemy, then enters cooldown (no other skill can be cast from that slot until it finishes). Damage includes `TalentSystem.GetFireballDamageBonus()` from the Fireball Training talent.

**Still missing (polish, not core logic):** final icon asset, a visual/audio effect on cast or hit (currently damage applies with no projectile/flash), and explicit feedback when MP is insufficient (currently the cast silently does nothing).

## TODOs (Phase 2B)

- Fireball polish: icon, cast/hit visual or audio feedback, MP-insufficient feedback
- Add Arcane Power skill
- Assign icon sprites to TalentDefinition and SkillDefinition ScriptableObject assets (now 7 assets, including Inventory Expansion)

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

Quest progress is implemented and persisted for the frozen Q1–Q12 tutorial. This section's earlier “not implemented” state is superseded.

## Implementation Notes

* `SaveManager` singleton with `DontDestroyOnLoad`
* JSON serialization via `JsonUtility`
* Save file at `Application.persistentDataPath/account_save.json` (old `player_save.json` ignored, no migration)
* `SaveManager` exposes `CurrentAccount`, `CurrentSave` (selected character), `CurrentVault` (shared vault)
* Account/character flow: create or load account → create or select character → `OnSaveLoaded` fires → systems initialize
* Save-on-quit via `OnApplicationQuit` / `OnApplicationPause`
* Systems initialize only after `SaveManager.OnSaveLoaded` fires

## BootScene / StartMenu UI (current, 2026-06-20)

`BootScene` is the normal entry scene. UI is scene-/prefab-authored, not generated by code. The old programmer-art `StartupMenu` (runtime-generated) is bypassed/disabled in normal flow; kept only as the `TestCombat` direct-open fallback.

* **New Save** → opens Character Select with an empty list. Does not enter `TestCombat`.
* **Load Save** → opens Character Select with existing characters; a zero-character account is valid.
* **Create New Character** → adds a character row, stays on Character Select.
* **Character row's Select button** is the only way to choose a character and enter `TestCombat` — no separate Continue step.
* Not yet implemented: character deletion, rename/text input, multiple account save slots, UI polish.

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

## Crafting Window (redesigned 2026-06-18)

Left side: result item icon/name/description, the materials list, and a Craft button. Right side: the full recipe list, craftable recipes shown before uncraftable ones.

* No recipe selected: item info and materials are empty, Craft button is disabled.
* Recipe selected: shows the result item's icon/name/description, lists each required material with owned/required count (`{owned}/{required}`), colored green when you have enough and red when you don't. Craft button is enabled only when every material requirement is met.
* Each recipe row in the list visually distinguishes craftable from uncraftable (background changes).
* Crafting (or any inventory change while the window is open) refreshes everything live: recipe order, row backgrounds, material counts/colors, and the Craft button.

---

# UI

## Main HUD

Always-visible HUD anchored to the bottom of the screen. Layout is manually placed in the scene (`CharacterPanel`/`ButtonBar`) — MainHUD's job is display logic and window switching, not layout.

**Character panel** (bottom-left):
* Name (static text, not script-driven — player naming isn't implemented yet)
* Level — `Lv. {level}`
* HP bar — slider + `{cur}/{max}` text, live via `OnPlayerHPChanged`
* MP bar — slider + `{cur}/{max}` text, shows real current MP (not a placeholder)
* XP bar — slider + `{cur}/{max}` text, fills toward the real level-up threshold via `OnPlayerExpGained`/`OnPlayerLevelChanged`
* Silver and Gold coin display — live via `OnCurrencyChanged`

**Button bar** (bottom strip) — all real Unity `Button` components:
* Auto Combat toggle — calls `PlayerCombatController.SetAutoCombat()`
* Inv — opens/closes Inventory window
* Craft — opens/closes Crafting window
* Vault — opens/closes Vault window
* Talent — opens/closes Talent window
* Map — opens/closes Map window
* Quest / Settings — placeholder buttons (log only, inactive in scene, no icon assets yet)

Each window-toggle button's `interactable` state mirrors whether its window is currently open: closed = clickable, open/current = greyed out and unclickable. This stays correct even if a window is closed via its own in-window Close button rather than the HUD.

## Window Flow

MainHUD owns window switching for the windows it has buttons for (Inv/Craft/Vault/Talent/Map): clicking a different window's button closes whatever is currently open and opens the new one; clicking the currently-open window's button just closes it. Only one MainHUD-managed window is open at a time.

All windows also expose their own `Open()`, `Close()`, `Toggle()` and can still be opened directly (e.g. debug keys below) — MainHUD's one-at-a-time rule only applies to its own button clicks.

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

# Historical Recommended Next Features (Superseded)

The demo is now feature-frozen. Do not treat this backlog as approval to add features; only polish, bug fixes, QA, and documentation are currently in scope.

Priority order based on completeness of the core loop:

1. **Save/Load Trigger** — ✅ Done. Account-based save/load with character select is implemented (see Save System above).

2. **Fireball polish pass** — casting/MP/cooldown/damage is implemented. Remaining: final icon asset, a visual or audio cue on cast/hit, and feedback when MP is insufficient (currently silent). Arcane Power can follow the same pattern once added. **This is the current next task.**

3. **Quest System** — add `QuestSystem`, `QuestDefinition` ScriptableObjects (Kill 10 Slimes, Kill 20 Slimes), and `QuestWindow` UI with progress tracking.

4. **Offline Progression** — save logout time on quit. On next load, calculate EXP/coins/materials earned while away and display a popup.

5. **Talent / Skill Icons** — assign `Icon` sprites to all six `TalentDefinition` assets and `SkillDef_Fireball` in the Inspector. The slot grid already renders them; they just show grey until sprites are set.

6. **Player / Enemy Animations** — ✅ Done (see Session Update 2026-06-16 below).

7. **Inventory Expansion talent** — ✅ Implemented 2026-06-17. Grants +20 capacity (one page) per level, max level 5, stacks additively with the Inventory Expansion consumable.

8. **Exact-slot inventory operations (recommended, not implemented)** — `RemoveItem`/`Equip` are itemId-based, not slot-index-based. Add `RemoveItemAt(slotIndex)`, `EquipFromSlot(slotIndex)`, `MoveItem(fromSlot, toSlot)` to support slot-to-slot drag/drop and equip-from-a-specific-stack.

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

---

# Session Update — MainHUD, Skill Hotbar, Crafting Window (2026-06-18)

## Completed across recent sessions

- **Fireball casting** — fully implemented: click the hotbar slot, spend MP, deal magic damage to the current/nearest valid enemy, enter cooldown. Hotbar slot shows a radial cooldown fill and a hover tooltip with skill details.
- **Main HUD** — display logic rewired to the manually laid-out HUD: `Lv. {level}` text, HP/MP/EXP shown as `{cur}/{max}`, all window-toggle buttons are real Buttons whose `interactable` state reflects whether their window is open, and MainHUD enforces one-window-at-a-time switching for its own buttons.
- **Crafting Window** — redesigned layout (item info / materials list / craft button on the left, sorted recipe list on the right) is now fully wired: empty state on open, live material owned/required counts with green/red coloring, craftable-first sorting, and a craftable/uncraftable visual on each recipe row. See "Crafting Window (redesigned 2026-06-18)" above.

## Known gaps / next steps

- Fireball polish: icon, cast/hit feedback, MP-insufficient feedback.
- Crafting Window's "enough materials" / successful-craft path should be manually re-tested in a normal play session with a real inventory.
- The Quest system is implemented and persisted. Settings remains unimplemented; the Quest/Settings HUD buttons are still placeholders separate from the always-visible Quest Window.
- Full end-to-end QA across save/load, inventory, talents, hotbar, crafting, and MainHUD together hasn't been done in one sitting yet.

---

# Session Update — Quest/Portal/Map Architecture (2026-06-19, uncommitted)

**Full detail lives in `Assets/CLAUDE.md` ("Session Update — Q6–Q10, Multi-Map Rework, Portal Redesign") and `.claude/HANDOFF_CURRENT_STATE.md` (exact tables + manual test route). This is a pointer, not a duplicate.**

- Quest System (above) and Map/Area Progression (above) are both implemented, superseding their original sketches in this file.
- Destination `MapDefinition` owns portal unlock requirements (`UnlockQuestId`/`UnlockEnemyId`/`UnlockKillCount`/`UnlockRequirementLabel`); `PortalInteractable` is travel-only; `PortalGate` is the evaluator. `EnemyKillTracker` is now backed by saved per-character global kill counts.
- `TestCombat` is now one scene with 4 map-root GameObjects switched by a new `MapContentController`, not one flat Grassland area.
- Save/load persists quest progress, feature unlocks, current map ID, and global enemy kill counts. Import normalizes missing old-save fields and does not replay completion rewards.
- This session note is historical. Use the frozen-state section and `.claude/HANDOFF_CURRENT_STATE.md`, then check `git status`, before assuming documentation matches `HEAD`.
