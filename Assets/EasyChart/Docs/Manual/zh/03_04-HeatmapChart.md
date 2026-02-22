# 热力图（Heatmap）

本章目标：把热力图在 EasyChart 里的“坐标/格子如何映射、`SeriesData` 字段怎么解释、颜色范围怎么算”的规则讲清楚，并标明它是 Pro 功能。

---

## 1. 适用场景

- 二维矩阵数据展示（行/列）
- 密度/强度可视化

---

## 2. 重要说明（Pro 功能）

- `SerieType.Heatmap` 的渲染器由 `EasyChartProBootstrap` 注册。
- 如果没有安装/启用 EasyChartPro：该 serie 会被当作“动态渲染器”尝试创建，但通常不会显示。

---

## 3. 最小可用配置（Checklist）

1. `ChartProfile.coordinateSystem = Cartesian2D`
2. 轴（Axis Settings）
   - 最常用：X=Category（列），Y=Category（行）
   - 也支持 X/Y 使用 Value 轴（见第 6 节）
3. Series
   - 添加 1 条 `Serie`
   - `Serie.type = Heatmap`
   - `Serie.seriesData` 至少 1 个点

---

## 4. Inspector 对应字段

- **Series**
  - `series[i].type = Heatmap`
  - `series[i].settings`：实际类型为 `HeatmapSettings`
    - `renderMode`：Grid / Gradient / Contour
    - `cellGapPx`
    - `xSplitCount` / `ySplitCount`（当 X/Y 使用 Value 轴时用于分格）
    - `autoRange / minValue / maxValue`
    - `lowColor / midColor / highColor`
    - `clamp`
    - `influenceMode`：None / Bleed / Smooth
    - `bleed / smooth / gradient / contour` 子配置

---

## 5. SeriesData 字段解释（按运行时代码）

热力图每个数据点对应一个“格子/像素区域”，运行时使用：

- **X 坐标（列）**：`SeriesData.x`
- **Y 坐标（行）**：`SeriesData.y`
- **强度值**：`SeriesData.value`
- **颜色覆盖**：如果 `SeriesData.useColor = true`，则直接用 `SeriesData.color`，跳过 `low/mid/high` 的插值。

> 注意：Heatmap 的 `x/y` 不接受字符串类目；Category 轴场景下仍然用“索引”。

---

## 6. 标准模板：二维 Category（X/Y）+ value 强度（最常用）

### 6.1 X 轴（Category：列）

- `AxisType = Category`
- `labels = ["Col0","Col1",...]`

### 6.2 Y 轴（Category：行）

- `AxisType = Category`
- `labels = ["Row0","Row1",...]`

### 6.3 数据写法

- `x = 列索引`（运行时会对 `x` 做 `RoundToInt`）
- `y = 行索引`（运行时会对 `y` 做 `RoundToInt`）
- `value = 强度`

### 6.4 重要细节：Category 轴下“格子数”与 `labelPlacement`

运行时会用 Axis 的 `labelPlacement` 决定“按 labels.Count 分格”还是“按 labels.Count-1 分格”：

- `CategoryLabelPlacement.CellCenter`
  - X 方向格子数 = `labels.Count`
  - Y 方向格子数 = `labels.Count`

- 其他（非 CellCenter）
  - X 方向格子数 = `max(1, labels.Count - 1)`
  - Y 方向格子数 = `max(1, labels.Count - 1)`

这会直接影响你应该填的 `x/y` 索引范围。

---

## 7. Value 轴热力图（X/Y 为数值轴）

当 X 或 Y 使用 `AxisType.Value` 时：

- 格子数量不再来自 labels，而来自：
  - X：`HeatmapSettings.xSplitCount`
  - Y：`HeatmapSettings.ySplitCount`

- `SeriesData.x/y` 会先根据 `_xMin/_xMax`、`_yMin/_yMax` 归一化，再映射到格子索引。

这适合做“连续值域上的密度/强度分布”。

---

## 8. 常见坑与排错

- **全部一个颜色 / 对比不明显**
  - 检查 `HeatmapSettings.autoRange` 是否开启
  - 或者手动设定 `minValue/maxValue`
  - 也检查是否所有点的 `value` 都几乎一样

- **颜色不按 low/mid/high 来**
  - 检查是否某些点启用了 `useColor=true`（会覆盖调色盘插值）

- **格子对不上（索引越界/偏一格）**
  - 检查 Category 轴的 `labelPlacement` 是否为 `CellCenter`
  - 根据第 6.4 节确定正确的格子数与索引范围

- **格子缝太大/太密**
  - 调 `HeatmapSettings.cellGapPx`

---

## 9. 深入参考

- 轴与范围、Series 与数据：`00-WorkflowAndLibrary.md`
- 常用配方：`05-CommonRecipes.md`
- FAQ：`06-FAQ.md`
