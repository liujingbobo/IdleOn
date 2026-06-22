

https://github.com/user-attachments/assets/984d3b2b-1011-47df-ba93-8605836bbc69

# IdleOn-Inspired Unity Demo

A small, polished Unity vertical slice inspired by the early-game feel of **IdleOn** and classic 2D idle RPGs.

This is **not** a full IdleOn clone and is not affiliated with IdleOn.
The goal of this project is to demonstrate a complete, playable idle-action RPG loop with combat, quests, progression, equipment, crafting, talents, map travel, vault upgrades, save/load, and offline rewards.

<img width="1672" height="941" alt="Generated image 1" src="https://github.com/user-attachments/assets/e5a5fec6-bf67-4719-9795-b30f182937fd" />

---

## Download

The demo build should be distributed through **GitHub Releases**, not committed directly into the repository.

Recommended release file:

```text
IdleOnDemo-v0.02.zip
```

---

## Current Demo Scope

This project is a frozen vertical slice.

Included:

* Q1–Q12 tutorial quest chain
* Four playable maps
* Player combat and auto combat
* Loot, inventory, equipment, crafting
* Talents and skill hotbar
* Vault upgrades
* Dialogue and quest NPC
* Save/load with account + character select
* Offline earnings after tutorial completion
* Polished UI windows and basic animations

Not included in the current demo:

* Map 4 / Map 5
* Boss fight
* Multiplayer
* Backend / login / cloud save
* Multi-lane pathfinding
* Settings menu
* Full production content pipeline

---

## Core Gameplay Loop

```text
Kill monsters
↓
Gain EXP, coins, and materials
↓
Level up
↓
Earn talent points
↓
Upgrade talents
↓
Craft and equip stronger gear
↓
Unlock new features and maps
↓
Upgrade account-wide vault bonuses
↓
Repeat
```

---

## Features

### Combat

The player moves and fights on a single horizontal ground lane.

* Click ground to move.
* Click enemies to attack.
* Click interactables to walk up and interact.
* Hold left mouse over drops to collect them.
* Auto Combat can automatically seek, move to, and attack enemies.
* Manual enemy clicks and Auto Combat share the same persistent target system.
* Fireball skill casts horizontally and damages the first enemy it hits.
* Player and enemies have Hurt and Death animations.
* Player death respawns the character in Town with HP restored.

### Maps and Travel

The demo contains four playable maps:

* Grassland 1
* Grassland 2
* Town
* Grassland 3

All maps are now prefab-backed and loaded through the runtime map loader.

Travel supports:

* Portal travel
* Map Window travel
* Locked/unlocked map states
* Current-map marker
* Portal back-spawn behavior
* Save/load current-map restore

Drops and projectiles are scoped to the current active map and are cleaned up when leaving a map.

### Quest System

The demo has a linear Q1–Q12 tutorial chain.

The tutorial teaches:

* Movement
* Combat
* Map travel
* NPC dialogue
* Material collection
* Crafting
* Equipment
* Auto Combat
* Talents
* Vault
* Map Window
* Offline progression unlock flow

After Q12, the tutorial is complete and the Chief NPC switches to daily/random dialogue instead of advancing quests.

### Feature Unlocks

Features unlock through tutorial progression:

| Feature          | Unlock                    |
| ---------------- | ------------------------- |
| Inventory        | Always available          |
| Crafting         | Q7                        |
| Auto Combat      | Q10                       |
| Talents          | Q10                       |
| Vault            | Q12                       |
| Map Window       | Q12                       |
| Offline Earnings | After tutorial completion |

### Inventory

The inventory is fixed-slot based.

* Items stack in stable slots.
* Empty slots stay empty.
* Items do not shift around after removal.
* Pagination supports 20 slots per page.
* Equipment can be dragged into equipment slots.
* Materials are used for quests and crafting.
* Currency is stored separately from inventory items.

### Equipment

Implemented equipment slots:

* Hat
* Weapon
* Armor
* Accessory
* Pants
* Shoes
* Ring 1
* Ring 2

Equipment affects player stats through the stat system.

### Crafting

Crafting uses recipe data and inventory materials.

Current examples include:

* Slime Sword
* Slime Armor
* Basic Hat

The crafting station in Town is hidden until Crafting is unlocked.

### Talents

Players earn talent points from leveling up.

The Talent Window supports:

* Talent grid UI
* Upgrade button
* Selected talent info panel
* Passive talents
* Skill talents
* Skill assignment mode
* 3-slot skill hotbar

Implemented talents include:

* Power Strike
* Thick Skin
* Swift Feet
* Lucky Hunter
* Mana Training
* Fireball Training
* Inventory Expansion

### Vault Upgrades

Vault upgrades are account-wide permanent upgrades.

Implemented upgrades:

| Upgrade        | Effect                          |
| -------------- | ------------------------------- |
| Bigger Damage  | Permanent damage increase       |
| Monster Tax    | Increased coin gain             |
| Natural Talent | Extra talent points on level-up |

The Vault UI has a slot list and a detail panel with name, icon, description, level, current effect, next effect, and upgrade cost.

### Offline Earnings

Offline earnings unlock after tutorial completion.

Conditions:

* Tutorial must be complete.
* Auto Combat must have been enabled before quitting.
* The player must have logged out from a supported map.

Currently supported maps:

* Grassland 2
* Grassland 3

On next login, the game calculates rewards based on:

* Last map
* Offline duration
* Simple generous reward formula

The popup shows earned rewards such as:

* Gold
* Slime Essence

Rewards are granted once and will not duplicate on repeated loads.

### Save / Load

The project uses JSON save data.

Save structure:

* Account save
* Multiple characters
* Account-wide vault data
* Per-character inventory, equipment, talents, hotbar, quest progress, map progress, current map, currency, EXP, level, and offline earnings state

Normal flow:

```text
BootScene
↓
Main Menu
↓
Character Select
↓
TestCombat
```

Direct-opening `TestCombat` in the Unity Editor also has a safe fallback for testing.

---

## Controls

| Input                   | Action                       |
| ----------------------- | ---------------------------- |
| Left click ground       | Move                         |
| Left click enemy        | Attack                       |
| Left click interactable | Walk to interact             |
| Hold left click on drop | Collect drop                 |
| Tab                     | Open / close Inventory       |
| C                       | Open / close Crafting Window |
| T                       | Open / close Talent Window   |
| V                       | Open / close Vault Window    |
| M                       | Open / close Map Window      |
| HUD Auto Combat Button  | Toggle Auto Combat           |
| Skill Hotbar Button     | Cast assigned skill          |

---

## Tech Stack

* Unity 6
* C#
* uGUI
* TextMeshPro
* ScriptableObjects
* JSON save files
* 2D Rigidbody / Collider systems
* Prefab-backed map loading
* Modular gameplay systems

---

## Project Architecture

The project is organized around small systems:

```text
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

Main systems include:

* PlayerCombatController
* EnemyController
* EnemyTargetRegistry
* InventorySystem
* EquipmentSystem
* CurrencySystem
* DropManager
* QuestSystem
* MapSystem
* MapContentController
* TalentSystem
* VaultSystem
* CraftingSystem
* SaveManager
* OfflineEarningsSystem

Data is generally stored in ScriptableObjects:

* ItemDefinition
* EnemyDefinition
* LootTable
* CraftRecipeDefinition
* TalentDefinition
* VaultUpgradeDefinition
* QuestDefinition
* MapDefinition

---

## Current Playable Flow

The intended vertical slice route is:

1. Start or load a character.
2. Complete movement tutorial.
3. Unlock Grassland 2.
4. Kill the tutorial slime.
5. Unlock Town.
6. Talk to the Chief.
7. Unlock Grassland 3.
8. Kill slimes and collect Slime Essence.
9. Return to Chief.
10. Unlock Crafting.
11. Craft Slime Sword.
12. Equip Slime Sword.
13. Unlock Auto Combat and Talents.
14. Level up and upgrade Fireball Training.
15. Complete the tutorial.
16. Unlock Vault and Map Window.
17. Enable Auto Combat, quit, and return later for offline earnings.

---

## Known Limitations

This is a demo vertical slice, not a full game.

Current limitations:

* Only four maps are playable.
* No Map 4, Map 5, or boss fight.
* No multi-lane navigation.
* No ladders or platform pathfinding.
* No cloud save.
* No settings menu.
* No character deletion / rename UI.
* Some visual effects and audio are still placeholder or missing.
* VFXRoot exists for future map-scoped effects but is currently reserved.

---

## Development Status

The demo is feature-frozen.

Current focus:

* Bug fixing
* QA
* Polish
* Documentation
* Release packaging

Major completed systems:

* Combat
* Auto Combat
* Loot
* Inventory
* Equipment
* Crafting
* Talents
* Vault
* Quest chain
* Map travel
* Dialogue
* Save/load
* Offline earnings
* Map prefab migration
* UI animation pass
* Player/enemy hit and death feedback

---

## Screenshots

Add screenshots or GIFs here:

```markdown
![Combat Screenshot](Docs/Screenshots/combat.png)
![Inventory Screenshot](Docs/Screenshots/inventory.png)
![Talent Window Screenshot](Docs/Screenshots/talent.png)
![Map Window Screenshot](Docs/Screenshots/map.png)
```

---

## Build Notes

Recommended distribution:

1. Build the game in Unity.
2. Zip the build folder.
3. Upload the zip to GitHub Releases.

Do not commit build archives directly into the repository.

---

## Credits & Attribution

This project was developed with the help of GPT, Codex, and Claude throughout planning, implementation, debugging, and documentation.

All visual art assets used in this demo are free and open-source / publicly available assets. Full asset credits and source links are listed below:

- **Complete UI Book Styles Pack** by Crusenho Agus Hennihuno — https://crusenho.itch.io/complete-ui-book-styles-pack
- **Free Adventurer and Slime Game Sprites** by Segel2D (adien.duabelas@gmail.com)
- **Pixelify Sans** font — Google Fonts, OFL licensed
- **Raven Fantasy Icons** by Clockwork Raven — https://clockworkraven.itch.io/raven-fantasy-icons
- **750 Effect and FX Pixel All (Free)** by BDragon1727 — https://bdragon1727.itch.io/750-effect-and-fx-pixel-all
- **Free Effect Bullet Impact Explosion 32x32** by BDragon1727 — https://bdragon1727.itch.io/free-effect-bullet-impact-explosion-32x32
- **Lively NPCs** by chierit — https://chierit.itch.io/lively-npcs
- **Platformer Tileset - Pixelart Grasslands** by BigManJD — https://biggermanjd.itch.io/platformer-tileset-pixelart-grasslands
- **Tiny RPG Character Asset Pack** by Zerie — https://zerie.itch.io/tiny-rpg-character-asset-pack
- **URP Retro CRT Shader** by Cyanilux — https://github.com/Cyanilux/URP_RetroCRTShader

