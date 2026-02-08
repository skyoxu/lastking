# lastking 文档索引

> 项目名：`lastking`  
> 技术口径：Godot 4.5.x + C#/.NET 8（Windows-only）

## 入口文档

- 项目总览：`README.md`
- 协作规范：`AGENTS.md`
- 架构与门禁口径：`CLAUDE.md`

## 快速开始

- 上手指南：`docs/TEMPLATE_GODOT_GETTING_STARTED.md`
- 测试框架：`docs/testing-framework.md`
- 手动发布：`docs/release/WINDOWS_MANUAL_RELEASE.md`

## 架构与 ADR

- Base 架构总览：`docs/architecture/base/00-README.md`
- ADR 索引：`docs/architecture/ADR_INDEX_GODOT.md`
- ADR 目录：`docs/adr/`

## 迁移资料（历史对照）

- 迁移索引：`docs/migration/MIGRATION_INDEX.md`
- 迁移阶段文档：`docs/migration/`

说明：`docs/migration/**` 用于历史迁移与对照，不作为当前运行时代码的唯一事实来源。

## 工作流文档

- 可玩度与发布流程：`docs/workflows/GM-NG-T2-playable-guide.md`
- 文档收敛流程：`docs/workflows/doc-stack-convergence-guide.md`
- SuperClaude 指令参考：`docs/workflows/superclaude-command-reference.md`
- TaskMaster 集成：`docs/workflows/task-master-superclaude-integration.md`

## 脚本入口

- 质量门禁：`scripts/ci/quality_gate.ps1`
- Base 文档校验：`scripts/ci/verify_base_clean.ps1`
- 文档编码校验：`scripts/python/check_encoding.py`
- 旧术语扫描：`scripts/python/scan_doc_stack_terms.py`

## 日志目录

- CI 日志：`logs/ci/`
- 审计日志：`logs/security/`

