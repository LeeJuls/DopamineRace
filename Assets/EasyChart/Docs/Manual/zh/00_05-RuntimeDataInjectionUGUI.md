# 运行时数据注入（UGUI）

对应脚本：`UGUIRuntimeJsonInjection`、`UGUIRuntimeJsonInjectionEditor`

本章介绍：在 UGUI 工作流（`UGUIChartBridge`）下，如何通过 JSON 在运行时把数据注入到图表中。

---

## 1. 这套方案适合什么场景？

- 你有一份来自服务器/业务层的 JSON（或你希望在运行时快速手工编辑 JSON）
- 你希望像编辑器里 `JSON Injection` 面板一样，直接“生成示例 → 修改 → 应用”
- 你已经通过 `ChartProfile` 把样式、轴、Series 类型等结构配置好了

这套注入逻辑的定位是：**更新数据为主**，结构变更（比如新增 Series、强行覆盖 Series 类型）不是它的主要目标。

---

## 2. 快速上手（最推荐的流程）

1. 在场景中搭好 `UGUIChartBridge`（并确保 `Profile` 已赋值）。
2. 在同一个 GameObject 上添加组件：`UGUIRuntimeJsonInjection`。
3. 点击 **Generate Example JSON** 生成一份与你当前 Profile 匹配的示例 JSON。
4. 在 `JSON Content` 文本框里修改数据。
5. 点击 **Apply JSON to Chart**。

你会看到组件内部：

- 解析 JSON → 转成 `ChartFeed`
- 将 `ChartFeed` 应用到 `UGUIChartBridge.Profile`
- 调用 `_bridge.Refresh()` 刷新图表

---

## 3. 组件与 Inspector 字段说明

`UGUIRuntimeJsonInjection` 必须和 `UGUIChartBridge` 在同一个物体上（脚本有 `[RequireComponent(typeof(UGUIChartBridge))]`）。

### 3.1 JSON Generation Settings

- **Example Mode（`ChartJsonExampleMode`）**
  - 控制“生成示例 JSON”时的格式。
  - 一般建议先用 `Standard` 或 `Standard_Axis`（更直观）。

- **Data Mode（`ChartJsonDatasMode`）**
  - 控制 `datas` 的数据表达方式。
  - `Standard`：`datas` 为对象数组（例如 `{ "x": 0, "value": 12 }`）。
  - `Values`：`datas` 为纯数值数组（更短）。
    - 备注：该格式需要走 `ChartJsonUtils` 的“灵活解析器”（基于 Newtonsoft 的反射解析）。如果你的项目里没有 Newtonsoft（`Newtonsoft.Json` / `Unity.Newtonsoft.Json`），可能会解析失败。
    - 因此 **推荐默认使用 `Standard`**，除非你确认项目已引入 Newtonsoft。

- **API Envelope（`UseApiEnvelope`）**
  - 生成示例 JSON 时，是否包一层接口返回壳：
    - `{ "code": 200, "message": "success", "data": { ...真正的ChartFeed... } }`
  - 应用时也会尝试自动从壳里提取 `data`。

- **Auto Generate（`AutoGenerateJson`）**
  - 当你切换 `Example Mode / Data Mode / API Envelope` 时，自动重新生成示例 JSON。

### 3.2 JSON Content

- **JSON Content（`JsonContent`）**
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

字段对应代码：

- `chartId` / `chartName`
  - 在 `UGUIRuntimeJsonInjection` 的注入路径里 **不会覆盖** Profile 的 `chartId/chartName`（它调用 `ChartJsonUtils.ApplyFeedToProfile(profile, feed)`，内部 `allowMetaOverwrite=false`）。
  - 但这两个字段可以用来帮助别的注入器（例如 `EasyChartDataSource`）在 UI 树里“按名字/ID 寻找 ChartElement”。

- `axes[]`
  - `axisId` 为 `AxisId` 枚举（如 `XBottom`、`XTop`、`YLeft`、`YRight` 等）。
  - `labels` 存在时会把该轴视为 Category，并直接覆盖 labels。

- `series[]`
  - **匹配优先级**：
    - 如果给了 `serieId`：按 `Serie.id` 精确匹配
    - 否则如果给了 `name`：按 `Serie.name` 匹配
    - 否则（`serieId` 与 `name` 都为空）：按索引匹配（第 0 个 feed 对应 Profile 第 0 个 serie）
  - `type`
    - 主要用于生成示例 JSON。
    - 在当前注入路径中：
      - 对已存在且能匹配到的 Serie：**不会强制改类型**（因为这里不允许覆盖 meta）。
      - 对“索引模式 + 超出 Profile 数量”而新建出来的 Serie：会使用 feed 里的 `type` 作为新 Serie 的类型。
  - `datas[]` 对应每个点：
    - `x/y/z/value` 数值
    - `id/name`（可选）
    - `useColor/color`（可选）

---

## 5. 应用时发生了什么？（注入流程）

点击 **Apply JSON to Chart** 时：

1. 若 JSON 是 API 壳（含 `data` 字段），会先尝试把 `data` 里的对象抽出来。
2. 调用 `ChartJsonUtils.TryDeserializeFeed(json, out feed)` 反序列化为 `ChartFeed`。
   - 会优先尝试 Newtonsoft（如果项目里有），否则回退到 Unity `JsonUtility`。
   - `type: "Line"` / `axisId: "XBottom"` 这类字符串，也会在回退路径中被规范化为枚举值再解析。
3. 调用 `ChartJsonUtils.ApplyFeedToProfile(_bridge.Profile, feed)` 把 feed 写回 Profile。
4. 调用 `_bridge.Refresh()` 触发重绘。

---

## 6. 常见问题与排错

- **点击 Apply 没反应 / 控制台有 warning：No UGUIChartBridge or ChartProfile found**
  - 确认对象上有 `UGUIChartBridge`
  - 确认 `UGUIChartBridge.Profile` 已赋值

- **报错：Failed to parse JSON**
  - 先用 Generate 生成一份能解析的 JSON，再在它的基础上改。
  - 如果你的接口返回有外层包裹，优先勾选 `API Envelope`，或确保 JSON 的 `data` 字段内才是 `ChartFeed`。

- **JSON 生效了但数据没变 / 只变了一部分**
  - 检查 `series` 的匹配方式（`serieId` / `name` / 索引模式）。
  - 如果你使用的是 `serieId/name` 匹配：确保 Profile 里确实存在对应的 Serie（该注入路径在这种模式下不会自动创建新 Serie）。
  - 如果你使用的是“索引模式”（`serieId` 与 `name` 都为空）：
    - 当 feed 的 `series[]` 数量 **超过** Profile 的 Series 数量时，会自动补创建新的 Serie。
    - 如果你不希望自动创建，请给每条 serie 明确填 `name` 或 `serieId`。

- **在编辑器 PlayMode 注入后，Profile 资产被改脏了**
  - 注入的本质是“把 feed 应用到 `ChartProfile` 上”。如果你把资产直接拖到桥接上，运行时改动可能会让该资产处于 dirty 状态。
  - 如果你不希望影响资产，建议在运行时对 Profile 做一份实例化拷贝再注入。
