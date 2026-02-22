# Scatter Chart (Scatter)

This chapter explains the data field conventions for Scatter charts in EasyChart, especially the compatibility behavior of `SeriesData.y/value`, and how the `z` dimension drives `sizeMapping`.

---

## 1. Use cases

- Correlation analysis (two numeric dimensions X/Y)
- Distribution visualization (point cloud)
- Outlier detection

---

## 2. Minimum viable setup (checklist)

1. `ChartProfile.coordinateSystem = Cartesian2D`
2. Axes
   - Common: X=Value, Y=Value
3. Series
   - Add 1 `Serie`
   - `Serie.type = Scatter`
   - `Serie.seriesData` has at least 2 points

---

## 3. Inspector fields

- `series[i].type = Scatter`
- `series[i].settings`: actual type is `ScatterSettings`
  - `point`: point style (visible/size/texture)
  - `hover`: hover highlight (enabled/pickRadius/scale, etc. depending on version)
  - `sizeMapping`: point size mapping

---

## 4. SeriesData field interpretation (runtime behavior)

Scatter chart uses:

- **X coordinate**: `SeriesData.x`
- **Y coordinate**: prefers `SeriesData.y`
  - Compatibility: if `y == 0` and `value != 0`, runtime uses `value` as y
- **Size mapping dimension**: `SeriesData.z` (when `sizeMapping.enabled=true`)

So there are two common patterns:

### 4.1 Recommended (explicit X/Y)

- `x = X value`
- `y = Y value`

### 4.2 Compatibility (legacy data: use value as y)

- `x = X value`
- `value = Y value`
- `y = 0`

> Recommendation: for new data, write `y` directly to avoid mixing meanings with `value`.

---

## 5. Standard template: Value X + Value Y

- X axis: `AxisType = Value`
- Y axis: `AxisType = Value`
- Data: use pattern 4.1 (x/y)

---

## 6. sizeMapping: actual behavior

When `ScatterSettings.sizeMapping.enabled = true`:

- point radius is mapped from `SeriesData.z`
- mapping range: `minValue/maxValue` -> `minSize/maxSize`
- if `clamp = true`, t is clamped to 0..1
- `curve` applies a curve transform to t (non-linear mapping)

If sizeMapping "doesn't work", check first:

- did you actually set `z` values (default 0)
- is `minValue/maxValue` equal (degenerates mapping)

---

## 7. Common pitfalls and troubleshooting

- **All points are on a horizontal line**
  - you may have filled only `value`, but also set `y` to a non-zero value (compatibility won't trigger)
  - recommend using `y` consistently as the Y coordinate

- **Hover does not respond**
  - `ScatterSettings.hover.enabled` must be enabled
  - too small `pickRadius` makes picking difficult

- **Points are too small / too large**
  - adjust `ScatterSettings.point.size`
  - or check `minSize/maxSize` in sizeMapping

---

## 8. Further reading

- Axes/range, Series and data: `00_02-WorkflowAndLibrary.md`
- Common recipes: `04_08-CommonRecipes.md`
- FAQ: `04_09-FAQ.md`
