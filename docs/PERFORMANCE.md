# Performance engineering

## Why host-only stutter mattered

The host renders and simulates every authoritative actor. Guests do not run the full wildlife, physics and loot work. In 0.7, extra world content multiplied a CPU-heavy terrain function and idle vehicle checks. Reducing UI cost alone would not explain or solve this asymmetry. The GPU was already active.

The root [release performance report](../PERFORMANCE.md) records exact measurements and limits. On the measured laptop at 1600×900, fixed-view averages including startup were 28.4 FPS for 0.6, 8.8 for 0.7 and 55.5 for the optimized candidate. Settled full-detail host scenes with two connected clients averaged 89–99 FPS. These two methods measure different intervals and should not be conflated.

## Budgets to preserve

| Mechanism | Purpose / source |
| --- | --- |
| Cached bilinear TerrainData queries | `TerrainSampling`; avoids repeated Perlin, roads and settlement distance work for every wheel/animal/player |
| Parked empty vehicle sleep | `RoadVehicle.FixedUpdate`; no terrain normals/raycasts at 50 Hz for idle vehicles; catch up boost on wake |
| Distant wildlife steps | `Wildlife`; roughly 0.25 s steps with elapsed movement outside player relevance; nearby threats retain responsiveness |
| Deferred presentation | `PresentationBudget`; lazy loot/vehicle models, bounded enables per frame, culled distant bodies and aim-aware ranges |
| Budgeted interest | `WorldInterest`; cached viewpoints, up to 64 actors / 12 visibility changes per frame, rather than periodic whole-region bursts |
| Cached HUD | `ReadableUI`, `RasterIcons`, `CombatUI`; cached styles/textures and repaint-only combat drawing |
| Terrain road mask | `BuildRoadCoverage` / terrain shader; one texture sample instead of a full road-distance loop per terrain fragment |
| Vegetation bounds/frustum/shadows | `ReserveWorld`; retain instancing, discard offscreen batches, reduce fine-understory shadow cost |
| Registered actor lists | `FieldPickup`, `RoadVehicle`, `WorldLoot`; avoid scene-wide searches in repeated interactions |

Treat numbers above as current implementation details, not arbitrary universal tuning constants. Measure before changing them. Population/content is retained; removing most of the world is not the baseline optimization strategy.

## Auto settings

`RuntimePerformance` uses measured frame time, with startup/warm-up and slower recovery, to adjust detail/shadow cost. It supports 60/90/120/144 FPS targets and uncapped, with Auto enabled by default. Background rendering is capped while networking/voice continue; benchmark flags force the measurement path to remain active. Manual presets disable Auto. The UI is kept at native resolution.

No GPU vendor/device-name tuning or system power/driver edits are involved. Any new adaptive mechanism should respond to observed work and expose a clear player control. Avoid oscillating settings every frame.

## How to investigate a regression

1. Record commit, Unity version, host/client roles, player count, CPU/GPU, resolution, frame cap and settings.
2. Establish if time is on the main thread, rendering/GPU, physics, allocations/GC, network serialization or loading. Run the same host route with and without HUD; don't infer causality solely from visible UI complexity.
3. Look for work multiplied by actor count and peers. Keep player relevance/viewpoint caches outside inner actor loops. Review instantiation bursts, string allocations and materials created by repeated state changes.
4. Compare a graphics-enabled host with two clients and similar background load. Include median/p95/p99 frame times and warm-up, not only average FPS. Drive through village entry and repeat leaving/rehosting.
5. Check server responsiveness and privacy after reducing work. Long-range aiming, collision, full-map vehicle pins, storm progression and late join must still function.
6. Keep the report/raw data locally under `artifacts`; add a versioned baseline only when releasing reviewed evidence.

See [TESTING](TESTING.md) and `tools/Measure-Performance.ps1`. Other GPUs, weak integrated systems, eight-human matches, physical voice devices and long public-internet games remain important follow-up measurements.
