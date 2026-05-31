# Waterfall Chart (Waterfall) (Pro)

This chapter explains how Waterfall charts work in EasyChart: displaying cumulative effects of sequential positive and negative values.

---

## 1. Use cases

- Financial statements (revenue breakdown)
- Budget analysis (income vs expenses)
- Process flow with gains and losses

---

## 2. Important note (Pro feature)

- The renderer for `SerieType.Waterfall` is registered by `EasyChartProBootstrap`.
- Without Pro installed/enabled, this serie will not render.

---

## 3. Minimum viable setup (checklist)

1. `ChartProfile.coordinateSystem = Cartesian2D`
2. Axes
   - X: usually `AxisType.Category` (steps/stages)
   - Y: `AxisType.Value`
3. Series
   - Add 1 `Serie`
   - `Serie.type = Waterfall`
   - `Serie.seriesData` has at least 2 points

---

## 4. SeriesData field interpretation (runtime behavior)

Waterfall chart uses:

- `x`: category index (step in the waterfall)
- `value`: the change amount (positive or negative)
- `name`: label for the step

---

## 5. Visual interpretation

- **Starting bar**: initial value
- **Positive bars**: increases (typically green)
- **Negative bars**: decreases (typically red)
- **Connecting lines**: show flow between bars
- **Total bar**: final cumulative value

---

## 6. Common settings (WaterfallSettings)

- **Colors**: positive color, negative color, total color
- **Connector**: line style between bars
- **Labels**: value display format

---

## 7. Common pitfalls and troubleshooting

- **Bars not connecting properly**
  - Ensure data is in sequential order
  - Check that values represent changes, not absolutes

- **Total bar incorrect**
  - Verify the sum of all changes equals expected total

---

## 8. Further reading

- Bar chart: `03_02-BarChart.md`
- Axes/range, Series and data: `00_02-WorkflowAndLibrary.md`
- FAQ: `04_09-FAQ.md`
