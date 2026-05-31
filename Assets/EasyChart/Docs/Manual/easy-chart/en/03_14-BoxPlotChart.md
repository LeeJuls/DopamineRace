# Box Plot Chart (BoxPlot) (Pro)

This chapter explains how Box Plot charts work in EasyChart: displaying statistical distribution data including quartiles, median, and outliers.

---

## 1. Use cases

- Statistical distribution visualization
- Comparing distributions across categories
- Identifying outliers and data spread

---

## 2. Important note (Pro feature)

- The renderer for `SerieType.BoxPlot` is registered by `EasyChartProBootstrap`.
- Without Pro installed/enabled, this serie will not render.

---

## 3. Minimum viable setup (checklist)

1. `ChartProfile.coordinateSystem = Cartesian2D`
2. Axes
   - X: usually `AxisType.Category` (groups)
   - Y: `AxisType.Value` (data range)
3. Series
   - Add 1 `Serie`
   - `Serie.type = BoxPlot`
   - `Serie.seriesData` has at least 1 point with statistical data

---

## 4. SeriesData field interpretation (runtime behavior)

Box Plot chart uses:

- `x`: category index
- `min`: minimum value (lower whisker)
- `q1`: first quartile (25th percentile)
- `median`: median value (50th percentile)
- `q3`: third quartile (75th percentile)
- `max`: maximum value (upper whisker)

---

## 5. Visual components

- **Box**: spans from Q1 to Q3 (interquartile range)
- **Median line**: horizontal line inside the box
- **Whiskers**: vertical lines extending to min/max
- **Outliers**: points beyond whiskers (optional)

---

## 6. Common settings (BoxPlotSettings)

- **Box width**: width of the box
- **Colors**: fill color, border color
- **Whisker style**: line width, cap style

---

## 7. Common pitfalls and troubleshooting

- **Box not visible**
  - Ensure EasyChartPro is installed and enabled
  - Verify all statistical fields are populated

- **Box appears inverted**
  - Check that Q1 < median < Q3

---

## 8. Further reading

- Axes/range, Series and data: `00_02-WorkflowAndLibrary.md`
- FAQ: `04_09-FAQ.md`
