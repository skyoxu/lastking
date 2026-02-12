# LASTKING PRD Shard 03 - UI, Save, i18n, Achievements, Ops

## Global Locked Constraints
- Platform: Steam single-player, Windows only, Godot 4.5.1 + C#.
- Match duration target: 60-90 minutes.
- Day/Night: 4 min Day + 2 min Night, Day1-Day15.
- Win: survive Day15. Lose: castle HP = 0.
- Friendly fire: disabled for all player attacks.
- Config-first balancing: no hardcoded balance values.

## Task Generation Rules
- Generate exactly 10 top-level tasks for this shard.
- Keep each top-level task complexity <= 6.
- Each task must include concrete acceptance criteria and test strategy.
- Use Windows-compatible command and pipeline assumptions.


## In Scope
- Camera and interaction: edge scrolling + keyboard scrolling.
- Runtime speed controls: pause / 1x / 2x with full timer freeze during pause.
- UI feedback for invalid placement, blocked actions, and migration/load errors.
- Save system: one autosave slot, starts-of-day autosave, migration required.
- Invalid config/load behavior: reject load with clear user-facing reason.
- Cloud save: Steam account binding model.
- Battle report minimal fields: seed, wave timer, build queue, reward choices, economy summary.
- Achievements: visible list, non-hidden, unlock and trigger timing must be deterministic.
- Localization: zh-CN and en-US at launch, key-based i18n workflow.
- Audio settings at launch: music + SFX channels.
- Non-functional goals: low-end baseline 45 FPS (1% low), average 60 FPS target.

## Out of Scope
- Deep wave budget formulas and raw schema field design.

## Acceptance Anchors
- Pause truly freezes all gameplay timers and queued countdowns.
- Save migration failures are visible and safely handled.
- i18n keys remain stable across patching.
