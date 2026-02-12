---

title: 'Game Architecture'
project: 'lastking'
date: '2026-02-09'
author: 'skyo'
version: '1.0'
stepsCompleted: [1, 2, 3, 4, 5, 6, 7, 8, 9]
status: 'complete'

# Source Documents
gdd: 'F:/Lastking/_bmad-output/lastking-gdd-v1.0.md'
epics: null
brief: null
engine: 'Godot 4.5.1 + C#'
platform: 'Windows (Steam)'
---

# Game Architecture


## Executive Summary

**Lastking** architecture is designed for **Godot 4.5.1 + C#** targeting **Windows (Steam)**.

**Key Architectural Decisions:**
- Runtime uses centralized phase orchestration with deterministic snapshots for day/night/wave flow.
- Path blocking is legal and enforced through nearest-blocker retarget policy with performance guardrails.
- Persistence adopts hybrid storage (SQLite + ConfigFile + Steam Cloud) with atomic save boundary (`seed/timer/queue`).

**Project Structure:** Hybrid organization with explicit boundaries across `Game.Core`, `Game.Godot`, `Scenes`, and `Scripts`.

**Implementation Patterns:** 7 patterns (4 standard + 3 novel) defined with examples, trigger boundaries, and enforcement rules for AI-agent consistency.

**Ready for:** Epic implementation phase.


## Document Status

This architecture document has been completed through the BMGD Architecture Workflow.

**Steps Completed:** 9 of 9 (Architecture Complete)

---

_Content will be added as we progress through the workflow._


## Project Context

### Game Overview

**Lastking** is a Windows-only Steam single-player tower defense game with high-pressure night assaults and macro-focused day planning.

### Technical Scope

**Platform:** Windows (Steam)  
**Genre:** Tower Defense + Macro Management  
**Project Level:** Medium-High complexity

### Core Systems

| System | Complexity | GDD Reference |
| --- | --- | --- |
| Day/Night Loop and Phase Orchestration | High | §4, §6 |
| Enemy Budgeting and Wave Composition | High | §10 |
| Path Blocking and Retargeting | High | §9, §10 |
| Economy and Population Flow | Medium-High | §7 |
| Building Placement and Upgrade Pipeline | Medium | §9 |
| Auto-combat and Rally Command Model | Medium | §4, §8 |
| Boss Mechanics and Clone Lifecycle | High | §11 |
| Reward Pool Constraints and Fallback | High | §12 |
| Save-State Determinism and Recovery | High | §14 |
| Achievement Trigger System | Medium | §15 |

### Architectural Decision Anchors (Pre-ADR Set)

#### A-01: Single-player deterministic-first runtime
- Decision: v1 excludes multiplayer runtime concerns and prioritizes deterministic-enough simulation state for reproducible save/load.
- Trade-off: less future-proof for online sync, but dramatically lowers v1 complexity and defect surface.
- Rationale: current scope is Steam single-player only.

#### A-02: Full path blocking is legal, enemy fallback is mandatory
- Decision: player defenses may fully block routes; enemies must retarget nearest blocking structure when pathing fails.
- Trade-off: pathfinding edge cases are costlier, but gameplay promise is preserved.
- Rationale: core GDD rule, non-negotiable.

#### A-03: Wave budgeting uses independent tracks
- Decision: normal wave budget grows daily at 120% multiplicative; elite and boss budgets are independent tracks.
- Trade-off: scheduler complexity rises, tuning becomes controllable.
- Rationale: avoids hidden coupling and scaling chaos.

#### A-04: Reward pool exhaustion has explicit fallback contract
- Decision: reward pool non-repeatable/non-stackable; exhausted pool falls back to gold-only choices (repeat allowed).
- Trade-off: reduced novelty when exhausted, but no dead-end UI states.
- Rationale: progression continuity and deterministic behavior.

#### A-05: Save snapshot must be atomic for critical fields
- Decision: auto-save payload must atomically include random seed, wave timer, and building queue.
- Trade-off: stricter persistence boundary, better reproducibility.
- Rationale: prevents non-reproducible simulation drift.

#### A-06: Performance budgets are first-class constraints
- Decision: Avg FPS 60 and 1% low 45 are architecture-time constraints.
- Trade-off: constrains feature expression, lowers late refactor risk.
- Rationale: hot path safety must be designed early.

### Architectural Decision Register (Execution-Ready)

| ID | Decision | Status | Existing ADR Mapping | Supersede Trigger | Acceptance Evidence |
| --- | --- | --- | --- | --- | --- |
| A-01 | Single-player deterministic-first runtime | Locked | ADR-0001, ADR-0011 | Introduce online/co-op scope | Fixed-seed save/load reproducibility tests |
| A-02 | Full path blocking legal; enemy attacks nearest blocker when path fails | Locked | Gap: new ADR needed | Path model/gate rule changes | Blocked-lane retarget tests |
| A-03 | Normal/Elite/Boss use independent budget tracks | Locked | Gap: new ADR needed | Scheduler merges tracks | Day1-15 balance contract tests |
| A-04 | Reward pool non-repeat/non-stack; exhausted pool -> gold-only | Locked | Gap: new ADR needed | Reward progression changes | Reward state-machine exhausted-pool tests |
| A-05 | Auto-save snapshot includes seed/timer/queue atomically | Locked | ADR-0006, ADR-0023 | Save schema refactor | Save/load round-trip consistency tests |
| A-06 | Performance budget is architecture constraint | Locked | ADR-0015 | Hardware/frame target changes | Perf smoke meets target |

### Self-Consistency Validation

| Check ID | Potential Conflict | Validation Result | Resolution Contract |
| --- | --- | --- | --- |
| SC-01 | One-wave-per-night vs independent elite/boss budgets | Pass | Scheduler uses normal baseline + optional special overlays |
| SC-02 | Non-repeat/non-stack vs gold fallback repeat | Pass | Restriction applies to non-gold reward entries only |
| SC-03 | Full path block vs one-way gate | Pass | Path fail triggers nearest-blocker attack |
| SC-04 | Day-start auto-save vs wave timer persistence | Pass | Canonical snapshot includes phase timer baseline |
| SC-05 | Persistent clones vs FPS targets | Conditional pass | Clone cap=10 with hot-path perf guardrails |
| SC-06 | Fixed 2 bosses vs difficulty scaling | Pass | Difficulty never modifies boss count |

### Architectural Invariants (Must Not Drift)

1. Boss-night boss count is always 2 and difficulty-independent.
2. Full path blocking is legal and enemy fallback behavior is mandatory.
3. Reward exhaustion always yields valid gold-only choices.
4. Save payload atomically includes seed/timer/queue.
5. Performance budget is a design-time constraint.

### Planning-Oriented Execution Model

#### World Model
- Windows-only single-player runtime.
- Deterministic-enough phase-driven simulation.
- Avg60 / 1%low45 is mandatory architecture input.
- Save atomicity boundary is non-negotiable.

#### Implementation Order
1. Non-reversible contracts (blocking, budget channels, reward fallback, save atomicity)
2. Runtime backbone (phase scheduler, channel composition, retarget lifecycle)
3. Hot-path safety (clone lifecycle budget, retarget throttling, telemetry points)
4. Feature slice expansion (advanced units, reward weights, achievements, difficulty modifiers)

#### Decision Gates
- G1: Contract completeness and non-conflict
- G2: Scheduler covers normal/elite/boss nights unambiguously
- G3: Deterministic blocked-path retarget behavior
- G4: Reproducible day-start save/load behavior
- G5: Explicit perf monitoring on retarget + clone hot paths

### Technical Requirements

- Single-player-only architecture (no networking stack in v1)
- Deterministic save payload (seed/timer/queue)
- Performance target: Avg60 / 1%low45
- Full path blocking support with nearest-blocker fallback
- Fixed boss-night boss count (2), independent from difficulty

### Complexity Drivers

- Independent normal/elite/boss budget tracks
- Full path-block legality + one-way gate interaction
- Reward constraints with deterministic fallback
- Persistent clone lifecycle under cap

### Technical Risks

- Retarget/path update frame spikes in blocked-lane scenarios
- Clone-heavy runtime pressure in late peaks
- Save/load drift without strict atomic snapshot
- Balance collapse without strict cap policy on reward-tech interactions


## Engine & Framework

### Selected Engine

**Godot** v4.5.1 with **C# (.NET)**

**Rationale:**
- Matches current repository baseline and avoids migration noise during architecture definition.
- Strong fit for Windows-only single-player 2D top-down gameplay.
- Keeps implementation aligned with existing layered structure (Scenes -> Adapters -> Core).

### Project Initialization

Use the current repository baseline as the starter (no external template adoption).

### Engine-Provided Architecture

| Component | Solution | Notes |
| --- | --- | --- |
| Rendering | Godot Forward Plus renderer | Existing project uses 4.5 Forward Plus profile |
| Physics | Built-in 2D/3D physics pipeline | Adequate for tower-defense collision/targeting |
| Audio | Godot audio bus and stream players | Native support, no extra middleware required |
| Input | InputMap + action system | Supports edge-scroll + keyboard control mapping |
| Scene Management | Node/Scene tree composition | Fits modular feature-scene architecture |
| Build/Export | Godot export pipeline | Windows-only release path already aligned |

### Engine Version Governance

- Baseline version is frozen at **Godot 4.5.1 + C#** for architecture and early implementation phases.
- No external starter template is adopted; the current repository baseline is the only starter.
- Re-evaluation point for engine upgrade is deferred to architecture validation/completion stages.
- Upgrade to a newer engine branch is allowed only if:
  1. Core contract tests remain green
  2. Save/load consistency checks remain stable
  3. Performance smoke gates remain within target (Avg60 / 1%low45)

### Remaining Architectural Decisions

1. Phase scheduler contract and ownership boundaries.
2. Wave channel model (normal/elite/boss independent budget tracks).
3. Path-block failover behavior contract and deterministic retarget policy.
4. Save-state atomic boundary for seed/timer/build queue.
5. Reward pool state machine and exhaustion fallback behavior.
6. Hot-path performance governance for blocked-lane and clone-heavy nights.


## Architectural Decisions

### Decision Summary

| Category | Decision | Version | Rationale |
| --- | --- | --- | --- |
| State Management | Autoload + Explicit State Machines | Godot 4.5.1 runtime model | Matches current layered architecture and enables deterministic phase transitions with testable boundaries. |
| Data Persistence | Hybrid: SQLite + ConfigFile + Steam Cloud | Godot 4.5.1 + Steamworks latest at integration date | Aligns with domain/settings separation and cloud requirement while preserving local robustness. |
| Runtime Orchestration | Central PhaseScheduler with immutable phase snapshots | Internal architecture contract | Ensures 4+2 cadence, one-wave-per-night, and independent budget channel composition remain consistent. |
| Pathing Failure Policy | PathFail -> nearest blocking structure -> cooldown re-evaluation | Internal AI/pathing contract | Preserves legal full-block gameplay while preventing uncontrolled pathfinding thrash. |
| Reward System | Explicit reward state machine (catalog_available/exhausted/gold_fallback) | Internal progression contract | Guarantees non-repeat/non-stack rules and deterministic gold fallback behavior. |
| Asset Management | Scene-based loading + lazy sub-assets + pre-wave warm-up | Godot scene/resource pipeline | Balances startup latency and combat-time hitch risk under 60 FPS target. |

### State Management

**Approach:** Autoload + Explicit State Machines
- Global orchestration state is owned by dedicated autoload services.
- Gameplay mode transitions are controlled by explicit state machine contracts.
- Scene/UI layers consume state via adapters, not ad-hoc globals.

### Data Persistence

**Save System:** Hybrid (SQLite + ConfigFile + Steam Cloud)
- SQLite stores domain progression and run-critical structures.
- ConfigFile stores user settings/preferences only.
- Steam Cloud syncs save artifacts for cross-device continuity.
- Atomic snapshot boundary must include: random seed, wave timer, building queue.

### Runtime Orchestration

**Approach:** Central PhaseScheduler
- Scheduler owns day/night/settlement phase authority.
- Wave composition uses independent channels: normal + optional special overlays.
- Subsystems consume immutable phase snapshots to reduce race conditions.

### AI Pathing and Block Handling

**Approach:** Deterministic path-fail fallback
- If no valid path exists, enemy selects nearest blocking structure.
- Retargeting uses cooldown/throttling to protect frame-time stability.
- Re-evaluation occurs on structural change events and policy cooldown ticks.

### Reward Architecture

**Approach:** Explicit progression state machine
- `catalog_available`: normal candidate selection with non-repeat/non-stack constraints.
- `exhausted`: no valid non-gold entries remain.
- `gold_fallback`: valid gold-only choices, repeat allowed.
- Transition conditions are explicit and testable.

### Asset Management

**Loading Strategy:** Scene-based + lazy + pre-wave warm-up
- Core gameplay scene assets load by scene boundaries.
- Secondary assets are lazily loaded on demand.
- Pre-wave warm-up prepares expected wave assets to minimize combat hitches.

### Version Verification Notes

| Technology | Verified Baseline | Verification Note |
| --- | --- | --- |
| Godot Engine | 4.5.1-stable | Verified from official download archive and release pages. |
| Godot Docs Branch | 4.5 | Confirms 4.5 branch documentation and .NET support context. |
| Steamworks SDK | Latest (portal-defined) | Official docs provide latest SDK entry point; exact version is resolved at integration date in partner portal. |

### ADR Alignment and Follow-up

- Existing alignment: ADR-0001, ADR-0011, ADR-0006, ADR-0023, ADR-0015.
- Proposed new ADRs to formalize locked contracts:
  1. Path blocking and nearest-blocker fallback contract
  2. Independent wave budget channels contract
  3. Reward pool exhaustion fallback contract

### Decision Stress-Test Addendum (ADR Challenge Pass)

#### D1 State Management (Autoload + Explicit State Machines)
- Assumption: global orchestration points remain limited and stable.
- Failure mode: autoload service grows into god-object and leaks domain logic.
- Guardrail:
  1. Autoload only orchestrates phase and service routing.
  2. Domain rules must stay in Core layer.
- Revisit trigger: if any autoload exceeds defined responsibility boundaries or causes circular dependencies.

#### D2 Persistence Hybrid (SQLite + ConfigFile + Steam Cloud)
- Assumption: storage responsibilities remain strictly separated.
- Failure mode: settings/domain data boundary drifts; cloud conflict resolution becomes ambiguous.
- Guardrail:
  1. Domain persistence only via SQLite schema contracts.
  2. ConfigFile only for settings.
  3. Cloud sync uses deterministic priority policy (local canonical at day-start checkpoint unless explicit restore).
- Revisit trigger: schema versioning breaks round-trip load consistency.

#### D3 Central PhaseScheduler + Immutable Snapshots
- Assumption: scheduler remains single source of phase truth.
- Failure mode: subsystems mutate phase state out-of-band.
- Guardrail:
  1. Scheduler is write-owner; consumers are read-only.
  2. Snapshot version id required for downstream processing.
- Revisit trigger: inconsistent phase outcomes under fixed seed replay.

#### D4 PathFail -> Nearest Blocker -> Cooldown Recheck
- Assumption: nearest-blocker strategy is deterministic enough across runs.
- Failure mode: frequent retarget loops produce frame spikes.
- Guardrail:
  1. Retarget cooldown floor and maximum retries per tick.
  2. Structural-change event debounce before recomputation.
- Revisit trigger: blocked-lane scenarios exceed frame budget thresholds.

#### D5 Reward State Machine (catalog/exhausted/gold_fallback)
- Assumption: transition graph is closed and exhaustive.
- Failure mode: illegal mixed state (catalog partially exhausted + invalid options shown).
- Guardrail:
  1. One active state at a time.
  2. Transition checks are unit-tested as pure logic.
- Revisit trigger: any invalid reward UI state observed in telemetry or tests.

#### D6 Asset Strategy (scene-based + lazy + pre-wave warm-up)
- Assumption: warm-up list accurately predicts wave-time needs.
- Failure mode: lazy loads still happen on hot path and hitch night combat.
- Guardrail:
  1. Pre-wave asset manifest generated from wave composition.
  2. Warm-up completion gate before night starts.
- Revisit trigger: hitch metrics exceed threshold in nightly perf smoke.

### Architecture Governance Hooks

| Decision | Mandatory Evidence | Rollback Condition |
| --- | --- | --- |
| D1 | state-transition consistency tests | repeated illegal transition or ownership violation |
| D2 | save/load round-trip + cloud conflict tests | deterministic restore cannot be guaranteed |
| D3 | fixed-seed replay phase consistency | phase divergence across identical inputs |
| D4 | blocked-lane perf and behavior tests | sustained retarget-induced frame spikes |
| D5 | reward state-machine invariant tests | invalid reward state reachable |
| D6 | pre-wave warm-up and hitch telemetry | night-phase stutter beyond accepted budget |

### ADR Lifecycle Rule

- Any change to D1-D6 requires:
  1. ADR update or supersede note
  2. explicit trigger reason
  3. acceptance evidence update
  4. rollback criterion declaration


## Cross-cutting Concerns

These patterns apply to ALL systems and are mandatory for every implementation.

### Error Handling

**Strategy:** `Result<T>` + layered handling

- Core layer returns `Result<T>` (no silent failure).
- Adapter layer maps engine/external failures into typed domain errors.
- Scene/UI layer decides user-facing feedback and fallback behavior.
- Unhandled exceptions are captured by a top-level safety handler and logged with context.

**Error Levels:**
- `Critical`: run integrity threatened (e.g., save snapshot corruption)
- `Recoverable`: operation fails but game loop continues
- `Validation`: input/state rejected by domain constraints

**Example (C#):**
```csharp
public sealed record ErrorInfo(string Code, string Message, string Context);

public readonly record struct Result<T>(bool IsSuccess, T? Value, ErrorInfo? Error)
{
    public static Result<T> Success(T value) => new(true, value, null);
    public static Result<T> Failure(string code, string message, string context) =>
        new(false, default, new ErrorInfo(code, message, context));
}
```

### Logging

**Format:** structured JSON lines  
**Destination:** console summary + file audit logs under `logs/**`

- Security/network/file/permission audits must be written to `logs/ci/<YYYY-MM-DD>/security-audit.jsonl`.
- Runtime and test artifacts follow repo SSoT paths (`logs/unit`, `logs/e2e`, `logs/perf`, `logs/ci`).
- Hot-path logging uses sampling/throttling to avoid frame-time spikes.

**Log Levels:**
- `ERROR`: invariant broken, operation aborted
- `WARN`: degraded but recovered
- `INFO`: lifecycle milestones (phase change, save checkpoint)
- `DEBUG`: diagnostic details (development/test mode)
- `TRACE`: ultra-verbose, disabled by default

**Example (JSONL):**
```json
{"ts":"2026-02-09T12:00:00Z","level":"WARN","action":"path.retarget","reason":"path_blocked","target":"wall_42","caller":"EnemyAiService"}
```

### Configuration

**Approach:** three-tier configuration model

1. **Code constants**: non-negotiable system constants and compile-time assumptions
2. **Balance config files**: gameplay tuning values (wave curves, costs, multipliers)
3. **Player settings**: user preferences via `ConfigFile` (`user://settings.cfg`)

**Configuration Structure:**
- `Game.Core/Contracts/**`: typed config contracts
- `Scripts/Core/**`: config consumption and validation
- `user://settings.cfg`: player-level preferences only
- No mixing domain persistence with settings persistence

### Event System

**Pattern:** typed event bus with contract-first event definitions

- Event types are strong-typed contracts in `Game.Core/Contracts/**`.
- Event naming convention: `${DOMAIN_PREFIX}.<entity>.<action>`.
- Event payloads are immutable records.
- Event publication failures follow explicit policy and are audited.

**Example (C#):**
```csharp
namespace Game.Core.Contracts.Events;

public sealed record CastleDamaged(
    string CastleId,
    int Damage,
    int RemainingHp,
    DateTimeOffset OccurredAt)
{
    public const string EventType = "core.castle.damaged";
}
```

### Debug Tools

**Available Tools:**
- Debug overlay (phase, wave, budget, entity counts)
- State inspector (scheduler state, reward state machine, save checkpoint state)
- Performance hooks (retarget hotspot, clone update cost, frame budget sampling)

**Activation:**
- Enabled in development/test mode only
- Disabled in release exports by default
- Controlled through explicit secure flags (e.g., `SECURITY_TEST_MODE`, debug build guards)

**Release Safety Rules:**
- No debug cheat command exposure in production
- No privileged debug path bypassing gameplay/security constraints

### Cross-cutting Enforcement Profile (Hard Rules)

#### E-01 Error Code Governance
- Every `Result<T>.Failure` must use a registered, enumerable error code.
- Error code format: `LK_<DOMAIN>_<CATEGORY>_<NNN>` (example: `LK_SAVE_IO_001`).
- New error codes require contract update in `Game.Core/Contracts/Common/ErrorCodes.cs`.
- Unknown/unregistered code is build-time lint failure.

#### E-02 Structured Logging Contract
- Security/audit logs MUST be JSONL with required fields:
  - `ts`, `level`, `action`, `reason`, `target`, `caller`
- Missing required fields is CI validation failure.
- Hot-path logs must declare sampling strategy (`sample_rate` or throttle policy).

#### E-03 Event Contract Registry
- All public event types must be declared in a single registry:
  - `Game.Core/Contracts/Events/EventTypeRegistry.cs`
- Event type strings are constants, no runtime free-form literals.
- Registry duplication or missing contract mapping is test failure.

#### E-04 Configuration Ownership Guard
- Constants, balance parameters, and player settings have non-overlapping ownership.
- Cross-write violations (e.g., settings writing to domain DB) are forbidden.
- Persistence boundary checks are required in unit tests.

#### E-05 Debug Feature Safety
- Debug features are compile/profile-gated.
- Release build must fail CI if debug-only controls are active.
- Any bypass path (economy, combat, save) in release is a hard gate failure.

### Cross-cutting Quality Gates (Executable)

| Gate | Rule | Evidence Artifact |
| --- | --- | --- |
| CG-01 | Error codes are registered and searchable | `logs/ci/<date>/typecheck.log` + contract scan output |
| CG-02 | JSONL audit schema is valid | `logs/ci/<date>/security-audit.jsonl` |
| CG-03 | Event registry and contracts are in sync | `logs/ci/<date>/contracts-registry-check.json` |
| CG-04 | Config ownership boundaries are respected | `logs/unit/<date>/coverage.json` + persistence tests |
| CG-05 | Release build has no debug bypass | `logs/ci/<date>/export.log` + release checks |

### AI Agent Implementation Rules (Conflict Prevention)

1. No string-literal event types outside contract constants.
2. No silent catch blocks in Core or Adapter layers.
3. No direct writes to domain persistence from UI/Scene layer.
4. No debug toggle leakage into release runtime code path.
5. Any cross-cutting rule change must update ADR mapping and acceptance evidence.

### Consistency Validation Addendum (Round 1)

#### CV-01 Domain Prefix Single Source
- Define `DOMAIN_PREFIX` once in `Game.Core/Contracts/Common/DomainPrefix.cs`.
- All event constants must be generated/composed from this source.
- Mixed literal prefixes are forbidden.

#### CV-02 Event Registry Evidence Tightening
- Add dedicated CI artifact for registry sync:
  - `logs/ci/<YYYY-MM-DD>/contracts-registry-check.json`
- Do not use generic task-link output as primary evidence for event registry integrity.

#### CV-03 Error Code Runtime Fallback Contract
- Build-time: unregistered error code fails CI.
- Runtime safety: map unknown code to `LK_SYS_UNKNOWN_000` and emit `ERROR` audit entry with full context.
- Release still treats recurrence as gate failure in next CI cycle.

#### CV-04 Logging Schema Clarification
- Mandatory audit fields remain:
  - `ts`, `level`, `action`, `reason`, `target`, `caller`
- `sample_rate` is optional and only required for throttled/hot-path logs.

#### CV-05 Release Debug Safety Consistency
- Debug tools must be both:
  1. compile/profile-gated
  2. runtime-flag-gated
- Passing only one gate is considered non-compliant.


## Project Structure

### Organization Pattern

**Pattern:** Hybrid (type-first at top level, feature/domain grouping inside)

**Rationale:**
- Preserves current repository baseline and minimizes churn.
- Keeps engine-facing code separate from pure domain code.
- Enables parallel development with clear ownership boundaries.

### Directory Structure

```text
lastking/
├── Game.Core/                             # Pure C# domain/contracts (no Godot API)
│   ├── Contracts/
│   │   ├── Common/
│   │   ├── Events/
│   │   ├── Combat/
│   │   ├── Economy/
│   │   └── Progression/
│   ├── Domain/
│   ├── Services/
│   ├── StateMachines/
│   └── Persistence/
├── Game.Core.Tests/                       # xUnit unit tests for Core
│   ├── Contracts/
│   ├── Domain/
│   ├── Services/
│   └── StateMachines/
├── Game.Godot/                            # Godot adapter layer (C# + Godot API)
│   ├── Adapters/
│   │   ├── Input/
│   │   ├── Time/
│   │   ├── Persistence/
│   │   └── EventBus/
│   ├── Runtime/
│   │   ├── PhaseScheduler/
│   │   ├── WaveOrchestration/
│   │   └── Diagnostics/
│   └── Composition/
├── Tests.Godot/                           # GdUnit4 and integration tests
│   ├── tests/
│   │   ├── Scenes/
│   │   ├── Integration/
│   │   └── Security/
│   └── addons/
├── Scenes/                                # Godot scenes by feature
│   ├── Core/
│   ├── Combat/
│   ├── Economy/
│   ├── UI/
│   └── Debug/
├── Assets/                                # Art/audio/ui assets
│   ├── art/
│   ├── audio/
│   ├── ui/
│   └── data/
├── Scripts/                               # Godot-attached scripts (thin layer)
│   ├── Core/
│   ├── Adapters/
│   ├── UI/
│   └── Debug/
├── docs/
│   ├── adr/
│   ├── architecture/
│   └── prd/
├── scripts/
│   ├── python/
│   └── ci/
├── logs/
│   ├── unit/
│   ├── e2e/
│   ├── perf/
│   └── ci/
├── _bmad/
├── project.godot
└── *.sln / *.csproj
```

### System Location Mapping

| System | Location | Responsibility |
| --- | --- | --- |
| Phase Scheduler | `Game.Godot/Runtime/PhaseScheduler/` | Day/Night/Settlement authority |
| Wave Budgeting | `Game.Core/Services/` + `Game.Godot/Runtime/WaveOrchestration/` | Budget calculation + runtime spawn orchestration |
| Path-Block Retarget | `Game.Core/Services/` + `Game.Godot/Adapters/` | Path-fail policy + engine query integration |
| Reward State Machine | `Game.Core/StateMachines/` | catalog/exhausted/gold_fallback transitions |
| Save Snapshot | `Game.Core/Persistence/` + `Game.Godot/Adapters/Persistence/` | Atomic payload + storage adapter |
| Event Contracts | `Game.Core/Contracts/Events/` | Typed events and registry |
| UI Flow | `Scenes/UI/` + `Scripts/UI/` | Presentation and user interactions |
| Debug/Perf Hooks | `Game.Godot/Runtime/Diagnostics/` + `Scripts/Debug/` | Overlay, inspectors, perf counters |

### Naming Conventions

#### Files
- C# source files: `PascalCase.cs`
- Test files: `<Subject>NameTests.cs`
- Scene files: `PascalCase.tscn`
- GDScript files: `snake_case.gd`
- Data files: `snake_case.json` / `snake_case.cfg`

#### Code Elements
| Element | Convention | Example |
| --- | --- | --- |
| Class / Record / Enum | PascalCase | `PhaseScheduler`, `CastleDamaged` |
| Method | PascalCase | `TryAdvancePhase` |
| Local variable / parameter | camelCase | `remainingHp` |
| Constant | UPPER_SNAKE_CASE | `MAX_CLONE_COUNT` |
| Private field | `_camelCase` | `_eventBus` |

#### Events
- Event type string convention: `${DOMAIN_PREFIX}.<entity>.<action>`
- Event constants are defined only in contracts (no runtime literals)

### Architectural Boundaries (Mandatory)

1. `Game.Core/**` MUST NOT reference Godot namespaces/types.
2. `Scripts/**` MUST remain thin; no core domain rules in scene scripts.
3. `Scenes/**` contain composition and wiring, not business rules.
4. `Game.Godot/**` adapts engine APIs but does not redefine domain contracts.
5. Cross-layer communication uses typed contracts/events, not ad-hoc string channels.

### Structure Consistency Validation Addendum (Round 1)

#### SV-01 Ownership Clarity: `Game.Godot` vs `Scripts`
- Risk: duplicate runtime logic across `Game.Godot/**` and `Scripts/**`.
- Rule:
  1. `Game.Godot/**` owns runtime system logic and engine adaptation orchestration.
  2. `Scripts/**` is scene glue only (node binding, signal hookup, view logic).
  3. Any domain rule found in `Scripts/**` is non-compliant.

#### SV-02 Core Purity Boundary
- Risk: accidental Godot API leak into `Game.Core/**`.
- Rule:
  1. `Game.Core/**` must not reference `Godot.*`.
  2. Engine-specific types must be mapped in adapter layer.
  3. Boundary violations fail CI type/lint gate.

#### SV-03 Mapping Completeness
- Risk: system-location map exists but not exhaustive for critical flows.
- Rule:
  1. Every critical system from Step 4 (D1-D6) must map to one primary location and optional adapter location.
  2. Unmapped critical system blocks architecture completion.

#### SV-04 Naming Convention Coherence
- Risk: mixed conventions create agent-generated drift.
- Rule:
  1. C# files/classes/methods follow documented conventions only.
  2. Event names must use `${DOMAIN_PREFIX}.<entity>.<action>`.
  3. String-literal event names outside contracts are forbidden.

#### SV-05 Test Structure Alignment
- Risk: production structure evolves but test structure lags.
- Rule:
  1. `Game.Core/**` changes require `Game.Core.Tests/**` mirror coverage updates.
  2. Scene/runtime behavior changes require `Tests.Godot/**` updates.
  3. Missing mirrored test updates requires explicit waiver note.

### Structural Invariants (Must Not Drift)

1. Domain logic remains in `Game.Core/**`, never in scene scripts.
2. Runtime orchestration remains in `Game.Godot/**`, not duplicated in `Scripts/**`.
3. Scene files own composition; adapters own engine interaction boundaries.
4. Contract/event definitions remain centralized and type-safe.
5. Critical systems D1-D6 always have deterministic file ownership.

### Structure Validation Gates

| Gate | Check | Evidence |
| --- | --- | --- |
| SG-01 | No Godot namespace in `Game.Core/**` | typecheck/lint output |
| SG-02 | No domain rules in `Scripts/**` | architecture lint or review checklist |
| SG-03 | D1-D6 mapping completeness | architecture section validation |
| SG-04 | Event naming consistency | contract registry validation |
| SG-05 | Test mirror alignment | unit/e2e change-set check |


## Implementation Patterns

These patterns ensure consistent implementation across all AI agents.

### Novel Patterns

#### 1) Path-Blocked Combat Reconciliation Pattern

**Purpose:**
Handle legal full-path blocking while preserving deterministic enemy behavior and frame stability.

**Components:**
- `PathQueryService` (adapter-facing path check)
- `BlockTargetSelector` (nearest blocking structure resolver)
- `RetargetPolicy` (cooldown/retry throttle)
- `EnemyActionCoordinator` (attack/retarget execution)

**Data Flow:**
1. Enemy requests path to castle
2. If path exists -> proceed normal navigation
3. If path fails -> emit `path.blocked` context -> select nearest blocker
4. Apply cooldown gate -> attack blocker
5. Re-evaluate path on structural-change events or cooldown tick

**State Management:**
Enemy action state machine with explicit `Navigating`, `Blocked`, `AttackingBlocker`, `Reevaluate`.

**Example (C#):**
```csharp
public Result<EnemyCommand> ResolveBlockedPath(EnemyContext context)
{
    var path = _pathQuery.TryFindPath(context.Position, context.CastlePosition);
    if (path.IsSuccess)
        return Result<EnemyCommand>.Success(EnemyCommand.Navigate(path.Value!));

    if (!_retargetPolicy.CanRetarget(context.EnemyId, context.Now))
        return Result<EnemyCommand>.Success(EnemyCommand.Hold());

    var target = _blockTargetSelector.FindNearestBlockingStructure(context);
    return target is null
        ? Result<EnemyCommand>.Failure("LK_AI_PATH_001", "No blocker target found", context.EnemyId)
        : Result<EnemyCommand>.Success(EnemyCommand.Attack(target.StructureId));
}
```

**Usage:**
Mandatory for all enemy units when path to castle is invalid.

#### 2) Multi-Channel Wave Budget Composition Pattern

**Purpose:**
Compose normal/elite/boss budgets deterministically without hidden coupling.

**Components:**
- `PhaseScheduler`
- `BudgetChannelResolver` (`normal`, `elite`, `boss`)
- `WaveComposer`
- `SpawnPlanEmitter`

**Data Flow:**
1. Scheduler enters night phase
2. Resolve active channels by day type
3. Compute each channel budget independently
4. Compose final spawn plan with explicit channel tags
5. Emit immutable spawn plan to runtime spawner

**State Management:**
Night orchestration state machine with `PrepareChannels`, `ComposePlan`, `DispatchPlan`, `Completed`.

**Example (C#):**
```csharp
public SpawnPlan ComposeNightPlan(int dayIndex, NightType nightType)
{
    var normal = _budgetCalculator.GetNormalBudget(dayIndex);
    var elite = nightType == NightType.Elite ? _budgetCalculator.GetEliteBudget(dayIndex) : 0;
    var boss = nightType == NightType.Boss ? _budgetCalculator.GetBossBudget(dayIndex) : 0;

    return _waveComposer.Compose(new BudgetChannels(normal, elite, boss), dayIndex, nightType);
}
```

**Usage:**
Mandatory for every night wave generation path.

#### 3) Exhaustive Reward State Transition Pattern

**Purpose:**
Guarantee valid reward options under non-repeat/non-stack constraints with deterministic gold fallback.

**Components:**
- `RewardCatalog`
- `RewardEligibilityEvaluator`
- `RewardStateMachine` (`catalog_available`, `exhausted`, `gold_fallback`)
- `RewardPresenter`

**Data Flow:**
1. Build eligible reward set from catalog + run history
2. If eligible non-gold entries exist -> `catalog_available`
3. If exhausted -> transition to `gold_fallback`
4. Present exactly 3 valid choices
5. Commit chosen reward and update run state

**State Management:**
Explicit transition graph, one active state at a time.

**Example (C#):**
```csharp
public RewardSelection BuildSelection(RunRewardState state)
{
    var eligible = _evaluator.GetEligibleNonGoldRewards(state);

    if (eligible.Count >= 3)
        return RewardSelection.FromCatalog(eligible.Take(3).ToArray());

    return RewardSelection.GoldFallback(_goldFallbackProvider.GetThreeChoices());
}
```

**Usage:**
Mandatory for every post-night reward decision.

### Communication Patterns

**Pattern:** Typed Event Bus + Dependency Injection

**Rules:**
- Contracts define event payloads and event type constants
- No string-literal event names outside contract layer
- Event publication failures are logged and auditable

**Example (C#):**
```csharp
public interface IEventBus
{
    void Publish<TEvent>(TEvent evt) where TEvent : class;
    void Subscribe<TEvent>(Action<TEvent> handler) where TEvent : class;
}
```

### Entity Patterns

**Creation Pattern:** Factory + Object Pooling (for enemies/projectiles/high-churn entities)

**Rules:**
- Use pooling for high-frequency spawn/despawn paths
- Use direct instantiation only for low-frequency entities

**Example (C#):**
```csharp
public interface IEnemyFactory
{
    EnemyInstance Create(EnemySpec spec);
}

public interface IEnemyPool
{
    EnemyInstance Rent(EnemySpec spec);
    void Return(EnemyInstance instance);
}
```

### State Patterns

**Pattern:** Explicit State Machine

**Rules:**
- State transitions must be explicit and testable
- Illegal transitions return typed failures, never silent fallback

**Example (C#):**
```csharp
public enum PhaseState { Day, Night, Settlement }

public Result<PhaseState> TryAdvance(PhaseState current)
{
    return current switch
    {
        PhaseState.Day => Result<PhaseState>.Success(PhaseState.Night),
        PhaseState.Night => Result<PhaseState>.Success(PhaseState.Settlement),
        PhaseState.Settlement => Result<PhaseState>.Success(PhaseState.Day),
        _ => Result<PhaseState>.Failure("LK_PHASE_001", "Invalid phase", current.ToString())
    };
}
```

### Data Patterns

**Access Pattern:** Repository + Adapter Facade

**Rules:**
- Core accesses domain data via repository contracts only
- Adapters implement persistence details (SQLite/ConfigFile/Cloud sync)
- Scene/UI layers never bypass repository contracts

**Example (C#):**
```csharp
public interface IRunSnapshotRepository
{
    Result<RunSnapshot> Load(string slotId);
    Result<bool> Save(string slotId, RunSnapshot snapshot);
}
```

### Consistency Rules

| Pattern | Convention | Enforcement |
| --- | --- | --- |
| Communication | Typed events + DI | contract validation + lint checks |
| Entity creation | Factory + pooling on hot paths | perf smoke + spawn path review |
| State transitions | Explicit state machine only | unit tests for legal/illegal transitions |
| Data access | Repository + adapter facade | boundary checks + architecture review |
| Novel pattern usage | Apply only in declared trigger scenarios | implementation checklist + tests |

### Pattern Consistency Validation Addendum (Round 1)

#### PV-01 Conflict Priority Matrix

| Conflict Pair | Priority Rule | Rationale |
| --- | --- | --- |
| Typed Event Bus vs synchronous critical path | Critical path keeps direct deterministic call; event bus for notifications only | Prevents latency/ordering ambiguity in phase-critical logic |
| Object Pooling vs low-frequency entities | Pooling is mandatory only for high-churn entities; low-frequency entities use direct creation | Avoids premature complexity and pool lifecycle bugs |
| State Machine vs ad-hoc flags | Explicit state machine always wins; flags are derived metadata only | Prevents hidden transition logic and illegal states |
| Repository contracts vs direct adapter access | Repository boundary always wins in Core/UI; adapter direct calls forbidden outside adapter layer | Preserves testability and boundary purity |
| Novel pattern flow vs generic fallback logic | Novel pattern contract has precedence in declared trigger scenarios | Avoids accidental bypass of unique gameplay rules |

#### PV-02 Trigger Boundaries (Must Be Explicit)

- **Path-Blocked Combat Reconciliation Pattern**
  - Trigger: path-to-castle invalid
  - Forbidden: direct random wall target selection
- **Multi-Channel Wave Budget Composition Pattern**
  - Trigger: every night composition cycle
  - Forbidden: merged budget shortcut without channel tags
- **Exhaustive Reward State Transition Pattern**
  - Trigger: every post-night reward generation
  - Forbidden: UI-layer ad-hoc fallback decisions

#### PV-03 Non-Bypass Rules

1. No implementation may skip the reward state machine and directly compose UI choices.
2. No enemy controller may bypass path-fail policy and attack arbitrary structures.
3. No wave generator may emit spawn plans without channel provenance (`normal`, `elite`, `boss`).
4. No subsystem may mutate phase state outside scheduler ownership.

#### PV-04 Verification Hooks

| Pattern | Required Test Shape | Failure Signal |
| --- | --- | --- |
| Path-Blocked Reconciliation | blocked-path deterministic behavior tests | inconsistent blocker target or retarget storm |
| Multi-Channel Budget | day-type channel composition tests | missing/incorrect channel contribution |
| Reward State Transition | exhausted-catalog fallback tests | invalid or empty reward choice set |
| State Machine | illegal transition tests | silent transition acceptance |
| Pooling | churn/perf smoke tests | allocation spikes or stale instance leakage |

#### PV-05 Documentation Precision Rule

- Every pattern must specify:
  1. owner component
  2. trigger condition
  3. forbidden shortcuts
  4. validation evidence
- Missing any of the four makes the pattern incomplete.


## Architecture Validation

### Validation Summary

| Check | Result | Notes |
| --- | --- | --- |
| Decision Compatibility | PASS | Engine, decisions, structure, and pattern rules are mutually compatible. |
| GDD Coverage | PASS | Core gameplay systems and technical constraints are architecture-mapped. |
| Pattern Completeness | PASS | Standard and novel patterns include examples, triggers, and enforcement hooks. |
| Epic/Feature Mapping | PASS | Critical systems (D1-D6) are mapped to concrete locations and responsibilities. |
| Document Completeness | PASS | Mandatory sections present and placeholder residue removed. |

### Coverage Report

**Systems Covered:** 8/8  
**Patterns Defined:** 7  
**Decisions Made:** 6 (D1-D6) + 5 (X1-X5) + 4 (P1-P4)

### Issues Resolved

- Removed structure placeholder `<Module>` and replaced with concrete contract domains (`Combat`, `Economy`, `Progression`).
- Revalidated no unresolved placeholder markers (`{{...}}`, `TODO`, `<Module>`).
- Confirmed conflict-priority rules and trigger boundaries for implementation patterns.

### Validation Date

2026-02-09


## Development Environment

### Prerequisites

- Windows 10/11 (x64)
- Godot 4.5.1 (Windows .NET build)
- .NET 8 SDK
- Python 3 (use `py -3`)
- Steamworks SDK access (Partner Portal latest) for cloud/achievement integration

### Setup Commands (Windows)

```bash
dotnet restore lastking.sln
dotnet build lastking.sln -warnaserror
py -3 scripts/python/quality_gates.py --typecheck --lint --unit --dup --scene --security --perf
```

### First Steps

1. Open project in Godot 4.5.1 .NET editor and validate scene/script import health.
2. Run .NET unit tests and Godot headless smoke/security/perf checks via quality gates.
3. Start epic-driven implementation with architecture boundaries and pattern rules as mandatory constraints.

