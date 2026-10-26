# 0002: Keep host simulation and visual budgets separate

Status: accepted baseline, October 25, 2026.

Only the listen-server player experienced severe stutter after world density increased. The host runs authoritative wildlife/vehicles and rendering, while guests do not run all that simulation. The GPU was already active; a vendor setting would not resolve repeated CPU terrain/physics work.

Use cached terrain sampling, idle vehicle sleep, bounded far-wildlife updates and per-frame network/model work budgets. Keep physics, loot authority and map data independent of shorter-range visual loading. Retain instanced foliage, cached HUD symbols and a baked road shader mask. Automatic quality uses observed frame time rather than device names.

This adds explicit relevance/caching lifecycles that must be cleaned up on rehost and invalidated when content changes. Measure graphics-enabled hosts with clients and frame-time tails. Revisit thresholds using profiling across hardware, while preserving long-range shots, threat response, collision and map privacy.
