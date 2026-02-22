# Line Chart (Line)

This chapter explains the key points of configuring a Line chart in EasyChart: how to set it up, how data is interpreted, and which fields affect rendering.

---

## 1. Use cases

- Trend changes (time series / category-based)
- Comparing multiple curves (same X dimension)
- Line styles such as smooth / step / straight

---

## 2. Minimum viable setup (checklist)

1. `ChartProfile.coordinateSystem = Cartesian2D`
2. Axes (Axis Settings)
   - X: usually **Category** (fill `labels`) or **Value** (numeric X axis)
   - Y: usually **Value**
3. Series (Series panel)
   - Add 1 `Serie`
   - `Serie.type = Line`
   - `Serie.seriesData` has at least 2 points (a line needs at least two points)

---

## 3. Inspector fields (what you see in panels)

- **ChartProfile / Coordinate System**
  - `coordinateSystem`

- **Axis Settings** (depends on coordinate system)
  - Cartesian: `cartesian.xAxisId / cartesian.yAxisId`
  - Axis list: `axes[]` (each Axis has `axisType/labels/min/max/autoRange/...`)

- **Series** (each curve)
  - `series[i].type = Line`
  - `series[i].settings`: actual type is `LineSettings`
    - `stroke`: line stroke (type/color/width/texture, etc.)
    - `point`: point marker style (toggle/size/texture, etc.)
    - `hover`: hover emphasis (enables pick radius / highlight)
    - `area`: area fill (fill under the line)
  - `series[i].labelSettings`: point labels (visibility/format/decimals/offset, etc.)

---

## 4. SeriesData field interpretation (runtime behavior)

Line chart uses:

- **X coordinate**: `SeriesData.x`
- **Y value**: `SeriesData.value`
- `SeriesData.y` is **not used for rendering** in line charts (do not treat `y` as the Y value).

Two common patterns:

### 4.1 Category X + Value Y (most common)

- X axis: `AxisType.Category`
- `AxisConfig.labels = ["A","B","C",...]`
- Data points:
  - `x = category index` (0/1/2..., mapped into labels)
  - `value = numeric value`

### 4.2 Value X + Value Y (numeric X axis)

- X axis: `AxisType.Value`
- Data points:
  - `x = X value`
  - `value = Y value`

> Additional note: when your axis dimensions are **X=Value, Y=Category**, runtime treats it as a transposed Cartesian layout (`transposed`) and swaps how X/Y are interpreted during rendering (useful for horizontal layouts).

---

## 5. Common style settings (LineSettings)

- **Line type**: `LineSettings.stroke.lineType`
  - `Straight`: straight lines
  - `Step`: step line
  - `Smooth`: smooth curve

- **Stroke width/color**: `LineSettings.stroke.width` / `LineSettings.stroke.color`

- **Point markers**: `LineSettings.point.show/size/textureFill`

- **Area fill**: `LineSettings.area.show` + `LineSettings.area.textureFill`

---

## 6. Common pitfalls and troubleshooting (by symptoms)

- **Line breaks / not visible**
  - Check whether `SeriesData.value` contains `NaN/Infinity`
  - A line needs at least 2 valid points

- **Points do not align with labels (Category X)**
  - Check that `x` is an index within 0..(labels.Count-1)
  - Do not write `x` as a category string (EasyChart uses index, not string)

- **I filled `y`, but the chart is wrong**
  - Line chart uses `value` as the Y value, not `y`

---

## 7. Further reading

- Axes/range, Series and data: `00_02-WorkflowAndLibrary.md`
- Common recipes: `04_08-CommonRecipes.md`
- FAQ: `04_09-FAQ.md`
