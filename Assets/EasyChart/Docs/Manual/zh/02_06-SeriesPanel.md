# Series 面板（数据与系列）

本章说明 `Unity Easy Chart/Library Editor` 右侧的 **Series** 面板。

Series 面板以“图表结构”的方式编辑 `ChartProfile.series`：你可以添加/删除/排序系列，设置每条 Serie 的类型与参数，并直接编辑 `seriesData`（数据点）。

---

## 面板结构概览

当你选中一个 `ChartProfile` 后，Series 面板会显示：

- 一组 Serie 卡片（每个卡片对应 `series[i]`）
- 最底部的 **+ Add Series** 按钮

每个 Serie 卡片通常由三部分组成：

- **Header（标题行）**：折叠按钮 + 标题
- **Body（主体）**：Name / Id / Type / Settings / Data
- **Footer（右下角控制）**：↑ ↓ X

---

## Header：折叠/展开

- Header 左侧有一个小按钮：
  - `▼` 表示已展开
  - `▶` 表示已折叠
- 折叠状态会被记住（按 Profile + index 存储），用于减少长配置的视觉负担。

---

## Name 与 Serie Id

### Name

- `Name` 字段用于显示与编辑该 Serie 的名称。
- 当你修改 Name 时，卡片标题会同步更新，并触发 Preview 刷新。

### Serie Id（只读）

如果该 Serie 支持 `id` 字段，面板会显示：

- **Serie Id**（只读文本框）
- **Copy**（按钮）复制 id 到剪贴板

这个 id 常用于：

- 交互/高亮/外部系统引用某条 serie
- 保持引用稳定（尤其是你会重排/增删 series 时）

---

## Type（系列类型）与兼容性提示

### Type 下拉框

- `Type` 用于选择 SerieType（Line/Bar/Scatter/Pie/Radar…）。
- 下拉框会基于注册表提供可选类型；如果当前类型不在列表里，会临时插入以保证可见。

### 兼容性警告

当 SeriesType 与 Profile 的 `coordinateSystem` 不兼容时，Series 面板会显示一段警告文字：

- 仍然允许渲染（不会强制阻止）
- 但会提示坐标轴/网格语义可能不一致

典型例子：

- Profile 是 `Polar2D`，但 SeriesType 选择了 Line/Bar（不推荐）

### Pro-only 类型限制

某些类型在 Free 版本不可用（例如 RingChart / HorizontalBar / Heatmap / Pie3D 等）。

- 当你尝试选择这些类型时，如果未安装 Pro：
  - 会显示提示文本
  - 并自动把下拉框回退到原来的类型（不会修改资产）

---

## Settings（系列参数）

Series 面板会为每条 serie 显示一组 Settings 配置：

- 根折叠块名称会随类型变化（例如 `LineSettings` / `BarSettings` / `PieSettings` …）。
- 某些类型会有更细分的子折叠（例如 Ring 的 layout/valueMapping 等）。

提示：

- 切换 Type 可能会触发“Settings 实例替换”（managedReference 结构变化）。
- 发生替换时会延迟一帧重建 UI，以避免序列化句柄失效。

---

## Data：seriesData（数据点）

Series 面板里会直接展示 `seriesData` 数组（Unity 的默认数组编辑器）。

- 默认会强制展开（便于编辑）。
- 当你增删/修改数据点时，会触发 Preview 刷新。

建议：

- 数据点较多时，可以配合 JSON Injection 面板进行批量编辑。

---

## Footer：排序与删除（渲染顺序）

每个 serie 卡片右下角有三个按钮：

- **↑**：把当前 serie 上移一位（`MoveArrayElement(index, index-1)`）
- **↓**：把当前 serie 下移一位（`MoveArrayElement(index, index+1)`）
- **X**：删除当前 serie（`DeleteArrayElementAtIndex(index)`）

渲染顺序提示：

- 通常 **后面的 serie 会绘制在更上层**。
- 因此你可以用 ↑↓ 来控制遮挡关系（例如点/线盖住柱子）。

---

## + Add Series（新增系列）

点击底部 **+ Add Series**：

- 会在 `series` 数组末尾插入一个新元素。
- 注意：如果当前已经存在至少一条 serie，Unity 的 `InsertArrayElementAtIndex(arraySize)` 会 **复制最后一个元素**（包括 type/settings）。
- 如果这是第一条 serie，会根据坐标系设置默认类型：
  - Polar2D：默认 Radar
  - 其他：默认 Line

新增后通常会：

- 自动填入名称（如 `Serie N`）
- 触发 `EnsureRuntimeData()`
- 刷新 Series 列表与 Preview

---

## 推荐工作流

### 1) 从零创建一张基础图

- + Add Series
- Type 选择 Line 或 Bar
- 在 seriesData 里加入几个点
- 去 Inspector 调整轴范围/可见数量

### 2) 调整遮挡关系

- 用 ↑↓ 调整 series 顺序
- 观察 Preview 中的层级变化

### 3) 大量数据/批量修改

- 在 JSON Injection 中切换 `Datas Format`
- Copy 到外部编辑器批量生成/替换数据
- 粘贴回来 ApplyToChart

---

## Help

- 点击标题栏最右侧 **Help** 图标可回到本章节。
