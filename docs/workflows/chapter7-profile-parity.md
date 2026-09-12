# Chapter 7 Profile Parity Note

The profile-aware Chapter 7 bundle in this repository includes `collect_ui_wiring_inputs.py` as a required dependency of the runner, writer, and validator surfaces. The collector must accept `--chapter7-profile-path`, apply the same task-scope policy as `_chapter7_profile.py`, and include the resolved task scope in its summary output.

For `lastking`, repository-specific policy remains in `docs/workflows/chapter7-profile.json`; the reusable implementation stays aligned with the upstream tooling contract. This note records the dependency closure found during PR #100 hard-gate validation so future template upgrades migrate the collector together with the rest of the Chapter 7 profile bundle.
