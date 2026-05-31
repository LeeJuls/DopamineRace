# Pie Chart (Pie)

This chapter explains how Pie charts work in EasyChart: how data fields are interpreted, how layout/aggregation/legend/interactions take effect, and which behaviors have hidden prerequisites, mapped to Inspector fields.

---

## 1. Use cases

- Showing proportions/composition
- Emphasizing the share of each category in the whole

Not suitable for:

- Too many categories (usually > 8–12 becomes hard to read)
- Precise comparison of tiny differences (a bar chart is usually better)

---

## 2. Minimum viable setup (checklist)

1. `coordinateSystem`: Pie does not rely on Cartesian/Polar coordinate systems (keep your Profile setting)
2. Add 1 `Serie`:
   - `type = Pie`
   - `settings = PieSettings`
   - `seriesData` has at least 1 point
3. Ensure each point has `value > 0`

> Note: Pie currently ignores points with `value <= 0`.

---

## 3. Important limitations (runtime behavior)

- **Only the first visible Pie serie is drawn**: the renderer iterates `Data.Series`, finds the first visible serie with `type=Pie`, draws it, then `break`s.
- Slice hidden state comes from `ChartInteractionState.HiddenPieSliceIds`, which is added/removed when clicking legend items.

---

## 4. SeriesData field interpretation (runtime behavior)

Pie mainly uses:

- `value`: numeric value (weight) of the slice
- `name`: slice name (recommended)
- `useColor + color`: per-slice custom color (optional)
- `id`: stable slice identifier (for hidden/interaction state; keep it stable)

### 4.1 Recommended pattern: explicit name + value

- `SeriesData.name = "Apple"`
- `SeriesData.value = 12`

### 4.2 Name fallback when name is empty

When `SeriesData.name` is empty, Pie may try to use **labels**, but with an important prerequisite:

- If `ChartData.CoordinateSystem == None` (typically pure Pie / no coordinate system), runtime **skips label fallback** and uses only `SeriesData.name`.

When coordinate system is not None, the fallback order is:

- Prefer Category axis labels that match `Data.Cartesian.xAxisId`
- Otherwise, use labels from any Category axis
- Final fallback: `Slice {index}`

> Therefore: if you do not want to depend on axis configuration, fill `SeriesData.name` directly.

### 4.3 Color source

- If `useColor=true` on the point: use `SeriesData.color`
- Otherwise: use the built-in palette in order

---

## 5. Common settings (PieSettings)

Pie `settings` is `PieSettings`, mainly including:

- `layout`: layout (angle/radius/gaps/center offset, etc.)
- `hover`: hover interaction (explode)
- `aggregation`: aggregation (TopN + Others)
- `legend`: Pie-specific legend settings (replaces global legend only for "pure Pie chart" cases)

### 4.1 layout (PieLayoutSettings)

Common fields:

- `startAngleDeg`: start angle (default -90 makes the first slice start at the top)
- `clockwise`: clockwise/counter-clockwise
- `angleRangeDeg`: angle range (default 360; use 180 for half-pie, etc.)
- `outerRadius`: outer radius
  - `<= 0`: auto
  - `0~1`: normalized by control size
  - `> 1`: pixels
- `innerRadius`: inner radius (Pie usually 0; >0 creates a hole, but RingChart is recommended for ring/progress style)
- `innerRadiusColor`: inner fill color
- `sliceGapPx`: gap between slices (pixels)
- `sliceGapType`: gap mode (Radial/Translate/Uniform)
- `cornerRadius`: corner radius (pixels, limited by slice thickness)
- `plot.padding`: padding (avoid clipping slices/outside labels)
- `plot.centerOffset`: center offset

### 4.2 hover (PieHoverSettings)

- `hover.enabled`: enable hover interaction
- `hover.explodeType`:
  - `Translate`: translate the whole slice
  - `Pull`: pull out / stretch
  - `Color`: brighten
  - `Stroke`: stroke emphasis
- `hover.explodeDistance`: translate/pull distance (pixels)

### 4.3 aggregation (PieAggregationSettings)

When there are many categories, you can merge small items into `Others`:

- `aggregation.enabled = true`
- `keepTopN`: keep top N, merge the rest
- `sortByValue`: sort by `value` before taking TopN
- `othersName`: name for Others
- `useOthersColor + othersColor`: Others color

> Note: aggregation only takes effect when `keepTopN > 0` and slice count exceeds N.

---

## 6. Legend (PieLegendSettings) and "hide slice" interaction

When the chart is a "pure Pie chart" (only Pie/RingChart/Pie3D and no other types):

- Legend prefers `PieSettings.legend` (or the legend on RingChartSettings/Pie3DSettings), instead of `ChartData.legend`.
- Clicking a legend item toggles `HiddenPieSliceIds`:
  - normal slices: `SeriesData.id` (if empty, uses index string)
  - aggregated Others: always `__ec_pie_others__`

`PieLegendSettings.source` affects where legend items come from:

- `Slice`: one entry per slice (default)
- `RingSlice`: provides label source for RingChart/RingSlice scenarios (prefers PolarAxes.angleAxis.labels)
- `Series`: one entry per serie (not slice-level)

---

## 7. Labels (SerieLabelSettings)

Pie labels are controlled by `Serie.labelSettings`:

- `show`: whether to show
- `fontSize / color / decimalPlaces`: font and value format
- `showName`: whether to include slice name
- `position`: `Outside/Inside/Center`
- `offset`: offset

---

## 8. Common pitfalls (by symptoms)

- **Some slices are not visible**
  - Check whether the point `value` is `<= 0`

- **Slice name is not what I expect**
  - Recommended: fill `SeriesData.name` directly
  - If you rely on labels: ensure you have a Category axis with `labels`, and the order matches data point indices

- **Slice colors change each time / hard to control**
  - For slices that need fixed colors: set `useColor=true` + `color` on the point

- **Hidden/interaction state is unstable**
  - Ensure each point `SeriesData.id` is stable (do not regenerate ids on each refresh)

---

## 9. Next

- Ring chart (RingChart): `03_07-RingChart.md`
