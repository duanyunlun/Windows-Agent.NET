# 2026-02-02：现有 Tool 功能清单（审计用）

目的：把当前仓库中**已实现且可通过 CLI 调用**的 Tool 功能完整列出，便于你逐项审计“能力是否符合要求/是否允许/是否需要加保护策略”。

## 证据来源（可追溯）

- Tool 注册清单：`src/Windows.Agent.Cli/ToolRegistry.cs`
- CLI 命令映射：`src/Windows.Agent.Cli/CliDispatcher.cs`
- Tool 实现源码：`src/Windows.Agent/Tools/**`

## CLI 输出结构（统一）

所有 CLI 命令默认输出 JSON 到 stdout：

- `success`：CLI 调度是否成功（注意：Tool 内部失败通常体现在 `result.raw` 或 `result.parsed`）
- `tool`：实际调用的 `namespace.class.method`
- `result.raw`：Tool 原始输出（字符串）
- `result.parsed`：当 `raw` 为 JSON 字符串时，CLI 会尝试解析并输出该字段

> 解析逻辑见：`src/Windows.Agent.Cli/CliDispatcher.cs`（`WriteToolResult`）

## Tool 分组（按 CLI group）

### Desktop（桌面自动化）

说明：本组能力会真实操作桌面（鼠标/键盘/窗口/剪贴板），可能抢焦点、影响当前用户操作。

| CLI（group action） | 主要参数/默认值（来自 CLI 映射） | 调用的 Tool 方法（`tool` 字段） | 副作用级别 |
|---|---|---|---|
| `desktop state` | `--vision` 默认 `false` | `Windows.Agent.Tools.Desktop.StateTool.GetDesktopStateAsync` | 只读（可能截图） |
| `desktop launch` | `--name` 必填 | `Windows.Agent.Tools.Desktop.LaunchTool.LaunchAppAsync` | 高 |
| `desktop switch` | `--name` 必填 | `Windows.Agent.Tools.Desktop.SwitchTool.SwitchAppAsync` | 高 |
| `desktop windowinfo` | `--name` 必填 | `Windows.Agent.Tools.Desktop.GetWindowInfoTool.GetWindowInfoAsync` | 只读 |
| `desktop click` | `--x --y` 必填；`--button` 默认 `left`；`--clicks` 默认 `1` | `Windows.Agent.Tools.Desktop.ClickTool.ClickAsync` | 高 |
| `desktop move` | `--x --y` 必填 | `Windows.Agent.Tools.Desktop.MoveTool.MoveAsync` | 高 |
| `desktop drag` | `--startx --starty --endx --endy` 必填 | `Windows.Agent.Tools.Desktop.DragTool.DragAsync` | 高 |
| `desktop scroll` | `--x/--y` 可选；`--type` 默认 `vertical`；`--direction` 默认 `down`；`--wheel` 默认 `1` | `Windows.Agent.Tools.Desktop.ScrollTool.ScrollAsync` | 高 |
| `desktop type` | `--x --y --text` 必填；`--clear/--enter/--interpretSpecialCharacters` 默认 `false` | `Windows.Agent.Tools.Desktop.TypeTool.TypeAsync` | 高 |
| `desktop key` | `--key` 必填 | `Windows.Agent.Tools.Desktop.KeyTool.KeyAsync` | 高 |
| `desktop shortcut` | `--keys` 必填（示例：`ctrl+shift+esc`） | `Windows.Agent.Tools.Desktop.ShortcutTool.ShortcutAsync` | 高 |
| `desktop clipboard` | `--mode` 必填（`copy|paste`）；`--text` 可选 | `Windows.Agent.Tools.Desktop.ClipboardTool.ClipboardAsync` | 中 |
| `desktop wait` | `--seconds` 必填 | `Windows.Agent.Tools.Desktop.WaitTool.WaitAsync` | 低 |
| `desktop screenshot` | 无参数 | `Windows.Agent.Tools.Desktop.ScreenshotTool.TakeScreenshotAsync` | 只读（落盘到临时目录） |
| `desktop openbrowser` | `--url/--search` 可选 | `Windows.Agent.Tools.Desktop.OpenBrowserTool.OpenBrowserAsync` | 高 |
| `desktop scrape` | `--url` 必填 | `Windows.Agent.Tools.Desktop.ScrapeTool.ScrapeAsync` | 中（网络请求） |
| `desktop powershell` | `--command` 必填 | `Windows.Agent.Tools.Desktop.PowershellTool.ExecuteCommandAsync` | 高（命令执行） |
| `desktop ui-find` | `--type` 必填（`text|className|automationId`）；`--value` 必填 | `Windows.Agent.Tools.Desktop.UIElementTool.(Find*)` | 只读 |
| `desktop ui-props` | `--x --y` 必填 | `Windows.Agent.Tools.Desktop.UIElementTool.GetElementPropertiesAsync` | 只读 |
| `desktop ui-wait` | `--type --value` 必填；`--timeoutMs` 默认 `5000` | `Windows.Agent.Tools.Desktop.UIElementTool.WaitForElementAsync` | 只读 |

对应 Tool 源码（按文件）：

- `src/Windows.Agent/Tools/Desktop/ClickTool.cs`：`ClickTool.ClickAsync(...)`
- `src/Windows.Agent/Tools/Desktop/MoveTool.cs`：`MoveTool.MoveAsync(...)`
- `src/Windows.Agent/Tools/Desktop/DragTool.cs`：`DragTool.DragAsync(...)`
- `src/Windows.Agent/Tools/Desktop/ScrollTool.cs`：`ScrollTool.ScrollAsync(...)`
- `src/Windows.Agent/Tools/Desktop/TypeTool.cs`：`TypeTool.TypeAsync(...)`
- `src/Windows.Agent/Tools/Desktop/KeyTool.cs`：`KeyTool.KeyAsync(...)`
- `src/Windows.Agent/Tools/Desktop/ShortcutTool.cs`：`ShortcutTool.ShortcutAsync(...)`
- `src/Windows.Agent/Tools/Desktop/ClipboardTool.cs`：`ClipboardTool.ClipboardAsync(...)`
- `src/Windows.Agent/Tools/Desktop/WaitTool.cs`：`WaitTool.WaitAsync(...)`
- `src/Windows.Agent/Tools/Desktop/ScreenshotTool.cs`：`ScreenshotTool.TakeScreenshotAsync()`
- `src/Windows.Agent/Tools/Desktop/OpenBrowserTool.cs`：`OpenBrowserTool.OpenBrowserAsync(...)`
- `src/Windows.Agent/Tools/Desktop/ScrapeTool.cs`：`ScrapeTool.ScrapeAsync(...)`
- `src/Windows.Agent/Tools/Desktop/PowershellTool.cs`：`PowershellTool.ExecuteCommandAsync(...)`
- `src/Windows.Agent/Tools/Desktop/StateTool.cs`：`StateTool.GetDesktopStateAsync(...)`
- `src/Windows.Agent/Tools/Desktop/LaunchTool.cs`：`LaunchTool.LaunchAppAsync(...)`
- `src/Windows.Agent/Tools/Desktop/SwitchTool.cs`：`SwitchTool.SwitchAppAsync(...)`
- `src/Windows.Agent/Tools/Desktop/GetWindowInfoTool.cs`：`GetWindowInfoTool.GetWindowInfoAsync(...)`
- `src/Windows.Agent/Tools/Desktop/UIElementTool.cs`：`UIElementTool.*`

### FileSystem（文件系统）

说明：本组能力会读写/创建/删除文件与目录，属于强副作用能力（尤其 delete/rmdir）。

| CLI（group action） | 主要参数/默认值（来自 CLI 映射） | 调用的 Tool 方法（`tool` 字段） | 副作用级别 |
|---|---|---|---|
| `fs read` | `--path` 必填 | `Windows.Agent.Tools.FileSystem.ReadFileTool.ReadFileAsync` | 只读 |
| `fs write` | `--path --content` 必填；`--append` 默认 `false` | `Windows.Agent.Tools.FileSystem.WriteFileTool.WriteFileAsync` | 高 |
| `fs create` | `--path --content` 必填 | `Windows.Agent.Tools.FileSystem.CreateFileTool.CreateFileAsync` | 高 |
| `fs delete` | `--path` 必填 | `Windows.Agent.Tools.FileSystem.DeleteFileTool.DeleteFileAsync` | 高 |
| `fs copy` | `--source --destination` 必填；`--overwrite` 默认 `false` | `Windows.Agent.Tools.FileSystem.CopyFileTool.CopyFileAsync` | 高 |
| `fs move` | `--source --destination` 必填；`--overwrite` 默认 `false` | `Windows.Agent.Tools.FileSystem.MoveFileTool.MoveFileAsync` | 高 |
| `fs info` | `--path` 必填 | `Windows.Agent.Tools.FileSystem.GetFileInfoTool.GetFileInfoAsync` | 只读 |
| `fs list` | `--path` 必填；`--files/--dirs` 默认 `true`；`--recursive` 默认 `false` | `Windows.Agent.Tools.FileSystem.ListDirectoryTool.ListDirectoryAsync` | 只读 |
| `fs mkdir` | `--path` 必填；`--parents` 默认 `true` | `Windows.Agent.Tools.FileSystem.CreateDirectoryTool.CreateDirectoryAsync` | 中 |
| `fs rmdir` | `--path` 必填；`--recursive` 默认 `false` | `Windows.Agent.Tools.FileSystem.DeleteDirectoryTool.DeleteDirectoryAsync` | 高 |
| `fs search-name` | `--directory --pattern` 必填；`--recursive` 默认 `false` | `Windows.Agent.Tools.FileSystem.SearchFilesTool.SearchFilesByNameAsync` | 只读 |
| `fs search-ext` | `--directory --ext` 必填；`--recursive` 默认 `false` | `Windows.Agent.Tools.FileSystem.SearchFilesTool.SearchFilesByExtensionAsync` | 只读 |

对应 Tool 源码（按文件）：

- `src/Windows.Agent/Tools/FileSystem/ReadFileTool.cs`：返回 JSON（`success/content/message/path/contentLength`）
- `src/Windows.Agent/Tools/FileSystem/WriteFileTool.cs`：返回 JSON（`success/message/path/contentLength/append`）
- `src/Windows.Agent/Tools/FileSystem/CreateFileTool.cs`：返回 JSON（`success/message/path/contentLength`）
- `src/Windows.Agent/Tools/FileSystem/DeleteFileTool.cs`、`CopyFileTool.cs`、`MoveFileTool.cs`、`GetFileInfoTool.cs`、`ListDirectoryTool.cs`、`CreateDirectoryTool.cs`、`DeleteDirectoryTool.cs`、`SearchFilesTool.cs`

### OCR（文字识别）

说明：本组能力会截图/读取文件并进行 OCR，可能消耗 CPU/GPU/模型资源。

| CLI（group action） | 主要参数/默认值（来自 CLI 映射） | 调用的 Tool 方法（`tool` 字段） | 副作用级别 |
|---|---|---|---|
| `ocr screen` | 无参数 | `Windows.Agent.Tools.OCR.ExtractTextFromScreenTool.ExtractTextFromScreenAsync` | 只读（截图） |
| `ocr region` | `--x --y --width --height` 必填 | `Windows.Agent.Tools.OCR.ExtractTextFromRegionTool.ExtractTextFromRegionAsync` | 只读（截图） |
| `ocr find` | `--text` 必填 | `Windows.Agent.Tools.OCR.FindTextOnScreenTool.FindTextOnScreenAsync` | 只读（截图） |
| `ocr coords` | `--text` 必填 | `Windows.Agent.Tools.OCR.GetTextCoordinatesTool.GetTextCoordinatesAsync` | 只读（截图） |
| `ocr file` | `--path` 必填 | `Windows.Agent.Tools.OCR.ExtractTextFromFileTool.ExtractTextFromFileAsync` | 只读（读文件） |

对应 Tool 源码（按文件）：

- `src/Windows.Agent/Tools/OCR/ExtractTextFromScreenTool.cs`：返回 JSON（`success/text/message`）
- `src/Windows.Agent/Tools/OCR/ExtractTextFromRegionTool.cs`：返回 JSON（`success/text/message/region`）
- `src/Windows.Agent/Tools/OCR/FindTextOnScreenTool.cs`：返回 JSON（`success/found/searchText/message`）
- `src/Windows.Agent/Tools/OCR/GetTextCoordinatesTool.cs`
- `src/Windows.Agent/Tools/OCR/ExtractTextFromFileTool.cs`

### SystemControl（系统控制）

说明：本组能力会修改系统状态（音量/亮度/分辨率）。**CLI 不会自动还原**，仅测试中有“尽最大努力恢复”的策略。

| CLI（group action） | 主要参数/默认值（来自 CLI 映射） | 调用的 Tool 方法（`tool` 字段） | 副作用级别 |
|---|---|---|---|
| `sys volume` | 优先级：`--mute` > `--percent` > `--inc` > `--dec`；无参数则读取当前音量 | `Windows.Agent.Tools.SystemControl.VolumeTool.*` | 高 |
| `sys brightness` | 优先级：`--percent` > `--inc` > `--dec`；无参数则读取当前亮度 | `Windows.Agent.Tools.SystemControl.BrightnessTool.*` | 高 |
| `sys resolution` | `--type` 必填（`high|medium|low`） | `Windows.Agent.Tools.SystemControl.ResolutionTool.SetResolutionAsync` | 高 |

对应 Tool 源码（按文件）：

- `src/Windows.Agent/Tools/SystemControl/VolumeTool.cs`：`SetVolumeAsync / SetVolumePercentAsync / GetCurrentVolumeAsync / SetMuteStateAsync`
- `src/Windows.Agent/Tools/SystemControl/BrightnessTool.cs`：`SetBrightnessAsync / SetBrightnessPercentAsync / GetCurrentBrightnessAsync`
- `src/Windows.Agent/Tools/SystemControl/ResolutionTool.cs`：`SetResolutionAsync`

## 下一步（审计建议）

建议你按“副作用级别”逐项审计并给出允许策略（默认允许/需显式开关/完全禁用）：

1. 高副作用：`desktop *`（除 state/screenshot/ui-* 外）、`fs write/create/delete/move/copy/rmdir`、`sys *`、`desktop powershell/openbrowser`
2. 只读/低副作用：`desktop state/windowinfo/screenshot/ui-*`、`fs read/info/list/search-*`、`ocr *`

