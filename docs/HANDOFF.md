# Current developer and AI-agent handoff

**October 29, 2026: active direction is Unreal migration. Unity work is frozen.** Read [Unreal migration](../Design/UNREAL-MIGRATION.md), [status/remaining gates](../Design/UNREAL-STATUS.md), [art/audio/UI direction](../Design/UNREAL-PRESENTATION.md), [rule catalog](../Design/RULES-CATALOG.md) and [setup/source boundaries](../Unreal/Pinefall/README.md). The native project initially compiles; runtime and presentation checks have separate gates. Do not interpret the legacy feature list below as implemented Unreal functionality.

The final Unity 0.9.1 suite was cancelled on the user's switch request. Prior frontier/fieldcraft and performance evidence exists, but there is no completed final-build full-suite claim. Do not relabel older measurements with a newer build hash. Preserve unfinished Unity work without resuming it.

## Unreal 0.3.1 traversal checkpoint

`PFMovementComponent` owns fast-crouch sliding and chained-hop momentum. Compressed custom input flag 0 is sprint; flag 1 is held hop. `FSavedMove_PF` snapshots slide/landing/flight/previous-crouch/momentum state for replay and prevents combining traversal moves. Server derives speed/eligibility through the same movement hooks; clients do not submit speed claims. `APFCharacter::CheckJumpInput` repeats a held jump only on real ground and tries a collision-checked uncrouch. Death/downed/vehicle/round restrictions still reject jumps. `bSliding` skips owner replication because owner presentation is predicted; peers receive authority's state. Matching 0.3.1 builds are required.

Air crouch preserves horizontal speed. `ProcessLanded` enters a buffered slide before remaining landing physics can clamp it to crouch-walk speed. Normal sprint is 940 cm/s, hop gains 170 cm/s per landing within 150 ms up to 2,400, slide limit 2,600. Walking steps are suppressed during slides; aiming overrides the modest speed FOV. Dedicated slide animation/foley remains first-pass work, not finished art.

`PFMovementProbe` is opt-in only. It creates identical local static floor/low-ceiling geometry, waits for client-owned-pawn readiness after travel and acknowledges synchronized fixture teleports/posture before advancing input cases. A locally spawned movable platform has no shared network identity: relative movement corrections used the wrong client height. Keep fixture basing static and verify `PF_MOVEMENT_CLIENT_ON_FIXTURE` plus the rendered floor. Count accepted server takeoffs rather than sampled air/ground transitions, which miss very short landings. `Test-UnrealMovement.ps1 -Rendered` captures actual owned sliding stance. Headless delay checks, actual rendering and broad terrain/frame-rate/audio/WAN claims are separate gates.

See [the 0.3.1 checkpoint](../tests/baselines/unreal-0.3.1/README.md) for the completed final-build evidence. Earlier failed development runs stay in ignored artifacts. The full Unity game remains migration work.

## Active Unreal 0.3 reserve

Read [native status](../Design/UNREAL-STATUS.md), [controls/rules](../Unreal/Pinefall/PLAYING.md) and [hit registration](../Design/HIT-REGISTRATION.md). Active map is WildfrontReserve: 2,016 m, eight named settlements/64 enterable cabins, connected graded roads, 208 supplies, sixteen trucks/three boats. Cedar Crossing remains the separate 504 m regression arena. This is not full Unity parity.

`PFReserveLayout` is shared layout/height data for editor generation and map bounds. Height synthesis stays out of runtime movement. `PFReserveBuilder` generates Landscape/HISM foliage/roads with real preserved collision. `PFReserveWorld` runs population only on authority, explicitly excludes client net mode and sends owner-only team/vehicle map snapshots. Never expose enemy-occupied vehicle pins or ordinary enemies. Atlas is a 2K height/road/roof/tree bake, not an ortho capture. Wildlife cap is 84 here, 28 in Cedar; distant work remains bounded.

`PFVehicle` is a first single-seat, server-simulated kinematic truck/boat with boost/brake, free look, safe exits, fuel/condition and service kits. Empty vehicles sleep. Clients interpolate replicated transforms and send driving through their owned character. The vehicle ignores its attached driver during sweeps. No passenger seats, Chaos suspension, wheel animation or final engine audio yet. `PFCharacterVehicle` handles replicated seat presentation, death-safe exit and validated service transactions. New appended item `ServiceKit` requires matching 0.3 peers; do not promise older client compatibility.

The user's current character direction is fun/stylized, not hyperreal. Active canonical mesh/animations: `/Game/Pinefall/Art/Characters/TrailRanger`. Reuse credited Quaternius inputs from `Assets/Art/Characters` without modifying Unity. Optional source rebuild: `merge_stylized_ranger.py`, Blender `prepare_stylized_ranger.py`, editor `import_stylized_ranger.py`, then `fix_stylized_materials.py`. Normal builds use committed assets. Seventeen clips include swimming and original combat/crouch poses. `PFRangerAnimInstance` blends the weapon upper pose and matches crouch movement rate. Crouch uses 35 cm pelvis lowering/back shift with ankle rotation constraints, 72 cm capsule half-height and 55 cm camera/shot offset. `PFRangerAssetTools` creates missing neck/head bodies and fits them during import. The earlier MakeHuman-derived ranger is historical; don't reactivate it accidentally. Asset reimport preserves stale sibling clips even if they were deleted individually; explicit refresh replaces the entire generated folder, so preserve authored consumers first.

Reserve authoring: optional Blender `prepare_reserve_props.py`, then editor `build_reserve.py`, Python `bake_reserve_atlas.py` (NumPy/Pillow), editor `import_reserve_atlas.py`. Setup supports `-RebuildReserve` and `-RebuildCharacterMaterials`. Explicit keyword Rotator arguments avoid Python/C++ argument-order differences. Blender +Y exports to UE -Y: cabin entrances face local -Y, vehicle body relative yaw +90. Generated HISM materials require instanced-static-mesh usage. Forced mesh save is required after material-array writeback.

`PFHitHistoryComponent` retains sixteen 30 Hz exact collision samples, maximum 350 ms. Shots send the rendered target movement timestamp; current cover wins. Never move live physics during rewind. The clock fallback remains measured; no fixed smoothing subtraction. New character geometry must pass the actual delayed host/client combat probe. See the hit-registration design for WAN/jitter, death ordering and anti-cheat limits.

Verification is tracked separately from implementation in `tests/baselines/unreal-0.3`. `Test-UnrealReserve.ps1` exercises real remote drive/steer, pause-menu motion, repair/refuel, boat motion, destructive exit, inventory transfers, privacy/bootstrap and replicated crouch/uncrouch. Presentation hooks (`PFCombatView`) are opt-in captures only, not combat evidence. No microphone capture was enabled. Keep rendering/imports separate from latency/performance evidence. No rendered-FPS or public-internet performance claim is made for this sweep.

Remaining priorities: finished art/hand grips and cabin variety, biome ecology/navigation, passenger/suspension/audio, full Unity arsenal/attachments, building/editing/harvesting/pads, dedicated slide animation/foley, voice, customization, weather/day cycle, narrator/emotes, caves and lobby-sized world crops. Do not label the 120-rule catalog complete.

## Frozen Unity handoff

**Current source: Pinefall Wildfront 0.9.1 Handling and Presentation, October 27, 2026.** Unity 2022.3.13f1, NGO 1.8.1, glTFast 6.0.1, Windows x64 / Mono / built-in rendering. Protocol PinefallReserve/9 requires matching 0.9 builds. Maximum twenty participants including bots.

Read AGENTS.md, SETUP.md, ARCHITECTURE.md, SYSTEMS.md, LIVING-WILDFRONT.md and TESTING.md. The repository root is the self-contained Unity project. Open Assets/Scenes/Reserve.unity. Ordinary builds use BuildReserve.BuildCurrent; Setup regenerates terrain/content. Existing asset GUIDs must be preserved.

## Current follow-up

Read [0.9.1 changes](HANDLING-PRESENTATION.md) for camera-aware snap selection, continuous-grade vehicles/host render interpolation, cached ground-item models, pickup cards, wildlife replenishment, anomaly caches and lighting/material adjustments. `LootModels` and `FieldAnomaly` are new domains. The existing protocol remains 9; no RPC/NV/prefab layout changed. Match peers should use the same 0.9.1 build. The previous closeout below describes historical 0.9 evidence, not current test results.

## What changed

The full reserve now has 60 named sites (14 major, 46 minor), 444 buildings, 89 road links, 44 service stops, 112 starting vehicles and 10 boats. Six connected cave systems use winding passages, branch chambers and a loop, with terrain-integrated mouths, caches and crystal illumination. Stronger mountain/mesa elevation is intentional; some natural slopes are steep.

ArenaTier locks at draft start: 1â€“5 participants use a 2,048 m crop, 6â€“12 use 4,096 m, 13â€“20 the full 6,144 m. Practice fills to eight; bots count. Free roam stays full size. Static world geometry is shared; loot/population/spawns/map/storm obey the arena bounds. This is not per-lobby terrain streaming.

The pickaxe and binding occupy the first hotbar tile; four structure bindings appear above guns. Hold Alt opens inventory and release closes it; custom bindings are retained. Connected builds inherit GridYaw/GridOrigin and snap in a player-relative frame, with automatic quarter turns and manual R offsets. Vertical cells are 3 m. Original editing/support limits remain 80 per builder and 512 globally.

LaunchPad is a registered network prefab. Fixed pads have map pins; loot kind 14 gives floor-mountable pads (carry three, F5). The server validates complete allied support, proximity, duplicates and cooldowns; the owner receives launch motion and replicates GlideState. Removing/editing the floor removes its pad. Supply relays use kind 15 and a 180-second shared cooldown.

**Explicit user override:** animal kills may award ordinary usable equipment. AnimalLoot uses 35% ordinary / 80% enraged extra-drop chances; bosses guarantee two rolls. Do not introduce pelts, bones, trophies or crafting. WildlifeHitboxes fits renderer-local model bounds once, then records oriented extents in ShotHistory. Preserve current-cover checks and the bounded remote rewind timeline.

LowGroundcover keeps a bounded 48 m-cell cache around the camera and draws short scanned grass in three detail levels. LivingForest uses scanned needle atlases, three distance meshes and existing harvest handles. SettlementDetail swaps only rendering to distant silhouettes; collision/gameplay persist. Default view distance is 900 m, configurable 750â€“1,800; adaptive detail stays hardware-agnostic. CombatEffects pools particles and tracers. No per-tree network actors or Updates.

FrontDoorUI supplies the character-focused Play screen, hosting, joining, discovery, roster and ready flow. CharacterPreview renders at most 30 times per second. Text uses actual font line height; handbook content height is measured and scrollable. Map-label styles are reused rather than adding new GUIStyle keys every repaint. --layout-audit 1 logs measured overflow during explicit visual checks.

RangerEmotes and EmoteWheel add six original skeletal gestures with server-authorized ID/start/expiry variables. Hold C, move direction and release to choose; center release/right click/Esc cancel. Left click also commits, and the click frame cannot fire a weapon. Movement, combat, damage, vehicles and expiry cancel. Local emotes use third person; nearby peers see the same server timeline. No root motion or invulnerability. Appended controls still fit two twenty-row columns.

Thirty original announcements use offline Kokoro af_heart WAVs and bounded prioritized event queues. Generator/model are not bundled. Physical microphone capture remains opt-in; synthetic test frames must never enable it.

Bunny-hop movement uses a 150 ms jump buffer and 100 ms edge grace. Hold jump or time landings; consecutive hops add 1.7 m/s up to 24 m/s. Fast crouch slides carry up to 26 m/s; ordinary sprint remains 9.4 m/s and hunger still limits speed. Momentum is cleared on respawn/teleport, water, vehicles and pad launch. SmokeBunnyHop is an explicit probe-only controller-input override.

## Contracts to preserve

- Host controls damage, rewards, construction, inventory, match and vehicles. Owner movement remains an anti-cheat limitation.
- Protocol review is mandatory for NV/RPC/layout/prefab/item changes. Pending approval IDs reserve capacity before player spawning.
- Ordinary enemy map positions and enemy-occupied vehicle icons stay private. Fixed pad pins do not expose an opponent's built pads.
- Cave mesh/ground queries share CaveSite profiles; underground drops and rescue/build roots must remain underground. Regenerate scene/road coverage and rebake the 4K atlas after geography edits.
- Keep cached terrain sampling, sleeping vehicles, bounded AI/visibility work and reset/rehost cleanup. Visual culling cannot remove authoritative collision.
- Current panels live in FrontDoorUI, BattleHUD, FieldcraftUI and FieldAtlasPanel. Legacy methods have distinct names; do not rewire them accidentally.
- Eight weapon IDs fill packed attachment nibbles. A ninth needs migration. Preserve dropped magazines/mods, source-specific armor and stance hitboxes.

## Previous 0.9 closeout status

The Windows candidate compiled successfully. See [0.9 verification](VERIFICATION-0.9.md) and its raw evidence before making any final-build test claim. Final full-detail graphics performance is approximately 56â€“74 FPS with HUD, below the historical 0.8 route. One hunger test fixture was corrected but not rerun; the latest render-tuned visual sequence was incomplete. Prior complete UI audits and movement/multiplayer checks are preserved separately. The user requested compile and push without another full sweep. Performance and final verification remain the first follow-up work.

## Verification and next work

Run validate_repository.py, Test-Game.ps1, Test-Fieldcraft.ps1, Test-Frontier.ps1, Test-Visual.ps1 and Measure-Performance.ps1. Keep editor builds and test groups sequential when checking startup/performance; never overwrite a player directory that a probe is using. Each wrapper owns its own processes and creates unique evidence folders. Historical 0.7.1 and 0.8 baselines remain unchanged.

The game remains a playable development release. Full commercial-game edit-pattern parity, wholly destructible settlements, production character animation/art, twenty-human long matches, low-end hardware and public-internet microphone listening require further work. There is no account backend, hosted relay, pilotable aircraft, full terrain destruction or competitive anti-cheat. A twenty-participant simulation/approval check does not prove twenty independent internet clients or their frame rate.
