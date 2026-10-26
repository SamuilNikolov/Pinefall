# Wildfront 0.8.0 — Fieldcraft — 2026-10-26

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
