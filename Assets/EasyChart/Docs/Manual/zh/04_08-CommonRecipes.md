# 常用配方（Common Recipes）

本章目标：把常用图表的“最低可用配置”整理成可照抄的配方（Series + Axis + 常见坑），用于你快速创建/排错。

---

## 0. 开始之前：最小检查清单

当你遇到“怎么都看不到/显示很怪”时，先按顺序检查：

1. `coordinateSystem` 是否与 SeriesType 匹配（Cartesian2D vs Polar2D）
2. `series` 是否至少 1 条，且该 Serie 的 `seriesData.Count > 0`
3. 轴类型是否匹配数据含义：
   - Category 轴：`labels` 非空，且数据点的 `x/y` 是索引（0/1/2...）
   - Value 轴：数据点 `x/y` 是连续数值
4. 是否存在 NaN/Infinity
5. 是否把 Value 轴范围“锁死”了（`autoRangeMin/autoRangeMax` 或固定 min/max），导致数据落在范围外

---

## 1. 折线图（Line）：类目 X + 数值 Y

### 目标效果

- X：类目标签（A/B/C/D）
- Y：数值
- 折线按类目对齐

### 配方

1. `coordinateSystem = Cartesian2D`
2. X 轴：
   - `axisType = Category`
   - `labels = [A, B, C, D]`
   - `LabelPlacement = Tick`
3. Y 轴：
   - `axisType = Value`
   - `autoRangeMin/autoRangeMax = true`
4. Series：
   - `type = Line`
   - 数据点：`x=类目索引`，`y=数值`

数据示例（概念）：

```txt
(x=0, y=10)
(x=1, y=20)
(x=2, y=15)
(x=3, y=30)
```

### 常见坑

- **点不落在标签上**：检查 `x` 是否从 0 开始，是否越界（labels.Count）
- **线看起来“断了/跳”**：检查是否有 NaN/Infinity

---

## 2. 柱状图（Bar）：类目居中 + Y 从 0 起

### 目标效果

- 每个类目一个柱子
- 标签在柱子中心对齐
- Y 轴从 0 起，避免误导

### 配方

1. `coordinateSystem = Cartesian2D`
2. X 轴：
   - `axisType = Category`
   - `labels` 填类目
   - `LabelPlacement = CellCenter`
3. Y 轴：
   - `axisType = Value`
   - 固定从 0 开始（例如 `minValue=0` + `autoRangeMax=true`，或等价字段组合）
4. Series：
   - `type = Bar`
   - `BarSettings.barWidth` 调整柱宽

数据示例：

```txt
(x=0, y=12)
(x=1, y=18)
(x=2, y=9)
```

### 常见坑

- **柱子夹在两个标签之间**：把 `LabelPlacement` 切到 `CellCenter`
- **柱子太挤/太疏**：调 `barWidth`、`barGap`、`categoryGap`

---

## 3. 并列柱（Grouped Bar）：多条 Serie 共享同一套类目

### 配方

- 多条 `Serie`，都设置 `type = Bar`
- 每条 Serie 都写同一套 `x=类目索引`
- 用 `Serie.name` 区分组名（图例/tooltip 会用到）

示例（概念）：

```txt
Serie A:
  (x=0, y=10) (x=1, y=12)
Serie B:
  (x=0, y=8)  (x=1, y=15)
```

---

## 4. 堆叠柱（Stacked Bar）：stacked + stackGroup

### 配方

- 需要堆叠的 Bar 系列：
  - `BarSettings.stacked = true`
  - `BarSettings.stackGroup = "Group1"`（同组会堆叠）

### 常见坑

- **堆叠后高度看起来不对**：确认所有参与堆叠的系列 `stackGroup` 完全一致

---

## 5. 散点图（Scatter）：Value X/Y + hover + sizeMapping

### 目标效果

- X/Y 都是连续数值
- 鼠标移上去点会变大（hover）
- 点大小可按某个维度映射（sizeMapping）

### 配方

1. `coordinateSystem = Cartesian2D`
2. X/Y 轴都设为 `Value`
3. `type = Scatter`
4. 数据点：至少 `x/value`，可选使用 `z` 作为第三维
5. `ScatterSettings.hover.enabled = true`

### 常见坑

- **点太小看不见**：提高 `PointSettings.size`
- **hover 没反应**：检查 `HoverHighlightSettings.enabled` 和 `pickRadius`

---

## 6. 热力图（Heatmap）：(x, y, value) 三元组

### 目标效果

- X/Y 是类目轴（二维标签）
- 颜色由 value 决定

### 配方

1. `coordinateSystem = Cartesian2D`
2. X 轴：Category + labels（列标签）
3. Y 轴：Category + labels（行标签）
4. `type = Heatmap`
5. 数据点：
   - `x = 列索引`
   - `y = 行索引`
   - `value = 强度`

示例（概念）：

```txt
(x=0, y=0, value=0.2)
(x=1, y=0, value=0.8)
(x=0, y=1, value=0.5)
```

### 常见坑

- **所有格子同一颜色**：检查 `HeatmapSettings.autoRange/minValue/maxValue/clamp`
- **格子太小/太密**：调 `cellSizePx` / `cellGapPx`

---

## 7. 雷达图（Radar）：维度索引 x + 数值 value

### 配方

1. `coordinateSystem = Polar2D`
2. `type = Radar`
3. 数据点：
   - `x = 维度索引`
   - `value = 数值`
   - `name = 维度名`（建议填，便于标签/tooltip）

示例：

```txt
(x=0, value=72, name="攻击")
(x=1, value=55, name="防御")
(x=2, value=90, name="速度")
```

### 常见坑

- **雷达图标签乱/缺失**：确保维度标签来源一致（不要依赖 Cartesian 的 axis 配置）
- **看不到雷达**：检查 `coordinateSystem` 是否为 Polar2D

---

## 8. 交互/tooltip 稳定性：SeriesData.id

如果你启用了选中、tooltip 或 hover，一般建议：

- 每个数据点的 `SeriesData.id` 保持稳定

> 否则当你每次刷新数据都生成一套新 id，会造成交互状态无法关联。

---

## 下一章

- 如果你希望继续写：可以新增 `06-FAQ.md`（常见问题 + 最快排错路线）。
