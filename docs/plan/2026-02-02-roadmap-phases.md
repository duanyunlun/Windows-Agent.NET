# 2026-02-02：路线图（按 Phase）

## 总目标

把当前项目演进为“以 CLI 为主”的 Windows 自动化工具集：每类能力可通过命令行单次调用；具备明确的安全策略；每个功能尽量有可重复的测试。

## Phase 0：文档与基线（本阶段必须先完成）

目标：
- 文档齐全：优化清单、路线图、计划、进展、参考。
- 建立“对外契约”基线：CLI 命令格式、输出 JSON schema、错误码约定。

验收：
- `docs/` 结构与索引齐全。
- `docs/reference/` 有可执行命令示例。

## Phase 1：独立 CLI 工程封装（非 MCP）

目标：
- 新建独立 CLI 调度工程（例如 `Windows.Agent.Cli`）。
- CLI 子命令按工具类别拆分（desktop/fs/ocr/sys）。
- CLI 内部通过“调用现有 Tools 类”执行，而不是直接调用 Service。

验收：
- `dotnet run --project <cli> -- help` 可用。
- 至少 1 个 desktop 命令 + 1 个 fs 命令可用。
- 输出 JSON 可解析，错误输出统一格式。

## Phase 2：测试体系与副作用控制

目标：
- CLI 命令解析与调度：纯单测（mock 服务接口）。
- 高危操作：默认禁用，需显式开关（例如 `--dangerous`）。
- 可选 Integration Tests（默认不跑），用于真实桌面验证。

验收：
- `dotnet test` 默认不触发桌面操作且稳定通过（在无 UI 环境也应可跑）。

## Phase 3：下线 MCP Server（必选）

目标：
- 完全移除 MCP 依赖与 `PackageType=McpServer`，只保留 CLI 入口（独立 CLI 工程为主）。
- 移除 `Windows.Agent` 内嵌 Legacy CLI（避免多入口与误用）。

验收：
- 主发布产物为 CLI（dotnet tool 或 `dotnet run --project ...`），不再作为 MCP Server 入口。
- 仓库内不再存在 MCP server.json / MCP 启动入口 / `ModelContextProtocol` 依赖。

## Phase 4：能力增强（UIA/可观测性/性能）

目标：
- State 输出具备真实可交互元素树（UIA）。
- OCR 初始化与缓存优化、资源释放优化。
- 日志/审计/权限策略完善。

验收：
- State 输出可用于稳定定位与操作（含坐标/selector/窗口信息）。
