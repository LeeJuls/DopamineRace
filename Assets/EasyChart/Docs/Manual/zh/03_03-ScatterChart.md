# 散点图（Scatter）

本章目标：把散点图在 EasyChart 的“数据字段约定”说清楚，尤其是 `SeriesData.y/value` 的兼容逻辑，以及 `z` 维度如何驱动 `sizeMapping`。

---

## 1. 适用场景

- 相关性分析（X/Y 两个数值维度）
- 分布展示（点云）
- 异常点识别（离群点）

---

## 2. 最小可用配置（Checklist）

1. `ChartProfile.coordinateSystem = Cartesian2D`
2. 轴
   - 常见：X=Value，Y=Value
3. Series
   - 添加 1 条 `Serie`
   - `Serie.type = Scatter`
   - `Serie.seriesData` 至少 2 个点

---

## 3. Inspector 对应字段

- `series[i].type = Scatter`
- `series[i].settings`：实际类型为 `ScatterSettings`
  - `point`：点样式（显示/大小/纹理）
  - `hover`：悬停高亮（enabled/pickRadius/scale 等，具体字段以版本为准）
  - `sizeMapping`：点大小映射

---

## 4. SeriesData 字段解释（按运行时代码）

散点图渲染时使用：

- **X 坐标**：`SeriesData.x`
- **Y 坐标**：优先使用 `SeriesData.y`
  - 兼容逻辑：如果 `y == 0` 且 `value != 0`，运行时会把 `value` 当成 y 来用
- **点大小映射维度**：`SeriesData.z`（当 `sizeMapping.enabled=true` 时）

因此你有两种常见写法：

### 4.1 推荐写法（显式 X/Y）

- `x = X 值`
- `y = Y 值`

### 4.2 兼容写法（旧数据：用 value 当 y）

- `x = X 值`
- `value = Y 值`
- `y = 0`

> 建议：新数据直接写 `y`，这样不会跟“点的其他含义（value）”混在一起。

---

## 5. 标准模板：Value X + Value Y

- X 轴：`AxisType = Value`
- Y 轴：`AxisType = Value`
- 数据：使用 4.1 的写法（x/y）

---

## 6. sizeMapping（点大小映射）的真实规则

当 `ScatterSettings.sizeMapping.enabled = true` 时：

- 点半径会根据 `SeriesData.z` 映射得到
- 映射范围：`minValue/maxValue` → `minSize/maxSize`
- 若 `clamp = true`，会把超范围的 t 值夹到 0..1
- `curve` 会对 t 做一次曲线变换（用于非线性映射）

如果你发现 sizeMapping “没效果”，优先检查：

- 是否真的给了 `z` 值（默认 0）
- `minValue/maxValue` 是否相等（相等会导致映射退化）

---

## 7. 常见坑与排错

- **点全在一条水平线**
  - 你可能只填了 `value`，但又把 `y` 也写成了非 0（兼容逻辑不会触发）
  - 建议统一用 `y` 作为纵坐标

- **hover 没反应**
  - `ScatterSettings.hover.enabled` 必须开启
  - `pickRadius` 太小也会导致很难拾取

- **点太小/太大**
  - 调整 `ScatterSettings.point.size`
  - 或检查 sizeMapping 的 `minSize/maxSize`

---

## 8. 深入参考

- 轴与范围、Series 与数据：`00-WorkflowAndLibrary.md`
- 常用配方：`05-CommonRecipes.md`
- FAQ：`06-FAQ.md`
