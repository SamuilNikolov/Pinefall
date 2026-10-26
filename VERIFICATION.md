# Wildfront 0.7.1 verification

Checked October 25, 2026 with the Windows player, built using Unity 2022.3.13f1. The full gameplay suite passes **146 assertions**, with **zero failures and zero captured runtime errors**. A separate three-player test also passes **32 compatibility assertions** using a 0.7.1 host and existing 0.7 clients.

| Run | Passing assertions |
| --- | ---: |
| host | 69 |
| client-a | 3 |
| client-b | 3 |
| late-join | 2 |
| practice-1 | 6 |
| practice-4 | 7 |
| polish | 24 |
| living-host | 24 |
| living-ally | 4 |
| living-enemy | 4 |

Coverage includes lobby/readiness, draft and free landings, armor/hunger, aiming/shooting, all attachments, inventory/drop limits, vehicle driving/steering/repairs, motorcycles, boats, wildlife behavior/rewards, sharks, supply claims, live vehicle-map movement/privacy, survey visibility/expiry, storm progression, team downing/revives, victory, restart, late joining and Solo/Squad practice. A same-tick show/despawn case verifies network spawn-queue cleanup. Three session returns/rehosts retain terrain and atlas; the longest measured return was 0.397 seconds on this machine.

Graphics-enabled performance tests measured both solo and a host with two connected clients, with HUD-on/off comparisons and several environments. See PERFORMANCE.md and PERFORMANCE-RESULTS.json for timings, hardware, resolution, procedure and limits. No headless FPS figure is used as evidence of rendering speed. Native captures were inspected for the HUD, map, sound controls and the new performance page. The final build only adds the settings screenshot step and changes the editor's default output folder; the tested gameplay/performance implementation is unchanged.

No automated run records microphone input. Voice networking tests use synthetic samples. Public-internet latency/router reachability, other GPUs, lower-end systems and long human matches remain unverified. Automatic detail is based on frame timing and contains no GPU-vendor tuning rule. Some character/animal art and animation remain developmental.

All reports and assembly hashes are included under verification/. Earlier game copies are separate and were not overwritten.
