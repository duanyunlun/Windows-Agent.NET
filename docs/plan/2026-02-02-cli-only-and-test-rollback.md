# 2026-02-02：CLI Only（移除 MCP）+ SystemControl 测试自动还原计划

## 目标

1) **入口收敛**：仓库不再提供 MCP Server；只保留独立 CLI 工程作为对外入口。  
2) **库形态明确**：`Windows.Agent` 作为“能力库”（Services + Tools），不再包含可执行入口与 MCP 元数据。  
3) **测试可回滚**：除 Desktop 外的测试（尤其是 `SystemControl`）在执行后必须尽量恢复原始系统设置（音量/静音、亮度、分辨率）。  

## 约束

- 不做无关重构，优先最小变更达成目标。
- SystemControl 的真实测试有系统副作用，必须：
  - 单测/集成测试分层；
  - 具备“恢复原状”的兜底逻辑；
  - 并避免并行执行导致互相干扰。

## 变更范围

### A. 移除 MCP（Phase 3 立即执行）

- 从 `Windows.Agent` 工程移除 MCP Server 启动入口（`AddMcpServer/WithStdioServerTransport/WithToolsFromAssembly`）。
- 移除 MCP 相关 NuGet 引用与 package metadata：
  - `ModelContextProtocol` 包
  - `.mcp/server.json` 打包项与 `PackageType=McpServer`
- 移除 Tools 上的 MCP attribute（例如 `[McpServerToolType]` / `[McpServerTool]`），保留 Tools 本身供 CLI 调用。
- 删除 `Windows.Agent` 工程内的 Legacy CLI（`src/Windows.Agent/Cli/`），避免双入口与误用。
- 对外入口收敛到 `Windows.Agent.Cli`：
  - `dotnet run --project src/Windows.Agent.Cli/Windows.Agent.Cli.csproj -- ...`
  - 后续若需要发布 dotnet tool，则以 `Windows.Agent.Cli` 作为打包工程。

### B. SystemControl 测试自动还原（Phase 2 的一部分，立即执行）

对 `src/Windows.Agent.Test/SystemControl/*.cs`：

- 每个测试在开始时记录“当前系统状态”，测试结束（成功/失败）后在 `DisposeAsync` 里恢复：
  - Volume：恢复音量百分比 + 静音状态（通过 NAudio 查询/恢复）。
  - Brightness：恢复到原始亮度（若原始亮度 < 20，则按实现限制恢复到 20）。
  - Resolution：记录当前 `DEVMODE`（`EnumDisplaySettings(ENUM_CURRENT_SETTINGS)`），测试结束后用 `ChangeDisplaySettings` 恢复。
- 禁止并行：
  - 建立 `SystemControl` 测试集合并 `DisableParallelization=true`，所有 SystemControl 测试类加入同一 collection，避免互相抢占系统设置。

## 验收标准（Definition of Done）

1) 代码层面不存在 MCP Server 入口与依赖：
   - `src/Windows.Agent/Program.cs` 不再作为入口（可删除或不再存在）。
   - `Windows.Agent.csproj` 不再包含 `ModelContextProtocol`、`PackageType=McpServer`、`.mcp/server.json` 打包。
  - `Windows.Agent.Tools.*` 不再引用 `ModelContextProtocol.Server`。
2) CLI 仍可构建与运行：
   - `dotnet build -c Release`
   - `dotnet run --project src/Windows.Agent.Cli/Windows.Agent.Cli.csproj -- help`
3) SystemControl 测试具备“自动还原”：
   - `dotnet test src/Windows.Agent.Test/Windows.Agent.Test.csproj -c Release --filter "Category=SystemControl"`
   - 运行后系统音量/静音/亮度/分辨率应尽量回到执行前（分辨率/音量/静音严格恢复；亮度受实现下限影响）。

## 最小验证命令

- 构建：
  - `dotnet build -c Release`
- 安全单测（默认推荐）：
  - `dotnet test src/Windows.Agent.Cli.Test/Windows.Agent.Cli.Test.csproj -c Release`
- SystemControl（有副作用，但应自动恢复）：
  - `dotnet test src/Windows.Agent.Test/Windows.Agent.Test.csproj -c Release --filter "Category=SystemControl"`

## 回滚方案

- Git 回退对应提交；或：
  - 还原 `Windows.Agent` 的 MCP 包引用与 Program 入口；
  - 恢复 `.mcp/server.json` 与 `PackageType=McpServer`；
  - 删除新增的 SystemControl 还原辅助代码（测试侧）。
