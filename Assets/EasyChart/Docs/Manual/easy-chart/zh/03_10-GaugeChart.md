# 仪表盘图（Gauge）(Pro)

本章介绍 EasyChart 中仪表盘图的工作原理：用于显示单一指标的可视化指示器，类似于速度表或进度表。

---

## 1. 适用场景

- 单一指标可视化（KPI、进度）
- 仪表板指示器
- 实时监控显示

---

## 2. 重要说明（Pro 功能）

- `SerieType.Gauge` 的渲染器由 `EasyChartProBootstrap` 注册。
- 如果未安装/启用 Pro，此系列将不会渲染。

---

## 3. 最小可用配置（Checklist）

1. 添加 1 条 `Serie`：
   - `type = Gauge`
   - `settings = GaugeSettings`
   - `seriesData` 至少 1 个点
2. 每个点有一个 `value` 表示当前指标值

---

## 4. SeriesData 字段解释（按运行时代码）

仪表盘图使用：

- `value`：要显示的当前指标值
- `name`：仪表盘的可选标签

---

## 5. 常用配置（GaugeSettings）

- **范围**：`minValue` / `maxValue` - 定义仪表盘刻度
- **外观**：弧度角度、颜色、指针样式
- **标签**：数值显示格式、刻度标记

---

## 6. 常见坑与排错

- **仪表盘不可见**
  - 确保已安装并启用 EasyChartPro
  - 检查 `value` 是否在定义的范围内

- **指针位置不正确**
  - 验证 `minValue` 和 `maxValue` 设置是否正确

---

## 7. 深入参考

- Series 与数据：`00_02-WorkflowAndLibrary.md`
- 常用配方：`04_08-CommonRecipes.md`
- FAQ：`04_09-FAQ.md`
