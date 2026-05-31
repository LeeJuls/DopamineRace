# UGUI工作流

本章目标：用 UGUI（Canvas/RectTransform）把 EasyChart 图表用起来，并理解两种渲染模式的选择：

- `ScreenSpaceOverlay`：画质最好（不使用 RenderTexture），但通常只在 Game 视图可见
- `WorldSpace`：Scene/Game 都可见（使用 RenderTexture），适合 3D 世界空间 UI

---

## 1. 推荐方案：UGUIChartBridge

推荐使用 `UGUIChartBridge` 组件。

它的定位是：

- 仍然使用 **UI Toolkit 的 `ChartElement`** 作为图表渲染核心
- 通过桥接把图表“贴合”到某个 UGUI `RectTransform` 上

因此它兼顾：

- 图表能力与 UI Toolkit 渲染
- UGUI 场景/Prefab 的搭建与布局习惯

---

## 2. 通用前置条件

无论你选哪种模式，都建议先准备：

- 一个要显示的 `ChartProfile`（建议从 `EasyChart/Library Editor` 克隆后修改得到）
- 一个 `PanelSettings` 资产，并在 `UGUIChartBridge` 的 `Panel Settings Asset` 中指定

> 备注：`Panel Settings Asset` 对字体渲染与整体稳定性更友好。

---

## 3. Screen Space Overlay（推荐用于 HUD/面板）

### 适用场景

- HUD、UI 面板、弹窗
- 追求画质与清晰度

### 特点

- 不使用 RenderTexture
- 通常只在 Game 视图可见

### 搭建步骤（概览）

1. 创建 `Canvas`
2. 在 Canvas 下创建一个带 `RectTransform` 的节点（`Image` 或空物体均可）
3. 添加 `UGUIChartBridge`
4. 配置：
   - `Profile`
   - `Panel Settings Asset`
   - `Render Mode = ScreenSpaceOverlay`
   - `Sort Order`（用于层级覆盖；仅 Screen Space Overlay 模式生效）

关键点：

- 该模式会把图表渲染在一个运行时创建/复用的 `UIDocument` 里。
- 如果被其他 UI 盖住，优先调大 `Sort Order`。

---

## 4. World Space（推荐用于 3D 世界空间面板）

### 适用场景

- 3D 世界里的看板/屏幕/面板
- 希望 Scene 视图也能看到渲染结果

### 特点

- 使用 RenderTexture
- Scene 与 Game 视图通常都可见
- 画质可能略受 RenderTexture 分辨率影响

### 搭建步骤（概览）

1. 创建 `Canvas`
2. 设置 `Render Mode = World Space`
3. 在 Canvas 下创建一个带 `RectTransform` 的节点（建议 `RawImage`）
4. 添加 `UGUIChartBridge`
5. 配置：
   - `Profile`
   - `Panel Settings Asset`
   - `Render Mode = WorldSpace`

关键点：

- World Space 模式会创建并维护一个 `RenderTexture`，并通过 `RawImage` 显示。
- 清晰度与 `RenderTexture` 分辨率强相关：分辨率通常来自目标 `RectTransform` 的宽高。
  - 如果图表模糊，请优先把目标 `RectTransform` 设大一些（例如 600x400+）。

---

## 5. 选型建议（快速结论）

- 优先选 **ScreenSpaceOverlay**：
  - 你做的是传统 UI（HUD/面板）
  - 你最在意清晰度

- 优先选 **WorldSpace**：
  - 你的图表要出现在 3D 世界里
  - 你希望 Scene 视图也能看到

---

## 6. 常见问题与排错

- **运行时不显示**
  - 确认目标物体的 `RectTransform` 尺寸不是 0
  - 确认 `Profile` 已赋值，且该 Profile 在 Library Editor 的 Preview 中能正常显示
  - 如果字体显示异常，优先检查 `Panel Settings Asset` 是否为空

- **World Space 模式图表模糊**
  - 提升目标 `RectTransform` 尺寸（会提高 RenderTexture 分辨率）
  - 避免运行时频繁剧烈缩放（会触发 RenderTexture 调整）

---

## 7. 备选：导出 UGUI Prefab

如果你的版本提供 `Export UGUI Prefab`：

- 你也可以将 Profile 导出为 UGUI 预制体并直接在 Canvas 下使用
- 但对交互/兼容性的覆盖范围取决于导出器版本
