# Pinefall presentation direction

**Character direction update, October 29:** the user explicitly prefers fun, somewhat cartoony characters over hyperrealistic people. Active TrailRanger uses stylized proportions, readable palette textures and a hooded outdoor silhouette. Keep terrain/light/material ambitions, but do not return to realistic skin/pore-heavy characters. Natural weapon grips, planted crouch and readable peer animations remain art-quality gates.


The target is a grounded wilderness battle royale: readable combat amid convincing landscape, with moments of danger and beauty. Professional presentation comes from consistent scale, material response, animation, interaction and sound. Increasing polygon counts alone will not fix a floating gun, harsh footsteps or an unreadable menu.

## Cedar Crossing visual benchmark

Use restrained forest greens, limestone, damp soil and weathered timber. Warm sun and cooler skylight separate planes without an orange/teal filter. Reserve amber for calls to action, ice blue for armor and interaction, red for real health damage. Keep grass below small-animal silhouettes along paths; use clusters of fern/brush rather than an evenly distributed wall. Frame a distant ridge, a readable water edge and a traversable clearing. Trees must have correct opacity, normal maps, meter scale and trunk collision. Maintain small shadowed pockets without crushing visibility.

Terrain needs broad landforms, erosion-like variation, slope-sensitive exposed rock, blended ground materials and authored path transitions. Later villages need an identifiable skyline, road connection, interiors with loops, useful roofs, cover spacing and named landmarks. A collection of scattered boxes is not a settlement. Caves should have concealed believable entrances, branching rooms, vertical connections and subtle crystal light, not a visible pipe laid on top of terrain.

Lumen, virtual shadows and temporal reconstruction are the high-quality starting configuration, with measured scalable alternatives. Do not mandate Epic settings or a GPU vendor. Texture streaming, instance density, shadows, view distance and AI budgets must respond to measured costs. Target a stable 60 fps rendered host with peers on a documented reference configuration; that is an acceptance target, not a current performance claim.

## Combat feedback contract

| Event | Immediate local response | Confirmed response |
| --- | --- | --- |
| Trigger | Short recoil impulse, visible muzzle, mechanical layer and shot report | Server accepts cadence/ammo and replicates bounded remote effects |
| Armor hit | Impact at real traced surface | Blue marker, concise armor sound and damage amount; distinct armor-break cue |
| Flesh hit | Surface impact | Neutral/white hit marker, short lower-pitched confirmation; red reserved for incoming health loss |
| Head hit | Same physical trace | Distinct small accent, no excessive particle cloud |
| Incoming hit | Directional impulse and short edge cue | Health/armor bar updates; armor equip/recovery never produces a red damage flash |
| Elimination | No speculative kill cue | Brief final confirmation, compact feed and contextual announcer; respect team downing rules |

Prioritize crosshair, silhouette, muzzle and contact point. Effects must not hide the target. Use decals, debris, sound and a few well-timed particles rather than persistent glowing outlines. Player readability should come from exposure-aware materials, local contrast and consistent character value, without revealing enemies through cover.

Weapons need correct proportions, receiver/magazine/bolt details, rough metal and polymer response, close-view geometry, sight alignment and convincing hand contacts. Build first-person arms and third-person aim layers deliberately. Reloads must match magazine state and fire gating. Locomotion should blend crouch, sprint, jump, slide, land and swimming with replicated stance. A temporary mannequin is useful scaffolding; replacing it and its animation set is a defined art task.

## Audio and animation

Separate firearm transient, body, mechanical action and distance tail; cap concurrency and attenuate remote reports. Choose outdoor/indoor tails by environment later. Footsteps are quiet, varied, surface-based and distance-driven, with no repetitive bright click. Avoid summing owner prediction and replicated playback. Wildlife calls establish direction and habitat without making every rabbit constantly vocal. Vehicles need load/rpm layers, tire/water contact and collision cues. Duck ambience slightly for critical announcements; do not suppress useful combat sound.

Narration should be brief, legible and occasionally witty. Prioritize imminent storm, elimination and supply events; discard stale repeated lines. Use licensed/offline recordings with documented provenance. Do not promise a hired voice actor when using synthesized speech. Voice capture stays explicitly opt-in and never starts during automated validation; later jitter-buffer checks use synthetic frames before consenting microphone tests.

## UI and map

Use a calm central composition in the front end: character/environment stage, clear Play action, mode and party summary, separate Host/Join paths. State prerequisites and connection failures where the action happens. During play keep health/armor lower left, weapons and build bindings lower right, compact teammate rows above health, compass top center and minimap top right. Reserve the center for aiming and brief confirmed events.

Hold Alt inventory should occupy the right side with translucent panels, large item silhouettes, category/rarity cues, readable amounts and explicit drop/swap actions. Full map should use a high-resolution terrain render with road/water/building detail, selective alliterative settlement labels and consistent zoom-aware icons. Team positions remain visible; enemies remain private except the deliberate timed survey rule. Avoid tiny paragraphs, excessive labels and decorative text that competes with play.

Inspect at 1280x720, 1600x900, 1920x1080 and ultrawide. Test long names, empty/full inventories, twenty-player rosters, missing network connections and settings opened while driving. Panels scroll or reflow; no clipped essential text. UI should update changed values/events, not rebuild every frame or query network adapters on each draw.

## Review sequence

First verify imported scale/materials and collision in daylight. Then inspect dusk, motion and gun handling. Then listen to overlapping combat, movement and ambience. Finally measure a graphics-enabled host with remote peers. Screenshots establish appearance, not fun or networking correctness; actual play and frame-time traces remain separate gates.
