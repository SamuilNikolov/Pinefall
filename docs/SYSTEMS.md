# System and file map

All runtime files below are under `Assets/Scripts/` unless otherwise stated. Names are a navigation aid, not a substitute for reading the current implementation. Several classes span files and still live in Unity's default assemblies.

| Responsibility | Primary files | Common dependencies / cautions |
| --- | --- | --- |
| Startup and session lifecycle | `ReserveSession.cs`, `ReserveLobby.cs`, `SessionAddress.cs`, `LanDiscovery.cs` | Prefab registration, approval protocol, leave/rehost cleanup, local/public address display |
| Round, storm, victory | `WildfrontMatch.cs`, `StormView.cs`, `FieldNarrator.cs` | Team state, server timing, supply events, global announcements |
| Landing draft | `SpawnDraft.cs`, `FreeSpawn.cs` | Per-player turn/bid/kit, intent previews, safe ground validation |
| Player identity / core combat | `Ranger.cs`, `OwnerTransform.cs`, `NetworkSeed.cs` | Owner motion, replicated state, hit validation and global player registry |
| Hits and confirmation | `RangerCombat.cs`, `ShotHistory.cs`, `PlayerHitboxes.cs`, `CombatFeedback.cs`, `RangerDamageSignals.cs` | Rewind timeline, anatomical hit regions, current cover, owner-only confirmations, separate damage/equip events |
| Building and harvesting | `RangerConstruction.cs`, `ConstructionWorld.cs`, `BuildPiece.cs`, `HarvestResources.cs` | Validated materials/cost/support, masks/doors, deterministic sparse foliage destruction |
| Mobility and spectating | `RangerMovement.cs`, `RangerSpectator.cs`, `RangerWeaponPose.cs` | Controller/visible stance, slide momentum, team restrictions, interest viewpoint, cached arm IK |
| Rockets | `RocketProjectile.cs` | Authoritative traveling projectile, blast falloff and cover, launcher model |
| Storm bosses | `StormMutation.cs` | Closing-phase roll, cap, scale/HP, organic rewards |
| Gun timing and equipment | `RangerGunHandling.cs`, `RangerEquipment.cs`, `WeaponData.cs`, `WeaponMods.cs` | Eight positional weapon IDs, magazine/ammo state, packed attachments, bots and drops |
| Inventory / survival / armor | `RangerSlots.cs`, `RangerSurvival.cs`, `RangerArmor.cs`, `RangerRescue.cs` | Transactions, food, damage source filtering, plates, downing/revive conditions |
| Player presentation | `RangerCamera.cs`, `RangerWater.cs`, `WeaponView.cs`, `AvatarStyle.cs`, `CharacterPreview.cs`, `CinematicView.cs`, `VisualRig.cs` | Viewmodel layer, camera modes, body animations and customization |
| Population / animals | `WildlifeDirector.cs`, `Wildlife.cs`, `NativeFauna.cs`, `SharkLife.cs` | Habitat tables, peaceful/hostile rules, storm changes, harvest, bounded distant simulation |
| Practice AI | `HunterBot.cs` | Server-owned Rangers, target selection, food/loot/revive actions |
| Loot and supply | `WorldLoot.cs`, `FieldPickup.cs`, `ExpeditionLoot.cs`, `SupplyDrop.cs`, `SurveyBeacon.cs` | Item IDs/quantities, rarity, animal reward restrictions, special bike/survey rewards |
| Vehicles and water transport | `RoadVehicle.cs`, `BoatVisual.cs`, `MotorcycleVisual.cs`, `ExpeditionVisuals.cs` | Server motion, seats, fuel/condition, sleeping empty vehicles, lazy model loading |
| Geography | `ExpeditionLayout.cs`, `ReserveBiomes.cs`, `ReserveWater.cs`, `VillageFootprints.cs` | Region indices, roads, house count, lakes, bridges, spawn/loot/vehicle locations |
| Terrain and foliage | `ReserveWorld.cs`, `ReserveUnderstory.cs`, `TerrainSampling.cs`, `SoftGeometry.cs`, `AutumnCanopy.cs` | Cached TerrainData, instancing, culling/shadows; editor generation shares analytic layout |
| Weather / atmosphere | `ReserveWeather.cs`, `WeatherState.cs`, `AmbientBirds.cs`, `Soundscape.cs`, `ReserveAudio.cs` | Shared time/weather, positional calls, recorded banks and procedural layers |
| Voice | `ProximityVoice.cs`, `VoiceJitterBuffer.cs` | Opt-in mic warmup, speech gate, synthetic tests, server distance routing, receive buffers |
| Network interest | `WorldInterest.cs`, `TransportMap.cs` | Observer budgets, delayed show cancellation, team-filtered 1 Hz map feed |
| Runtime performance | `RuntimePerformance.cs`, `PresentationBudget.cs` | Frame-time feedback, detail/shadows, deferred model creation; never remove authority |
| 0.8 active presentation | `BattleHUD.cs`, `FieldcraftUI.cs`, `FieldAtlasPanel.cs` | Translucent panels, squad cards, confirmed enemy HP, world edit grid; old panel methods retained under legacy names |
| Menus / HUD | `ReserveUI.cs`, `PolishedMenus.cs`, `CombatUI.cs`, `ReadableUI.cs`, `RasterIcons.cs`, `FieldHUD.cs`, `ExpeditionUI.cs` | IMGUI event types, input ownership, style/icon cache, HUD repaint-only path |
| Atlas and handbook | `FieldAtlas.cs`, `GuideUI.cs`, `TransportMap.cs`, `WorldSign.cs` | Baked 4K base, label zoom/collision, privacy, handbook content |
| Input | `GameBindings.cs` | Rebinding, default control definitions, gameplay/menu focus |
| Opt-in integration probes | `ExpeditionProbe.cs`, `LivingReserveProbe.cs`, `PolishProbe.cs`, `WildfrontProbe.cs`, `SmokeProbe.cs` | Launched only with command-line flags; JSON reports and error collection |
| Graphics probes | `PerformanceProbe.cs`, `CaptureProbe.cs`, `AtlasBakeProbe.cs` | Actual renderer, frame-time sampling, screen captures and map photography |

## Editor generation

Under `Assets/Editor/`, `BuildReserve.cs` contains `Setup`, `Build`, `BuildCurrent`, terrain creation and base prefabs. Companion partial files add expedition content, imported assets, shark geometry, road stops, villages, bridges, atlas styling, weapon icons, road coverage and handbook export.

Important entry points:

- `BuildReserve.BuildCurrent`: prepare missing Fieldcraft prefabs/materials, export handbook data and compile the committed `Reserve.unity` into the requested Windows player path.
- `BuildReserve.Setup`: regenerate scene/prefabs/derived content and `handbook-data.json`.
- `BuildReserve.Build`: call Setup, then BuildCurrent.
- `PrepareRoadCoverage`: private helper; returns immediately if the PNG already exists. The repository adds an explicit regeneration menu for geography work.

## Cross-domain changes

| Change | Also inspect |
| --- | --- |
| New item kind | Spawn tables, server pickup/drop bounds, packed inventory, UI icon/description, bot handling, late-join replication |
| New weapon | Every weapon-index array/bitfield, thumbnails/viewmodels, mod compatibility, loot weighting, ammo, draft kits and probe assumptions |
| New vehicle type | Model/physics/seat count, collider independent of visuals, spawn/distribution, minimap glyphs, repair/fuel UI, supply fallback |
| Larger world / moved village | Terrain generation, cache, roads, foundations, water, bridges, loot/vehicles, spawn validation, atlas image/labels and probe coordinates |
| New map overlay | Team targeting, visibility/expiry, information beyond actor interest range, label collisions and input handling |
| Session manager service | Reset/despawn/leave lifecycle, static registries and three consecutive rehosts |
