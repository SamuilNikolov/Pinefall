# Repository import verification — October 25, 2026

Fresh checkout import and build using Unity 2022.3.13f1, without the old project's Library. The portable build wrapper produced a Windows x64 player. The portable full gameplay wrapper then passed 146 assertions across ten reports, with zero failures/captured runtime errors.

`gameplay/` preserves those reports, distinct from the historical 0.7.1 release. `build-manifest.json` records the tested player hashes and parameters. Logs, full player binaries and generated editor caches are not committed. See [repository verification](../../../docs/REPOSITORY-VERIFICATION.md) for the final scope and limitations.

`performance/host-two-clients.json` records the new six-scene rendered wrapper check, including its actual hardware, frame-time tails, zero microphone starts and no captured runtime errors. This run was not a controlled performance comparison with historical builds.
