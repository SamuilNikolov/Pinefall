# Pinefall agent instructions

## Read before editing

1. Read `README.md`, `docs/HANDOFF.md`, and `docs/ARCHITECTURE.md`.
2. Locate the relevant domain using `docs/SYSTEMS.md`; read its actual implementation before making claims.
3. Read `docs/TESTING.md` and select checks that exercise the behavior being changed.
4. Inspect `git status` and preserve unrelated work. Read any more specific instructions added under your target directories.

These instructions apply to the entire repository. They describe project constraints; a human's task can explicitly override workflow preferences. They do not authorize publishing, messaging, changing router/firewall settings, recording a microphone, deleting a checkout, or stopping someone else's processes.

## Current baseline

- Unity **2022.3.13f1**, Windows x64, Mono, built-in render pipeline.
- NGO **1.8.1**, glTFast **6.0.1**, protocol string **PinefallReserve/9**, maximum twenty players including bots.
- The repository root is the Unity project. Never depend on the original author's `outputs/` or `work/` directories.
- The scene is committed at `Assets/Scenes/Reserve.unity`; generated assets and all `.meta` files are intentional source inputs.
- Use `BuildReserve.BuildCurrent` for routine builds. `BuildReserve.Setup` / `BuildReserve.Build` regenerate assets and scene content.

## Preserve game and multiplayer invariants

- Damage, inventory transactions, reward generation, storm state, spawning and vehicle simulation are host-controlled. Validate client requests on the server. Owner-driven movement is an existing limitation, not a secure anti-cheat boundary.
- No ordinary enemy map tracking. Team-only survey snapshots are a deliberate timed exception. Enemy-occupied vehicle pins must stay hidden.
- The display culling budget and the authoritative simulation are separate concerns. Turning off a visual must not disable collision, gameplay, loot or map state.
- Preserve opt-in voice: no microphone capture in automated checks. Synthetic voice data is permitted in tests. Maintain 75 m routing/falloff and warm-up-on-enable behavior.
- Normally peaceful prey flees outside closing storm phases. The user explicitly changed animal rewards for 0.9: kills may drop regular equipment, ammo, armor, attachments and launchpads. Do not add pelts, bones, trophies or a crafting economy. Sharks cannot damage a player protected aboard a boat.
- Session exit/rehost must stop microphone capture, clear network registries and cancel queued visibility work while retaining the static world/atlas.
- Changes to network variables, RPC signatures, prefab registration, IDs or serialized structs require a protocol review. Do not silently promise compatibility because the version string is unchanged.

## Performance constraints

- The host previously stalled from repeated analytic terrain height queries, idle vehicle physics, far-animal work and bursty visibility updates. Read `docs/PERFORMANCE.md` before touching these systems.
- Use cached terrain samples at runtime. Do not add scene searches, string churn, per-actor player scans or new array/material allocations to hot update loops.
- Parked empty vehicles should sleep; distant simulation/visual work must remain bounded. Avoid replacing the full-road shader mask with a per-pixel road loop.
- GPU rendering was already active. Never implement vendor/device-name special cases to mask an unmeasured bottleneck.
- Headless simulation speed is not evidence of rendering FPS. Measure a graphics-enabled host with clients, settled scenes, frame-time tails and hardware/resolution recorded.

## Editing and collaboration

- Make focused changes. Avoid mass reformatting, wholesale folder moves or speculative rewrites of networking/UI.
- Preserve Unity GUIDs: move/rename through Unity or move each asset and its `.meta` together. Never regenerate all metadata.
- Add new behavior to the closest existing domain; use descriptive names and readable formatting for new code. Do not add another partial file for unrelated responsibilities.
- Do not casually change positional content IDs, packed bitfields, prefab names or `Resources.Load` paths. Read the extension recipes first.
- Keep package manifest and lock together; do not upgrade Unity/packages as an incidental cleanup.
- After geography edits, regenerate the scene, road coverage and 4K atlas as documented. Check generated diffs rather than accepting all changes blindly.
- Keep generated caches, executables, local IPs, access tokens and runtime logs out of commits. Asset provenance belongs in notices, not private download histories.
- Coordinate ownership before parallel work on `ReserveSession`, `Ranger`, `ReserveUI`, `WildfrontMatch`, `BuildReserve`, `ExpeditionLayout` or the generated scene. Separate PRs for refactors and behavior changes.

## Verify and hand off

1. Run `python tools/validate_repository.py`.
2. For C#/asset changes, compile/build in the pinned editor and run affected standalone probes. Run the full suite for shared/network/lifecycle changes.
3. For UI/audio/render changes, inspect a real rendered player. Do not label black/headless screenshots as visual verification.
4. Report exactly what ran, results, skipped checks and any unsupported compatibility claim.
5. Update `CHANGELOG.md`, affected docs and `docs/HANDOFF.md` when behavior or the working baseline changes. Use `docs/HANDOFF-TEMPLATE.md` for substantial follow-up work. Archive old evidence; never edit a historical result to represent a new run.
6. Commit/push only within the human's authorization. Leave reviewable changes and a concise explanation of outcome and remaining risk.
