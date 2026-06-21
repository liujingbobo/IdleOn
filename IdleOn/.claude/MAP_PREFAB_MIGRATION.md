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
- **Phase 1B** — `Assets/_scenes/TestCombat.unity` only. Added a `MapRuntimeContext` MonoBehaviour component to each of the four map roots (`Map_grassland_1`, `Map_grassland_2`, `Map_grassland_3`, `Map_town`), plus a new `Runtime` child Transform under each root containing `DropsRoot`/`ProjectilesRoot`/`VFXRoot` empty child Transforms. Each root's `MapRuntimeContext` has its `dropsRoot`/`projectilesRoot`/`vfxRoot` fields assigned to the matching new child. No existing GameObject (floor/portals/enemies/spawn points/visuals) was moved, renamed, or reparented; no `m_IsActive` flags changed; no code files touched. fileIDs used: `9100001000001`–`9100001000009` (grassland_1), `9100002000001`–`9100002000009` (grassland_2), `9100003000001`–`9100003000009` (grassland_3), `9100004000001`–`9100004000009` (town) — verified unique within the scene (112 occurrences = 4 × 28 references, no collisions). `git diff --stat` shows only `TestCombat.unity` changed, 576 insertions, 0 deletions.
  - Verification done: confirmed all four `MapRuntimeContext` components reference the correct root GameObject and the correct three child Transforms; confirmed `Map_grassland_3`'s existing `LocalEnemyRespawner` component (fileID `965281053`) was left untouched, only appended after in the root's `m_Component` list. Verification NOT done (requires Unity Editor, unavailable in this session): opening the scene in-editor to confirm no Missing Script/Missing Reference errors, and a live play-through of map travel/portal back-spawn/Grassland2 tutorial slime/Grassland3 respawn. Since nothing references `MapRuntimeContext` yet and no existing component/hierarchy was altered, behavior risk is low, but in-editor open + play-test is still recommended before treating this phase as fully verified.
  - Known risks: none introduced to existing systems (additive-only change — new components/GameObjects, no existing references altered). Residual risk is purely "unverified in Editor" rather than "known to break something."
  - Not committed — left as a working-tree change per instructions.

- **Phase 2A** — created `Assets/_assets/Prefabs/Maps/Map_grassland_2.prefab` from the existing `TestCombat` scene root `Map_grassland_2` (instanceID 264208), via `manage_prefabs.create_from_gameobject`. Prefab-extraction only, no loader wiring.
  - Tool note: `create_from_gameobject` requires the source GameObject to be locatable by `GameObject.Find`, which does not see inactive roots. Worked around by temporarily setting `Map_grassland_2.activeSelf = true`, running the prefab creation, then setting it back to `false` immediately after. Verified the original scene root (instanceID 264208) was preserved unchanged afterward — same instanceID, same 4 children, `activeSelf` restored to `false`. `MapContentController.maps[1].Root` still resolves to instanceID 264208 (unaffected).
  - This active-toggle round-trip (plus Unity's internal prefab-instance link bookkeeping during creation) left `TestCombat.unity` dirty in the open Editor session. **Not saved** — `git status`/`git diff --stat` confirm the on-disk scene file is untouched; only the new prefab folder is untracked. The in-memory dirty flag will clear on next scene reload/discard and was never written to disk.
  - Prefab contents verified (via `open_prefab_stage`, read-only, closed without saving): root `Map_grassland_2` has `MapRuntimeContext` with `dropsRoot`/`projectilesRoot`/`vfxRoot` correctly pointing to its own `Runtime/DropsRoot`/`ProjectilesRoot`/`VFXRoot` children. Both portals (`Portal_back_to_grassland_1`, `Portal_to_town`) carry `PortalInteractable`+`PortalGate`, with `PortalGate.visual`/`portalCollider`/`requirementText` correctly self-referencing their own `Visual`/`RequirementText`/collider children — these came in as nested prefab instances of `Portal.prefab`. `Slime_g2` (nested instance of `Slime.prefab`) has all 8 expected components; `EnemyHealthBar.barRoot`/`backgroundRenderer`/`fillRenderer` correctly point to its own `HealthBar`/`Background`/`Fill` children. No Missing Script / Missing Reference anywhere in the prefab (16 objects total).
  - `MapDefinition` (`MapDef_Grassland2.asset`) was **not** touched — no `mapPrefab` field added, no reference to the new prefab anywhere yet. `MapContentController` still drives the map exclusively from the scene-baked root. Current gameplay/travel behavior is unchanged.
  - Known risks: none introduced to existing systems — extraction only, scene root preserved, no MapContentController/MapDefinition wiring added. Residual note: the open Editor session currently shows `TestCombat.unity` as dirty (harmless, unsaved); if the Editor is later saved for an unrelated reason before this is reviewed, double-check no unintended scene state lands in the commit.
  - Not committed — new prefab + meta files left as untracked working-tree additions.

## 5. Planned phases

- ~~**Phase 1B**~~ — done, see section 4 above.
- ~~**Phase 2A**~~ — done, see section 4 above.
- **Phase 2B** — wire the new prefab into `MapDefinition`/`MapContentController` loading path (not yet started — explicit go-ahead required, touches a protected system).
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

**Before Phase 2:** open `TestCombat` in Unity Editor and confirm no Missing Script/Missing Reference errors, then play-test map travel, portal back-spawn, Grassland2 tutorial slime, and Grassland3 respawn to close out Phase 1B verification.

**Phase 2A:** done — see section 4. `Map_grassland_2.prefab` exists and is verified clean, not yet wired into loading.

**Phase 2B:** decide and implement how the prefab gets used at runtime (e.g. add `MapDefinition.MapPrefab` field + a load path in/alongside `MapContentController`), without removing the scene-baked roots yet. Requires explicit go-ahead before touching `MapContentController`/`MapDefinition`.
