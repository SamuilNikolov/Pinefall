# Current developer and AI-agent handoff

**Baseline:** Pinefall Wildfront 0.7.1, October 25, 2026. **Scope of this import:** preserve the working Unity source and prepare a self-contained collaboration repository. Runtime game features are not rewritten as part of this handoff.

## Start here

Read [AGENTS](../AGENTS.md), [setup](SETUP.md), [architecture](ARCHITECTURE.md) and [system map](SYSTEMS.md). The repository root is the Unity project. A fresh contributor does not need the original developer's parent workspace, historical build folders, model download cache, audio tools or chat history.

Pinned editor: **2022.3.13f1**. Main scene: `Assets/Scenes/Reserve.unity`. Runtime namespace: `Pinefall`. Target: Windows x64 / Mono / built-in rendering. NGO 1.8.1, glTFast 6.0.1. Wire approval: `PinefallReserve/7`. Maximum eight players including practice bots.

## Current game

The committed source includes battle royale/free roam, solo/duo/squad rules, bots, individual spawn bidding/free-point selection, seven guns/nine attachments, inventory/hunger/armor, vehicles/motorcycles/boats, supply specials and surveys, procedural wildlife/sharks, storm aggression, shared weather/day cycle, named settlement map, opt-in proximity voice and narration. The root handbook describes actual rules.

The world is 6,144 m square, with six core biomes plus an autumn variant, 48 named places, 304 buildings, 71 road links, five lakes and 91 starting vehicles. Baseline wildlife is about 192 land animals plus 15 sharks with replenishment/nearby herds. Counts have limits and procedural spawn constraints; do not assume every point is always populated.

## Most recent gameplay investigation

The user's key clue was **only the host stuttered**. The GPU was already rendering. The 0.7.1 fix removes repeated runtime analytic terrain calculations and idle car ground probes; budgets distant wildlife, network visibility and model creation; caches HUD drawing; bakes road coverage; and improves vegetation culling/shadows. Auto settings use frame time rather than a device/vendor rule.

Do not regress these hot paths. Read [performance engineering](PERFORMANCE.md) before editing them. Turning off a model cannot disable required collision/server state. Full-map transport uses its own targeted feed and must survive presentation/interest culling. `WorldInterest.Forget` handles an important NGO same-tick show/despawn queue issue.

## Evidence and status

- Historical Windows gameplay suite: **146 assertions**, ten reports, zero failures/captured runtime errors.
- Historical cross-version run: **32 assertions**, 0.7.1 host with 0.7 allied/enemy clients; protocol 7. 0.6 is incompatible.
- Settled graphics-enabled host + two clients: about **89–99 FPS at 1600×900** on an i5-13450HX / RTX 4060 Laptop. Full methodology, frame-time tails and limits are in root `PERFORMANCE.md` and JSON.
- Native HUD/map/settings captures were visually inspected. Voice automation never starts a physical microphone.
- Stored reports are historical release evidence. Repository plumbing/editor defaults require their own fresh import/build validation; see [testing](TESTING.md). Future contributors must not treat these prior passes as tests of new changes.

The repository handoff also received a **fresh import/build and a new 146-assertion gameplay run** using the portable wrappers. See [repository verification](REPOSITORY-VERIFICATION.md) for evidence distinct from the historical release. No runtime behavior was changed; four files received trailing-whitespace normalization for clean contribution checks.

## Repository-specific changes

The import retains generated scene/resources, all `.meta` GUIDs and package lock while excluding Unity caches/builds. Editor build output now defaults to `Builds/Windows/Pinefall.exe` within the checkout, rather than a sibling historical release directory. Editor regeneration metadata uses 0.7.1. Explicit menus refresh road coverage and atlas/handbook so geography changes can be reproduced. These changes are editor tooling; runtime gameplay source is preserved.

Portable PowerShell wrappers build, run the gameplay suite and measure a rendered route. A dependency-free Python validator checks repository structure, metadata, file sizes, documentation targets and pinned dependencies. GitHub CI runs that validator only. There is no licensed hosted Unity runner configured.

## Where the next contributor should focus

Use [ROADMAP](ROADMAP.md). High-value immediate work is longer multiplayer soak, lower-end hardware profiling, vehicle contact/handling, art/animation improvements and internet join diagnostics. For structural cleanup, extract typed content and pure rules in small steps before introducing folders/assemblies or migrating UI.

Keep existing positional content IDs, packed layouts, prefab paths and partial-class boundaries in mind. Changes crossing `Ranger`, `ReserveSession`, `ReserveUI`, `WildfrontMatch`, `ExpeditionLayout` and `BuildReserve` need shared-file coordination. Avoid combining a broad refactor with a behavior/network migration.

## Version and documentation touchpoints

Search before releasing: `BuildReserve.BuildCurrent` and `Setup` bundle version, `BuildHandbook` export version, `handbook-data.json`, visible UI labels, root README/player guide, CHANGELOG, verification/performance records and this handoff. Protocol approval/discovery version is separate; review it when schemas change.

No project-wide open-source license has been chosen. Keep all third-party notices and source attribution. No relay or account backend exists. Public-internet reachability, real microphone hardware and long human matches remain unverified.

## Handoff discipline

For a substantial change, record the resulting behavior, exact files, test evidence, remaining work and safe next step using [HANDOFF-TEMPLATE](HANDOFF-TEMPLATE.md). Keep this current summary concise and factual; archive obsolete results instead of stacking contradictory claims. A local path, private IP or chat turn is not a reproducible setup instruction.
