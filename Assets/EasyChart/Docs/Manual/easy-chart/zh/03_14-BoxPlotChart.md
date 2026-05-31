# 箱线图（BoxPlot）(Pro)

本章介绍 EasyChart 中箱线图的工作原理：显示统计分布数据，包括四分位数、中位数和异常值。

---

## 1. 适用场景

- 统计分布可视化
- 跨类目比较分布
- 识别异常值和数据分散程度

---

## 2. 重要说明（Pro 功能）

- `SerieType.BoxPlot` 的渲染器由 `EasyChartProBootstrap` 注册。
- 如果未安装/启用 Pro，此系列将不会渲染。

---

## 3. 最小可用配置（Checklist）

1. `ChartProfile.coordinateSystem = Cartesian2D`
2. 轴
   - X：通常 `AxisType.Category`（分组）
   - Y：`AxisType.Value`（数据范围）
3. Series
   - 添加 1 条 `Serie`
   - `Serie.type = BoxPlot`
   - `Serie.seriesData` 至少 1 个点，包含统计数据

---

## 4. SeriesData 字段解释（按运行时代码）

箱线图使用：

- `x`：类目索引
- `min`：最小值（下须）
- `q1`：第一四分位数（25%）
- `median`：中位数（50%）
- `q3`：第三四分位数（75%）
- `max`：最大值（上须）

---

## 5. 视觉组件

- **箱体**：从 Q1 到 Q3（四分位距）
- **中位线**：箱体内的水平线
- **须**：延伸到最小/最大值的垂直线
- **异常值**：超出须范围的点（可选）

---

## 6. 常用配置（BoxPlotSettings）

- **箱体宽度**：箱体的宽度
- **颜色**：填充颜色、边框颜色
- **须样式**：线宽、端点样式

---

## 7. 常见坑与排错

- **箱体不可见**
  - 确保已安装并启用 EasyChartPro
  - 验证所有统计字段已填充

- **箱体显示反了**
  - 检查 Q1 < median < Q3

---

## 8. 深入参考

- 轴与范围、Series 与数据：`00_02-WorkflowAndLibrary.md`
- FAQ：`04_09-FAQ.md`
