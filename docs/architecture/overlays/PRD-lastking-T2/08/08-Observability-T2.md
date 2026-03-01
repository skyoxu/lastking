---
PRD-ID: PRD-lastking-T2
Title: 可观测性索引 — Lastking T2
Status: Locked
ADR-Refs:
  - ADR-0003
  - ADR-0015
Test-Refs:
  - logs/ci/<YYYY-MM-DD>/release-health.json
  - logs/perf/<YYYY-MM-DD>/summary.json
---

本页锁定执行证据产物命名和失败处理流程，避免日志不可追溯。

## Artifact Naming

- Unit evidence: `logs/unit/<YYYY-MM-DD>/coverage.json`
- E2E evidence: `logs/e2e/<YYYY-MM-DD>/.../junit.xml`
- CI evidence: `logs/ci/<YYYY-MM-DD>/task-triplet-audit/report.json`
- Overlay lint evidence: `logs/ci/<YYYY-MM-DD>/overlay-lint/report.json`
- Perf evidence: `logs/perf/<YYYY-MM-DD>/summary.json`

## Mandatory JSON Fields

- Security audit JSONL lines must include: `ts`, `action`, `reason`, `target`, `caller`.
- Perf summary JSON must include: `p95`, `p50`, `samples`, `scene`, `gate`.
- Release health JSON must include: `window`, `threshold`, `value`, `status`.

## Gate Failure Handling

- Overlay lint fails: block merge immediately and require docs/test refs fix.
- Task triplet mapping fails: block merge and require overlay/task link repair.
- Release health fails: block release promotion; keep deployment artifacts for rollback.
- Perf gate fails: block hard gate path or downgrade to soft gate only via explicit env flag.

## Task Evidence Matrix (P0)

备注：`T22/T23/T28/T29` 的证据字段必须可机读，禁止仅截图作为唯一证据。

| Task Group | Required Artifact | Minimum Fields |
| --- | --- | --- |
| `T21` Windows export/runtime | `logs/ci/<YYYY-MM-DD>/export.log` | `platform`, `profile`, `status`, `duration_ms` |
| `T22` camera scroll | `logs/e2e/<YYYY-MM-DD>/runtime-ui/summary.json` | `camera_mode`, `edge_threshold_px`, `keyboard_vector`, `clamped` |
| `T23` speed modes | `logs/e2e/<YYYY-MM-DD>/runtime-ui/summary.json` | `speed_mode`, `timers_frozen`, `resume_tick`, `status` |
| `T24` runtime feedback | `logs/e2e/<YYYY-MM-DD>/runtime-ui/summary.json` | `error_code`, `message_key`, `displayed`, `duration_ms` |
| `T25` save + migration | `logs/ci/<YYYY-MM-DD>/save-migration/report.json` | `save_version`, `migration_path`, `result`, `error_code` |
| `T26` steam cloud binding | `logs/ci/<YYYY-MM-DD>/steam-cloud/report.json` | `account_id_hash`, `sync_direction`, `conflict_policy`, `result` |
| `T27` achievements | `logs/ci/<YYYY-MM-DD>/achievements/report.json` | `achievement_id`, `trigger`, `deterministic_key`, `result` |
| `T28` i18n switch | `logs/e2e/<YYYY-MM-DD>/settings/summary.json` | `language_from`, `language_to`, `applied`, `persisted` |
| `T29` audio settings | `logs/e2e/<YYYY-MM-DD>/settings/summary.json` | `channel`, `value`, `applied`, `persisted` |
| `T30` performance | `logs/perf/<YYYY-MM-DD>/summary.json` | `avg_fps`, `fps_1pct_low`, `samples`, `gate` |
| `T31-T40` config governance | `logs/ci/<YYYY-MM-DD>/config-governance/report.json` | `config_hash`, `schema_version`, `fallback_used`, `status` |

## Manifest Drift Guard

- 任务回链路径由 `overlay-manifest.json` 统一声明，变更页面文件名时必须先更新 manifest。
- 运行 `py -3 scripts/python/sync_task_overlay_refs.py --dry-run --prd-id PRD-lastking-T2` 作为预检，防止路径漂移。

## Conflict and Rejection Evidence

- 云存档冲突必须有单独证据文件：`logs/ci/<YYYY-MM-DD>/steam-cloud/conflict-report.json`。
- 迁移拒绝必须给出稳定错误码，并写入：`logs/ci/<YYYY-MM-DD>/save-migration/reject-report.json`。
- 配置回退必须记录触发原因与生效默认值快照，避免“静默自愈”不可追溯。

## Release Health Linkage

- CI release-health job is the authoritative gate for Crash-Free enforcement.
- Overlay docs only reference gate ownership and evidence path, not threshold duplication.
- Any gate policy change must be captured by ADR before doc/script change.
