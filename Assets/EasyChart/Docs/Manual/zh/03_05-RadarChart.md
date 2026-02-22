# 雷达图（Radar）

本章目标：把雷达图在 EasyChart 中“维度标签来源、数值范围如何计算、数据点顺序如何解释”的规则讲清楚，并对应到 Inspector 字段。

---

## 1. 适用场景

- 多维指标对比
- 能力/属性雷达

---

## 2. 最小可用配置（Checklist）

1. `ChartProfile.coordinateSystem = Polar2D`
2. Series
   - 添加 1 条 `Serie`
   - `Serie.type = Radar`
   - `Serie.seriesData` 建议至少 3 个点（少于等于 2 个维度运行时不会绘制/无法 hover）
3. PolarAxes（推荐配置）
   - `polarAxes.angleAxis.labels`：维度名
   - `polarAxes.radiusAxis`：数值范围（可自动/手动）

---

## 3. Inspector 对应字段

- **ChartProfile / Coordinate System**
  - `coordinateSystem = Polar2D`

- **PolarAxes（建议用这套来配 Radar 的轴）**
  - `polarAxes.angleAxis.labels`：维度标签
  - `polarAxes.radiusAxis.autoRangeMin/autoRangeMax/minValue/maxValue/autoRangeRounding/labelFormat/...`

- **Series**
  - `series[i].type = Radar`
  - `series[i].settings`：实际类型为 `RadarSettings`
    - `radar`：布局（startAngleDeg / clockwise / innerRadius / outerRadius / plot / background）
    - `stroke`：折线样式
    - `area`：面积填充
    - `point`：点样式（点显示也会影响 hover 拾取半径）
  - `series[i].labelSettings`：数据点标签（可显示维度名与数值）

---

## 4. SeriesData 字段解释（按运行时代码）

Radar 的关键点是：**维度顺序由 `seriesData` 列表索引决定**。

- **数值**：使用 `SeriesData.value`
- **维度索引**：使用“点在 `seriesData` 里的位置 i”（0..dimensionCount-1）
- `SeriesData.x` 在 Radar 渲染中 **不参与定位**（不要依赖 x 来表达维度）

维度数量（dimensionCount）来源：

1. 优先 `Data.PolarAxes.angleAxis.labels.Count`
2. 如果没配 angleAxis.labels，则使用（优先）某个 Category Axis 的 labels（见第 5 节）
3. 再不行就用 `seriesData.Count`（或多条 serie 取最大 count）

---

## 5. 维度标签（Dimension Label）的真实来源顺序

运行时维度名按以下优先级解析：

1. `polarAxes.angleAxis.labels[i]`
2. `axes[]` 里某个 `AxisType.Category` 的 `labels[i]`
   - 会优先匹配 `Data.XAxisId` 对应的 Category 轴
3. `seriesData[i].name`
4. 都没有时显示 `Dim i`

> 建议：做 Radar 时直接用 `polarAxes.angleAxis.labels` 统一管理维度名；`SeriesData.name` 作为兜底。

---

## 6. 数值范围（Radius Axis）如何计算

Radar 的半径值域使用 `SeriesData.value` 计算：

- 默认会对所有 Radar serie 的 value 做自动范围（auto range）
- 如果你配置了 `polarAxes.radiusAxis`：
  - `autoRangeMin/autoRangeMax` 会决定 min/max 是否自动
  - `minValue/maxValue` 在对应 autoRange 关闭时生效
  - `autoRangeRounding` 会对自动出来的 min/max 做“整十/整百/自定义单位”的取整
  - `labelFormat` 会影响 tooltip/标签的格式化

---

## 7. 常见坑与排错

- **看不到雷达图**
  - 检查 `coordinateSystem` 是否为 `Polar2D`
  - 维度数必须大于 2（labels 或 seriesData 至少 3）

- **维度对不上/顺序错乱**
  - Radar 不看 `x`，它按 `seriesData` 的列表顺序当维度顺序
  - 需要你在 `seriesData` 里按维度顺序放点

- **hover 很难触发**
  - Radar 的拾取半径和 `RadarSettings.point.size` 相关
  - 如果 `point.show=false`，拾取半径会变成 0（基本不可 hover）

---

## 8. 深入参考

- Series 与数据：`00-WorkflowAndLibrary.md`
- 常用配方：`05-CommonRecipes.md`
- FAQ：`06-FAQ.md`
