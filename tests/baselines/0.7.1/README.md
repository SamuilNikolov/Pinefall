# 0.7.1 historical baseline

Recorded October 25, 2026, before the source repository handoff. These are **historical player-build results**, not evidence that any later branch has been built or tested.

| Folder | Evidence |
| --- | --- |
| `gameplay/` | Ten reports, 146 passing assertions, zero failures/captured runtime errors |
| `compatibility/` | Three reports, 32 passing assertions, 0.7.1 host and 0.7 ally/enemy clients |
| `performance/` | Solo and three-player graphics-enabled host route, real CPU/GPU/resolution, median/p95/p99 frame times |
| `build-manifest.json` | Original packaged-player/assembly hashes and release counts |

The performance samples used 1600×900 on an i5-13450HX / RTX 4060 Laptop machine with the original user's older game still open. Settled host samples with the HUD averaged 89–99 FPS; the HUD-off comparison is reported separately. The route samples ten seconds after two seconds settling in each view at full detail/uncapped. See [release methodology](../../../PERFORMANCE.md) and [verification limits](../../../VERIFICATION.md).

Audio networking checks use synthetic packets, not physical recording. Public-internet connections, long eight-human games and other hardware were not verified. Existing older binaries and external diagnostic executables are deliberately absent from the repository. New check commands are documented in [TESTING](../../../docs/TESTING.md).
