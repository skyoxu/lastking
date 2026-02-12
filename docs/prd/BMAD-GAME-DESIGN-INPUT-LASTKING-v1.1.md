# BMAD Game Design Input - Lastking (v1.1 Locked)

Use this prompt as direct input for the `bmad game design` agent.

---

## Role and Goal
You are the game design agent for project `Lastking`.
Generate a production-usable enriched PRD based on locked constraints.
Do NOT change locked constraints unless explicitly marked as "optional recommendation".

## Baseline Architecture Constraints
- Project uses Godot + C# with layered structure: Scenes -> Adapters -> Core.
- Non-essential low-level architecture changes are out of scope.
- Favor extension/optimization over foundational rewrites.

## Locked Product Constraints
- Steam single-player, Windows only.
- No mobile target, no online features.
- Match target: 60-90 minutes.
- Day/Night loop: 4 min day + 2 min night.
- Total days: 15.
- Win: survive day 15.
- Lose: castle HP reaches 0 (only fail condition).

## Locked Map/Camera/Build Constraints
- Fixed vertical view; horizontal map width around 3 screens at 1024x768 baseline.
- Supports edge scrolling and keyboard scrolling.
- Two main lanes from left/right pressure center castle.
- One random template for v1.
- Obstacle distribution: 80% in middle screen, 20% in outer screens.
- Obstacles are indestructible for both sides.
- Base obstacle total footprint: 100 building-area units.
- Hidden grid placement.
- Area units: castle=4, wall=1, MG tower=1, barracks=2.
- Wall supports drag-line building.
- Defensive structures can fully block paths.
- One-way gate: friendly can pass outward, enemy cannot pass inward.
- Bad-map reroll is implemented as starting a new run (not in-match reroll).

## Locked Economy and Unit Constraints
- Resources: Gold, Wood, Population, Tech points.
- Core bottleneck: Gold.
- Start resources: Gold=800, Wood=150, Pop cap=50.
- Tech points: start at 1, +1/day (external modifiers allowed).
- Housing: +50 Gold/15s and +10 Population cap.

### Player Units (initial)
- Gunner: 100 Gold, 1 Pop, 10s train, 50 HP.
- Tank: 500 Gold, 3 Pop, 20s train, 200 HP.
- No melee units in v1.

### Defense (initial)
- MG tower: 200 Gold, 500 HP.
- Wall Lv1: 20 Gold, 500 HP.
- Mine: 50 Gold, one-time use.

### Building upgrade (initial)
- Max level: 5.
- First upgrade cost/time:
  - Castle 1000 / 2 min
  - Barracks 600 / 1 min
  - MG tower 400 / 0.5 min
  - Housing 500 / 1 min

## Locked Enemy and Boss Constraints
- Night base budget day1 = 50.
- Daily budget growth = 120% multiplicative.
- Elite and Boss budgets are independent (not in normal conversion weight).
- Enemy subtype classes required: Fast / Armored / Ranged / Self-destruct.
- If path is fully blocked, enemies attack nearest blocking structure.

### Special nights
- Day 5: elite night.
- Day 10: elite night.
- Day 15: boss night.
- Boss count on day15 is fixed at 2, not scaled by difficulty.

### Boss mechanics (locked)
- Every 20s: invulnerable for 5s.
- Every 20s: summon 2 clones.
- Clone stats: 50% attack, 50% HP.
- Clone lifetime: permanent.
- Clone cap: 10 active.
- Clone can be slowed.

## Locked Control, Reward, and Meta Constraints
- Combat is primarily auto-resolve.
- Macro rally points allowed; no micro control.
- Skills auto-cast.
- No friendly fire from player attacks.

### Reward system (3-choice)
- Trigger once after each night settlement.
- Normal/Elite/Boss nights have different pools.
- Reward pool candidates:
  - one relic
  - one advanced unit
  - +3 tech points
  - +600 Gold
- Reward effects are run-permanent.
- Rewards are non-repeatable and non-stackable.
- If pool is exhausted, fallback to Gold-only choices.
- Gold fallback choices are allowed to repeat.
- Advanced unit pool size in v1: 2 units (new units, not upgrades).

### Tech constraints
- Tech tree affects unit numeric stats only.
- Mechanic unlocks mostly come from rewards.
- Tech dimensions to design:
  - Attack Speed (% with cap)
  - Damage (flat, no cap)
  - Production Speed (% with cap)
  - Range (flat, no cap)
  - HP (flat, no cap)
  - Build Cost (% with cap)

### Save and achievement constraints
- Steam scope in v1: cloud save + achievements only.
- Save slots: 1 auto + 3 manual.
- Auto-save is protected and cannot be overwritten by manual save.
- No rotation overwrite strategy.
- Auto-save records: random seed + wave timer + building queue.
- Achievements: 20 total, none hidden.

## Performance and Hardware Constraints
- Minimum spec target:
  - Win10 64-bit
  - i3-6100 / R3 1200
  - 8GB RAM
  - GTX 750 Ti / RX 460 / Intel UHD 630
  - 3GB storage
- Recommended:
  - i5-10400 / R5 3600
  - 16GB RAM
  - GTX 1650 / RX 570
- Performance acceptance:
  - Avg FPS: 60
  - 1% low: 45 acceptable

---

## Required Output Format
Produce a full PRD v1.1 enriched draft in Chinese with these sections:

1. Vision and design pillars (strictly aligned to locked constraints)
2. Core gameplay loop and player decision model
3. Day/Night economy and pressure model
4. Enemy system design and day1-day15 balance table
5. Elite night and boss night detail design
6. Two advanced unit designs (full numeric sheet)
7. Three reward pools (normal/elite/boss) with weights and fallback logic
8. Tech tree numeric table (5 levels, cap rules explicit)
9. Difficulty level 1-5 cumulative modifier table
10. Building upgrade progression and economy pacing
11. UI/interaction/camera specification
12. Save/load and achievement spec (20 achievements: name + condition + trigger)
13. Performance budgets and validation checklist
14. Milestones, acceptance criteria, risks, and tuning hooks

## Output Rules
- Preserve all locked constraints exactly.
- Optional suggestions must be in a separate "Optional Recommendations" section.
- Avoid introducing online/mobile/live-service assumptions.
