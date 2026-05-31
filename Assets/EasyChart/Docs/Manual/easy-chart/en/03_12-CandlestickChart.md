# Candlestick Chart (Candlestick) (Pro)

This chapter explains how Candlestick (K-line) charts work in EasyChart: displaying financial OHLC data (Open, High, Low, Close) in a traditional candlestick format.

---

## 1. Use cases

- Stock price visualization
- Financial market analysis
- Trading pattern recognition

---

## 2. Important note (Pro feature)

- The renderer for `SerieType.Candlestick` is registered by `EasyChartProBootstrap`.
- Without Pro installed/enabled, this serie will not render.

---

## 3. Minimum viable setup (checklist)

1. `ChartProfile.coordinateSystem = Cartesian2D`
2. Axes
   - X: usually `AxisType.Category` (dates/times) or `AxisType.Value`
   - Y: `AxisType.Value` (price range)
3. Series
   - Add 1 `Serie`
   - `Serie.type = Candlestick`
   - `Serie.seriesData` has at least 1 point with OHLC data

---

## 4. SeriesData field interpretation (runtime behavior)

Candlestick chart uses:

- `x`: time/category index
- `open`: opening price
- `high`: highest price
- `low`: lowest price
- `close`: closing price (or `value`)

---

## 5. Common settings (CandlestickSettings)

- **Colors**: bullish (up) color, bearish (down) color
- **Width**: candle body width
- **Wick**: wick/shadow line style

---

## 6. Visual interpretation

- **Bullish candle** (close > open): typically green/white, body shows gain
- **Bearish candle** (close < open): typically red/black, body shows loss
- **Wicks**: vertical lines showing high/low range

---

## 7. Common pitfalls and troubleshooting

- **Candles not visible**
  - Ensure EasyChartPro is installed and enabled
  - Verify OHLC data fields are populated

- **Colors are inverted**
  - Check bullish/bearish color settings

- **Y axis range incorrect**
  - Enable auto-range or manually set min/max to cover all prices

---

## 8. Further reading

- OHLC chart: `03_13-OHLCChart.md`
- Axes/range, Series and data: `00_02-WorkflowAndLibrary.md`
- FAQ: `04_09-FAQ.md`
