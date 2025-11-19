---
ADR-ID: ADR-0006
title: SQLite 数据存储策略（WAL 模式与备份回退）
status: Accepted
decision-time: '2025-08-17'
deciders: [架构团队, 数据团队]
archRefs: [CH05, CH06, CH11]
verification:
  - path: src/shared/storage/sqlite/init.ts
    assert: 启用 journal_mode=WAL 与 wal_autocheckpoint
  - path: scripts/db/wal-checkpoint.mjs
    assert: WAL 超阈值或定期触发检查点
  - path: scripts/db/backup.mjs
    assert: 在具备能力时执行 VACUUM INTO/Online Backup；否则稳定文件拷贝回退
  - path: tests/unit/db/migrations.spec.ts
    assert: 迁移可正确应用与回滚
impact-scope:
  - src/shared/db/
  - electron/db/
  - scripts/db-*.mjs
tech-tags: [sqlite, wal, database, performance, storage]
depends-on: []
depended-by: []
test-coverage: tests/unit/adr-0006.spec.ts
monitoring-metrics: [implementation_coverage, compliance_rate]
executable-deliverables:
  - src/main/db/init.ts
  - scripts/db-checkpoint.mjs
  - tests/unit/db/sqlite-wal.spec.ts
supersedes: []
---

# ADR-0006: 数据存储与持久化策略

## Context

本地持久化需在性能、可靠性与开发复杂度之间取得平衡。SQLite 具备 ACID 与较好性能，WAL 模式在高并发读场景更优。Windows-only 环境下需考虑原生驱动缺失时的稳定回退与一致的 CLI 语义。

## Decision

- SQLite + WAL：
  - 启用 `journal_mode=WAL`、`synchronous=NORMAL`、`wal_autocheckpoint` 等 Pragmas；
  - 结合周期性维护（检查点/统计/索引维护）。
- 备份策略（脚本与 CLI）：
  - Node 版：`scripts/db/backup.mjs --mode=backup|vacuum|auto`。
    - 优先使用 Online Backup 或 `VACUUM INTO`；当缺少原生能力时回退为“文件拷贝”。
    - `--allow-missing-driver`：缺驱动时返回 `{ ok: true, skipped: true }` 且 exit 0（仅工具层兜底，功能/E2E 不建议依赖）。
  - CLI 版：`scripts/db/backup-cli.mjs`（基于 sqlite3.exe，存在时优先）。
- 检查点：`scripts/db/wal-checkpoint.mjs` 遵循阈值/计划触发，确保 WAL 控制与空间回收。
- 日志统一：所有操作日志写入 `logs/YYYYMMDD/{unit|guard}/`，便于排查与审计。

## Consequences

- 正向：在具备原生能力时使用最快路径；缺能力时可稳定回退，保证契约与持续集成稳定。
- 代价：部分高级能力需按环境探测启用；对大文件/长路径需要额外测试与监控。

## Verification

- 单元测试：覆盖 help/缺库/文件拷贝回退最小用例；可选覆盖 VACUUM/Online Backup 分支（具备依赖时）。
- 工具自检：备份/检查点脚本返回稳定 JSON；回退分支输出字段一致（`ok/mode/method/timestamp`）。

## References

- CH 章节：CH05、CH06、CH11
- 相关 ADR：ADR‑0005（质量门禁）
- 外部文档：SQLite WAL/VACUUM INTO、better‑sqlite3 文档
