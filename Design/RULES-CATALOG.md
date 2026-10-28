# Pinefall rules and migration ledger

Frozen when the user stopped Unity work on October 27, 2026. This inventories behavior, user overrides and outstanding requests. It does **not** claim Unreal already implements these features. Later user decisions supersede the ledger.

Exact tables: [balance](Legacy/balance.json). Machine-readable [ledger](rules.json). Hashed [source inventory](Legacy/source-inventory.json). Regenerate with `python tools/export_legacy_rules.py`. Starting-kit fields omitted from C# default to zero.

Each rule ID needs an Unreal owner, data mapping, implementation and acceptance scene. Preserve intent and balance; redesign legacy technology and bugs. Unity metres become Unreal centimetres. The first slice is deliberately smaller than the complete reserve.

## Session

### SES-01 — Listen server

Host plays and simulates; friends join by address and UDP 7777. LAN discovery exists. Internet requires reachable routing/firewall; no relay or NAT traversal is implemented.

Unity evidence: [ReserveSession.cs](../Assets/Scripts/ReserveSession.cs), [SessionAddress.cs](../Assets/Scripts/SessionAddress.cs), [LanDiscovery.cs](../Assets/Scripts/LanDiscovery.cs). Unreal: **planned**, not ported by this inventory.

### SES-02 — Capacity and teams

Twenty participants including bots maximum; solo/duo/squad teams contain one/two/four. Team allocation and pending approvals reserve capacity; practice bots can yield a full slot.

Unity evidence: [WildfrontMatch.cs](../Assets/Scripts/WildfrontMatch.cs), [RoundArena.cs](../Assets/Scripts/RoundArena.cs), [ReserveSession.cs](../Assets/Scripts/ReserveSession.cs). Unreal: **planned**, not ported by this inventory.

### SES-03 — Ready flow

Host selects rules. Unready humans block launch and withdrawing readiness cancels countdown. Battle royale and free-roam sandbox are separate modes.

Unity evidence: [ReserveLobby.cs](../Assets/Scripts/ReserveLobby.cs). Unreal: **planned**, not ported by this inventory.

### SES-04 — Practice and late join

Practice fills to eight with bots. Bots move, scavenge, fight, swim and can be downed. Joining a draft/active round spectates until the next round.

Unity evidence: [WildfrontMatch.cs](../Assets/Scripts/WildfrontMatch.cs), [HunterBot.cs](../Assets/Scripts/HunterBot.cs). Unreal: **planned**, not ported by this inventory.

### SES-05 — Exit and rehost

Leave cleans actors, microphone, narration and registries while retaining reusable static terrain/atlas. Rehost works repeatedly; settings never pause shared physics.

Unity evidence: [ReserveSession.cs](../Assets/Scripts/ReserveSession.cs). Unreal: **planned**, not ported by this inventory.

## Draft

### DRA-01 — Budget

Each player has ten points; bid plus one of eight starting-kit costs cannot exceed ten. Exact kits and defaults are in Legacy/balance.json.

Unity evidence: [SpawnDraft.cs](../Assets/Scripts/SpawnDraft.cs), [ExpeditionLayout.cs](../Assets/Scripts/ExpeditionLayout.cs). Unreal: **planned**, not ported by this inventory.

### DRA-02 — Shared sequential choices

Each player bids and lands separately, including teammates. Bids, kit/intents and map previews replicate so all participants see collective decisions.

Unity evidence: [SpawnDraft.cs](../Assets/Scripts/SpawnDraft.cs). Unreal: **planned**, not ported by this inventory.

### DRA-03 — Priority and deadlines

High bids select first; ties rotate deterministically by round. Bid turns last 25 seconds, landing turns eighteen; bots/timeouts/disconnections have fallback decisions.

Unity evidence: [SpawnDraft.cs](../Assets/Scripts/SpawnDraft.cs). Unreal: **planned**, not ported by this inventory.

### DRA-04 — Free point selection

Choose a safe valid point inside the arena or an unclaimed named region. Resolve ground/obstructions/water; opposing selected landings reserve a sixty-metre exclusion.

Unity evidence: [SpawnDraft.cs](../Assets/Scripts/SpawnDraft.cs), [FreeSpawn.cs](../Assets/Scripts/FreeSpawn.cs). Unreal: **planned**, not ported by this inventory.

### DRA-05 — Kit tradeoff

Unspent points after bid/kit yield one spare magazine per two points. Kits vary primary/sidearm, rarity, ammo, medicine, repair supplies, fuel and armor.

Unity evidence: [RangerEquipment.cs](../Assets/Scripts/RangerEquipment.cs). Unreal: **planned**, not ported by this inventory.

### DRA-06 — Deployment reset

Only draft participants deploy; others spectate. Restore player kits/orders, construction, wildlife, loot, transport and supply schedule as a coordinated round transition.

Unity evidence: [WildfrontMatch.cs](../Assets/Scripts/WildfrontMatch.cs). Unreal: **planned**, not ported by this inventory.

## World

### WRL-01 — Arena tiers

1–5 participants: 2048 m; 6–12: 4096 m; 13–20: 6144 m. Five is small. Bots count. Arena freezes per round; free roam stays full size.

Unity evidence: [RoundArena.cs](../Assets/Scripts/RoundArena.cs). Unreal: **planned**, not ported by this inventory.

### WRL-02 — Biomes

Meadow, desert, frozen expanse, mountains, jungle and wetlands, plus autumn floodplain. Terrain, texture, foliage, animals, precipitation and sound must reflect habitat.

Unity evidence: [ReserveBiomes.cs](../Assets/Scripts/ReserveBiomes.cs), [ReserveUnderstory.cs](../Assets/Scripts/ReserveUnderstory.cs). Unreal: **planned**, not ported by this inventory.

### WRL-03 — Named places

Sixty alliterative two-word sites: fourteen major, forty-six minor; 444 buildings, 89 roads and 44 mobility stops. Exact names/coordinates/descriptions are exported.

Unity evidence: [ExpeditionLayout.cs](../Assets/Scripts/ExpeditionLayout.cs), [VillageFootprints.cs](../Assets/Scripts/VillageFootprints.cs). Unreal: **planned**, not ported by this inventory.

### WRL-04 — Water

Five lakes and river surfaces support depth, shorelines, swimming, bridges, boating and loot consistently. Painted water alone is not a gameplay water implementation.

Unity evidence: [ReserveWater.cs](../Assets/Scripts/ReserveWater.cs), [RangerWater.cs](../Assets/Scripts/RangerWater.cs). Unreal: **planned**, not ported by this inventory.

### WRL-05 — Caves and mountains

Six terrain-integrated branching caves include chambers, side routes, a loop, caches and blue/violet/teal crystals. Underground movement and drops remain underground; varied mountains/mesas supply verticality.

Unity evidence: [CaveLayout.cs](../Assets/Scripts/CaveLayout.cs). Unreal: **planned**, not ported by this inventory.

### WRL-06 — Activities

Scavenging, hunting, orders, relays, caches, mobility stops, fixed pads, supplies and anomalies fill routes. New villages need intentional cover/routes/activities, not repeated empty huts.

Unity evidence: [ExpeditionLoot.cs](../Assets/Scripts/ExpeditionLoot.cs), [FieldAnomaly.cs](../Assets/Scripts/FieldAnomaly.cs). Unreal: **planned**, not ported by this inventory.

## Storm

### STM-01 — Phases

Warmup, sequential bids, sequential landing, active battle and results are distinct server phases. Rings wait, then close; the next target is visible ahead of time.

Unity evidence: [WildfrontMatch.cs](../Assets/Scripts/WildfrontMatch.cs), [SpawnDraft.cs](../Assets/Scripts/SpawnDraft.cs). Unreal: **planned**, not ported by this inventory.

### STM-02 — Schedule

Full-size targets: 3040/2200/1520/938/500/200/0 m. Waits: 90/60/55/45/40/30/20 s; closes: 100/140/125/110/85/60/45 s. Scale radii by arena/6144. Initial radius equals arena size × 0.704.

Unity evidence: [WildfrontMatch.cs](../Assets/Scripts/WildfrontMatch.cs). Unreal: **planned**, not ported by this inventory.

### STM-03 — Moving circles

First target is centered at origin. Later targets use bounded randomized drift. Center/radius interpolate throughout closing; preserve explicit formulas until a reviewed rebalance.

Unity evidence: [WildfrontMatch.cs](../Assets/Scripts/WildfrontMatch.cs). Unreal: **planned**, not ported by this inventory.

### STM-04 — Exposure

Outside circle or arena costs 2+2×stage-index health each second. Armor never absorbs storm. Wall, tint, warning/damage feedback, map, countdown and narration communicate danger.

Unity evidence: [WildfrontMatch.cs](../Assets/Scripts/WildfrontMatch.cs), [StormView.cs](../Assets/Scripts/StormView.cs). Unreal: **planned**, not ported by this inventory.

### STM-05 — Victory

In multi-team battles the last alive team wins; zero teams gives no winner. Downed counts alive until eliminated. Results/restart restore participants for the next draft.

Unity evidence: [WildfrontMatch.cs](../Assets/Scripts/WildfrontMatch.cs). Unreal: **planned**, not ported by this inventory.

### STM-06 — Animal pressure

Closing enrages/tints living land wildlife, including normally peaceful prey. Outside closing, wildlife near the storm edge tries to reach safety.

Unity evidence: [Wildlife.cs](../Assets/Scripts/Wildlife.cs), [SharkLife.cs](../Assets/Scripts/SharkLife.cs). Unreal: **planned**, not ported by this inventory.

## Movement

### MOV-01 — Speeds and view

Walk 4.8 m/s, sprint 9.4, crouch 2.5 before hunger penalties. Select first/third person; aiming changes camera/FOV; scoped weapons can enter scoped view.

Unity evidence: [RangerMovement.cs](../Assets/Scripts/RangerMovement.cs), [RangerCamera.cs](../Assets/Scripts/RangerCamera.cs). Unreal: **planned**, not ported by this inventory.

### MOV-02 — Stamina and hunger

Sprint uses stamina with regeneration/reduced fatigue. Hunger below twenty locks sprint; below ten multiplies ordinary movement by 0.7. Avoid an unexplained low global momentum cap.

Unity evidence: [Ranger.cs](../Assets/Scripts/Ranger.cs), [RangerMovement.cs](../Assets/Scripts/RangerMovement.cs). Unreal: **planned**, not ported by this inventory.

### MOV-03 — Slide

Crouch while grounded and moving ≥6.5 m/s. Standing crouch is not sliding. Momentum entry up to 26 m/s, slope influence, 1.35 s duration, 1.7 s cooldown; jump can exit.

Unity evidence: [RangerMovement.cs](../Assets/Scripts/RangerMovement.cs). Unreal: **planned**, not ported by this inventory.

### MOV-04 — Bunny hop

150 ms jump buffer, 100 ms edge grace, held jump chaining. Repeated hops add 1.7 m/s up to 24 with air steering. Reset on teleport, respawn, water, vehicles and pads.

Unity evidence: [RangerMovement.cs](../Assets/Scripts/RangerMovement.cs), [LaunchPad.cs](../Assets/Scripts/LaunchPad.cs). Unreal: **planned**, not ported by this inventory.

### MOV-05 — Replicated stance

Other players see crouch, slide, glider and facing. Stance, animation and hit regions agree; changing only the local camera is insufficient.

Unity evidence: [RangerMovement.cs](../Assets/Scripts/RangerMovement.cs), [PlayerHitboxes.cs](../Assets/Scripts/PlayerHitboxes.cs), [AvatarStyle.cs](../Assets/Scripts/AvatarStyle.cs). Unreal: **planned**, not ported by this inventory.

### MOV-06 — Menus

Inventory/map/settings consume appropriate input without freezing gravity, vehicles, storm or others. Restore camera/cursor/input on close, death and rehost.

Unity evidence: [Ranger.cs](../Assets/Scripts/Ranger.cs), [RangerCamera.cs](../Assets/Scripts/RangerCamera.cs). Unreal: **planned**, not ported by this inventory.

### MOV-07 — Swim

Depth selects swimming and movement follows water surface. Boarding boats protects swimmers from sharks; exiting can expose them. Water behavior is distinct from ordinary ground motion.

Unity evidence: [RangerWater.cs](../Assets/Scripts/RangerWater.cs), [SharkLife.cs](../Assets/Scripts/SharkLife.cs). Unreal: **planned**, not ported by this inventory.

## Weapons

### WPN-01 — Taxonomy

Eight explicit classes: sniper, assault rifle, shotgun, pistol, SMG, marksman rifle, support gun, launcher. Category/mode/cadence/damage/spread/pellets/reload/range/zoom are data, not inferred from a model.

Unity evidence: [WeaponData.cs](../Assets/Scripts/WeaponData.cs). Unreal: **planned**, not ported by this inventory.

### WPN-02 — Definitions

Ridgeline, Trail Scout, Brushmaster, Service Nine, Kestrel, Longwatch, Sawmill and Stormbreak RPG retain exported parameter tables. Each eventually needs appropriate model, handling, sound and animation.

Unity evidence: [WeaponData.cs](../Assets/Scripts/WeaponData.cs). Unreal: **planned**, not ported by this inventory.

### WPN-03 — Rarity

Common/uncommon/rare/epic/legendary: gray/green/blue/purple/amber. Damage multiplier 1+0.065×rarity-index. Rarity cues must not erase realistic weapon materials.

Unity evidence: [WeaponData.cs](../Assets/Scripts/WeaponData.cs), [LootModels.cs](../Assets/Scripts/LootModels.cs). Unreal: **planned**, not ported by this inventory.

### WPN-04 — Aim and falloff

Class-specific range falloff; shotgun/SMG/pistol lose more than sniper/marksman. Aim/crouch affect spread; class-specific bloom/recoil apply. Aiming and shooting work simultaneously.

Unity evidence: [WeaponData.cs](../Assets/Scripts/WeaponData.cs), [RangerGunHandling.cs](../Assets/Scripts/RangerGunHandling.cs). Unreal: **planned**, not ported by this inventory.

### WPN-05 — Ammo

Pools: .308, .223, 12-gauge, 9 mm and rockets. Magazines persist per gun through slot changes/drop. Reload consumes compatible reserves only and cannot create ammo.

Unity evidence: [RangerEquipment.cs](../Assets/Scripts/RangerEquipment.cs), [RangerSlots.cs](../Assets/Scripts/RangerSlots.cs). Unreal: **planned**, not ported by this inventory.

### WPN-06 — Server validation

Validate alive/action, equipment/reload/ammo, finite origin/direction, reach, cadence, timestamps and monotonic sequences. Reject replays and client-declared damage.

Unity evidence: [RangerCombat.cs](../Assets/Scripts/RangerCombat.cs). Unreal: **planned**, not ported by this inventory.

### WPN-07 — Hit regions

Stance-aware anatomical player regions and model-fitted oriented animal bounds register front/side/rear. Player headshots multiply by 1.4. Crosshair-only client damage is invalid.

Unity evidence: [PlayerHitboxes.cs](../Assets/Scripts/PlayerHitboxes.cs), [WildlifeHitboxes.cs](../Assets/Scripts/WildlifeHitboxes.cs). Unreal: **planned**, not ported by this inventory.

### WPN-08 — Latency fairness

Legacy uses 500 ms bounded history, presentation-delay compensation, current cover and aggregate pellet damage. Unreal needs remote-shot/latency fairness tests; replicated movement alone is not parity.

Unity evidence: [ShotHistory.cs](../Assets/Scripts/ShotHistory.cs), [RangerCombat.cs](../Assets/Scripts/RangerCombat.cs). Unreal: **planned**, not ported by this inventory.

### WPN-09 — RPG

Server traveling rocket, blast falloff, cover protection and structure damage. Dedicated rocket ammo and one-round reload. Launcher accepts only the speed-loader handling mod.

Unity evidence: [RocketProjectile.cs](../Assets/Scripts/RocketProjectile.cs), [WeaponMods.cs](../Assets/Scripts/WeaponMods.cs). Unreal: **planned**, not ported by this inventory.

### WPN-10 — Feedback

Immediate local shot prediction is separate from server damage confirmation. Distinct hits/headshots/armor/break/kills, recoil, muzzle/impact/tracer, near/far positional audio and incoming feedback.

Unity evidence: [CombatFeedback.cs](../Assets/Scripts/CombatFeedback.cs), [CombatEffects.cs](../Assets/Scripts/CombatEffects.cs), [RangerDamageSignals.cs](../Assets/Scripts/RangerDamageSignals.cs). Unreal: **planned**, not ported by this inventory.

## Attachments

### MOD-01 — Slots and compatibility

Three slots: optic/barrel/handling; nine definitions exported. One per slot/weapon; marksman optic fits selected rifles/support gun; RPG accepts only speed loader.

Unity evidence: [WeaponMods.cs](../Assets/Scripts/WeaponMods.cs). Unreal: **planned**, not ported by this inventory.

### MOD-02 — Effects

Optics 1.5/3/6×; reinforced barrel +12% damage; precision barrel -25% spread; brake -30% recoil; loader -20% reload time; grip -20% spread/-15% recoil; stock -25% recoil.

Unity evidence: [WeaponMods.cs](../Assets/Scripts/WeaponMods.cs). Unreal: **planned**, not ported by this inventory.

### MOD-03 — Transactions

Bag stacks cap five. Fit consumes one and returns replaced mod if space; detach checks space. Show explicit effect/compatibility. Guns carry fitted mods when dropped/shared.

Unity evidence: [WeaponMods.cs](../Assets/Scripts/WeaponMods.cs), [RangerEquipment.cs](../Assets/Scripts/RangerEquipment.cs). Unreal: **planned**, not ported by this inventory.

## Inventory

### INV-01 — Slots

Three gun slots plus pickaxe/food/heal hotbar. Slot/icon inventory reorders without magazine loss. Hold Alt opens, release closes; saved custom bindings survive.

Unity evidence: [RangerSlots.cs](../Assets/Scripts/RangerSlots.cs), [FieldcraftUI.cs](../Assets/Scripts/FieldcraftUI.cs). Unreal: **planned**, not ported by this inventory.

### INV-02 — Capacity

Ammo 400 per ordinary pool, rockets twelve; medkits/fuel five; food eight; plates six; survey charges two; pads three; repair supplies fifty; wood/stone/metal 999 each.

Unity evidence: [RangerEquipment.cs](../Assets/Scripts/RangerEquipment.cs), [RangerSurvival.cs](../Assets/Scripts/RangerSurvival.cs). Unreal: **planned**, not ported by this inventory.

### INV-03 — Sharing

Drop guns, ammo, food, medicine, armor/plates, mods, repair/fuel, materials, pads and surveys. Partial pickups leave excess; validate range, quantity, ownership, capacity and state.

Unity evidence: [RangerSurvival.cs](../Assets/Scripts/RangerSurvival.cs), [RangerEquipment.cs](../Assets/Scripts/RangerEquipment.cs). Unreal: **planned**, not ported by this inventory.

### INV-04 — Identity and placement

Dropped guns preserve rarity/magazine/mods, vests preserve durability/tier. Drops stay on reachable cave/water surfaces rather than teleporting up through terrain.

Unity evidence: [RangerSurvival.cs](../Assets/Scripts/RangerSurvival.cs), [CaveLayout.cs](../Assets/Scripts/CaveLayout.cs). Unreal: **planned**, not ported by this inventory.

### INV-05 — Ground models

Recognizable equipment with natural surface finish and restrained rarity markers replaces generic blocks. Detailed model culling cannot remove authoritative pickup state.

Unity evidence: [LootModels.cs](../Assets/Scripts/LootModels.cs), [ExpeditionVisuals.cs](../Assets/Scripts/ExpeditionVisuals.cs). Unreal: **planned**, not ported by this inventory.

## Survival

### SUR-01 — Initial state

Health 100; normal equipment initialization gives hunger 85 and one food. Practice/free-roam equipment differs from draft kits intentionally.

Unity evidence: [Ranger.cs](../Assets/Scripts/Ranger.cs), [RangerEquipment.cs](../Assets/Scripts/RangerEquipment.cs). Unreal: **planned**, not ported by this inventory.

### SUR-02 — Hunger

Each second lose 0.15 hunger moving >5 m/s, 0.045 mounted, otherwise 0.065. UI communicates thresholds. At zero, three health damage every three seconds; sprint locks under twenty.

Unity evidence: [RangerSurvival.cs](../Assets/Scripts/RangerSurvival.cs). Unreal: **planned**, not ported by this inventory.

### SUR-03 — Food and healing

Food: +35 hunger/+5 health; medkit +45 health; rich meat +20 health/+15 hunger, capped at 100. Reject unusable/full restoration where appropriate to avoid wasting resources.

Unity evidence: [RangerSurvival.cs](../Assets/Scripts/RangerSurvival.cs), [RangerEquipment.cs](../Assets/Scripts/RangerEquipment.cs). Unreal: **planned**, not ported by this inventory.

## Armor

### ARM-01 — Tiers

Light/field/heavy durability 50/75/100; absorption 25/35/50%, bounded by remaining armor. This is source-specific protection, not a universal second health bar.

Unity evidence: [RangerArmor.cs](../Assets/Scripts/RangerArmor.cs). Unreal: **planned**, not ported by this inventory.

### ARM-02 — Sources

Gunshots and animal attacks use armor; storm, hunger and anomaly exposure bypass it. Unreal should use explicit damage types, replacing negative attacker-number conventions.

Unity evidence: [RangerArmor.cs](../Assets/Scripts/RangerArmor.cs), [FieldAnomaly.cs](../Assets/Scripts/FieldAnomaly.cs). Unreal: **planned**, not ported by this inventory.

### ARM-03 — Equip and plate

Equipping has a separate blue pulse, never a red damage flash. One plate restores thirty durability to an equipped nonfull vest; replacement leaves the old vest as loot.

Unity evidence: [RangerArmor.cs](../Assets/Scripts/RangerArmor.cs), [RangerEquipment.cs](../Assets/Scripts/RangerEquipment.cs). Unreal: **planned**, not ported by this inventory.

## Death

### DED-01 — Knock

Active duo/squad lethal damage can down if an ally fights and fewer than two previous knocks. Start downed at 35 health, thirty-second bleed; cannot fight/drive; further damage can eliminate.

Unity evidence: [Ranger.cs](../Assets/Scripts/Ranger.cs), [RangerRescue.cs](../Assets/Scripts/RangerRescue.cs). Unreal: **planned**, not ported by this inventory.

### DED-02 — Revive

Hold interact three seconds within 3.2 m; input pulses expire at 0.45 s. Damage/state/range interrupts. Revive restores 35 health and at least 25 hunger.

Unity evidence: [RangerRescue.cs](../Assets/Scripts/RangerRescue.cs). Unreal: **planned**, not ported by this inventory.

### DED-03 — Elimination

Cancel actions/vehicle, drop inventory, credit opposing killer and announce globally. Free roam respawns after six seconds; battle royale waits for next round.

Unity evidence: [RangerRescue.cs](../Assets/Scripts/RangerRescue.cs), [Ranger.cs](../Assets/Scripts/Ranger.cs). Unreal: **planned**, not ported by this inventory.

### DED-04 — Spectating

Solo: cycle all survivors. Duo/squad: surviving teammates only until the whole team dies, then everybody. Camera, interest, inputs and map privacy follow the observer.

Unity evidence: [RangerSpectator.cs](../Assets/Scripts/RangerSpectator.cs). Unreal: **planned**, not ported by this inventory.

## Wildlife

### ANI-01 — Species

Six habitat tables distribute sixteen prefab species including sharks. Exact prefab health/kind are exported. Habitat, water, clearing, slope, arena and varied session timing constrain spawning.

Unity evidence: [WildlifeDirector.cs](../Assets/Scripts/WildlifeDirector.cs), [SharkLife.cs](../Assets/Scripts/SharkLife.cs). Unreal: **planned**, not ported by this inventory.

### ANI-02 — Population

Forty land animals per available habitat, 320 live ceiling. Every four–seven seconds, relevant replenishment targets fourteen within 260 m of a human fighter, with ninety-metre player buffers.

Unity evidence: [WildlifeDirector.cs](../Assets/Scripts/WildlifeDirector.cs). Unreal: **planned**, not ported by this inventory.

### ANI-03 — Normal behavior

Boars/wolves/bears/sharks classify hostile; deer/hare/fox and other prey flee normally. Bear classification exists without an instantiated bear asset. Do not invent species as completed content.

Unity evidence: [Wildlife.cs](../Assets/Scripts/Wildlife.cs). Unreal: **planned**, not ported by this inventory.

### ANI-04 — Senses

Range/facing/LOS/crouch/speed/rage affect detection. Predators investigate gunshots, prey flees, herd members react to nearby damage. Animation/audio should telegraph state before a cheap attack.

Unity evidence: [Wildlife.cs](../Assets/Scripts/Wildlife.cs), [Soundscape.cs](../Assets/Scripts/Soundscape.cs). Unreal: **planned**, not ported by this inventory.

### ANI-05 — Mutations

Closing tints/enrages land animals. Once per stage, eligible nearby animals roll 12%, at most four live bosses. Boss scale 1.7, max HP 3×, attack 1.7×, speed 1.15×; sharks excluded.

Unity evidence: [StormMutation.cs](../Assets/Scripts/StormMutation.cs), [Wildlife.cs](../Assets/Scripts/Wildlife.cs). Unreal: **planned**, not ported by this inventory.

### ANI-06 — Hunting

Valid shots/pickaxe kill animals; rewards and corpse harvest are one-time. Death stops movement; corpses clean after eighty seconds. Small HP bars appear on relevant focus/damage.

Unity evidence: [Wildlife.cs](../Assets/Scripts/Wildlife.cs), [RangerConstruction.cs](../Assets/Scripts/RangerConstruction.cs). Unreal: **planned**, not ported by this inventory.

### ANI-07 — Explicit reward override

User superseded organic-only drops. Extra equipment chance: normal 35%, enraged 80%, boss two guaranteed rolls. No pelts, bones, storm trophies or crafting economy.

Unity evidence: [AnimalLoot.cs](../Assets/Scripts/AnimalLoot.cs). Unreal: **planned**, not ported by this inventory.

### ANI-08 — Reward distribution

Equipment roll: weapon 18%, ammo 29%, mod 20%, vest 13%, plates 10%, medkit 5%, pad 5%. Normal/enraged/boss rarities differ; bosses may yield legendary. Base food/meat is separate.

Unity evidence: [AnimalLoot.cs](../Assets/Scripts/AnimalLoot.cs), [WildfrontMatch.cs](../Assets/Scripts/WildfrontMatch.cs). Unreal: **planned**, not ported by this inventory.

### ANI-09 — Sharks

Seek fighting swimmers within 65 m, attack within 3 m for eighteen every 1.5 s; remain in sufficient water. Occupied boats protect players; no shark bosses.

Unity evidence: [SharkLife.cs](../Assets/Scripts/SharkLife.cs). Unreal: **planned**, not ported by this inventory.

## Construction

### BLD-01 — Harvest

Visible pickaxe and key; start 80 wood/40 stone/20 metal, discover/share more. Resource HP/depletion/disappearance synchronize; each resource caps 999.

Unity evidence: [RangerConstruction.cs](../Assets/Scripts/RangerConstruction.cs), [HarvestResources.cs](../Assets/Scripts/HarvestResources.cs). Unreal: **planned**, not ported by this inventory.

### BLD-02 — Pickaxe damage

45 resource damage yields normally lost-health/3 materials (minimum one); build sixty, animal thirty, player twenty. Local swing 0.48 s, server cadence 0.45 s.

Unity evidence: [RangerConstruction.cs](../Assets/Scripts/RangerConstruction.cs). Unreal: **planned**, not ported by this inventory.

### BLD-03 — Four forms

Wall/floor/stairs/pyramid, ten material each; 4 m horizontal cells/3 m storeys. Four build bindings above weapons. Hold fire turbo places at 0.09 s intervals.

Unity evidence: [BuildGrid.cs](../Assets/Scripts/BuildGrid.cs), [RangerConstruction.cs](../Assets/Scripts/RangerConstruction.cs). Unreal: **planned**, not ported by this inventory.

### BLD-04 — Orientation and snap

Start relative to camera/player facing; connected grid inherits origin/yaw. Automatic quarter turns with six-degree dead band after diagonals; R adds rotation. Aim-projected floors extend; jumps retain grid/storey.

Unity evidence: [BuildGrid.cs](../Assets/Scripts/BuildGrid.cs), [RangerConstruction.cs](../Assets/Scripts/RangerConstruction.cs). Unreal: **planned**, not ported by this inventory.

### BLD-05 — Placement

Alive/actionable/on foot, finite valid coordinates/type/material/turn, twelve-metre reach, grid alignment, materials, bounds/support, no duplicates/obstructions/player trapping.

Unity evidence: [ConstructionWorld.cs](../Assets/Scripts/ConstructionWorld.cs). Unreal: **planned**, not ported by this inventory.

### BLD-06 — Health and limits

Eighty pieces/player, 512 globally. Wood/stone/metal maxima 300/450/600 HP; new pieces start eighty and grow for six seconds. Compact context HP indicators.

Unity evidence: [ConstructionWorld.cs](../Assets/Scripts/ConstructionWorld.cs), [BuildPiece.cs](../Assets/Scripts/BuildPiece.cs). Unreal: **planned**, not ported by this inventory.

### BLD-07 — Edit forms

Nearby allied structures only. Walls use connected 3×3 masks, windows/doors/low/diagonal patterns; floors/ramps quadrants and pyramids alternate forms. Reset restores full geometry.

Unity evidence: [BuildPiece.cs](../Assets/Scripts/BuildPiece.cs), [ConstructionWorld.cs](../Assets/Scripts/ConstructionWorld.cs). Unreal: **planned**, not ported by this inventory.

### BLD-08 — Editing controls

H enters/confirms, mouse paints, RMB resets, R rotates eligible ramps/pyramids, Escape cancels. No edit closing through players. Door use/open state replicates separately.

Unity evidence: [RangerConstruction.cs](../Assets/Scripts/RangerConstruction.cs), [FieldcraftUI.cs](../Assets/Scripts/FieldcraftUI.cs). Unreal: **planned**, not ported by this inventory.

### BLD-09 — Repair and support

Two matching material repairs fifty HP. Destroy support and unsupported connected pieces collapse. Edits, doors, HP and sparse harvest state reconcile for late joiners.

Unity evidence: [ConstructionWorld.cs](../Assets/Scripts/ConstructionWorld.cs), [BuildPiece.cs](../Assets/Scripts/BuildPiece.cs). Unreal: **planned**, not ported by this inventory.

### BLD-10 — Parity limit

Four main forms/current masks exist; complete Fortnite edit patterns/prediction/art/animations were not established. Unreal must explicitly test camera-relative 90-degree building chains.

Unity evidence: [BuildGrid.cs](../Assets/Scripts/BuildGrid.cs), [BuildPiece.cs](../Assets/Scripts/BuildPiece.cs). Unreal: **planned**, not ported by this inventory.

## Launchpads

### PAD-01 — Placement

Fixed map pads plus lootable pads (carry three). F5 attaches to a complete nearby allied floor. Duplicate placement rejects without spending an item.

Unity evidence: [LaunchPad.cs](../Assets/Scripts/LaunchPad.cs). Unreal: **planned**, not ported by this inventory.

### PAD-02 — Launch

Server contact check and per-player 2.5 s cooldown. Forward 21/up 25 m/s; automatic glider on descent, steering eighteen, descent capped 4.8 m/s.

Unity evidence: [LaunchPad.cs](../Assets/Scripts/LaunchPad.cs). Unreal: **planned**, not ported by this inventory.

### PAD-03 — Support and privacy

Floor removal/edit removes mounted pad; pad 250 HP. Public fixed pins do not expose enemy built pads. Glider pose/state replicates.

Unity evidence: [LaunchPad.cs](../Assets/Scripts/LaunchPad.cs), [TransportMap.cs](../Assets/Scripts/TransportMap.cs). Unreal: **planned**, not ported by this inventory.

## Vehicles

### VEH-01 — Fleet and seats

Wagon, buggy, pickup, van, motor launch, motorcycle; 112 starting full-world vehicles including ten boats. Two seats buggy/bike, four otherwise.

Unity evidence: [RoadVehicle.cs](../Assets/Scripts/RoadVehicle.cs), [ExpeditionLoot.cs](../Assets/Scripts/ExpeditionLoot.cs). Unreal: **planned**, not ported by this inventory.

### VEH-02 — Speed

Base 29/43/35/25/29/48 m/s × (0.3+0.7×condition/100) × (1+0.12×tune). Reverse one-third; boost ×1.4 and costs charge/fuel. Exact acceleration/steering/fuel remain code-linked.

Unity evidence: [RoadVehicle.cs](../Assets/Scripts/RoadVehicle.cs). Unreal: **planned**, not ported by this inventory.

### VEH-03 — Authority and input

Server simulation; current human driver alone commands gas/steering/brake/boost. Reject nonfinite/expired input; support free-look and responsive steering. Enemy teams cannot board occupied hostile vehicles.

Unity evidence: [RoadVehicle.cs](../Assets/Scripts/RoadVehicle.cs), [RangerEquipment.cs](../Assets/Scripts/RangerEquipment.cs). Unreal: **planned**, not ported by this inventory.

### VEH-04 — Services

Two repair supplies +30 condition; one fuel can +45 fuel; three repair supplies add tune up to level two. Dead engine needs repair, empty tank fuel; show concrete effects.

Unity evidence: [RangerEquipment.cs](../Assets/Scripts/RangerEquipment.cs). Unreal: **planned**, not ported by this inventory.

### VEH-05 — Terrain handling

Drive continuous slopes at graded speed; reject cliffs/collisions, follow bridges/builds, interpolate host camera/body. Parked empty cars sleep. Do not copy legacy hard uphill stalls into Chaos.

Unity evidence: [RoadVehicle.cs](../Assets/Scripts/RoadVehicle.cs). Unreal: **planned**, not ported by this inventory.

### VEH-06 — Water travel

Boats use any valid connected water at depth ≥0.35 m, not one named lake/river. Safe shore/swim exits, aboard fighting and shark protection are required.

Unity evidence: [RoadVehicle.cs](../Assets/Scripts/RoadVehicle.cs), [RangerWater.cs](../Assets/Scripts/RangerWater.cs). Unreal: **planned**, not ported by this inventory.

### VEH-07 — Map

Type-specific live icons about once per second; condition/fuel. Hide enemy-occupied transport so icons do not expose ordinary opponent positions.

Unity evidence: [TransportMap.cs](../Assets/Scripts/TransportMap.cs). Unreal: **planned**, not ported by this inventory.

## Supplies

### SUP-01 — Schedule

First scheduled supply at 65 s, then 140 s spacing; at most six scheduled drops; no new schedule when next radius <200 m. Favor next safe zone.

Unity evidence: [SupplyDrop.cs](../Assets/Scripts/SupplyDrop.cs). Unreal: **planned**, not ported by this inventory.

### SUP-02 — Flight

Aircraft flyover, parachute, landing at 26 s; map/narrator/global cues. Interact within 4.5 m after landing; server opens and rewards once.

Unity evidence: [SupplyDrop.cs](../Assets/Scripts/SupplyDrop.cs). Unreal: **planned**, not ported by this inventory.

### SUP-03 — Specials

35% motorcycle, next 25% survey, otherwise ordinary supply. Bike obstruction fallback is survey. Base weapon/ammo/medical contents remain specified in source.

Unity evidence: [SurveyBeacon.cs](../Assets/Scripts/SurveyBeacon.cs), [SupplyDrop.cs](../Assets/Scripts/SupplyDrop.cs). Unreal: **planned**, not ported by this inventory.

### SUP-04 — Survey exception

Carry two. Twelve seconds active, opponent snapshots every four seconds for activating team only; all players warned. Ordinary opponents otherwise never appear on the map.

Unity evidence: [SurveyBeacon.cs](../Assets/Scripts/SurveyBeacon.cs). Unreal: **planned**, not ported by this inventory.

### SUP-05 — Caches and relays

Cache: mod, medicine, plates. Selected hamlet relay: contestable global supply flight, shared 180 s cooldown, not private silent loot.

Unity evidence: [ExpeditionLoot.cs](../Assets/Scripts/ExpeditionLoot.cs), [RangerEquipment.cs](../Assets/Scripts/RangerEquipment.cs). Unreal: **planned**, not ported by this inventory.

### SUP-06 — Anomalies

Attempt six/ten/fourteen sites for small/medium/full arena. Blue motes, 7 m hazard: four health every two seconds during active match, bypass armor. Securing removes hazard and adds epic gun 80%/legendary 20% plus cache supplies.

Unity evidence: [FieldAnomaly.cs](../Assets/Scripts/FieldAnomaly.cs), [RangerEquipment.cs](../Assets/Scripts/RangerEquipment.cs). Unreal: **planned**, not ported by this inventory.

## Objectives

### OBJ-01 — Field orders

Reach a nearby unvisited named region, hunt, survive a safe storm cycle, then new orders. Hunting and completion grant explicit supplies. State/rewards remain server-owned.

Unity evidence: [RangerSurvival.cs](../Assets/Scripts/RangerSurvival.cs). Unreal: **planned**, not ported by this inventory.

### OBJ-02 — Counters

Player eliminations, hunts/harvests, orders and team victory are distinct. Encourage exploration and tactical choices; no fake progression/currency UI.

Unity evidence: [Ranger.cs](../Assets/Scripts/Ranger.cs), [RangerSurvival.cs](../Assets/Scripts/RangerSurvival.cs). Unreal: **planned**, not ported by this inventory.

## Presentation

### UI-01 — Front end

Main/host/join/discovery/mode/team/roster/readiness, address+port, character, settings and complete handbook. Only functioning controls; no fake stores/passes/premium currencies/ranks.

Unity evidence: [FrontDoorUI.cs](../Assets/Scripts/FrontDoorUI.cs), [ReserveUI.cs](../Assets/Scripts/ReserveUI.cs). Unreal: **planned**, not ported by this inventory.

### UI-02 — Combat HUD

Compass/minimap, survivors/teams/kills, ring/time, health/blue armor/hunger/stamina, ammo/class/reload, tool/weapons/build bindings, notices/captions/context prompts; keep world visible.

Unity evidence: [BattleHUD.cs](../Assets/Scripts/BattleHUD.cs). Unreal: **planned**, not ported by this inventory.

### UI-03 — Team visibility

Minimal teammate HP/armor/downed/name information, head nametags and map pins. Nearby animal dots have another color. Ordinary enemies disappear from map after deployment.

Unity evidence: [BattleHUD.cs](../Assets/Scripts/BattleHUD.cs), [FieldAtlasPanel.cs](../Assets/Scripts/FieldAtlasPanel.cs). Unreal: **planned**, not ported by this inventory.

### UI-04 — Atlas

Detailed overhead terrain, zoom/pan, readable alliterative place labels with collision handling, roads/buildings/water/current-next circles and authorized team/vehicle/pad/supply/objective pins.

Unity evidence: [FieldAtlas.cs](../Assets/Scripts/FieldAtlas.cs), [FieldAtlasPanel.cs](../Assets/Scripts/FieldAtlasPanel.cs). Unreal: **planned**, not ported by this inventory.

### UI-05 — Animal radar

Approximately seventy-five-metre nearby animal dots help prey tracking; distinct from team icons. Not unrestricted opponent tracking.

Unity evidence: [FieldHUD.cs](../Assets/Scripts/FieldHUD.cs), [FieldAtlasPanel.cs](../Assets/Scripts/FieldAtlasPanel.cs). Unreal: **planned**, not ported by this inventory.

### UI-06 — Damage information

Distinct confirmed outgoing/incoming/head/armor/break/kill feedback and storm tint. Enemy HP only briefly after damage and unobstructed; no permanent reveal or through-cover HP labels.

Unity evidence: [RangerDamageSignals.cs](../Assets/Scripts/RangerDamageSignals.cs), [FieldcraftUI.cs](../Assets/Scripts/FieldcraftUI.cs). Unreal: **planned**, not ported by this inventory.

### UI-07 — Inventory

Slots/icons/rarity, quantities, armor and explicit mod effects; drop/reorder/fit/remove with background visible. No unexplained gun tuning.

Unity evidence: [FieldcraftUI.cs](../Assets/Scripts/FieldcraftUI.cs), [RangerSlots.cs](../Assets/Scripts/RangerSlots.cs). Unreal: **planned**, not ported by this inventory.

### UI-08 — Readability and bindings

Text fits supported windows, long copy scrolls, long names handled. Persist rebindings. Hold inventory/emote actions differ from toggles. Restore cursor and do not pause shared physics.

Unity evidence: [ReadableUI.cs](../Assets/Scripts/ReadableUI.cs), [GameBindings.cs](../Assets/Scripts/GameBindings.cs). Unreal: **planned**, not ported by this inventory.

### UI-09 — Appearance

Body model, palette, headgear and first/third view exist, but current character/gear meshes are partly placeholders. Do not claim that equals a realistic-human Unreal asset pipeline.

Unity evidence: [AvatarStyle.cs](../Assets/Scripts/AvatarStyle.cs), [CharacterPreview.cs](../Assets/Scripts/CharacterPreview.cs). Unreal: **planned**, not ported by this inventory.

## Audio

### AUD-01 — Soundscape

Recorded class-specific gunshots with near/far/occlusion layers, restrained surface footsteps, positional animal calls/engines/water/weather. Remove fatiguing repetition/sharp synthetic beeps.

Unity evidence: [CombatFeedback.cs](../Assets/Scripts/CombatFeedback.cs), [ReserveAudio.cs](../Assets/Scripts/ReserveAudio.cs), [Soundscape.cs](../Assets/Scripts/Soundscape.cs), [Soundscape.cs](../Assets/Scripts/Soundscape.cs). Unreal: **planned**, not ported by this inventory.

### AUD-02 — Narrator

Global storm/supply/kill/mutation/relay/victory events, varied remarks, captions and priority queue. Existing offline WAV bank is replaceable content, not a paid runtime TTS dependency.

Unity evidence: [FieldNarrator.cs](../Assets/Scripts/FieldNarrator.cs), [WildfrontMatch.cs](../Assets/Scripts/WildfrontMatch.cs). Unreal: **planned**, not ported by this inventory.

### AUD-03 — Proximity speech

Opt-in mic; PTT and open mic. Anybody including opponents hears distance-faded voice to seventy-five metres. Warm capture prevents key stutter; buffered receive handles jitter.

Unity evidence: [ProximityVoice.cs](../Assets/Scripts/ProximityVoice.cs), [VoiceJitterBuffer.cs](../Assets/Scripts/VoiceJitterBuffer.cs). Unreal: **planned**, not ported by this inventory.

### AUD-04 — Voice settings

Input/threshold/output/mutes/talk indicators; stop capture on disable/leave. No microphone automation. Synthetic packets prove routing, not physical-device or internet audio quality.

Unity evidence: [ProximityVoice.cs](../Assets/Scripts/ProximityVoice.cs). Unreal: **planned**, not ported by this inventory.

## Weather

### WTH-01 — Cycle

Full day 300 s of play. Host clear/overcast/storm selection; weather cycles every 180 s if enabled. Biome-specific rain/snow/dust; time continues in menus.

Unity evidence: [WeatherState.cs](../Assets/Scripts/WeatherState.cs), [ReserveWeather.cs](../Assets/Scripts/ReserveWeather.cs). Unreal: **planned**, not ported by this inventory.

### WTH-02 — Atmosphere

Shared wetness/cloud/sky/fog/light/precipitation and delayed thunder. Battle storm exposure is separate from ordinary weather. Cinematic changes must preserve opponents and prey readability.

Unity evidence: [ReserveWeather.cs](../Assets/Scripts/ReserveWeather.cs), [StormView.cs](../Assets/Scripts/StormView.cs). Unreal: **planned**, not ported by this inventory.

## Emotes

### EMO-01 — Radial input

Hold C, mouse direction, release to choose; center cancels; LMB commits, RMB/Escape cancels. Selection click cannot shoot. Rebindable action.

Unity evidence: [EmoteWheel.cs](../Assets/Scripts/EmoteWheel.cs). Unreal: **planned**, not ported by this inventory.

### EMO-02 — Gestures

Wave/Cheer/Point/Salute/Clap/Shuffle, finite replicated original poses. Alive, grounded, stationary, on-foot only. Motion, combat, damage, water/vehicles and expiry interrupt.

Unity evidence: [RangerEmotes.cs](../Assets/Scripts/RangerEmotes.cs). Unreal: **planned**, not ported by this inventory.

## Engineering

### NET-01 — Authority redesign

Server controls damage, inventory, rewards, storm, draft, animals, cars and building. Replace legacy owner-driven movement with Unreal prediction/correction; it is not a secure anti-cheat model.

Unity evidence: [OwnerTransform.cs](../Assets/Scripts/OwnerTransform.cs), [RangerCombat.cs](../Assets/Scripts/RangerCombat.cs). Unreal: **planned**, not ported by this inventory.

### NET-02 — Relevance and lifecycle

Interest follows players/spectators; handle hide/show/despawn/late join/rehost races. Visual culling never removes collision/state. Map transport is a separately filtered feed.

Unity evidence: [WorldInterest.cs](../Assets/Scripts/WorldInterest.cs), [TransportMap.cs](../Assets/Scripts/TransportMap.cs). Unreal: **planned**, not ported by this inventory.

### NET-03 — Budgets

Bound far AI, parked cars, harvested changes, build counts and cosmetic pools; cache terrain/styles/icons. Rendered host with clients and frame tails proves FPS, not null-renderer simulation speed. No GPU-vendor logic.

Unity evidence: [RuntimePerformance.cs](../Assets/Scripts/RuntimePerformance.cs), [PresentationBudget.cs](../Assets/Scripts/PresentationBudget.cs). Unreal: **planned**, not ported by this inventory.

### NET-04 — Scalability

Hardware-independent quality/view distance/frame targets, volume/captions/bindings. GPU was already active in Unity. Unreal Lumen/Nanite/TSR are tools, not automatic realism or FPS guarantees.

Unity evidence: [RuntimePerformance.cs](../Assets/Scripts/RuntimePerformance.cs). Unreal: **planned**, not ported by this inventory.

### NET-05 — Content acceptance

Every asset needs license, scale, rig, collisions, LOD and cost. Preserve Unity GUID/evidence. Internet, physical mic, twenty independent users, long matches and production art need separate tests.

Unity evidence: [ReserveSession.cs](../Assets/Scripts/ReserveSession.cs). Unreal: **planned**, not ported by this inventory.

## Outstanding requests

### GAP-01 — Aircraft

Supply planes are flyovers only. Pilotable planes were proposed, not implemented. Track a future decision rather than claiming migrated existing capability.

Unity evidence: [SupplyDrop.cs](../Assets/Scripts/SupplyDrop.cs). Unreal: **planned**, not ported by this inventory.

### GAP-02 — Commercial parity

Complete Fortnite edits/build prediction, Apex-quality slide animation and CS2-style contrast were inspirations, not verified parity. Require explicit behavior/visual scenes and original licensed art.

Unity evidence: [BuildPiece.cs](../Assets/Scripts/BuildPiece.cs), [RangerMovement.cs](../Assets/Scripts/RangerMovement.cs). Unreal: **planned**, not ported by this inventory.

### GAP-03 — Professional finish

Host performance regression, simple cars/animals/characters, imperfect pickup selection/cameras, limited bots, owner movement and absent relay are improvement requirements, not legacy rules to reproduce.

Unity evidence: [RoadVehicle.cs](../Assets/Scripts/RoadVehicle.cs), [Ranger.cs](../Assets/Scripts/Ranger.cs), [OwnerTransform.cs](../Assets/Scripts/OwnerTransform.cs). Unreal: **planned**, not ported by this inventory.

## Weapon table

| Weapon | Class | Magazine | Player / animal damage | Interval | Reload | Range |
| --- | --- | ---: | ---: | ---: | ---: | ---: |
| Ridgeline | Sniper | 5 | 80 / 110 | 1.15 s | 2.5 s | 650 m |
| Trail Scout | AssaultRifle | 15 | 27 / 48 | 0.14 s | 1.9 s | 350 m |
| Brushmaster | Shotgun | 6 | 12 / 19 | 0.9 s | 2.8 s | 85 m |
| Service Nine | Pistol | 12 | 23 / 30 | 0.3 s | 1.4 s | 110 m |
| Kestrel | SMG | 28 | 18 / 25 | 0.095 s | 1.8 s | 125 m |
| Longwatch | MarksmanRifle | 10 | 52 / 73 | 0.45 s | 2.4 s | 550 m |
| Sawmill | SupportGun | 40 | 27 / 40 | 0.16 s | 3.3 s | 330 m |
| Stormbreak RPG | Launcher | 1 | 110 / 220 | 1.1 s | 3.8 s | 280 m |

All eight kits, nine mods, forty bindings, sixteen animal prefabs and sixty named sites are in the exported tables. This catalog covers player-facing rules and engineering invariants. Hashed implementation remains available for deeper edge cases; legacy bugs are not desirable rules and requested features are not silently treated as completed.
