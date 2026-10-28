# Native hit registration and latency fairness

October 28, 2026. Unreal 5.6.1, native 0.2.1. Matching builds required: the fire RPC and replicated character fields changed. Unity remains frozen.

## Why current-position traces were unfair

A remote player aims at a delayed, visually smoothed opponent. By the time the request reaches authority, that opponent has moved. Checking only the latest server collision rejects shots that visibly connected. Camera socket rotation also moved the local eye away from the server eye. Both problems affected the previous implementation.

Riot describes server-authoritative validation against the shooter's historical view, bounded rewind, buffering and the remaining latency tradeoffs in [Peeking into VALORANT's Netcode](https://www.riotgames.com/en/news/peeking-valorants-netcode). Its [gameplay consistency update](https://playvalorant.com/en-us/news/game-updates/valorant-gameplay-consistency-update/) also discusses jitter and divergent views. These are design references, not claims that Pinefall implements Riot's stack or performance.

## Implemented pipeline

1. Local input immediately produces recoil, sound and muzzle presentation. Damage and hit/kill confirmation wait for authority.
2. A local trace determines the aim endpoint. When it sees a player, the request uses that player's replicated movement timestamp adjusted by the actual visual smoothing displacement projected along velocity. The listen host uses its displayed target time too. Other shots use a measured server-clock estimate with a bounded smoothing allowance.
3. Authority validates the shooter, equipment, ammunition, reload state, increasing sequence, cadence, finite time/aim, range and plausible direction. Requests older than 350 ms or over 75 ms into the future are rejected. Accepted future tolerance is clamped to now. The timestamp is a bounded client hint, not a trusted damage result or full anti-cheat system.
4. Each living player's actual skeletal physics spheres, capsules and boxes are sampled after animation/physics at 30 Hz into sixteen server-only frames. Historical transforms interpolate between samples. Teleports clear history. Unsupported convex/tapered collision falls back to current traces rather than using a larger substitute hitbox.
5. The server first traces current world cover and other non-rewound actors. Historical player primitives can win only if closer than that obstruction. It never teleports live bodies to perform rewind. Closest teammates still obstruct a shot even when friendly damage is disallowed.
6. Damage, armor, head multiplier, ammunition and elimination remain server-owned. Confirmed feedback goes only to the shooter. History is never broadcast and adds no enemy map tracking.

The head has a separately fitted sphere; the neck is narrow. First-person camera height and server shot origin share standing/crouched eye offsets. Listen-server cosmetic mesh displacement is removed from recorded geometry to avoid compensating the same delay twice.

## Measured failure and correction

A fixed 100 ms smoothing subtraction rewound a moving target roughly 50 cm too far at the fixture's speed. Using the actual replicated movement time and visible offset fixed that case without inflating collision. This is still an estimate when velocity changes sharply: reversals, jumps, low update rates and long stalls need broader testing.

## Verification and limits

`tools/Test-UnrealCombat.ps1 -PacketLagMs 80` launches separate host/client processes and uses ordinary owner input against actual player collision. Artificial lag is applied per sender; it is not a measured real-world ping. Fixtures arrange terrain positions, ammunition, health and cover. Cases cover standing/crouching, scoped first/third person, both shooting roles, moving targets, current cover and a lethal headshot. Read the saved report for the exact checks that passed.

This does not prove twenty-player load, WAN behavior, jitter/loss resilience, rendered FPS or perfect host neutrality. Animals currently use present collision. Current cover takes priority over historical visibility, deliberately favoring protection from new cover. Requests arriving after the shooter is eliminated are rejected; simultaneous lethal trades are not compensated. A dedicated server would remove the host player's zero-network path, but that deployment is not implemented here. Future work should test both directions across RTT/jitter/loss profiles, abrupt movement and lethal exchanges before changing rewind limits.
