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

- **Min/Max**（图标按钮）
  - 点击切换面板高度：展开(□) / 收起(-)
  - 收起时面板高度较小（更偏“辅助工具”）
  - 展开时面板高度较大（更适合长 JSON）

- **ApplyToChart**（图标按钮）
  - 把当前文本框里的 JSON 尝试解析为 Feed，并应用到选中的 `ChartProfile`。
  - 成功后会：
    - 标记资产为 Dirty 并 `SaveAssets()`
    - 刷新 Series 列表
    - 刷新 Preview

- **Help**（图标按钮）
  - 点击打开本文档章节。

---

## 控件说明（按钮行）

标题栏下方还有一行按钮（可能会自动换行）：

- **API Envelope**（图标开关）
  - 控制示例 JSON 是否包裹为“API 返回格式”。
  - 你需要把 Feed 直接交给某个 HTTP API/服务时，这个选项会更方便。
  - 切换后会重新生成示例，并覆盖文本框（详见“覆盖规则”）。

- **Feed Mode**（下拉框）
  - 用于控制"示例 JSON 输出包含哪些层级/字段"。
  - 选项：
    - `Compact`：仅数据值，最小负载。格式为 `{"series": [{"datas": [1, 2, 3]}]}` 
    - `Standard`：名称 + 结构化数据对象。包含 `chartName`、`series[].name` 和 `datas` 对象数组
    - `Full`：所有元数据包括轴、类型、颜色等。适用于完整复制/迁移图表配置
  - 一般建议：
    - **快速更新数据**：用 `Compact`
    - **一般编辑与 Apply**：用 `Standard`
    - **完整复制/迁移**：用 `Full`

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

---

## 运行时 JSON Injection 组件

除了编辑器面板，EasyChart 还提供运行时 JSON Injection 组件，用于在游戏运行时动态更新图表数据。

### 组件位置

- **UGUI**: `EasyChart/UGUI Runtime JSON Injection`
- **UIToolKit**: `EasyChart/UI Toolkit Runtime JSON Injection`

### 主要属性

| 属性 | 说明 |
|------|------|
| **Chart Element Name** (UIToolKit) | 可选。指定要更新的 ChartElement 名称。支持直接查找 ChartElement，或查找 TemplateContainer 后获取内部 ChartElement。 |
| **JSON Mode** | JSON 生成模式：`Compact`（仅数据值）、`Standard`（名称+结构化数据）、`Full`（所有元数据） |
| **API Envelope** | 是否将 JSON 包裹为 API 响应格式 |
| **Auto Generate** | 开启后，修改 JSON Mode 或 API Envelope 时自动重新生成示例 JSON |
| **JSON Content** | 要注入的 JSON 字符串，可直接编辑或通过代码设置 |

### 主要方法

#### `ApplyJsonToChart()`
解析 `_jsonContent` 中的 JSON 并应用到图表。

```csharp
var injection = GetComponent<UIToolKitRuntimeJsonInjection>();
injection.JsonContent = jsonFromServer;  // 设置 JSON
injection.ApplyJsonToChart();            // 应用到图表
```

#### `ApplyFeed(ChartFeed feed)` ⭐ 推荐用于定期刷新
直接应用已解析的 Feed 对象，**跳过 JSON 解析**，性能更高。

```csharp
// 首次解析 JSON
var result = ChartJsonUtils.ValidateAndParseFeed(json);
if (result.IsValid) {
    _cachedFeed = result.Feed;  // 类型为 ChartFeed
}

// 后续定期更新数据（每帧/每秒调用）
injection.ApplyFeed(_cachedFeed);
```

#### `GenerateExampleJson()`
基于当前图表配置生成示例 JSON。返回生成的 JSON 字符串。

```csharp
string exampleJson = injection.GenerateExampleJson();
// 可用于了解当前图表的数据结构
```

### 事件回调

```csharp
injection.OnJsonApplied += (success, message) => {
    if (success) {
        Debug.Log("数据更新成功！");
    } else {
        Debug.LogError("更新失败：" + message);
    }
};
```

### UIToolKit 多图表支持

当页面有多个图表时，通过 **Chart Element Name** 指定目标：

```xml
<!-- UXML 示例 -->
<ui:Instance template="BarDemo" name="Bar1" />
<ui:Instance template="PieDemo" name="Pie1" />
```

```csharp
// 脚本中指定名称
injection.ChartElementName = "Bar1";  // 更新 Bar1
injection.ApplyJsonToChart();

injection.ChartElementName = "Pie1";  // 更新 Pie1
injection.ApplyJsonToChart();
```

**查找逻辑**：
1. 先尝试直接查找 name 匹配的 `ChartElement`
2. 如果未找到，尝试查找 name 匹配的 `VisualElement`，再从中获取 `ChartElement`

### 定期刷新示例

```csharp
public class ChartDataRefresher : MonoBehaviour
{
    public UIToolKitRuntimeJsonInjection injector;
    private ChartFeed _feed;  // 或 ChartJsonFeed -> ChartFeed
    
    void Start()
    {
        // 初始解析 JSON
        string initialJson = LoadInitialData();
        var result = ChartJsonUtils.ValidateAndParseFeed(initialJson);
        if (result.IsValid) _feed = result.Feed;
        
        // 每 2 秒刷新
        InvokeRepeating(nameof(UpdateChartData), 2f, 2f);
    }
    
    void UpdateChartData()
    {
        // 从服务器获取新数据
        var newData = FetchFromServer();
        
        // 更新 feed 中的数值
        for (int i = 0; i < _feed.data.Count; i++)
        {
            _feed.data[i].value = newData[i];
        }
        
        // 高效应用（无 JSON 解析开销）
        injector.ApplyFeed(_feed);
    }
}
```

### 注意事项

1. **Profile 克隆**：首次注入时会自动克隆 Profile 以避免修改原始资源
2. **多图表切换**：UIToolKit 版本支持动态切换 `ChartElementName` 来更新不同图表
3. **错误处理**：使用 `OnJsonApplied` 事件捕获解析或应用错误
4. **性能优化**：高频刷新请使用 `ApplyFeed()` 而非 `ApplyJsonToChart()`
