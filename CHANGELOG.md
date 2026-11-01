# Pinefall Wildfront changelog

## Reference HUD and dedicated settings — 2026-11-01

- Reorganized the right-hand HUD into boxed equipment, a smaller build row and three material cards above it, following the supplied reference. Rarity colors, larger icons, selection outlines and fixed bindings replace the enclosing dashboard. Equipped ammo appears once inside its slot; unarmed players retain the quickbar.
- Added a clearly named Settings entry immediately below Enter the world, available through the same Escape menu during play. Display, audio and controls have a dedicated page with frame limit, graphics quality, live volume/sensitivity values and control help.
- Escape returns from Settings to the menu, then closes the menu. Preferences now save when closing with Escape as well as through buttons. Graphics/FPS changes preserve the current window resolution; simulation remains active.

## Native playability and rendering follow-up — 2026-11-01

- Added source-preserving distance LODs for short grass and skipped offscreen cosmetic animal animation on clients. Preserved substantial cover across quality settings; no physics/collision is removed with foliage detail.
- Kept moving construction previews movable and collision-free, avoided collision rebuilds on material changes, and reused edited door geometry. Edit hovering is frame-responsive and has a distinct pale highlight.
- Corrected recoil to an explicit upward camera impulse independent of mouse axis inversion. Attached third-person weapons to the animated hand and allowed the hand to drive their reload motion.
- Removed the redundant gun-information card, reduced survival-panel size and persistent text, added solid selected-slot colors and contextual build resources. Improved center-dot/scope contrast; replaced hundreds of scope-mask draw elements with one mesh.
- Repaired the reserve's invalid water mesh reference, synchronized its visible surface with its water volume and generated texture-free Single Layer Water shading.
- Completed the earlier requested seven-surface CC0 biome art pass with source attribution and deterministic packed inputs. Additional texture work is now frozen while performance/mechanics are prioritized. See `docs/PERFORMANCE.md` and the current handoff for measured scope and limitations.

## Nanite terrain rendering follow-up — 2026-10-31

- Built Nanite Landscape data for the reserve while retaining the collision/fallback heightfield and dense scanned scenery. Excluded short grass from persistent indirect-lighting contributions without reducing density or receiving light.
- Added slope-aware scanned rock projections and subtle wetland material response, and reduced daylight exposure bias. Graphics measurements and limitations are recorded in `docs/PERFORMANCE.md`.
- Added a saved Balanced graphics option: High scenery/texture/shadow detail with lighter indirect lighting/reflections, plus a same-process rendered comparison that checks unchanged density, resolution, peers and animal population.

## Unreal 0.5 construction/presentation checkpoint — 2026-10-31

- Added player-relative editable walls, floors, ramps and cones, materials/harvesting, supported collapse, shared resource drops and multiplayer prediction. First pieces fit terrain without another build; connected ramps continue upward.
- Fixed procedural build triangle winding so capsule contact normals face outward. A real remote owner now ascends a ramp and holds position under simulated latency. Grounded previews avoid putting the player beneath a ramp midpoint.
- Moved the minimap/status to the top left. Added a prominent live local arrow independent of the team feed, with spectated-pawn fallback. Kept it visible beside the right-side inventory. Fixed-key equipment, pickaxe/build binds, darker backplates and aligned readable inventory cards improve the HUD.
- Added scanned biome scenery/short instanced ground cover, stochastic PBR terrain, revised original houses/vehicles, natural animal coat detail, restrained loot rarity cues, a 4K atlas and a clearer storm wall. Animal meshes remain temporary stylized art.
- Removed continuous supported-animal bone physics. Shots/swings query the same authored animated primitives on demand, while movement capsules, current cover and player lag compensation remain intact. Unsupported shape assets keep ordinary query collision.
- Current forest measurements retain scenery and all 84 animals: 51.5 FPS for one instance and 47.8 FPS for a rendered host with two local headless clients at 1600×900 on the measured laptop. These are static-scene results, not a whole-match or universal hardware guarantee.
- Development checkpoint only: full Unity parity, packaged distribution, production character/animal art, broad biome/vehicle/WAN/20-human playtesting and final audio remain open. See the checkpoint evidence and native handoff.

## Unreal 0.3.1 traversal checkpoint - 2026-10-29

- Added fast-crouch sliding, buffered air-crouch landings, low-drag steering and slope response through engine walking collision.
- Added held/timed chained hops with substantial speed gain, prediction/replay state, replicated slide stance and traversal reset paths.
- Reduced sprint fatigue, silenced walking steps during slides, added modest speed FOV and contextual slide controls.
- Passed 24 delayed and 24 rendered-owner movement/ceiling/menu/reset/peer checks plus the existing combat, reserve, survival, round, wildlife and remote shooting regressions. Evidence: tests/baselines/unreal-0.3.1.


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
