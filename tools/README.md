# Build, test and maintain

These commands work from the repository root or another working directory because paths are resolved relative to the scripts. They require no paths from the original author's workspace. Windows PowerShell 5.1+ is supported; Python checks need 3.10+ with the standard library only.

## Repository checks

```powershell
python tools/validate_repository.py
git diff --check
```

Checks include the importable project, package lock/pins, Force Text serialization, companion metadata/unique GUIDs, normal GitHub file ceiling, local documentation links, obvious credential/private-path hygiene and consistency of historical report totals. This is a narrow hygiene check, not a complete secret scanner, asset license audit, Unity compiler or game test. Files under ignored build/cache folders are not scanned.

The warning for the baseline 52.5 MiB Stag animation is GitHub's advisory size threshold, not an error. No baseline file reaches 100 MiB. See [asset policy](../docs/ASSETS.md) before adding large art.

## Build

```powershell
$env:UNITY_EDITOR = 'C:\Program Files\Unity\Hub\Editor\2022.3.13f1\Editor\Unity.exe'
.\tools\Build-Game.ps1
.\tools\Build-Game.ps1 -UnityPath 'D:\Unity\2022.3.13f1\Editor\Unity.exe' -OutputPath 'Builds\Review\Pinefall.exe'
```

`-RegenerateWorld` explicitly recreates the scene/derived content before building; omit it for ordinary work. Close the editor for this checkout before batch builds. Logs go to `artifacts/build`. An activated editor and Windows Mono build support are required. The script does not install tools, change system settings or terminate another editor.

## Gameplay suite

```powershell
.\tools\Test-Game.ps1
.\tools\Test-Game.ps1 -Suite Expedition
.\tools\Test-Game.ps1 -Suite Living -PortBase 18930
.\tools\Test-Game.ps1 -Suite Polish -PlayerPath 'D:\Review\Pinefall.exe'
```

| Parameter | Default / meaning |
| --- | --- |
| `PlayerPath` | `Builds/Windows/Pinefall.exe`; absolute or relative to the repository |
| `Suite` | `All`, or `Expedition`, `Living`, `Polish` |
| `PortBase` | 17930; reserves base, base+1, base+4, base+10 and base+19 for sequential groups |
| `OutputPath` | Parent folder; default `artifacts/tests`; a unique run child is always created |

The suite opens hidden headless children, drives the actual integration probes, checks exits/reports and cleans up only its own process handles. A failed/empty/missing report is an error. It never records a microphone. A new run cannot accidentally consume old JSON. See [test coverage and limits](../docs/TESTING.md).

## Construction, combat, voice and native UI (0.8)

```powershell
.\tools\Test-Fieldcraft.ps1
.\tools\Test-Visual.ps1
.\tools\Test-Visual.ps1 -Width 1280 -Height 720 -OutputPath artifacts/visual-small
```

The Fieldcraft suite starts three headless players, then a late joiner; default UDP port 18100. It checks new gameplay and synthetic voice without recording a microphone. Native visual QA uses UDP 7798 and opens a normal game window. Run rendered performance separately from other test groups to avoid distorting timings.

## Graphics performance

```powershell
.\tools\Measure-Performance.ps1 -Players 3 -Width 1600 -Height 900
.\tools\Measure-Performance.ps1 -Players 1 -Port 18950
```

This opens a normal rendered host window and optional hidden/headless peers. `Players` accepts 1 or 3; default is 3. `PlayerPath`, `OutputPath`, `Port`, `Width`, and `Height` are configurable. The six-scene route takes about 75 seconds after initialization; synthetic peers finish after their bounded run. Results go to a unique folder under `artifacts/performance`.

The script verifies connections, six positive samples, no captured runtime errors and zero microphone starts. It prints the actual GPU/CPU, resolution and FPS/frame-time table. No universal FPS pass threshold is imposed. Compare like-for-like hardware/settings and examine p95/p99 as well as average FPS.

## World generation and screenshots

Explicit refresh commands are under the Unity **Pinefall** menu. [CONTENT](../docs/CONTENT.md) describes road mask, atlas photography and handbook export. [TESTING](../docs/TESTING.md) describes native screenshot capture, its reserved port and visual/audio review. Screenshots and reports stay in ignored `artifacts`; curated release evidence can be committed under a new baseline after review.
