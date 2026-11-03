# Native control-feel checkpoint, November 2, 2026

Unreal 5.6.1 Development Editor runtime. This is an incremental playability pass,
not full Unity parity or a production release.

## Changes

- Saved 75-110 degree unscoped FOV, independent 0.25-2x ADS mouse multiplier,
  75-200% crosshair sizing and white/cyan/lime/pink crosshairs. Weapon zoom and
  server accuracy are unchanged; invalid FOV settings are bounded at use.
- Confirmed damage text updates on the next widget tick rather than waiting for
  the 10 Hz inventory refresh. Unchanged confirmation text is cached; markers
  remain server-confirmed. Menus/map/inventory suppress center confirmation text.
- Hidden roster, legacy team/banner and gun-detail formatting is skipped during
  play. The native squad HUD remains. Added `Pinefall_UIRefresh` trace scope.
- New installs use Balanced; existing saved quality preferences are retained.
  No new textures, scenery reductions or simulation/network protocol changes.

## Functional evidence

- Native compilation passed (`artifacts/feel-release-build.log`, local).
- `ui-checks.json`: 30 real UI-handler/settings/transaction checks, including
  persistence, ADS transition/hip-look separation, FOV bounds, color/size,
  inventory focus and stale-click behavior. Not physical keyboard automation.
- `combat-checks.json`: 16 two-process checks with 80 ms per-sender artificial
  lag, including confirmed remote damage, current cover, headshot/elimination,
  ammo rejection and upward recoil. Not public WAN/loss/jitter certification.

Real rendered 1280x720 captures were reviewed: `settings-controls-720.png` shows the lower scrollable settings controls; `hud-720.png` shows the unchanged equipment/build/material hierarchy. Scroll is required for the full controls guide; no main-menu screenshots containing private addresses are included.

## Performance evidence and limits

`high-render.json` and `lighter-gi-render.json` retain a same-process static forest
comparison: graphics-enabled host, two real local headless clients capped at
30 FPS, 84 animals, 1600x900, 87% screen scale, High console quality settings,
UE CPU/GPU tracing active, i5-13450HX / RTX 4060 Laptop, battery reported.
The saved overall-quality field still says 3 because this experiment applies
console overrides without changing the user's saved Epic profile.

High-lighting sample: 24.9 FPS, p95 88.9 ms, p99 179.3 ms.
Lighter indirect lighting sample: 74.1 FPS, p95 16.5 ms, p99 19.4 ms;
GPU average 7.33 ms. No foliage, population, resolution or collision reduction.
Available physical memory was severely constrained (minimum 0.28/0.05 GB),
so do not attribute the entire difference to lighting or claim a reliable 3x
gain. The comparison changed `sg.GlobalIlluminationQuality` 2 to 1;
**reflections remained 2** in the measured report. The old experiment executor
only applied the first comma-separated console command. This is not an actual
Balanced-preset application; use the existing `-CompareLighting` test for that.
The local trace is retained under the run directory, not committed.

An earlier current-settings run at Epic/100% measured 26.5 FPS and poor tails.
It is not comparable to historical High/87% measurements. Neither run establishes
whole-match, moving-camera, twenty-player, packaged-build or all-hardware FPS.
The UI cleanup itself has no isolated FPS-gain claim. The measured binary includes
the camera/crosshair controls and hidden-panel cleanup; the later immediate-hit
text/default-only changes are separately compiled/gameplay-checked.

Local sources: `artifacts/unreal-render/20261102-191358-Forest`,
`artifacts/unreal-inventory/20261102-191903`,
`artifacts/unreal-combat/20261102-191929`. Keep private addresses/logs and raw traces
outside commits. See the handoff for remaining migration work.
