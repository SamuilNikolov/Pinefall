# 0003: Start with honest repository CI

Status: accepted for the initial handoff, October 25, 2026.

The repository can validate metadata, package pins, links, size limits and required files on hosted GitHub runners without Unity credentials. A real Unity build needs the pinned editor/modules and an activated license; multiplayer/frame-rate checks also need appropriate runtime hardware.

Run dependency-free repository validation in the default workflow. Provide local PowerShell build/gameplay/performance wrappers and preserve historical release evidence separately. Do not make a green static workflow imply a successful Unity compile or multiplayer test. Do not configure a self-hosted runner to execute untrusted pull requests by default.

This leaves manual/licensed build evidence as a review requirement. Revisit hosted Unity CI when the owner selects licensing, runner isolation, secrets policy and artifact retention. Add real compilation/tests then, with a clearly named status check.
