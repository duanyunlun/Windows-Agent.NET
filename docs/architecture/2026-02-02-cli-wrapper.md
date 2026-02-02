# 2026-02-02：独立 CLI 工程架构（Tools 调用）

## 目标

让 CLI 成为主要对外入口；内部复用现存 Tools 作为“可调用单元”，避免在 CLI 中重复定义参数与业务逻辑。

## 模块

- `Windows.Agent.Cli`（新）：命令行入口、解析、输出格式、错误码、策略（后续）。
- `Windows.Agent`（现有）：Service + Tools（作为能力提供方）。

## 调用链

CLI -> Tool（参数保持一致）-> Service（实际执行）

## 测试策略

- CLI/Tool 映射：使用 mock `Windows.Agent.Interface.*`，验证 Tool 被正确调用与输出符合约定。
- 真正的桌面交互（点击/输入/拖拽）：放在 Integration Tests，默认跳过。
