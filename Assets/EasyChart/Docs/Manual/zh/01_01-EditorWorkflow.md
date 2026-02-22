# 编辑器工作流与面板说明

本章目标：让你清楚在 `EasyChart/Library Editor` 里“**哪里改什么**”，以及常见编辑流程（创建、克隆、预览、导出）。

---

## 1. 你在编辑的是什么？（ChartProfile）

在 Library Editor 里被选中的图表，本质上是一个 `ChartProfile` 资产。

- 它是 **可复用配置**：同一个 Profile 可以被多个场景/Prefab 引用。
- 它是 **可预览配置**：在编辑器里修改后可以立即看到预览变化。

建议：将你自己的 Profile 放到 `Assets/EasyChart/Library/Custom/`（或你团队约定目录）。

---

## 2. Library Editor 的三个核心区域

虽然 UI 细节可能随版本略有变化，但整体可以按下面理解：

### 2.1 左侧：资源树（Library Tree）

这里展示的是图表资产（`ChartProfile`）所在的文件夹结构。

常用操作：

- 在文件夹上右键：
  - `New Folder...`：新建子目录
  - `New Chart...`：创建新的 `ChartProfile`
- 在图表上右键：
  - `Clone`：复制一个新的 Profile（用于做变体）
  - `Export to UXML`：导出（若你的工作流需要把配置同步/落盘到 UXML）
  - `Ping`：在 Project 面板定位该资产
  - `Rename...` / `Delete`

> 建议：用 `Clone` 来做“同款不同配色/不同数据规模”的图表变体，避免从零配置。

### 2.2 右侧：Inspector（配置面板）

这里是你主要编辑的地方。

通常会包含：

- **基础设置**：
  - `coordinateSystem`
  - `padding`（如果有）
  - `animationDuration`（如果有）
- **Series 列表**：每个 Serie 代表一条线/一组柱/一个散点序列等
- **Axes（坐标轴）**：
  - 选择使用哪个 `XAxisId/YAxisId`
  - 对应 `AxisConfig` 的显示、label、range、ticks 等
- **Legend / Tooltip / Grid**：若你的版本已暴露这些设置

> 经验：先把 `coordinateSystem`、`Series`、`Axes` 配好，其他属于“锦上添花”。

### 2.3 预览区（Preview）

用于检查：

- 是否有数据
- 轴范围是否正确
- 标签是否拥挤/偏移
- Tooltip / Legend 的交互（如果启用）

预览出现异常时优先排查顺序：

1. `coordinateSystem` 是否正确
2. `series` 是否至少有 1 条且有数据点
3. Axis 的 `axisType` 与数据 `x/y` 的含义是否匹配
4. Value 轴是否被手动锁死范围（`autoRangeMin/autoRangeMax`）

### 2.4 JSON Injection（JSON 注入面板）

在左侧面板下方有一个 **JSON Injection** 区域，用于：

- 快速生成当前选中 `ChartProfile` 的“注入 JSON 示例”
- 将你粘贴/编辑的 JSON 应用回当前选中 Profile（`ApplyToChart`）

常用控件：

- **API Envelope**：是否使用 `{ code, message, data }` 外层包裹。
  - 打开时：生成 JSON 会包一层；解析时也可以识别并自动取 `data`。
- **Feed Mode**：示例 JSON 的“结构层级/字段完整度”（用于兼容不同注入协议）。
- **Datas Format**：`datas` 字段内部数据点格式（例如更紧凑的数组，或更易读的对象）。
- **ApplyToChart**：将当前文本框中的 JSON 解析并写回到当前选中的 `ChartProfile`。

---

## 3. 推荐的编辑流程（从 0 到可复用）

### Step 1：创建或选择一个 ChartProfile

- 新建：在目标文件夹右键 `New Chart...`
- 已有：在左侧树点击选择

如果你希望使用“先克隆再修改”的方式（更推荐）：

- 先在窗口顶部工具栏 **Clone** 一个你自己的 Library
- 然后在你自己的库里右键 Profile -> `Clone` 生成变体

### Step 2：确定坐标系

- `Cartesian2D`：Line/Bar/Scatter/Heatmap
- `Polar2D`：Radar

> 建议：坐标系先定下来，再选 SeriesType，避免后续切换带来风格/轴设置混淆。

### Step 3：配置 Series

- 添加 Series
- 设置 `type`
- 填充 `seriesData`

建议：先用少量数据点（3~8 个）把效果跑通，再扩展数据量。

### Step 4：配置 Axes

最常见组合：

- X：Category
  - `labels` 填文本
- Y：Value
  - 开启自动范围（默认）

当你希望更专业的轴显示：

- 使用 `labelFormat`（例如 `F1`、`N0` 等）
- 使用 `autoRangeMin/autoRangeMax` 只锁定一端范围
- 如果是 Value 轴需要单位：
  - `showUnit=true`
  - `unitText="个"/"万"`
  - `unitLabelStyle` 调字体/颜色/位置

### Step 5：克隆出变体（推荐）

当你需要同款图表做多个版本（配色、字号、轴显示略不同）：

- 右键图表 -> `Clone`
- 修改差异项

这样可以保证风格一致，也更便于版本管理。

当你想把图表用于 UI：

- UI Toolkit：导出为 UXML，然后在 UI Builder 中组装页面（可参考示例场景 `Assets/EasyChart/Demo/Scenes/EasyChart_QuickStart.unity` 与模板 `Assets/EasyChart/Demo/UIToolKit/NewUXMLTemplate.uxml`）
- UGUI：可导出为 UGUI Prefab 并在 Canvas/RectTransform 工作流中使用

---

## 4. 常见坑（快速定位）

- **看不到任何东西**
  - `seriesData` 是否为空
  - `AxisType` 是否和数据含义匹配（Category 轴配了 labels 吗）

- **Value 轴显示很怪（范围太大/太小）**
  - 检查 `autoRangeMin/autoRangeMax`
  - 检查 rounding/unit 是否把范围吸附到不合适的单位上

- **柱状图与标签不对齐**
  - 关注 `LabelPlacement`（Tick vs CellCenter）

---

## 下一章

- `00-WorkflowAndLibrary.md`：轴类型、标签放置、自动范围、取整、单位显示等内容已合并到第 7 节
