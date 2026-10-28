# Native Unreal 0.2 verification checkpoint

October 28, 2026. Development Editor, Unreal 5.6.1. Source fingerprints and exact limits are in verification.json. This is a small migration arena, not Unity feature parity.

- scoped-fire.json: five checks in a real host/client range, including scoped firing, server ammunition decrement and remote confirmed damage.
- round.json: 36 checks across three real processes. Owning clients select explicit teams and issue normal lobby/draft requests; authority advances timers and arranges damage/inventory fixtures for deterministic rule checks.
- wildlife.json: fifteen arranged authority checks and a separate wrapper check for three replicated skeletal species on the remote client. wildlife-import.json lists seven mesh/skeleton imports and 89 clips.
- scope.png, inventory.png and armor.png: actual rendered 1600x900 player captures. The armor screenshot uses injected presentation-only cues; it is not hit registration evidence. Scope HUD and inventory wrapping were inspected after fixes. The round lobby was also inspected at 1280x720 without clipping.

The round and wildlife regressions ran on build 19; build 20 changes only the scoped-fire probe and shot diagnostic. Build 20 compiled and passed the scoped-fire regression. Combat audio forced reimport completed without decoder ensures. Audible quality remains a human listening gate. No frame-rate, moving-target latency fairness, WAN, physical microphone or twenty-player claim is made. Historical 0.1 evidence is unchanged.
