# Unreal implementation and verification status

October 28, 2026. Initial migration working tree; **not a replacement release for Unity 0.9**.

## Native foundation

The runtime/editor modules compile with Unreal 5.6.1 and Visual Studio 2022. The runtime contains predicted CharacterMovement sprint/crouch/jump, two server-validated hitscan weapons, magazines/reload state, skeletal-mesh traces, replicated health/armor, practice targets, six-second range respawn, direct-IP host/join, camera modes, a native UMG HUD/menu, sensitivity/volume/quality settings, and a twenty-player admission limit.

Editor tooling imports prepared scanned meshes/materials/audio, creates the weapon table, and generates a 504 m Landscape and instanced foliage. Source preparation and rule export use standard-library Python. Installed Epic content is copied locally by setup and excluded from Git.

## Evidence and limits

| Gate | Current status |
| --- | --- |
| Native runtime/editor compilation | Passed Unreal 5.6.1 Development Editor build after the input/camera/editor fixes |
| Rule export | 120 linked rules; 8 weapons / 8 kits / 9 mods / 40 bindings / 16 fauna / 60 sites |
| Environment generation | Passed with `PF_BASELINE_ASSETS_OK`; map save and dirty-package save are checked |
| Visible rendered menu/game review | Inspected 1600x900 native menu and first-person range; corrected near-ground square repetition, mouse pitch inversion, camera lag, missing foliage usage flags and rifle/Nanite glass conflict |
| Host/client firing and damage | Passed four assertions in a real separate-client local range test: admission, five-round magazine, server ammo decrement and remote 80-damage confirmation |
| Packaged standalone build | Pending; launch scripts currently run through the installed Unreal editor |
| Frame rate, WAN, audible sound quality | Pending; no Unreal performance or internet-readiness claim |

The eight-row data table currently enables only **Ridgeline** and **Service Nine**. Range ammunition reserves are unlimited. The character is a temporary Epic mannequin with basic locomotion. Advanced weapon animation/IK, lag compensation, proper death/spectating and polished handling remain M1 tasks. Native armor damage is a small range implementation, not full source-specific legacy armor parity.

The close-ground material now uses three continuous overlapping patches with hashed translation/quarter-turn sampling, applied consistently to color, normal and roughness maps. This corrects the visible surface grid rather than hiding it with distance fog. Broader color patches and distant-normal fading are separate treatments. Near foliage and the wider environment still need an art pass; the generated scene is not a finished realistic landscape.

Evidence summary is in `tests/baselines/unreal-0.1`. Rendered review covered 1600x900 only, with first-run shader preparation; it is not a frame-rate benchmark. Moving-player hitboxes, latency/loss, long-session lifecycle and other display sizes remain unverified.

## Explicitly not migrated yet

The battle royale loop, teams/draft, storm, hunger/inventory/loot economy, animal AI/rewards/bosses/sharks, six-biome world, villages/caves, vehicles/boats, construction/editing/harvesting, launchpads, slide/bunnyhop, full map, character customization, narrator/weather/day cycle, emotes and proximity voice are **planned**. Preserve their specifications in the rule ledger; do not advertise them as Unreal features. The visual lake currently has no swim/boat behavior.

## Next developer entry

Read [UNREAL-MIGRATION.md](UNREAL-MIGRATION.md), [UNREAL-PRESENTATION.md](UNREAL-PRESENTATION.md) and [the project README](../Unreal/Pinefall/README.md). Next address weapon/hand animation, close-view handling and the sparse generated scenery. Expand validation to player targets under latency, menu/input edge cases and other display sizes before treating M0 as a release. Keep old Unity evidence attached to the build it actually tested.
