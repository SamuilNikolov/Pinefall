# Unreal 0.5 construction, presentation and performance checkpoint

October 31, 2026; Unreal **5.6.1**, native development version **0.5.0-unreal-construction**. Final UI build 35 compiled successfully. `verification.json` identifies exact source artifacts, build numbers and final source/config/script/content hashes. This is an editor-launched development checkpoint, not packaged distribution or full Unity parity.

## Functional checks

- `reserve.json`: 83 assertions, including 51 real forest/desert/snow first-piece foundation cases (four shapes/four orientations plus buried walls), actual remote driving/steering, menu continuation, fuel/service, boats, shared inventory, crouch and private map feed.
- `construction.json`: 40 assertions with three real processes and **80 ms artificial delay per sender**. Validated placement/cost/rejection, ownership/edits/door/collision, harvest/destruction/late join/reset, current build cover and support. A real owning client walks up a built ramp, has an upward walkable capsule contact and holds position after releasing input. This caught reversed procedural triangle winding; it does not certify every edit pattern on all terrain.
- `inventory.json`: 18 real UI-handler/focus/hotbar/revision checks. Alt hold and Tab fallback, debug F1–F4 removal, safe slot reordering, drops/equip/capacity. No physical OS keyboard automation claim.
- `combat.json`: 16 real-player aiming/body/head/current-cover checks, **80 ms delay per sender**, preserving bounded player rewind.
- `movement.json`: 24 delayed slide/hop/ceiling/stance/menu/reset checks.
- `wildlife.json`: 24 behavior/attack/storm/alpha/reward/authored-hit/miss assertions, plus three remote skeletal species. The on-demand geometry tests cover deer/wolf/fox; the other species' production-art/playtest gate remains open.
- `world.json`: 15 physics/survival checks. `round.json`: 36 assertions across three processes. `smoke.json`: five join/remote scoped-fire checks.

Core simulation regressions ran on build 33. Builds 34–35 only refine UI text/backplates, inventory alignment/ammunition readability and inventory minimap visibility; inventory was rerun on 35. The 83-check reserve suite was rerun successfully on final build 35.

## Actual graphics and performance

Same i5-13450HX / RTX 4060 Laptop GPU, D3D12, 1600×900, quality level 2, 87% screen percentage, Nanite enabled, density/view-distance scales 0.8, uncapped, no audio. Both current measurements wait for the full **84-animal** reserve population, assets/shaders and peer readiness before sampling.

| Run | Average FPS | p95 frame | p99 frame | Scope |
| --- | ---: | ---: | ---: | --- |
| `render-solo.json` | 51.5 | 26.5 ms | 30.6 ms | One rendered listen host |
| `render-host-two-peers.json` | 47.8 | 27.7 ms | 32.2 ms | Rendered host plus two real local NullRHI clients capped at 30 FPS |

The latter records three players throughout, 84 animals, 6.6 ms average game-thread time and 16.8 ms average GPU time. Available physical memory reached about 0.32 GB on the 16 GB machine; these are local multi-process editor measurements, not a universal hardware recommendation.

The early failed candidate (`render-early-failed-candidate.json`, 6.9 FPS) lacked a settled-population gate. Do not claim an apples-to-apples percentage gain from it. CPU tracing located substantial end-physics waiting around continually updated animated bone bodies. `PFAnimalHitQueries` now evaluates the same authored primitives against current bones on demand; movement capsules, animations, current cover and player history remain intact. Unsupported primitive types retain ordinary query collision. A command-list batching experiment did not justify changing engine defaults. Visual scenery was retained.

`forest-host.png` is the actual build-33 measured scene with the top-left local arrow. `inventory-720p.png` is the final build-35 inventory: readable occupied ammo, aligned cards/drop buttons, all twelve slots, local minimap and visible world. `gameplay-720p.png` and `build-720p.png` show the final build-35 gameplay/mode HUD. Later inventory/mode text refinements do not relabel the performance sample. Test windows were visible; black hidden-window captures were rejected.

## Remaining gates

The main scene and 720p inventory were visually reviewed. The atlas is a 4K layout bake, not an orthographic world render. Temporary stylized animal silhouettes remain despite new coat detail; character/weapon-hand production art, final sound mix, full biome/vehicle/water/cave polish and broad combat/build-edit playtesting remain open. Fine grass fades at 55/60 m; Nanite scenery uses detail reduction rather than a proven hard HISM end-cull cutoff. Collision remains independent of visibility.

No whole-match frame-time guarantee, twenty-human load test, broad WAN/loss/jitter test, microphone/voice certification, production asset completeness or full Unity feature parity is claimed. All automated checks use `-nosound` and do not record microphones. Earlier checkpoint evidence is unchanged.
