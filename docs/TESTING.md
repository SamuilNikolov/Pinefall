# Verification and reproducible checks

## Active Unreal round build

`tools/Test-UnrealCombat.ps1 -PacketLagMs 80` fires from actual owning controllers into standing, crouched and moving player skeletal collision, with current-cover, FP/TP, ammunition and skull checks. The setting delays outgoing packets on both processes; it is a specific artificial-delay case, not general WAN/loss evidence. Fixtures arrange positions, health and cover, never accepted hits. `tools/Test-UnrealWorld.ps1` checks saved trunk/rock cover with capsule sweeps and bullets, automatic water entry/exit, immersion and arranged air/drowning/recovery cases. Render and audible reviews remain separate.

`tools/Test-UnrealBattle.ps1` launches a host and two remote owners. It selects explicit teams, exercises ready/countdown/bidding/landing RPCs, then checks arranged inventory, armor, survival, revive, storm, spectating and results cases. Authority advances clocks and arranges damage; this is not moving-target shooting or latency evidence. `tools/Test-UnrealWildlife.ps1` checks arranged prey/predator decisions, attacks, rage/alpha state, reward transactions and remote skeletal spawning in two processes. No automated check opens a microphone.

Use `tools/Setup-Unreal.ps1 -RebuildRoundAssets` only when regenerating the round atlas/icons/materials. Wildlife import repairs the common glTF skeleton root in derived files, preserving source models; its saved report validates mesh/clip skeleton association. Audio commandlet imports explicitly load the Bink decoder module before importing recorded clips.

Use `tools/Build-Unreal.ps1` for native compilation, `tools/Setup-Unreal.ps1 -RebuildEnvironment` for explicit scene regeneration and `tools/Test-Unreal.ps1` for the two-process range smoke test. The latter requires a real remote owner confirmation after an ordinary server-validated rifle shot, not a direct health override. It uses a separate local UDP port, synthetic input and owned child processes; no microphone is enabled. Logs/results go under ignored `artifacts/unreal-tests`.

Rendered launch/captures are a separate gate. Optional `-PFCapture=<absolute PNG path> -PFQuitAfterCapture` captures the visible HUD after 25 game seconds and exits after 30. First-run shader work may require another settled capture. `-PFSmoke` hides the menu; `-PFSmokeFire` performs one guarded synthetic ordinary shot. These flags do not bypass server damage/ammo validation. A range-target smoke does not prove moving-player lag compensation, gameplay parity, audible quality or WAN reachability. See [Unreal status](../Design/UNREAL-STATUS.md).

The Unity procedures below are frozen historical/reference workflows. The final 0.9.1 suite was cancelled when the user switched engines.

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

## Fieldcraft multiplayer checks (0.8)

```powershell
.\tools\Test-Fieldcraft.ps1
```

Runs a host, allied/enemy clients, then a late joiner on a separate port. Exercises delayed remote shots against a moving host, anatomical head/body misses, crouch replication, construction cost/edits/doors/stairs/support collapse, harvesting, late-join state, boss mutation, material drops, boat seams, slide/menu gravity, spectator restrictions, blue vest feedback, RPG travel/blast/cover and synthetic voice burst/reordering/device-rate playback. No real microphone is opened. `-PlayerPath`, `-Port` and `-OutputPath` are configurable.

Use `Test-Visual.ps1` for the rendered Fieldcraft panels and geometry. `-Width 1280 -Height 720` exercises the smaller layout; default is 1600×900. It opens a normal game window briefly and captures inventory, HUD, map, edit grid, crouch, boss, launcher and spectating.

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


## Living Wildfront checks (0.9)

Run tools/Test-Frontier.ps1 against the built player. It exercises arena boundaries/freeze, population bounds, hold inventory, a 37-degree build frame and four connected wall turns, floor launchpad transactions/removal, actual launch/glide/landing, all six cave floor/ceiling collisions and underground rescue/drop rules, oriented animal body hits/misses, usable boss rewards, twenty authoritative participants/duo teams and simultaneous approval-slot reservations. It never captures a microphone. This is not a twenty-independent-client stress test.

Test-Visual.ps1 enables --layout-audit 1 and captures the real renderer. Text labels are checked with actual GUIStyle metrics. The expanded sequence includes main/host/join/browser/lobby/character, every handbook page, audio/performance/controls, draft, HUD/inventory/map, editing, stance/boss/spectator and cave/crystal/ground-cover views. Review UI_TEXT_OVERFLOW diagnostics and screenshots at 1600×900 and 1280×720. A completed capture sequence alone does not imply good layout.

The mountain road check compares graded cross-slope with the unmodified natural terrain (including water carving), rather than banning steep natural mountain slopes introduced deliberately in 0.9. It detects an added road ridge; it does not certify every road as easy to drive.

Run test groups sequentially for final evidence and keep the tested build directory immutable. Editor imports and multiple simultaneous Unity groups can interfere with startup and invalidate performance comparisons. Keep failed development evidence separate from release evidence.

Frontier also checks emote directional/center selection, hold release, click commit, visible raised-hand posing, damage/motion rejection and expiry. Fieldcraft checks an actual remote owner and host observing replicated emote state. Native captures include the wheel and a visible wave.

The final movement checks verify that stationary crouch cannot slide, fast travel can slide, holding jump produces repeated real controller jumps, and chained hops gain substantial capped speed.

## Handling/presentation checks (0.9.1)

Frontier includes camera-projected floor extension, turn dead bands, storey boundaries, a real occupied 36-degree ramp drive and anomaly population/rewards. `tools/Test-PolishVisual.ps1` opens the actual player for close ground-equipment, pickup-card, anomaly, woodland and highland captures with layout auditing. Run it after other Unity processes, then use the unchanged graphics route for a comparable host/client measurement.

The 0.9.1 expedition stock-count assertion includes the separately identified anomaly-cache population. It still compares the ordinary settlement/house/mobility/cave stock formula exactly; this is not a relaxed minimum-count check.
