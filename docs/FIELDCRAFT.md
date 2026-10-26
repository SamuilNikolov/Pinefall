# Fieldcraft systems — 0.8

This update retains the 6,144 m reserve and 4K atlas while adding construction, improved combat/voice and clearer presentation. Unity stays pinned to 2022.3.13f1. **Protocol 8 requires matching 0.8 peers.** The four construction forms and cell edits are original Pinefall implementations inspired by familiar battle royale mechanics; full Fortnite pattern/animation parity is not claimed.

## Construction and harvesting

| Action | Default input |
| --- | --- |
| Pickaxe / return to weapon | Backquote / select a weapon |
| Build mode / shape | Z / F1 wall, F2 floor, F3 stairs, F4 pyramid |
| Place repeatedly / material / rotation | Hold LMB / RMB / R |
| Edit targeted allied piece | H |
| Edit cells | Paint the world grid or side panel |
| Confirm / reset / cancel | H / RMB / Escape |
| Door / repair | E / G |

`RangerConstruction` handles local preview/input and validated owner RPCs. `ConstructionWorld` owns transactions and sparse resource changes; `BuildPiece` owns replicated shape/material/mask/HP/team/door state. Prefabs are registered explicitly by `ReserveSession`; `BuildFieldcraft` prepares missing resource assets without regenerating the scene.

- Start with 80 wood, 40 stone, 20 metal; each stack caps at 999. Ten matching materials buys a piece. Repair spends two matching materials for up to 50 HP. Vehicle repair salvage (`Craft`) remains separate.
- Existing deterministic foliage colliders map to `HarvestNode` entries. A node retains every instanced trunk/crown/rock matrix handle; zeroing a destroyed node disables its collider and every visual handle. No network object or Update is added per tree.
- Pickaxe swings have server range, state and cadence checks. Harvest payout is based on actual lost HP (about one material per three HP); repeated swings against depleted nodes cannot duplicate rewards. Gunfire/explosions destroy nodes without granting materials.
- Only changed resource HP is networked reliably, in late-join chunks of 64. Round/session reset restores nodes. Material loot has kind 13 and can be collected, dropped and shared.
- Horizontal cells are 4 m; pieces are 3 m high. Root height snaps to 0.75 m for uneven terrain, with ground/static-structure support; stacking rises by 3 m. Server checks bounds, 12 m range, cost, duplicates, obstructions and relevant player overlaps. Limits: 80 pieces per builder, 512 world-wide.
- Pieces begin at 80 HP, growing during their first six seconds toward 300 wood / 450 stone / 600 metal. Editing preserves HP. Destroying a support schedules a bounded spatial-grid support traversal and collapses unsupported pieces.
- Walls use connected 3×3 masks with windows/doors/low/diagonal forms. Floors/stairs use 2×2 masks; pyramid corner heights use four bits (zero is the full pyramid). Stair/pyramid edits can rotate in place. The side panel and projected grid share the same mask/commit path. Some advanced bent-stair patterns, trapped-player checks for every possible ramp/cone edit, and building replacement contests remain future work.
- Static houses are cover and attachment surfaces; this update does not make every authored house destructible. Trees, rocks, player builds, actors and vehicle condition have their own damage paths.

## Combat, damage and player visibility

The previous gun path used current physics against trigger-only remote players while clients saw interpolated poses. `RangerCombat`, `ShotHistory` and `PlayerHitboxes` now validate and resolve shots server-side against bounded historical anatomical regions. History queries never relocate live colliders.

Remote owner transforms are recorded at the received NGO network tick using full state position/yaw, rather than recording the host's interpolated proxy. Host/bots/animals are sampled on FixedUpdate. NGO 1.8.1 shows owner-authoritative remote players at two ticks behind server time on clients, one on the server, plus its second interpolation smoothing. The shot stamp/aim state/bounded smoothing allowance identifies that rendered timeline. History is 32 samples with a maximum 500 ms rewind; discontinuous teleports clear or split history.

Validation includes finite normalized vectors, weapon ownership/state, ammo, bounded origin/time, increasing sequence, timestamp cadence and a burst token budget. Current world cover still blocks hits. Destructible cover and vehicle positions are not historically rewound. Owner-driven player movement remains a trust/security limitation.

Player regions include a head-sized capsule, torso, arms, separated legs and stance/yaw adjustments for crouching, sliding and downing. They are an approximation of the animated silhouette, not per-triangle skin collision. Confirmed owner-targeted receipts drive damage numbers, armor/flesh/kill sounds and hit markers. Enemy HP appears briefly after damage, within range and line of sight; it is not a pre-damage locator. Healing/equipment changes do not emit damage revisions: vest pickup instead pulses blue. The cloth shader modestly lifts local contrast without emission, outlines or seeing through cover.

| Class | Weapon | Mode |
| --- | --- | --- |
| Sniper | Ridgeline | Bolt-action |
| Assault rifle | Trail Scout | Automatic |
| Shotgun | Brushmaster | Pump, eight pellets |
| Pistol | Service Nine | Semi-auto |
| SMG | Kestrel | Automatic |
| Marksman rifle | Longwatch | Semi-auto |
| Support gun | Sawmill | Automatic |
| Launcher | Stormbreak RPG | Single traveling rocket |

There are five ammo families. Close-range classes have stronger falloff. Rockets move at 44 m/s, last 6.7 seconds and apply a 6 m falloff blast with cover protection, structure damage and reduced self-damage. Rockets are uncommon loot, never animal rewards. Only the speed loader fits the launcher. Existing IDs 0–6 remain stable; launcher ID 7 fills the eighth installed-mod nibble. **A ninth gun requires a packing/protocol migration.**

## Motion, animals and spectators

`RangerMovement` synchronizes stance collider/controller height and visible pose independently of local input. Faster 9.4 m/s sprint drains less stamina; a 1.35-second slide carries bounded momentum. Menus stop input, while gravity/swimming/shared simulation continue. Cached stance bone poses and two-bone weapon grip IK improve remote crouch/held-tool appearance; these do not replace a complete authored animation system.

Vehicle speeds increased, steering remains speed-sensitive, and boats query the water surface without treating lake/river height seams as land grades. Shallow/dry ground remains a constraint.

`StormMutation` rolls once per nearby land animal per closing cycle (12%, within 600 m of active players, cap four living bosses). A boss grows 1.7×, gains two additional base HP pools, 1.15× movement and 1.7× attack. It persists until killed; extra rewards are food and restorative meat. Sharks are excluded.

`RangerSpectator` validates selection server-side. Solo may watch anyone alive; Duos/Squads watch surviving teammates until the team is eliminated, then anyone alive. Downed teammates still count as surviving. Death/target loss automatically selects an eligible target. Camera and `WorldInterest` use the selected target's viewpoint. Dead players cannot use this to spectate enemies while their team remains active.

## Voice

The earlier 12 ms per-message guard discarded valid 20 ms audio frames when NGO delivered several callbacks together. Receive overflow also cleared whole queues. The replacement uses:

- Actual microphone frequency/channels, usually 48 kHz native capture, resampled into 20 ms 16 kHz mu-law frames.
- Warm capture only after explicit enabling; PTT gates transmission without restarting the device. Open mic has a smoothed envelope and 300 ms release.
- Sequenced packets and a 70-frame/s token budget with room for ordinary bursts, instead of individual arrival-time rejection.
- Preallocated receive slots, an 80 ms start buffer, reordering/duplicate handling, loss concealment and old-backlog trimming.
- `OnAudioFilterRead` device-block playback with continuous device-rate resampling, avoiding dependence on long streamed-clip callbacks. Audio-thread access is synchronized and uses preallocated buffers.
- Existing 75 m server routing/falloff across teams; reset unregisters handlers and destroys receivers. No automation enables or records a real microphone.

Synthetic network bursts and deterministic playback tests exercise transport and buffering; real microphone/device quality and public-internet loss remain separate human playtests. This is a simple voice codec, not Opus or an echo-cancellation service.

## Presentation and extension boundaries

Active panels are in `BattleHUD`, `FieldcraftUI` and `FieldAtlasPanel`. They keep survival/squad status left and weapons/supplies right, with translucent inventory/settings, icons, class/mod details and a world edit grid. Duo/Squad cards show health, armor, distance and speaking; they remain visible on the map. The map retains its real 4K terrain raster, zoom, labels, privacy and live transport feed. Legacy panel methods have distinct unused names pending a separate cleanup; do not accidentally rewire them.

Run `Test-Game.ps1`, `Test-Fieldcraft.ps1`, rendered `Test-Visual.ps1` and the graphics-enabled performance route for shared changes. See [0.8 verification](VERIFICATION-0.8.md) for actual results, not a blanket production guarantee.
