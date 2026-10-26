# Test evidence

The current integration checks are opt-in C# probes under `Assets/Scripts`, driven by the scripts in [tools](../tools/README.md). These are real standalone host/client checks rather than Unity Test Framework unit tests. No custom EditMode/PlayMode assembly has been introduced in this handoff.

`baselines/` holds immutable historical release reports and methodology. New local runs belong in ignored `artifacts/`; their summaries/artifacts accompany a pull request. Do not replace a historical report with a later run under the same release name.

Read [TESTING](../docs/TESTING.md) before making verification claims. Repository CI validates source/evidence consistency only.

- [0.7.1 release baseline](baselines/0.7.1/README.md): historical gameplay, compatibility and rendered timing results.
- [Repository import checkpoint](baselines/repository-import-2026-10-25/README.md): fresh checkout build and portable-wrapper gameplay verification.
