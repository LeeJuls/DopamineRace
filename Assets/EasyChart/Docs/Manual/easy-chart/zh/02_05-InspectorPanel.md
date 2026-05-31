# Inspector 面板

本章说明 `Unity Easy Chart/Library Editor` 中间区域底部的 **Inspector** 面板。

Inspector 的定位是：以“配置视角”直接编辑当前选中 `ChartProfile` 的序列化字段（坐标系、轴、网格、交互、图例等），并驱动 Preview 实时更新。

---

## 面板结构概览

当你在左侧 Library 树选中一个 `ChartProfile` 后，Inspector 会构建一组折叠面板（Foldout），通常包括：

- **Chart Settings**：图表基础设置（例如背景、名称等）
- **Coordinate System**：坐标系选择与相关项
- **Axis Settings**：轴配置（X/Y 或 Angle/Radius）
- **Grid Settings**：网格相关配置
- **Hover Settings**：悬停/提示相关配置
- **Legend Settings**：图例配置

提示：

- 如果你选中的是文件夹而不是 `ChartProfile`，Inspector 会清空（这是正常的）。

---

## Chart Settings（常用）

### Chart Name

Inspector 里有 `Chart Name` 文本框（来自 `ChartProfile.chartName`）。它不只是显示名，还会参与资产重命名流程：

- 当你在该字段输入新名字并 **失去焦点** 或按 **Enter**：
  - 编辑器会尝试把 `.asset` 文件重命名为该名字
  - 同时尽量保持 `profile.name` 与 `profile.chartName` 同步

注意：

- 名字会被做文件名清理（非法字符会被移除/替换）。
- 如果重命名失败（例如同名冲突），会弹出 Error，并回退字段。

### Background

`Background` 通常是一个子折叠块，包含背景颜色/透明度等字段（具体以版本为准）。

---

## Coordinate System（坐标系）

这里会显示 `coordinateSystem` 选择项。

坐标系会影响：

- Series 可选的类型/语义（例如 Polar2D 常见是 Radar；Pie 类属于特殊布局）
- Axis Settings 中显示的是 Cartesian（X/Y）还是 Polar（Angle/Radius）配置

建议：

- 在开始配置前先确定坐标系，避免后续大规模调整。

---

## Axis Settings（轴配置）

### 轴选择（X Axis / Y Axis）

在 Cartesian 模式下，顶部会提供 X/Y 轴的选择下拉：

- **X Axis**：例如 `XBottom` / `XTop`
- **Y Axis**：例如 `YLeft` / `YRight`

选择改变时会：

- 确保轴列表里存在对应 AxisId 的元素（必要时自动创建）
- 刷新下面的 Axis 字段 UI
- 触发 Preview 刷新

### 单个 Axis 的常见字段

每个 Axis 配置通常包含：

- `axisType`：Category / Value 等
- `visible`：是否显示
- `color` / `width`：轴线样式

#### LabelTexts（分类轴标签）

Inspector 会提供一个 `LabelTexts`（内部字段名 `labels`）用于配置分类标签。

#### Range（范围）

常见有：

- `minValue` / `maxValue`
- `autoRangeMin` / `autoRangeMax`（开关）
- `autoRangeRounding`（自动范围的取整策略）
- `autoRangeUnit`（某些取整策略下会出现）

#### Ticks / VisibleCount

如果轴支持自动刻度：

- `autoTicks` 开启时会显示 `splitCount`。
- 对 **Category Axis** 来说，这个字段会显示为 **VisibleCount**（表示可见分类数量）。

#### Category Auto Scroll（分类轴自动滚动）

如果轴支持分类滚动，常见字段包括：

- `categoryAutoScroll`：是否自动滚动（跑马灯效果）。
- `categorySmoothScroll`：是否平滑滚动。
- `categoryScrollInterval`：滚动间隔。
- `categoryScrollStep`：每次滚动步长。

#### Unit（单位显示）

常见字段：

- `showUnit`：是否显示单位。
- `unitText`：单位文本（例如 `ms`/`%`/`MB`）。
- `unitLabelStyle`：单位文本样式。

---

## Polar Axis（极坐标轴）

当 `coordinateSystem = Polar2D` 时，Axis Settings 会显示 `polarAxes`：

- **Angle Axis**（angleAxis）
- **Radius Axis**（radiusAxis）

常见字段含义与 Cartesian 类似：

- `labels`：角度/维度标签（Radar 的维度名称通常来自这里）。
- `visible/color/width`：轴线样式。
- `showLabels/fontSize/labelColor/labelPosition/labelOffset`：标签显示控制。
- `autoRangeMin/autoRangeMax/minValue/maxValue`：半径轴范围。
- `autoTicks/splitCount`：刻度数量。

---

## Grid Settings（字段说明，仅 Cartesian2D）

Grid Settings 在 Cartesian2D 下可见，核心字段来自 `cartesianGrid`：

- **xGridColor / xGridLineWidth**：X 方向网格线颜色与线宽。
- **yGridColor / yGridLineWidth**：Y 方向网格线颜色与线宽。

如果需要虚线：

- `xGridDashed` / `yGridDashed`：是否虚线。
- `xGridDashLength` / `yGridDashLength`：虚线实线段长度。
- `xGridDashGap` / `yGridDashGap`：虚线间隔。
- `xGridDashOffset` / `yGridDashOffset`：虚线偏移。

---

## Hover Settings（字段说明，仅 Cartesian2D）

Hover Settings 在 Cartesian2D 下可见，核心字段来自 `hover`：

- **cursorLineColor**：悬停光标线颜色。
- **cursorLineWidth**：线宽。
- **cursorLineDashed**：是否虚线。
- **cursorLineDashLength / cursorLineDashGap / cursorLineDashOffset**：虚线参数。

---

## Legend Settings（字段说明）

Legend Settings 来自 `legendSettings`（某些情况下会被自动隐藏，见下文）。

- **enabled**：是否显示图例。
- **position**：图例位置（Top/Bottom/Left/Right）。
- **fontSize / color**：文字大小与颜色。
- **backgroundColor**：图例背景色。
- **itemSpacing**：图例项间距。
- **offset**：相对边缘的偏移。
  - 当 offset 为默认值时，会随着 position 自动给一个常用偏移（例如 Bottom 默认 `y=-30`）。

---

## Legend Settings（可能会自动隐藏）

当图表是“纯 Pie 系列”（只有 Pie/Ring/Pie3D，没有非 Pie 系列）时，Legend Settings 可能会被自动隐藏。

这是为了避免在某些布局下显示无意义或冲突的图例配置。

---

## 编辑建议与排错

- **改动较多时**：建议配合顶部工具栏的 `Save` 保存资产。
- **修改了关键结构**（例如坐标系、轴类型、Series Type）：
  - 改完观察 Preview 是否正确刷新
  - 如出现不一致，尝试切换一下选中 Profile 触发重建

---

## Help

- 点击标题栏最右侧 **Help** 图标可回到本章节。
