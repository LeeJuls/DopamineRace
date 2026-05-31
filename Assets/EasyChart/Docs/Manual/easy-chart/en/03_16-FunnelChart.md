# Funnel Chart (Funnel) (Pro)

This chapter explains how Funnel charts work in EasyChart: displaying progressive reduction through stages, commonly used for conversion analysis.

---

## 1. Use cases

- Sales funnel (leads → customers)
- Conversion analysis (visits → signups → purchases)
- Process drop-off visualization

---

## 2. Important note (Pro feature)

- The renderer for `SerieType.Funnel` is registered by `EasyChartProBootstrap`.
- Without Pro installed/enabled, this serie will not render.

---

## 3. Minimum viable setup (checklist)

1. Add 1 `Serie`:
   - `type = Funnel`
   - `settings = FunnelSettings`
   - `seriesData` has at least 2 points
2. Each point has `value > 0`

---

## 4. SeriesData field interpretation (runtime behavior)

Funnel chart uses:

- `value`: the quantity at this stage
- `name`: stage name/label

---

## 5. Visual interpretation

- **Trapezoid segments**: each stage represented by a trapezoid
- **Width**: proportional to value (wider = larger value)
- **Top-to-bottom flow**: shows progressive reduction
- **Labels**: stage names and values/percentages

---

## 6. Common settings (FunnelSettings)

- **Orientation**: vertical or horizontal
- **Alignment**: left, center, or right
- **Gap**: space between segments
- **Colors**: per-segment or gradient

---

## 7. Common pitfalls and troubleshooting

- **Funnel not visible**
  - Ensure EasyChartPro is installed and enabled
  - Check that values are positive

- **Segments appear equal size**
  - Verify values are different
  - Check if min/max width settings are too restrictive

- **Order is wrong**
  - Data should be ordered from largest to smallest (or vice versa)

---

## 8. Further reading

- Pie chart: `03_06-PieChart.md`
- Axes/range, Series and data: `00_02-WorkflowAndLibrary.md`
- FAQ: `04_09-FAQ.md`
