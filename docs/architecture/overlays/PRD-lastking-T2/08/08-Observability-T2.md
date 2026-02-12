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

## Release Health Linkage

- CI release-health job is the authoritative gate for Crash-Free enforcement.
- Overlay docs only reference gate ownership and evidence path, not threshold duplication.
- Any gate policy change must be captured by ADR before doc/script change.
