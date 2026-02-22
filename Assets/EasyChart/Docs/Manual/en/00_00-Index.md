# Quick Overview

This manual is intended for a workflow where you configure charts (the `ChartProfile` asset) in the Unity Editor via **`EasyChartLibraryWindow`**.

- Menu entry: `EasyChart/Library Editor`
- Manual viewer: `EasyChart/Manual`

---

## Table of Contents

### A. Getting Started & Workflows

- [Quick overview](./00_00-Index.md)
- [Quick start: create your first chart in 2 minutes](./00_01-QuickStart.md)
- [UIToolKit workflow (recommended)](./00_02-WorkflowAndLibrary.md)
- [UGUI workflow](./00_03-UGUIWorkflow.md)
- [Runtime data injection (UIToolKit)](./00_04-RuntimeDataInjectionUIToolKit.md)
- [Runtime data injection (UGUI)](./00_05-RuntimeDataInjectionUGUI.md)

### B. Editor & Panels

- [Editor workflow and panels](./01_01-EditorWorkflow.md)
- [Library panel (asset tree)](./01_02-LibraryPanel.md)
- [JSON Injection panel](./01_03-JsonInjectionPanel.md)
- [Preview panel](./02_04-PreviewPanel.md)
- [Inspector panel](./02_05-InspectorPanel.md)
- [Series panel](./02_06-SeriesPanel.md)

### C. Series Configuration (Goal-Oriented)

- [Line chart](./03_01-LineChart.md)
- [Bar chart](./03_02-BarChart.md)
- [Scatter chart](./03_03-ScatterChart.md)
- [Heatmap chart](./03_04-HeatmapChart.md)
- [Radar chart](./03_05-RadarChart.md)
- [Pie chart](./03_06-PieChart.md)
- [Ring chart](./03_07-RingChart.md)

### D. Reference (Lookup by Field)

- [Common recipes](./04_08-CommonRecipes.md)
- [FAQ (fastest troubleshooting path)](./04_09-FAQ.md)

### E. Updates & Roadmap

- [Roadmap / update plan](./05_01-UpdatePlan.md)

---

## Conventions & Terminology

- **ChartProfile**: A chart configuration asset (reusable; previewable in the editor).
- **Series / Serie**: A data series (e.g. one line in a line chart, or one group of bars in a bar chart).
- **SeriesData**: The set of data points in a series.
- **Axis**: Axis configuration (`AxisType=Category/Value`).
- **Category**: Category axis (uses the `labels` list).
- **Value**: Value axis (continuous numeric range).

---

## Recommended Project Structure

Recommended to create a dedicated folder in your project for chart assets:

- `Assets/EasyChart/Library/Custom/`: your own `ChartProfile` assets
- `Assets/EasyChart/Docs/Manual/`: this manual (Markdown chapters)

---

## Manual Version

- This manual will be kept in sync with EasyChart field and editor feature updates.
