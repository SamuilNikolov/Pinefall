# 0.9.0 compile and verification status

Unity 2022.3.13f1 successfully built the Windows x64 / Mono player. The final build log reports PINEFALL_BUILD Succeeded, with no shader compilation errors. Protocol 9 requires the same build for every peer.

This sweep is a **compiled playable development candidate**, not a fully green final-build verification. The user requested compile and push rather than another full sweep.

## Completed checks and their limits

| Evidence | Result | Scope |
| --- | --- | --- |
| Earlier full gameplay | 146 passes / 0 failures / 0 runtime errors | Before final movement/render changes |
| Fieldcraft multiplayer | 75 passes / 0 failures / 0 runtime errors | Movement candidate, before last rendering-only changes |
| Frontier integration | 51 passes / 0 failures / 0 runtime errors | Bunny-hop physics/gain, fast crouch slides, emotes, caves, launchpads, rotated building, arena/capacity |
| Later full gameplay candidate | 145 passes / 1 failure / 0 runtime errors | Hunger assertion fixture corrected afterward; correction compiled but not rerun |
| UI layout | Complete prior sequences passed at 1600×900 and 1280×720 | Latest render-tuned visual sequence did not reach its completion marker |
| Final graphics route | Six rendered samples, no captured runtime errors | RTX 4060 Laptop GPU, i5-13450HX, 1600×900, two local headless clients |

The hunger fixture originally waited a frame after eating before asserting the exact hunger total, allowing periodic decay to cross the threshold. It now asserts the host transaction before yielding. This change needs a confirming rerun.

## Performance still needs work

The latest full-detail route averaged 72.1 FPS in the meadow HUD, 73.6 in the named village, 74.4 in autumn woods, 55.6 in rainforest and 74.0 at the waterfront. The meadow without HUD averaged 93.1 FPS. These are lower than the historical 0.8 route on this machine. Distant shading and village detail were reduced, but this does **not** establish the requested performance goal as complete. Automatic detail remains available; this report is not a hardware-wide promise.

## Other remaining limits

Twenty authoritative participants and approval-slot reservation are checked, not twenty independent internet clients. Automated voice uses synthetic data and does not start a physical microphone. Public-internet device listening, long human matches, saturated construction and lower-end hardware remain unverified. Character/world art still mixes scans with stylized/procedural assets, and complex commercial-game edit-pattern parity is not claimed.

Raw reports and final runtime hashes are in [candidate evidence](../tests/baselines/0.9.0/README.md). Older baselines remain intact.
