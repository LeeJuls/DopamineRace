# 横向柱状图（HorizontalBar）

本章介绍 EasyChart 中横向柱状图的工作原理：与普通柱状图类似，但采用横向布局，更适合长类目标签。

---

## 1. 适用场景

- 长标签的类目对比
- 排名展示
- 调查结果可视化

---

## 2. 最小可用配置（Checklist）

1. `ChartProfile.coordinateSystem = Cartesian2D`
2. 轴
   - Y：通常 `AxisType.Category`（填写 `labels`）
   - X：通常 `AxisType.Value`
3. Series
   - 添加 1 条 `Serie`
   - `Serie.type = HorizontalBar`
   - `Serie.seriesData` 至少 1 个点

---

## 3. Inspector 对应字段

- **Axis Settings**
  - `cartesian.xAxisId / cartesian.yAxisId`
  - `axes[]`（X/Y 对应 AxisConfig）

- **Series**
  - `series[i].type = HorizontalBar`
  - `series[i].settings`：实际类型为 `BarSettings`
    - `barWidth`
    - `stacked` / `stackGroup`
    - `barGap` / `categoryGap`
    - `cornerRadius` / `cornerSegments`
    - `textureFill`（颜色/纹理）
    - `border` / `background`
    - `hover`（开启后支持拾取/高亮）

---

## 4. SeriesData 字段解释（按运行时代码）

横向柱状图使用：

- **类目/垂直位置**：`SeriesData.x`（映射到 Y 轴类目）
- **柱长**：`SeriesData.value`

---

## 5. 与普通柱状图的区别

- 布局为横向而非纵向
- 类目在 Y 轴，数值在 X 轴
- 更适合显示长类目名称

---

## 6. 常见坑与排错

- **柱子显示为纵向而非横向**
  - 确保 `type = HorizontalBar`，而不是 `Bar`
  - 检查轴配置（Y 应为 Category）

- **标签被截断**
  - 增加图表设置中的左侧内边距

---

## 7. 深入参考

- 柱状图：`03_02-BarChart.md`
- 轴与范围、Series 与数据：`00_02-WorkflowAndLibrary.md`
- 常用配方：`04_08-CommonRecipes.md`
