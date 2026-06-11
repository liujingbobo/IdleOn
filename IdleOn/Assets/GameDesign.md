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

---

# Combat

## Auto Combat

Player:

* Finds nearest monster
* Moves toward monster
* Attacks when in range
* Finds new target after kill

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

# Equipment

Slots:

* Weapon
* Hat
* Armor
* Ring

Equipment provides stat bonuses.

---

# Loot

Monsters can drop:

* Coins
* Slime Gel
* Equipment

Loot enters inventory automatically.

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
