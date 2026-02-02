# 2026-02-02：独立 CLI 工程改造计划（调用现有 Tools）

## 目标

实现一个独立 CLI 工程作为“调度封装”，对外提供命令行调用；对内不直接调用 Service，而是调用现存 `Windows.Agent.Tools.*` 类的公开方法，以复用工具层的输入参数与行为。

## 约束

- 不一次性大重构：优先最小可用（MVP）+ 可测试。
- CLI 输出默认 JSON 到 stdout；日志到 stderr。
- 默认“只读命令”可直接执行；高危命令需要显式开关（Phase 2 落地）。

## 步骤

1. 新建工程 `src/Windows.Agent.Cli/Windows.Agent.Cli.csproj`（console）
2. CLI 引用 `Windows.Agent` 项目，构建 DI 容器，按需 new Tool 类
3. 将命令映射到 Tool 方法：
   - `desktop click` -> `Windows.Agent.Tools.Desktop.ClickTool.ClickAsync`
   - `fs read` -> `Windows.Agent.Tools.FileSystem.ReadFileTool.ReadFileAsync`
   - `ocr screen` -> `Windows.Agent.Tools.OCR.ExtractTextFromScreenTool.ExtractTextFromScreenAsync`
   - `sys volume` -> `Windows.Agent.Tools.SystemControl.VolumeTool.*`
4. 输出规范：统一 `success / data / message / error`（JSON）
5. 单测：
   - 对每个命令映射写 1 个测试（mock service，验证 tool 方法被调用且参数正确）

## 验证

- `dotnet build -c Release`
- `dotnet run --project src/Windows.Agent.Cli/Windows.Agent.Cli.csproj -- help`
- `dotnet test`

## 回滚

- 删除 `src/Windows.Agent.Cli/` 及其测试工程；
- 不影响现有主工程构建与运行。
