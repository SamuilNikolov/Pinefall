# Contributing to Pinefall

Start with [setup](docs/SETUP.md), [architecture](docs/ARCHITECTURE.md) and the [handoff](docs/HANDOFF.md). Human and AI-assisted changes use the same review standard: understand the existing behavior, make a scoped improvement, and provide reproducible evidence.

## A normal contribution

1. Fork or clone the repository and create a branch such as `feature/vehicle-suspension`, `fix/rehost-cleanup`, or `docs/content-guide`.
2. Open an issue for a larger feature or structural migration. State the player problem, intended behavior and affected systems. Small fixes can go straight to a pull request.
3. Implement one coherent change. Preserve `.meta` GUIDs, pinned dependencies and existing network behavior unless that is the explicit purpose of the change.
4. Run the repository validator and relevant Unity checks. Explain before/after behavior, exact build/test commands, results and anything not verified.
5. Update user-facing rules, extension guidance, credits and the changelog when applicable. Include screenshots for UI changes and measured frame times for optimization claims.
6. Submit the pull request using the template. Prefer a small reviewable diff over reformatting unrelated source.

## Shared-file coordination

Several systems are spread across partial classes. Two developers editing different `Ranger*.cs` files can still change the same network schema or invariant. Agree on ownership and merge order for changes to match/session state, input/inventory, map visibility and editor generation. The scene and large animation/terrain assets are particularly expensive to merge. Follow [development workflow](docs/DEVELOPMENT.md).

## Review expectations

- Multiplayer features must handle remote clients, late joining and session exit/rehost, not only host-local play.
- Repeated actions must not duplicate rewards or consume invalid quantities. RPC requests need ownership/role, state, bounds and distance validation appropriate to the action.
- A display change must remain legible at 1280×720 and not expose enemy positions.
- Performance comparisons must name hardware, resolution, host/client roles, scene, warm-up and frame cap. Average FPS alone is insufficient when the bug is stutter.
- Test failures and missing equipment/licenses are reported honestly. Historical baseline passes do not count as tests of your new change.
- Real microphone recording and external network/security changes require explicit user consent. Automated voice checks use synthetic packets.

## Assets and licensing

Only contribute content you have rights to provide. Add source, author, license and modifications to [the notices](THIRD-PARTY-NOTICES.md) and sound selections to `AUDIO-SOURCES.json`. Retain bundled license texts. Never copy a commercial game's assets, UI art or branded audio.

No project-wide source license or contributor license agreement has been selected. Do not add an assumed MIT license or relicense third-party files. Coordinate broader redistribution terms with the owner; see [licensing status](LICENSE.md).

## Reporting bugs

Use the bug template. Include version/commit, host versus client, player count, reproduction, expected/actual behavior, relevant logs with personal addresses removed, and hardware/settings for frame-rate issues. Never include tokens, router credentials or microphone recordings in an issue.
