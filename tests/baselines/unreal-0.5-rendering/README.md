# Native rendering follow-up — October 31, 2026

Unreal 5.6.1, native 0.5 development build. This follows the original construction/presentation checkpoint; it does not replace historical reports.

## Shipped changes

- Built Nanite Landscape data for Wildfront Reserve, with skirts and a synchronous editor gate checking current data and actual components. The regular Landscape remains for collision and fallback rendering. Cedar was not regenerated.
- Slope-aware world-projected scanned rock textures, coherent normal/roughness sampling, subtle wetland moisture response and reduced daylight exposure bias.
- Short grass retains its instances, density and 55/60 m fades, while no longer contributing to persistent indirect lighting. Scanned canopy, rocks, collision, roads and atlas are retained.
- Saved **Balanced** graphics option alongside Medium/High/Epic. Balanced keeps High geometry, textures, shadows and foliage, using level-1 indirect lighting/reflections. High remains the default and retains Lumen. No experimental render-thread CVars are installed globally.

## Controlled lighting comparison

One rendered host, two real local NullRHI clients capped at 30 FPS, all 84 animals, static forest view. Each profile was sampled for 22 seconds with a settling interval between them. The probe exercised the actual settings application without saving test settings. It verified unchanged resolution, density, view distance, Nanite state, peers and population.

| Same session | Average FPS | p95 frame time | p99 frame time |
| --- | ---: | ---: | ---: |
| High | 55.0 | 20.2 ms | 21.1 ms |
| Balanced | 64.6 | 17.2 ms | 18.1 ms |

Hardware: i5-13450HX / RTX 4060 Laptop, D3D12, 1600×900, 87% internal resolution, High foliage density 0.8. Windows reported battery power during the sweep. Memory was constrained by three local instances. These short static windows do not establish full-match, twenty-human, other-hardware or plugged-in performance. High precedes Balanced, so ordering/warmth remains a limitation.

`same-session-high.json` and `same-session-balanced.json` contain frame samples, actual GI/reflection CVars, population and timings. Run `tools/Test-UnrealRender.ps1 -CompareLighting -Capture` from a High baseline to reproduce the method. The script rejects incorrect profiles or changed scene settings.

## Nanite measurements and rejected experiments

The original state measured 47.7 FPS. Separate-process Nanite-on measurements ranged from **44.1 to 75.0 FPS**; a terrain-only Nanite-disabled run measured 56.4 FPS. The apparent 75 FPS gain did not reproduce consistently. These reports are retained explicitly to avoid cherry-picking. Nanite terrain is enabled and built, but a reliable universal speedup is **not** claimed.

The grass/coarse-shadow experiment yielded 48.8 FPS with CPU tracing enabled. The combined HZB/disabled-parallel-render-graph experiment regressed to 25.8 FPS and was rejected. Neither experimental CVar configuration is shipped. Raw traces/logs remain ignored local artifacts; provenance is in `verification.json`.

## Verification

- Final native editor/runtime compilation succeeded: `artifacts/unreal-06-build7.log`.
- Reserve: **83** checks, including terrain foundations and real remote vehicle movement/service/boat/inventory paths.
- Inventory: **18** UI and transaction checks.
- Reviewed real rendered images: `balanced-forest.png`, `forest-ground.png`, `highland.png`.
- The latest repository validation result is recorded in the handoff. No new full combat/network suite or packaged standalone release is claimed for this rendering-only follow-up.

Source and generated map/material fingerprints are in `verification.json`. Rebuild terrain Nanite data after height edits; preserve the regular collision heightfield. The focused update script avoids regenerating all settlements/foliage/roads.
