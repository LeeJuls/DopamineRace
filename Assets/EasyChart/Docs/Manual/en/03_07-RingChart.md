# Ring Chart (RingChart)

This chapter explains what RingChart really means (it is not a donut pie), and aligns its `SeriesData` interpretation, `RingChartSettings` configuration, and Pro/base differences with runtime behavior.

---

## 1. What is RingChart? (very important)

In the current EasyChart implementation:

- `SerieType.RingChart` renders **multiple progress rings** (one ring per data point)
- each ring is a **full 360° background ring + one progress arc**
- it is not a pie chart that splits the circle into multiple slices

If you want a donut pie chart that shows composition:

- it is closer to `SerieType.Pie` + `layout.innerRadius > 0`
- but you should choose based on intent:
  - composition/proportion: use Pie
  - multi-metric progress/completion: use RingChart

---

## 2. Important note (Pro feature)

- The renderer for `SerieType.RingChart` is registered by `EasyChartProBootstrap`.
- Without Pro installed/enabled, this serie usually won't render.

---

## 3. Minimum viable setup (checklist)

1. Add 1 `Serie`
   - `type = RingChart`
   - `settings = RingChartSettings`
   - `seriesData` has at least 1 point
2. Each point has `value > 0`

> Note: RingChart ignores points with `value <= 0`.

---

## 4. SeriesData field interpretation (runtime behavior)

RingChart mainly uses:

- `value`: the raw progress value
- `name`: ring name
- `useColor + color`: ring color (per-point override)
- `id`: stable identifier (for legend/hidden state; keep it stable)

### 4.1 Percent mode (default): value supports both 0~1 and 0~100

When `RingChartSettings.valueMapping.mode = Percent` (default):

- `value <= 0`: the ring is filtered out
- `0~1`: treated as ratio (0.72 = 72%)
- `> 1`: treated as percent (72 = 72%, runtime divides by 100)

Recommendation: standardize one convention within your team (all 0~1 or all 0~100) to avoid mistakes.

### 4.2 Range mode: map value to 0..1

When `RingChartSettings.valueMapping.mode = Range`:

- It first determines the range `min/max`:
  - `autoRange=true`: compute from values across all rings
  - `autoRange=false`: use `minValue/maxValue`
- Then it maps to `(value-min)/(max-min)` and clamps to 0..1

### 4.3 Name fallback when name is empty

When `SeriesData.name` is empty, RingChart tries to fall back to labels:

- If `ChartData.CoordinateSystem == None`: it won't use labels, and falls back to `Ring {i}`
- Otherwise it prefers: `Data.PolarAxes.angleAxis.labels[i]`
- Otherwise: labels from Cartesian/any Category axis `labels[i]`
- Final fallback: `Ring {i}`

If you do not want to depend on PolarAxes configuration, fill `SeriesData.name` directly.

---

## 5. Inspector fields (RingChartSettings)

- `series[i].type = RingChart`
- `series[i].settings`: actual type is `RingChartSettings`
  - `layout`: angles/radius/inner-outer ring/padding/center offset
  - `valueMapping`: Percent/Range mapping rules
  - `hover`: hover emphasis (Translate/Pull/Color/Stroke)
  - `legend`: RingChart legend settings (effective for pure Pie charts)
  - `showBackground/backgroundAlpha/backgroundColor`: background ring
  - `cornerRadius`: rounded cap
  - `ringGapPx`: gap between rings

### 5.1 layout (RingChartLayoutSettings)

Common fields:

- `startAngleDeg`: start angle
- `clockwise`: clockwise/counter-clockwise
- `angleRangeDeg`: default 360; use for half-ring progress, etc.
- `outerRadius`: outer radius (<=0 auto; 0~1 normalized; >1 pixels)
- `innerRadius`: inner radius (0~1 normalized or pixels)
- `plot.padding`: padding (avoid clipping hover/labels)
- `plot.centerOffset`: center offset

### 5.2 hover (PieHoverSettings)

- `hover.enabled`: enable
- `hover.explodeType`:
  - `Translate`: translate the whole ring
  - `Pull`: pull/stretch
  - `Color`: brighten
  - `Stroke`: stroke emphasis
- `hover.explodeDistance`: translate/pull distance (pixels)

### 5.3 Background ring and spacing

- `showBackground`: draw background ring
- `backgroundAlpha`: background ring alpha (multiplied into final color alpha)
- `backgroundColor`: background ring color (when alpha=0, it falls back to ring color)
- `ringGapPx`: gap between rings
- `cornerRadius`: rounded cap (limited by ring thickness)

---

## 6. Legend and hide interaction (shared HiddenPieSliceIds with Pie)

- RingChart shares `ChartInteractionState.HiddenPieSliceIds` with Pie.
- Hidden key for each ring: prefer `SeriesData.id`, otherwise use the index string.
- Legend label source is affected by `PieLegendSettings.source`:
  - `RingSlice` prefers `polarAxes.angleAxis.labels`.

---

## 7. Labels (SerieLabelSettings)

RingChart labels also use `Serie.labelSettings`:

- `show`: whether to show
- `showName`: whether to show name
- `decimalPlaces`: decimals (note: this displays the raw `value`, not a percent text multiplied by 100)
- `position`:
  - `Outside`: outside label + leader line
  - `Center`: centered on the ring

---

## 6. Common pitfalls (by symptoms)

- **I thought it was a donut pie, but it looks wrong**
  - This is a multi-ring progress chart: each point is one progress ring

- **Progress is wrong (e.g. I set 75 but it is almost full)**
  - `value>1` is treated as percent and divided by 100
  - For 75%: use `0.75` or `75`

- **Some rings are not visible**
  - Check whether `value <= 0` is being filtered

- **Interaction/hidden state is unstable**
  - Ensure `SeriesData.id` is stable

---

## 8. Further reading

- Pie (composition/proportion): `03_06-PieChart.md`
- Series data structure: `00_02-WorkflowAndLibrary.md`
