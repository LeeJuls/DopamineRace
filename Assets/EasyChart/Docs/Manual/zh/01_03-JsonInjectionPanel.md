# JSON Injection 面板

本章说明 `Unity Easy Chart/Library Editor` 窗口左侧底部的 **JSON Injection** 面板。

它的定位是：用一段可读/可复制的 JSON 来表达当前 `ChartProfile` 的配置（或外部导入的配置），并支持 **ApplyToChart** 将 JSON 解析后回写到选中的 Profile。

---

## 面板位置与作用

- **位置**：Library 面板（资源树）下方。
- **主要用途**：
  - **导出**：把当前选中 `ChartProfile` 转成示例 JSON（Feed）
  - **编辑**：在文本框里手动修改 JSON
  - **导入/应用**：点击 **ApplyToChart**，把 JSON 解析并应用到当前选中的 `ChartProfile`

适用场景：

- **调试**：快速定位“某个字段是否生效”。
- **批量修改**：复制 JSON 到外部编辑器（支持多光标/查找替换），再粘贴回来 Apply。
- **与外部系统对接**：例如你的工具链/脚本生成 Feed，再在编辑器里 Apply。

---

## 控件说明（标题栏）

标题栏从左到右一般包含：

- **Min/Max**（按钮文字会变化）
  - 用于切换面板高度。
  - `Min`：收起为较小高度（更偏“辅助工具”）。
  - `Max`：展开为较大高度（更适合长 JSON）。

- **ApplyToChart**（图标按钮）
  - 把当前文本框里的 JSON 尝试解析为 Feed，并应用到选中的 `ChartProfile`。
  - 成功后会：
    - 标记资产为 Dirty 并 `SaveAssets()`
    - 刷新 Series 列表
    - 刷新 Preview

- **Help**（图标按钮）
  - 打开本章节。

---

## 控件说明（按钮行）

标题栏下方还有一行按钮（可能会自动换行）：

- **API Envelope**（图标开关）
  - 控制示例 JSON 是否包裹为“API 返回格式”。
  - 你需要把 Feed 直接交给某个 HTTP API/服务时，这个选项会更方便。
  - 切换后会重新生成示例，并覆盖文本框（详见“覆盖规则”）。

- **Feed Mode**（下拉框）
  - 用于控制“示例 JSON 输出包含哪些层级/字段”。
  - 选项来自内部枚举（常见有）：
    - `Lite`
    - `Standard / ID`
    - `Standard / Default`
    - `Standard / With Axes`
    - `Full`
  - 一般建议：
    - **快速看结构**：用 `Lite`
    - **需要稳定引用**：用 `Standard / ID`
    - **需要包含轴配置**：用 `Standard / With Axes`
    - **需要完整复制/迁移**：用 `Full`

- **Datas Format**（下拉框）
  - 控制 `seriesData`（数据点）字段的输出格式。
  - 常见选项：
    - `Values`：更精简，偏“只关心数值”。
    - `Standard`：默认格式，适合一般编辑与 Apply。
    - `Full`：更完整（可能包含更多字段/结构），适合迁移/还原。

- **Copy**（图标按钮）
  - 复制当前文本框内容到剪贴板。

---

## 文本框与“覆盖规则”（非常重要）

JSON 文本框是可编辑的，但为了避免你手写的内容被自动覆盖，面板内部有一个“脏标记”逻辑：

- **只要你手动改过文本框内容**，就会认为“用户已修改”（dirty）。
- 当处于 dirty 状态时：
  - 编辑器不会自动用示例 JSON 覆盖你的内容。
- 但当你切换以下选项时，会**强制覆盖**（同时清除 dirty）：
  - `API Envelope`
  - `Feed Mode`
  - `Datas Format`
  - 或在切换选中 Profile 时（会重置为该 Profile 的示例）

建议：

- 如果你要做大幅改动：
  - 先 Copy 到外部编辑器改
  - 改完再粘贴回来 Apply

---

## ApplyToChart 的行为与注意事项

- **ApplyToChart 会修改当前选中的 `ChartProfile` 资产**。
- 如果 JSON 解析失败，会在 Console 输出错误：
  - `ApplyToChart failed: invalid JSON or unsupported format.`
- `Full` 模式下会允许覆盖更多“Meta/结构”信息（例如某些标识/配置），因此更强大也更危险。

建议：

- 在 Apply 前确保：
  - 左侧已选中正确的 `ChartProfile`
  - JSON 格式正确（括号/逗号）
  - 你理解当前 Feed Mode 会覆盖哪些内容

---

## 推荐工作流

### 1) 从当前 Profile 导出并微调

- 选中一个 `ChartProfile`
- 选择合适的 `Feed Mode` / `Datas Format`
- Copy 到外部编辑器微调
- 粘贴回来
- ApplyToChart

### 2) 从外部导入配置

- 把外部 JSON 粘贴到文本框
- ApplyToChart
- 去 Inspector / Series 进一步精调

---

## Help

- 点击标题栏最右侧 **Help** 图标可回到本章节。
