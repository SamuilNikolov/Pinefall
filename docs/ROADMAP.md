# Roadmap and known limits

These are proposed follow-up work items, not implemented capabilities or commitments. Prioritize a reviewable slice that improves how a full match feels, then measure its cost.

## First priorities

| Priority | Work item | Evidence needed to finish |
| --- | --- | --- |
| P0 | Longer host/client soak and eight-player match testing | Multiple rounds, disconnect/rejoin, memory trend, return/rehost, worst frame-time tails |
| P0 | Profile lower-end and integrated GPUs | Same rendered host route/settings, Auto behavior, frame times and readable HUD at lower budgets |
| P1 | Vehicle handling and terrain-contact quality | Driveable road/bridge/shore route, responsive steering/braking, stable remote passengers and safe exit |
| P1 | Stronger connection diagnostics and join experience | Actionable errors, matching-build handling, real internet sessions and explicit relay/VPN strategy if desired |
| P1 | Input/voice/audio polish | Physical devices with consent, quiet footsteps, consistent near/far gunshot levels, no PTT warmup stutter |
| P1 | Character/animal art and locomotion quality | Licensed rigged content, transitions, LOD/collider validation, representative GPU/CPU/memory measurement |

## Structural work suitable for separate PRs

- Extract a typed content registry with stable IDs and validators while keeping compatibility adapters for packed inventory/network state. Audit every bit capacity before adding more guns/items.
- Separate pure match/draft rules from presentation and add focused deterministic tests, retaining integration probes for real NGO behavior.
- Format and simplify touched gameplay methods; reduce cross-domain singleton/static dependencies with explicit lifecycle ownership.
- Establish domain folders and later assembly definitions after mapping cycles; preserve `.meta` GUIDs and partial-class assembly boundaries.
- Migrate menus/HUD incrementally if needed, rather than replacing IMGUI and gameplay commands together. Verify controller/input focus and UI scaling in each slice.
- Define release packaging/version metadata in one place, then remove drift among editor setup, player labels, handbook and docs.
- Establish licensed Unity build automation with secure handling of external pull requests. Current hosted CI checks repository integrity only.

## Engaging world activities to prototype

Possible small objectives include timed repair stations, signal towers with a deliberate information tradeoff, wildlife rescue/avoidance encounters, shoreline salvage and multi-route settlement contracts. Prototype one with clear reward, readable UI and a bounded network/simulation cost. Respect storm pacing and avoid turning wildlife into implausible gun/fuel loot containers.

## Current limits to preserve in expectations

- Windows x64 is the tested target; other platforms and dedicated hosting are not validated.
- Up to eight players including bots, not a large commercial battle royale population.
- Direct UDP hosting only: no relay, account/authentication service, matchmaking, persistence or hardened anti-cheat.
- Human movement is owner-authoritative; server action validation is not a complete exploit defense.
- Stylized/adapted models and simple animation/vehicle physics remain in parts of the game.
- Six core biome IDs plus an autumn variant; this is not seven fully independent simulation biomes.
- Some weather/engine audio is procedural; not every call is a species-specific field recording.
- Recorded release performance measurements cover one laptop and short scripted scenes; long human games, real microphone devices and public-internet latency are unverified.
- Construction covers the four common forms and grid edits, with a bounded piece count; advanced edit patterns, replacement contests and saturated-build stress testing remain follow-ups.
- Project-wide code licensing and a long-term large-asset/LFS policy remain owner decisions.
