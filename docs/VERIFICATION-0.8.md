# Fieldcraft 0.8.0 verification — October 26, 2026

The release was compiled in Unity **2022.3.13f1**, Windows x64 / Mono. Approval/discovery uses protocol **8**, requiring matching builds. The final build completed successfully; generated prefabs/materials and stable metadata are committed. Terrain and atlas were retained because geography did not change.

## Actual gameplay results

| Run | Reports | Passing assertions | Failures / captured runtime errors |
| --- | ---: | ---: | ---: |
| Full existing gameplay suite | 10 | 146 | 0 / 0 |
| New Fieldcraft host, ally, enemy, late join | 4 | 73 | 0 / 0 |
| Total | 14 | **219** | **0 / 0** |

The final runs are preserved in [0.8 evidence](../tests/baselines/0.8.0/README.md), alongside runtime-assembly/executable hashes. Earlier failing development runs were retained locally, not substituted for these reports. One support-collapse fixture initially left an adjacent foundation from an unsuccessful placement attempt, legitimately supporting the tower; the isolated fixture now verifies collapse after removing its sole support.

New coverage includes building cost/duplicates/support, actual walking up stairs, window collision, allied/enemy edits, synchronized doors, harvesting handles/collider/material payout, material drops, boss HP/scale/organic rewards, late-join construction/resource/boss state, menu gravity, boat lake/river seams, slide/crouch collision and remote visible stance, spectator permissions/viewpoint, blue vest feedback versus genuine hurt events, eight weapon classes/falloff and RPG travel/blast/cover/structure damage.

The remote enemy client confirmed **14 out of 14** scripted shots at a sinusoidally moving host with **130 ms added submission delay**. The host independently observed the damage. This demonstrates the prior interpolation/trigger-hitbox failure under that scenario, not perfect hit registration for every internet condition. Separate checks reject stale/future clocks and verify head-sized regions and misses beside the head/between feet.

Voice tests send three sequenced synthetic packets per 60 ms burst through actual local transport, check sustained receive counts, reorder frames, conceal a missing frame and resample into a stereo 48 kHz device block. All gameplay/voice runs report zero physical microphone starts. They do not verify the user's microphone or audible public-internet conversation.

## Native visual review

Graphics-enabled game windows were captured and inspected at **1600×900** during iteration and **1280×720** for the final build. The final sequence completed with no logged runtime exception. It covers HUD/squad cards, slot inventory/mods/materials, 4K atlas/legend, settings, voice/narrator controls, key bindings, world/side-panel editing, window collision presentation, pickaxe, RPG, crouch, boss and actual spectating. The edit panel no longer competes with minimap/weapon information. Armor is blue; storm violet is separate.

Examples: [HUD](images/fieldcraft-hud.png), [inventory](images/fieldcraft-inventory.png), [map](images/fieldcraft-map.png). Captures show the actual player, not a UI mockup. The art remains partly stylized/adapted; this update is not a claim of commercial-game visual parity.

## Rendered performance

The final route used an **i5-13450HX / RTX 4060 Laptop GPU**, full detail, uncapped **1600×900**, a rendered host and two local headless clients. It ran separately from the integration groups, with two-second settles and six ten-second samples. No unrelated Pinefall game was running during the measurement.

| Scene | Average FPS | Median ms | p95 ms | p99 ms |
| --- | ---: | ---: | ---: | ---: |
| Meadow HUD | 104.3 | 9.49 | 11.65 | 12.54 |
| Meadow without HUD | 125.4 | 7.83 | 9.57 | 13.39 |
| Named village | 102.5 | 9.69 | 11.53 | 12.45 |
| Autumn woods | 118.5 | 8.34 | 10.15 | 11.28 |
| Rainforest | 105.7 | 9.26 | 11.76 | 13.67 |
| Waterfront | 105.4 | 9.40 | 11.35 | 12.36 |

The route completed with zero captured runtime errors and zero microphone starts. HUD-on scenes averaged **102.5–118.5 FPS** on this machine. This is a short route on one laptop, not a controlled before/after comparison, an Auto-mode guarantee for other hardware, or a saturated 512-piece arena test. Full data is retained in [the performance report](../tests/baselines/0.8.0/performance-host.json).

## Repository and packaging

The repository validator checks importability/metadata/package pins/documentation targets, and PowerShell wrappers pass syntax parsing. Git patch whitespace is checked before publication. The existing Stag animation remains the expected >50 MiB advisory; no normal GitHub file exceeds 100 MiB. GitHub CI checks repository integrity, not a licensed Unity build.

The standalone export retains executable companions and credits. Every copied runtime file was compared with its built counterpart by SHA-256. The runtime assembly hash is the meaningful game-code identifier; Unity's executable shell can remain unchanged across releases. Older builds and historical reports were preserved.

## Practical limits

- Public-internet reachability, physical microphones/listening, severe jitter/loss, long eight-human matches, saturated 512-piece construction and lower-end hardware are unverified.
- Rewind is bounded at 500 ms and keeps present environmental cover/vehicles. Owner-driven movement is not competitive anti-cheat.
- Construction includes the four main forms, grid edits, doors, repairs and support collapse; advanced bent stairs/replacement contests and complete Fortnite pattern parity are not implemented. Static houses are not entirely destructible.
- Character/animal art and movement remain adapted/stylized in places. Birds are ambient; aircraft are supply flyovers rather than pilotable vehicles.
- All friends need 0.8/protocol 8; no compatibility with 0.7/0.7.1 is claimed. Direct UDP hosting still needs reachability through the host's router/firewall.
