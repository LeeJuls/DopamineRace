# 饼图（Pie）

本章目标：把饼图在 EasyChart 中“数据字段怎么解释、布局/聚合/图例/交互如何生效、哪些行为有隐藏前提”的规则讲清楚，并对应到 Inspector 字段。

---

## 1. 适用场景

- 展示“占比/构成”
- 强调每个分类在整体中的比例

不适合：

- 类别过多（通常 > 8~12 个会很难读）
- 需要精确对比微小差异（更建议条形图）

---

## 2. 最小可用配置（Checklist）

1. `coordinateSystem`：Pie 不依赖 Cartesian/Polar 坐标系（按你的 Profile 现有设置即可）
2. 添加 1 条 `Serie`：
   - `type = Pie`
   - `settings = PieSettings`
   - `seriesData` 至少 1 个点
3. 确保每个点的 `value > 0`

> 注意：Pie 当前会忽略 `value <= 0` 的点。

---

## 3. 重要限制（按运行时代码）

- **只绘制第一条可见 Pie serie**：当前渲染器会遍历 `Data.Series`，找到第一条 `type=Pie` 且可见的 serie 绘制后就 `break`。
- Pie 的切片隐藏状态来自 `ChartInteractionState.HiddenPieSliceIds`，会在图例点击时写入/移除。

---

## 4. SeriesData 字段解释（按运行时代码）

Pie 主要使用：

- `value`：该切片的数值（权重）
- `name`：切片名称（推荐填写）
- `useColor + color`：切片自定义颜色（可选）
- `id`：切片稳定标识（用于隐藏/交互状态，建议保持稳定）

### 4.1 最推荐写法：显式写 name + value

- `SeriesData.name = "苹果"`
- `SeriesData.value = 12`

### 4.2 name 为空时的名称来源

当 `SeriesData.name` 为空时，Pie 会尝试从 **labels** 获取名称，但有一个前提：

- 如果 `ChartData.CoordinateSystem == None`（通常是纯 Pie/无坐标系图），运行时会 **跳过 labels 兜底**，只使用 `SeriesData.name`。

在非 None 坐标系下，名称兜底顺序是：

- 优先使用 `Data.Cartesian.xAxisId` 对应的 Category 轴 labels
- 若找不到，再使用任意一个 Category 轴 labels
- 最后兜底：`Slice {index}`

> 因此：如果你不想依赖轴配置，建议直接填 `SeriesData.name`。

### 4.3 颜色来源

- 若点上 `useColor=true`：使用 `SeriesData.color`
- 否则：使用内置调色板按顺序分配

---

## 5. 常用配置（PieSettings）

Pie 的 `settings` 是 `PieSettings`，主要包含：

- `layout`：布局（角度/半径/间隙/居中偏移等）
- `hover`：悬停交互（explode）
- `aggregation`：聚合（TopN + Others）
- `legend`：Pie 专用图例配置（只在“纯 Pie 图表”场景下替代全局 legend）

### 4.1 layout（PieLayoutSettings）

常用字段：

- `startAngleDeg`：起始角度（默认 -90 让第一片从“正上方”开始）
- `clockwise`：顺时针/逆时针
- `angleRangeDeg`：角度范围（默认 360，做“半圆饼”可设 180 等）
- `outerRadius`：外半径
  - `<= 0`：自动
  - `0~1`：按控件大小比例
  - `> 1`：像素
- `innerRadius`：内半径（Pie 通常为 0；>0 会变成“中间有洞”的效果，但更推荐用 RingChart 类型做圆环）
- `innerRadiusColor`：内圈填充颜色
- `sliceGapPx`：切片间隙（像素）
- `sliceGapType`：间隙计算方式（Radial/Translate/Uniform）
- `cornerRadius`：圆角（像素，受切片厚度限制）
- `plot.padding`：留白（避免切片/外侧标签被裁剪）
- `plot.centerOffset`：中心偏移

### 4.2 hover（PieHoverSettings）

- `hover.enabled`：是否启用悬停交互
- `hover.explodeType`：
  - `Translate`：整体平移
  - `Pull`：拉伸（拉出）
  - `Color`：变亮
  - `Stroke`：描边强调
- `hover.explodeDistance`：平移/拉伸距离（像素）

### 4.3 aggregation（PieAggregationSettings）

当分类很多时，可以把小项合并为 `Others`：

- `aggregation.enabled = true`
- `keepTopN`：保留前 N 个，其余合并
- `sortByValue`：是否按 `value` 值排序后再取 TopN
- `othersName`：Others 的名称
- `useOthersColor + othersColor`：Others 颜色

> 注意：聚合只在 `keepTopN > 0` 且切片数量超过 N 时生效。

---

## 6. 图例（PieLegendSettings）与“隐藏切片”交互

当图表是“纯 Pie 图表”（只包含 Pie/RingChart/Pie3D 且没有其它类型）时：

- 图例会优先使用 `PieSettings.legend`（或 RingChartSettings/Pie3DSettings 上的 legend），而不是 `ChartData.legend`。
- 点击图例条目会切换 `HiddenPieSliceIds`：
  - 普通切片：`SeriesData.id`（若为空则用索引字符串）
  - 聚合的 Others：固定使用 `__ec_pie_others__`

`PieLegendSettings.source` 会影响“图例条目从哪里来”：

- `Slice`：每个切片一条（默认）
- `RingSlice`：为 RingChart/RingSlice 场景提供 label 来源（优先 PolarAxes.angleAxis.labels）
- `Series`：每条 serie 一条（不再是切片级）

---

## 7. 标签（SerieLabelSettings）

Pie 标签由 `Serie.labelSettings` 控制：

- `show`：是否显示
- `fontSize / color / decimalPlaces`：字体与数值格式
- `showName`：是否在标签里显示切片名称
- `position`：`Outside/Inside/Center`
- `offset`：偏移

---

## 8. 常见坑（按现象排查）

- **某些切片不显示**
  - 检查该点 `value` 是否 `<= 0`

- **切片名称不是我想要的**
  - 推荐：直接填写 `SeriesData.name`
  - 如果依赖 labels：确保你确实有一个 Category 轴并填写了 `labels`，且顺序与数据点索引一致

- **切片颜色每次不一样/难以控制**
  - 对需要固定颜色的切片：给该点设置 `useColor=true` + `color`

- **隐藏/交互状态不稳定**
  - 确保每个点的 `SeriesData.id` 稳定（不要每次刷新都重新生成一套新 id）

---

## 9. 下一章

- 圆环图（RingChart）：`16-RingChart.md`
