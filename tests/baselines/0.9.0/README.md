# 0.9.0 compiled-candidate evidence

This is **partial verification**, not a uniformly green final-build baseline. See build-info.json for exact scope and the final compiled runtime hash.

- earlier-gameplay: 146 passing assertions across ten reports, before the final movement/render edits.
- fieldcraft: 75 passing assertions across host/ally/enemy/late join, on the movement candidate before final render tuning.
- frontier: 51 passing assertions, including actual repeated bunny hops, speed gain, fast-only slide entry, emotes, caves, launchpads, building frames and capacity checks.
- candidate-gameplay: 145 passes and one failed hunger assertion. The test fixture was corrected to check before periodic decay, compiled, and not rerun.
- performance-host.json: actual final render-tuned host, RTX 4060 Laptop GPU, 1600×900, with two local headless clients. Roughly 56–74 FPS with HUD across the route; lower than the historical 0.8 result. Headless FPS is not graphics evidence.

Complete UI captures before the last rendering-only tuning passed font/layout audit at 1600×900 and 1280×720. The latest visual sequence ended without its completion marker. Physical microphones and a twenty-human internet session were not tested. Historical 0.8 and 0.7.1 evidence is unchanged.
