---
STORY-DOC-ID: STORY-LASTKING-BATTLEMAPSCREEN-V1
Title: BattleMapScreen Epics And Stories
Status: Draft
Owner: codex
Last Updated: 2026-05-04
Encoding: UTF-8
Based-On:
  - docs/gdd/battlemapscreen-epics.md
  - docs/gdd/battlemapscreen-gd-responsibility-split-draft.md
  - docs/gdd/battlemapscreen-node-tree-draft.md
  - docs/gdd/battlemapscreen-node-migration-map.md
  - docs/gdd/battle-map-screen-ui-spec.zh-CN.md
  - docs/gdd/battle-map-ui-layout-and-slots.zh-CN.md
  - docs/gdd/battle-map-ui-legend-spawn-path-visual-spec.zh-CN.md
  - docs/gdd/battle-map-ui-selection-range-placement-effects-spec.zh-CN.md
  - docs/gdd/combat-feedback-ui-spec.zh-CN.md
  - docs/gdd/combat-feedback-ui-field-mapping-and-pressure-minimum.zh-CN.md
  - docs/gdd/combat-feedback-pressure-state-machine-and-copy.zh-CN.md
  - docs/gdd/outcome-ui-spec.zh-CN.md
---

# BattleMapScreen Epics And Stories

## Epic 1: Stable Battle Screen Frame And Ownership

Deliver a stable BattleMapScreen entry surface that preserves existing HUD ownership, screen navigation, runtime bridge compatibility, localization, and migration-safe scene structure while introducing the new three-band battle frame.

### Story 1.1: Introduce The Stable Battle Screen Frame

As a player,
I want BattleMapScreen to open inside a stable three-band frame,
So that the battle screen feels consistent and ready for production UI.

**Acceptance Criteria:**

- **Given** BattleMapScreen is opened through the existing ScreenNavigator flow
  **When** the scene loads
  **Then** the screen presents a stable top, battlefield, and bottom composition aligned to the 1600 x 900 baseline
  **And** only the battlefield area is eligible for horizontal scrolling on narrow displays.
- **Given** the new frame is introduced
  **When** Main.tscn, HUD.tscn, and ScreenNavigator are active
  **Then** BattleMapScreen does not create a second HUD or duplicate global navigation ownership
  **And** the persistent HUD remains the owner of global top-bar state.

**Covers:** FR1, FR2, FR3, FR48, FR49, FR50, FR51

### Story 1.2: Isolate Legacy Prototype And Runtime Ownership

As a developer,
I want prototype UI and runtime bridge nodes separated into explicit ownership containers,
So that migration can continue without breaking current runtime entry points.

**Acceptance Criteria:**

- **Given** the current BattleMapScreen scene tree contains prototype controls and runtime bridge nodes
  **When** the scene tree is reorganized
  **Then** battlefield presentation layers, runtime bridge nodes, and legacy prototype nodes are separated into explicit roots
  **And** the old prototype subtree is clearly marked as migration-only.
- **Given** existing tests and bridge callers depend on current runtime behavior
  **When** the runtime bridge is moved under a dedicated runtime root
  **Then** CombatExperienceRuntimeBridge and WaveTimer remain callable and behaviorally stable
  **And** bridge methods used by tests continue to resolve correctly.

**Covers:** FR50, FR51, FR52, FR56

### Story 1.3: Reduce BattleMapScreen.gd To A Coordinator

As a developer,
I want BattleMapScreen.gd reduced to scene orchestration responsibilities,
So that new battlefield controllers can be added without growing a monolithic screen script.

**Acceptance Criteria:**

- **Given** BattleMapScreen.gd currently mixes debug controls, rendering, navigation, and summary logic
  **When** the first script split is implemented
  **Then** BattleMapScreen.gd retains scene wiring, mode arbitration, safe bridge access, and screen exit handoff only
  **And** prototype debug flow ownership is extracted into a dedicated temporary controller.
- **Given** existing prototype controls still support current tests
  **When** the extraction is complete
  **Then** the legacy control row still works through delegated behavior
  **And** the root script no longer owns the main prototype button flow directly.

**Covers:** FR53, FR54, FR55, FR56, FR59

### Story 1.4: Preserve Existing Global UI Ownership And Localization

As a player,
I want the battle screen to keep existing HUD-backed global information and localization behavior,
So that screen migration does not regress readability or language support.

**Acceptance Criteria:**

- **Given** the new battle screen frame is active
  **When** the player views top-level run state
  **Then** resources, time, speed state, pressure summary, and other global fields remain owned by the existing HUD pipeline
  **And** BattleMapScreen does not reimplement the same event parsing locally.
- **Given** the locale changes between zh-CN and en-US
  **When** the battle screen is visible
  **Then** battle UI text remains localizable in both languages
  **And** temporary local scene text does not drift away from the existing localization model.

**Covers:** FR4, FR5, FR30, FR31, FR32, FR48, FR59, FR60

## Epic 2: Battlefield Planning And Spatial Readability

Deliver a battlefield that players can read and plan around through clear slot regions, wall boundaries, spawn-side signaling, placement legality, building selection, clipped ranges, and linked deployed-unit feedback.

### Story 2.1: Build The Battlefield Region And Slot Layout

As a player,
I want the battlefield regions and slot grid to be visually structured,
So that I can understand where building and defense planning is allowed.

**Acceptance Criteria:**

- **Given** BattleMapScreen is visible
  **When** the battlefield is rendered
  **Then** the map reads as left wall, inner castle region, right wall, and right outer field
  **And** the battlefield uses a strict 48 x 48 slot grid across the valid buildable regions.
- **Given** walls are part of the battlefield
  **When** region layout is shown
  **Then** walls are visibly permanent and non-buildable
  **And** no buildable slot crosses a wall boundary.

**Covers:** FR6, FR7, FR8, FR9, FR10, FR11

### Story 2.2: Implement Placement Legality Overlays

As a player,
I want placement mode to show where I can and cannot build,
So that I can plan quickly without guesswork.

**Acceptance Criteria:**

- **Given** the player enters placement mode from a build command
  **When** placement overlays appear
  **Then** valid cells use region-aware highlighting with warm inner-zone and cool outer-zone color logic
  **And** only valid slot regions receive overlay coverage.
- **Given** a target cell is permanently invalid
  **When** it is evaluated in placement mode
  **Then** it appears as a greyed cell with lock or cross marking.
- **Given** a target cell is temporarily invalid because of unit or building occupancy
  **When** it is evaluated in placement mode
  **Then** it appears as a red single-cell frame
  **And** no reason text is required.
- **Given** walls are present during placement
  **When** placement overlays are active
  **Then** walls display as stable red blockers
  **And** they do not pulse.

**Covers:** FR10, FR11, FR12, FR21, FR22, FR23, FR24, FR25, FR26, FR27

### Story 2.3: Implement Building Selection And Clipped Range Feedback

As a player,
I want building selection to show purpose and reach,
So that I can understand what a placed building currently controls.

**Acceptance Criteria:**

- **Given** the player selects a building
  **When** the selected object is an economy building
  **Then** the building shows the shared outline color and a light base highlight
  **And** no range overlay is displayed.
- **Given** the player selects a defense building
  **When** the selection is active
  **Then** the building shows the shared outline color and a clipped warm range overlay.
- **Given** the player selects a unit-producing building
  **When** the selection is active
  **Then** the building shows the shared outline color and a clipped cool range overlay
  **And** only already-deployed units linked to that selected building are outlined.
- **Given** placement mode is entered while a building is selected
  **When** the mode changes
  **Then** the active building selection is cleared
  **And** placement mode takes priority over selection mode.

**Covers:** FR16, FR17, FR18, FR19, FR20, FR21

### Story 2.4: Implement Spawn-Side And Path Readability Cues

As a player,
I want to read where pressure comes from and where enemies are trying to go,
So that I can understand threat direction without explicit path arrows.

**Acceptance Criteria:**

- **Given** the battlefield is in normal play
  **When** the player looks at the outer edges
  **Then** the right spawn band shows weak red glow
  **And** the glow identifies the enemy entry sides without a separate legend box.
- **Given** a wave starts
  **When** spawn emphasis is triggered
  **Then** the spawn-side glow pulses strongly for 3 to 5 seconds
  **And** then returns to weak persistent glow.
- **Given** enemies move across the battlefield
  **When** path readability is evaluated
  **Then** intent is read from enemy movement and retargeting behavior
  **And** no explicit arrow or route-line UI is required.

**Covers:** FR13, FR14, FR15

## Epic 3: Combat Awareness And Pressure Feedback

Deliver high-signal combat readability so players can understand battle state, local urgency, pressure escalation, counts, skill readiness, and HUD-supported after-action guidance without relying on prototype summary text.

### Story 3.1: Implement Battlefield-Local Combat Feedback Surfaces

As a player,
I want immediate battlefield-local feedback during combat,
So that I can react to hits, pressure spikes, and key events in real time.

**Acceptance Criteria:**

- **Given** combat is active
  **When** attacks, hits, or pressure events occur
  **Then** the battlefield can show damage numbers, hit flashes, wall-under-attack emphasis, and short prompt messages
  **And** these feedback elements appear in the battlefield-local feedback layer rather than inside permanent summary text.
- **Given** damage-number display is disabled in settings
  **When** combat continues
  **Then** damage numbers are suppressed
  **And** the remaining combat feedback still functions.

**Covers:** FR28, FR29

### Story 3.2: Implement Pressure State Mapping With Existing Inputs

As a player,
I want battle pressure to be summarized clearly,
So that I can sense escalation before the run collapses.

**Acceptance Criteria:**

- **Given** only existing runtime inputs are available
  **When** pressure state is computed for UI
  **Then** the pressure model uses existing wave and castle-HP related inputs first
  **And** placeholder or inferred logic is allowed where no formal dedicated pressure contract exists.
- **Given** pressure changes over time
  **When** the HUD summary and battlefield prompts update
  **Then** the UI supports Stable, Warning, Danger, and Critical states
  **And** the top bar acts as the persistent summary while battlefield prompts act as the primary urgent callout.

**Covers:** FR33, FR34, FR35, FR36, FR37

### Story 3.3: Implement Bottom-Bar Combat State And Action Readability

As a player,
I want the bottom operation bar to show actionable counts and readiness states,
So that I can understand army capacity and available actions at a glance.

**Acceptance Criteria:**

- **Given** the bottom operation bar is visible
  **When** counts are displayed
  **Then** battle-relevant counts use current/max formatting
  **And** morale is shown as a numeric placeholder if no stable runtime source exists yet.
- **Given** spells and skills are shown in the bottom bar
  **When** an action is available, unavailable, or cooling down
  **Then** the icon state reflects that status through full visibility, 50 percent opacity, or a radial cooldown mask.

**Covers:** FR30, FR31, FR32, FR33

### Story 3.4: Replace Prototype Summary Text With Production Feedback Ownership

As a player,
I want combat readability to come from the HUD and battlefield layers rather than prototype labels,
So that the battle screen communicates state in a production-ready way.

**Acceptance Criteria:**

- **Given** the battle screen has production feedback layers available
  **When** combat state changes
  **Then** pressure, outcome guidance, and local battle prompts are owned by their intended HUD and battlefield controllers
  **And** the prototype Summary, Legend, and MetricsHelp labels are no longer required for the final reading model.
- **Given** the combat loop proceeds from build through after-action guidance
  **When** the player observes the battle flow
  **Then** the combined HUD and battlefield feedback remains sufficient to understand progress without relying on the legacy text stack.

**Covers:** FR34, FR57, FR58

## Epic 4: Outcome Resolution And Run Transition

Deliver daily settlement, victory, and defeat outcome flows that clearly communicate result, reason, evidence, reward context, and the allowed next actions for continuing or ending the run.

### Story 4.1: Implement Daily Settlement Outcome Modal

As a player,
I want a clear daily settlement modal after a completed night,
So that I can review the phase result and choose the next reward path.

**Acceptance Criteria:**

- **Given** a night ends and the run is not terminal
  **When** daily settlement is triggered
  **Then** a centered battlefield modal appears and pauses time while it is active
  **And** the modal communicates that the run will continue after resolution.
- **Given** daily settlement is active
  **When** the modal is shown
  **Then** it exposes a three-choice reward entry
  **And** the view can include the relevant subset of HP, reward summary, kill count, or resource snapshot.

**Covers:** FR38, FR41, FR42, FR46

### Story 4.2: Implement Victory Outcome Modal

As a player,
I want a clear victory resolution after defeating the final boss,
So that I understand the run is complete and what actions are now allowed.

**Acceptance Criteria:**

- **Given** the final boss is defeated
  **When** victory outcome is triggered
  **Then** a centered battlefield victory modal appears and pauses time while active
  **And** the modal communicates that live battle cannot be resumed.
- **Given** the victory modal is active
  **When** actions are presented
  **Then** the allowed actions are Return to Main Menu and Restart
  **And** there is no Continue Battle option.

**Covers:** FR38, FR39, FR41, FR43, FR44

### Story 4.3: Implement Defeat Outcome Modal

As a player,
I want a clear defeat resolution when the wall falls,
So that I understand why the run ended and what I can do next.

**Acceptance Criteria:**

- **Given** wall or castle HP reaches zero or below
  **When** defeat outcome is triggered
  **Then** a centered battlefield defeat modal appears and pauses time while active
  **And** the modal communicates that live battle cannot be resumed.
- **Given** the defeat modal is active
  **When** defeat messaging is rendered
  **Then** the modal explicitly states that the wall was breached
  **And** the allowed actions are Return to Main Menu and Restart only.

**Covers:** FR38, FR40, FR41, FR43, FR44, FR45

### Story 4.4: Implement Outcome Evidence And Transition Wiring

As a player,
I want outcome views to include trustworthy evidence and correct next-step transitions,
So that the result screen is both informative and operationally clear.

**Acceptance Criteria:**

- **Given** any outcome modal is shown
  **When** summary content is composed
  **Then** the view includes the appropriate subset of final HP, kill count, reward result summary, and resource snapshot
  **And** an expandable evidence panel can surface runtime evidence context.
- **Given** an outcome modal is resolved
  **When** the system performs the next transition
  **Then** daily settlement continues the run automatically after resolution
  **And** victory or defeat transitions follow the allowed terminal actions without re-entering live battle.

**Covers:** FR41, FR42, FR43, FR46, FR47
