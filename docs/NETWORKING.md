# Multiplayer, authority and reachability

## Current topology

Pinefall uses a listen server over **UDP**, via Unity Transport and NGO 1.8.1. One player's process is both server and local client. The default game port is **7777** and can be changed in the host/join UI. The host listens on `0.0.0.0`; joining currently accepts IPv4 addresses. Eight slots include practice bots, with a bot removable when a human joins a full practice session.

`ReserveSession` configures a 20 Hz network tick, explicit network prefab registration and connection approval using `PinefallReserve/8`. NGO scene management is disabled; all players load the same committed static reserve. Do not assume remote scene loading will repair mismatched terrain/assets.

LAN discovery uses UDP **47776** with a versioned query and a host response. This is separate from the game port and is not an internet server browser. The public address lookup on the host card is informational. There is no relay, NAT traversal guarantee, encrypted voice service, persistent lobby backend or account authentication in this build.

## Authority and data flow

| Data | Existing authority / path |
| --- | --- |
| Human movement and camera | Owner input and owner-authoritative transform; server checks actions, but movement is not hardened anti-cheat |
| Health, damage, inventory, food/armor, weapon actions | Server state mutations through gameplay methods/RPCs |
| Vehicle motion/occupancy/fuel and wildlife | Host simulation with replicated state/poses |
| Match phases, storm, draft, supplies, weather | Server time and replicated match/manager state |
| Builds and harvested nodes | Host validates placement/cost/edits; networked pieces and sparse reliable resource changes with late-join snapshots |
| Spectator target | Host validates team restrictions; selected target supplies camera and interest viewpoint |
| Player appearance and choices | Client requests validated and applied to replicated state |
| Voice | Opt-in local capture, unreliable custom messages through the host, alive/proximity filtering and positional playback |
| Vehicle map pins | Server-generated targeted team feed once a second; independent of visual range |
| Survey | Consumable-triggered, team-targeted position snapshots for 12 seconds; pulses every four seconds and global warning |

Validate sender ownership/role, current phase, range, state, indices and quantity on new RPCs. Repeated use, disconnect during a transaction and object despawn are routine cases. An `IsServer` guard alone does not validate an untrusted request. This development networking has not received a competitive security audit.

## Visibility and privacy

`WorldInterest` narrows remote actor observers within roughly 1,100 m of the player or eligible teammate being spectated. It caches client viewpoints and budgets actor checks/visibility changes across frames. The host remains an observer for simulation. Call the appropriate register/forget hooks for actors; pending NGO `NetworkShow` work must be canceled before same-tick despawn.

Rendering has a different budget: `PresentationBudget` can hide/defer models at shorter ranges, extending appropriate ranges while aiming. It must never control item ownership, authoritative collision or full-map transport state.

Ordinary player maps show teammates, not enemies. The vehicle feed excludes a vehicle if an enemy occupies any seat. A team-only timed survey is the explicit reveal exception; do not broadcast all positions and merely hide their icons locally as a new implementation shortcut.

## Local and internet joining

1. Host a session and keep that game running. Use the displayed game port.
2. On the same computer, use `127.0.0.1`. On the same LAN, use the host's LAN IPv4 address or discovery.
3. From another location, use the host's public IPv4 address. The router must forward the chosen **UDP** port to the host computer's current LAN address, and the host's firewall must allow that actual executable.
4. If the host address changes, a stale router rule can point to another device. A DHCP reservation can stabilize the host's LAN address. Double NAT or carrier-grade NAT can prevent direct forwarding; use an agreed VPN/relay solution if direct hosting is unavailable.

Never turn off the entire firewall as a development step. A PowerShell TCP connection test is not proof that a UDP game is reachable. Verify the host is listening and attempt a real join from the relevant network. Do not publish personal LAN/public addresses, router pages or credentials in bug reports.

## Compatibility and schema changes

0.8 uses protocol 8 and rejects older clients. Protocol 7 compatibility between 0.7 and 0.7.1 is historical evidence only. Player network variables, combat RPCs, voice frame format and registered construction/rocket prefabs changed for 0.8. Use matching builds on all peers.

Review a protocol bump whenever network variable ordering/types, RPC signatures, registered prefab hashes, item/weapon/vehicle IDs, packed layouts or serialized map structs change. If bumping, update both approval payload and comparison, discovery query/version, visible error text/docs, and affected tests. Reject incompatible clients clearly instead of allowing partially decoded state. Search for `PinefallReserve/8` and `PINEFALL_FIND_8`; do not replace unrelated numeric 7 values globally.

## Shot validation and voice playback

See [Fieldcraft](FIELDCRAFT.md) for the implementation and limits. Shots carry a sequence, server-time stamp, aim state and bounded render-smoothing allowance. The server rejects replays, invalid clocks/origins and cadence violations, then queries a 500 ms history without moving live colliders. Player regions follow stance/yaw; current world geometry still blocks shots. Environment destruction is not rewound.

Voice uses sequenced 20 ms, 16 kHz mu-law frames. A burst token budget replaces rejection based on individual RPC arrival intervals. An 80 ms receive buffer reorders frames and conceals losses; audio is resampled into small device DSP blocks. Physical capture remains explicitly opt-in.

## Required multiplayer checks

Exercise host-local and remote-owner actions, a friendly and enemy team, late join, network hide/show followed by despawn, return to reserve and rehost. Voice automation uses synthetic packets; physical microphones and real internet jitter require a separate consenting human session. Connection success through the developer's router has not been established by the repository's localhost probes.
