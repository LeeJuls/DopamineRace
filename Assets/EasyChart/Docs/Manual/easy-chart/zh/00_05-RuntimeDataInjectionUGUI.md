# 运行时数据注入（UGUI）

本章介绍：在 UGUI 工作流（`UGUIChartBridge`）下，如何通过 JSON 在运行时把数据注入到图表中。

对应脚本：`UGUIRuntimeJsonInjection`

---

## 1. 这套方案适合什么场景？

- 你有一份来自服务器/业务层的 JSON（或你希望在运行时快速手工编辑 JSON）
- 你希望像编辑器里 `JSON Injection` 面板一样，直接"生成示例 → 修改 → 应用"
- 你已经通过 `ChartProfile` 把样式、轴、Series 类型等结构配置好了

这套注入逻辑的定位是：**更新数据为主**，结构变更（比如新增 Series、强行覆盖 Series 类型）不是它的主要目标。

---

## 2. 快速上手（最推荐的流程）

1. 在场景中搭好 `UGUIChartBridge`（并确保 `Profile` 已赋值）。
2. 在同一个 GameObject 上添加组件：`UGUIRuntimeJsonInjection`（菜单：`Add Component > EasyChart > UGUI Runtime JSON Injection`）。
3. 点击 **Generate Example JSON** 生成一份与你当前 Profile 匹配的示例 JSON。
4. 在 `JSON Content` 文本框里修改数据。
5. 点击 **Apply JSON to Chart**。

组件内部会：

- 首次注入时自动克隆 Profile（避免修改原始资产）
- 解析 JSON → 转成 `ChartFeed`
- 将 `ChartFeed` 应用到克隆的 Profile
- 调用 `_bridge.Refresh()` 刷新图表

---

## 3. 组件与 Inspector 字段说明

`UGUIRuntimeJsonInjection` 必须和 `UGUIChartBridge` 在同一个物体上（脚本有 `[RequireComponent(typeof(UGUIChartBridge))]`）。

### 3.1 JSON Generation Settings

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

### 3.2 JSON Content

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
var injection = GetComponent<UGUIRuntimeJsonInjection>();

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

- **点击 Apply 没反应 / 控制台有 warning：No UGUIChartBridge or ChartProfile found**
  - 确认对象上有 `UGUIChartBridge`
  - 确认 `UGUIChartBridge.Profile` 已赋值

- **报错：Failed to parse JSON**
  - 组件会使用 `ChartJsonUtils.ValidateAndParseFeed` 进行验证
  - 先用 Generate 生成一份能解析的 JSON，再在它的基础上改
  - 如果你的接口返回有外层包裹，优先勾选 `Use Api Envelope`，或确保 JSON 的 `data` 字段内才是 `ChartFeed`

- **JSON 生效了但数据没变 / 只变了一部分**
  - 检查 `series` 的匹配方式（`serieId` / `name` / 索引模式）
  - 如果你使用的是 `serieId/name` 匹配：确保 Profile 里确实存在对应的 Serie
  - 如果你使用的是"索引模式"（`serieId` 与 `name` 都为空）：
    - 当 feed 的 `series[]` 数量 **超过** Profile 的 Series 数量时，会自动补创建新的 Serie
    - 如果你不希望自动创建，请给每条 serie 明确填 `name` 或 `serieId`

- **担心修改原始 Profile 资产**
  - 组件会在首次注入时自动克隆 Profile，创建一个运行时副本
  - 克隆的 Profile 名称会带 `(Runtime)` 后缀
  - 原始资产不会被修改
