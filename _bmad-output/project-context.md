---
project_name: 'lastking'
user_name: 'skyo'
date: '2026-02-09 20:22:55'
sections_completed:
  - technology_stack
  - engine_rules
  - performance_rules
  - organization_rules
  - testing_rules
  - platform_rules
  - anti_patterns
status: 'complete'
rule_count: 43
optimized_for_llm: true
source_architecture: 'F:/Lastking/_bmad-output/game-architecture.md'
---

# Project Context for AI Agents

This file defines critical implementation constraints for AI agents working on Lastking.
Read this file before writing code, tests, or architecture updates.

## Technology Stack & Versions

- Engine: Godot 4.5.1 (.NET build)
- Language: C# (.NET 8) for core/runtime, GDScript only for thin scene glue where needed
- Platform: Windows only (Steam single-player)
- Persistence: SQLite (domain), ConfigFile (settings), Steam Cloud (sync)
- Testing: xUnit + FluentAssertions + NSubstitute + coverlet, GdUnit4 for scene/integration
- Observability/quality: structured logs in `logs/**`, CI quality gates, Sentry release-health workflow

## Critical Implementation Rules

### Engine-Specific Rules

- Keep Core domain independent from Godot APIs.
- `Game.Core/**` must not reference `Godot.*` namespaces/types.
- Use adapters for all engine-bound operations (time, input, persistence, event dispatch).
- Scene scripts in `Scripts/**` are thin orchestration glue only.
- Runtime ownership split:
  - Domain logic -> `Game.Core/**`
  - Engine/runtime orchestration -> `Game.Godot/**`
  - Scene composition/wiring -> `Scenes/**` + `Scripts/**`

### Performance Rules

- Architecture target: Avg FPS 60, 1% low 45.
- Treat retargeting and clone updates as hot paths.
- Use object pooling for high-churn entities (enemies/projectiles).
- Avoid per-frame allocations in hot paths.
- Logging in hot paths must be sampled/throttled.
- Pre-wave warm-up required for expected combat assets.

### Code Organization Rules

- Organization pattern is Hybrid (type-first top-level, feature/domain grouping inside).
- Mandatory boundaries:
  - `Game.Core/**` no engine dependencies
  - `Game.Godot/**` adapter + runtime composition only
  - `Scripts/**` no domain rule implementations
- Contracts are SSoT in `Game.Core/Contracts/**`.
- Public events must be typed contracts, not string literals in runtime code.
- Event type naming convention:
  - `${DOMAIN_PREFIX}.<entity>.<action>`
- Define `DOMAIN_PREFIX` once in `Game.Core/Contracts/Common/DomainPrefix.cs`.

### Testing Rules

- Mirror production changes with tests in the corresponding test project.
- Domain/state changes require xUnit updates in `Game.Core.Tests/**`.
- Scene/runtime behavior changes require GdUnit4/integration updates in `Tests.Godot/**`.
- Required validation shape for core novel systems:
  - blocked-path deterministic retarget tests
  - channel-composed wave budget tests
  - exhausted reward fallback tests
  - illegal state transition tests
- Never disable tests to pass gates.

### Platform & Build Rules

- Windows-only assumptions are allowed; do not add cross-platform abstractions unless requested.
- Use Windows-compatible commands and scripts (`py -3`, `dotnet`, Godot headless Windows flows).
- Keep release builds free of debug bypasses.
- Debug tools must be both compile/profile-gated and runtime-flag-gated.

### Critical Don't-Miss Rules

- Full path blocking is legal gameplay behavior.
- If path-to-castle fails, enemy must target nearest blocking structure.
- Wave budgeting uses independent channels (`normal`, `elite`, `boss`).
- Reward system state machine is mandatory:
  - `catalog_available`
  - `exhausted`
  - `gold_fallback` (repeat allowed)
- Save snapshot atomic boundary must include:
  - random seed
  - wave timer baseline/state
  - building queue state
- Boss-night boss count is fixed at 2 and does not scale with difficulty.

## Enforcement and Evidence

- Use CI artifacts under `logs/ci/<YYYY-MM-DD>/` for compliance evidence.
- Security/audit records use JSONL with required fields:
  - `ts`, `level`, `action`, `reason`, `target`, `caller`
- Event registry sync evidence file:
  - `logs/ci/<YYYY-MM-DD>/contracts-registry-check.json`
- Placeholder text is forbidden in architecture-spec outputs.

## Anti-Patterns to Avoid

- Putting Godot types or node logic in `Game.Core/**`.
- Implementing domain decisions directly in scene scripts.
- Ad-hoc string event names outside contracts.
- Bypassing phase scheduler ownership with side-channel state mutation.
- Generating reward UI choices without passing reward state machine.
- Merging normal/elite/boss budgets into one opaque value.
- Handling blocked-path enemies with random target heuristics.
- Treating debug toggles as runtime user options in release exports.
- Writing settings into domain DB or domain state into settings files.

## Usage Guidance

- Read this file before implementation planning and code generation.
- If a new change conflicts with any rule here, update architecture and ADR mapping first.
- If architecture evolves, update this file immediately to prevent agent drift.

Last Updated: 2026-02-09 20:22:55
