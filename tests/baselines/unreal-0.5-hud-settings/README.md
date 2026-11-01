# Reference HUD and settings verification — November 1, 2026

UI-only follow-up to native 0.5. Unreal 5.6.1 Development Editor compilation passed (`artifacts/hud-settings-verified-build.log`). No environment textures, simulation rules or network schemas were changed.

## Reviewed rendered views

- [720p equipment/material/build tiers](hud-720.png)
- [1080p equipment/material/build tiers](hud-1080.png)
- [720p building context](build-720.png)
- [720p dedicated Settings page](settings-720.png)

The supplied reference guided structure, spacing and hierarchy. All seven existing equipment bindings and four build bindings remain. Material cards use original native vector icons. Weapon and item textures preserve aspect ratio; equipped ammo is shown in its slot instead of a separate panel. The quickbar is no longer gated on having a weapon name. Settings is directly below the main Enter the world action; the same entry is available through Escape in-game. Main-menu captures were reviewed locally but are not committed because they contain local network addresses.

## Functional checks

[24 UI/transaction checks passed](ui-checks.json), covering existing Alt/Tab inventory access and transactions plus the dedicated settings page, visible cursor, unpaused world, frame-limit selection, Escape-to-menu navigation, persistence of changed volume/mouse preferences after reload, and restoration of game input. Test changes to preferences were restored afterward. These are real widget/controller handlers, not physical keyboard/mouse automation.

Final settings buttons were checked without automatic wrapping at 720p. Frame-cap/graphics callbacks apply non-resolution preferences, so they preserve the current window size. Closing or backing out saves preferences. Controls currently supports mouse sensitivity and a binding reference; full key rebinding/voice settings remain migration gates.

Captures are graphics-enabled single-player presentation fixtures with sound disabled and explicit benchmark immunity. They are not a new FPS, audio, WAN or multiplayer stress benchmark. Previous rendering/performance evidence keeps its original scope in the playability baseline.
