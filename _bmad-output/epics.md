---
stepsCompleted:
  - step-01-validate-prerequisites
inputDocuments:
  - docs/gdd/battle-map-combat-ui-gdd.md
  - docs/gdd/battle-map-screen-ui-spec.zh-CN.md
  - docs/gdd/battle-map-ui-layout-and-slots.zh-CN.md
  - docs/gdd/battle-map-ui-legend-spawn-path-visual-spec.zh-CN.md
  - docs/gdd/battle-map-ui-selection-range-placement-effects-spec.zh-CN.md
  - docs/gdd/battlemapscreen-gd-responsibility-split-draft.md
  - docs/gdd/battlemapscreen-godot-scene-restructure-recommendation.zh-CN.md
  - docs/gdd/battlemapscreen-master-wireframe.zh-CN.md
  - docs/gdd/battlemapscreen-node-migration-map.md
  - docs/gdd/battlemapscreen-node-tree-draft.md
  - docs/gdd/combat-feedback-pressure-state-machine-and-copy.zh-CN.md
  - docs/gdd/combat-feedback-ui-field-mapping-and-pressure-minimum.zh-CN.md
  - docs/gdd/combat-feedback-ui-spec.zh-CN.md
  - docs/gdd/combat-loop-gdd.md
  - docs/gdd/combat-loop-task-candidates.md
  - docs/gdd/outcome-ui-spec.zh-CN.md

---

# Lastking - Epic Breakdown

## Overview

This document provides the complete epic and story breakdown for Lastking, decomposing the requirements from the selected battle, combat feedback, outcome, and BattleMapScreen restructure GDD documents into implementable stories.

## Requirements Inventory

### Functional Requirements

FR1: The system must present BattleMapScreen as a stable three-band battle interface with a top status bar, a center battlefield, and a bottom operation bar.
FR2: The system must keep the battlefield aligned to a 1440 x 600 gameplay canvas within a 1440 x 900 baseline screen composition.
FR3: The system must allow horizontal scrolling only for the battlefield area on narrow displays, while avoiding vertical scrolling in the target layout.
FR4: The system must display global run information in the top bar, including existing project resource names, day and hour readout, day or night state, pause and speed controls, relic entry, wave placeholder, boss placeholder, pressure summary, and settings entry.
FR5: The system must format top-bar time as `Day N | Hh | Day/Night` at the presentation layer.
FR6: The system must render the battlefield as three fixed horizontal regions: left outer field, left wall, inner castle region, right wall, and right outer field.
FR7: The system must enforce a strict 50 x 50 slot grid for battlefield placement instead of free placement.
FR8: The system must treat the two 20px walls as permanent non-buildable and non-crossable boundaries.
FR9: The system must treat attacks against either wall as attacks against castle durability.
FR10: The system must support category-based placement rules where unit-producing buildings and traps are outer-field only, defense buildings are config-driven for inner or outer use, and other buildings are inner-castle only.
FR11: The system must support configuration-owned slot unlock rules, footprint sizes, trap charges, and special pre-placed non-removable structures.
FR12: The system must release a trap slot again after the trap has exhausted all charges.
FR13: The system must show wall boundaries, spawn-side readability, selection feedback, range feedback, and placement legality directly on the battlefield rather than through a permanent RTS legend panel.
FR14: The system must show both spawn sides with weak red glow during normal play and a stronger pulse for 3 to 5 seconds at wave start.
FR15: The system must communicate enemy path intent through enemy movement behavior rather than path arrows or route lines.
FR16: The system must allow building selection but must not allow unit selection.
FR17: The system must use one shared outline color for all selected buildings.
FR18: The system must show economy-building selection with outline plus light base highlight and no range overlay.
FR19: The system must show defense-building selection with outline plus clipped warm range overlay.
FR20: The system must show unit-producing-building selection with outline plus clipped cool range overlay and outlines for only the already-deployed units linked to that selected building.
FR21: The system must make placement mode and selection mode mutually exclusive, and entering placement mode must cancel current selection.
FR22: The system must overlay placement feedback directly on valid slot regions when placement mode is active.
FR23: The system must present inner-zone placement with warm color language and outer-zone placement with cool color language.
FR24: The system must show fixed invalid placement cells as greyed cells with lock or cross markers.
FR25: The system must show temporary invalid placement cells as red single-cell frames without explanatory text.
FR26: The system must treat occupancy by a building, friendly unit, or enemy unit as temporary placement invalidity.
FR27: The system must keep walls visible at all times and render them as stable red blockers during placement mode.
FR28: The system must display battlefield-local instant combat feedback, including damage numbers, hit flashes, wall-under-attack emphasis, wave arrival prompts, boss-pressure prompts, and pressure-state prompts.
FR29: The system must allow damage-number visuals to be toggled in settings.
FR30: The system must present bottom-bar battle counts in current/max format.
FR31: The system must display morale as a numeric placeholder until a formal runtime source exists.
FR32: The system must display spells and skills as icons, with unavailable actions shown at 50 percent opacity and cooldown shown with a radial clock mask style.
FR33: The system must reuse existing contracts and runtime outputs first, and use placeholders where stable runtime fields do not yet exist.
FR34: The system must map existing combat feedback to current reusable events and fields such as resources, day-night identity, castle HP, speed state, reward feedback, tax feedback, and tech feedback.
FR35: The system must support a minimum viable pressure state model using existing runtime inputs only.
FR36: The system must support four named pressure states: Stable, Warning, Danger, and Critical.
FR37: The system must show pressure as a concise persistent summary in the top bar and as short event-driven prompts inside the battlefield.
FR38: The system must support three outcome flows: daily settlement, victory settlement, and defeat settlement.
FR39: The system must trigger victory when the final boss is defeated.
FR40: The system must trigger defeat when wall or castle HP reaches zero or below.
FR41: The system must present outcome UI as a centered battlefield modal that pauses time while active.
FR42: The system must allow daily settlement to continue the run and expose a three-choice reward entry.
FR43: The system must end the run on victory or defeat and must not allow return to live battle from those terminal outcomes.
FR44: The system must show Return to Main Menu and Restart actions for victory and defeat outcomes.
FR45: The system must explicitly state that defeat occurred because the wall was breached.
FR46: The system must show an appropriate subset of final HP, kill count, reward result summary, and resource snapshot in outcome views.
FR47: The system should include an expandable evidence panel inside the outcome modal for runtime evidence context.
FR48: The system must preserve the independent persistent HUD and must not create a second HUD inside BattleMapScreen.
FR49: The system must preserve Main.tscn navigation ownership and ScreenNavigator-based screen switching.
FR50: The system must preserve CombatExperienceRuntimeBridge as an independent runtime and test bridge.
FR51: The system must restructure BattleMapScreen into separated battlefield, overlay, and runtime-bridge ownership layers.
FR52: The system must isolate the current Margin/VBox prototype subtree as a temporary legacy migration container before removal.
FR53: The system must reduce BattleMapScreen.gd to a scene composition and coordination role instead of keeping it as a multi-purpose script.
FR54: The system must move prototype debug flow controls out of BattleMapScreen.gd into temporary legacy or debug controller ownership.
FR55: The system must move battlefield actor visualization, placement overlays, selection overlays, local feedback overlays, and outcome modal ownership into dedicated scene-local controller responsibilities.
FR56: The system must preserve existing bridge methods and test-visible runtime paths during migration until synchronized test updates are ready.
FR57: The system must maintain player-visible combat loop coverage from build, spawn, target, attack, damage, cleanup, reward state, and after-action guidance.
FR58: The system must keep HUD-based pressure, outcome, and next-step guidance readable as the combat loop closes.
FR59: The system must support reuse-first implementation where domain logic stays in Game.Core and Godot integration stays in Game.Godot.
FR60: The system must preserve localization support for both zh-CN and en-US battle UI text.

### NonFunctional Requirements

NFR1: BattleMapScreen UI must preserve compatibility with existing screen navigation and instantiation through ScreenNavigator.
NFR2: BattleMapScreen migration must not duplicate persistent HUD responsibilities already owned by HUD.tscn and HUD.cs.
NFR3: Runtime bridge methods currently used by tests must remain stable throughout migration until test updates are intentionally performed.
NFR4: Scene restructuring should minimize drift between current implementation, tests, and design documentation.
NFR5: The battlefield UI must make placement legality readable without repetitive trial-and-error placement attempts.
NFR6: Battlefield readability should prioritize actor silhouettes, timing clarity, and feedback urgency over decorative motion.
NFR7: Pressure-state messaging should upgrade quickly and downgrade slowly to avoid noisy oscillation.
NFR8: Runtime cleanup must prevent sustained object growth during long combat sessions.
NFR9: Combat feedback must remain concise and high-signal rather than relying on large persistent text blocks.
NFR10: The target layout should avoid vertical scrolling in mainstream display usage.
NFR11: Output localization must support both zh-CN and en-US.
NFR12: The migration sequence should preserve behavior first, move prototype UI second, and delete legacy nodes last.
NFR13: All implementation should follow reuse-first boundaries: contracts in Game.Core, engine adaptation in Game.Godot.
NFR14: Outcome and HUD evidence should remain traceable to runtime events and test evidence.
NFR15: The system should keep prototype compatibility shells explicitly temporary and removable.

### Additional Requirements

- Use docs/gdd battle, combat, and outcome specifications as the authoritative planning source for this epic set.
- Exclude docs/gdd/ui-gdd-flow.md, docs/gdd/t1-t46-m1-wiring-audit.md, and docs/gdd/bmad-epic-task-alignment.md from this planning pass.
- Deduplicate overlapping requirements across battle, combat feedback, outcome, scene restructure, node migration, and responsibility split drafts.
- Treat the current Margin/VBox controls and text stack as legacy migration-only ownership, not final product UI.
- Keep top-bar state, persistent pressure summary, persistent resource summary, progression summary, and persistent outcome summary in HUD.
- Keep screen switching, menu routing, and fade transition ownership in Main.tscn and ScreenNavigator.
- Keep CombatExperienceRuntimeBridge independent from presentation hierarchy and reusable by tests.
- Introduce new battlefield-layer containers before moving existing visual nodes.
- Preserve current test-visible node paths until synchronized test migration is part of the implementation slice.
- Prioritize extracting prototype debug flow ownership from BattleMapScreen.gd before building deeper placement and selection controllers.
- Maintain compatibility with current battle loop closure work from build through after-action guidance.
- Prefer implementation-sized stories directly tied to scene restructure, controller extraction, HUD wiring, and test-safe migration.

### UX Design Requirements

UX-DR1: Implement BattleMapScreen with a stable 1440 x 900 frame split into an 80px top bar, 600px battlefield, and 220px bottom operation bar.
UX-DR2: Implement a battlefield-only horizontal scrolling behavior for narrow displays while keeping the top and bottom surfaces stable.
UX-DR3: Implement the battlefield as a visually readable three-region layout with left outer field, left wall, inner castle region, right wall, and right outer field.
UX-DR4: Implement a strict 50 x 50 visible build-slot grid with visually distinct inner and outer placement zones.
UX-DR5: Implement persistent wall readability and spawn-side weak glow with wave-start pulse behavior.
UX-DR6: Implement battlefield meaning through overlays and in-scene cues rather than a permanent text legend panel.
UX-DR7: Implement selection visuals with one shared building outline color, economy-building base glow, clipped warm defense range, and clipped cool unit-building range.
UX-DR8: Implement linked-unit highlighting only for already-deployed units owned by the selected unit-producing building.
UX-DR9: Implement placement mode with region-aware valid highlighting, grey fixed-invalid cells, red temporary-invalid single-cell frames, and stable red wall blockers.
UX-DR10: Implement mutual exclusion between placement mode and building selection mode.
UX-DR11: Implement battlefield-local instant feedback surfaces for damage numbers, hit flashes, pressure prompts, and wall-under-attack emphasis.
UX-DR12: Implement bottom-bar interaction states for building, production, counts, morale placeholder, spells, and skills with availability and cooldown readability.
UX-DR13: Implement top-bar pressure communication as a concise summary label and battlefield pressure communication as short event-driven prompts.
UX-DR14: Implement daily settlement, victory, and defeat as centered battlefield modals with clear visual tone differences.
UX-DR15: Implement victory and defeat outcomes with terminal actions only, and no continue-battle affordance.
UX-DR16: Implement an expandable evidence panel pattern inside the outcome modal for runtime proof and summary details.
UX-DR17: Implement the target BattleMapScreen node tree with dedicated layers for map base, boundaries, slots, actors, selection, range, combat feedback, and outcome anchor.
UX-DR18: Implement BattleMapScreen.gd as a coordinator rather than the owner of all visual, text, and debug interaction behavior.

### FR Coverage Map

{requirements_coverage_map}

## Epic List

{epics_list}
