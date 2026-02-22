# 折线图（Line）

本章目标：把“折线图在 EasyChart 里到底怎么配置、数据怎么解释、哪些字段会影响渲染”的关键点一次讲清楚。

---

## 1. 适用场景

- 趋势变化（时间序列/按类目变化）
- 多条曲线对比（同一套 X 维度）
- 需要平滑/阶梯/直线等线型表达

---

## 2. 最小可用配置（Checklist）

1. `ChartProfile.coordinateSystem = Cartesian2D`
2. 轴（Axis Settings）
   - X：通常用 **Category**（填写 `labels`）或 **Value**（数值横轴）
   - Y：通常用 **Value**
3. Series（Series 面板）
   - 添加 1 条 `Serie`
   - `Serie.type = Line`
   - `Serie.seriesData` 至少 2 个点（折线需要至少两点才能连线）

---

## 3. Inspector 对应字段（你在面板里看到的是什么）

- **ChartProfile / Coordinate System**
  - `coordinateSystem`

- **Axis Settings**（与坐标系相关）
  - Cartesian：`cartesian.xAxisId / cartesian.yAxisId`
  - 轴列表：`axes[]`（每个 Axis 的 `axisType/labels/min/max/autoRange/...`）

- **Series**（每条曲线）
  - `series[i].type = Line`
  - `series[i].settings`：实际类型为 `LineSettings`
    - `stroke`：线条（线型/颜色/宽度/纹理等）
    - `point`：点样式（开关/大小/纹理等）
    - `hover`：悬停强调（开启后才会有“拾取半径/高亮”）
    - `area`：面积填充（折线下方填充）
  - `series[i].labelSettings`：数据点标签（是否显示、格式、小数位、偏移等）

---

## 4. SeriesData 字段解释（最关键，按运行时代码）

折线图渲染时使用：

- **X 坐标**：`SeriesData.x`
- **Y 数值**：`SeriesData.value`
- `SeriesData.y` 在折线图中 **不参与渲染**（不要把 y 当作折线的纵值）。

两种常见写法：

### 4.1 Category X + Value Y（最常用）

- X 轴设为 `AxisType.Category`
- `AxisConfig.labels = ["A","B","C",...]`
- 数据点：
  - `x = 类目索引`（0/1/2...，会按索引映射到 labels）
  - `value = 数值`

### 4.2 Value X + Value Y（数值横轴）

- X 轴设为 `AxisType.Value`
- 数据点：
  - `x = 横轴数值`
  - `value = 纵轴数值`

> 额外说明：当你的轴维度是 **X=Value, Y=Category** 时，运行时会认为是“笛卡尔坐标转置”（transposed），会在渲染时交换 X/Y 的解释方式（用于横向布局的场景）。

---

## 5. 常用样式配置（LineSettings）

- **线型**：`LineSettings.stroke.lineType`
  - `Straight`：直线
  - `Step`：阶梯线
  - `Smooth`：平滑曲线

- **线条粗细/颜色**：`LineSettings.stroke.width` / `LineSettings.stroke.color`

- **点标记**：`LineSettings.point.show/size/textureFill`

- **面积填充**：`LineSettings.area.show` + `LineSettings.area.textureFill`

---

## 6. 常见坑与排错（按现象）

- **线断裂 / 不显示**
  - 检查 `SeriesData.value` 是否出现 `NaN/Infinity`
  - 折线至少 2 个有效点

- **点不在标签上（Category X）**
  - 检查 `x` 是否为 0..(labels.Count-1) 的索引
  - 检查是否误把 `x` 写成了“类目字符串”（EasyChart 这里是索引，不是字符串）

- **我填了 y，但图不对**
  - 折线图纵值用的是 `value`，不是 `y`

---

## 7. 深入参考

- 轴与范围、Series 与数据：`00-WorkflowAndLibrary.md`
- 常用配方：`05-CommonRecipes.md`
- FAQ：`06-FAQ.md`
