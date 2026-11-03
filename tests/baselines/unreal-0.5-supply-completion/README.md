# Native supply completion - November 2, 2026

Unreal 5.6.1 Development Editor runtime, before the subsequently requested terrain/road redesign.

Implemented: original cargo-plane flyover, authored open canopy with suspension ropes, positional low aircraft rumble, queued prerecorded inbound/landed/claimed calls, 35% motorcycle / 25% survey / 40% ordinary special rolls, blocked-bike survey fallback, shareable receivers and bounded team-only surveys.

Survey receivers cap at two, activate through B or the revision-checked inventory action, consume one and run twelve seconds with four-second snapshots. Opposing coordinates live only on owner-only PlayerController fields for the activating team. Public GameState carries only a warning timestamp. Expiry and lobby reset clear snapshots; normal transport pins still hide enemy occupants.

Motorcycles use distinct original models, narrower collision, two animated wheels, 2,800 cm/s base speed, and the existing authoritative drive/boost/fuel/repair/exit system. Delivery checks mild dry ground and rotated body clearance, with bounded attempts and receiver fallback. Motorcycle map pins use the reserve's private transport feed. Empty vehicles sleep and reward bikes are removed on round reset. Driver body is visible in third person.

## Verification

Native compilation passed (`artifacts/specials-unlocked-build.log`). 63 round assertions passed across host/ally/enemy processes. Client logs additionally prove an ally receives one enemy snapshot, an opposing client receives no survey coordinates, and all three clients queue all three recorded supply cues. The round test accelerates crate landing, arranges health/stock and uses an identical shared static platform on each process for independent motorcycle driving; normal owner movement input performs the drive. Random delivery must be allowed to fall back when terrain is obstructed. This is deterministic integration evidence, not a natural-duration twenty-player/WAN match.

Host/join smoke and the complete existing regression sweep passed: 30 inventory/settings, 16 combat, 15 world, 24 wildlife, 86 reserve/vehicle, 24 movement and 40 construction assertions. Construction uses three processes with 80 ms packet lag; other groups use default local settings. Raw result files and relative evidence locations are in verification.json. All automation disables sound and never opens a microphone; clip loading/queueing is verified, not audible human listening.

Real 1280x720 captures of the motorcycle, aircraft and parachute were reviewed and retained. Survey-map rendering was also reviewed locally; that image contains the machine-derived player name and stays in ignored local artifacts. Fixed presentation clocks are explicit screenshot fixtures, not gameplay proof. Current models are original game art; no final-production-art, packaged-build or new FPS certification claim.

## Reproduction / compatibility

Normal clones use committed native meshes/audio/materials. Source regeneration: Python + NumPy Scripts/prepare_supply_specials.py and Scripts/build_supply_audio.py, then tools/Setup-Unreal.ps1 -RebuildSupplySpecials. Four source GLBs and the original propeller WAV are retained; imported narration reuses the existing credited Kokoro af_heart WAVs. No Blender/runtime Python/TTS/microphone dependency. Playback uses a bounded expiring queue and resets on round transition/EndPlay.

Replicated motorcycle flags, appended SurveyBeacon enum, private survey fields, public warning timestamp and multicast narration require matching native binaries. This checkpoint closes the four supply gaps named in the preceding handoff; the entire Unity migration remains separate work. No terrain, old test evidence or frozen Unity content was changed here.
