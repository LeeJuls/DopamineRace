# 运行时数据注入（UIToolKit）

本章介绍：在 UI Toolkit 工作流下，如何在运行时给 `ChartElement` 注入数据。

对应脚本：`UIToolKitRuntimeJsonInjection`

---

## 1. 这套方案适合什么场景？

- 你的图表是 UI Toolkit 体系（`UIDocument` + UXML + `ChartElement`）
- 你希望通过 JSON 在运行时注入数据
- 你已经通过 `ChartProfile` 把样式、轴、Series 类型等结构配置好了

这套注入逻辑的定位是：**更新数据为主**，结构变更（比如新增 Series、强行覆盖 Series 类型）不是它的主要目标。

---

## 2. 快速上手（最推荐的流程）

1. 在场景里准备 `UIDocument`，并确保 UXML 里有 `ChartElement`，且 `ChartElement` 已绑定 `ChartProfile`。
2. 在挂着 `UIDocument` 的 GameObject 上添加组件：`UIToolKitRuntimeJsonInjection`（菜单：`Add Component > EasyChart > UI Toolkit Runtime JSON Injection`）。
3. 在 Inspector 里填写：
   - `Chart Element Name`（可选，用于指定目标 `ChartElement` 的 name）
4. 点击 **Generate Example JSON** 生成一份与当前 Profile 匹配的示例 JSON。
5. 在 `JSON Content` 文本框里修改数据。
6. 点击 **Apply JSON to Chart**。

组件内部会：

- 从 `UIDocument.rootVisualElement` 里找到目标 `ChartElement`
- 首次注入时自动克隆 Profile（避免修改原始资产）
- 解析 JSON 并应用到 Profile
- 调用 `ForceRefreshProfile()` 刷新图表

---

## 3. Inspector 字段说明

`UIToolKitRuntimeJsonInjection` 的核心配置字段：

### 3.1 Target

- **Chart Element Name**
  - 目标 `ChartElement` 的 `name`（UXML/USS 的那个 name）。
  - 如果为空，组件会查找 UIDocument 中的第一个 `ChartElement`。

### 3.2 JSON Generation Settings

- **JSON Mode（`ChartJsonMode`）**
  - 控制"生成示例 JSON"时的格式。
  - `Compact`：仅数据值，最小负载。格式为 `{"series": [{"datas": [1, 2, 3]}]}`
  - `Standard`：名称 + 结构化数据对象。包含 `chartName`、`series[].name` 和 `datas` 对象数组
  - `Full`：所有元数据包括轴、类型、颜色等。适用于完整复制/迁移图表配置
  - 一般建议先用 `Standard`（更直观）。

- **Use Api Envelope**
  - 生成示例 JSON 时，是否包一层接口返回壳：
    - `{ "code": 200, "message": "success", "data": { ...真正的ChartFeed... } }`
  - 应用时也会尝试自动从壳里提取 `data`。

- **Auto Generate**
  - 当你切换 `JSON Mode` 或 `Use Api Envelope` 时，自动重新生成示例 JSON。

### 3.3 JSON Content

- **JSON Content**
  - 你要注入的 JSON 字符串。
  - 如果为空，点击 Apply 时会直接警告并返回。

---

## 4. JSON 格式（ChartFeed）

底层的数据模型是 `ChartFeed`：

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

字段对应代码：`Scripts/Runtime/Feed/ChartFeed.cs`

### 4.1 axes 字段

- `axisId` 为 `AxisId` 枚举（如 `XBottom`、`XTop`、`YLeft`、`YRight` 等）。
- `labels` 存在时会把该轴视为 Category，并直接覆盖 labels。

### 4.2 series 字段

- **匹配优先级**：
  - 如果给了 `serieId`：按 `Serie.id` 精确匹配
  - 否则如果给了 `name`：按 `Serie.name` 匹配
  - 否则（`serieId` 与 `name` 都为空）：按索引匹配（第 0 个 feed 对应 Profile 第 0 个 serie）
- `type`：主要用于生成示例 JSON，在当前注入路径中不会强制改类型。
- `datas[]` 对应每个点：
  - `x/y/z/value` 数值
  - `id/name`（可选）
  - `useColor/color`（可选）

---

## 5. 代码调用方式

除了在 Inspector 中手动操作，你也可以在代码中调用：

```csharp
var injection = GetComponent<UIToolKitRuntimeJsonInjection>();

// 设置 JSON 内容
injection.JsonContent = yourJsonString;

// 应用到图表
injection.ApplyJsonToChart();

// 或者生成示例 JSON
injection.GenerateExampleJson();
string exampleJson = injection.JsonContent;
```

---

## 6. 常见问题与排错

- **不显示 / TryGetChartElement 失败**
  - 确认 `UIDocument` 组件存在且已正确配置
  - 确认 UXML 中有 `ChartElement`
  - 如果指定了 `Chart Element Name`，确认名称与 UXML 中一致

- **JSON 解析失败**
  - 组件会使用 `ChartJsonUtils.ValidateAndParseFeed` 进行验证
  - 会优先尝试 Newtonsoft（若项目里存在 `Newtonsoft.Json`）
  - 否则使用 Unity `JsonUtility`
  - 建议先用 **Generate Example JSON** 生成一份能解析的 JSON，再在它的基础上改

- **注入后 Series 对不上 / 更新错了线**
  - 优先使用 `serieId` 做稳定匹配
  - 如果只用 `name`，且同名 Serie 存在多个，脚本会使用第一个

- **担心修改原始 Profile 资产**
  - 组件会在首次注入时自动克隆 Profile，创建一个运行时副本
  - 克隆的 Profile 名称会带 `(Runtime)` 后缀
  - 原始资产不会被修改
