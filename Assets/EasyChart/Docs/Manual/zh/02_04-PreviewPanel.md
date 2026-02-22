# Preview 面板

本章说明 `Unity Easy Chart/Library Editor` 中间区域顶部的 **Preview** 面板。

Preview 的作用是：把当前选中的 `ChartProfile` 直接渲染出来，让你在编辑配置时能即时看到结果。

---

## Preview 会在什么时候刷新

Preview 刷新是“延迟刷新”（避免你连续拖动/输入时频繁重绘）：

- 当你在 **Inspector** 或 **Series** 面板修改任何绑定字段时，会触发一次 `ScheduleUpdatePreview()`。
- 当你在 **JSON Injection** 面板点击 **ApplyToChart** 后，会触发刷新。
- 当你在左侧 **Library** 树切换到另一个 `ChartProfile` 时，会刷新到新 Profile。

实现上会把刷新放到 `EditorApplication.delayCall`，因此你可能会感觉到“改完后稍后才更新”——这是预期行为。

---

## Preview 显示什么

- Preview 使用一个运行时的图表组件（例如 `ChartElement`）进行绘制。
- Preview 会直接读取当前选中 `ChartProfile` 的数据并渲染。

你可以把 Preview 理解为：

- **你编辑的就是它渲染的**
- **你看到的就是运行时的效果**（大多数情况下）

---

## 常见问题与排错

### 1) 预览为空

优先排查：

- 是否选中了一个 `ChartProfile`
- 是否至少存在一条 `Serie`
- `seriesData` 是否为空（没有数据点）

### 2) 数据有但显示很怪 / 看不到

常见原因：

- **坐标系与 SeriesType 不匹配**：例如 Profile 是 `Polar2D`，但 Series 选择了非 Radar 的类型。
- **轴范围/数据范围不匹配**：例如数值全都落在轴范围之外。
- **分类轴可见数量（VisibleCount）太小**：导致只显示一小段。

### 3) Console 报错 “Preview refresh failed”

当刷新过程中出现异常，会在 Console 输出：

- `[EasyChartLibraryWindow] Preview refresh failed: ...`

这通常意味着：

- 某个配置组合不合法
- 或某个字段值超出预期（例如 null / NaN）

建议处理：

- 先回退最近一次改动
- 再逐步改回去定位哪一个字段触发异常

---

## 提示

- Preview 只负责“呈现结果”，结构性问题通常需要回到 **Inspector/Series/JSON Injection** 去修。
- 如果你在短时间内修改了很多字段，Preview 可能在最后一次改动后才统一刷新（这是为了性能）。

---

## Help

- 点击标题栏最右侧 **Help** 图标可回到本章节。
