# Performance engineering

## Native Unreal 0.5

### November 1 gameplay/rendering follow-up

The grass meshes now retain LOD0 and add 50%/18% distance LODs: live grass 2,176/1,088/392 triangles, dry grass 384/192/70. Their original density and 55/60 m cosmetic fades remain. Actual cover groups disable density thinning and distance removal; non-Nanite fern beds retain a 300 m end distance. These adjustments run once on each reserve machine, including clients. Collision and server hit validation remain independent of rendering quality. Fine low grass is cosmetic, not reliable concealment.

A controlled same-process forest comparison with the new biome surfaces measured **45.4 FPS with forced LOD0, 55.3 FPS with distance LODs**. Frame p95 improved 32.8→26.3 ms, p99 43.8→36.2 ms, GPU 12.9→8.9 ms. Both samples used a graphics host, two local 30-FPS headless clients, 84 animals, High/87% rendering at 1600×900, density .8, and reported battery power. This isolates grass LODs in one static scene, not an all-hardware or whole-match promise. Source reports: `artifacts/unreal-render/20261101-014229-Forest`. The texture pool required ~279 MB of its 800 MB budget with no wanting resources; increasing it was not justified. Disabling software contrast-adaptive shading regressed performance and was rejected.

Additional fixes: remote offscreen wildlife skips cosmetic skeletal animation while authority continues accurate bone evaluation; moving construction ghosts remain movable and never cook collision, avoiding static/movable re-registration churn; material changes no longer rebuild structural collision; edited door slabs are reused; the scope uses one mesh instead of hundreds of mask draw elements. Actual placed predictions remain static to preserve replicated movement-base behavior. Graphics tests use explicit host-only benchmark immunity to prevent animal death/respawn from contaminating samples; ordinary games do not enable it.

`Test-UnrealRender.ps1 -CompareCommands` supports isolated same-session experiments. Do not persist experimental console commands or equate NullRHI integration speed with rendering performance. Current graphics verification and the latest gameplay reports are documented in the handoff; historical reports below retain their original scope.

The final post-repair forest run measured **61.9 FPS, p95 20.2 ms, p99 23.8 ms** with the same 84-animal/two-peer/High-87%/1600×900 conditions. See [retained current evidence](../tests/baselines/unreal-0.5-playability/README.md). Generated Landscape Nanite data passes the editor helper but still produces a stale-data MapCheck warning on uncooked `-game` reload; regular Landscape rendering/collision remains. Active Landscape Nanite on reload is an unresolved validation gate, not an assumed source of the measured gain. No shader fallback or unsupported Nanite water material remains in the final forest log.

### Nanite terrain follow-up

Wildfront Reserve now has generated Nanite Landscape data, in addition to its existing Nanite scanned trees/rocks. The editor helper checks that built components exist and match the terrain content ID. This changes rendering, not the underlying heightfield collision, roads, instanced population or atlas. Regular Landscape data is still required. Fine-grass indirect-lighting contribution is disabled; receiving light, density scaling and 55/60 m fades remain. High/Epic retain Lumen. The optional Balanced profile preserves High geometry, texture, shadow and foliage settings, but uses level-1 GI/reflections. No experimental render-task configuration is shipped.

The fresh original-state forest run measured 47.7 FPS (p95 31.0 ms, p99 43.5 ms). A grass/coarse-shadow experiment produced only 48.8 FPS; disabling parallel render-graph execution together with HZB occlusion regressed to 25.8 FPS and was rejected. The first built-Nanite-terrain run measured 75.0 FPS (p95 16.6 ms, p99 19.3 ms). A follow-up with **only `landscape.RenderNanite 0`** measured 56.4 FPS (p95 24.1 ms, p99 35.4 ms). A subsequent Nanite-on repeat fell to 44.1 FPS (p95 32.6 ms, p99 45.2 ms), so the apparent speedup is not yet reproducible. These are separate static forest runs with 84 animals, a rendered host plus two local 30 FPS NullRHI clients, 1600×900, quality 2, 87% internal resolution and 0.8 foliage density on the documented laptop. Placement is procedural and available memory varies; do not treat one pair as a universal gain or full-match certification.

The final same-process comparison measured High at 55.0 FPS (p95 20.2 ms, p99 21.1 ms) and Balanced at 64.6 FPS (p95 17.2 ms, p99 18.1 ms), retaining detail/density/resolution/peers/population. Windows reported battery power during the sweep; available physical memory was low with three local processes. See [all retained reports and exact scope](../tests/baselines/unreal-0.5-rendering/README.md).

The CPU trace identified rendering-thread task/visibility waits rather than GPU saturation. Keep experimental settings isolated to test command lines. See Epic's [Nanite Landscape documentation](https://dev.epicgames.com/documentation/en-us/unreal-engine/using-nanite-with-landscapes-in-unreal-engine?application_version=5.6) for the dual-data requirement. Do not assume merely setting the checkbox builds usable Nanite terrain.

See [exact checkpoint evidence](../tests/baselines/unreal-0.5/README.md). Native profiling uses real graphics, frame tails, game/render/RHI/GPU timing, minimum physical memory, peer count and settled wildlife population. Test clients are capped at 30 FPS; run graphics measurement separately from integration tests. Cold editor startup/shader/asset work is excluded from samples, and the 84-animal reserve budget must be populated before recording.

The important CPU fix is `PFAnimalHitQueries`: cached authored sphere/capsule/box templates are transformed by current animated bones only when a shot, pickaxe swing or relevant bite query needs them. Supported animal meshes do not maintain per-bone Chaos query bodies every frame. Movement capsules, animation, damage authority, current cover and player rewind are retained. Unsupported convex/tapered assets fall back to ordinary collision instead of receiving an inaccurate substitute.

Fine grass fades over 55/60 m (green/dry), uses instancing/density scaling and does not cast grass shadows. Trees/rocks use Nanite detail reduction; do not mistake their nominal HISM cull distances for a hard Nanite cutoff. Visibility changes must never remove collision. No vendor-name special case, driver change or machine power-setting edit was used. A command-list batching experiment did not justify changing engine defaults.

## Historical Unity engineering


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

## Fieldcraft additions

0.8 keeps resource destruction sparse: existing foliage collider/matrix handles are registered once, changed HP is replicated, and there is no per-tree Update or network actor. Built pieces combine visible parts into one render mesh, cap at 80/player and 512/world, and traverse a spatial support grid only after removal. Shot history has bounded rings; voice receive/device playback uses preallocated buffers. See [0.8 verification](VERIFICATION-0.8.md) for a graphics-enabled release run. The route does not stress a saturated 512-piece arena.


## Living Wildfront visual budget

0.9 defaults to a 900 m view distance, adjustable from 750 to 1,800 m. Village detail changes to simple silhouettes at approximately 175–300 m, depending on adaptive detail; scoped views retain more detail. Collision, loot and map state are unaffected. Fir branch scans have three geometry levels. Low grass is 0.17–0.31 m tall in a bounded 49-cell camera cache, with one new cell prepared per frame and three combined-mesh detail levels. It is omitted for headless/atlas rendering and when far above ground. Grass materials are shared by biome rather than cloned per spatial batch. Far tree batches do not submit shadow casting beyond the useful shadow range.

Map labels reuse three styles. Text styles reuse immutable base/font/tint combinations, with fading tint quantized to 64 steps and line heights cached per window size. Map labels reuse stable styles; neither path creates unbounded per-frame style keys. Character previews have a 30 Hz render limit. Impact particles and tracers have fixed pools.

Do not run a graphics measurement beside editor builds or parallel integration groups, and never rebuild into a player folder currently under test. Record resolution, graphics device, mean frame rate and p95/p99. Twenty participant capacity and short local transport tests are not twenty-human rendering evidence.
