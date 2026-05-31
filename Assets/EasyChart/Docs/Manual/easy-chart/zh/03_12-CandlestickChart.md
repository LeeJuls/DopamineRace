# K线图（Candlestick）(Pro)

本章介绍 EasyChart 中 K 线图（蜡烛图）的工作原理：以传统蜡烛图格式显示金融 OHLC 数据（开盘价、最高价、最低价、收盘价）。

---

## 1. 适用场景

- 股票价格可视化
- 金融市场分析
- 交易模式识别

---

## 2. 重要说明（Pro 功能）

- `SerieType.Candlestick` 的渲染器由 `EasyChartProBootstrap` 注册。
- 如果未安装/启用 Pro，此系列将不会渲染。

---

## 3. 最小可用配置（Checklist）

1. `ChartProfile.coordinateSystem = Cartesian2D`
2. 轴
   - X：通常 `AxisType.Category`（日期/时间）或 `AxisType.Value`
   - Y：`AxisType.Value`（价格范围）
3. Series
   - 添加 1 条 `Serie`
   - `Serie.type = Candlestick`
   - `Serie.seriesData` 至少 1 个点，包含 OHLC 数据

---

## 4. SeriesData 字段解释（按运行时代码）

K 线图使用：

- `x`：时间/类目索引
- `open`：开盘价
- `high`：最高价
- `low`：最低价
- `close`：收盘价（或 `value`）

---

## 5. 常用配置（CandlestickSettings）

- **颜色**：阳线（上涨）颜色、阴线（下跌）颜色
- **宽度**：蜡烛实体宽度
- **影线**：上下影线样式

---

## 6. 视觉解读

- **阳线**（收盘价 > 开盘价）：通常为绿色/白色，实体表示涨幅
- **阴线**（收盘价 < 开盘价）：通常为红色/黑色，实体表示跌幅
- **影线**：显示最高/最低价范围的垂直线

---

## 7. 常见坑与排错

- **蜡烛不可见**
  - 确保已安装并启用 EasyChartPro
  - 验证 OHLC 数据字段已填充

- **颜色反了**
  - 检查阳线/阴线颜色设置

- **Y 轴范围不正确**
  - 启用自动范围或手动设置 min/max 以覆盖所有价格

---

## 8. 深入参考

- OHLC 图：`03_13-OHLCChart.md`
- 轴与范围、Series 与数据：`00_02-WorkflowAndLibrary.md`
- FAQ：`04_09-FAQ.md`
