# Third-party notices

## Unreal migration foundation

The Unreal project adds [Pine Sapling Small](https://polyhaven.com/a/pine_sapling_small), Poly Haven, CC0, with 1K bark/needle/normal/roughness/opacity maps. Powered by Poly Haven. Source checksums, prepared-model modifications and the five reused audio sources are documented in [Unreal source-art provenance](Unreal/Pinefall/SourceArt/README.md). The rifle, pistol, fern, grass and terrain sources retain the licenses credited below.

Epic mannequin/template/Starter Content is copied from the user's installed Unreal distribution by setup, excluded from Git and subject to Epic's applicable terms. It is not licensed under this repository's original-code license or represented as CC0. No Fortnite/Apex game assets were imported.

## Models and animations — Quaternius, CC0 1.0

Deer, stag, fox, wolf, and bull models and their animations originate from the **Ultimate Animated Animal Pack** by Quaternius. The publisher labels the pack CC0 1.0 and permits personal and commercial use.

- Publisher: https://quaternius.com/packs/ultimateanimatedanimals.html
- License: https://creativecommons.org/publicdomain/zero/1.0/
- Download mirror used: https://github.com/benjaminpjones/catch-the-animal/tree/main/models/glTF
- Files used: Deer.gltf, Stag.gltf, Fox.gltf, Wolf.gltf, and Bull.gltf. Imported and scaled for this demo; animation clips extracted. The bull is presented as a wild ox.

The ranger and humanoid animations originate from Quaternius' CC0 **Universal Base Characters (Standard)**, **Modular Character Outfits – Fantasy (Standard)**, and **Universal Animation Library (Standard)**. The ranger's component meshes were assembled in a public example project.

- Publisher: https://quaternius.com/
- Download mirror used: https://github.com/ibra-kdbra/Generate_infinite/tree/main/public/models/characters
- Provenance: that repository's README acknowledgements and tools/characters/build.mjs identify these CC0 packs.
- Files used: male_ranger.glb (renamed Hunter.glb) and animations.glb. Imported, scaled, and animation bindings adapted.

## Terrain photographs — Poly Haven, CC0

- **Aerial Grass Rock**, Rob Tuytel: https://polyhaven.com/a/aerial_grass_rock
- **Forest Floor**, eye-candy.xyz: https://polyhaven.com/a/forest_floor
- Diffuse and OpenGL normal maps downloaded at 1K. License: https://polyhaven.com/license

## Fonts — SIL Open Font License 1.1

- **Spectral**, Production Type: Assets/Art/Fonts/ReserveSerif.ttf. License included as Spectral-OFL.txt.
- **Lato**, Łukasz Dziedzic: Assets/Art/Fonts/ReserveSans.ttf. License included as Lato-OFL.txt.
- Sources: https://github.com/google/fonts/tree/main/ofl/spectral and https://github.com/google/fonts/tree/main/ofl/lato

## Engine packages

Unity and Unity packages retain their own applicable licenses. Package Manager metadata and license texts are retained with the editable project's resolved packages. Package versions are fixed in Packages/manifest.json and Packages/packages-lock.json.

Terrain geometry, procedural foliage, outpost props, weapon models, menus, weather, and synthesized sound were created for this project. No paid assets were purchased.

## Expedition additions

Horse and Alpaca use the same Quaternius Ultimate Animated Animal Pack (CC0) as the existing wildlife: https://quaternius.com/packs/ultimateanimatedanimals.html . RangerAlternate is the female ranger from the same Quaternius character collection used for the original ranger, retrieved through the previously documented public mirror; embedded WebP textures were converted to PNG. Smaller fauna, ground-cover meshes, vehicle and weapon geometry, and procedural sound clips are original runtime assets in this project. Pronghorn, elk, and arctic fox are adaptations of the existing CC0 deer, stag, and fox models, rather than dedicated production models.

## Wetlands additions — Poly Haven, CC0

Textured models and their 1K textures were obtained from the publisher's API and adapted into Unity resources:

- Service Pistol: https://polyhaven.com/a/service_pistol
- Bolt Action Rifle 7.62: https://polyhaven.com/a/bolt_action_rifle_7_62
- Fern 02: https://polyhaven.com/a/fern_02
- Bark photograph from Fir Sapling Medium: https://polyhaven.com/a/fir_sapling_medium (the high-detail sapling model is not shipped).

Additional terrain and prop materials use diffuse and OpenGL normal maps:

- Sandy Gravel 02: https://polyhaven.com/a/sandy_gravel_02
- Snow 02: https://polyhaven.com/a/snow_02
- Rock Face 03: https://polyhaven.com/a/rock_face_03
- Wood Planks Grey: https://polyhaven.com/a/wood_planks_grey
- Green Metal Rust: https://polyhaven.com/a/green_metal_rust

Publisher license: https://polyhaven.com/license . These assets are CC0. Carbine, shotgun, SMG, and support-gun appearances adapt the imported bases and original geometry; they are not separately sourced accurate weapon replicas. The prior statement that all weapons were procedural applies to the older demo, not this version.

Announcer WAV files were generated locally with the Windows System.Speech synthesizer using original game-event text. No microphone recording, celebrity imitation, or third-party recorded dialogue was used. Water, boats, aircraft, parachute, birds, bridges, UI symbols, and other procedural additions were created for this project. No paid content was purchased.

## Version 0.6 neural narration

The earlier Windows System.Speech recordings were replaced. The 0.6–0.8 event recordings used original game text synthesized offline with Piper 1.8.0 and the en_US-ljspeech-high voice. Voice model card: https://huggingface.co/rhasspy/piper-voices/blob/main/en/en_US/ljspeech/high/MODEL_CARD . The card identifies training from scratch on the public-domain LJ Speech dataset: https://keithito.com/LJ-Speech-Dataset/ . Piper source and generator license: https://github.com/OHF-Voice/piper1-gpl . Only generated WAV audio is shipped; the generator and ONNX model are not bundled or linked into the game. No paid account, microphone recording or celebrity imitation was used. Shark geometry, roadside shelters, map additions and UI are original procedural/code assets. Weapon thumbnails are renders of the previously credited Poly Haven gun assets.


## Version 0.7 recorded sound design

- Firearm reports: The Free Firearm Sound Library, Ben Jaszczak, Brian Nelson, Kevin Heras and Matthew Nanney, CC0. https://opengameart.org/content/the-free-firearm-sound-library . Selected Mosin Nagant, AR-15, Mossberg, 1911, Carl Gustav M45 and AK-47 takes are trimmed, converted to mono, filtered, level-balanced and faded for this game's fictional weapons.
- Grass/snow/wood footsteps: Impact Sounds, Kenney, CC0. https://kenney.nl/assets/impact-sounds . Sand/gravel footsteps, cloth and equipment cues: RPG Audio, Kenney, CC0. https://kenney.nl/assets/rpg-audio . Modified by trimming, filtering, level adjustment, mono conversion and fades.
- Boar calls adapted from Pig SFX Pack by Vinrax, CC BY 3.0: https://opengameart.org/content/pig-sfx-pack . License: https://creativecommons.org/licenses/by/3.0/ . Selected pig_idle files are filtered, trimmed and level-balanced.
- Canid calls adapted from Dog growls by congusbongus, derived from Dogs growling.wav by juskiddink: https://opengameart.org/content/dog-growls and https://freesound.org/people/juskiddink/sounds/121565/ . CC BY 3.0, https://creativecommons.org/licenses/by/3.0/ . Selected clips are filtered, trimmed and level-balanced. These are adapted dog growls, not species-specific wolf/fox field recordings.
- Ibex/alpaca call adapted from Goat bleat by leone: https://opengameart.org/content/goat-bleat . CC BY 4.0, https://creativecommons.org/licenses/by/4.0/ . Filtered, trimmed and level-balanced.
- Elk/stag call adapted from Elk bugle, Great Sand Dunes National Park, credited solely to NPS and identified as public domain: https://www.nps.gov/media/video/view.htm?id=7EDBAA31-84CA-4B99-AF79-E96A42164E88 . A short excerpt is filtered, level-balanced and faded. No NPS endorsement is implied.
- Quiet bird ambience: Ambient Bird Sounds by isaiah658, CC0. https://opengameart.org/content/ambient-bird-sounds . Excerpted, filtered, level-balanced and faded.

Original sound downloads and decoding tools remain in work/ and are not bundled. Only processed clips used by the game are shipped. The project records the processed filenames/source selection in AUDIO-SOURCES.json. Procedural wind, rain, thunder and vehicle-engine layers remain original code. No paid sound assets or subscriptions were used.


## Version 0.9 scanned detail and neural narration

Additional Poly Haven assets, CC0 (https://polyhaven.com/license): Aerial Grass Rock (https://polyhaven.com/a/aerial_grass_rock), Forest Floor (https://polyhaven.com/a/forest_floor), Grass Medium 01 (https://polyhaven.com/a/grass_medium_01), and the twig diffuse/opacity/normal atlas from Fir Sapling Medium (https://polyhaven.com/a/fir_sapling_medium). Ground maps are 2K; grass uses the selected mid-size mesh variant and 1K maps. Fir atlases are mapped onto original bounded branch meshes with three distance levels. The million-triangle reference tree is not bundled. Normal/roughness/opacity import settings, tint and wind are adapted for Unity. Asset downloads were verified against the source API checksums; the game makes no asset API requests. Powered by Poly Haven.

All 30 current announcer WAVs are original event text synthesized offline using Kokoro-82M / Kokoro ONNX, af_heart voice, speed 1.02. Model: https://huggingface.co/hexgrad/Kokoro-82M ; voice documentation: https://huggingface.co/hexgrad/Kokoro-82M/blob/main/VOICES.md ; generator: https://github.com/thewh1teagle/kokoro-onnx . The model and published voice assets identify Apache-2.0 licensing. Audio is normalized, faded and saved as mono 24 kHz WAV. Only generated audio is included; the neural model, voice pack and generator are not shipped or linked at runtime. The speaker is a synthetic fictional narrator, not a hired actor or an imitation of a named person. Original scripts and output hashes are recorded in AUDIO-SOURCES.json.

Cave density/meshing, branching layouts, crystal geometry/materials, launchpads, glider geometry and HUD pictograms are original project code/assets. No Fortnite or Apex assets, audio or source code were copied.

The six social emotes use original procedural skeletal poses on the previously credited characters. Wheel silhouettes, sector textures and input behavior are original code; no commercial dance animations were imported.


## Unreal 0.2.1 character and terrain provenance

The clothed ranger uses CC0 generated MakeHuman-derived body, shirt, jeans, boots, hair and animation outputs published by Kasper Olesen in [CharacterCreator](https://github.com/kblood/CharacterCreator). Its asset manifest and LICENSE-NOTES explicitly distinguish CC0 generated outputs from the GPL-3.0 generator. Only generated assets and license evidence are included under Unreal/Pinefall/SourceArt/Characters/FieldRanger; the upstream generator code is not bundled or linked. Pinefall's original Scripts/prepare_ranger.py dresses/bakes the character, removes covered geometry, smooths the surface and authors grip/reload/crouch clips. Prepared/FieldRanger contains eighteen clips; canonical runtime assets are Art/Characters/Ranger. Optional regeneration requires Blender 4.3. Normal build/play uses committed assets and needs no Blender. This is a temporary ranger, not a commissioned production character.

Additional scanned ground maps: Poly Haven [Forest Leaves 02](https://polyhaven.com/a/forest_leaves_02) and [Rocky Terrain](https://polyhaven.com/a/rocky_terrain), CC0 under [the publisher license](https://polyhaven.com/license). Selected 2K diffuse, OpenGL normal and roughness maps have checksum provenance in SourceArt/Terrain/Scans and are used in modified native ground materials. upgrade_world.py builds slope/stochastic/triplanar sampling and the original elliptical lake surface.

The native collision derivatives `Art/World/SM_Limestone` and `SM_TrunkCollision` copy Epic Starter Content SM_Rock and the Engine cylinder respectively, with project collision settings. They remain Epic-provided content under the applicable Unreal license, not CC0/project-authored geometry. The lake disk is original geometry. Setup still supplies the referenced ignored Starter Content textures/assets from the user's installed engine.
