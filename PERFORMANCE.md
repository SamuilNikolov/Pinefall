# 0.7.1 host performance investigation

The GPU was already in use. The largest avoidable costs were CPU work on the authoritative host, extra HUD drawing, and terrain shader work. Existing clients did not run the host's wildlife and vehicle simulation, explaining why the host could stutter while guests remained smoother.

## What changed

- Parked, empty vehicles sleep instead of performing ground-height calculations, terrain-normal reconstruction and bridge raycasts fifty times per second. Boost charge catches up when the vehicle wakes.
- Runtime height queries sample a cached copy of the actual terrain grid. They no longer recompute Perlin terrain, 71 road distances and 48 settlement pads for each bird, animal, vehicle wheel and player query.
- Distant land wildlife simulates at a bounded lower frequency, retaining elapsed-time movement. Nearby threats retain the responsive path, and wildlife counts remain unchanged.
- Loot and vehicle models are created and animated only near the view. Model creation is limited per frame to smooth village entry. Distant wildlife bodies/animations are culled; scoped aiming extends presentation distance. Physics, loot state and live map data remain authoritative.
- Network interest changes are distributed across frames instead of processing whole regions in a periodic burst. The network tick rate and live map feed remain unchanged.
- HUD pictograms use cached textures instead of hundreds of tiny rectangle draws. Text styles are cached, and the combat HUD runs on repaint events instead of repeating its work during layout/input events. Scene-wide pickup/vehicle searches were replaced with registered lists.
- Terrain road coverage is baked into a small single-channel texture. Each terrain pixel samples it once instead of looping over all roads. Vegetation remains GPU-instanced, with better frustum rejection and cheaper understory shadows.
- Auto performance responds to measured frame time on any supported graphics device. It adjusts distant detail and shadows without a GPU-vendor rule. Targets are 60, 90, 120, 144 FPS or uncapped. Background rendering is limited to 30 FPS while networking and voice continue.

The 48 locations, 304 searchable buildings, 91 starting vehicles, expanded wildlife and map features are retained.

## Measured results

Measured on 13th Gen Intel(R) Core(TM) i5-13450HX and NVIDIA GeForce RTX 4060 Laptop GPU at 1600 × 900. Your original 0.6 session remained open and untouched. These numbers are measurements on this machine, not promises for other hardware.

A 45-second scripted fixed-view check, including initial startup frames, averaged **28.4 FPS in 0.6**, **8.8 FPS in 0.7**, and **55.5 FPS in the optimized build**. This check uses the configured frame cap and includes loading effects; the steady scene samples below deliberately settle first and run uncapped.

The route uses ten-second samples after a two-second settling period at each position. The host run waits for two real network clients to connect. NPC placement and the normal day cycle can vary workload. Frame times come from actual graphics-enabled player frames; no headless FPS results are used here.

| Scene | Solo average FPS | Host + 2 clients average FPS | Host p95 frame ms | Host p99 frame ms |
| --- | ---: | ---: | ---: | ---: |
| Meadow HUD | 102.0 | 98.8 | 13.48 | 20.00 |
| Meadow without HUD | 111.2 | 115.3 | 11.18 | 15.39 |
| Named village | 86.4 | 92.3 | 12.92 | 14.09 |
| Autumn woods | 99.0 | 94.4 | 13.28 | 17.83 |
| Rainforest | 94.7 | 97.2 | 13.26 | 16.77 |
| Waterfront | 96.5 | 89.0 | 14.29 | 19.00 |

The HUD-off comparison shows that the HUD contributes some cost, while the server simulation and scene work were the larger problems. PresentMon also measured the fixed-view CPU/GPU path: the 0.6 capture had about 25 ms median CPU frame work versus about 10 ms GPU work; the optimized capture had about 12 ms versus 4 ms. The 0.7 presentation trace contains many very short presents, so its Unity frame samples are used for the old-build FPS comparison.

All performance runs recorded zero runtime errors and never started microphone capture. Other GPU models and low-end machines were not benchmarked. Automatic detail uses frame timing, not this laptop's model. The final rebuild only adds the settings screenshot test step and updates the editor's default output path; gameplay performance code is unchanged.

## Controls

Open Settings → Performance & frame rate. Auto is enabled by default; choose a sustainable target or uncapped. The current graphics device is shown there, and the HUD can show FPS/frame time. Light, Balanced and High remain manual alternatives in Settings.
