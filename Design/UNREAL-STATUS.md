# Unreal implementation and verification status

October 28, 2026. Native 0.2 round working tree; **not full Unity parity or a finished visual release**.

## Implemented first passes

Unreal 5.6.1 runtime/editor modules build with Visual Studio 2022. Cedar Crossing remains a 504 m Landscape with 480 m playable bounds. It is a small migration test arena, not the six-biome reserve.

- Native host/join, twenty-player admission limit, solo/duo/squad team sizes, readiness/countdown cancellation, ten-point loadout/landing bidding, individual turn-based landing, cyclic storm, victory/results and return to camp.
- Owner-only twelve-slot inventory, finite battle ammunition, food/hunger/stamina, three armor tiers, medkits, plates, validated dropping/pickup and stale-click rejection. Two actual weapons and two kits; other source definitions are reference data.
- Team downing, held nearby revival, elimination loot, ragdoll presentation and team-restricted spectator selection until a team wipe. Range mode retains respawns and unlimited reserve ammunition.
- Seven animated animals: deer, stag, fox, wolf, bull, horse and alpaca. Peaceful prey flees; wolves attack. Closing storms enrage wildlife and can produce larger alpha variants. Food and ordinary equipment rewards use server-owned transactions. These models are temporary Quaternius art, not realistic replacement assets.
- Cached/budgeted wildlife decisions, sleeping far movement and preserved collision. Direct steering is an initial implementation; complex obstacle navigation still needs work.
- Smooth aim/FOV and sensitivity transitions, weapon kick/reload pose, recorded gunfire, quieter varied footsteps, positional animal calls, original hit/armor/elimination cues. Accepted damage produces colored hit markers, short-lived visible-target health bars, damage direction and blue armor/red health feedback. Equipping armor does not invoke damage feedback.
- Right-side inventory/draft panels, survival bars, minimal team information and a terrain atlas with storm/landing/team markers. Ordinary enemy tracking remains disabled.

The scope mask is drawn below foreground HUD elements. Visual checks and multiplayer checks are separate gates; a screenshot injected with the opt-in armor presentation probe does not prove actual damage.

## Verification

Current evidence and source fingerprints are in [the 0.2 checkpoint](../tests/baselines/unreal-0.2/README.md): successful native compilation, five remote scoped-fire checks, 36 round checks, fifteen wildlife checks and three remote skeletal spawns. Scope, armor cue and inventory were rendered/reviewed at 1600x900; the round lobby was reviewed at 1280x720. Audio forced reimport passed without errors or decoder ensures. Historical M0 evidence in `unreal-0.1` remains unchanged. Current checks are `tools/Build-Unreal.ps1`, `tools/Test-Unreal.ps1`, `tools/Test-UnrealBattle.ps1` and `tools/Test-UnrealWildlife.ps1`.

The round probe uses three real processes and owning-client lobby/draft commands; it advances clocks and arranges damage on authority for deterministic rule checks. The wildlife probe uses two processes and checks remote skeletal spawning plus arranged AI/reward cases. Neither proves moving-player hit fairness, WAN operation, microphone quality, audible mix, graphics performance or twenty-player load. These remain explicit open gates.

## Remaining migration and presentation work

Vehicles/boats/swimming, biome-specific ecology, the large reserve/villages/caves, construction/editing/harvesting, launchpads, sliding/bunny-hop, attachments and the full weapon set, customization, weather/day cycle, narrator/emotes and proximity voice remain to migrate. The lake currently has visual water only. No Unreal microphone capture is implemented.

First-person hands/IK, proper weapon reload animations, muzzle/surface-specific effects, moving-player latency compensation, production wildlife models, dense realistic scenery, map zoom/minimap, teammate state beyond pawn relevance, full key rebinding and packaged standalone distribution also remain. Do not advertise this working tree as finished or label placeholder art as a realism upgrade.

Read [the native handbook](../Unreal/Pinefall/PLAYING.md), [migration plan](UNREAL-MIGRATION.md), [presentation direction](UNREAL-PRESENTATION.md) and [source-linked rules](RULES-CATALOG.md) before continuing. Unity remains frozen.
