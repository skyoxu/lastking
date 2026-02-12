# Lastking Game Design Document (GDD) v1.0

## 1. Document Info
- Project: Lastking
- Version: 1.0
- Date: 2026-02-09
- Platform: Steam (Windows only)
- Engine: Godot 4 + C#

## 2. Vision
Lastking is a high-pressure single-player tower defense game focused on macro decisions: economy, construction, and army composition under escalating night assaults.

## 3. Experience Pillars
1. High pressure with readable rules
2. Macro strategy over micro control
3. Meaningful day-night rhythm
4. Replay value through randomized map pressure

## 4. Core Loop
1. Day phase (4 min): build, upgrade, recruit, set rally points
2. Night phase (2 min): defend one incoming wave
3. Post-night: pick one reward from 3 choices
4. Repeat until day 15 survival or castle destroyed

## 5. Win/Lose Conditions
- Win: survive through day 15 boss night
- Lose: castle HP reaches 0

## 6. Match Structure
- Match duration: 60-90 min
- Total days: 15
- Special nights:
  - Day 5: Elite Night
  - Day 10: Elite Night
  - Day 15: Boss Night

## 7. Economy and Resources
- Resources: Gold, Wood, Population, Tech Points
- Start: Gold 800 / Wood 150 / Pop Cap 50
- Housing output: +50 Gold per 15s and +10 Pop Cap
- Tech points: start 1, +1 per day

## 8. Player Forces
- Gunner: 100 Gold, 1 Pop, 10s, 50 HP, single-target ranged
- Tank: 500 Gold, 3 Pop, 20s, 200 HP, AoE ranged
- No melee units in v1

## 9. Defenses and Building
- MG Tower: 200 Gold, 500 HP
- Wall Lv1: 20 Gold, 500 HP, drag-line building enabled
- Mine: 50 Gold, single-use
- Defensive structures may fully block paths
- One-way gate: friendly exit allowed, enemy entry denied

## 10. Enemy and Budget
- Day1 base budget: 50
- Daily growth: 120% multiplicative
- Elite and Boss budgets calculated independently
- Enemy subtypes: Fast / Armored / Ranged / Self-destruct
- If path blocked, enemies attack nearest blocking structure
- Boss night fixed boss count: 2

## 11. Boss Mechanics (v1)
- Every 20s: invulnerable for 5s
- Every 20s: summon 2 clones
- Clone: 50% attack, 50% HP, permanent, cap 10, can be slowed

## 12. Reward System (3-choice)
- Trigger once after each night
- Different pools for normal/elite/boss nights
- Candidate rewards:
  - One relic
  - One advanced unit
  - +3 tech points
  - +600 Gold
- Effects are run-permanent
- Non-repeatable and non-stackable
- If pool exhausted, fallback to gold-only choices (repeat allowed)

## 13. Difficulty
- 5 levels, cumulative modifiers
- Castle starts at 1000/1000

## 14. Save and Steam Scope
- Steam features in v1: cloud save + achievements only
- Save slots: 1 auto-save + 3 manual
- Auto-save slot cannot be overwritten manually
- Auto-save records: random seed, wave timer, building queue

## 15. Achievements
- Total 20
- No hidden achievements

## 16. Performance Targets
- Average FPS target: 60
- 1% low acceptable: 45
- Minimum spec target:
  - Win10 64-bit
  - i3-6100 / Ryzen 3 1200
  - 8GB RAM
  - GTX 750 Ti / RX 460 / Intel UHD 630
  - 3GB storage

## 17. Design Tasks for Enrichment
- Day1-Day15 full balance table
- Three reward pools with weights and fallback
- Two advanced unit full definitions
- 5-level tech numeric table with cap rules
- 5-level difficulty cumulative table
- 20 achievement definitions (name, condition, trigger)
