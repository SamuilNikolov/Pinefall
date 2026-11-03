# Optional agent skills for native development

The project builds without agent skills. The source, pinned Unreal 5.6.1 installation,
repository instructions and retained test evidence remain authoritative.

Two community skills were reviewed and installed locally for the November 2 playability
pass from [kevinpbuckley/unreal-engine-skills](https://github.com/kevinpbuckley/unreal-engine-skills):

- `ue-profiling-and-optimization`: measure CPU/render/GPU frame times, capture Insights
  traces, identify expensive work, change one variable and measure again.
- `ue-character-and-movement`: CharacterMovement prediction, input, crouch and movement
  presentation. Preserve our custom saved moves, replay and intentional hopping momentum.

These skills describe UE 5.8. Check every relevant API against the installed **5.6.1**
headers and compile/tests; do not upgrade the engine, adopt experimental Mover, replace
our replication or trust example line numbers as part of installing a skill.
The existing local `frontend-design` skill remains useful for deliberate HUD hierarchy.

With Codex's skill-installer available, install the two optional folders using:

```powershell
python "$env:USERPROFILE/.codex/skills/.system/skill-installer/scripts/install-skill-from-github.py" --repo kevinpbuckley/unreal-engine-skills --ref master --path skills/core/ue-profiling-and-optimization skills/core/ue-character-and-movement
```

Review the upstream instructions before installation; upstream `master` can change.
Skills install outside the checkout and become automatically discoverable on the next
turn. They do not grant permissions, prove performance, supply assets or complete Unity
feature parity. Do not copy private machine paths, traces or local addresses into commits.

For actual work, start with `AGENTS.md`, `docs/HANDOFF.md`, `Design/UNREAL-STATUS.md`
and the relevant native source. Use `tools/Test-UnrealRender.ps1` with real graphics and
peers, separately from headless gameplay checks. Record settings, population, resolution,
frame-time tails and limitations; do not compare unlike quality profiles as a speedup.
