# Native 0.5 playability follow-up — November 1, 2026

Development evidence for the grass-budget, cover, water, construction-preview, recoil and HUD follow-up. The native project remains Unreal 5.6.1; this is not full Unity parity or a packaged production release.

## Gameplay verification

| Check | Result | Scope |
| --- | --- | --- |
| Native Development Editor compilation | Passed | `artifacts/mechanics-timing-fix-build.log` |
| [Combat](combat.json) | 16 passed | Actual host/client shots, standing/crouched/moving targets, scoped FP/TP, head damage/current cover; 80 ms artificial delay per sender. Both logs confirmed +0.600 degrees camera recoil. |
| [Construction](construction.json) | 40 passed | Three real processes, delayed replication, editing, harvesting, late join, remote ramp ascent/idle floor, support/collapse/reset. |
| [Reserve](reserve.json) | 86 passed | Saved 2 km reserve, foundations across biomes/orientations, vehicles, service/sharing, crouch and actual lake mesh/visual footprint/shared water level. |
| [Inventory](inventory.json) | 18 passed | Actual UI access and slot transactions. |

The first final edit-timing candidate overrode the ramp fixture's explicit movement tick interval and failed its two ascent/floor checks (`artifacts/unreal-construction/20261101-014212`). Editing now changes timing only on entering/leaving editing, preserving traversal timing. The archived construction report is the subsequent passing run (`20261101-014814`); the failed report was not relabeled.

## Rendering evidence

The [same-session grass comparison](grass-forced-lod0.json) measured 45.4 FPS with forced source LOD0 versus [55.3 FPS with distance LODs](grass-distance-lods.json), p95 32.8→26.3 ms, p99 43.8→36.2 ms and GPU 12.9→8.9 ms. Near source geometry, density, resolution, quality, peers and animal count were retained. This isolates the LOD change in one scene.

The later [forest rendering report](forest-render.json) measured 66.9 FPS (p95 17.9 ms, p99 20.4 ms). It is a separate run, not the denominator for a controlled percentage improvement. Both runs used an i5-13450HX / RTX 4060 Laptop GPU, D3D12, 1600×900 High/87% internal resolution, density .8, all 84 animals and two local headless peers capped at 30 FPS. Windows reported battery power. [Exact scene/test scope](forest-scope.json). This forest report predates the final water-material regeneration and corresponding landscape Nanite refresh; a final report will be retained separately when available.

Graphics tests enable explicit host-only benchmark immunity to avoid death/respawn changing a stationary sample. Ordinary games do not. This is static-scene evidence on one machine, not whole-match, twenty-human, WAN, audio or all-hardware certification.

After the final repeat-generation/material-usage repair, the [final forest run](final-forest-render.json) measured **61.9 FPS, p95 20.2 ms, p99 23.8 ms**, with the same resolution/quality/population/peer conditions and reported battery power. [Final scope](final-forest-scope.json), [final HUD capture](final-forest-hud.png). No water shader fallback, unsupported water-Nanite material or missing material-usage warning appeared in that run.

Remaining engine validation issue: the editor helper reports current generated Landscape Nanite data at save, but an uncooked `UnrealEditor -game` reload still emits a stale-data MapCheck warning. Regular Landscape rendering/collision remains available. These measured FPS results are real rendered results, but **do not certify active Landscape Nanite after reload** or attribute their improvement to it. Diagnose the saved-versus-reloaded Landscape content IDs before making that claim.

## Actual reviewed images

- [Forest HUD at 1600×900](forest-hud.png): top-left map, compact vitals, fixed keys, solid selected slot and compact ammo instead of the large weapon-information card.
- [Pistol aiming](pistol-aim.png) and [third-person grip](third-person-pistol.png): existing weapon geometry and animated hand attachment. Proper first-person arms/custom IK remain unfinished.
- [Build preview](build.png) and [edit grid](edit.png): contextual resources, readable controls and distinct pale hovered cell. The normal game world remains visible.
- [Scope](scope.png): single-mesh lens mask, contrasting cross lines and fixed center point.
- [Lake](lake.png): current visible water surface. The camera is a presentation fixture; its minimap still represents the actual pawn, not the external camera.

Water review caught a repeated-generation bug in Unreal 5.6's expression-deletion helper, which could leave multiple mandatory water-output nodes and cause a default-material fallback. The script now drains the expression collection before rebuilding. The 128-triangle water disk uses ordinary raster rendering because Single Layer Water is not a supported Nanite shading model. Terrain Nanite data is rebuilt after material changes. No new textures are used for water, the scope or edit hover.

Further texture additions are paused at the user's direction. The preceding seven CC0 surface sets retain original provenance/checksums and source-preserving prepared inputs. Remaining art/animation and full-game verification gates are in the developer handoff.
