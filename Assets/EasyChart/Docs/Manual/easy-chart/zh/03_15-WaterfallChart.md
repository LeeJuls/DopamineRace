# 瀑布图（Waterfall）(Pro)

本章介绍 EasyChart 中瀑布图的工作原理：显示连续正负值的累积效果。

---

## 1. 适用场景

- 财务报表（收入分解）
- 预算分析（收入与支出）
- 带有增减的流程

---

## 2. 重要说明（Pro 功能）

- `SerieType.Waterfall` 的渲染器由 `EasyChartProBootstrap` 注册。
- 如果未安装/启用 Pro，此系列将不会渲染。

---

## 3. 最小可用配置（Checklist）

1. `ChartProfile.coordinateSystem = Cartesian2D`
2. 轴
   - X：通常 `AxisType.Category`（步骤/阶段）
   - Y：`AxisType.Value`
3. Series
   - 添加 1 条 `Serie`
   - `Serie.type = Waterfall`
   - `Serie.seriesData` 至少 2 个点

---

## 4. SeriesData 字段解释（按运行时代码）

瀑布图使用：

- `x`：类目索引（瀑布中的步骤）
- `value`：变化量（正或负）
- `name`：步骤标签

---

## 5. 视觉解读

- **起始柱**：初始值
- **正值柱**：增加（通常为绿色）
- **负值柱**：减少（通常为红色）
- **连接线**：显示柱之间的流动
- **总计柱**：最终累计值

---

## 6. 常用配置（WaterfallSettings）

- **颜色**：正值颜色、负值颜色、总计颜色
- **连接器**：柱之间的线条样式
- **标签**：数值显示格式

---

## 7. 常见坑与排错

- **柱子连接不正确**
  - 确保数据按顺序排列
  - 检查值表示的是变化量，而非绝对值

- **总计柱不正确**
  - 验证所有变化量之和等于预期总计

---

## 8. 深入参考

- 柱状图：`03_02-BarChart.md`
- 轴与范围、Series 与数据：`00_02-WorkflowAndLibrary.md`
- FAQ：`04_09-FAQ.md`
