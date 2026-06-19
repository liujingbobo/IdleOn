# Handoff — Current State (2026-06-19)

Quick-start context after `/clear`. Full detail lives in `Assets/CLAUDE.md` (top sections + dated session updates) and `Assets/GameDesign.md`. Test scene: `TestCombat`.

---

## Phase 1 — Movement model (single-lane, no-gravity, feet-root)

Supersedes the old Dynamic-RB / gravity / `transform.position`-write movement (old docs kept for history only).

- **One lane per scene, no gravity.** Player + enemies are deterministic; they never fall.
- **`GroundLane` (`_scripts/World/GroundLane.cs`)** = single source of truth. `GroundLane.Current` (static) → `GroundY`, `MinX`, `MaxX`, `ClampX(x)`. Registers in `OnEnable`, clears `Current` in `OnDisable` (covers disable + destroy). TestCombat values: `GroundY = -2.0`, `MinX = -9.5`, `MaxX = 10.5`.
- **Player (scene) + `Slime.prefab`:** Rigidbody2D = **Kinematic, gravityScale 0**. Movement = `Rigidbody2D.MovePosition` in `FixedUpdate`, **X only**, Y forced to `GroundLane.GroundY`, X clamped to lane. No velocity writes, no transform-writes for movement.
- **Root = feet.** Player root.y `-2.0` (capsule offset.y `+0.5`, Sprite child localY `+0.41` to preserve visual). Slime already feet-root. **Imported sprite pivots NOT changed — use Sprite child localPosition offsets.**
- **Click-to-move:** ignores clicked Y; X = `GroundLane.ClampX(clickX)`.
- **Enemies:** patrol bounds clamped to lane in `Start`; Y forced to `GroundY`; spawner spawns/respawns on `GroundY`. Enemy hitbox ≥ 1 tile tall from feet up (Slime box: offset.y 0.5, size.y 1).
- **Fireball:** spawnY = `GroundLane.GroundY + projectileHeight` (`projectileHeight` field on PlayerCombatController, default 0.6, keep ≤ 1). Horizontal only. `FireballProjectile.prefab` already Kinematic + trigger; kinematic trigger hits verified.

---

## Phase 2 — WorldInteractable (same-lane walk-to-interact)

Click world object → walk to it on the lane → `Interact()` when in range. Same-lane only.

- **Contract (`_scripts/World/`):** `IWorldInteractable` (`InteractionX`, `InteractionRange`, `CanInteract(player)`, `Interact(player)`); `WorldInteractable` abstract base (`InteractionX => transform.position.x`, `interactionRange = 0.4`, `CanInteract => isActiveAndEnabled`).
- **Click priority (`PlayerCombatController.HandleClick`):** drop pickup → UI → enemy → **WorldInteractable** → ground move. Interactable checked **after enemy** (enemy wins), **before** ground move.
- **Flow:** click interactable → store `_pendingInteractable`, walk to `GroundLane.ClampX(InteractionX)` in `ManualMove`; in `UpdateManualMove`, when within `InteractionRange` → `Interact(gameObject)` once (pending cleared **before** call → no double-fire) → resume auto-combat/idle.
- **Cancel rules:** ground click cancels pending; another interactable replaces; manual enemy attack cancels; destroyed/disabled interactable cancels safely (Unity-null + `isActiveAndEnabled` guard).
- **Colliders are trigger/click** — never physically block player/enemy/Fireball/drops/movement. Fireball still only damages `EnemyController`.
- **`CraftingStationInteractable`** → `MainHUD.OpenCraftingWindow()` (NOT `CraftingWindow.Open()`).
- **`MainHUD.OpenCraftingWindow()`** = public, **open-only**, reuses central switching (closes current HUD window, opens Crafting, refreshes button sprites). Not a toggle.
- **`PortalInteractable`** → `MapSystem.Instance.TravelTo(destinationMapId)` if valid, else warns.
- **TestCombat placeholders:** `CraftingStation` (x=5, trigger collider, MainHUD wired); `Portal` (x=-5, trigger collider, `destinationMapId = grassland_2`). Both **invisible (collider-only)**.

---

## Known working (play-verified)

- Compile clean (0 project errors).
- Player feet rest at GroundY, no fall/jitter/visual-jump; Y snaps back when displaced; X clamps to lane.
- Slimes Kinematic on GroundY; patrol/chase within lane; spawn/respawn on GroundY; hitbox feet→+1 tile.
- Fireball spawns at (GroundY + 0.6), horizontal, damages + kills Kinematic slimes (kinematic trigger works); kill respawns on GroundY.
- Click → walk → interact: player 4.0→4.66 (within 0.4 of station x=5) triggered once; pending cleared.
- CraftingStation: with Inventory open, interact closed Inventory + opened Crafting via MainHUD; 2nd interact left Crafting open (open-only); craft button sprite synced.
- Portal: locked dest → safe no-op; after unlock → `CurrentMapId = grassland_2`.
- Enemy melee/combat intact; HUD HP/MP/EXP/currency + save/load (from prior fixes) intact.

> QA note: play-mode tests used a throwaway in-memory character ("InterQA"/"LaneQA"); `IsLoaded` backing field was set false before stopping play so `OnApplicationQuit` autosave did NOT overwrite the real `account_save.json`. Do the same when reflection-testing.

---

## Next recommended task

**Add visuals for the interactable placeholders** (CraftingStation, Portal in TestCombat) so they are clickable/visible in normal play (currently collider-only). Then optionally:
- `NPCDialogueInteractable` once a dialogue system exists.
- Phase 3: multi-lane (lanes + ladders/portals as connectors, walk-to-connector then cross), still no physics jumping.

Earlier-flagged polish (still open, not blocking): camera follow may need a dedicated follow-target child, and enemy attack float-text (`playerPos + 0.8`) may need an offset tweak — both because player root moved center→feet in Phase 1.

---

## Files changed (Phase 1 + Phase 2)

**New scripts:**
- `Assets/_scripts/World/GroundLane.cs`
- `Assets/_scripts/World/IWorldInteractable.cs`
- `Assets/_scripts/World/WorldInteractable.cs`
- `Assets/_scripts/World/CraftingStationInteractable.cs`
- `Assets/_scripts/World/PortalInteractable.cs`

**Edited scripts:**
- `Assets/_scripts/Combat/PlayerCombatController.cs` — FixedUpdate lane Y-lock + MovePosition; click lane-X + WorldInteractable pass; `_pendingInteractable` logic; Fireball lane spawnY + `projectileHeight`.
- `Assets/_scripts/Enemies/EnemyController.cs` — FixedUpdate lane Y-lock + MovePosition; patrol clamp to lane.
- `Assets/_scripts/Enemies/EnemySpawner.cs` — spawn/respawn on `GroundLane.GroundY` (`LaneSpawnPos`).
- `Assets/_scripts/UI/MainHUD.cs` — added open-only `OpenCraftingWindow()`. (Also earlier-session: HP wiring + `OnSaveLoaded` refresh.)

**Prefab/scene:**
- `Assets/_assets/Prefabs/Enemies/Slime.prefab` — RB → Kinematic, gravityScale 0, UseFullKinematicContacts on.
- `TestCombat` scene — Player RB/collider/root/Sprite-child offsets; new `GroundLane`, `CraftingStation`, `Portal` objects.

**Docs:** `Assets/CLAUDE.md`, `Assets/GameDesign.md` (Phase 1 + Phase 2 sections + dated session updates).

---

## Do NOT touch (unless task explicitly says)

- **`PlayerStats.Recalculate()`** stat pipeline (extend only at end). Note: stats are NOT re-applied after load (talent/equipment bonuses) — separate known gap, out of current scope.
- **Save/load architecture** (`SaveManager`, `AccountSaveData`, `PlayerSaveData`).
- **UI layouts / data:** inventory, crafting, talents, SkillHotbar, HUD layout, item/equipment ScriptableObjects.
- **Combat damage logic** and **Fireball behavior/art/animations** (only spawn height was adjusted).
- **`CraftingWindow` / `CraftingDetailPanel` / `CraftingRecipeSlotUI` / `IngredientRowUI`** (protected; redesigned earlier under instruction).
- **`Enemy.prefab`** — broken/unreferenced stub (no RB/collider/sprite); the real enemy is `Slime.prefab`.
- **`FireballProjectile.prefab`** — already Kinematic + trigger; left as-is.
- **Physics layer collision matrix** — Player↔Enemy disabled; harmless now (Kinematic doesn't depenetrate), leave it.
- Now-unused-but-kept on PlayerCombatController: `TryResolveMoveTarget`, `groundLayerMask`, `maxGroundSearchDistance` — left in place, no movement use.
- **Not implemented (don't assume present):** multi-lane, ladders, portals-as-lane-connectors, BFS/pathfinding, jumping, dialogue/quest/offline systems.
