# Pinefall Unreal reserve build

**Unreal 5.6.1 / Windows / native C++**. This is the active migration project. The repository root still contains the frozen Unity reference implementation.

Install Unreal 5.6.1 with **templates and Starter Content**, plus Visual Studio 2022's **Game development with C++** workload and a Windows SDK. No paid marketplace assets or Python package installation is required. Epic template/Starter Content files are copied from your installation and remain subject to Epic's terms.

From the repository root:

```powershell
.\tools\Setup-Unreal.ps1
.\tools\Run-Unreal.ps1
```

After setup, double-click the repository's `PLAY-UNREAL.cmd` for a quick test window. Close and reopen the game after native code or generated-map updates; an already running instance retains the old scene and controls.

Pass `-EngineRoot 'D:\Epic\UE_5.6'` to either script for a different installation. Setup builds the native modules, bootstraps ignored Epic assets and generates Cedar Crossing if absent. `Setup-Unreal.ps1 -RebuildEnvironment` deliberately replaces the generated map; do not use it on an authored scene without preserving your changes. Initial shader/Nanite processing can take several minutes.

To edit, open `Pinefall.uproject` after setup. Native code builds with `tools/Build-Unreal.ps1`. Do not check in Binaries, Intermediate, Saved or DerivedDataCache. Prepared glTF sources and their licenses are tracked; importer output belongs under `/Game/Pinefall`. Do not move or relabel installed Epic content as CC0.

## Quick play and local multiplayer

```powershell
.\tools\Run-Unreal.ps1 -Mode Practice
.\tools\Run-Unreal.ps1 -Mode Host
.\tools\Run-Unreal.ps1 -Mode Duo
.\tools\Run-Unreal.ps1 -Mode Join -Address '127.0.0.1:7777'
```

The menu also exposes practice, host and address entry. Friends need the same Unreal source/build and a reachable UDP port. This foundation does not include matchmaking or a relay. A displayed LAN address does not prove public reachability. It cannot connect to Unity clients.

Choose Host solo battle royale, Host duos or Host squads for the round flow; `-Mode Solo`, `Duo` and `Squad` also work. Everyone readies, the host starts, players split ten points between two actual kit options and landing priority, then each player claims a separate map point. Rounds now use Wildfront Reserve, a 2,016 m region with eight settlements and transport. Cedar remains the separate practice/regression arena. The full Unity reserve is still not ported.

WASD move; mouse look; Shift sprint; hold Ctrl crouch; Space jump; LMB fire; hold RMB aim; R reload; 1 rifle; 2 pistol; V first/third person; hold Alt inventory; E pickup/hold revive; M map; ] next spectator; Escape closes panels or opens the menu. Settings do not pause simulation. The separate range keeps unlimited reserve reloads and six-second respawns. Read [PLAYING.md](PLAYING.md) for the native rules, equipment, wildlife and explicit missing features.

## Source boundaries

| Module / class | Responsibility |
| --- | --- |
| `Pinefall` runtime | Native gameplay, replication, input and UMG; no editor/Python dependency |
| `PinefallEditor` | Landscape and instanced foliage generation; excluded from runtime |
| `PFMovementComponent` | Sprint in saved/compressed moves and normal CharacterMovement correction |
| `PFCharacter` | Embodiment, camera, weapon state, fire requests and confirmed damage |
| `PFHitHistoryComponent` | Bounded server-only skeletal history; current-cover-respecting lag compensation |
| `PFRangerAnimInstance` | Native locomotion, crouch and weapon-pose sampling/blending |
| `PFReserveWorld` / `PFReserveLayout` | Shared geography, server-only stock and private map feeds |
| `PFVehicle` / `PFCharacterVehicle` | Authoritative truck/boat movement, input, camera/seat state and service |
| `PFWaterBody` | Elliptical swimming volume; movement immersion and character oxygen rules |
| `PFWeaponDefinition` | Data-table schema; eight legacy rows, two enabled weapons |
| `PFGameMode` / `PFMatchRules` / `PFPlayerState` | Admission, team/readiness/draft, storm, downing, results and range respawn |
| `PFMatchState` / `PFStormVisual` | Public server-clock round state and client storm surface |
| `PFInventoryComponent` / `PFWorldPickup` | Owner inventory, revision-checked transactions, finite ammo, survival supplies and shared drops |
| `PFWildlife` / `PFWildlifeDirector` | Replicated skeletal animals, bounded ecology, aggression, attacks, mutation and rewards |
| `PFGameInstance` | Travel, address validation and cached local-address lookup |
| `PFPlayerController` / `PFInterface` | Input/menu/lobby/inventory, team-aware spectating and settings |
| `PFCombatHUD` / `PFTacticalMap` | Scope/hit/damage/brief-health feedback; atlas, circles and private team map markers |
| `Scripts/build_baseline.py` | Explicit editor import and generated environment; never runtime |

`Scripts/build_round_assets.py` produces the atlas, supplies/zone/rage materials and inventory thumbnails. `Scripts/import_wildlife.py -PFImportAllWildlife` imports the seven credited animal sources with a canonical shared skin root, verifies their clip/skeleton links and saves an import report. `Scripts/build_combat_audio.py` builds original confirmation cues and imports existing credited wildlife/footstep recordings. Editor tooling explicitly registers the installed Bink decoder for commandlet imports without opening an audio device. Ordinary runtime has no editor/Python dependency.

Read [migration phases](../../Design/UNREAL-MIGRATION.md), [presentation direction](../../Design/UNREAL-PRESENTATION.md), [status and limits](../../Design/UNREAL-STATUS.md), and [all legacy rules](../../Design/RULES-CATALOG.md). Production biome/village art, passenger seats, richer transport handling/audio, construction, additional weapons, broader latency/loss fairness, voice and further realism/animation work remain substantial work. These legacy specifications are not completion evidence.

The 0.2.1 ranger uses committed CC0 sources and eighteen prepared clips. `import_ranger.py` fits head/neck collision; add `-PFRefreshRangerAnimations` when changing prepared clips. This explicitly replaces the generated ranger asset folder; preserve any authored edits or external consumers first. Optional regeneration uses `prepare_ranger.py` in Blender 4.3, which is not required to build/play. After geography regeneration run `upgrade_world.py` and then `build_round_assets.py` to refresh ground/lake presentation and the atlas. Setup handles these passes for `-RebuildEnvironment`. Read [hit-registration design and limits](../../Design/HIT-REGISTRATION.md) before changing shot timing.

`Scripts/build_reserve.py` creates the separate reserve. `bake_reserve_atlas.py` uses the saved native terrain/layout report to produce the 2K map; `import_reserve_atlas.py` imports it. Original transport/cabin/supply source meshes are reproducible with `prepare_reserve_props.py` in Blender 4.3. `fix_stylized_materials.py` assigns existing/fresh explicit shaders and forces a skeletal-asset save; it preserves rooted/authored material graphs instead of deleting them during commandlet startup. `tools/Test-UnrealReserve.ps1` is the integration gate. Read PLAYING.md for vehicle controls and first-pass limitations.

The active character is the Quaternius-derived TrailRanger. Optional regeneration uses `merge_stylized_ranger.py`, Blender `prepare_stylized_ranger.py`, editor `import_stylized_ranger.py -PFRefreshRangerAnimations`, then `fix_stylized_materials.py`. Refresh rebuilds the entire generated asset folder because mesh-only reimports retain stale animation siblings; preserve any authored consumers first.
