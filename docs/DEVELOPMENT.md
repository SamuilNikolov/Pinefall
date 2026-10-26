# Development and structural changes

## Source workflow

Use a branch per change and pull requests for review. A direct contributor can branch from `main`; a fork contributor can add this repository as `upstream`. Fetch and rebase/merge before starting shared-file work. Never force-push another person's branch.

```sh
git switch main
git pull --ff-only
git switch -c feature/describe-the-change
```

Before a commit run `git status`, `git diff --stat`, `git diff --check` and the repository validator. Stage selected paths rather than local captures or logs. `git diff` of large generated scene/animation data needs particular attention; do not regenerate them for a UI text change.

## Unity version control

The project uses visible `.meta` files and text serialization. Each GUID is an identity used by scenes/prefabs/materials, not disposable cache metadata. Move/rename via Unity or move the companion metadata in the same commit. Keep `Packages/manifest.json` and `Packages/packages-lock.json` synchronized.

Git LFS is not required by this baseline; no committed file exceeds GitHub's normal 100 MiB ceiling. Some animation YAML files exceed the 50 MiB advisory threshold. Avoid adding larger files to ordinary Git. Decide an LFS policy with the owner before introducing heavy production art; migrating existing history is a separate coordinated action.

UnityYAMLMerge can be configured locally if desired, using the Smart Merge tool bundled with the pinned editor. Do not assume every contributor has a custom merge driver configured. Assign one person to a scene-generation pass; resolve conflicts by understanding asset identity and regeneration inputs, not by choosing an entire scene side blindly.

## Practical ownership boundaries

Use the system map to choose a work area. Examples of parallel-safe intentions after coordination are sound-bank assets, a standalone handbook page, and a vehicle visual. Each still needs an integrator when touching shared tables.

Coordinate before editing these shared hotspots:

- `ReserveSession`: services, prefab registrations, protocol and lifecycle.
- `Ranger` partials: network fields, packed equipment, input and server actions.
- `WildfrontMatch` / `SpawnDraft`: round timing, teams, supplies and selection state.
- `ReserveUI` partials / `ReadableUI`: layout constants, shared styles, focus/cursor ownership.
- `ExpeditionLayout` / editor `BuildReserve` partials / `Reserve.unity`: generated world identities and geography.

## Evolving toward clearer modules

The handoff deliberately preserves the importable, tested scene. A safe sequence for future organization is:

1. Improve formatting only in touched methods. Split an oversized method into named operations with unchanged behavior and tests.
2. Extract pure content records and validation helpers from static tables/partial classes. Keep existing IDs and serializers stable through adapters.
3. Move cohesive scripts into domain folders with their `.meta` files. Review scene/prefab import for lost references and `Resources` path changes. Keep this a behavior-neutral PR.
4. Introduce assembly definitions only after mapping dependencies. Current partial classes must remain in one assembly. Separate editor code from runtime and probes deliberately; don't create cycles among singleton-heavy domains.
5. Replace a UI panel or data boundary in a small vertical slice, retaining the existing gameplay command path. Prove remote-client and return/rehost behavior before expanding the migration.

Do not combine an editor upgrade, netcode upgrade, UI rewrite, content-ID migration and asset-folder reorganization. Each has a different failure surface and validation burden.

## Definition of done

A change explains the player benefit, compiles in the pinned editor, passes affected scenarios, preserves authoritative/privacy constraints, includes current docs/credits and leaves no accidental generated or personal files. For multiplayer/shared lifecycle changes, include late join and repeated exit/rehost. For performance, compare graphics-enabled host runs. Use the PR template to state evidence and limitations plainly.

Use semantic release versions when making a release, but review the network protocol independently. A protocol-compatible version bump is not automatic proof of wire compatibility. Update the version/export locations listed in [handoff](HANDOFF.md).
