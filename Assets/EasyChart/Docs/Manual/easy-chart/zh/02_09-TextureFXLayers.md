# TextureFX Layers 编辑（Pro）

本章介绍如何编辑 **TextureFX Layers**（纹理特效层），为图表添加高级视觉效果。TextureFX Layers 可在 Series 面板、Inspector 面板等多个位置进行配置。

**⚠️ 注意**：TextureFX Layers 是 **Pro 版本**功能。

---

## 1. TextureFX Layers 位置

TextureFX Layers 属性位于不同图表类型的设置中：

### 1.1 可应用的位置

| 图表类型 | 设置路径 |
|---------|---------|
| **Line（折线图）** | Line Settings → `textureFXLayers` |
| **Line（区域）** | Line Settings → Area → `textureFXLayers` |
| **Line（点）** | Line Settings → Point → `textureFXLayers` |
| **Bar（柱状图）** | Bar Settings → `textureFXLayers` |
| **Scatter（散点）** | Scatter Settings → Point → `textureFXLayers` |
| **Radar（雷达图）** | Radar Settings → Line/Area/Point → `textureFXLayers` |
| **Line3D（3D折线）** | Line3D Settings → Line/Point → `textureFXLayers`（Pro） |

### 1.2 如何找到

1. 在 Library 面板选中 `ChartProfile`
2. 在 Series 面板选中一条 Series
3. 在 Inspector 面板展开对应的设置折叠栏
4. 找到 `Texture FX Layers` 属性

---

## 2. TextureFX Layer 列表界面

### 2.1 列表视图

`Texture FX Layers` 显示为可折叠列表：

```
Texture FX Layers
├── [0] Layer 0  ▼
├── [1] Layer 1  ▶
└── + (Add)
```

- 点击 `+` 添加新层
- 点击层名称展开/折叠详细设置
- 右键点击层显示上下文菜单

### 2.2 层的操作

右键点击层，显示菜单：

| 选项 | 功能 |
|------|------|
| **Copy** | 复制该层的所有设置 |
| **Paste** | 粘贴到当前层（覆盖） |
| **Duplicate** | 复制该层并创建新层 |
| **Delete** | 删除该层 |
| **Move Up** | 向上移动 |
| **Move Down** | 向下移动 |

**层的顺序**：上层会覆盖下层，如层 1 在层 0 之上渲染。

---

## 3. Layer 详细设置

展开一个 Layer，显示以下设置组：

### 3.1 基础纹理 (Fill)

| 属性 | 说明 | 默认值 |
|------|------|--------|
| `Texture` | 纹理资源 | None |
| `Color` | 纹理着色颜色 | 白色 |
| `Tiling` | UV 平铺（缩放）| (1, 1) |
| `Offset` | UV 偏移 | (0, 0) |

**Tiling 示例**：
- `(1, 1)` - 正常显示
- `(2, 1)` - 水平重复 2 次
- `(1, 0.5)` - 垂直压缩

### 3.2 动画类型 (Animation)

#### Animation Type

| 选项 | 效果 |
|------|------|
| **None** | 静态纹理，无动画 |
| **Texture UV Move** | 纹理持续移动（流水效果）|
| **Texture Scale** | 纹理缩放动画（呼吸效果）|

#### 当 Animation Type = Texture UV Move

| 属性 | 说明 | 默认 |
|------|------|------|
| `UV Move Speed` | UV 移动速度 | (1, 0) |

- X > 0：纹理向右移动
- X < 0：纹理向左移动
- Y > 0：纹理向上移动
- Y < 0：纹理向下移动

**常用设置**：
- 水平流动：`(0.5, 0)`
- 垂直流动：`(0, 0.5)`
- 对角线：`(0.3, 0.3)`

#### 当 Animation Type = Texture Scale

| 属性 | 说明 | 默认 |
|------|------|------|
| `Scale Type` | 缩放模式 | `Sin` |
| `Scale Speed` | 缩放速度 | `1` |
| `Scale From` | 起始缩放 | `(1, 1)` |
| `Scale To` | 结束缩放 | `(1.2, 1.2)` |

**Scale Type 选项**：

| 类型 | 效果 |
|------|------|
| `Zoom In` | 从小放大到大，循环 |
| `Zoom Out` | 从大缩小到小，循环 |
| `Sin` | 正弦波平滑缩放 |
| `PingPong` | 放大→缩小→放大 |

### 3.3 颜色动画 (Color Animation)

颜色动画可以与 UV/缩放动画同时使用。

| 属性 | 说明 | 默认 |
|------|------|------|
| `Color Animation Type` | 颜色动画模式 | `PingPong` |
| `Color Animation Speed` | 动画速度 | `1` |
| `Color Animation Gradient` | 颜色渐变定义 | 白→透明 |

**Color Animation Type**：

| 类型 | 效果 |
|------|------|
| `None` | 无颜色动画 |
| `Loop` | 从头到尾循环 |
| `PingPong` | 往返渐变 |
| `Clamp` | 播放一次保持 |

**Gradient 设置**：
- 点击 Gradient 字段打开编辑器
- 添加/删除颜色关键点
- 调整时间位置和透明度

### 3.4 淡入淡出 (Fade)

| 属性 | 说明 | 默认 |
|------|------|------|
| `Fade Type` | 淡出类型 | `None` |
| `Fade Intensity` | 强度 (0-1) | `0.5` |
| `Fade Softness` | 柔和度 (0-1) | `0.5` |

**Fade Type 选项**：

| 类型 | 效果 |
|------|------|
| `None` | 无淡出 |
| `Edge` | 边缘淡出 |
| `Center` | 中心透明，边缘可见 |
| `Radial` | 径向晕影 |
| `Direction Horizontal` | 水平渐变 |
| `Direction Vertical` | 垂直渐变 |

### 3.5 变形效果 (Deform)

| 属性 | 说明 | 默认 |
|------|------|------|
| `Deform Type` | 变形类型 | `None` |
| `Deform Intensity` | 变形强度 | `0.1` |
| `Deform Speed` | 动画速度 | `1` |
| `Wave Frequency` | 波浪频率 | `4` |

**Deform Type 选项**：

| 类型 | 效果 |
|------|------|
| `None` | 无变形 |
| `Wave` | 正弦波浪扭曲 |
| `Rotate` | UV 旋转 |
| `Pulse` | 径向脉冲扩展 |

---

## 4. 常用效果配置

### 4.1 流水效果（线条）

适用于数据线、进度条：

```
Animation Type: Texture UV Move
UV Move Speed: (0.5, 0)
Texture: 条纹纹理或箭头纹理
```

### 4.2 脉冲发光（点标记）

适用于散点图、数据点高亮：

```
Animation Type: Texture Scale
Scale Type: Sin
Scale Speed: 2
Scale From: (1, 1)
Scale To: (1.3, 1.3)

Color Animation Type: PingPong
Color Animation Speed: 2
Color Animation Gradient: 白色 (alpha 1 → 0.3 → 1)
```

### 4.3 呼吸柱状图

适用于 Bar 图表：

```
Animation Type: Texture Scale
Scale Type: Sin
Scale Speed: 1
Scale From: (1, 1)
Scale To: (1.05, 1.05)

Texture: 渐变纹理或格子纹理
```

### 4.4 流动区域填充

适用于折线图区域：

```
Animation Type: Texture UV Move
UV Move Speed: (0.2, 0.1)
Fade Type: Edge
Fade Intensity: 0.3

Texture: 噪点纹理或流动纹理
```

### 4.5 波浪线条

适用于装饰性线条：

```
Deform Type: Wave
Deform Intensity: 0.05
Deform Speed: 1.5
Wave Frequency: 6

Animation Type: Texture UV Move
UV Move Speed: (0.3, 0)
```

---

## 5. 层的组合使用

多个 Layer 可以叠加产生复杂效果：

### 示例：发光 + 流动

**Layer 0（底层）**：发光效果
```
Texture: 圆形渐变纹理
Color: 蓝色
Animation Type: Texture Scale
Scale Type: Sin
Scale From: (1, 1)
Scale To: (1.5, 1.5)
```

**Layer 1（上层）**：流动效果
```
Texture: 条纹纹理
Color: 白色
Animation Type: Texture UV Move
UV Move Speed: (1, 0)
Fade Type: Edge
```

效果：蓝色发光底色上，白色条纹持续流动。

### 层顺序原则

- **Layer 0**：基础层，建议放静态或缓慢动画
- **Layer 1+**：叠加层，可以放动态效果
- **透明纹理**：上层使用半透明纹理可以看到下层

---

## 6. 性能考虑

### 6.1 动画开销

启用动画的 Layer 会导致图表**每帧重绘**：

| 情况 | 性能影响 |
|------|---------|
| 无动画 Layer | 正常渲染，无额外开销 |
| 1 个动画 Layer | 轻微开销 |
| 3+ 动画 Layer |  noticeable 开销 |
| 图表不可见 | 动画自动暂停 |

### 6.2 优化建议

1. **限制动画层数**：最多 2-3 个动画层
2. **静态替代**：如果不需要动画，设置 Animation Type 为 None
3. **可见性检测**：当图表不在屏幕内时，动画自动暂停
4. **移动设备**：谨慎使用，建议使用静态 Layer
5. **复杂图表**：大量 Series 时减少动画层数

### 6.3 纹理资源

- 使用**小尺寸纹理**（256x256 或 512x512）
- 启用 **Mipmaps** 以获得更好的缩放质量
- 使用 **Repeat** 环绕模式（自动处理）

---

## 7. 复制/粘贴工作流

### 7.1 复制 Layer 设置

1. 配置好一个 Layer 的所有属性
2. 右键点击该 Layer
3. 选择 **Copy**
4. 切换到另一个 Series
5. 找到 Texture FX Layers，右键点击 Layer
6. 选择 **Paste**

### 7.2 跨图表类型复制

TextureFX Layers 设置**可以在不同图表类型间复制**：

- Line 的 Layer → 粘贴到 Bar
- Bar 的 Layer → 粘贴到 Scatter
- 注意：某些特定设置可能不适用

### 7.3 批量应用

如果多条 Series 需要相同效果：

1. 配置好一条 Serie 的 TextureFX Layers
2. **启用 Sync**（Series 面板顶部）
3. 复制 Layer
4. 粘贴到同类型其他 Series
5. **Sync 会自动同步**所有 Layer 设置

---

## 8. 预览与调试

### 8.1 实时预览

所有 TextureFX 修改都会**实时反映在 Preview 面板**：

- 调整参数立即看到效果
- 动画效果在 Preview 中自动播放
- 可以暂停/播放动画查看细节

### 8.2 调试图层

如果效果不符合预期：

1. **检查 Layer 顺序**：上层遮挡下层
2. **检查透明度**：透明纹理才能看到叠加效果
3. **简化测试**：先只留一个 Layer，确认基础效果
4. **检查纹理**：确保纹理已正确赋值，不是 None

---

## 9. 不支持 TextureFX 的图表

以下图表类型**暂不支持** TextureFX Layers：

| 图表类型 | 原因 |
|---------|------|
| **Pie（饼图）** | 使用扇形渲染方式 |
| **Ring（圆环图）** | 使用环形渲染方式 |
| **Heatmap（热力图）** | 使用颜色插值代替纹理 |
| **Gauge（仪表盘）** | 使用自定义渲染 |

---

## 10. 常见问题

### Q: 为什么动画不播放？

检查：
1. **Animation Type** 不是 `None`
2. **Speed** 不是 0
3. Preview 面板是否可见
4. 图表是否被隐藏（`visible` 属性）

### Q: 多层叠加后看不到下层？

- 上层纹理需要**透明通道（Alpha）**
- 检查上层 `Color` 的透明度（A 值）
- 使用 `Fade Type` 创建渐变透明

### Q: 纹理显示为纯色方块？

- 纹理可能**未赋值**（显示为白色）
- 检查纹理的 **Wrap Mode** 是否为 Repeat
- 检查 **Tiling** 设置是否合理

### Q: 如何创建循环无缝动画？

- 使用 **UV Move** 动画
- 选择**无缝纹理**（Seamless Texture）
- 设置合适的 **Tiling** 值

### Q: TextureFX 会影响数据更新吗？

**不会**。TextureFX 是纯视觉效果，不影响数据注入或图表刷新。

---

## 11. 延伸阅读

- `03_01-LineChart.md`：折线图详细说明
- `03_02-BarChart.md`：柱状图详细说明
- `03_08-TextureFXLayers.md`：TextureFX 功能总览
- `04_08-CommonRecipes.md`：常用效果配方

---

## 下一章

- `03_01-LineChart.md`：折线图完整配置指南
