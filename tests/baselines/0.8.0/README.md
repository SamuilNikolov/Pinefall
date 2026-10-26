# Fieldcraft 0.8.0 release evidence

Recorded October 26, 2026 with Unity 2022.3.13f1, Windows x64 / Mono, protocol 8. `build-info.json` records the tested runtime assembly and executable hashes. All peers used that build; compatibility with earlier protocols is rejected.

- `gameplay/`: ten reports, **146 passing assertions**, zero failures/captured runtime errors.
- `fieldcraft/`: host, ally, enemy and late joiner; **73 passing assertions**, zero failures/captured runtime errors. The remote shooter confirmed **14/14** scripted shots against a moving host with 130 ms of added submission delay.
- `performance-host.json`: separate graphics-enabled host with two local clients; actual hardware/settings and frame-time tails are in the report. These are short route samples, not a universal hardware guarantee or a saturated-build stress test.
- Native captures were inspected at 1600×900 and 1280×720 during development; the final 1280×720 run includes HUD, inventory, map, settings, voice/narration, controls, grid editing, crouch, boss, RPG and spectating. Curated HUD/inventory/map images are in `docs/images`.

Headless test FPS is not rendering evidence. Unsupported shader startup messages from the null graphics device are expected in headless probes; the rendered capture/performance runs check an actual GPU separately. Runtime error handlers attach after startup.

No automated run starts a physical microphone. Voice coverage uses synthetic bursts over actual local transport plus deterministic reordering/loss/device-rate playback. Public-internet reachability, microphone/device listening, severe jitter, eight-human long matches and lower-end hardware remain unverified. See [release verification](../../../docs/VERIFICATION-0.8.md) for details and limits.
