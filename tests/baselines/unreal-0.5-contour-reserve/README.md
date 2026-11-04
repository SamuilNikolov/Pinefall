# Contour reserve verification, November 3, 2026

Unreal 5.6.1 Development Editor build. The regenerated reserve and 4K atlas retain eight villages and 64 enterable houses. Eleven routes follow the terrain for 8.97 km. The layout spans 173 m vertically and the largest road centre adjustment is 69 cm.

The build, 42 geography checks, 300 gameplay checks, and host/join smoke test passed. The reserve probe swept a standing player capsule through every entrance and checked blocking rear walls across all three house styles. It also covered truck steering and coasting, boat travel, and first-piece construction on real terrain. The construction run passed 40 checks with 80 ms of artificial packet delay on the host and initial client. The first run timed out during late join. The successful run used a 360-second deadline. The battle suite passed 63 assertions with three processes.

The rendered village and lake views are included here. The forest benchmark ran at 1600×900 and 87% internal resolution on an RTX 4060 Laptop GPU with the laptop on battery. With two local headless clients and all 84 animals, the host averaged 26.4 FPS. The 95th-percentile frame took 59.2 ms and the 99th took 112.0 ms. GPU time averaged 10.9 ms and minimum available system memory was 0.21 GB. The timing points to waits outside GPU execution. The run captures a static scene with all three processes sharing the same computer.

See [verification.json](verification.json) for the report inventory. Rebuild the authored map with `tools/Setup-Unreal.ps1 -RebuildReserve` and verify it with `tools/Test-UnrealReserve.ps1`.

A separate host-only run at the same requested resolution and quality averaged 25.2 FPS, with p95 at 104.6 ms and p99 at 197.7 ms. These runs were taken at different times and do not isolate the cost of local clients or the map update. Host frame pacing remains open.
