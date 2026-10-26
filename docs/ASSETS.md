# Assets, imports and generated outputs

## Inputs included in the repository

`Assets/Art` contains imported source models, terrain/prop textures and fonts. `Assets/Resources` holds runtime-loaded prefabs, extracted animations, processed audio, the 4K atlas and road coverage. `Assets/Generated` includes generated terrain/material data required by the committed scene. These are source-controlled intentionally so a new contributor can import and play without reproducing the original download/generation environment.

All asset and directory `.meta` files under `Assets` are required. The repository validator rejects missing/orphaned metadata and duplicate GUIDs. A `Resources` name is also a runtime identity; moving a prefab out of that tree or renaming it requires updating the callers and registration/generation paths.

## Regeneration versus cache

| Path / artifact | Commit? | How it is produced |
| --- | --- | --- |
| `Assets/Scenes/Reserve.unity` | Yes | `BuildReserve.Setup`, then reviewed scene work |
| `Assets/Generated` / runtime prefabs / clips | Yes | Editor generation/import and asset extraction |
| `Assets/Resources/DetailedAtlas.png` | Yes | Graphics-enabled `AtlasBakeProbe`, style/import pass |
| `Assets/Resources/RoadCoverage.png` | Yes | Road-coverage editor helper; explicit refresh after road changes |
| `handbook-data.json` | Yes | Editor export; text/HTML guides still need manual edits |
| `atlas-style.sha256` | Yes | Atlas styling hash, updated when the image is styled |
| `Library`, `Temp`, `Logs`, IDE state | No | Regenerated editor-local cache |
| `Builds`, `artifacts`, captures/logs | No | Local build/test output; share reviewed release artifacts separately |

See the [content recipes](CONTENT.md) before regenerating the world. An unrelated source change should not recreate huge animation YAML or terrain assets.

## Adding third-party content

Record original author, direct source, license/version, relevant license text, modifications, destination filenames and any attribution requirement. Update `THIRD-PARTY-NOTICES.md`; sound clips also update `AUDIO-SOURCES.json`. Use clearly named originals and processed derivatives where appropriate. Do not include private download credentials, paid asset packages or unrelated tools in the repository.

The baseline uses Quaternius and Poly Haven CC0 content, OFL fonts and a mixture of CC0/public-domain/CC BY audio. Adapted animals and weapons can reuse source meshes and should not be described as dedicated realistic models. Piper-generated event WAV files are included; its neural generator/model are not a runtime dependency and are not bundled.

Project-wide licensing has not been selected; see [LICENSE.md](../LICENSE.md). Third-party license obligations are independent of that decision. A provenance record is not a completed legal audit.

## Asset scale and future art upgrades

No file in this initial source handoff exceeds the normal GitHub file ceiling, and ordinary Git can import the baseline without LFS. Some text animation clips are large. Before introducing high-resolution production models/audio, agree on LFS/object storage, budgets and release packaging rather than expanding the clone indefinitely. Do not rewrite published history without coordination.

For a realism upgrade, prefer a measured vertical slice: one improved character or species, correct rig/animations, collider and LOD setup, materials, sound, memory/render cost and remote-client behavior. Keep a licensed lightweight fallback. Better art should not reintroduce host simulation or model-loading bursts.
