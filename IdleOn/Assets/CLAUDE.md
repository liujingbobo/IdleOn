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

---

## Folder Structure

Assets/_Project/

* Art
* Audio
* Prefabs
* Scenes
* ScriptableObjects
* Scripts

Scripts:

* Core
* Characters
* Combat
* Enemies
* Inventory
* Equipment
* Talents
* Vault
* Quests
* Save
* UI

---

## UI Systems

### Float Text

Use `FloatTextManager.Show(text, worldPos, FloatTextType, isCritical)` to display damage numbers.

Types: `Physical` (orange), `Magic` (blue), `Heal` (green).

`isCritical = true` increases font size.

FloatTextManager uses an object pool — do not instantiate FloatText directly.

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

Each `EnemyDefinition` holds a `DropTable` — a list of `DropEntry` records.

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
* EnemyDefinition (includes DropTable)
* Talents
* Vault Upgrades
* Quests
* MapDefinition
* MapRegistry

Do not hardcode game data into UI scripts.

---

## Code Style

Use clear names.

Examples:

* PlayerCombatController
* EnemySpawner
* InventorySystem
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
