# Living Wildfront 0.9

Protocol 9 requires the same build on every peer. The lobby accepts twenty players including bots. The arena locks at draft start: 1–5 use 2,048 m, 6–12 use 4,096 m, 13–20 use the full 6,144 m reserve. Five belongs to Small. Practice still fills to eight; bots count. Free roam remains full size. This crops the playable area, map and round populations from a shared static world; it does not stream a different terrain asset per lobby.

Hold Alt for inventory and release to close. Existing explicit custom bindings survive. The pickaxe and its current binding occupy the first hotbar tile. Four shape shortcuts sit above weapon slots, with a separate F5 launchpad tile. Shape selection/placement precedes weapon input while building.

BuildGrid stores the initial facing and origin per connected run. BuildPiece replicates GridYaw/GridOrigin; attachments inherit these. Cells are 4 m horizontal and 3 m vertical. Turning changes the shape quarter turn relative to the local frame; R adds an offset. Support traversal tests rotated neighbors in a bounded spatial graph. The shared 512-piece / 80-per-builder budget remains. These are original mechanics; full competitive-game edit-pattern and build-fight parity is not claimed.

LaunchPad registers one network prefab. Fixed pads are distributed by region; portable loot kind 14 carries up to three. F5 requires a nearby complete allied floor, rejects duplicates, and consumes one item. Removing/editing its floor despawns the pad. The server checks contact/cooldown; the owner receives launch motion and replicates GlideState. Type 6 transport-map pins describe fixed pads without leaking an enemy's build position.

AnimalLoot adds usable equipment to food: 35% ordinary, 80% enraged, two guaranteed boss rolls. Guns, ammo, mods, vests, plates, medkits and pads are possible. There is no pelts/bones/trophy economy. WildlifeHitboxes fits imported model bounds once and records oriented extents in ShotHistory. Test head/rear/flank hits and environmental cover when replacing an animal.

The full reserve has 60 places, 14 major / 46 minor, 444 buildings, 89 road links and 112 initial vehicles. Relays (loot kind 15) call shared supply flights with a 180-second cooldown. Per-round loot and mobility filter against the selected arena.

CaveSite defines a winding connected passage graph, asymmetric chambers, a branch alcove and a loop. Floors account for mountain overburden. BuildCaves replaces aligned terrain-hole patches with an isosurface of terrain solid minus the connected cave void: the entrances and hillside form one surface. Exterior triangles retain terrain shading; interior triangles use triplanar scanned rock. Chambers have original crystal geometry, emission and three unshadowed local lights per cave. Static caves are not destructible terrain. Drops, build roots and below-ground rescue consult the same cave profile. Regenerate terrain, road coverage and atlas after changing it.

Rendering uses scanned ground detail, scanned grass/fern patches and needle-atlas fir branches with three distance levels. Village renderers switch to simple silhouettes at distance without removing collision/gameplay. Default view range is 900 m, adjustable from 750–1,800 m; scoped views retain at least 850 m. Adaptive detail is based on measured frame time, with no vendor/device rules. Pooled impact particles and tracers have fixed bounds. Profile graphics-enabled hosts; capacity tests do not establish twenty-human performance.

Thirty original announcements use offline Kokoro af_heart synthesis, event variants and bounded priority queues. Generated WAVs are shipped, generator/model are not. Critical storm information can interrupt idle banter. See notices for provenance. Physical microphones and public-internet play require a consenting human test.

Run Test-Frontier, Test-Fieldcraft, Test-Game, Test-Visual and Measure-Performance. Final evidence belongs in a new versioned baseline; never rewrite earlier results.


## Emotes

Hold C (rebindable) to open the six-way emote wheel. Move the pointer toward Wave, Cheer, Point, Salute, Clap or Shuffle and release the key to select. Return the pointer to the center before releasing to cancel. Left click selects; right click or Escape cancels. The wheel uses a visible relative pointer while camera look is held. The selection click is consumed so it cannot fire a gun.

Gestures use original skeletal posing, are authorized and timed by the server, and replicate to nearby players. The camera shows your character while emoting. Stand still on foot; moving, attacking, taking damage, entering a vehicle, gliding or dying interrupts the pose. These are six original social gestures, not imported commercial-game dances.


Fast movement plus crouch initiates a slide; crouching from rest does not. Hold Jump or time landing jumps to bunny hop. The jump buffer is 150 ms with 100 ms edge grace. Each chained hop adds 1.7 m/s, up to 24 m/s; slides can carry up to 26 m/s. Grounded running remains 9.4 m/s. Hunger still prevents high-speed travel.

Distant grass/fir layers use a cheaper vertex-lit cutout path; nearby scans keep their detailed surface lighting. Far terrain stops sampling normal/roughness maps that cannot be resolved at that distance. Village silhouettes begin around 175–300 m, with greater detail while aiming.
