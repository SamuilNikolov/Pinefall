# Verification and reproducible checks

## Layers and their meaning

| Layer | Command / entry | Evidence |
| --- | --- | --- |
| Repository integrity | `python tools/validate_repository.py` | Required files, package pins, metadata GUIDs, link targets, file limits and hygiene; no Unity compile claim |
| Unity player build | `tools/Build-Game.ps1` | Editor exit, build marker, standalone executable and build log |
| Gameplay integration | `tools/Test-Game.ps1` | Real host/client processes, assertions and captured runtime errors in JSON |
| Frame rate / stutter | `tools/Measure-Performance.ps1` | Graphics-enabled host route, frame-time percentiles and optional peers |
| Visual/manual play | Packaged renderer / `--capture` | HUD/map/input/readability, actual audio and human game feel |
| Public internet / microphones | Consenting human session | Router reachability, jitter/loss, physical device startup and audible range |

Passing one layer does not imply the next. Headless tests cannot prove graphics performance, HUD readability, good sound or public-internet connectivity.

## Repository checks

```powershell
python tools/validate_repository.py
git diff --check
```

GitHub Actions runs the repository validator on pushes and pull requests. It needs no Unity license or secrets and does not auto-run executable code from Unity assets. There is no claim of automated editor compilation in hosted CI. A licensed Unity runner can be added later after the owner defines credential and untrusted-contribution isolation.

## Build and run the full standalone suite

```powershell
.\tools\Build-Game.ps1
.\tools\Test-Game.ps1
```

The suite is adapted from the 0.7.1 checks and uses separate local ports, unique result folders under `artifacts/tests/`, hidden headless children and bounded waits. It only terminates processes it launched. Reports must exist, contain assertions and have no `failed` or `errors`. A stale report from a prior run cannot satisfy a new run.

The full suite consists of:

- Expedition host with allied/enemy clients and a late joiner: lobby/draft, combat, mods, equipment, storm, teams, downing/revive, victory and restarting.
- Solo and four-person-team practice: bots and round objectives.
- Living Reserve host with allied/enemy clients: villages/transport, map privacy, hunger, survey timing, supply bike/beacon, synthetic voice and network show/despawn cleanup.
- Polish host: equipment/armor/water/controls/lifecycle, including three returns/rehosts with retained world/atlas.

Headless players use Unity's null graphics device and can print unsupported-shader messages during startup. They do not demonstrate a graphics failure in a rendered player or prove visual correctness. The probe's captured runtime-error list begins when its log handler attaches; inspect the full log separately for startup/crash investigation. Use the rendered route and native review for actual shader/render validation.

0.7.1 produced 146 assertions across ten reports. Counts can change with deliberate test updates; the wrapper requires nonempty passing reports and no captured errors rather than treating a historical count as the only correctness criterion. A fixed baseline assertion count is reported separately.

Useful options:

```powershell
.\tools\Test-Game.ps1 -Suite Living -PortBase 18930
.\tools\Test-Game.ps1 -Suite Polish -PlayerPath 'D:\PinefallBuild\Pinefall.exe'
```

See `tools/README.md` for all parameters. Avoid changing role/team arguments without reading the probe; some checks intentionally wait for exactly the expected teammates/enemies. Timeouts, absent late join markers and missing reports are failures.

## Graphics performance

```powershell
.\tools\Measure-Performance.ps1 -Players 3 -Width 1600 -Height 900
```

This briefly opens a rendered host window and uses two synthetic/headless peers. It waits for connections and runs six ten-second samples after two-second settles: meadow HUD, same view without HUD, village, autumn, rainforest and waterfront. The `PerformanceProbe` records actual graphics device/CPU, resolution, average FPS, median/p95/p99 milliseconds, runtime errors and microphone start count. It uses full detail and uncapped rendering for comparison; test values are not Auto-mode guarantees.

For a frame spike investigation also inspect Unity Profiler and, when available, a graphics presentation trace such as PresentMon. Keep scene, cap, peer count, day/weather, settings and background load comparable. Do not publish one GPU's timings as a promise for all hardware.

## Visual and manual QA

Run the actual player at 1280×720 and at a larger window. Check menu focus/cursor, inventory fit/drop/share, map zoom/pan/name collisions, hostile/team icon distinction, storm damage/readability, scoped shots, vehicles/boats and session return. Confirm no visually hidden object loses required physical gameplay.

Optional scripted capture (reserves UDP **7798** internally):

```powershell
$captures = Join-Path $PWD 'artifacts\visual'
New-Item -ItemType Directory -Force $captures | Out-Null
& '.\Builds\Windows\Pinefall.exe' --capture $captures --quick-ui 1 --silence 1 `
  -screen-fullscreen 0 -screen-width 1280 -screen-height 720 `
  -logFile "$captures\capture.log"
```

This is a graphics-enabled window, not a hidden/headless proof. `CaptureProbe` includes map/HUD/settings snapshots. Some hidden-window capture paths render the world but leave IMGUI black; inspect the normal native-window PNGs before accepting evidence. Capture files can include local settings; review before sharing.

`--silence 1` is for automation. Auditory quality requires listening in a separate play session: surface steps, near/far/occluded gunshots, wildlife calls, weather, engine levels and global announcements. Do not turn on physical microphones automatically.

## Compatibility and release evidence

The baseline separately tested a 0.7.1 host with two 0.7 clients: 32 assertions. Old binaries are not committed here, so the default suite does not reproduce that cross-version claim. A fresh compatibility run must name each build/commit and retain their hashes.

Historical reports are in [tests/baselines/0.7.1](../tests/baselines/0.7.1/README.md). New runs belong in ignored `artifacts`, with a summary attached to the PR. When producing a release, copy reviewed results into a new versioned baseline, document hardware and skipped checks, hash the build and retain notices. Never overwrite old JSON to make it appear newly tested.
