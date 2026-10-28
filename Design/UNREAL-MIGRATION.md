# Unreal migration: a convincing game before a bigger game

Active direction, October 28, 2026. Unity development is frozen at the unfinished 0.9.1 working tree. Unreal **5.6.1** is the new target. The Unity project remains the rule reference and fallback; it is not being mechanically translated into C++.

## What must survive

[RULES-CATALOG.md](RULES-CATALOG.md) contains 120 source-linked rules and exceptions. [rules.json](rules.json) is the machine-readable ledger. [Legacy/balance.json](Legacy/balance.json) preserves eight weapons, eight starting kits, nine attachments, forty bindings, sixteen animal prefabs, sixty named sites, arena sizing and storm timing. [Legacy/source-inventory.json](Legacy/source-inventory.json) records the exact audited source hashes. Regenerate with `python tools/export_legacy_rules.py`; the catalog describes the legacy game, not Unreal feature completion.

The user superseded the old animal-reward restriction: animals may drop usable equipment, ammunition, armor, attachments and launchpads. No pelts, bones or trophies. Preserve enemy map privacy, team-aware spectating, opt-in 75 m proximity voice, separate teammate landing choices, player-relative building and momentum movement. Never silently replace these with a generic shooter template.

## First playable: Cedar Crossing

Build one attractive 504 m woodland range with hills, water, short ground cover, scanned trees, scanned weapon models, atmospheric light and a restrained HUD. Practice, host and direct-IP join share the same character and combat code. Two weapons prove input, aiming, reload state, player hit regions and damage feedback before the complete arsenal is ported. A small scene makes lighting, collision, sound and latency faults easier to isolate.

This is a foundation, not the complete reserve. Data rows for eight weapons do not mean eight functional weapon classes. An environment water surface does not mean boats or swimming. A mannequin and an unarmed locomotion graph are temporary animation scaffolding, not finished character art. Compilation does not prove visual quality or multiplayer fairness.

## Milestones and gates

| Stage | Deliverable | Required evidence before expanding |
| --- | --- | --- |
| M0 foundation | Native project, reproducible asset import, Cedar Crossing, two weapons, settings, practice/host/join | Compile; regenerate from source; visible UI/render review; two real processes connect; remote damage acknowledged |
| M1 combat feel | Weapon/hand alignment, aim offsets, recoil/recovery, reload montages, eight strict gun families, hit-region tests, bounded rewind | Host/client stationary and moving hit tests at simulated latency/loss; ammo and rate rejection; audible and visible confirmed hits; stable frame-time tails |
| M2 round loop | Lobby, teams, independent spawn draft, arena crop, storm cycles, hunger, inventory, armor, downing, spectating | Complete solo/duo/squad matches and leave/rehost loops; no private enemy data in ordinary map replication |
| M3 living world | One complete village, activities, animal ecology/rage/bosses, equipment rewards, roads, repairable vehicles and boats | A ten-minute route has useful choices and cover; uphill driving stays smooth; water connectivity and animal authority tested |
| M4 construction/mobility | Harvesting, relative grid, walls/floors/stairs/pyramids and editing, support, pads, slide/bunnyhop | Repeatable turning/stacking tests with latency, collision/support rejection and replicated movement; no speed exploits beyond deliberate momentum design |
| M5 reserve | Six biome identities, named dense settlements, caves/crystals, supplies, map, weather/day, emotes, narration, opt-in voice | Twenty-player simulation budget; graphics-enabled host with peers; map readability at 720p/1080p/1440p; voice jitter/loss using synthetic frames first |

Do not re-create the full giant reserve before M1 is enjoyable. Each stage must remain playable. Record feature status and evidence in [UNREAL-STATUS.md](UNREAL-STATUS.md); do not mark ledger rules implemented just because a corresponding class exists.

## Technical ownership

Use native C++ for authoritative rules, transactions, replication and movement prediction; data assets/tables for balance; Blueprint children and animation graphs for presentation. Python is editor tooling only. Keep the runtime module independent of UnrealEd, Landscape generation and Python. GameMode owns authority, GameState owns shared phase state, PlayerState owns team/identity, Controller owns local UI, Character owns embodiment. Persistent local settings and travel belong outside the match actor.

Movement starts with CharacterMovement prediction rather than owner-transform teleport replication. Add slide/momentum to saved moves, correction and replay together. Firing predicts only cosmetics locally; the server validates equipment, cadence, ammunition, origins and direction before awarding damage. Build bounded historical hit poses and current-cover checks before claiming equal host/client treatment. Remote aim must be tested rather than fixed by simply widening every hitbox.

Inventory, construction, animal rewards and vehicle possession require explicit validated transactions. Use stable identifiers, schema versions and compact changed-state replication. Never spawn one replicated actor per blade of grass or tree decoration. Harvest handles and foliage visuals are separate. Relevancy must not destroy authoritative collision. Empty parked vehicles sleep. Server simulation and client display have separate budgets.

Large-world production should use authored Landscape regions, World Partition/HLOD, instanced vegetation and bounded AI activation. The initial generated map is intentionally a conventional small level; it does not yet demonstrate world streaming. Avoid choosing a large framework solely to acquire a menu: keep this source approachable and add systems when their milestone needs them.

## Asset transition

Re-import source meshes/textures/audio, preserving license provenance. Unity materials, shaders, scenes, C# logic, Animator graphs, NGO state and UI do not transfer as working Unreal systems. Installed Epic mannequin/Starter Content are bootstrapped locally and ignored in Git, with Epic terms retained. CC0 scan sources and deterministic preparation/import scripts are tracked. No paid marketplace dependencies or copied Fortnite/Apex assets are required.

Official references: [Unity asset migration](https://dev.epicgames.com/documentation/unreal-engine/migrating-assets-from-unity-to-unreal-engine), [networked character movement](https://dev.epicgames.com/documentation/unreal-engine/understanding-networked-movement-in-the-character-movement-component-for-unreal-engine), [multiplayer foundation](https://dev.epicgames.com/documentation/unreal-engine/multiplayer-programming-quick-start-for-unreal-engine), [editor Python](https://dev.epicgames.com/documentation/unreal-engine/scripting-the-unreal-editor-using-python).
