# Current developer and AI-agent handoff

**Current source:** Pinefall Wildfront **0.8.0 Fieldcraft**, October 26, 2026. Unity **2022.3.13f1**, NGO **1.8.1**, glTFast **6.0.1**, Windows x64 / Mono / built-in rendering. Protocol **PinefallReserve/8**; all peers must use matching 0.8 builds. Maximum eight players including bots.

## Start here

Read [AGENTS](../AGENTS.md), [setup](SETUP.md), [architecture](ARCHITECTURE.md), [system map](SYSTEMS.md) and [Fieldcraft](FIELDCRAFT.md). The repository root is the self-contained Unity project; open `Assets/Scenes/Reserve.unity`. Use `BuildReserve.BuildCurrent` or `tools/Build-Game.ps1` for ordinary builds. It prepares missing Fieldcraft prefabs/materials and exports handbook JSON without regenerating terrain.

## Current functionality

The 6,144 m square reserve retains 48 named places, 304 buildings, 71 road links, five lakes, 91 vehicles and six core biomes plus autumn presentation. Existing lobby/draft, Solo/Duos/Squads, bots, survival, mods, weather, supplies/surveys and map privacy remain. Fieldcraft adds four building forms/grid editing/doors/support collapse, pickaxe resources/material drops, alpha mutations, sliding/crouch replication, eight weapon classes/RPG, server rewind/anatomical hits, confirmed combat feedback, spectating rules, blue armor pickup, redesigned translucent HUD/panels and squad cards.

Voice now handles actual native capture rate, network packet bursts, sequencing/jitter/loss and device DSP playback. Mic capture is explicitly opt-in; tests use synthetic frames only. The key previous bug was a per-callback 12 ms rejection discarding normal batched 20 ms frames.

## Important implementation contracts

- Damage, construction/material transactions, animal mutation/rewards, vehicles, match and spectator selection are host-controlled. Movement is owner-driven and remains a security limitation.
- `ShotHistory` records raw owner states at their network tick and samples server actors. The render timeline includes NGO interpolation/smoothing. Rewind is bounded at 500 ms; environmental cover remains current. Do not record the server's interpolated remote proxy as raw owner history.
- Trees/rocks use deterministic sparse `HarvestNode` state with references into existing instancing batches. Do not add per-tree network objects/Update loops or lose trunk/crown handles. Late join and round/reset restoration matter.
- Structures use one combined render surface with separate edit collision and door hinge. Do not re-enable the hidden source renderers after previewing. Support collapse uses a spatial grid. Keep 80/player and 512/world bounds unless measured.
- Weapon ID 7 is the launcher; eight IDs fill installed-mod uint nibbles. A ninth entry requires schema migration. Five ammo families now include rockets. Preserve existing IDs and dropped weapon state.
- Enemy HP is a short owner-confirmed, line-of-sight receipt. Ordinary enemy map positions and enemy-occupied vehicle pins stay hidden. Spectator restrictions are server-validated.
- Active UI is in BattleHUD/FieldcraftUI/FieldAtlasPanel. Old implementations retain distinct legacy names; remove them in a behavior-neutral cleanup only after checking call sites.
- Preserve terrain caches, sleeping idle vehicles, bounded distant animals/visibility/model work and hardware-independent settings. Read [performance](PERFORMANCE.md) before touching hot paths.

## Evidence and remaining work

[0.8 verification](VERIFICATION-0.8.md) records the final build checks, native captures and measured route. Historical 0.7.1 reports remain untouched under `tests/baselines/0.7.1`. They are not evidence of new code. New checks are portable through `Test-Fieldcraft.ps1` and `Test-Visual.ps1`, alongside the full legacy suite and performance route.

The requested mechanics are playable development implementations, with original geometry/UI, not commercial-game asset/animation or full editing-pattern parity. Static villages are not wholly destructible. Better authored motion/art, build replacement contests/advanced edits, heavily saturated construction, longer human/network soaks, lower-end hardware and public-internet microphone playtests remain worthwhile next steps. No real microphone, account backend, hosted relay, pilotable aircraft or competitive anti-cheat is implied.

## Safe continuation

Start from the versioned evidence and reproduce a focused issue before changing shared partials. Preserve GUIDs, network prefab registration, packed layouts and reset/rehost paths. Run full multiplayer/lifecycle checks for protocol/shared changes, plus native rendering for UI/audio work. Keep builds/caches/logs/private IPs out of Git. No project-wide open-source license has been chosen; retain notices.
