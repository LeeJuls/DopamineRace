
# Common Recipes

This chapter provides copy-ready recipes for common charts (Series + Axes + common pitfalls) to help you create and troubleshoot quickly.

---

## 0. Before you start: minimum checklist

When you see "nothing shows" or "it looks weird", check in this order:

1. Does `coordinateSystem` match the SeriesType (Cartesian2D vs Polar2D)
2. Does `series` contain at least 1 serie, and does that serie have `seriesData.Count > 0`
3. Do axis types match what your data means:
   - Category axis: `labels` is not empty, and data `x/y` are indices (0/1/2...)
   - Value axis: data `x/y` are continuous numeric values
4. Do you have any NaN/Infinity
5. Did you lock Value axis range (`autoRangeMin/autoRangeMax` or fixed min/max) so data is outside the range

---

## 1. Line chart (Line): Category X + numeric Y

### Target

- X: category labels (A/B/C/D)
- Y: numeric values
- line points aligned to categories

### Recipe

1. `coordinateSystem = Cartesian2D`
2. X axis:
   - `axisType = Category`
   - `labels = [A, B, C, D]`
   - `LabelPlacement = Tick`
3. Y axis:
   - `axisType = Value`
   - `autoRangeMin/autoRangeMax = true`
4. Series:
   - `type = Line`
   - points: `x=category index`, `y=value`

Data example (conceptual):

```txt
(x=0, y=10)
(x=1, y=20)
(x=2, y=15)
(x=3, y=30)
```

### Common pitfalls

- **Points do not align with labels**: check `x` starts from 0 and is within range (labels.Count)
- **Line looks broken/jumpy**: check NaN/Infinity

---

## 2. Bar chart (Bar): centered categories + Y starts from 0

### Target

- one bar per category
- labels centered under bars
- Y axis starts from 0 to avoid misleading scaling

### Recipe

1. `coordinateSystem = Cartesian2D`
2. X axis:
   - `axisType = Category`
   - fill `labels` with categories
   - `LabelPlacement = CellCenter`
3. Y axis:
   - `axisType = Value`
   - force start at 0 (e.g. `minValue=0` + `autoRangeMax=true`, or equivalent)
4. Series:
   - `type = Bar`
   - adjust bar width via `BarSettings.barWidth`

Data example:

```txt
(x=0, y=12)
(x=1, y=18)
(x=2, y=9)
```

### Common pitfalls

- **Bars appear between labels**: switch `LabelPlacement` to `CellCenter`
- **Bars too dense/too sparse**: adjust `barWidth`, `barGap`, `categoryGap`

---

## 3. Grouped bars (Grouped Bar): multiple series share the same categories

### Recipe

- multiple `Serie`, all `type = Bar`
- each Serie uses the same `x=category index` convention
- use `Serie.name` as group name (used by legend/tooltip)

Example (conceptual):

```txt
Serie A:
  (x=0, y=10) (x=1, y=12)
Serie B:
  (x=0, y=8)  (x=1, y=15)
```

---

## 4. Stacked bars (Stacked Bar): stacked + stackGroup

### Recipe

- Bar series that should stack:
  - `BarSettings.stacked = true`
  - `BarSettings.stackGroup = "Group1"` (same group stacks)

### Common pitfalls

- **Stack height looks wrong**: ensure all stacked series use exactly the same `stackGroup`

---

## 5. Scatter chart (Scatter): Value X/Y + hover + sizeMapping

### Target

- X/Y are continuous numeric values
- point grows on hover
- point size can be mapped by a dimension (sizeMapping)

### Recipe

1. `coordinateSystem = Cartesian2D`
2. Set both X/Y axes to `Value`
3. `type = Scatter`
4. Data points: at least `x/value`, optionally use `z` as third dimension
5. `ScatterSettings.hover.enabled = true`

### Common pitfalls

- **Points are too small**: increase `PointSettings.size`
- **Hover does not respond**: check `HoverHighlightSettings.enabled` and `pickRadius`

---

## 6. Heatmap chart (Heatmap): (x, y, value) triplets

### Target

- X/Y are Category axes (2D labels)
- color is determined by value

### Recipe

1. `coordinateSystem = Cartesian2D`
2. X axis: Category + labels (column labels)
3. Y axis: Category + labels (row labels)
4. `type = Heatmap`
5. Data points:
   - `x = column index`
   - `y = row index`
   - `value = intensity`

Example (conceptual):

```txt
(x=0, y=0, value=0.2)
(x=1, y=0, value=0.8)
(x=0, y=1, value=0.5)
```

### Common pitfalls

- **All cells look the same**: check `HeatmapSettings.autoRange/minValue/maxValue/clamp`
- **Cells too small/too dense**: adjust `cellSizePx` / `cellGapPx`

---

## 7. Radar chart (Radar): dimension index + value

### Recipe

1. `coordinateSystem = Polar2D`
2. `type = Radar`
3. Data points:
   - `x = dimension index`
   - `value = numeric value`
   - `name = dimension name` (recommended for labels/tooltip)

Example:

```txt
(x=0, value=72, name="Attack")
(x=1, value=55, name="Defense")
(x=2, value=90, name="Speed")
```

### Common pitfalls

- **Radar labels are missing/messy**: ensure your dimension label source is consistent (do not depend on Cartesian axes)
- **Radar not visible**: check `coordinateSystem` is Polar2D

---

## 8. Interaction/tooltip stability: SeriesData.id

If you enabled selection/tooltip/hover, it is generally recommended:

- keep each point `SeriesData.id` stable

> Otherwise, if you generate new ids every refresh, interaction state cannot be associated correctly.

---

## Next

- Next: `04_09-FAQ.md` (common issues + the fastest troubleshooting path)
