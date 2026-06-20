# Handoff — Frozen Demo State

Last updated: 2026-06-20. Test scene: **`Assets/_scenes/TestCombat.unity`**.

## Scope

The playable vertical slice is frozen. The Q1–Q12 tutorial, one-scene map flow, feature gates, Quest Window, and tutorial persistence are complete.

Until explicitly approved, next work is limited to bug fixes, visual/UX polish, regression testing, and one uninterrupted human Q1–Q12 release playthrough. Do **not** add systems, quests, Map4/Map5, or boss content.

## Tutorial quest chain

`QuestSystem` remains one-active-quest, linear, and non-branching.

| Quest | Objective | Unlock |
|---|---|---|
| q1 | Move | grassland2 portal requirement satisfied |
| q2 | Enter `grassland_2` | — |
| q3 | Kill `slime` ×1 | town portal requirement satisfied |
| q4 | Enter `town` | — |
| q5 | End dialogue `chief_q5` | grassland3 portal requirement satisfied |
| q6 | Kill `slime` ×5 + collect `slime_essence` ×5 | — |
| q7 | End dialogue `chief_q7` | Craft |
| q8 | Craft `slime_sword` ×1 | — |
| q9 | Equip `slime_sword` ×1 | — |
| q10 | End dialogue `chief_q10` | AutoCombat + Talents |
| q11 | Upgrade `fireball_training` ×1 | — |
| q12 | End dialogue `chief_q12` | Vault + Map; chain ends |

Chief dialogue routing:

- q5 → `chief_q5`
- q6 → `chief_hint_q6`
- q7 → `chief_q7`
- q8 → `chief_hint_q8`
- q9 → `chief_hint_q9`
- q10 → `chief_q10`
- q11 → `chief_hint_q11`
- q12 → `chief_q12`

Q6/Q8/Q9/Q11 dialogues are hints only and are not objective target IDs. After q12, the Chief randomly plays `chief_daily_01/02/03`; daily dialogue never advances a quest.

Q11 tells the player to kill monsters, level up, earn a Talent Point, then upgrade Fireball Training. No free Talent Point is granted.

## Quest Window

`Canvas/QuestPanel` is an always-visible top-right tracker.

- `QuestWindowUI` is wired to `QuestName` and `QuestDescription`.
- It shows the active title and live current/required objective counts.
- It refreshes on quest changes, objective progress, and persistent-state restoration.
- After q12 it displays **Tutorial Complete** / **Keep exploring.**
- Old `Canvas/ObjectiveHelper` map-objective UI is disabled and must remain disabled.

## Save/load

Account JSON remains at `Application.persistentDataPath/account_save.json`; the existing account/character architecture is unchanged.

Per-character tutorial persistence stores:

- active quest ID;
- completed quest IDs;
- active objective counts;
- unlocked feature flags;
- global enemy kill counts by enemy ID;
- current map ID;
- existing `MapProgressData`.

`TutorialProgressSaveData.cs` defines `QuestSaveData` and `EnemyKillSaveData`. Old saves normalize missing/null fields. Quest import rebuilds runtime state without replaying completion, EXP, or feature rewards. Enemy-kill import does not raise fake kill events. `EnemyKillTracker` is now backed by saved per-character global counts.

Restore order:

1. `SaveManager` selects `CurrentSave`.
2. Quest, feature, and enemy-kill state import.
3. Existing `OnSaveLoaded` subscribers restore map/inventory/talent state.
4. `MapSystem` raises `OnMapChanged`.
5. `MapContentController` activates the saved root and moves the player.
6. `OnPersistentProgressLoaded` refreshes Quest Window and PortalGate.

## Map and portal architecture

The demo uses one Unity scene, not separate map scenes. `TestCombat` roots:

- `Map_grassland_1`
- `Map_grassland_2`
- `Map_town`
- `Map_grassland_3`

`MapContentController` switches roots and places the persistent player at the configured spawn.

`PortalInteractable` is travel-only and stores only `destinationMapId`. Never add quest/kill requirement fields to portal instances.

`PortalGate` evaluates the **destination** `MapDefinition`:

| Destination | Requirement |
|---|---|
| `grassland_1` | none |
| `grassland_2` | `UnlockQuestId=q1` |
| `town` | `UnlockQuestId=q3` |
| `grassland_3` | `UnlockQuestId=q5` |

Portal visual alpha is `1.0` unlocked and `0.5` locked. PortalGate also syncs `MapProgressData.IsUnlocked` because `MapSystem.TravelTo` still requires that legacy travel flag. `MapProgressData` is compatibility state, not the source of portal requirements.

## Feature gates and polish

- Inventory is always available.
- Craft unlocks at q7.
- AutoCombat + Talents unlock at q10.
- Vault + Map unlock at q12.
- `startAutoCombatOnPlay` is disabled.
- AutoCombat starts off and remains hidden before q10.
- Slime Essence, Slime Sword, and Silver currency have nonblank existing-project icons.

## Manual release route

1. New account/character starts on `grassland_1`; Q1 visible; AutoCombat off; gated HUD buttons hidden.
2. Move → q2; grassland2 portal unlocks.
3. Enter grassland2 → q3.
4. Kill one slime → q4; town portal unlocks.
5. Enter town → q5.
6. Finish `chief_q5` → q6; grassland3 portal unlocks.
7. In q6, verify `chief_hint_q6` does not advance.
8. Kill five slimes and collect five visible Slime Essence drops; Quest Window updates live → q7.
9. Save/quit/reload during partial q6; verify quest, counts, global kills, current root, and portals restore.
10. Return to town; finish `chief_q7` → q8; Craft appears.
11. Verify `chief_hint_q8` does not advance; craft Slime Sword → q9.
12. Verify `chief_hint_q9` does not advance; equip Slime Sword → q10.
13. Finish `chief_q10` → q11; AutoCombat + Talents appear.
14. Verify `chief_hint_q11` does not advance. Kill monsters until leveling grants a Talent Point; upgrade Fireball Training → q12.
15. Finish `chief_q12`; Vault + Map appear; Quest Window shows Tutorial Complete.
16. Talk repeatedly after q12; only daily dialogue plays and no quest restarts.
17. Save/quit/reload after q7, q10, and q12; verify features and chain state are not duplicated or lost.
18. End with no game compile/runtime errors or warnings.

## Known debt / out of scope

- Map4, Map5, and boss content are not implemented and are outside the frozen demo.
- A possible future Map4 requirement is q12 (older notes may say q10) plus 10 global slime kills. It is not active.
- PortalGate’s `MapProgressData.IsUnlocked` synchronization remains compatibility glue.
- Full release still requires one uninterrupted human Q1–Q12 playthrough with real save/quit/reload checkpoints.
- MCP transport disconnect messages are tooling noise, not game errors; release QA should use a cleared console.

## Do not touch casually

- SaveManager/account save architecture
- QuestSystem’s linear one-active-quest model
- PortalGate / PortalInteractable / destination-MapDefinition ownership
- MapSystem / MapContentController
- DialogueSystem core
- MainHUD window-toggle logic
- movement/GroundLane model
- Fireball behavior
- Inventory/Crafting/Equipment/Talent internals
- Quest Window layout
- ObjectiveHelper disabled state
