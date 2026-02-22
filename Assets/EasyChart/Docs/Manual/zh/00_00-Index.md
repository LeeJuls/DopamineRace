# 快速导览

本手册面向通过 **`EasyChartLibraryWindow`** 在 Unity 编辑器里配置图表（`ChartProfile` 资产）的工作流。

- 菜单入口：`EasyChart/Library Editor`
- 手册查看器：`EasyChart/Manual`

---

## 目录

### A. 上手与工作流

- [快速导览](./00_00-Index.md)
- [快速上手：2 分钟做出第一张图](./00_01-QuickStart.md)
- [UIToolKit工作流（推荐）](./00_02-WorkflowAndLibrary.md)
- [UGUI工作流](./00_03-UGUIWorkflow.md)
- [运行时数据注入（UIToolKit）](./00_04-RuntimeDataInjectionUIToolKit.md)
- [运行时数据注入（UGUI）](./00_05-RuntimeDataInjectionUGUI.md)

### B. 编辑器与面板

- [编辑器工作流与面板说明](./01_01-EditorWorkflow.md)
- [Library 面板（资源树）](./01_02-LibraryPanel.md)
- [JSON Injection 面板](./01_03-JsonInjectionPanel.md)
- [Preview 面板](./02_04-PreviewPanel.md)
- [Inspector 面板](./02_05-InspectorPanel.md)
- [Series 面板](./02_06-SeriesPanel.md)

### C. Series详细配置（用户目的导向）

- [折线图（Line）](./03_01-LineChart.md)
- [柱状图（Bar）](./03_02-BarChart.md)
- [散点图（Scatter）](./03_03-ScatterChart.md)
- [热力图（Heatmap）](./03_04-HeatmapChart.md)
- [雷达图（Radar）](./03_05-RadarChart.md)
- [饼图（Pie）](./03_06-PieChart.md)
- [圆环图（RingChart）](./03_07-RingChart.md)

### D. 配置项参考（按字段分类，查字典）

- [常用配方（Common Recipes）](./04_08-CommonRecipes.md)
- [FAQ（常见问题与最快排错路线）](./04_09-FAQ.md)

### E. 更新与规划

- [更新计划（Roadmap / Update Plan）](./05_01-UpdatePlan.md)

---

## 约定与术语

- **ChartProfile**：图表配置资产（可复用，可在编辑器预览）。
- **Series / Serie**：数据序列（例如折线的一条线、柱状图的一组柱）。
- **SeriesData**：序列中的数据点集合。
- **Axis**：坐标轴配置（`AxisType=Category/Value`）。
- **Category**：类目轴（使用 `labels` 列表）。
- **Value**：数值轴（连续数值范围）。

---

## 推荐文件组织

建议在项目中为图表配置建立一个统一目录：

- `Assets/EasyChart/Library/Custom/`：你自己的 `ChartProfile` 资产
- `Assets/EasyChart/Docs/Manual/`：本手册章节（Markdown）

---

## 手册版本

- 本手册将随 EasyChart 的字段与编辑器功能迭代同步更新。
