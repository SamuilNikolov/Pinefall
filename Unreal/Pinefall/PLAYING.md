# Playing the native Unreal build

Unreal 5.6.1, native version **0.2.1**. Matching Unreal clients are required. This is a development build launched through the installed editor, not a packaged replacement for the Unity release.

## Start and join

Run `PLAY-UNREAL.cmd` at the repository root. Choose a solo, duo or squad host from the menu. The practice range remains available separately. For command-line launching use `tools/Run-Unreal.ps1 -Mode Solo`, `Duo`, `Squad`, `Practice`, `Host` (range) or `Join -Address 'address:7777'`.

The host starts a listen server on UDP 7777. Guests enter the host's reachable address and port. The menu lists local addresses; internet access still depends on the host's router/firewall or VPN. There is no relay, discovery service or automatic public-connectivity test. Unity clients cannot join this build.

## Round flow

1. Choose a team and ready up. The host can change solo/duo/squad while in camp. Everyone must be ready before the host starts the five-second countdown. Withdrawing readiness cancels it.
2. Spend ten points between landing priority and a starting kit. The two native kits currently enabled are Bare essentials (zero points: pistol and one spare magazine) and Ridgeline marksman (seven points: rifle, pistol, three rifle magazines and one medkit). Everyone receives one food item. Other Unity kits have not been migrated.
3. The highest bid chooses a landing first. Each player has eighteen seconds. Click a safe map point to preview; on your turn it locks the choice. Everyone sees the draft choices, and teammates may land separately. Opposing landing claims require sixty meters of separation. A timeout chooses a valid landing automatically.
4. Scavenge supplies, hunt and survive the storm. This first native arena uses Cedar Crossing's 480-meter playable bounds, not the full Unity reserve or lobby-sized crops. The storm uses seven wait/close cycles with the source timings and radii scaled to this arena. Waiting periods are 90/60/55/45/40/30/20 seconds; closing periods are 100/140/125/110/85/60/45 seconds. Standing outside the current circle causes health damage that bypasses armor. Closing phases enrage animals.
5. The last surviving team wins. A one-person session remains a survival session until death. Dead players do not respawn during a round. The host can return everyone to a fresh, unready camp after results.

Late arrivals spectate an ongoing round. In duos/squads, eliminated players spectate living teammates while their team survives, then any survivor after a team wipe. In solos, they may spectate any survivor. Practice range deaths still respawn after six seconds.

## Controls

| Action | Control |
| --- | --- |
| Move / look | WASD / mouse |
| Sprint / crouch / jump | Hold Shift / hold Ctrl / Space |
| Fire / aim | Left mouse / hold right mouse; aiming and firing work together |
| Reload / rifle / pistol | R / 1 / 2 |
| First or third person | V |
| Inventory | Hold left Alt; release to close |
| Equip, use or share inventory | Click a slot to equip/use; Drop shares one, Shift + Drop shares the stack |
| Pick up / revive | E / hold E beside a downed teammate |
| Map / close a panel | M / Escape |
| Next spectator target | ] |

In water, WASD swims toward your view, Space rises and Ctrl dives. Air lasts about twenty-five seconds while your head is submerged; an empty air supply causes eight health damage per second and bypasses armor. Returning to the surface restores air. These survival ticks run during battle rounds.

The menu blocks local combat input but does not pause physics, the storm or the network. Sensitivity, volume and graphics quality are available. Sliding, bunny hopping, pickaxe/build controls and rebinding are still pending in Unreal.

## Equipment and survival

Only the Ridgeline rifle and Service Nine pistol are functional. Both use server-validated cadence, magazine state and traces against skeletal collision. Player shots use a measured server clock and at most 350 ms of server-only skeletal hit history, with current solid cover always blocking the shot. This is bounded compensation, not a promise that arbitrary latency/loss is fair. The player is a clothed MakeHuman-derived ranger rather than the Epic mannequin, with separate rifle/pistol holds, reload gestures, crouch and movement clips. Third-person guns follow the animated wrist with a grip offset. First-person arms and fully articulated magazine/bolt animations remain unfinished. The pistol scan is assembled with one seated magazine.

Aiming has a smooth zoom and reduced sensitivity; the Ridgeline has a circular optic. Recoil moves the view weapon and settles after a shot. Confirmation markers, damage amounts and sounds follow accepted damage, not merely a trigger pull. White marks indicate ordinary hits, blue armor hits and amber eliminations. Incoming damage has a short edge pulse and direction cue; environmental storm damage has no attacker direction. Brief target health bars appear only after a confirmed hit and are hidden by cover.

Round ammunition is finite. Reloading consumes only the missing rounds; dropped guns retain their magazine. The range has unlimited reserve reloads. Twelve inventory slots hold guns, ammunition, food, medicine, plates and vests. Total pool limits are 400 rounds per supported weapon, eight food, five medkits and six plates. Drop/pickup transactions are server controlled, with stale-click rejection.

Food restores 35 hunger and five health; medkits restore 45 health. Hunger drains faster above 5 m/s. Empty hunger causes three health damage every three seconds and prevents running; sprint requires hunger above twenty and available stamina. Light/field/heavy vests absorb 25/35/50 percent of ordinary incoming damage, limited by their 50/75/100 durability. Plates restore thirty durability to an equipped vest. Hunger and storm damage bypass armor. Equipping a vest does not flash a damage indicator.

A team member can be downed twice while another teammate is able to revive. Downed players have thirty seconds before bleeding out. Hold E within 3.2 meters with a clear path for three seconds; movement out of range, damage, release or invalid state interrupts. A successful revive restores 35 health and at least 25 hunger. Only one reviver can advance a target at a time.

## Wildlife: first native pass

Deer, stag, fox, wolf, bull, horse and alpaca use imported skeletal movement/attack/death clips. Normal prey flees nearby players; wolves pursue and attack with a damage windup and a fresh distance/cover check. Server-controlled populations grow toward twenty-eight living animals with randomized placement and replenishment. Distant animals retain collision but stop expensive movement work; the director caches players and budgets thinking.

Closing storm phases turn prey aggressive and add a 5.5 percent mutation roll per animal per closing stage. Alphas have 1.6 times the size, matching larger collision and 2.5 times health. Enraged animals have a restrained warm overlay. Settled storm phases restore normal aggression rules; an alpha stays large. Animal calls are positional and intermittent.

Kills drop food and may drop ordinary usable equipment. Extra-drop odds are 35 percent normally and 80 percent while enraged; alphas award two equipment rolls plus three food. The current pool contains the two supported weapons, rifle ammunition, medicine, plates and vests. There are no pelts, bones or trophies. Attachments and launchpads will join the pool when their actual gameplay systems are migrated. The imported animal art is temporary low-poly scaffolding, not a completed realism pass.

## Still missing

The larger biome reserve, dense named villages, branching caves, usable cars/boats, sharks, the other six gun families, attachments, construction/editing, launchpads, slide/bunnyhop, customization/emotes, weather/day cycle, narration and opt-in proximity voice remain migration work. Cedar has scanned forest-floor/rock surfaces, a more varied ridge, denser planted trees, fitted trunk collision and rock-surface collision. Its elliptical lake has ripples, depth-faded shores and native swimming physics. This remains a small generated test arena, not the full reserve.

The atlas currently shows Cedar Crossing, storm circles, public landing choices and relevant teammates; it never draws ordinary enemy positions. Rarity metadata exists but its full combat/modifier presentation is unfinished.

See `Design/UNREAL-STATUS.md` for exact evidence and unverified areas. Compilation and headless tests do not establish visual quality, frame rate, moving-player hit fairness or internet reliability.
