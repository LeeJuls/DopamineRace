# 3D 图表（3D Charts）(Pro)

本章介绍 EasyChart Pro 中可用的 3D 图表类型，包括 Line3D、Bar3D、Scatter3D 和 Pie3D。

---

## 1. 概述

3D 图表为数据可视化提供深度和透视效果。它们使用 Unity 的 3D 渲染管线渲染，支持：

- 交互式相机控制（旋转、缩放）
- 自动旋转动画
- 悬停效果
- TextureFXLayers 高级效果

**支持的 3D 图表类型：**
- **Line3D**：3D 折线/轨迹可视化
- **Bar3D**：带深度的 3D 柱状图
- **Scatter3D**：3D 散点图
- **Pie3D**：带透视的 3D 饼图

---

## 2. 坐标系统

3D 图表使用 `CoordinateSystemType.Cartesian3D`，提供：

- **X 轴**：水平方向（左右）
- **Y 轴**：垂直方向（高度/数值）
- **Z 轴**：深度方向（前后）

### 2.1 Cartesian3D 网格设置

在 Inspector 面板中配置 3D 网格：

- **show**：显示/隐藏网格
- **gridColor**：网格线颜色
- **gridLineWidth**：网格线宽度
- **gridWidth/Height/Depth**：3D 空间尺寸
- **showXYPlane/XZPlane/YZPlane**：切换各个平面
- **useAxisColors**：为每个轴使用不同颜色
- **showLabels**：显示轴标签
- **labelColor/fontSize**：标签样式

---

## 3. Line3D 图表

### 3.1 数据映射

- **X**：沿 X 轴的位置（类目索引或数值）
- **Y (value)**：高度/数值
- **Z**：深度位置

### 3.2 Line3DSettings

#### 描边设置 (`stroke`)
- **lineType**：`Straight`、`Smooth` 或 `Step`
- **width**：线宽（世界单位）
- **color**：线条颜色
- **textureFill**：基础纹理设置
- **textureFXLayers**：高级纹理效果 (Pro)

#### 点设置 (`point`)
- **show**：显示点标记
- **size**：点大小（世界单位）
- **texture**：自定义点纹理
- **color**：点颜色
- **textureFXLayers**：高级纹理效果 (Pro)

#### 相机设置 (`camera`)
- **yawDeg**：水平旋转角度
- **pitchDeg**：垂直旋转角度
- **distance**：相机到目标的距离
- **fov**：视野角度
- **target**：相机注视目标
- **autoRotate**：启用自动旋转
- **autoRotateSpeed**：旋转速度（度/秒）

#### 悬停设置 (`hover`)
- **enabled**：启用悬停检测
- **highlightIntensity**：悬停时亮度增加
- **scaleMultiplier**：悬停时大小增加

#### 背景设置 (`background`)
- **transparent**：使用透明背景
- **color**：背景颜色（如果不透明）

---

## 4. Bar3D 图表

### 4.1 数据映射

- **X**：类目位置
- **Y (value)**：柱高度
- **Z**：深度位置（用于多行）

### 4.2 Bar3DSettings

与 Line3D 类似的结构：
- 柱状图特定渲染设置
- 相机和悬停设置
- TextureFXLayers 支持

---

## 5. Scatter3D 图表

### 5.1 数据映射

- **X**：X 位置
- **Y (value)**：Y 位置（高度）
- **Z**：Z 位置（深度）

### 5.2 Scatter3DSettings

- 点渲染设置
- 大小映射选项
- 相机和悬停设置

---

## 6. Pie3D 图表

### 6.1 数据映射

- **value**：扇区大小
- **name**：扇区标签

### 6.2 Pie3DSettings

- 扇区渲染设置
- 爆炸/分离设置
- 相机设置
- 标签定位

---

## 7. 通用 3D 功能

### 7.1 相机自动旋转

为展示显示启用持续旋转：

```
camera.autoRotate = true
camera.autoRotateSpeed = 30  // 度/秒
```

### 7.2 悬停检测

3D 图表使用射线检测支持悬停检测：

1. 启用 `hover.enabled`
2. 鼠标悬停时点/柱会高亮
3. 使用 `highlightIntensity` 和 `scaleMultiplier` 自定义

### 7.3 TextureFXLayers

3D 图表支持 TextureFXLayers：
- 线条上的 UV 动画
- 点上的颜色动画
- 详见 [纹理特效层](./03_08-TextureFXLayers.md)

---

## 8. 性能考虑

- 3D 图表渲染到 RenderTexture
- 自动旋转会触发持续重绘
- 悬停检测使用物理射线检测
- 不需要时考虑禁用功能

---

## 9. 示例设置

### Line3D 快速开始

1. 创建新的 ChartProfile
2. 设置 `coordinateSystem = Cartesian3D`
3. 添加 `type = Line3D` 的 Series
4. 添加带有 X、value (Y) 和 Z 坐标的数据点
5. 配置相机设置以获得所需的视角

### 示例数据

```json
{
  "series": [{
    "type": "Line3D",
    "seriesData": [
      {"x": 0, "value": 10, "z": 0},
      {"x": 1, "value": 25, "z": 0.5},
      {"x": 2, "value": 15, "z": 1},
      {"x": 3, "value": 30, "z": 1.5}
    ]
  }]
}
```

---

## 10. 延伸阅读

- [纹理特效层](./03_08-TextureFXLayers.md)
- [常用配方](./04_08-CommonRecipes.md)
- [FAQ](./04_09-FAQ.md)
