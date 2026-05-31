# Series 面板 - 调色盘功能

本章介绍 Series 面板顶部的 **调色盘（Color Palette）** 功能，用于快速为多条 Series 应用统一的配色方案。

---

## 1. 调色盘图标位置

在 Library Editor 的 **Series 面板**顶部工具栏，从左到右依次是：

1. **调色盘图标**（彩色圆形 🎨）- 打开配色方案选择器
2. **Sync 开关**（同步图标）- 启用/禁用属性同步
3. **添加 Serie 按钮**（+）- 添加新 Series

点击**彩色圆形图标**即可打开调色盘选择器。

---

## 2. 调色盘选择器界面

调色盘选择器是一个悬浮弹窗，包含：

### 2.1 调色盘列表

显示所有可用的配色方案，每个方案包括：

- **名称**：调色盘名称（如 "Google Charts"、"D3 Category"）
- **颜色预览**：水平排列的颜色样本
- **颜色数量**：显示该方案包含的颜色组数

### 2.2 操作按钮

- **Manage Palettes**：打开调色盘管理器（定位资产文件）
- **Reset to Defaults**：重置为默认调色盘配置

---

## 3. 使用调色盘

### 3.1 应用配色方案

1. 在 Library 面板选中一个 `ChartProfile`
2. 确保该 Profile 有多条 Series
3. 点击 Series 面板顶部的**调色盘图标**（彩色圆形）
4. 在弹出的选择器中点击想要的配色方案
5. 观察 Preview 面板 - 所有 Series 的颜色已更新

### 3.2 应用效果

调色盘应用规则：

| Serie 序号 | 使用的颜色组 |
|-----------|------------|
| 第 1 条 | 调色盘第 1 组颜色 |
| 第 2 条 | 调色盘第 2 组颜色 |
| 第 3 条 | 调色盘第 3 组颜色 |
| ... | 依此类推 |
| 超过颜色组数 | **循环使用** |

**示例**：调色盘有 6 组颜色，图表有 8 条 Series：
- Serie 1-6：使用颜色 1-6
- Serie 7-8：循环使用颜色 1-2

---

## 4. 内置调色盘

EasyChart 内置多套专业配色方案：

### 4.1 Google Charts

- **风格**：明亮现代，基于 Google Material Design
- **颜色**：蓝色、红色、黄色、绿色、紫色、青色
- **适用**：通用商业图表

### 4.2 D3 Category

- **风格**：Web 可视化标准
- **颜色**：蓝色、橙色、绿色、红色、紫色、棕色、粉色、灰色
- **适用**：数据可视化报告

### 4.3 Tableau 10

- **风格**：行业标准，专业商务
- **颜色**：钢蓝、橙色、绿色、红色、紫色、棕色、粉色、灰色
- **适用**：商务演示、分析报告

### 4.4 Modern Blue

- **风格**：现代简洁，蓝色主调
- **颜色**：蓝色、橙色、绿色、紫色、青色、粉色
- **适用**：企业品牌、科技风格

### 4.5 Cool Ocean

- **风格**：冷色调，海洋风格
- **颜色**：翠绿、青绿、蓝色、紫色、靛蓝、深蓝
- **适用**：环境数据、海洋主题

### 4.6 Warm Sunset

- **风格**：暖色调，日落风格
- **颜色**：珊瑚红、橙色、金黄、粉红、紫红、深红
- **适用**：热情、活力的场景

---

## 5. 自定义调色盘

### 5.1 打开调色盘管理器

1. 点击调色盘选择器底部的 **Manage Palettes** 按钮
2. Project 面板会自动定位到：`Assets/EasyChart/Editor/SeriesColorPalettes.asset`

### 5.2 编辑调色盘

选中 `SeriesColorPalettes.asset` 后，在 Inspector 中：

#### 添加新调色盘

1. 展开 `palettes` 列表
2. 点击 `+` 按钮添加新调色盘
3. 设置 `name` 字段（调色盘名称）
4. 展开 `colorSets` 添加颜色组

#### 编辑颜色组

每个 **SeriesColorSet** 包含：

| 字段 | 说明 | 默认值 |
|------|------|--------|
| `baseColor` | 主色（线条/柱子/点）| 白色 |
| `areaColor` | 区域填充色 | baseColor 的半透明版 |
| `highlightColor` | 高亮色（hover）| baseColor 的亮版 |

**颜色自动生成**：
- 如果只设置 `baseColor`，`areaColor` 和 `highlightColor` 会自动生成
- `areaColor` = baseColor + 透明度降低
- `highlightColor` = baseColor + 亮度提高

#### 删除调色盘

1. 在 `palettes` 列表中找到要删除的调色盘
2. 点击 `-` 按钮
3. 点击 **Reset to Defaults** 可恢复默认配置

---

## 6. 颜色组详解

### 6.1 颜色组结构

```
SeriesColorSet
├── baseColor (Color)      # 主色
├── areaColor (Color)      # 区域填充
└── highlightColor (Color) # 高亮色
```

### 6.2 各图表类型的应用

| 图表类型 | baseColor | areaColor | highlightColor |
|---------|-----------|-----------|---------------|
| Line | 线条颜色 | 线下区域填充 | 点高亮 |
| Bar | 柱子颜色 | - | 柱子高亮 |
| Scatter | 点颜色 | - | 点高亮 |
| Pie | 扇形颜色 | - | 扇形高亮 |
| Radar | 线条颜色 | 填充区域 | 点高亮 |

---

## 7. 调色盘与 Sync 的关系

### 7.1 两个功能的区别

| 功能 | 作用 | 持续性 |
|------|------|--------|
| **调色盘** | 一次性应用所有颜色 | 一次性操作 |
| **Sync** | 持续同步属性修改 | 持续生效 |

### 7.2 推荐工作流

**场景 1：先 Palette 后 Sync**
1. 使用 Palette 设置初始颜色方案
2. 启用 Sync 统一调整其他属性（线宽、动画等）
3. 关闭 Sync 单独调整特定 Series

**场景 2：仅使用 Palette**
1. 使用 Palette 设置颜色
2. 保持 Sync 关闭
3. 逐条微调其他属性

**⚠️ 注意**：如果启用 Sync 后修改 `color`，会同步到所有 Series，覆盖 Palette 的颜色分配。

---

## 8. 使用建议

### 8.1 选择调色盘的原则

| 场景 | 推荐调色盘 |
|------|-----------|
| 商务报告 | Tableau 10, Google Charts |
| 科技风格 | Modern Blue |
| 环保/海洋 | Cool Ocean |
| 活动/促销 | Warm Sunset |
| 通用场景 | D3 Category |

### 8.2 颜色数量不足

如果 Series 数量 > 调色盘颜色组数：

- **方案 1**：编辑调色盘，添加更多颜色组
- **方案 2**：使用多个相近调色盘组合
- **方案 3**：手动调整循环后的颜色

### 8.3 品牌色方案

创建公司品牌调色盘：

1. 打开 `SeriesColorPalettes.asset`
2. 添加新调色盘，命名为 "Company Brand"
3. 使用公司的品牌色作为 `baseColor`
4. 让 `areaColor` 和 `highlightColor` 自动生成

---

## 9. 常见问题

### Q: 应用调色盘后颜色没变化？

检查：
1. 是否选中了正确的 `ChartProfile`
2. 图表是否有 Series（空图表无效果）
3. 尝试点击 Preview 面板的刷新按钮

### Q: 如何只修改一条 Serie 的颜色？

1. **不要点击调色盘**（会应用到所有）
2. 在 Series 列表中选中该 Serie
3. 在 Inspector 中直接修改 `color` 属性
4. 确保 Sync 开关为关闭状态

### Q: 调色盘会影响已有颜色设置吗？

**会覆盖**。点击调色盘会重新分配所有 Series 的颜色。如果想保留某些 Series 的颜色：

1. 记录当前颜色
2. 应用调色盘
3. 手动恢复特定 Series 的颜色

### Q: 如何导出/导入自定义调色盘？

调色盘存储在 `SeriesColorPalettes.asset` 中：

- **导出**：复制该文件发送给其他人
- **导入**：将文件放入项目 `Assets/EasyChart/Editor/` 目录

### Q: 渐变效果可以用调色盘设置吗？

**不能直接设置**。调色盘设置的是基础颜色（`baseColor`）。

如果需要渐变：
1. 使用 Palette 设置基础色
2. 在 Inspector 中手动启用 `gradient`
3. 配置渐变色

---

## 10. 快捷键

| 操作 | 方式 |
|------|------|
| 打开调色盘选择器 | 点击调色盘图标 |
| 应用配色方案 | 点击调色盘项 |
| 打开调色盘管理器 | 点击 Manage Palettes |

---

## 下一章

- `02_09-TextureFXLayers.md`：TextureFX Layers 编辑说明（Pro）
