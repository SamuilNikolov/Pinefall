# Native supply objectives, November 2, 2026

Unreal 5.6.1 Development Editor runtime, incremental Unity migration. No packaged production release/full parity claim.

## Implementation

Supply scheduling matches the legacy first-delay/spacing/cap/small-circle rules: 65 seconds, 140 seconds, six per round, no new supply when next radius is below 200 m. Sampling is bounded to sixteen attempts and reuses actual dry, moderate-slope Landscape ground validation. Public markers are replicated world objectives, never player positions.

A replicated timestamp drives the 26-second cosmetic descent independently on each machine. Visual ticking stops at landing, at most six actors are globally relevant, and an existing one-second authoritative timer schedules objectives. No new per-frame scene scans, dynamic lights or textures. Crate uses existing service-case art and an engine sphere canopy; these are interim visuals. Gold markers and six-second HUD notices cover inbound, landed and claimed events.

Opening uses the existing owned-character interaction RPC, with authoritative Active/health/vehicle/landing/range/cover/duplicate validation. Lock before reward generation; release six shared world pickups, not direct inventory grants. Weapon is epic/legendary and ammunition matches it; other rewards are heavy armor, plates, medicine and food. Full inventory does not block opening. Claimed invisible crates stop intercepting pickup targeting. Pin/timer cleanup is explicit on claim, expiry and round reset.

Compatibility: replicated MatchState fields and a new native actor class require matching builds. Item IDs and inventory schemas are unchanged. No older-binary compatibility claim.

## Evidence

`round-checks.json`: 47 checks across host/ally/enemy processes. Existing readiness, bidding, landing, storm, hunger, ammo, inventory, revival, spectating, victory and lobby-return checks remain. New checks cover scheduled supply creation, initial delay, valid safe location, airborne rejection, remote-client opening, six rewards, duplicate rejection, marker removal and reset. The wrapper additionally requires an owning-client log showing receipt of the public supply marker. Timing and positions are arranged for determinism; this is not a natural-duration match or WAN/loss test.

`landed-crate-720.png`: actual rendered reserve at 1280x720, reviewed for crate, beacon and surrounding HUD. Explicit presentation fixture, not gameplay evidence. No microphone or audio capture. No new frame-rate certification.

Native compilation passed (`artifacts/supply-final-verified-build.log`). The final three-process round rerun includes the claimed-crate focus fix. Broader fresh regression results are recorded in `regressions.json`: host/join smoke, 30 inventory/settings, 16 combat, 15 world, 24 wildlife, 86 reserve/vehicle, 24 movement and 40 construction checks all passed. Construction uses three processes and 80 ms artificial packet lag; other broad regressions use their default local network settings. The broad sweep preceded the final crate-only focus fix; its final round rerun passed 47 checks. Raw logs/builds/private addresses stay in ignored local artifacts.

## Still missing from Unity supplies

Aircraft flyover, recorded narrator calls, motorcycle/survey special rewards, survey activation/privacy routing and contestable relay interactions are not implemented by this pass. The native arsenal still contains only its two working guns. Do not mark the entire Supplies catalog or Unity migration complete.
