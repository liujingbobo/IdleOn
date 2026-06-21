# Map Prefab Migration — Tracking Doc

## 1. Purpose

Canonical tracking doc for migrating from scene-baked map roots (current `TestCombat` architecture) to runtime-loaded map prefabs. Every future Claude/Codex prompt for this migration must **read this file first** and **update it at the end** of each pass. This doc, not memory or chat history, is the source of truth for what phase the migration is in.

## 2. Confirmed design decisions

- Keep `TestCombat` as the only gameplay scene.
- Each map becomes a prefab.
- Travel instantiates/loads the destination map prefab and destroys/offloads the old map instance (destroy, not just deactivate, once migration is live).
- Old map drops are deleted on unload.
- Old map projectiles/skill effects/VFX are deleted on unload.
- Drops/projectiles/VFX should eventually be parented under the current map's runtime root, so unload cleanup is automatic via normal child-destruction.
- Enemy state resets when leaving/returning to a map.
- MapWindow travel and Portal travel use the same travel flow (already true today — both call `MapSystem.TravelTo`).
- Portal back-spawn behavior is preserved.
- Q1–Q12 quest chain, save/account architecture, combat behavior, and the `PortalInteractable` travel-only contract must not be rewritten casually — these are protected systems per `Assets/CLAUDE.md` / `.claude/HANDOFF_CURRENT_STATE.md`.

## 3. Current architecture summary

- Scene has four baked roots: `Map_grassland_1`, `Map_grassland_2`, `Map_town`, `Map_grassland_3`.
- `MapContentController` activates/deactivates these existing roots on `OnMapChanged` — never destroys/instantiates.
- `MapSystem` owns `CurrentMapId`/`PreviousMapId` and `TravelTo`/`RespawnAtDefault`.
- `PortalGate` handles unlock gating off the destination `MapDefinition`.
- `MapWindowUI` handles map point display/travel; `HasMapContent` currently scans `SceneManager.GetActiveScene().GetRootGameObjects()` by name (`"Map_" + mapId`) — this coupling must change when roots stop being permanent scene children.
- `DropManager` parents pooled drops under the `DropManager` singleton, not under any map root.
- Fireball/projectiles spawn with no parent (scene root) — not parented under a map runtime root yet.
- Grassland2 tutorial slime is pre-placed and does not respawn (by design).
- Grassland3 uses `LocalEnemyRespawner` (re-enables the same disabled instance after a delay; demo/local-only, not the final architecture).

## 4. Completed phases

- **Phase 0** — audit/design completed. Full audit covered current systems, save/load restore path, drop/projectile parenting, and produced the phased plan below.
- **Phase 1A** — `Assets/_scripts/World/MapRuntimeContext.cs` (+ `.meta`) created. Foundation/dead-code only:
  - Exposes `DropsRoot`, `ProjectilesRoot`, `VFXRoot` (each falls back to own `transform` if unset).
  - Static `Current`, self-registers `OnEnable`, clears `OnDisable` only if `Current == this`.
  - No existing system references it. No scene/prefab edits. No behavior change.

## 5. Planned phases

- **Phase 1B** — add `Runtime`/`DropsRoot`/`ProjectilesRoot`/`VFXRoot` containers and a `MapRuntimeContext` component to the *current* four scene map roots, scene-only wiring if possible. Do not change `MapContentController` behavior yet.
- **Phase 2** — migrate one map prefab first (`grassland_2` recommended — smallest, single enemy, no respawner), without switching live travel yet.
- **Phase 3** — migrate all four current maps and switch `MapContentController`/new `MapLoader` flow live in `TestCombat`. Requires explicit go-ahead — this touches a protected system.
- **Phase 4** — parent drops/projectiles/VFX under the current map's runtime root at spawn time.
- **Phase 5** — unify enemy spawn points / decide future replacement for `LocalEnemyRespawner` and the unused global `EnemySpawner`. Not currently approved; treat as out of scope until explicitly requested.

## 6. Risk notes

- Do not destroy old scene roots until `MapContentController`/`MapWindowUI` references are fully replaced — a dangling `Root` reference silently no-ops `SetActive`.
- `MapWindowUI.HasMapContent` currently relies on scene roots existing by name; must be swapped to a prefab/lookup-based check before roots stop being permanent scene children.
- `LocalEnemyRespawner` runs coroutines on its host GameObject — destroying that root mid-wait kills the pending respawn silently.
- `DropManager`'s pool currently persists across map switches; if pooled-but-inactive drops end up parented under a map root, destroying that map on travel would shrink/break the pool. Needs a deliberate pool-vs-spawn-parent split (see Phase 0 audit, Phase 4 notes).
- Portal back-spawn (`MapContentController.FindBackPortal`/`SpawnNearPortal`) must be revalidated after any map-load mechanism change — it currently assumes portals are baked children of the activated root.
- Save/load current-map restore (`MapSystem.Initialize` → `OnMapChanged` → `MapContentController.HandleMapChanged`) must be revalidated after any map-load change — confirm it still lands on the correct map/default spawn after a fresh load.

## 7. Future prompt protocol

Every future prompt for this migration must:

- Read `.claude/MAP_PREFAB_MIGRATION.md` first.
- Start by running `git status` and `git diff --stat`.
- Work in one small phase only — do not mix unrelated tasks.
- At the end, update `.claude/MAP_PREFAB_MIGRATION.md` with:
  - phase attempted
  - changed files
  - verification result
  - known risks
  - next recommended step
- Not commit unless explicitly asked.

## 8. Next recommended step

**Phase 1B only:**

- Scene-only or minimal scene wiring.
- Add `MapRuntimeContext` to the current four map roots.
- Create/assign `Runtime` child containers (`DropsRoot`/`ProjectilesRoot`/`VFXRoot`).
- Do not change `MapContentController` behavior yet.
- Verify no gameplay behavior changes.
