# Current developer and AI-agent handoff

**Current source: Pinefall Wildfront 0.9.0 Living Wildfront, October 26, 2026.** Unity 2022.3.13f1, NGO 1.8.1, glTFast 6.0.1, Windows x64 / Mono / built-in rendering. Protocol PinefallReserve/9 requires matching 0.9 builds. Maximum twenty participants including bots.

Read AGENTS.md, SETUP.md, ARCHITECTURE.md, SYSTEMS.md, LIVING-WILDFRONT.md and TESTING.md. The repository root is the self-contained Unity project. Open Assets/Scenes/Reserve.unity. Ordinary builds use BuildReserve.BuildCurrent; Setup regenerates terrain/content. Existing asset GUIDs must be preserved.

## What changed

The full reserve now has 60 named sites (14 major, 46 minor), 444 buildings, 89 road links, 44 service stops, 112 starting vehicles and 10 boats. Six connected cave systems use winding passages, branch chambers and a loop, with terrain-integrated mouths, caches and crystal illumination. Stronger mountain/mesa elevation is intentional; some natural slopes are steep.

ArenaTier locks at draft start: 1–5 participants use a 2,048 m crop, 6–12 use 4,096 m, 13–20 the full 6,144 m. Practice fills to eight; bots count. Free roam stays full size. Static world geometry is shared; loot/population/spawns/map/storm obey the arena bounds. This is not per-lobby terrain streaming.

The pickaxe and binding occupy the first hotbar tile; four structure bindings appear above guns. Hold Alt opens inventory and release closes it; custom bindings are retained. Connected builds inherit GridYaw/GridOrigin and snap in a player-relative frame, with automatic quarter turns and manual R offsets. Vertical cells are 3 m. Original editing/support limits remain 80 per builder and 512 globally.

LaunchPad is a registered network prefab. Fixed pads have map pins; loot kind 14 gives floor-mountable pads (carry three, F5). The server validates complete allied support, proximity, duplicates and cooldowns; the owner receives launch motion and replicates GlideState. Removing/editing the floor removes its pad. Supply relays use kind 15 and a 180-second shared cooldown.

**Explicit user override:** animal kills may award ordinary usable equipment. AnimalLoot uses 35% ordinary / 80% enraged extra-drop chances; bosses guarantee two rolls. Do not introduce pelts, bones, trophies or crafting. WildlifeHitboxes fits renderer-local model bounds once, then records oriented extents in ShotHistory. Preserve current-cover checks and the bounded remote rewind timeline.

LowGroundcover keeps a bounded 48 m-cell cache around the camera and draws short scanned grass in three detail levels. LivingForest uses scanned needle atlases, three distance meshes and existing harvest handles. SettlementDetail swaps only rendering to distant silhouettes; collision/gameplay persist. Default view distance is 900 m, configurable 750–1,800; adaptive detail stays hardware-agnostic. CombatEffects pools particles and tracers. No per-tree network actors or Updates.

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

## Closeout status

The Windows candidate compiled successfully. See [0.9 verification](VERIFICATION-0.9.md) and its raw evidence before making any final-build test claim. Final full-detail graphics performance is approximately 56–74 FPS with HUD, below the historical 0.8 route. One hunger test fixture was corrected but not rerun; the latest render-tuned visual sequence was incomplete. Prior complete UI audits and movement/multiplayer checks are preserved separately. The user requested compile and push without another full sweep. Performance and final verification remain the first follow-up work.

## Verification and next work

Run validate_repository.py, Test-Game.ps1, Test-Fieldcraft.ps1, Test-Frontier.ps1, Test-Visual.ps1 and Measure-Performance.ps1. Keep editor builds and test groups sequential when checking startup/performance; never overwrite a player directory that a probe is using. Each wrapper owns its own processes and creates unique evidence folders. Historical 0.7.1 and 0.8 baselines remain unchanged.

The game remains a playable development release. Full commercial-game edit-pattern parity, wholly destructible settlements, production character animation/art, twenty-human long matches, low-end hardware and public-internet microphone listening require further work. There is no account backend, hosted relay, pilotable aircraft, full terrain destruction or competitive anti-cheat. A twenty-participant simulation/approval check does not prove twenty independent internet clients or their frame rate.
