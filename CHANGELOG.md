# Pinefall Wildfront changelog

## Unreal 0.3 reserve checkpoint - 2026-10-29

- Added a separate 2,016 m reserve with eight named settlements, 64 enterable cabins, graded road links, a lake/piers, 208 stock placements, sixteen trucks and three boats.
- Switched to a textured stylized ranger; rebuilt crouch with planted ankle rotations, matching collision/eye height, swimming clips and real remote stance checks.
- Repaired persistent character material assignment and explicit skeletal shader usage; added source-texture extraction/material tooling.
- Added server-controlled driving/steering, look-around camera, boost/braking, ground/water following, remote display smoothing, safe exit, damage/fuel, service kits and sleeping empty vehicles.
- Added original recognizable supply meshes, larger wildlife replenishment, a 2K layout atlas, map zoom/pan, minimap, live private team/transport icons and equipment/vehicle HUDs.
- Added a separate real-owner reserve integration probe. Final results and visual limitations are recorded separately; this remains a migration build, not Unity parity or a finished art release.


## Unreal 0.2.1 combat/world working tree - 2026-10-28

- Replaced the mannequin with a dressed CC0 MakeHuman-derived ranger, native movement/weapon-pose blending, authored rifle/pistol holds, reload gestures, crouch poses and fitted skull/neck collision.
- Fixed detached pistol magazine geometry, sight placement and camera/server eye-origin disagreement. Third-person pose/offset work remains visually unfinished.
- Added measured owning-client clock sync and bounded server-only skeletal hit history with current-cover rejection; separate real-player combat probes cover standing/crouched targets, camera modes, headshots and moving targets in both host/client shooting roles at simulated latency.
- Added scanned forest/rock surfaces, triplanar cliff color, denser planted pines, root-centered tree imports, fitted trunk collision, surface collision for rocks and a shaped swimming lake with air/drowning rules.
- Compiled and checked as a migration checkpoint; see tests/baselines/unreal-0.2.1 and Design/UNREAL-STATUS.md for results and unfinished art/performance gates.

## Unreal 0.2 round and combat working tree - 2026-10-28

- Added the first native team lobby, loadout/landing bidding, cyclic storm, hunger/inventory, armor, down/revive, spectating and round results loop.
- Added seven animated wildlife species, bounded server AI, storm rage/alpha variants, positional calls and food/equipment rewards.
- Added smooth scoped aiming, sensitivity scaling, weapon recoil/reload presentation, server-confirmed hit/armor/elimination cues, brief visible-target health bars and directional incoming damage. Armor uses blue feedback.
- Added contextual right-side inventory/draft panels and terrain/storm/team mapping. Scope masks retain readable foreground HUD elements.
- Added separate multi-process round and wildlife probes, asset import tooling and the native handbook. This remains a small migration arena with temporary art; see Design/UNREAL-STATUS.md for unported functionality and open verification gates.

## Unreal 0.1 foundation — 2026-10-28

- Froze the unfinished Unity 0.9.1 sweep at the user's migration request; its final full-suite verification was cancelled.
- Added a native Unreal 5.6.1 project with predicted character movement, two-weapon server firing, range respawn, direct-IP host/join and UMG settings/HUD.
- Added scanned-art preparation/import and Cedar Crossing: a 504 m Landscape, instanced vegetation, slope-aware materials, sky/fog and water presentation.
- Exported 120 source-linked rules and exact legacy balance/source hashes. Added phased migration, presentation direction, feature status and portable setup/build/launch scripts.
- This is an initial foundation. Full battle royale, advanced animation, lag compensation, vehicles, wildlife, construction and voice remain to migrate. See `Design/UNREAL-STATUS.md` for the verification gates.



## 0.9.1 — Handling and presentation — 2026-10-27



- Camera-directed building, stable quarter-turn boundaries, connected-frame fallback across jumps/openings, and aim-projected adjacent floors.

- Continuous uphill vehicle travel, graded speed, bridge/build support, nonallocating obstacle checks, neutral stale input, and interpolated host camera/body presentation.

- Detailed imported dropped weapons and shared, differentiated supply models; neutral equipment materials with small rarity footprints and thin locators.

- Readable right-side pickup cards, natural foliage/ground colors, scan-based triplanar stones and pebbles, stronger terrain normals, more directional ambient lighting and edge-aware image resolve.

- More frequent relevant wildlife replenishment, 40 target animals per available biome and a 320-live-animal ceiling; all group members respect arena/player buffers.

- Six/ten/fourteen scattered storm-anomaly caches for small/medium/full arenas. Active-match exposure within 7 m costs 4 health every 2 s; retrieval awards an epic/legendary weapon plus the usual cache supplies.

- Added real 36-degree occupied hill-climb checks, snap edge cases and a focused rendered presentation capture route.



## 0.9.0 — Living Wildfront — 2026-10-26



- Buffered/held bunny-hop chaining with substantial speed gain, air steering and momentum-preserving fast crouch slides.

- Six replicated original gestures and a rebindable hold/release/click emote wheel with center cancellation.

- Character-focused main/host/join/lobby screens, font-height-safe labels and measured complete handbook scrolling.

- Hold-Alt inventory, explicit pickaxe hotbar and four shape shortcuts; player-relative connected build frames and quarter turns.

- Lobby-sized 2 / 4 / 6.1 km arenas for 1–5 / 6–12 / 13–20 participants. Protocol 9, twenty-player capacity including bots.

- Fixed and lootable floor-mounted launchpads, owner launch motion and replicated steerable gliders.

- Sixty named settlements, 444 buildings, 89 road links, supply relays, expanded travel and stronger mountain/mesa elevation.

- Six terrain-integrated branching cave systems with chambers, a loop route, underground caches and emissive crystal clusters.

- Photographic ground/foliage detail, three-level fir rendering, distant village silhouettes and a 900 m default view budget.

- Animal bounds fitted to the model, directional body hit tests and ordinary usable item rewards; bosses guarantee two equipment rolls.

- Pooled hit particles/tracers, separate blue shield cues, expanded original event scripts and offline Kokoro narration.





- Added pickaxe harvesting, finite wood/stone/metal, discovered/shareable materials, replicated walls/floors/stairs/pyramids, grid edits, doors, repairs and support collapse.

- Corrected remote hit recognition with bounded server history matched to NGO rendering delay; added stance-aware anatomical player regions, owner-confirmed damage numbers, armor/hit/elimination feedback and discreet damaged-enemy HP.

- Added eight explicit weapon classes and class-specific firing/range behavior, including a traveling RPG with blast falloff and cover protection.

- Added larger storm alpha mutations with bounded population, enhanced HP/attacks and organic rewards.

- Increased sprint speed, reduced fatigue, added replicated sliding/crouch poses and weapon grip IK; increased vehicle speed and fixed boat lake/river level transitions. Settings keep gravity and world simulation running.

- Added server-validated spectating: any survivor in Solo; teammates while the team survives in Duos/Squads, then any survivor.

- Reworked voice capture/resampling, burst acceptance, sequencing, receive jitter/loss handling and device-block playback. Physical microphone capture remains opt-in.

- Replaced the main HUD/inventory/map/settings with clearer translucent layouts, icons and minimal squad health/armor/distance/voice cards. Vest changes use blue positive feedback; real damage retains the hurt effect. Character material contrast is restrained and respects occlusion.

- Bumped approval/discovery to protocol 8. All peers need 0.8; older clients are incompatible.

- Added separate multiplayer and rendered UI checks, current handbook content and developer documentation. See docs/VERIFICATION-0.8.md for actual release results and remaining limits.



# Repository handoff — 2026-10-25



- Added the importable 0.7.1 Unity source, stable asset metadata and package lock to the Pinefall repository.

- Added setup, architecture/system maps, content recipes, networking, testing, performance, asset provenance, roadmap and AI-agent handoff guidance.

- Added portable Windows build/gameplay/performance scripts, repository integrity CI, issue and pull-request templates.

- Kept the runtime implementation unchanged; made editor output checkout-local and exposed explicit road-mask/atlas/handbook refresh commands.

- Preserved historical release reports separately from new checkout validation. No project-wide source license was assumed.



# Wildfront 0.7.1 — Host Performance



- Removed redundant parked-vehicle physics/terrain work and cached runtime terrain heights.

- Budgeted distant wildlife, model creation and host interest updates while keeping the expanded reserve.

- Cached HUD icons/styles and removed per-frame scene searches.

- Replaced per-pixel road segment loops with baked coverage; improved vegetation culling and shadows.

- Added hardware-independent automatic performance controls, frame-rate targets, GPU identification and an FPS/frame-time display.

- Benchmarked solo and three-player host gameplay with graphics enabled; see PERFORMANCE.md.



# Wildfront 0.7 — Living Reserve



- Expanded to 48 alliteratively named settlements, including 14 larger twelve-building villages; added market areas, village streets and roadside details.

- Increased road connections, repair stops, cars and boats; added rideable two-seat motorcycles with steering forks, leaning and shared repair/fuel systems.

- Added the Amberwood autumn floodplain, broadleaf canopy variation, two lakes and more moorings. Re-rendered the 4K atlas.

- Added live whole-map car/motorcycle/boat icons, fuel/condition hover detail, stronger village label hierarchy and zoom-dependent hamlet names. Enemy-occupied transport is hidden.

- Increased biome wildlife and nearby herds; added visible-animal paw cues and wider wildlife tracking.

- Replaced footstep/gun playback with varied recorded assets, quieter local movement, distance/cover-aware firearm reports, animal calls and a separate sound mix page.

- Reworked hunger into an icon-led segmented meter with explicit states, food key/count, early warnings and slower hunger drain while mounted.

- Added motorcycle and survey-beacon supply bonuses, tool inventory/drop support, twelve-second team-only position pulses, global exposure warning and a remappable Survey key.

- Kept the existing round modes, public spawn draft, attachments, storm behavior, proximity voice, downing/revives and terrain-preserving session returns.



0.7 requires matching versions between friends. Previous game copies remain separate. See VERIFICATION.md for checks and outstanding limits.
