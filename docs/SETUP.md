# Import, play and build

## Requirements

- Windows 10/11 x64 is the tested player environment. Source import/build on macOS or Linux is not validated; the supplied editor build entry point targets Windows x64.
- Unity Hub and **Unity 2022.3.13f1** with **Windows Build Support (Mono)** and an activated license. `ProjectSettings/ProjectVersion.txt` is authoritative.
- Git; PowerShell for the wrappers. Python 3.10+ for repository checks. No extra Python packages are required.
- Internet access on first import for Unity Package Manager. The project pins NGO 1.8.1 and glTFast 6.0.1, with transitive versions in `Packages/packages-lock.json`.
- Disk headroom for Unity's generated `Library` and build output; these are much larger than the compressed repository transfer.

There are no paid asset dependencies, external voice API keys, matchmaking accounts, neural voice generators or private files needed to run the committed game. Imported models, generated scene/prefabs/clips, road coverage and map imagery are included.

## Fresh import

Clone the repository, add its root folder to Hub and select the pinned editor. Wait for imports/package resolution; do not open an arbitrary parent folder as the project. Open `Assets/Scenes/Reserve.unity`. The scene contains the reserve application, generated terrain/static settlements, vista camera and preview herd. Press Play and host or join from the menus.

Unity may write platform-specific project settings on a first import. Review diffs before committing. `Library`, `Temp`, `Logs`, `UserSettings`, IDE state and `Builds` are ignored. The repository is configured for text serialization and visible asset metadata.

## Build the committed scene

In the editor use **Pinefall → Build current scene**. Despite the menu wording, the method builds the named `Assets/Scenes/Reserve.unity` scene, not an arbitrary unsaved active scene. Output defaults to `Builds/Windows/Pinefall.exe`.

Or from the repository root:

```powershell
$env:UNITY_EDITOR = 'C:\Program Files\Unity\Hub\Editor\2022.3.13f1\Editor\Unity.exe'
.\tools\Build-Game.ps1
```

The wrapper resolves paths from its own location, passes an explicit output path, saves a log under `artifacts/build/`, checks the process exit and `PINEFALL_BUILD Succeeded` marker, and requires the player to exist. Close this checkout's editor before a batch build; two editor instances cannot use the same `Library` safely. You do not need to close another installed game or another checkout.

Equivalent direct entry point:

```powershell
& $env:UNITY_EDITOR -batchmode -nographics -quit -projectPath (Get-Location).Path `
  -executeMethod BuildReserve.BuildCurrent `
  -pinefallBuildPath "$PWD\Builds\Windows\Pinefall.exe" `
  -logFile "$PWD\build.log"
```

Keep `Pinefall.exe`, `Pinefall_Data`, `UnityPlayer.dll` and the other build companions together. Do not commit them to source control; distribute a ZIP through a GitHub release or another artifact store.

## Regenerate the world deliberately

**Pinefall → Rebuild reserve scene** calls `BuildReserve.Setup`. **Build playable demo** calls `Setup` and then builds. Both recreate scene/static props, terrain, prefabs, some imported runtime derivatives, thumbnails and handbook data. They can overwrite manual scene/prefab edits.

The wrapper's `-RegenerateWorld` switch explicitly selects that workflow and retains graphics for thumbnail rendering:

```powershell
.\tools\Build-Game.ps1 -RegenerateWorld
```

Use a clean branch and inspect the resulting asset diffs. To refresh road coverage and atlas photography after a geography change, follow [CONTENT](CONTENT.md); rebuilding alone does not invalidate every cache.

## Multiplayer locally

Launch two copies of the built player. Host in one and join `127.0.0.1` with the same chosen port in another. Start with free roam to check basic movement, then exercise a battle round. To avoid colliding with a person's regular game, automation defaults to UDP 17930 and adjacent ports.

For reproducible multi-process checks use [tools](../tools/README.md), which launch and clean up only their own children. Do not kill every `Pinefall` process by name.

## Troubleshooting

| Symptom | Check |
| --- | --- |
| Many missing classes/packages | Correct editor, completed Package Manager import, manifest and lock both present |
| Pink terrain or missing models | Shader import errors, glTFast import, preserved `.meta` files; do not bulk delete/recreate metadata |
| Scene loses prefab references | Recover original GUID metadata from Git; renaming without the companion `.meta` breaks references |
| Build cannot open project | Close this checkout's editor and check that no batch import already owns its lock |
| Local host cannot start | Another process may own the port; choose a different port rather than terminating an unrelated session |
| LAN works, internet times out | Read [NETWORKING](NETWORKING.md); public address discovery is not a connectivity test |
| Frame rate drops only on host | Check host simulation/terrain/visibility budgets before changing GPU settings; see [PERFORMANCE](PERFORMANCE.md) |
| Huge unexpected Git diff | Check line endings, editor version and generated scene rebuild; separate intentional changes from import churn |
