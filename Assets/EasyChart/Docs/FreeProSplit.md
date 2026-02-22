# Free vs Pro 划分与发布规则

本文用于固化 EasyChart 的 Free/Pro 划分口径、工程结构与发布导出规则，防止后续开发过程出现边界漂移。

## 1. 总体策略（结论）

- 两个独立包：Free 包 / Pro 包
- Pro 包包含 Free 全量（Pro 为 superset）
- Free 遇到 Pro-only 资产/配置：允许直接报错（不做降级兼容）

## 2. 功能边界（Scheme 2）

### 2.1 Free 包必须包含

- 基础 2D 图表类型与常用工作流（运行时可用 + 编辑器可配置/预览）
- `EasyChartLibraryWindow`（LibraryWindow）
- 运行时数据注入能力（例如 `ChartFeed` / `ApplyJson`）

### 2.2 Pro 包增量方向（优先级）

- A：新增图表类型
  - Gauge / Funnel / BoxPlot / Candlestick(K线) / Treemap / Sunburst 等
- B：3D 图表
  - 3D Bar / 3D Scatter / 3D Surface 等
- C：2D 高级视觉效果
  - 贴图 UV 动画、特效类渲染能力等

### 2.3 2D 高级效果具体划分

- Free：Bar 头部圆角、hover 效果
- Pro：贴图 UV 动画（以及后续更复杂的材质/特效能力）

## 3. 工程结构（asmdef / 目录）

当前工程采用 asmdef 拆分，保证 Free 不依赖 Pro，便于导出两个包。

- `Assets/EasyChart/Scripts/Runtime/` -> `EasyChart.Runtime`
- `Assets/EasyChart/Scripts/Editor/` -> `EasyChart.Editor`（Editor-only，引用 `EasyChart.Runtime`）
- `Assets/EasyChart/EasyChartPro/Scripts/Runtime/` -> `EasyChart.Pro.Runtime`（引用 `EasyChart.Runtime`）
- `Assets/EasyChart/EasyChartPro/Scripts/Editor/` -> `EasyChart.Pro.Editor`（Editor-only，引用 `EasyChart.Editor` + `EasyChart.Pro.Runtime`）

约束：

- Pro-only 代码必须放在 `EasyChartPro/**` 下。
- Free 的 asmdef 不得引用任何 `EasyChart.Pro.*` asmdef。

## 4. 发布/导出规则

### 4.1 Free 包

- 导出包含：`Assets/EasyChart/**`
- 导出排除：`Assets/EasyChart/EasyChartPro/**`（以及未来所有 `*Pro` 资源目录，例如 `TexturesPro/**`, `ShadersPro/**`, `DemoPro/**`）

### 4.2 Pro 包

- 全量导出 `Assets/EasyChart/**`（包含 `Scripts` + `EasyChartPro`）

## 5. 报错策略（Free 遇到 Pro-only）

- 允许直接报错，但要求错误信息可定位：
  - 运行时：明确 `Debug.LogError` 指出需要 Pro
  - 编辑器：避免空引用，尽量显示可读提示
