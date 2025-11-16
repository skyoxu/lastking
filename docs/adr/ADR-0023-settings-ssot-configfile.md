 # ADR-0023: Settings SSoT = ConfigFile (user://)

 - Status: Accepted
 - Context: 现状中 SettingsPanel 将设置写入 SQLite（DB），而模板基线与多处文档强调“轻量设置”应以 ConfigFile（user://）为主（见 CH05/CH06 与安全基线）。这造成“设置的单一事实来源（SSoT）”口径不一致；测试已分别覆盖 DB 与 ConfigFile 场景，但缺少统一口径与迁移路径。
 - Decision: 规范层确定“Settings 的 SSoT 为 ConfigFile（user://）”；DB 保留用于领域/存档等业务数据。短期内不改实现，仅文档收敛并补测试占位；后续 Phase 7.3 计划将 SettingsPanel 回迁至 ConfigFile，必要时提供双写/迁移脚本。
 - Consequences:
   - Settings 读取/写入的规范口径：ConfigFile 是权威来源；DB 不再作为 SSoT 被引用于规范或示例。
   - 测试与 CI：保留 DB 路径的兼容用例（软门禁），新增 ConfigFile UTF‑8 回读一致性与无 DB 降级用例；在迁移完成前，不要求移除 DB 相关测试。
   - 安全与路径：继续遵循仅 user:// 读写与路径规范化（见 ADR‑0002）。
   - 迁移计划（Phase 7.3，单独 PR）：
     1) 将 SettingsPanel 的持久化实现切换为 ConfigFile；
     2) 若需要保留 DB 中历史数据，提供一次性迁移（DB→ConfigFile），并在首次运行时执行；
     3) 更新/精简测试：把“口径占位”用例改为强断言；
     4) 文档将本 ADR 由 Proposed 调整为 Accepted；如对 ADR‑0006 的“数据存储分工”有冲突，则以本 ADR Supersede 相关段落。
 - Supersedes: （暂不生效）在该 ADR 状态转为 Accepted 且完成迁移时，视情况 Supersede ADR‑0006 中“设置持久化使用 DB”的相关描述（如有）。
 - References: ADR‑0002（安全基线）、ADR‑0005（质量门禁）、ADR‑0006（数据存储）、ADR‑0020（测试策略）；Tests.Godot 中 ConfigFile/Locale/无 DB 降级用例；Base CH05/CH06。
