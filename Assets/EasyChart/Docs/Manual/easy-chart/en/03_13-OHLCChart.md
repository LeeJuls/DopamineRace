# OHLC Chart (OHLC) (Pro)

This chapter explains how OHLC charts work in EasyChart: displaying financial Open-High-Low-Close data using line segments instead of candle bodies.

---

## 1. Use cases

- Stock price visualization (cleaner than candlestick)
- Financial market analysis
- High-density data display

---

## 2. Important note (Pro feature)

- The renderer for `SerieType.OHLC` is registered by `EasyChartProBootstrap`.
- Without Pro installed/enabled, this serie will not render.

---

## 3. Minimum viable setup (checklist)

1. `ChartProfile.coordinateSystem = Cartesian2D`
2. Axes
   - X: usually `AxisType.Category` (dates/times) or `AxisType.Value`
   - Y: `AxisType.Value` (price range)
3. Series
   - Add 1 `Serie`
   - `Serie.type = OHLC`
   - `Serie.seriesData` has at least 1 point with OHLC data

---

## 4. SeriesData field interpretation (runtime behavior)

OHLC chart uses:

- `x`: time/category index
- `open`: opening price
- `high`: highest price
- `low`: lowest price
- `close`: closing price (or `value`)

---

## 5. Visual interpretation

- **Vertical line**: shows high-low range
- **Left tick**: opening price
- **Right tick**: closing price
- More compact than candlestick, suitable for dense data

---

## 6. Difference from Candlestick

| Feature | Candlestick | OHLC |
|---------|-------------|------|
| Body | Filled rectangle | No body |
| Open/Close | Body edges | Horizontal ticks |
| Density | Lower | Higher |
| Visual impact | Stronger | Cleaner |

---

## 7. Common pitfalls and troubleshooting

- **Chart not visible**
  - Ensure EasyChartPro is installed and enabled
  - Verify OHLC data fields are populated

- **Ticks too small**
  - Adjust tick width in settings

---

## 8. Further reading

- Candlestick chart: `03_12-CandlestickChart.md`
- Axes/range, Series and data: `00_02-WorkflowAndLibrary.md`
- FAQ: `04_09-FAQ.md`
