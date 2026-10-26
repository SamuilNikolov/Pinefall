# Source repository verification — October 25, 2026

This records checks of the collaboration-ready checkout itself, separately from the historical 0.7.1 release evidence.

## Fresh import and build

The repository was imported by Unity **2022.3.13f1** with no copied `Library` or original workspace cache. Package Manager resolved the committed manifest/lock and imported the committed assets/scene. `tools/Build-Game.ps1` built `BuildReserve.BuildCurrent` into the checkout's `Builds/Windows/Pinefall.exe`, with exit code 0 and `PINEFALL_BUILD Succeeded`.

The runtime implementation is preserved from 0.7.1. Four runtime C# files received trailing-whitespace cleanup; there are no behavior changes. Editor changes make the build path checkout-local, correct setup version metadata and expose explicit road-coverage/atlas/handbook refresh commands.

## Fresh standalone gameplay suite

```powershell
.\tools\Build-Game.ps1
.\tools\Test-Game.ps1
```

The new test wrapper passed **146 assertions across ten reports**, with zero failures/captured runtime errors:

| Scenario | Passing assertions |
| --- | ---: |
| Expedition host | 69 |
| Allied / enemy client | 3 / 3 |
| Late join | 2 |
| Solo / squad practice | 6 / 7 |
| Living Reserve host | 24 |
| Living Reserve ally / enemy | 4 / 4 |
| Polish, including repeated exit/rehost | 24 |

The run used its own UDP ports starting at 17930. Existing user game processes were preserved. Test reports and player hashes are stored in [the repository-import baseline](../tests/baselines/repository-import-2026-10-25/README.md); local logs/binaries/caches are excluded from Git.

The scripted shader warnings of null/headless graphics do not serve as graphics evidence. Probe error capture begins when its handler attaches. See [TESTING](TESTING.md) for that distinction and separate native/render checks.

## Rendered performance wrapper

`tools/Measure-Performance.ps1 -Players 3 -Width 1600 -Height 900` connected two real local clients and collected all six rendered host samples, with zero captured runtime errors and zero microphone starts. The GPU was reported as NVIDIA GeForce RTX 4060 Laptop GPU. The new run is retained under the repository-import baseline, rather than substituted for the original release benchmark.

This check verifies the portable measurement workflow. It is not a controlled before/after comparison: the user's separate game and other background work remained active, and load/thermal conditions can differ from the historical release. Full-detail uncapped HUD-on samples in this run ranged from 63.8 to 72.3 FPS. Raw median/p95/p99 data is included; do not turn either historical or fresh samples into a hardware-wide FPS guarantee.

## Repository checks

`python tools/validate_repository.py` and staged `git diff --check` pass. The validator checks metadata, package pins, required files, links, file sizes and narrow hygiene patterns. Its single expected advisory concerns the existing Stag animation above 50 MiB; every file is below 100 MiB and this baseline needs no Git LFS setup. PowerShell scripts also passed syntax parsing.

GitHub CI runs repository checks only, not licensed Unity compilation. The local build and gameplay run above provide separate evidence. The initial pipeline was not claimed green before it actually ran on GitHub.

## Limits

The tests do not establish real-internet reachability, physical microphone behavior, eight-human long matches, other operating systems or lower-end hardware. The historical 32 cross-version assertions remain historical; the new suite used the newly built player on all peers. No project-wide source license has been selected.
