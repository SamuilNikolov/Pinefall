# Pinefall Wildfront 0.7.1 — Host Performance

An eight-slot Windows PvPvE battle royale development build in Unity 2022.3.13f1. This pass concentrates on settlements, travel, wildlife, sound and readable navigation.

## Start and join

Extract the whole Windows ZIP and run Pinefall.exe beside Pinefall_Data. Use 0.7.1 for the performance fixes. The network protocol remains version 7; 0.6 uses a different protocol. Your earlier game folders are preserved separately.

Choose Host a game or Join friends. The host card displays separate local/internet addresses and UDP port 7777. Same-network friends can browse sessions or use the local address. Internet friends use the public address and port, with a router rule forwarding UDP 7777 to the host computer and an inbound Windows allowance for this actual executable. An allowance for your older game folder may not apply to this copy. A displayed public address does not verify reachability. No relay service is included.

## Performance fix

This update removes repeated host terrain/vehicle work, budgets distant simulation and visual loading, caches HUD drawing and replaces the terrain road loop with a baked mask. It retains the fuller reserve. Settings → Performance & frame rate provides hardware-independent automatic detail, FPS targets and the current GPU name. See PERFORMANCE.md and PERFORMANCE-RESULTS.json for actual graphics-enabled measurements and their limits.

## Living Reserve features

- Forty-eight named settlements: fourteen larger villages of twelve buildings plus thirty-four smaller four-building hamlets, totaling 304 searchable buildings. Named locations use two words with matching initials. Villages have streets, market stalls, fences, supply rooms and vehicle access.
- Seventy-one road links, thirty-five repair lay-bys and ninety-one starting vehicles, including eighteen trail motorcycles and ten motor launches. Supply bonuses can add transport during play. Condition, fuel, repairs, boost and upgrades still matter.
- A live map feed uses car, bike and boat pictograms even beyond the local render range. It updates once per second. Hover to read engine/fuel values. Enemy-occupied vehicles stay hidden. Major village names have stronger outlined type; smaller names appear as you zoom, with collision avoidance and location leaders.
- A refreshed 4096-pixel render covers actual trees, structures, streets, bridges, terrain and five lakes. Amberwood is an autumn floodplain variant with broadleaf canopies and its own habitat label, alongside the existing six core biomes.
- Wildlife starts around 192 land animals plus fifteen lake sharks, with biome replenishment and small nearby herds up to a bounded total. Clear paw markers identify animals actually visible within 95 metres; map tracking reaches 160 metres. Prey remains peaceful normally and enrages during closing storm phases.
- Fifty-seven processed recorded sound clips replace the repeating synthetic footstep and gunshot playback. Surfaces vary, local steps are quieter, firearm reports attenuate with distance and muffle through cover, and larger animal families have positional calls. Quiet bird ambience and reload cues add context. Settings → Sound mix separates footsteps, weapons, wildlife and ambience. Some underlying weather and engine layers remain procedural.
- Health, armor, hunger and stamina have distinct icons/meters. Ten hunger segments, food count/key, state text and early warnings show what to do. Hunger drains more slowly in vehicles; food restores sprinting once hunger returns above twenty.
- Supply crates have a 35% bonus motorcycle chance, 25% survey-beacon chance, or 40% ordinary outcome. A blocked/water motorcycle delivery substitutes a beacon. Survey beacons reveal enemy snapshots to the activating team for twelve seconds, in four-second pulses, and warn everyone. They can be carried (maximum two), dropped and shared.

## Default controls

WASD move/drive/steer; mouse look; Shift sprint/boost; Ctrl crouch; Space jump/brake; LMB fire; RMB aim; R reload; E interact/hold revive; 1–3 guns; 4 food; 5 medkit; B armor plate; G repair; F refuel; T vehicle upgrade; X flashlight; Q survey beacon; I inventory; M map; V camera; L lobby; Tab roster; N push-to-talk; Esc menu.

Use Settings → Key bindings to change keyboard/mouse actions. Escape remains available. Voice starts disabled. Enable push-to-talk or speech-gated open mic explicitly; all teams within 75 metres can hear one another with distance falloff. Disabling voice or leaving stops capture.

## Handbook and project

GAME-GUIDE.html is the offline reference page and GAME-GUIDE.md is its text version. The in-game handbook covers the match, storm, animals, weapons, attachments, teams, travel and controls. THIRD-PARTY-NOTICES.md and AUDIO-SOURCES.json document assets and sound modifications. VERIFICATION.md and verification/ describe the actual checks and their limits.

The source ZIP contains Assets, Packages, ProjectSettings and documentation. Open it in Unity 2022.3.13f1; Pinefall → Build playable demo rebuilds the scene/player. No paid assets or voice-service account are required. The neural narrator's generator/model are not needed to play. To refresh atlas imagery after geography changes, run a graphics-enabled player with --bake-atlas <absolute PNG path>, copy its result to Assets/Resources/DetailedAtlas.png and rebuild.

This is still a development build. Some character/animal art, animation and vehicle physics remain stylized or simple. Longer human matches, real microphone hardware, public-internet latency and lower-end GPU profiling need further testing.
