# Series 面板 - Sync 同步功能

本章介绍 Series 面板顶部的 **Sync** 开关，用于快速同步多条 Series 的属性设置。

---

## 1. Sync 开关位置

在 Library Editor 的 **Series 面板**顶部工具栏，从左到右依次是：

1. **调色盘图标**（彩色圆形）- 打开配色方案选择器
2. **Sync 开关**（同步图标）- 启用/禁用属性同步
3. **添加 Serie 按钮**（+）- 添加新 Series

Sync 开关有两种状态：

- **关闭**（灰色/黄色）：修改只影响当前选中的 Serie
- **开启**（绿色）：修改自动同步到所有**相同类型**的 Series

---

## 2. 启用 Sync

### 操作步骤

1. 在 Library 面板选中一个 `ChartProfile`
2. 确保该 Profile 有**多条相同类型**的 Series（如 3 条 Line）
3. 在 Series 面板顶部找到 Sync 开关
4. 点击开关，变为**绿色**表示已启用

---

## 3. 使用 Sync 同步属性

### 示例：统一修改线宽

假设你有 3 条 Line 系列：

1. **启用 Sync**（开关变绿色）
2. 在 Series 列表中点击任意一条 Line
3. 在 Inspector 面板找到 `Line Settings` → `lineWidth`
4. 将值从 `2` 改为 `5`
5. 观察 Preview 面板 - 所有 Line 的线宽都变为 5

### 示例：统一动画设置

1. **启用 Sync**
2. 选中任意一条 Bar
3. 修改 `animationDuration` 为 `0.5`
4. 修改 `animationEasing` 为 `EaseInOut`
5. 所有 Bar 系列的动画设置同步更新

---

## 4. 同步规则

### 4.1 类型匹配

Sync **只同步相同类型**的 Series：

| 修改的 Serie 类型 | 受影响的 Series |
|------------------|----------------|
| Line | 所有 Line |
| Bar | 所有 Bar |
| Scatter | 所有 Scatter |

**混合图表示例**：
- 图表有 Line + Bar + Line
- 修改第一条 Line → 只影响第二条 Line
- 修改 Bar → 不影响任何 Line

### 4.2 排除的属性

以下属性**不会被同步**（需要独立设置）：

| 属性 | 说明 |
|------|------|
| `name` | 系列名称 |
| `id` | 系列唯一标识 |
| `type` | 系列类型（Line/Bar/Scatter 等）|
| `data` / `seriesData` | 数据点值 |

### 4.3 支持的属性类别

**通用属性**：
- `visible` - 可见性
- `showLabel` - 显示标签
- `labelSettings` - 标签设置

**样式属性**：
- `color` / `gradient` / `opacity` - 颜色相关

**动画属性**：
- `animationDuration` - 动画时长
- `animationEasing` - 缓动函数
- `animationDelay` - 延迟

**Line 专用**：
- `lineWidth` / `lineStyle` / `smooth` / `showArea`

**Bar 专用**：
- `barWidth` / `barGap` / `stacked` / `stackGroup`

**Scatter 专用**：
- `pointSize` / `pointShape`

**TextureFX**：
- `textureFXLayers` - 纹理特效层（Pro）

---

## 5. 禁用 Sync

当你需要**单独修改**某条 Serie 时：

1. 点击 Sync 开关，变为**灰色/黄色**（关闭状态）
2. 选中要修改的 Serie
3. 修改属性 - 只影响当前选中的 Serie

---

## 6. 自动关闭机制

Sync 开关在以下情况会自动关闭：

- **切换 Profile**：选中其他 ChartProfile 时
- **删除 Serie**：删除最后一条某类型的 Serie 时
- **重新导入 Library**：刷新 Library 数据时

这是为了防止意外同步到错误的图表。

---

## 7. 使用建议

### 推荐工作流

1. **初始设置**：使用 Sync 统一设置基础样式
   - 启用 Sync
   - 设置线宽、颜色、动画等

2. **个性化调整**：关闭 Sync 进行微调
   - 禁用 Sync
   - 单独调整特定 Series 的属性

3. **数据设置**：始终独立设置
   - 数据（`data`）不会被同步，需要逐条设置

### 最佳实践

| 场景 | 是否使用 Sync | 原因 |
|------|--------------|------|
| 统一图表风格 | ✅ 启用 | 保持视觉一致性 |
| 设置公司品牌色 | ✅ 启用 | 所有系列使用相同颜色 |
| 单独高亮某条线 | ❌ 禁用 | 避免影响其他 |
| 设置不同数据 | ❌ 禁用（自动）| 数据本身不被同步 |

---

## 8. Undo 支持

所有 Sync 操作都支持 Unity 的 Undo：

- 按 `Ctrl+Z` 撤销同步操作
- 按 `Ctrl+Y` 重做

如果同步结果不符合预期，立即撤销即可。

---

## 9. 常见问题

### Q: 为什么修改后其他 Series 没有变化？

检查以下几点：

1. **Sync 开关是否启用**（应为绿色）
2. **类型是否相同**（Line 修改不影响 Bar）
3. **属性是否在排除列表**（name/id/type/data 不被同步）
4. **是否有其他相同类型的 Series**

### Q: 能否同步到不同类型的 Series？

**不能**。Sync 设计上只同步相同类型的 Series。不同图表类型（Line vs Bar）的属性结构不同，无法直接同步。

### Q: Sync 会影响 Palette（调色盘）应用的颜色吗？

**会**。如果启用 Sync 后修改 `color` 属性，会同步到所有相同类型的 Series，覆盖 Palette 的颜色。

**建议**：
1. 先使用 Palette 设置初始颜色
2. 关闭 Sync 后再单独调整特定 Series 的颜色

### Q: 如何知道哪些属性会被同步？

除了 `name`、`id`、`type`、`data` 这四个属性，其他所有属性都会被同步。

### Q: 子属性（如 labelSettings.fontSize）会被同步吗？

**会**。只要父级属性对象（`labelSettings`）被修改，其所有子属性都会同步。

---

## 10. 快捷键

| 操作 | 快捷键 |
|------|--------|
| 启用/禁用 Sync | 点击开关 |
| 撤销同步 | `Ctrl+Z` |
| 重做 | `Ctrl+Y` |

---

## 下一章

- `02_08-ColorPalette.md`：调色盘功能，快速应用配色方案
