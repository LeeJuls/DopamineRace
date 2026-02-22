# Bar Chart (Bar)

This chapter explains bar charts in EasyChart: the data interpretation rules (`SeriesData.x/value`), how grouping/stacking really behaves, and commonly used style fields.

---

## 1. Use cases

- Category comparisons (compare values of A/B/C)
- Grouped comparison (multiple Bar series side-by-side under the same category)
- Stacked totals (stack bars within the same category)

---

## 2. Minimum viable setup (checklist)

1. `ChartProfile.coordinateSystem = Cartesian2D`
2. Axes
   - X: usually `AxisType.Category` (fill `labels`)
   - Y: usually `AxisType.Value` (recommended to start from 0)
3. Series
   - Add 1 `Serie`
   - `Serie.type = Bar`
   - `Serie.seriesData` has at least 1 point

---

## 3. Inspector fields

- **Axis Settings**
  - `cartesian.xAxisId / cartesian.yAxisId`
  - `axes[]` (AxisConfig for X/Y)

- **Series**
  - `series[i].type = Bar`
  - `series[i].settings`: actual type is `BarSettings`
    - `barWidth`
    - `stacked` / `stackGroup`
    - `barGap` / `categoryGap`
    - `cornerRadius` / `cornerSegments`
    - `textureFill` (color/texture)
    - `border` / `background`
    - `hover` (enables picking/highlight)

---

## 4. SeriesData field interpretation (runtime behavior)

Bar charts primarily use:

- **Category / horizontal position**: `SeriesData.x`
  - Runtime rounds `x` with `RoundToInt`, so **treat it as a category index**.

- **Bar height**: `SeriesData.value`

- `SeriesData.y` / `SeriesData.z` are **not used for rendering** in Bar charts (do not treat `y` as height).

---

## 5. Most common template: Category X + Value Y

### 5.1 X axis (Category)

- `AxisType = Category`
- `labels = ["A","B","C",...]`
- Recommended: `labelPlacement = CellCenter` (easier to center-align bars)

### 5.2 Data pattern

- `x = category index` (0/1/2...)
- `value = bar height`

---

## 6. Grouped bars (multiple series side-by-side): the actual rule

Key points:

- multiple `Serie`, all `type=Bar`
- all series share the same X categories (same labels)
- each series uses the same `x` index to land in the same category

Spacing fields:

- `BarSettings.barGap`: gap between bar groups within a category
- `BarSettings.categoryGap`: extra gap between categories (affects edge padding)

---

## 7. Stacked bars (stacked): the actual rule

Stacking happens between Bar series with the same stackGroup:

- `BarSettings.stacked = true`
- `BarSettings.stackGroup = "Group1"`

Runtime stacking notes:

- for the same `x` (category index), it accumulates positive and negative values separately (positive/negative stacks are separate)
- the top of each stacked segment = current accumulated base + `value`

---

## 8. Common pitfalls and troubleshooting

- **Bars appear between labels / not aligned**
  - Check X axis `labelPlacement` (recommend `CellCenter`)
  - Ensure `x` is an integer index (runtime rounds)

- **Bars do not start from 0**
  - Check whether Y axis (Value Axis) has `autoRangeMin` disabled and `minValue=0` locked

- **Stacking result is wrong**
  - Check that all series that should stack use the same `stackGroup`
  - Remember: positive and negative values stack separately

---

## 9. Further reading

- Axes/range, Series and data: `00_02-WorkflowAndLibrary.md`
- Common recipes: `04_08-CommonRecipes.md`
- FAQ: `04_09-FAQ.md`
