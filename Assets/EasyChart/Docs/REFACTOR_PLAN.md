# EasyChart 重构计划

基于对现有架构的分析以及对 XCharts 源码的研究，为了支持混合图表（Mixed Chart）及更多图表类型（Bar, Pie 等）的扩展，制定以下重构计划。

## 1. 核心数据结构重构 (Data Architecture)

目标：建立一个既能承载多种图表配置，又能完美兼容 Unity 序列化机制的数据底层。

### 1.1 保持单一 `Serie` 类 (Monolithic Approach)
参考 XCharts 的设计，不采用多态继承（`LineSerie : BaseSerie`），而是保持一个包含所有可能的配置字段的单一 `Serie` 类。
*   **优势**：避免 Unity 序列化丢失子类数据的问题，支持运行时无损切换图表类型。
*   **修改**：
    *   新增 `public SerieType type;` 枚举 (Line, Bar, Pie, Scatter)。
    *   引入组合式配置结构体（Struct/Class），保持主类整洁：
        *   `public LineSettings lineSettings;` (包含 smooth, width, color)
        *   `public BarSettings barSettings;` (包含 barWidth, stack, gap)
        *   `public PieSettings pieSettings;` (包含 radius, center, roseType)

### 1.2 全局配置升级
*   `ChartData` 中增加全局 `ChartType` 枚举 (Line, Bar, Pie, Mixed)。
    *   **Line/Bar 模式**：强制所有 Series 为同一类型，简化 UI。
    *   **Mixed 模式**：允许每个 Series 独立设置类型。

---

## 2. 编辑器与交互重构 (Editor & UX)

目标：通过编辑器脚本屏蔽底层数据的复杂性，提供“上下文感知”的配置体验。

### 2.1 动态 Series 属性面板
*   重构 `EasyChartLibraryWindow` 的 `RefreshSeriesList` 方法。
*   实现动态 GUI 绘制逻辑：
    *   读取当前 Series 的 `type`。
    *   **只绘制**与该类型相关的属性字段（例如：选了 Bar 就隐藏 Line 的平滑设置）。
    *   使用 `EditorGUI.BeginChangeCheck` 监听类型变化，实时刷新 UI。

### 2.2 类型切换交互
*   在 Series 卡片（Title 区域或属性顶部）增加 **Type 下拉框**。
*   在全局 `General Properties` 顶部增加 **Chart Type 下拉框**。
    *   切换全局类型时，自动将所有 Series 重置为对应类型（需弹出确认提示）。

### 2.3 上下文感知配置
*   如果是 `Pie Chart`，自动隐藏 `AxisSettings`（XY 轴配置）。
*   如果是 `Mixed Chart`，显示所有通用配置。

---

## 3. 渲染管线重构 (Rendering Pipeline)

目标：解耦 `ChartElement` 与具体渲染逻辑，支持多渲染器叠加。

### 3.1 渲染器抽象
*   强化 `BaseSeriesRenderer`，确保其不依赖于具体的 `ChartData.Type`。
*   定义标准接口：`Draw(Painter2D painter, Serie serie)`。

### 3.2 多渲染器管理 (Multi-Renderer System)
*   **现状**：`ChartElement` 只有一个 `_activeSeriesRenderer`。
*   **重构为**：`List<BaseSeriesRenderer> _renderers`。
*   **渲染流程**：
    1.  遍历 `Data.Series`。
    2.  根据 Serie 类型（或分组），通过工厂方法实例化对应的 Renderer（`LineSeriesRenderer`, `BarSeriesRenderer`）。
    3.  按层级顺序添加到 `_chartArea`：Grid -> Bar -> Line -> Point -> Tooltip。

---

## 4. 坐标与交互系统 (Coordinate & Interaction)

目标：支持非笛卡尔坐标系（如极坐标）的渲染与交互。

### 4.1 坐标转换抽象
*   提取 `GetPixelPos` 方法，定义 `ICoordinateSystem` 接口。
    *   `GridCoord` (实现 XY 轴映射)。
    *   `PolarCoord` (实现 角度/半径 映射)。

### 4.2 交互检测重构 (Hit Testing)
*   **现状**：`OnPointerMove` 直接通过 X 轴索引查找数据。
*   **重构为**：委托模式。
    *   `ChartElement` 将鼠标位置传递给所有活跃的 Renderers。
    *   Renderer 返回 `HitResult`（包含命中的数据点、距离等）。
    *   `ChartElement` 汇总结果显示 Tooltip。这样可以自然支持 Pie Chart 的扇区点击检测。

---

## 5. 执行路线图 (Roadmap)

1.  **Step 1 (Data)**: 修改 `Serie` 类，添加 `SerieType` 枚举及 `BarSettings` 等结构体。（不影响现有逻辑，仅增加字段）。
2.  **Step 2 (Editor)**: 升级 `EasyChartLibraryWindow`，实现基于类型的动态属性显示。
3.  **Step 3 (Render)**: 创建 `BarSeriesRenderer` 原型，并在 `ChartElement` 中实现简单的多 Renderer 支持（先支持 Line + Bar 混合）。
4.  **Step 4 (Interaction)**: 重构交互逻辑，适配混合图表的 Tooltip 显示。
