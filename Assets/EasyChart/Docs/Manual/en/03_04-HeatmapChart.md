# Heatmap Chart (Heatmap)

This chapter explains the rules for Heatmaps in EasyChart: how coordinates/cells are mapped, how `SeriesData` fields are interpreted, and how the value-to-color range is calculated. It also notes that Heatmap is a Pro feature.

---

## 1. Use cases

- 2D matrix visualization (rows/columns)
- Density/intensity visualization

---

## 2. Important note (Pro feature)

- The renderer for `SerieType.Heatmap` is registered by `EasyChartProBootstrap`.
- If EasyChartPro is not installed/enabled, this serie may be treated as a "dynamic renderer" and attempted to be created, but it usually won't render.

---

## 3. Minimum viable setup (checklist)

1. `ChartProfile.coordinateSystem = Cartesian2D`
2. Axes (Axis Settings)
   - Most common: X=Category (columns), Y=Category (rows)
   - X/Y can also use Value axes (see section 7)
3. Series
   - Add 1 `Serie`
   - `Serie.type = Heatmap`
   - `Serie.seriesData` has at least 1 point

---

## 4. Inspector fields

- **Series**
  - `series[i].type = Heatmap`
  - `series[i].settings`: actual type is `HeatmapSettings`
    - `renderMode`: Grid / Gradient / Contour
    - `cellGapPx`
    - `xSplitCount` / `ySplitCount` (used when X/Y are Value axes)
    - `autoRange / minValue / maxValue`
    - `lowColor / midColor / highColor`
    - `clamp`
    - `influenceMode`: None / Bleed / Smooth
    - sub settings: `bleed / smooth / gradient / contour`

---

## 5. SeriesData field interpretation (runtime behavior)

Each Heatmap data point corresponds to one "cell/pixel area". Runtime uses:

- **X coordinate (column)**: `SeriesData.x`
- **Y coordinate (row)**: `SeriesData.y`
- **Intensity**: `SeriesData.value`
- **Color override**: if `SeriesData.useColor = true`, runtime uses `SeriesData.color` directly and skips interpolation from `low/mid/high`.

> Note: Heatmap `x/y` do not accept string categories. With Category axes, you still use indices.

---

## 6. Standard template: 2D Category (X/Y) + value intensity (most common)

### 6.1 X axis (Category: columns)

- `AxisType = Category`
- `labels = ["Col0","Col1",...]`

### 6.2 Y axis (Category: rows)

- `AxisType = Category`
- `labels = ["Row0","Row1",...]`

### 6.3 Data pattern

- `x = column index` (runtime applies `RoundToInt`)
- `y = row index` (runtime applies `RoundToInt`)
- `value = intensity`

### 6.4 Important detail: cell count vs `labelPlacement` for Category axes

Runtime uses the axis `labelPlacement` to decide whether to split into `labels.Count` cells or `labels.Count-1` cells:

- `CategoryLabelPlacement.CellCenter`
  - X cell count = `labels.Count`
  - Y cell count = `labels.Count`

- Others (non CellCenter)
  - X cell count = `max(1, labels.Count - 1)`
  - Y cell count = `max(1, labels.Count - 1)`

This directly affects the valid range of indices you should write into `x/y`.

---

## 7. Heatmap with Value axes (X/Y are numeric axes)

When X or Y uses `AxisType.Value`:

- Cell count no longer comes from labels. It comes from:
  - X: `HeatmapSettings.xSplitCount`
  - Y: `HeatmapSettings.ySplitCount`

- `SeriesData.x/y` are normalized using `_xMin/_xMax` and `_yMin/_yMax`, then mapped into cell indices.

This is suitable for intensity/density distribution over a continuous value range.

---

## 8. Common pitfalls and troubleshooting

- **All cells look the same / low contrast**
  - Check whether `HeatmapSettings.autoRange` is enabled
  - Or manually set `minValue/maxValue`
  - Also check whether all points have almost the same `value`

- **Colors do not follow low/mid/high**
  - Check whether some points set `useColor=true` (it overrides palette interpolation)

- **Cells are misaligned (out-of-range / off-by-one)**
  - Check whether Category axis `labelPlacement` is `CellCenter`
  - Use section 6.4 to determine correct cell count and index ranges

- **Cell gaps are too large/too tight**
  - Adjust `HeatmapSettings.cellGapPx`

---

## 9. Further reading

- Axes/range, Series and data: `00_02-WorkflowAndLibrary.md`
- Common recipes: `04_08-CommonRecipes.md`
- FAQ: `04_09-FAQ.md`
