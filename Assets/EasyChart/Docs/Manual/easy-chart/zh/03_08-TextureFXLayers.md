# 纹理特效层（TextureFXLayers）(Pro)

本章介绍 EasyChart Pro 中的 TextureFXLayers 功能，该功能为图表元素提供高级纹理效果和动画。

---

## 1. 概述

TextureFXLayers 是 Pro 功能，允许您为图表元素应用动画纹理效果，包括：

- **线条描边** (Line, Line3D)
- **区域填充** (Line area)
- **点标记** (Line, Scatter, Line3D)
- **柱状图填充** (Bar)
- **雷达图填充** (Radar)

每个元素可以有多个纹理层，每层有独立的动画设置。

---

## 2. TextureFXLayer 属性

### 2.1 基础纹理设置 (`fill`)

- **Texture**: 要应用的纹理
- **Color**: 纹理的着色颜色
- **Tiling**: UV 平铺（缩放）
- **Offset**: UV 偏移

### 2.2 动画设置

#### 动画类型 (`animationType`)

- **None**: 无 UV 动画
- **TextureUvMove**: 持续移动纹理 UV 坐标
- **TextureScale**: 动画纹理缩放（放大/缩小）

#### UV 移动设置 (当 `animationType = TextureUvMove`)

- **uvMoveSpeed**: UV 移动速度（默认：`(1, 0)` - 向右移动）
  - 正 X：纹理向右移动
  - 正 Y：纹理向上移动

#### 缩放设置 (当 `animationType = TextureScale`)

- **scaleType**: 缩放动画类型
  - `ZoomIn`: 从 `scaleFrom` 到 `scaleTo` 重复缩放
  - `ZoomOut`: 从 `scaleTo` 到 `scaleFrom` 重复缩放
  - `Sin`: 在 `scaleFrom` 和 `scaleTo` 之间正弦振荡
  - `PingPong`: 在 `scaleFrom` 和 `scaleTo` 之间往返
- **scaleSpeed**: 缩放动画速度
- **scaleFrom**: 起始缩放（默认：`(1, 1)`）
- **scaleTo**: 结束缩放（默认：`(1.2, 1.2)`）

### 2.3 颜色动画设置

颜色动画可以与 UV/缩放动画组合使用。

- **colorAnimationType**: 颜色动画类型（默认：`PingPong`）
  - `None`: 无颜色动画
  - `Loop`: 从头到尾循环渐变
  - `PingPong`: 往返渐变
  - `Clamp`: 播放一次并保持在结束状态
- **colorAnimationSpeed**: 颜色动画速度（默认：`1`）
- **colorAnimationGradient**: 定义随时间变化的颜色渐变
  - 默认：白色，透明度从 255 渐变到 0

### 2.4 淡入淡出效果设置

- **fadeType**: 淡入淡出类型
  - `None`: 无淡入淡出
  - `Edge`: 所有边缘淡出
  - `Center`: 中心淡出（透明中心）
  - `Radial`: 径向晕影（边缘淡出）
  - `DirectionHorizontal`: 水平渐变淡出
  - `DirectionVertical`: 垂直渐变淡出
- **fadeIntensity**: 淡入淡出强度 (0-1)
- **fadeSoftness**: 淡入淡出过渡柔和度 (0-1)

### 2.5 变形效果设置

- **deformType**: UV 变形类型
  - `None`: 无变形
  - `Wave`: 正弦波扭曲
  - `Rotate`: 围绕中心的 UV 旋转
  - `Pulse`: 从中心径向扩展
- **deformIntensity**: 变形强度
- **deformSpeed**: 变形动画速度
- **waveFrequency**: 波浪变形的频率（默认：`4`）

---

## 3. 支持的图表类型

### 3.1 折线图 (2D)

TextureFXLayers 可应用于：

- `LineSettings.stroke.textureFXLayers` - 线条描边
- `LineSettings.area.textureFXLayers` - 线下区域填充
- `LineSettings.point.textureFXLayers` - 点标记

### 3.2 柱状图 (2D)

- `BarSettings.textureFXLayers` - 柱状图填充

### 3.3 散点图 (2D)

- `ScatterSettings.point.textureFXLayers` - 点标记

### 3.4 雷达图 (2D)

- `RadarSettings.stroke.textureFXLayers` - 雷达图描边
- `RadarSettings.area.textureFXLayers` - 雷达图区域填充
- `RadarSettings.point.textureFXLayers` - 点标记

### 3.5 3D 折线图 (Pro)

- `Line3DSettings.stroke.textureFXLayers` - 3D 线条描边
- `Line3DSettings.point.textureFXLayers` - 3D 点标记

### 3.6 暂不支持

以下图表类型目前不支持 TextureFXLayers：

- **饼图** - 使用不同的渲染方式
- **圆环图** - 使用不同的渲染方式
- **热力图** - 使用颜色插值代替

---

## 4. 编辑器工作流

### 4.1 添加 TextureFXLayers

1. 在 Series 面板中选择一个系列
2. 展开设置折叠面板（如 "Line"、"Point"、"Area"）
3. 找到 `Texture FX Layers` 属性
4. 点击 "+" 添加新层
5. 配置层属性

### 4.2 复制/粘贴支持

TextureFXLayers 支持复制/粘贴功能：

- 右键点击一个层进行复制
- 右键点击另一个层进行粘贴
- 渐变属性在复制/粘贴过程中完全保留

---

## 5. 默认值

创建新的 TextureFXLayer 时，应用以下默认值：

| 属性 | 默认值 |
|------|--------|
| `uvMoveSpeed` | `(1, 0)` |
| `colorAnimationType` | `PingPong` |
| `colorAnimationGradient` | 白色，透明度 255→0 |
| `scaleFrom` | `(1, 1)` |
| `scaleTo` | `(1.2, 1.2)` |

---

## 6. 性能考虑

- 启用动画的 TextureFXLayers 会触发持续重绘
- 在移动设备上谨慎使用
- 当图表不可见时考虑禁用动画
- 多个层会叠加，可能影响性能

---

## 7. 示例

### 7.1 流水效果（线条）

```
animationType: TextureUvMove
uvMoveSpeed: (0.5, 0)
```

### 7.2 脉冲发光效果（点）

```
colorAnimationType: PingPong
colorAnimationSpeed: 2
colorAnimationGradient: 白色 (透明度 1→0.3→1)
```

### 7.3 呼吸缩放效果（柱状图）

```
animationType: TextureScale
scaleType: Sin
scaleSpeed: 1
scaleFrom: (1, 1)
scaleTo: (1.1, 1.1)
```

---

## 8. 延伸阅读

- [折线图](./03_01-LineChart.md)
- [柱状图](./03_02-BarChart.md)
- [常用配方](./04_08-CommonRecipes.md)
