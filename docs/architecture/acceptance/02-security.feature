Feature: 02 Godot 安全基线
  Scenario: 基线护栏启用
    Given 应用以生产配置启动
    Then 资源访问仅允许 res:// 与 user://
    And 仅允许 HTTPS 外联且受白名单约束
