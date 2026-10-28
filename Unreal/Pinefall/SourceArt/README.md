# Source-art provenance

These inputs build the Unreal foundation. Original project code is covered by the repository license; third-party assets retain their own terms. Installed Epic content is intentionally absent here.

## Scanned meshes and terrain

Poly Haven assets are CC0: [license](https://polyhaven.com/license). **Powered by Poly Haven.**

- `pine_sapling_small`: [Pine Sapling Small](https://polyhaven.com/a/pine_sapling_small), 1K glTF with bark/needle maps and separate twig opacity PNG. `manifest.json` records source-file sizes and MD5 values. Derived A/B/C variants select source mesh nodes, bake transforms and ground their roots.
- `Prepared/SM_Rifle`: [Bolt Action Rifle 7.62](https://polyhaven.com/a/bolt_action_rifle_7_62), selected static body/scope/wrap/bolt/trigger meshes, normalized to 1.1 m. This is a fictional in-game weapon appearance.
- `Prepared/SM_Pistol`: [Service Pistol](https://polyhaven.com/a/service_pistol), selected body/magazine geometry, normalized to 0.3 m.
- `Prepared/SM_Fern`: [Fern 02](https://polyhaven.com/a/fern_02), grounded and scaled to 0.65 m.
- `Prepared/SM_Grass`: [Grass Medium 01](https://polyhaven.com/a/grass_medium_01), selected low mesh, grounded and scaled to 0.24 m.
- `Terrain`: derivatives of the already credited grass/rock maps in the Unity project. Exact input paths and preparation are recorded by `tools/prepare_unreal_gltf.py` and the existing notices. Opacity and normal/roughness handling are changed for Unreal; the source license stays CC0.

Prepared glTF files preserve material slots and bake selected node transforms. They are generated source intermediates, not hand-edited models. Regenerate with `python tools/prepare_unreal_gltf.py`. The converter currently assumes the selected assets' uniform scales for normal transforms; extend inverse-transpose handling before using arbitrary nonuniform transforms.

## Audio

All five WAVs are byte-identical copies of existing processed clips; they are not new recordings. The root `AUDIO-SOURCES.json` and `THIRD-PARTY-NOTICES.md` retain source selection, authors and processing details.

| Unreal source | Unity source | Attribution / license |
| --- | --- | --- |
| `Audio/Rifle.wav` | `Assets/Resources/Audio07/rifle0.wav` | Free Firearm Sound Library, Ben Jaszczak, Brian Nelson, Kevin Heras and Matthew Nanney; Mosin Nagant M_21P; CC0 |
| `Audio/Pistol.wav` | `Assets/Resources/Audio07/pistol0.wav` | Same library/authors; 1911 A_34P; CC0 |
| `Audio/Step.wav` | `Assets/Resources/Audio07/grass0.wav` | Kenney Impact Sounds, footstep_grass_000; CC0 |
| `Audio/Reload.wav` | `Assets/Resources/Audio07/reload0.wav` | Kenney RPG Audio, metalLatch; CC0 |
| `Audio/Birds.wav` | `Assets/Resources/Audio07/birds0.wav` | Ambient Bird Sounds by isaiah658; CC0 |

Source links: [firearms](https://opengameart.org/content/the-free-firearm-sound-library), [Impact Sounds](https://kenney.nl/assets/impact-sounds), [RPG Audio](https://kenney.nl/assets/rpg-audio), [birds](https://opengameart.org/content/ambient-bird-sounds). Importing an audio file does not mean its full runtime mixer/event behavior is already implemented.

## Native round additions

Seven wildlife meshes and 89 animation clips derive from the existing `Assets/Art/Animals` glTF sources: Quaternius Ultimate Animated Animal Pack, CC0. See the root third-party notices. `Scripts/import_wildlife.py` adds the existing common identity armature to the derived skin so Unreal imports a consistent skeleton for mesh and animation; original models remain unchanged. The runtime selects five clips per species. These are temporary stylized models.

`SourceArt/Audio/Feedback` contains original short generated confirmation/hurt cues, reproducible with `Scripts/build_combat_audio.py`. Additional imported `GrassStep0..4` use the credited Kenney grass clips. `Canid`, `Elk` and `Goat` use `canid0`, `elk0` and `goat0` from `Assets/Resources/Audio07`; the root audio manifest/notices retain authors, source recordings, processing and licenses. Canid is an adapted dog growl (CC BY 3.0), not a species-specific field recording.

Round UI weapon thumbnails reuse the existing credited weapon icon sources; supply icons and the Cedar atlas/materials are original generated artwork from `Scripts/build_round_assets.py`. No Fortnite or Apex visual/audio assets are copied.

## Epic bootstrap

Setup copies Quinn/Manny, template animation/input and selected Starter Content from the user's installed Unreal 5.6.1 distribution. These are Epic-provided assets subject to Epic's applicable license, not CC0 or project-authored models. They live in ignored `Content/Characters`, `Content/Input`, `Content/StarterContent`. Their use is temporary baseline scaffolding. No Fortnite/Apex assets, purchased packs or external accounts are required.

## Native ranger and scanned ground (0.2.1)

`Characters/FieldRanger` contains CC0 generated body, clothing, hair and animation outputs published by Kasper Olesen in [CharacterCreator](https://github.com/kblood/CharacterCreator). Its asset license manifest and LICENSE-NOTES distinguish these generated MakeHuman-derived outputs from the GPL generator code; that code is not bundled. `Scripts/prepare_ranger.py` is original project preparation: dressed female morphology, covered-body removal, smoothing, original hold/reload/crouch poses and eighteen exported clips. `Prepared/FieldRanger` is the reproducible derivative; runtime imports live in Art/Characters/Ranger. Regeneration requires Blender 4.3, not a runtime dependency. The normal setup uses the committed prepared/imported assets.

`Terrain/Scans` contains verified 2K diffuse, OpenGL normal and roughness maps for Poly Haven [Forest Leaves 02](https://polyhaven.com/a/forest_leaves_02) and [Rocky Terrain](https://polyhaven.com/a/rocky_terrain), CC0 under [their license](https://polyhaven.com/license). The manifest records original download checksums and SHA-256. `upgrade_world.py` builds slope/stochastic/triplanar material sampling from these maps and creates the original elliptical lake surface.


## Unreal 0.2.1 character and terrain provenance

The clothed ranger uses CC0 generated MakeHuman-derived body, shirt, jeans, boots, hair and animation outputs published by Kasper Olesen in [CharacterCreator](https://github.com/kblood/CharacterCreator). Its asset manifest and LICENSE-NOTES explicitly distinguish CC0 generated outputs from the GPL-3.0 generator. Only generated assets and license evidence are included under Unreal/Pinefall/SourceArt/Characters/FieldRanger; the upstream generator code is not bundled or linked. Pinefall's original Scripts/prepare_ranger.py dresses/bakes the character, removes covered geometry, smooths the surface and authors grip/reload/crouch clips. Prepared/FieldRanger contains eighteen clips; canonical runtime assets are Art/Characters/Ranger. Optional regeneration requires Blender 4.3. Normal build/play uses committed assets and needs no Blender. This is a temporary ranger, not a commissioned production character.

Additional scanned ground maps: Poly Haven [Forest Leaves 02](https://polyhaven.com/a/forest_leaves_02) and [Rocky Terrain](https://polyhaven.com/a/rocky_terrain), CC0 under [the publisher license](https://polyhaven.com/license). Selected 2K diffuse, OpenGL normal and roughness maps have checksum provenance in SourceArt/Terrain/Scans and are used in modified native ground materials. upgrade_world.py builds slope/stochastic/triplanar sampling and the original elliptical lake surface.
