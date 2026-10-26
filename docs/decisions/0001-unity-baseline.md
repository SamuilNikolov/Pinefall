# 0001: Preserve an importable Unity baseline

Status: accepted for the initial repository handoff, October 25, 2026.

The tested game was developed in a local generated Unity project. A new contributor needs to import and run without the original workspace or downloading/regenerating art through private scripts. A broad folder/namespace/assembly rewrite during import would make regressions and lost asset references harder to separate from the handoff.

Keep the Unity project at repository root, pin editor/packages, and commit source art, scene, runtime prefabs, generated TerrainData, extracted clips, atlas/road mask and `.meta` GUIDs. Exclude editor caches/builds/local reports. Use ordinary Git for the current file sizes; establish an LFS policy before much larger art arrives.

This makes the clone larger but self-contained. Future modularization should use focused behavior-neutral moves with GUID preservation. Revisit storage when asset growth or clone time justifies a coordinated LFS/object-store change. Revisit assembly boundaries after mapping partial-class/shared-singleton dependencies.
