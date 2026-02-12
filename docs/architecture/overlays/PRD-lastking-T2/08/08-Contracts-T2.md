---
PRD-ID: PRD-lastking-T2
Title: 契约索引 — Lastking T2
Status: Locked
ADR-Refs:
  - ADR-0020
  - ADR-0023
  - ADR-0031
  - ADR-0032
  - ADR-0033
Test-Refs:
  - Game.Core.Tests/Domain/GameConfigTests.cs
---

本页用于锁定 T2 契约层执行规则，防止“配置漂移导致玩法行为隐式变化”。

## Contract Inventory

| Contract Path | Responsibility | Versioned | Runtime Required |
| --- | --- | --- | --- |
| `Game.Core/Contracts/Config/enemy-config.schema.json` | enemy archetypes and stats | yes | yes |
| `Game.Core/Contracts/Config/difficulty-config.schema.json` | difficulty tier and lock policy | yes | yes |
| `Game.Core/Contracts/Config/spawn-config.schema.json` | wave cadence and channel composition | yes | yes |
| `Game.Core/Contracts/Config/pressure-normalization.config.schema.json` | pressure baseline constants | yes | yes |
| `Game.Core/Contracts/Config/config-change-audit.schema.json` | audit event payload schema | yes | optional |

## Field Constraints

- 所有经济与战斗核心数值统一为整数语义，不允许小数输入。
- 所有百分比字段必须定义上限，且上限通过 schema 显式约束。
- 所有枚举字段（night type, enemy role, reward kind）必须包含未知值拒绝策略。
- 所有时序字段必须以毫秒或秒为单位，禁止混合单位。

## Versioning and Migration

- 契约版本采用 `major.minor.patch`，并与存档元数据绑定。
- `major` 变化触发强制迁移，失败时拒绝载入并保留原存档。
- `minor` 变化允许前向兼容，必须提供字段默认值策略。
- `patch` 变化仅允许非行为性修订（注释、描述、校验补充）。

## Breaking Change Policy

- 破坏性变更必须先更新 ADR（Accepted 或 Supersede）再更新 schema。
- 破坏性变更必须新增至少一条迁移测试与一条回退测试。
- 破坏性变更必须在 `logs/ci/<YYYY-MM-DD>/` 生成独立审计摘要。

## Local Validation

- `py -3 scripts/python/validate_overlay_execution.py --prd-id PRD-lastking-T2`
- `py -3 scripts/python/validate_contracts.py`
