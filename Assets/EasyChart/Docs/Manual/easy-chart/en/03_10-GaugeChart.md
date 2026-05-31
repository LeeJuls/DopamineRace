# Gauge Chart (Gauge) (Pro)

This chapter explains how Gauge charts work in EasyChart: displaying single metrics with visual indicators similar to speedometers or progress meters.

---

## 1. Use cases

- Single metric visualization (KPIs, progress)
- Dashboard indicators
- Real-time monitoring displays

---

## 2. Important note (Pro feature)

- The renderer for `SerieType.Gauge` is registered by `EasyChartProBootstrap`.
- Without Pro installed/enabled, this serie will not render.

---

## 3. Minimum viable setup (checklist)

1. Add 1 `Serie`:
   - `type = Gauge`
   - `settings = GaugeSettings`
   - `seriesData` has at least 1 point
2. Each point has a `value` representing the current metric

---

## 4. SeriesData field interpretation (runtime behavior)

Gauge chart uses:

- `value`: the current metric value to display
- `name`: optional label for the gauge

---

## 5. Common settings (GaugeSettings)

- **Range**: `minValue` / `maxValue` - defines the gauge scale
- **Appearance**: arc angle, colors, pointer style
- **Labels**: value display format, tick marks

---

## 6. Common pitfalls and troubleshooting

- **Gauge not visible**
  - Ensure EasyChartPro is installed and enabled
  - Check that `value` is within the defined range

- **Pointer position incorrect**
  - Verify `minValue` and `maxValue` are set correctly

---

## 7. Further reading

- Series and data: `00_02-WorkflowAndLibrary.md`
- Common recipes: `04_08-CommonRecipes.md`
- FAQ: `04_09-FAQ.md`
