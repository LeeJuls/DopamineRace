# 柱状图（Bar）

本章目标：把柱状图在 EasyChart 中的 **数据解释规则（`SeriesData.x/value`）**、**并列/堆叠的真实行为**、以及常用样式字段一次讲清楚。

---

## 1. 适用场景

- 类目对比（A/B/C 的值对比）
- 分组对比（同一类目下多条 Bar 并列）
- 堆叠总量（同一类目下多条 Bar 堆叠）

---

## 2. 最小可用配置（Checklist）

1. `ChartProfile.coordinateSystem = Cartesian2D`
2. 轴
   - X：通常 `AxisType.Category`（填写 `labels`）
   - Y：通常 `AxisType.Value`（建议从 0 起）
3. Series
   - 添加 1 条 `Serie`
   - `Serie.type = Bar`
   - `Serie.seriesData` 至少 1 个点

---

## 3. Inspector 对应字段

- **Axis Settings**
  - `cartesian.xAxisId / cartesian.yAxisId`
  - `axes[]`（X/Y 对应 AxisConfig）

- **Series**
  - `series[i].type = Bar`
  - `series[i].settings`：实际类型为 `BarSettings`
    - `barWidth`
    - `stacked` / `stackGroup`
    - `barGap` / `categoryGap`
    - `cornerRadius` / `cornerSegments`
    - `textureFill`（颜色/纹理）
    - `border` / `background`
    - `hover`（开启后支持拾取/高亮）

---

## 4. SeriesData 字段解释（按运行时代码）

柱状图核心使用：

- **类目/横向位置**：`SeriesData.x`
  - 运行时会对 `x` 做 `RoundToInt`，因此**你应该把它当作“类目索引”来用**。

- **柱高**：`SeriesData.value`

- `SeriesData.y` / `SeriesData.z` 对 Bar 图 **不参与渲染**（不要把 y 当柱高）。

---

## 5. 最常见模板：Category X + Value Y

### 5.1 X 轴（Category）

- `AxisType = Category`
- `labels = ["A","B","C",...]`
- 推荐 `labelPlacement = CellCenter`（柱子更容易居中对齐）

### 5.2 数据写法

- `x = 类目索引`（0/1/2...）
- `value = 柱高`

---

## 6. 分组柱（多系列并列）的真实规则

并列柱的关键点是：

- 多条 `Serie`，都 `type=Bar`
- 所有 serie 共享同一套 X 类目（同一套 labels）
- 每条 serie 的每个点使用相同的 `x` 索引落到同一个类目

并列间距相关字段：

- `BarSettings.barGap`：同一类目下，各组柱之间的间隔
- `BarSettings.categoryGap`：类目与类目之间的额外间隔（会影响边缘留白）

---

## 7. 堆叠柱（stacked）的真实规则

堆叠发生在“同一个 stackGroup 的 Bar serie”之间：

- `BarSettings.stacked = true`
- `BarSettings.stackGroup = "Group1"`

运行时堆叠逻辑要点：

- 对同一个 `x`（类目索引）分别累计正值/负值（正负会分开堆）
- 堆叠后的每根柱顶部 = 当前累计底 + `value`

---

## 8. 常见坑与排错

- **柱子夹在两个标签之间 / 对不齐**
  - 优先检查 X 轴 `labelPlacement`（建议 `CellCenter`）
  - 确认 `x` 是否为整数索引（运行时会 Round）

- **柱子从中间起，不从 0 起**
  - 检查 Y 轴（Value Axis）的 `autoRangeMin` 是否关闭并锁定 `minValue=0`

- **堆叠结果不对**
  - 检查是否所有需要堆叠的 serie 都设置了相同的 `stackGroup`
  - 注意：正值和负值会分别堆叠

---

## 9. 深入参考

- 轴与范围、Series 与数据：`00-WorkflowAndLibrary.md`
- 常用配方：`05-CommonRecipes.md`
- FAQ：`06-FAQ.md`
