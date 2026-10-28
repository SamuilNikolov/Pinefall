# Pinefall Unreal foundation

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
.\tools\Run-Unreal.ps1 -Mode Join -Address '127.0.0.1:7777'
```

The menu also exposes practice, host and address entry. Friends need the same Unreal source/build and a reachable UDP port. This foundation does not include matchmaking or a relay. A displayed LAN address does not prove public reachability. It cannot connect to Unity clients.

WASD move; mouse look; Shift sprint; hold Ctrl crouch; Space jump; LMB fire; hold RMB aim; R reload; 1 rifle; 2 pistol; V first/third person; Escape menu. Settings do not pause the simulation. The range has unlimited reserve ammunition and six-second respawns; it is not yet battle royale.

## Source boundaries

| Module / class | Responsibility |
| --- | --- |
| `Pinefall` runtime | Native gameplay, replication, input and UMG; no editor/Python dependency |
| `PinefallEditor` | Landscape and instanced foliage generation; excluded from runtime |
| `PFMovementComponent` | Sprint in saved/compressed moves and normal CharacterMovement correction |
| `PFCharacter` | Embodiment, camera, weapon state, fire requests and confirmed damage |
| `PFWeaponDefinition` | Data-table schema; eight legacy rows, two enabled weapons |
| `PFGameMode` / `PFPlayerState` | Range rules, capacity, respawn and replicated elimination count |
| `PFGameInstance` | Travel, address validation and cached local-address lookup |
| `PFPlayerController` / `PFInterface` | Local input mode, menu/HUD and settings |
| `Scripts/build_baseline.py` | Explicit editor import and generated environment; never runtime |

Read [migration phases](../../Design/UNREAL-MIGRATION.md), [presentation direction](../../Design/UNREAL-PRESENTATION.md), [status and limits](../../Design/UNREAL-STATUS.md), and [all legacy rules](../../Design/RULES-CATALOG.md). The near-term goal is a good-looking, responsive, tested combat slice before large-world feature parity.
