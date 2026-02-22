# 圆环图（RingChart）

本章目标：说明 RingChart 的真实含义（它不是 donut pie），并把它在 EasyChart 中的 `SeriesData` 字段解释、`RingChartSettings` 配置与 Pro/基础差异对齐到运行时代码。

---

## 1. RingChart 是什么？（非常重要）

在 EasyChart 当前实现中：

- `SerieType.RingChart` 渲染的是 **多条“进度环”**（每个数据点一条环）
- 每条环都是 **完整 360° 的底环 + 一段进度弧**
- 它不是“多个 slice 分割圆周”的饼图

如果你想要“占比构成”的圆环饼图（donut pie）：

- 目前更接近 `SerieType.Pie` + `layout.innerRadius > 0`
- 但推荐仍按你的设计决定：
  - 构成占比：用 Pie
  - 多指标进度/完成率：用 RingChart

---

## 2. 重要说明（Pro 功能）

- `SerieType.RingChart` 的渲染器由 `EasyChartProBootstrap` 注册。
- 未安装/启用 Pro 时，该 serie 通常不会显示。

---

## 3. 最小可用配置（Checklist）

1. 添加 1 条 `Serie`
   - `type = RingChart`
   - `settings = RingChartSettings`
   - `seriesData` 至少 1 个点
2. 每个点的 `value > 0`

> 注意：RingChart 会忽略 `value <= 0` 的点。

---

## 4. SeriesData 字段解释（按运行时代码）

RingChart 主要使用：

- `value`：环的进度“原始值”
- `name`：环的名称
- `useColor + color`：环颜色（点级颜色覆盖）
- `id`：稳定标识（用于 legend/隐藏状态，建议保持稳定）

### 4.1 Percent 模式（默认）：value 同时支持 0~1 和 0~100

当 `RingChartSettings.valueMapping.mode = Percent`（默认）时：

- `value <= 0`：该环会被过滤
- `0~1`：按比例（0.72 = 72%）
- `> 1`：按百分比（72 = 72%，运行时会除以 100）

建议：团队统一用一种写法（全 0~1 或全 0~100），避免误用。

### 4.2 Range 模式：把 value 映射到 0..1

当 `RingChartSettings.valueMapping.mode = Range` 时：

- 会先确定范围 `min/max`：
  - `autoRange=true`：从所有 ring 的 value 自动求范围
  - `autoRange=false`：使用 `minValue/maxValue`
- 再把 `value` 映射为 `(value-min)/(max-min)` 并 clamp 到 0..1

### 4.3 name 为空时的名称来源

当 `SeriesData.name` 为空时，RingChart 会尝试从 labels 兜底：

- 若 `ChartData.CoordinateSystem == None`：不会使用 labels 兜底，最终会退回到 `Ring {i}`
- 否则优先：`Data.PolarAxes.angleAxis.labels[i]`
- 再否则：Cartesian/任意 Category 轴的 `labels[i]`
- 最终兜底：`Ring {i}`

如果你不想依赖 PolarAxes 配置，建议直接填 `SeriesData.name`。

---

## 5. Inspector 对应字段（RingChartSettings）

- `series[i].type = RingChart`
- `series[i].settings`：实际类型为 `RingChartSettings`
  - `layout`：角度/半径/内外环/留白/中心偏移
  - `valueMapping`：Percent/Range 映射规则
  - `hover`：悬停强调（Translate/Pull/Color/Stroke）
  - `legend`：RingChart 的图例设置（纯 Pie 图表时生效）
  - `showBackground/backgroundAlpha/backgroundColor`：背景环
  - `cornerRadius`：端头圆角
  - `ringGapPx`：环与环间距

### 5.1 layout（RingChartLayoutSettings）

常用字段：

- `startAngleDeg`：起始角度
- `clockwise`：顺/逆时针
- `angleRangeDeg`：默认 360；可做“半环进度”
- `outerRadius`：外半径（<=0 自动；0~1 比例；>1 像素）
- `innerRadius`：内半径（0~1 比例或像素）
- `plot.padding`：留白（避免 hover/标签被裁剪）
- `plot.centerOffset`：中心偏移

### 5.2 hover（PieHoverSettings）

- `hover.enabled`：是否启用
- `hover.explodeType`：
  - `Translate`：整条环平移
  - `Pull`：拉伸（拉出）
  - `Color`：变亮
  - `Stroke`：描边强调
- `hover.explodeDistance`：平移/拉伸距离（像素）

### 5.3 背景环与间距

- `showBackground`：是否绘制背景环
- `backgroundAlpha`：背景环透明度（最终会乘到颜色 alpha 上）
- `backgroundColor`：背景环颜色（alpha=0 时会回退用 ring 本身颜色）
- `ringGapPx`：环与环的间距
- `cornerRadius`：端头圆角（受环厚度限制）

---

## 6. 图例与隐藏交互（与 Pie 共用 HiddenPieSliceIds）

- RingChart 与 Pie 共用 `ChartInteractionState.HiddenPieSliceIds`。
- 每条环的隐藏 key：优先 `SeriesData.id`，否则使用索引字符串。
- 图例条目 label 的来源受 `PieLegendSettings.source` 影响：
  - `RingSlice` 会优先从 `polarAxes.angleAxis.labels` 取名称。

---

## 7. 标签（SerieLabelSettings）

RingChart 的标签同样使用 `Serie.labelSettings`：

- `show`：是否显示
- `showName`：是否显示 name
- `decimalPlaces`：数值小数位（注意：这里显示的是原始 `value`，不是自动乘 100 的百分比文本）
- `position`：
  - `Outside`：外侧标签 + 引导线
  - `Center`：贴在环中间

---

## 6. 常见坑（按现象排查）

- **我以为它是 donut pie，但显示不对**
  - 这是多环进度图：每个点是一条“进度环”

- **进度不对（比如填 75 结果几乎满圈）**
  - `value>1` 会按百分比除以 100
  - 如果你想 75%：用 `0.75` 或 `75`

- **某些环不显示**
  - 检查 `value <= 0` 是否被过滤

- **交互/隐藏状态不稳定**
  - 确保 `SeriesData.id` 稳定

---

## 8. 深入参考

- 饼图（构成占比）：`15-PieChart.md`
- Series 数据结构：`00-WorkflowAndLibrary.md`
