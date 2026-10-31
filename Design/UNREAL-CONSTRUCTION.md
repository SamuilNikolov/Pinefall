# Native construction target

The target is Fortnite's classic building interaction: four dedicated shape keys, turbo placement while holding primary fire, a visible validity preview, rotate/material selection, player-relative first grid frame, connected 90-degree turns, and on-surface tile/path editing with confirm-on-release and reset. Original Pinefall geometry and materials are used. Match measured interactions rather than claiming access to Epic's private implementation.

Grid: 512 cm tile, 384 cm storey; wall canonical shared edges; floor/cone four-corner edits; wall 3×3 valid-pattern edits; ramps support straight, half-width and turn/switchback patterns. Each structural cluster retains its initial frame; targeting an existing piece adopts its frame. Snap direction has a dead band. Floors use feet/storey and downward camera intersection, not a distant horizon hit.

The server validates build distance, bounds, geometry ownership, edit lease/revision, resources, duplicate occupancy and support. Material costs are 10 with separate wood/stone/metal counters capped at 999. A local predicted static base provides responsive collision; a server request ID resolves it when the real actor arrives. Failed requests remove predictions. Current builds block shots on the server; player rewind never rewinds cover.

Harvest the existing instanced trees/rocks through sparse health changes rather than spawning a network actor for every plant. Match visual instance and hidden trunk collision once at load. Destroyed resources remove both intentionally, late join applies sparse deltas, and a round reset restores the original transforms. Decorative grass remains non-interactive. All non-placement work must stay bounded; complete structures and parked vehicles do not tick.

Editing is a world-surface grid with the mouse still controlling aim. Primary drag selects cells/path; release confirms when enabled. Secondary resets. Invalid patterns remain visibly invalid rather than accepting arbitrary voxel holes. Team edit leases expire after disconnect or abandonment. Unsupported structures collapse after support changes, using an indexed graph and a budgeted queue.

Storm presentation must be readable in sunlight and darkness: an open, finely segmented luminous wall, moving energy/intersection detail, clear outside-zone post processing and HUD warning, and purple map coverage beyond the safe circle. Rendering effects do not decide damage or safe-zone geometry.

Verification must include a real remote owner, collision through edited holes/doors/ramps, cost/rejection/late join/reset, destruction/support, and rapid rotated connected placement. A compile or a posed screenshot is not proof of Fortnite interaction parity.

## Terrain foundations (October 30 working tree)

First pieces can start directly on static terrain, rocks and building surfaces; a pre-existing player build is not required. Foundation validation samples the real piece footprint. Walls check both ends and the middle over their vertical extent, so partial terrain intersection on a slope is allowed. Floors, ramps and cones check their footprint/surface contact. Existing player pieces and hidden tree-trunk collision do not mask the underlying ground trace.

A grounded player who has moved away from the previous cluster or onto a different terrain elevation starts a fresh player-relative grid when no nearby pieces need the old alignment. Connected builds retain their grid for stairs, floors and 90-degree turns. Material cost, reach, duplicate occupancy and connected support for elevated pieces remain enforced by authority. This is permissive ground placement, not unlimited unsupported sky building.

The real-reserve probe now checks all four shapes at all four rotations on forest, desert and snowy terrain, plus partially buried wall foundations. The construction probe separately checks actual multiplayer placement, edits and unsupported collapse. Report results from their saved runs; do not equate a successful validation call with a complete playthrough of every slope on the map.

On October 31, the initial unsupported preview also gained a bounded vertical terrain fit (at most one 384 cm storey). It moves the actual new frame onto a valid surface rather than merely accepting a floating piece. Assigned frames and connected previews are never moved this way; the server still independently validates real ground/support. The wider biome/orientation test caught and corrected an intermediate sampling adjustment.
