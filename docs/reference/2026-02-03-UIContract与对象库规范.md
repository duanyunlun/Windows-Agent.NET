# 2026-02-03：UI Contract（对象库）规范（Windows.Agent）

## 1. 规范目标

桌面自动化测试最大的难点不是“如何点击/输入”，而是：

- 自然语言用例里说的“那个按钮/那个输入框”，如何 **稳定、可审计** 地定位到；
- 失败时如何证明“点对了窗口/点对了控件/动作确实发生”。

因此本规范要求：在写用户手册/测试用例时同步维护 **UI Contract（对象库/控件库）**，把控件信息结构化，降低 AI 编译难度与误点风险。

> 适用对象：对“我们能控制的应用 A”（AI 写的桌面应用）强制执行；对第三方应用作为推荐但不强制。

## 2. 文件格式与版本

- 支持：YAML（`.yml/.yaml`）或 JSON（`.json`）
- 必须包含：`contractVersion`（字符串，语义化版本）
- 建议：所有 ID 使用稳定英文（便于在测试步骤中引用）

## 3. UI Contract 最小字段（MUST）

> 表中示例仅说明形式；实际值由应用 A 的实现与手册约定决定。

### 3.1 顶层字段

| 字段 | 类型 | 必填 | 含义 | 示例 |
|---|---|---:|---|---|
| `contractVersion` | string | 是 | 契约版本 | `"1.0.0"` |
| `app.name` | string | 是 | 应用名（业务名） | `"AppA"` |
| `app.processNames` | string[] | 否（推荐） | 进程名白名单（用于更稳的窗口绑定） | `["AppA.exe"]` |
| `windows` | map | 是 | 窗口定义集合 | 见下 |
| `controls` | map | 是 | 控件定义集合 | 见下 |
| `assertions` | map | 否（推荐） | 断言定义集合 | 见下 |

### 3.2 窗口定义 `windows.<id>`

窗口是所有定位的根。每个窗口至少提供一种可匹配方式：

| 字段 | 类型 | 必填 | 含义 | 示例 |
|---|---|---:|---|---|
| `windows.<id>.titleContains` | string | 三选一 | 标题包含匹配 | `"AppA"` |
| `windows.<id>.titleRegex` | string | 三选一 | 标题正则匹配 | `"^AppA\\s+-\\s+.*$"` |
| `windows.<id>.className` | string | 三选一 | 窗口类名匹配（Win32 ClassName） | `"HwndWrapper[AppA.exe;;...]"` |

建议附加（非必需）：

- `windows.<id>.description`：窗口用途说明

### 3.3 控件定义 `controls.<id>`

控件定位采用降级链路：`UIA → OCR → 坐标`。每个控件必须至少提供一种定位方式（UIA/OCR/坐标其一），但强烈建议同时提供 2 种以上以提高鲁棒性。

| 字段 | 类型 | 必填 | 含义 | 示例 |
|---|---|---:|---|---|
| `controls.<id>.windowId` | string | 是 | 控件所属窗口 ID | `"main"` |
| `controls.<id>.description` | string | 否（推荐） | 控件用途（手册/报告可读） | `"发送 HTTP 模拟请求"` |
| `controls.<id>.uia` | object | 否 | UIA 定位信息（优先） | 见下 |
| `controls.<id>.ocr` | object | 否 | OCR 定位信息（降级） | 见下 |
| `controls.<id>.fallbackCoords` | object | 否 | 坐标兜底（最后） | 见下 |

#### 3.3.1 UIA 定位 `controls.<id>.uia`（至少提供一种）

| 字段 | 类型 | 必填 | 含义 | 示例 |
|---|---|---:|---|---|
| `automationId` | string | 四选一 | UIA AutomationId | `"btnSendHttp"` |
| `name` | string | 四选一 | UIA Name（可见文本） | `"发送"` |
| `controlType` | string | 四选一 | 控件类型（Button/Edit/Text/…） | `"Button"` |
| `path` | string | 四选一 | 控件路径（未来扩展） | `"Window/MainPanel/SendButton"` |

> 注意：对于 Electron/自绘应用，`automationId` 可能为空或不稳定，此时应优先 OCR。

#### 3.3.2 OCR 定位 `controls.<id>.ocr`

| 字段 | 类型 | 必填 | 含义 | 示例 |
|---|---|---:|---|---|
| `text` | string | 是 | 要识别的文字 | `"发送"` |
| `occurrence` | int | 否 | 第 N 次出现（用于重复文本） | `1` |

#### 3.3.3 坐标兜底 `controls.<id>.fallbackCoords`

建议使用“相对窗口矩形”的偏移，而不是绝对屏幕坐标：

| 字段 | 类型 | 必填 | 含义 | 示例 |
|---|---|---:|---|---|
| `offsetX` | int | 是 | 相对窗口左上角偏移 X | `420` |
| `offsetY` | int | 是 | 相对窗口左上角偏移 Y | `180` |

> 风险：分辨率/DPI/布局变化会导致偏移失效。使用坐标兜底时必须要求截图证据与后置断言。

### 3.4 断言定义 `assertions.<id>`（推荐）

断言用于“动作后验证”，避免“只执行不验证”。

建议支持的断言类型（可组合）：

| 类型 | 字段示例 | 含义 |
|---|---|---|
| OCR 文本出现 | `ocrText: "请求成功"` | 屏幕出现某文字 |
| 窗口标题变化 | `windowTitleContains: "已登录"` | 标题包含关键字 |
| 日志包含 | `logPattern: "NullReferenceException"` | 日志出现关键行 |
| 文件存在 | `fileExists: "C:\\temp\\out.json"` | 生成结果文件 |
| 剪贴板包含 | `clipboardContains: "token="` | 复制后的结果包含关键字 |

## 4. 示例（按钮发送 HTTP 模拟请求）

### 4.1 YAML 示例

```yaml
contractVersion: "1.0.0"
app:
  name: "AppA"
  processNames: ["AppA.exe"]

windows:
  main:
    description: "主窗口"
    titleContains: "AppA"

controls:
  btn_send_http:
    windowId: "main"
    description: "发送 HTTP 模拟请求"
    uia:
      automationId: "btnSendHttp"
      controlType: "Button"
    ocr:
      text: "发送"
      occurrence: 1
    fallbackCoords:
      offsetX: 420
      offsetY: 180

assertions:
  send_http_success:
    ocrText: "请求成功"
```

### 4.2 对应的“编排器执行要点”

1) EnsureWindow：置前 + 校验窗口正确
2) ResolveElement：优先 `btn_send_http.uia`，失败降级 `ocr`，再失败才用 `fallbackCoords`
3) Act：Invoke/Click
4) Assert：OCR 出现“请求成功” 或 读取应用日志关键行

## 5. AI 编译规则（必须明确执行）

编排器（AI）在把用例翻译成 CLI 指令时必须遵循：

1) **每一步动作前必须 EnsureWindow**
2) **优先 UIA**（可用时优先 Invoke/Value 等模式），失败才降级 OCR，再失败才允许坐标
3) **坐标动作必须采集证据**（至少 screenshot + 窗口矩形）并执行后置断言
4) **每一步必须有断言或可读回证据**（至少一种：OCR/剪贴板/文件/日志）
5) **失败分层归因**（CliError/LocateError/ActError/AssertError/AppError），并生成证据包

## 6. 第三方应用的最低可用策略（无 UI Contract）

当应用 A 无法提供 UI Contract（第三方、闭源、无可访问性信息）时：

- 仅承诺“尽力而为”的自动化（稳定性不保证）
- 优先策略：窗口标题（EnsureWindow） + OCR（ResolveElement） + 截图证据 + 断言
- 坐标只能作为最后兜底，并要求强约束环境（固定分辨率/缩放/窗口位置）

