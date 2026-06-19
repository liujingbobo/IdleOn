# Handoff — Current State (2026-06-19, updated for Codex handoff)

Quick-start after `/clear` or for a fresh Codex session. Full detail in `Assets/CLAUDE.md` + `Assets/GameDesign.md`. Test scene: **TestCombat**.

**Committed:** HEAD `ee10615` (through portal/HUD feature gates, Q1–Q5, EnemySpawner still active at that point).
**Uncommitted (working tree, not yet committed):** Q6–Q10 content, multi-map single-scene rework, portal redesign, EnemyKillTracker, item/recipe/dialogue assets for the new quests. See `git status` for the exact file list — do not assume HEAD matches the running scene.

---

## What's real right now (committed + uncommitted together)

### 1. Movement/combat (committed, stable — do not touch casually)
Single-lane, no-gravity, feet-root. `GroundLane.Current` (static) is the source of truth for `GroundY`/`MinX`/`MaxX`/`ClampX`. Player + enemies: Kinematic Rigidbody2D, `gravityScale=0`, moved via `MovePosition` in `FixedUpdate`, X-only. Click-to-move ignores clicked Y, clamps X to lane. Fireball is a straight horizontal projectile at lane height, hits first enemy only, no pierce, no AoE.

### 2. World interactions (committed)
`WorldInteractable` walk-to-interact base (click → walk to `InteractionX` on lane → `Interact()` in range 0.4). `PortalInteractable` is **travel-only** — stores `destinationMapId`, calls `MapSystem.TravelTo`, owns no unlock logic. `CraftingStationInteractable` → `MainHUD.OpenCraftingWindow()` (open-only, not toggle). `NPCDialogueInteractable` → `DialogueSystem.StartDialogue`, now optionally asks a `QuestDialogueSelector` for a per-active-quest dialogue override (falls back to its fixed dialogue if no entry matches). `DialogueWindowUI` = click-anywhere-to-advance.

### 3. Quest system (in-memory only — no save/load wiring)
`QuestSystem` singleton, one active linear quest, `NextQuestId` chain, no branching. `QuestDef_Q1`..`QuestDef_Q10` all exist as assets. Objective types (`QuestObjective.Type`, confirmed from assets): `2`=enemy kill, `3`=item collect, `4`=item craft, `5`=item equip, `6`=talent upgrade, `7`=dialogue ended.

| Quest | Objective(s) | Exp | UnlocksFeatures (flags) | Next |
|---|---|---|---|---|
| q1 | ground move | — | — | q2 |
| q2 | enter `grassland_2` (map changed) | — | — | q3 |
| q3 | kill `slime` ×1 | — | — | q4 |
| q4 | enter `town` (map changed) | — | — | q5 |
| q5 | dialogue `chief_q5` ×1 | — | — | q6 |
| q6 | kill `slime` ×5, collect `slime_essence` ×5, dialogue `chief_q6` ×1 | 100 | `2` = Craft | q7 |
| q7 | craft `slime_sword` ×1, equip `slime_sword` ×1 | 100 | — | q8 |
| q8 | dialogue `chief_q8` ×1 | 100 | `5` = AutoCombat(1)+Talents(4) | q9 |
| q9 | talent upgrade `fireball_training` ×1 | 100 | — | q10 |
| q10 | dialogue `chief_q10` ×1 | 150 | `24` = Vault(8)+Map(16) | — (end) |

`FeatureFlags` bit values: `AutoCombat=1, Craft=2, Talents=4, Vault=8, Map=16`. Inventory is never gated.

All 4 previously-deferred event raisers are now wired (no longer placeholders): `DropManager.Collect`→`RaiseItemCollected`, `CraftingSystem.Craft`→`RaiseItemCrafted`, `EquipmentSystem.Equip`→`RaiseItemEquipped`, `TalentSystem.Upgrade`→`RaiseTalentUpgraded`.

`QuestDialogueSelector` (`_scripts/Quests/`) — maps active quest id → `DialogueDefinition`, attached near the Town NPC; new assets `DialogueDef_ChiefQ5/Q6/Q8/Q10`.

### 4. Playable map flow (uncommitted rework)
**Single scene (`TestCombat`), one player, multiple map-root GameObjects** — not separate Unity scenes. Roots in-scene: `Map_grassland_1`, `Map_grassland_2`, `Map_town`, `Map_grassland_3`. New `MapContentController` (`_scripts/World/`) subscribes to `GameEvents.OnMapChanged`: activates only the current map's root, moves the persistent Player to that map's configured spawn point, and pre-marks every *configured* map's `MapProgressData.IsUnlocked = true` (this is unconditional — it does not check quest state; `PortalGate`, below, is what actually decides if a portal is usable). Does not touch `MapSystem`/`PortalGate`/`PortalInteractable`/`WorldInteractable`.

`grassland_1` is the default active map on a fresh game. `grassland_2` has the slime encounter for q3. `town` has the Chief NPC + CraftingStation. `grassland_3` has 5 slimes for q6's kill objective.

**Global `EnemySpawner` is disabled** (`m_IsActive: 0` in the scene) for this vertical slice — enemies in each map root are pre-placed, not spawner-driven.

### 5. Portal architecture (uncommitted rework — destination-MapDef owns unlocks)
Prefab: `Assets/_assets/Prefabs/Portal.prefab` (animated, has a hidden-by-default `RequirementText` TMP child wired to `PortalGate.requirementText`).

- `PortalInteractable` stores **only** `destinationMapId`. It owns zero unlock logic.
- The **destination** `MapDefinition` is the sole source of truth for unlock requirements: `UnlockQuestId` (optional), `UnlockEnemyId` + `UnlockKillCount` (optional pair), `UnlockRequirementLabel` (optional display text) — all ANDed.
- `PortalGate` (`_scripts/Quests/PortalGate.cs`) is a pure evaluator: reads `portal.DestinationMapId` → `GameDatabase.Instance.Maps.GetMap()` → checks `QuestSystem.Instance.IsCompleted(UnlockQuestId)` and `EnemyKillTracker.Instance.GetKillCount(UnlockEnemyId) >= UnlockKillCount` → drives portal visual alpha (1.0 unlocked / 0.5 locked), `Collider2D.enabled`, `PortalInteractable.enabled`. **Never disables the portal root.**
- Requirement text only shows when locked specifically by a kill requirement (`"{current}/{required} × {label}"`); a quest-only lock shows no text, just half alpha.
- `PortalGate.Evaluate()` also force-sets `MapProgressData.IsUnlocked = true` on the destination once its own requirements are met — this exists because `MapSystem.TravelTo` still gates on that legacy flag. **Note:** `MapContentController` (above) already unconditionally sets this flag true for every configured map on every map change, so this sync in `PortalGate` is currently redundant in practice but kept as a correctness belt-and-suspenders since `PortalGate` doesn't know about `MapContentController`. Worth simplifying later, not urgent.
- 6 portal instances in `TestCombat`: `Portal_to_grassland_3`, `Portal_back_to_grassland_2`, `Portal_back_to_grassland_1`, `Portal_back_to_town`, `Portal_to_town`, `Portal_to_grassland_2`. All stale per-instance `unlockQuestId` overrides removed — all 6 resolve `destinationMapId` correctly (some via prefab default `grassland_2`, no override needed).
- `ObjectiveHelper`/old top-HUD map objective UI (`ObjectiveHelperUI`) is **disabled** (`activeSelf: false`), not deleted, for this quest demo — it predates destination-MapDef unlocks and would show stale/wrong text.

### 6. Current MapDefinition requirements

| MapId | UnlockQuestId | UnlockEnemyId / UnlockKillCount |
|---|---|---|
| `grassland_1` | — (default unlocked) | — |
| `grassland_2` | `q1` | — |
| `town` | `q3` | — |
| `grassland_3` | `q5` | — |

No kill-gated map exists yet. A future `grassland_4` is the intended first example of `UnlockEnemyId`/`UnlockKillCount`/`UnlockRequirementLabel` in use — likely `UnlockQuestId=q10`, `UnlockEnemyId=slime`, `UnlockKillCount=10`.

---

## Temporary / technical debt (read before touching)

- **`EnemyKillTracker`** (`_scripts/World/EnemyKillTracker.cs`) — **temporary**, in-memory-only singleton. Subscribes to `GameEvents.OnEnemyKilled`, tracks kills **globally** (not per-map) by `enemyId`. Resets every session. Comment in the file flags it for migration into `PlayerSaveData` (global enemy kill counts) later — do that migration instead of extending this class further.
- `PortalGate`'s `MapProgressData.IsUnlocked` sync (see Portal architecture above) is a stopgap bridging the new destination-MapDef model onto `MapSystem`'s old auto-unlock-next-map flag. `MapSystem.cs` itself was never modified.
- `MapProgressData.IsComplete` is dead/unused in this demo — nothing sets it true anymore (the old `MapSystem.CompleteObjective` auto-unlock flow was removed from the gate path).
- Save/load does **not** persist: quest progress, feature unlocks, current map-root state, or `EnemyKillTracker` counts. Every play session starts from q1 / grassland_1 / no features unlocked, regardless of `PlayerSaveData.CurrentMapId`/`MapProgress` on disk.
- Map4 (kill-requirement portal UI with live `current/required` label) and Map5/boss content are not implemented.
- Full manual Q1–Q10 UI playthrough (clicking through actual game UI, not headless `execute_code` reflection) has not been done this round for Q7–Q10 — only Q1–Q6 was walked end-to-end in Play mode by the prior session.

---

## Manual test route (for a human or Codex with Unity access)

1. Play → New Save → Create Character → enters `grassland_1` (default active map root, all portals visible).
2. Walk player (ground-move) → completes q1 → `Portal_to_grassland_2` flips to alpha 1, collider/interactable enabled.
3. Click portal → travel to `grassland_2` → entering completes q2 automatically.
4. Kill a slime in `grassland_2` → completes q3 → town-bound portal unlocks.
5. Travel to `town` → entering completes q4 automatically.
6. Talk to the Chief NPC (dialogue `chief_q5`) → completes q5 → `grassland_3` portal unlocks.
7. Travel to `grassland_3`. Kill 5 slimes + collect 5 `slime_essence` (drops) + talk to Chief (`chief_q6`) → completes q6 → unlocks Craft button (HUD).
8. Open Crafting (Craft button or CraftingStation in town) → craft `slime_sword` (consumes `slime_essence` via `Recipe_SlimeSword`) → equip it → completes q7.
9. Talk to Chief (`chief_q8`) → completes q8 → unlocks Talents window + AutoCombat HUD toggle.
10. Open Talents → upgrade `fireball_training` → completes q9.
11. Talk to Chief (`chief_q10`) → completes q10 → unlocks Vault + Map windows. Quest chain ends (no `NextQuestId`).

---

## Codex next suggested tasks (in order)

1. **First**: inspect `git status` — large uncommitted working tree (Q6–Q10 content, map rework, portal redesign, new assets). Do not touch files outside what's asked.
2. Manually verify Q1–Q6 route in `TestCombat` (steps 1–7 above) in the actual Editor/Play mode UI — prior verification of this segment was via headless `execute_code` reflection, not a real UI playthrough.
3. Verify Q7–Q10 UI path (steps 8–11 above) — not yet UI-tested at all.
4. Implement save/load for quest progress, feature unlocks, and global enemy kill counts (move `EnemyKillTracker`'s counts into `PlayerSaveData`).
5. Implement the Map3→Map4 portal with `q10` + 10-slime-kill requirement (first real use of `UnlockEnemyId`/`UnlockKillCount`/`UnlockRequirementLabel` + the requirement-text UI).
6. Build Map4/Map5/boss content.

---

## Do NOT touch casually (unless task explicitly says so)

- **Movement model** (`GroundLane`, Kinematic RB2D + `MovePosition`, click-to-move X-clamp) — committed, stable, verified.
- **Fireball behavior** (straight projectile, lane height, first-enemy-only, no pierce).
- **`DialogueSystem` core** (`_scripts/Dialogue/`) — headless runtime, `StartDialogue/Advance/EndDialogue`. `QuestDialogueSelector` is the only quest-aware layer on top; it does not belong inside `DialogueSystem` itself.
- **`PortalInteractable`'s travel-only role** — it must never gain unlock-requirement fields again; that logic belongs solely on the destination `MapDefinition` + `PortalGate`.
- **`QuestSystem`'s linear one-active-quest model** — do not add branching/multiple-concurrent-quests without explicit instruction.
- **`MainHUD` window-toggle logic** (`OnXButtonClicked`/`ToggleWindow`/`RefreshButtonStates`) — `OpenCraftingWindow()` is the only sanctioned open-only entry point for non-button callers (e.g. `CraftingStationInteractable`).
- **`MapSystem.cs`** — protected; `PortalGate`/`MapContentController` only read/write `MapProgressData.IsUnlocked` via `GetProgress()`, never modify `MapSystem` itself.
- Save/load architecture, `PlayerStats.Recalculate` pipeline, `WorldInteractable` base, `Slime.prefab` art — see `Assets/CLAUDE.md` "Protected Systems" for the full list (still current).

> Untracked `_assets/_Temp/RawGenerated/*` and stray `*.meta` files in `DialogueBody/` are MCP/user art artifacts — not part of gameplay logic; ignore unless asked.
> QA: play-mode tests are save-safe only if no character is selected (`SaveManager.AutoSave` guards on `IsLoaded`). Keep doing the same for headless testing.
