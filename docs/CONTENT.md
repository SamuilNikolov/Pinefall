# Extending gameplay and world content

Content is currently largely code-driven. Numeric IDs, packed state and `Resources` names connect multiple files, so adding an entry to one table is rarely sufficient. Use a small vertical slice and demonstrate it on a remote client.

## A settlement, road or activity

1. Start in `ExpeditionLayout`: regions hold names, coordinates, biome/type, vehicle and loot settings; roads reference region indices. Keep existing region order stable. Location names use two words with matching initials, such as **Copper Crossing**.
2. Inspect `VillageFootprints` and editor `BuildVillageDetails`, `BuildRoadside` and `BuildBridges`. Add an intentional layout/activity rather than increasing counts blindly. Plan streets, searchable rooms, cover, safe vehicle access and navigation sightlines.
3. Check server loot/transport spawning and free-point landing collision/water rules. Names/map labels must match the actual geometry. Avoid intersecting roads with cliffs or buildings.
4. Regenerate scene content deliberately, review prefab/terrain changes, then refresh the road coverage and atlas below. Verify foundations, driveable routes, bridge height, loot reachability, map label collisions and remote-client interest behavior.

The baseline has 48 places, 14 major villages and 34 smaller hamlets. Adding a major activity can be more valuable than duplicating empty houses. See the roadmap for possible compact objectives; those ideas are proposals, not implemented features.

## A biome or water feature

`ReserveBiomes` defines six core habitat weights and an autumn presentation variant. Biome changes reach terrain layers/shader, foliage, sound surfaces, weather coloration, habitat tables, map colors/labels and loot/travel. Append or migrate IDs explicitly; don't reorder existing biome arrays without updating every indexed consumer.

`ReserveWater` owns lake geometry/depth/surface queries. Terrain must be carved consistently, docks/bridges must line up, boats need navigable depth and sharks must obey swimming/boat protection rules. Verify shoreline spawning, leaving boats, storm circles over water and map photography. A painted blue patch alone is not a navigable lake.

## A wildlife species

1. Import an appropriately licensed model/animation with provenance. Follow the existing `BuildReserve.Animal`, `BuildExpedition` and `NativeFauna` patterns to produce a network prefab and model rig/collider.
2. Register the prefab in `ReserveSession` and add it to the appropriate `WildlifeDirector.Habitats` entries. Update species identity, size, health and behavior flags intentionally; current hostility checks include species-name semantics.
3. Keep distant simulation bounded, body animation behind presentation relevance, and required colliders independent of visual enablement. Respect the shared population cap and players' spawn buffers.
4. Add suitable sound-bank behavior and credits; avoid claiming an adapted sound/model is a species-accurate production asset.
5. Test normal flight/aggression, storm tint/enrage, hit/harvest, reward types, late join and hide/show/despawn. Animal rewards are logical survival resources; no guns, ammunition or fuel.

## A firearm or attachment

Seven gun IDs currently participate in arrays, three held slots, packed upgrade fields and four-bit-per-weapon mod state. The ninth/eighth/etc entry must not be assumed safe without auditing every capacity and shift. Existing mod IDs 1–9 are shared by bags, loot, UI and serialization.

Start with `WeaponData`, then inspect `Ranger`, `RangerEquipment`, `RangerGunHandling`, `RangerSlots`, `WeaponMods`, `WeaponView`, `WorldLoot`, `ExpeditionLoot`, draft kits, icons and bot choices. Define ammo type, damage, fire/reload timing, spread/recoil, range, rarity weights and compatible attachments. Preserve simultaneous aim/fire and server fire cadence validation.

Use actual named modifications and player-visible effects. Replace vague upgrade text only after tracing legacy upgrade fields that still influence behavior. Update inventory fit/unfit/drop behavior and handbook data. Test reloading, full inventory, invalid IDs, repeated requests, dropped gun mod preservation, damage and remote visuals. Review protocol compatibility if packing or IDs change.

## A vehicle

`RoadVehicle.Type` selects physics, capacity and visuals. In the baseline type 4 is a boat and type 5 is a motorcycle. Inspect all type branches and glyph selection; IDs are not a generalized asset catalog yet.

Add physical collider/seating before the lazy model is enabled. Empty parked vehicles must sleep without terrain/raycast work, and wake behavior must retain fuel/boost/state consistency. Verify steering at low/high speeds, reverse/braking, looking around, driver/passenger transitions, exit clearance, damage/repair/fuel and host/client synchronization. Place initial vehicles using layout/mobility/boat stops; special supplies use safe dry-ground checks and a beacon fallback. Map state must update beyond render range and hide enemy occupants.

## Refresh generated content after geography edits

1. On a clean branch, run **Pinefall → Rebuild reserve scene** or the explicit regeneration build. This regenerates terrain/static structures/prefabs, not all photograph caches.
2. Run **Pinefall → Regenerate road coverage**. The normal build helper skips coverage when its PNG exists; the explicit command overwrites it while retaining its `.meta` GUID. Review the PNG/import settings.
3. Build the current scene to an intermediate player.
4. Photograph the actual world using that graphics-enabled player:
   ```powershell
   $atlas = Join-Path $PWD 'artifacts\atlas\DetailedAtlas.png'
   New-Item -ItemType Directory -Force (Split-Path $atlas) | Out-Null
   & '.\Builds\Windows\Pinefall.exe' --bake-atlas $atlas --silence 1 `
     -screen-fullscreen 0 -logFile "$PWD\artifacts\atlas\capture.log"
   ```
   Wait for `PINEFALL_ATLAS_BAKED` and a valid 4096×4096 PNG. This needs a GPU; do not use `-nographics`.
5. Copy the PNG contents to `Assets/Resources/DetailedAtlas.png`, preserving the original `.meta`. The editor menu **Pinefall → Refresh atlas and handbook** reapplies atlas styling/import settings and exports current handbook JSON.
6. Build again and inspect map base, roads, water and label alignment. Update player handbook text/HTML manually when rules change; handbook JSON export alone does not rewrite these pages.

Also examine `atlas-style.sha256`, which prevents repeat water styling. Its hash must correspond to the new styled PNG. Do not keep stale map imagery when moving roads/villages, or regenerate it for unrelated code changes.
