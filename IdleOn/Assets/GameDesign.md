# IdleOn-Inspired Demo

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

## Movement

Use simple direct 2D movement toward the target position.

Use Vector2.MoveTowards — no A* pathfinding required.

## Enemy Behavior

Enemies stand at their spawn position.

When the player enters the enemy's attack range, the enemy attacks the player periodically.

Enemies do not chase the player.

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

---

# Equipment

Equipment provides stat bonuses when equipped.

Slots: Hat, Weapon, Armor, Accessory, Pants, Shoes, Ring1, Ring2.

---

# Loot

Each enemy has a drop table.

Each drop entry defines:

* Drop type: Item or Currency
* Which item or which currency
* Drop chance
* Min and Max quantity

Multiple entries can drop from one kill.

Loot enters inventory (items) or currency wallet (coins) automatically.

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

Save:

* Level
* EXP
* Coins
* Inventory
* Equipment
* Talent Levels
* Vault Levels
* Quest Progress
* Last Logout Time

---

# UI

Main HUD:

* Character Name
* Class
* Level
* HP Bar
* MP Bar
* Auto Combat Toggle

Buttons:

* Inventory
* Talents
* Quests
* Map

Windows:

* Inventory
* Talents
* Quests
* Vault
* Offline Rewards
