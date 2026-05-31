# Horizontal Bar Chart (HorizontalBar)

This chapter explains how Horizontal Bar charts work in EasyChart: similar to regular bar charts but with horizontal layout, better suited for long category labels.

---

## 1. Use cases

- Category comparisons with long labels
- Ranking displays
- Survey results visualization

---

## 2. Minimum viable setup (checklist)

1. `ChartProfile.coordinateSystem = Cartesian2D`
2. Axes
   - Y: usually `AxisType.Category` (fill `labels`)
   - X: usually `AxisType.Value`
3. Series
   - Add 1 `Serie`
   - `Serie.type = HorizontalBar`
   - `Serie.seriesData` has at least 1 point

---

## 3. Inspector fields

- **Axis Settings**
  - `cartesian.xAxisId / cartesian.yAxisId`
  - `axes[]` (AxisConfig for X/Y)

- **Series**
  - `series[i].type = HorizontalBar`
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

Horizontal Bar charts use:

- **Category / vertical position**: `SeriesData.x` (mapped to Y axis categories)
- **Bar length**: `SeriesData.value`

---

## 5. Difference from regular Bar chart

- Layout is horizontal instead of vertical
- Categories are on Y axis, values on X axis
- Better for displaying long category names

---

## 6. Common pitfalls and troubleshooting

- **Bars appear vertical instead of horizontal**
  - Ensure `type = HorizontalBar`, not `Bar`
  - Check axis configuration (Y should be Category)

- **Labels are cut off**
  - Increase left padding in chart settings

---

## 7. Further reading

- Bar chart: `03_02-BarChart.md`
- Axes/range, Series and data: `00_02-WorkflowAndLibrary.md`
- Common recipes: `04_08-CommonRecipes.md`
