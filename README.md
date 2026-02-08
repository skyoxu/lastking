# lastking（Godot + C#）

`lastking` 是一个 Windows-only 的 Godot 4 + C#（.NET 8）项目模板。

## 项目定位

- 目标：提供可直接开发、测试、导出的 Godot + C# 基线工程
- 技术口径：Godot 4.5.x + C#/.NET 8 + xUnit + GdUnit4
- 运行平台：Windows 桌面

## 快速开始

1. 配置 Godot .NET 可执行路径：
   - `setx GODOT_BIN C:\Godot\Godot_v4.5.1-stable_mono_win64.exe`
2. 执行基础检查：
   - `./scripts/test.ps1 -GodotBin "$env:GODOT_BIN"`
3. 执行质量门禁：
   - `./scripts/ci/quality_gate.ps1`
4. 导出 Windows 可执行：
   - `./scripts/ci/export_windows.ps1 -GodotBin "$env:GODOT_BIN" -Output build\lastking.exe`

## 常用入口

- 文档总索引：`docs/PROJECT_DOCUMENTATION_INDEX.md`
- Godot + C# 上手：`docs/TEMPLATE_GODOT_GETTING_STARTED.md`
- 测试框架：`docs/testing-framework.md`
- 架构骨干：`docs/architecture/base/00-README.md`
- 手动发布：`docs/release/WINDOWS_MANUAL_RELEASE.md`

## 目录概览

- `Game.Core/`：纯 C# 领域逻辑
- `Game.Core.Tests/`：xUnit 单元测试
- `Game.Godot/`：Godot 运行时适配层
- `Game.Godot.Tests/`：Godot 相关测试
- `docs/`：架构、迁移、流程与发布文档
- `scripts/`：CI、本地工具、自动化脚本

## 协作与规范

- AI/协作规则：`AGENTS.md`
- 架构与质量口径：`CLAUDE.md`

