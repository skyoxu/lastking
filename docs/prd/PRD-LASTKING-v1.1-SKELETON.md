# Lastking PRD Skeleton (v1.1 Locked)

## 0. Document Purpose
- This file is the locked input skeleton for the `bmad game design` agent.
- It must be expanded without unnecessary low-level architecture changes.
- Current implementation baseline: Godot + C#, layered as Scenes -> Adapters -> Core.

## 1. Product Scope
- Project name: `Lastking`
- Platform: Steam single-player, Windows only
- Engine/Language: Godot 4 + C#
- Online/Mobile: not in scope for v1
- Reference direction:
  - UI/interaction style inspired by `王之凝视`
  - System pressure inspired by `They Are Billions`

## 2. Core Experience
- Genre: high-pressure tower defense + macro economy/build/recruit loop
- Match duration target: 60-90 minutes
- Objective: defend the central castle until day 15 ends
- Loss condition (only): castle HP reaches 0
- Win condition (v1): survive all 15 days

## 3. Match Timing
- Day phase: 4 minutes
- Night phase: 2 minutes
- One night = one wave
- Special nights:
  - Day 5: elite night
  - Day 10: elite night
  - Day 15: boss night

## 4. Camera and Map
- Fixed vertical field
- Horizontal width around 3 screens at 1024x768 baseline
- Scrolling: edge scrolling + keyboard scrolling
- Main attack pressure from left/right lanes toward center castle
- Map generation: one random template for v1

### 4.1 Obstacle Rules (Locked)
- Bad-map reroll is NOT an in-match action; player restarts a new run instead
- Obstacle distribution:
  - 80% in the middle one-screen region
  - 20% in outer two-screen regions
- Obstacles are indestructible for both sides
- Base difficulty total obstacle footprint: 100 building-area units

## 5. Build/Grid Rules
- Grid is hidden visually, but building placement uses grid occupancy
- Area units:
  - Castle: 4
  - Wall: 1
  - MG Tower: 1
  - Barracks: 2
- Wall supports drag-line building
- Mine/trap is one-time consumable
- Path blocking is allowed: defensive structures may fully block routes
- Gate rule: one-way gate, friendly units can exit, enemies cannot enter

## 6. Economy and Resources
- Resource types: Gold, Wood, Population, Tech Points
- Core bottleneck resource: Gold
- Starting resources:
  - Gold: 800
  - Wood: 150
  - Population cap: 50
- Tech points:
  - Start with 1
  - +1 per day (plus external modifiers)
- Housing income: +50 Gold per 15s, +10 Population cap per house

## 7. Combat Roster (Initial)

### 7.1 Player Units
- Gunner:
  - Cost: 100 Gold
  - Population: 1
  - Train time: 10s
  - HP: 50
  - Role: single-target, medium range, high fire rate, normal move speed
- Tank:
  - Cost: 500 Gold
  - Population: 3
  - Train time: 20s
  - HP: 200
  - Role: AoE around target point, long range, low fire rate, very slow move speed
- No melee unit class in v1

### 7.2 Defensive Buildings
- MG Tower: 200 Gold, 500 HP
- Wall (Lv1): 20 Gold, 500 HP
- Mine: 50 Gold (single-use)

### 7.3 Building Upgrade
- Upgrade cap: level 5
- First upgrade cost/time:
  - Castle: 1000 / 2 min
  - Barracks: 600 / 1 min
  - MG Tower: 400 / 0.5 min
  - Housing: 500 / 1 min

## 8. Enemy System and Wave Economy
- Base night budget day1: 50
- Budget growth: 120% daily (multiplicative)
- Elite and Boss budgets are computed independently (not part of normal conversion weight)
- Enemy archetypes:
  - Base
  - Elite
  - Boss
  - Subtypes: Fast, Armored, Ranged, Self-destruct

### 8.1 Path-Blocked Behavior (Locked)
- If path is fully blocked, enemies prioritize attacking the nearest blocking structure

### 8.2 Boss Night Rule (Locked)
- Boss count on day15: fixed at 2
- Boss count does NOT scale with difficulty level

## 9. Boss Mechanics (Locked)
- Mechanic A: every 20s, invulnerable for 5s
- Mechanic B: every 20s, summon 2 clones
- Clone stats and rules:
  - 50% of boss attack
  - 50% of boss HP
  - Permanent duration
  - Maximum 10 active clones on map
  - Can be slowed

## 10. Control and Friendly Fire
- Auto-combat first
- Macro command allowed: rally point setting
- Micro control not allowed
- Skills auto-cast
- No friendly fire for all player attacks

## 11. Reward System (3-Choice)
- Trigger frequency: exactly once after each night settlement
- Normal/Elite/Boss nights use different reward pools
- Reward pool entries:
  - One relic
  - One advanced unit
  - +3 tech points
  - +600 Gold
- Reward properties:
  - Permanent for the current run
  - Non-repeatable
  - Non-stackable
- Fallback rule: if pool options are exhausted, reward choices become Gold-only
- Advanced unit pool size in v1: 2 distinct units (new units, not upgrades)

## 12. Tech Tree Constraints
- Tech tree affects unit numeric stats only (no new mechanics unlocked here)
- Mechanic unlocks are mainly through reward system
- Initial stat dimensions to design:
  - Attack Speed (% with cap)
  - Damage (flat, no cap)
  - Production Speed (% with cap)
  - Range (flat, no cap)
  - HP (flat, no cap)
  - Build Cost (% with cap)

## 13. Difficulty and Castle
- Difficulty levels: 5 (numeric, cumulative effect)
- Castle initial HP: 1000/1000
- Castle durability does not increase by default unless explicitly defined later

## 14. Save and Steam Scope
- Steam launch scope in v1: Cloud Save + Achievements only
- Save slots:
  - 1 auto-save slot (auto-save at each day start)
  - 3 manual save slots
- Save policy:
  - Auto-save slot cannot be overwritten by manual saves
  - No rotation overwrite strategy
- Auto-save data must include:
  - Random seed
  - Wave timer state
  - Building queue state

## 15. Achievement Requirement
- Total achievements: 20
- Hidden achievements: none
- `game design` output must include: Name + Condition + Trigger Timing for all 20

## 16. Performance Target and Hardware Budget

### 16.1 Minimum Spec (Recommended for Store Page)
- OS: Windows 10 64-bit
- CPU: Intel i3-6100 / Ryzen 3 1200
- RAM: 8 GB
- GPU: GTX 750 Ti / RX 460 / Intel UHD 630 (DX11)
- Storage: 3 GB available

### 16.2 Recommended Spec
- CPU: i5-10400 / Ryzen 5 3600
- RAM: 16 GB
- GPU: GTX 1650 / RX 570
- Target resolution: 1920x1080

### 16.3 Performance Acceptance
- Average FPS target: 60
- 1% low target: 45 (acceptable)

## 17. Required Enrichment Output (for game design)
- Day1-Day15 full balance table:
  - Budgets
  - Composition logic
  - Spawn pacing
  - Elite/Boss independent budget integration
- Three reward pools with weights and exhaustion-safe logic
- Two advanced unit full designs (role + numbers)
- 5-level difficulty cumulative modifiers
- Unit-only tech tree numeric table with cap rules
- UI layout and interaction refinement spec
- Milestones, acceptance criteria, risk list, and tuning hooks
