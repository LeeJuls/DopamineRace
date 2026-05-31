# OHLC 图（OHLC）(Pro)

本章介绍 EasyChart 中 OHLC 图的工作原理：使用线段而非蜡烛实体显示金融开高低收数据。

---

## 1. 适用场景

- 股票价格可视化（比 K 线更简洁）
- 金融市场分析
- 高密度数据展示

---

## 2. 重要说明（Pro 功能）

- `SerieType.OHLC` 的渲染器由 `EasyChartProBootstrap` 注册。
- 如果未安装/启用 Pro，此系列将不会渲染。

---

## 3. 最小可用配置（Checklist）

1. `ChartProfile.coordinateSystem = Cartesian2D`
2. 轴
   - X：通常 `AxisType.Category`（日期/时间）或 `AxisType.Value`
   - Y：`AxisType.Value`（价格范围）
3. Series
   - 添加 1 条 `Serie`
   - `Serie.type = OHLC`
   - `Serie.seriesData` 至少 1 个点，包含 OHLC 数据

---

## 4. SeriesData 字段解释（按运行时代码）

OHLC 图使用：

- `x`：时间/类目索引
- `open`：开盘价
- `high`：最高价
- `low`：最低价
- `close`：收盘价（或 `value`）

---

## 5. 视觉解读

- **垂直线**：显示最高-最低价范围
- **左侧刻度**：开盘价
- **右侧刻度**：收盘价
- 比 K 线更紧凑，适合密集数据

---

## 6. 与 K 线图的区别

| 特性 | K 线图 | OHLC |
|------|--------|------|
| 实体 | 填充矩形 | 无实体 |
| 开/收盘 | 实体边缘 | 水平刻度 |
| 密度 | 较低 | 较高 |
| 视觉冲击 | 较强 | 更简洁 |

---

## 7. 常见坑与排错

- **图表不可见**
  - 确保已安装并启用 EasyChartPro
  - 验证 OHLC 数据字段已填充

- **刻度太小**
  - 在设置中调整刻度宽度

---

## 8. 深入参考

- K 线图：`03_12-CandlestickChart.md`
- 轴与范围、Series 与数据：`00_02-WorkflowAndLibrary.md`
- FAQ：`04_09-FAQ.md`
