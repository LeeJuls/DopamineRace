# Radar Chart (Radar)

This chapter explains how Radar charts work in EasyChart: where dimension labels come from, how value ranges are calculated, and how data point order is interpreted, mapped to Inspector fields.

---

## 1. Use cases

- Multi-dimensional metric comparison
- Ability/attribute radar

---

## 2. Minimum viable setup (checklist)

1. `ChartProfile.coordinateSystem = Polar2D`
2. Series
   - Add 1 `Serie`
   - `Serie.type = Radar`
   - `Serie.seriesData` is recommended to have at least 3 points (with <= 2 dimensions, runtime will not draw / hover won't work)
3. PolarAxes (recommended)
   - `polarAxes.angleAxis.labels`: dimension names
   - `polarAxes.radiusAxis`: value range (auto/manual)

---

## 3. Inspector fields

- **ChartProfile / Coordinate System**
  - `coordinateSystem = Polar2D`

- **PolarAxes** (recommended for configuring Radar axes)
  - `polarAxes.angleAxis.labels`: dimension labels
  - `polarAxes.radiusAxis.autoRangeMin/autoRangeMax/minValue/maxValue/autoRangeRounding/labelFormat/...`

- **Series**
  - `series[i].type = Radar`
  - `series[i].settings`: actual type is `RadarSettings`
    - `radar`: layout (startAngleDeg / clockwise / innerRadius / outerRadius / plot / background)
    - `stroke`: polyline style
    - `area`: area fill
    - `point`: point style (point visibility also affects hover pick radius)
  - `series[i].labelSettings`: data point labels (can show dimension name and value)

---

## 4. SeriesData field interpretation (runtime behavior)

Key point for Radar: **dimension order is defined by the index of items in the `seriesData` list**.

- **Value**: uses `SeriesData.value`
- **Dimension index**: uses the position `i` in `seriesData` (0..dimensionCount-1)
- `SeriesData.x` is **not used for positioning** in Radar rendering (do not rely on x to represent dimensions)

Where does dimensionCount come from:

1. Prefer `Data.PolarAxes.angleAxis.labels.Count`
2. If angleAxis.labels is not configured, it uses labels from a Category Axis (see section 5)
3. Otherwise, fall back to `seriesData.Count` (or the maximum count among multiple series)

---

## 5. Actual priority order for dimension labels

Runtime resolves dimension names in this priority order:

1. `polarAxes.angleAxis.labels[i]`
2. `labels[i]` from a `AxisType.Category` axis in `axes[]`
   - it prefers the Category axis that matches `Data.XAxisId`
3. `seriesData[i].name`
4. If none exists, it shows `Dim i`

> Recommendation: for Radar, manage dimension names via `polarAxes.angleAxis.labels`. Use `SeriesData.name` as a fallback.

---

## 6. How radius value range is calculated

Radar radius range is calculated from `SeriesData.value`:

- By default, it computes auto range from values across all Radar series
- If you configure `polarAxes.radiusAxis`:
  - `autoRangeMin/autoRangeMax` decides whether min/max are automatic
  - `minValue/maxValue` take effect when the corresponding auto range is disabled
  - `autoRangeRounding` rounds auto min/max to tens/hundreds/custom unit
  - `labelFormat` affects tooltip/label formatting

---

## 7. Common pitfalls and troubleshooting

- **Radar chart not visible**
  - Check `coordinateSystem` is `Polar2D`
  - Dimension count must be > 2 (labels or seriesData must be at least 3)

- **Dimensions do not match / order is wrong**
  - Radar does not use `x`. It uses `seriesData` list order as dimension order
  - Put points in `seriesData` in the intended dimension order

- **Hover is hard to trigger**
  - Radar pick radius is related to `RadarSettings.point.size`
  - If `point.show=false`, pick radius becomes 0 (almost impossible to hover)

---

## 8. Further reading

- Series and data: `00_02-WorkflowAndLibrary.md`
- Common recipes: `04_08-CommonRecipes.md`
- FAQ: `04_09-FAQ.md`
