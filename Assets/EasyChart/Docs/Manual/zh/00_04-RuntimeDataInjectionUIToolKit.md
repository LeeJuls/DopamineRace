# 运行时数据注入（UIToolKit）

本章介绍：在 UI Toolkit 工作流下，如何在运行时给 `ChartElement` 注入数据。

对应脚本：`EasyChartDataSource`

---

## 1. 这套方案适合什么场景？

- 你的图表是 UI Toolkit 体系（`UIDocument` + UXML + `ChartElement`）
- 你希望提供一套“业务侧更容易调用”的注入 API（labels / values / x-y / pie / ring）
- 或者你希望直接拿一段 JSON 注入（`ChartFeed`）

---

## 2. 快速上手（最推荐的流程）

1. 在场景里准备 `UIDocument`，并确保 UXML 里有 `ChartElement`。
2. 在挂着 `UIDocument` 的 GameObject 上添加组件：`EasyChartDataSource`。
3. 在 Inspector 里填写：
   - `uiDocument`
   - `chartElementName`（默认 `main-chart`，对应 UXML 中 `ChartElement` 的 `name`）
   - `profile`（可选，但强烈建议填：让样式/Series 类型来自你在编辑器配置好的 `ChartProfile`）
4. 在运行时用代码调用：
   - `SetCategoryLabels(...)`
   - `SetSeriesValues(...)` / `SetSeriesXY(...)`
   - 或 `ApplyJson(...)`

组件内部会：

- 从 `UIDocument.rootVisualElement` 里找到目标 `ChartElement`
- 必要时用 `profile` 初始化图表 Data
- 修改 `ChartElement.Data` 并调用 `RefreshData()`

---

## 3. Inspector 字段说明

`EasyChartDataSource` 的核心配置字段：

- `uiDocument`
  - 指向当前 UI 的 `UIDocument`。
  - 如果不填，脚本会尝试 `GetComponent<UIDocument>()`。

- `chartElementName`
  - 目标 `ChartElement` 的 `name`（UXML/USS 的那个 name）。默认值为 `main-chart`。
  - 如果你希望用 JSON 中的 `chartId/chartName` 自动定位，也可以让 `ChartElement` 的 `name` 与之保持一致（见第 5 节）。

- `profile`
  - 可选。
  - 如果赋值，组件会将 `ChartElement.Profile = profile`，用于初始化/保持样式、Series 结构等。

- `playAnimationOnRefresh`
  - 每次注入后调用 `RefreshData(..., playAnimation: playAnimationOnRefresh)`。

- `allowCreateSeriesFromFeed`
  - 当你用 JSON（`ApplyJson`）注入时，如果 feed 中的 series 无法匹配到现有 Serie：
    - `false`（默认）：不创建新 Serie，只更新匹配到的部分。
    - `true`：允许根据 feed 创建新的 Serie（可能触发重建 renderers）。

---

## 4. 常用注入 API（不写 JSON）

### 4.1 设置类目轴标签

`SetCategoryLabels(labels, axisId = AxisId.XBottom)`

- 会把该轴设为 Category，并覆盖 `labels`。

### 4.2 单序列 y 值（自动 x=0..n-1）

`SetSeriesValues("Sales", values)`

- 默认会找到/创建一条 Serie（默认类型为 Line，不强制改类型）。
- 写入 `SeriesData.value`，并把 `SeriesData.x` 设为索引。

### 4.3 XY 点

`SetSeriesXY("Scatter", x, y)`

- 把 `x[]` 写入 `SeriesData.x`，把 `y[]` 写入 `SeriesData.value`。

### 4.4 Pie / Ring 注入

- `SetPie(serieName, names, values)`
  - 强制该 Serie 为 `Pie` 类型。
  - 使用 `SeriesData.name` 作为切片名，`SeriesData.value` 为数值。

- `SetRing(serieName, names, percents)`
  - 强制该 Serie 为 `RingChart` 类型。
  - 使用 `SeriesData.name` 作为环名，`SeriesData.value` 为进度值。

---

## 5. JSON 注入（ChartFeed）

你可以用：`ApplyJson(json)`

该方法会把 JSON 解析为 `ChartFeed` 并应用到 `ChartElement.Data`。

### 5.1 `ChartFeed` 结构

```json
{
  "chartId": "optional",
  "chartName": "optional",
  "axes": [
    {
      "axisId": "XBottom",
      "labels": ["Mon", "Tue", "Wed"]
    }
  ],
  "series": [
    {
      "serieId": "optional",
      "name": "optional",
      "type": "Line",
      "datas": [
        { "x": 0, "value": 12 },
        { "x": 1, "value": 18 }
      ]
    }
  ]
}
```

对应字段见运行时代码 `Scripts/Runtime/Feed/ChartFeed.cs`。

### 5.2 ChartElement 的定位规则（`chartId` / `chartName`）

`ApplyJson` 内部会尝试：

- 如果 feed 提供了 `chartId`：优先 `rootVisualElement.Q<ChartElement>(chartId)`
- 其次如果提供了 `chartName`：尝试 `Q<ChartElement>(chartName)`
- 都找不到才退回到 `chartElementName`（默认 `main-chart`）

因此：

- 如果你只有一个图表，保持默认值也没问题。
- 如果你一个 UI 里有多个 `ChartElement`，建议让每个图表的 `name` 与 feed 的 `chartId` 或 `chartName` 对齐。

### 5.3 series 匹配与类型覆盖

`ApplyJson` 会先检查 JSON 内是否出现过 `"type":`，若出现则认为你希望允许类型覆盖（`allowTypeOverride=true`）。

Serie 匹配规则：

- 如果 `serieId` 非空：按 `Serie.id` 匹配
- 否则如果 `name` 非空：按 `Serie.name` 匹配
- 否则（索引模式）：按 feed 的序号匹配（第 i 条对第 i 条）

当无法匹配到 Serie 时：

- `allowCreateSeriesFromFeed=false`（默认）：该条 feed 会被跳过（不创建）。
- `allowCreateSeriesFromFeed=true`：会创建新的 Serie，并使用 feed 的 `type/name/serieId`。

对已匹配到的 Serie：

- 仅当 `allowTypeOverride=true` 且不是索引模式时，才会允许覆盖 `id/name/type`。

---

## 6. 常见问题与排错

- **不显示 / TryGetChart 失败**
  - 确认 `uiDocument` 赋值正确
  - 确认 UXML 中 `ChartElement` 的 `name` 与 `chartElementName` 一致

- **JSON 解析失败**
  - `EasyChartDataSource` 解析 JSON 时：
    - 会优先尝试 Newtonsoft（若项目里存在 `Newtonsoft.Json`）
    - 否则使用 Unity `JsonUtility`，并把 `type/axisId` 的字符串写法转换为枚举整数再解析
  - 建议先用一份已知能解析的 JSON（例如从编辑器 JSON 面板生成）再改。

- **注入后 Series 对不上 / 更新错了线**
  - 优先使用 `serieId` 做稳定匹配。
  - 如果只用 `name`，且同名 Serie 存在多个，脚本会使用第一个并给 warning。

- **JSON 想新增 Serie 但没新增**
  - 把 `allowCreateSeriesFromFeed` 打开。
