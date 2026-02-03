# 2026-02-03：UI Contract 与诊断能力进展（Windows.Agent）

## 本次目标

- 把“业务工作流 / UIA 边界 / UI Contract / 错误流水线”落到可审计文档。
- 补齐 UI Contract 的解析/校验/解释能力与 CLI 命令。
- 统一 CLI 输出 schema（顶层 success 与 exit code 对齐），并提供失败快照能力。
- 增加最小诊断通道（日志 tail），并补单测与审计表。

## 已完成

### 文档

- 新增：
  - `docs/architecture/2026-02-03-桌面客户端自动化测试业务工作流.md`
  - `docs/architecture/2026-02-03-错误观测与报告流水线.md`
  - `docs/reference/2026-02-03-UIA兼容性与混合定位策略.md`
  - `docs/reference/2026-02-03-UIContract与对象库规范.md`
- 更新：
  - `docs/reference/2026-02-02-CLI命令速查.md`
  - `docs/reference/2026-02-02-CLI命令与工具对照表-审计用.md`
  - `docs/reference/2026-02-03-桌面自动化点击与输入测试流程.md`（修正命令格式示例）
  - `docs/文档索引.md`

### 代码（CLI/Tools）

- UI Contract：
  - 新增 YAML/JSON 加载、校验、解释能力（`src/Windows.Agent/Contracts/*`）
  - 新增 Tool：`src/Windows.Agent/Tools/Contracts/ContractTool.cs`
  - 新增 CLI 命令：`contract validate/explain`
- 诊断通道：
  - 新增 Tool：`src/Windows.Agent/Tools/Diagnostics/TailLogTool.cs`
  - 新增 CLI 命令：`diag tail-log`
- CLI 输出：
  - 统一 schema：`schemaVersion/success/code/message/tool/input/result/error/artifacts/session`
  - 顶层 `success` 与 exit code 对齐（失败 exit code=1）
  - 新增 `--snapshot-on-error`、`--session`
- 去除“代码注释中的 MCP 残留表述”（Tools XML summary）

## 验证（本机执行）

1) CLI 单测：

```powershell
dotnet test src/Windows.Agent.Cli.Test/Windows.Agent.Cli.Test.csproj -c Release
```

结果：通过（11 tests）。

2) FileSystem 分类测试（避免 Desktop/OCR/SystemControl 抢焦点/改系统设置）：

```powershell
dotnet test src/Windows.Agent.Test/Windows.Agent.Test.csproj -c Release --filter "Category=FileSystem"
```

结果：通过（90 tests）。

> 备注：编译阶段仍会出现 OCR/SystemControl 相关告警（属于既有代码质量告警，不影响本次目标验收）。

## 待办（下一步）

- Phase 4（UIA）：`desktop uia-tree/uia-find/uia-invoke/uia-setvalue`（含 selector 解析单测 + 手动 Desktop 集成测）
- Phase 4（诊断）：`diag eventlog/collect`、更完善的证据包采集与归档
- 输出一致性：推动更多 Tool 统一输出 JSON（减少非 JSON raw 的启发式判定）

