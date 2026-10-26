# Third-party notices

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

The earlier Windows System.Speech recordings were replaced. The current event recordings use original game text synthesized offline with Piper 1.8.0 and the en_US-ljspeech-high voice. Voice model card: https://huggingface.co/rhasspy/piper-voices/blob/main/en/en_US/ljspeech/high/MODEL_CARD . The card identifies training from scratch on the public-domain LJ Speech dataset: https://keithito.com/LJ-Speech-Dataset/ . Piper source and generator license: https://github.com/OHF-Voice/piper1-gpl . Only generated WAV audio is shipped; the generator and ONNX model are not bundled or linked into the game. No paid account, microphone recording or celebrity imitation was used. Shark geometry, roadside shelters, map additions and UI are original procedural/code assets. Weapon thumbnails are renders of the previously credited Poly Haven gun assets.


## Version 0.7 recorded sound design

- Firearm reports: The Free Firearm Sound Library, Ben Jaszczak, Brian Nelson, Kevin Heras and Matthew Nanney, CC0. https://opengameart.org/content/the-free-firearm-sound-library . Selected Mosin Nagant, AR-15, Mossberg, 1911, Carl Gustav M45 and AK-47 takes are trimmed, converted to mono, filtered, level-balanced and faded for this game's fictional weapons.
- Grass/snow/wood footsteps: Impact Sounds, Kenney, CC0. https://kenney.nl/assets/impact-sounds . Sand/gravel footsteps, cloth and equipment cues: RPG Audio, Kenney, CC0. https://kenney.nl/assets/rpg-audio . Modified by trimming, filtering, level adjustment, mono conversion and fades.
- Boar calls adapted from Pig SFX Pack by Vinrax, CC BY 3.0: https://opengameart.org/content/pig-sfx-pack . License: https://creativecommons.org/licenses/by/3.0/ . Selected pig_idle files are filtered, trimmed and level-balanced.
- Canid calls adapted from Dog growls by congusbongus, derived from Dogs growling.wav by juskiddink: https://opengameart.org/content/dog-growls and https://freesound.org/people/juskiddink/sounds/121565/ . CC BY 3.0, https://creativecommons.org/licenses/by/3.0/ . Selected clips are filtered, trimmed and level-balanced. These are adapted dog growls, not species-specific wolf/fox field recordings.
- Ibex/alpaca call adapted from Goat bleat by leone: https://opengameart.org/content/goat-bleat . CC BY 4.0, https://creativecommons.org/licenses/by/4.0/ . Filtered, trimmed and level-balanced.
- Elk/stag call adapted from Elk bugle, Great Sand Dunes National Park, credited solely to NPS and identified as public domain: https://www.nps.gov/media/video/view.htm?id=7EDBAA31-84CA-4B99-AF79-E96A42164E88 . A short excerpt is filtered, level-balanced and faded. No NPS endorsement is implied.
- Quiet bird ambience: Ambient Bird Sounds by isaiah658, CC0. https://opengameart.org/content/ambient-bird-sounds . Excerpted, filtered, level-balanced and faded.

Original sound downloads and decoding tools remain in work/ and are not bundled. Only processed clips used by the game are shipped. The project records the processed filenames/source selection in AUDIO-SOURCES.json. Procedural wind, rain, thunder and vehicle-engine layers remain original code. No paid sound assets or subscriptions were used.
