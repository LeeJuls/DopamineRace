# Inspector Panel

This chapter explains the **Inspector** panel at the bottom of the middle area in `Unity Easy Chart/Library Editor`.

Inspector is designed to edit the serialized fields of the currently selected `ChartProfile` from a "configuration" perspective (coordinate system, axes, grid, interactions, legend, etc.), and drive the Preview to update.

---

## Panel structure overview

After you select a `ChartProfile` in the left Library tree, Inspector builds a set of foldouts (Foldout), typically including:

- **Chart Settings**: basic chart settings (e.g. background, name)
- **Coordinate System**: coordinate system selection and related options
- **Axis Settings**: axis configuration (X/Y or Angle/Radius)
- **Grid Settings**: grid-related configuration
- **Hover Settings**: hover/tooltip related configuration
- **Legend Settings**: legend configuration

Tip:

- If you select a folder instead of a `ChartProfile`, Inspector will be empty (this is expected).

---

## Chart Settings (common)

### Chart Name

Inspector contains a `Chart Name` text field (from `ChartProfile.chartName`). It is not only a display name, it also participates in asset renaming:

- When you type a new name and then **lose focus** or press **Enter**:
  - the editor attempts to rename the `.asset` file to that name
  - and tries to keep `profile.name` and `profile.chartName` in sync

Notes:

- The name will be sanitized for filenames (invalid characters are removed/replaced).
- If renaming fails (e.g. name conflict), an Error dialog is shown and the field is reverted.

### Background

`Background` is usually a sub-foldout that contains background color/alpha fields (depending on version).

---

## Coordinate System

This area shows the `coordinateSystem` selector.

The coordinate system affects:

- available Series types/semantics (e.g. Polar2D is commonly Radar; Pie is a special layout)
- whether Axis Settings shows Cartesian (X/Y) or Polar (Angle/Radius) configuration

Recommendation:

- decide the coordinate system before you start configuring, to avoid large adjustments later.

---

## Axis Settings

### Axis selection (X Axis / Y Axis)

In Cartesian mode, the top provides X/Y axis dropdowns:

- **X Axis**: e.g. `XBottom` / `XTop`
- **Y Axis**: e.g. `YLeft` / `YRight`

When the selection changes, it will:

- ensure the axes list contains an element for that AxisId (auto-create if needed)
- refresh the Axis field UI below
- trigger a Preview refresh

### Common fields of a single Axis

Each Axis configuration typically contains:

- `axisType`: Category / Value, etc.
- `visible`: whether to show
- `color` / `width`: axis line style

#### LabelTexts (Category axis labels)

Inspector provides `LabelTexts` (internal field name `labels`) to configure category labels.

#### Range

Common fields include:

- `minValue` / `maxValue`
- `autoRangeMin` / `autoRangeMax`
- `autoRangeRounding`
- `autoRangeUnit`

#### Ticks / VisibleCount

If the axis supports auto ticks:

- When `autoTicks` is enabled, it shows `splitCount`.
- For **Category Axis**, this field is displayed as **VisibleCount** (number of visible categories).

#### Category Auto Scroll

If the axis supports category scrolling, common fields include:

- `categoryAutoScroll`: whether to auto scroll (marquee effect).
- `categorySmoothScroll`: whether to scroll smoothly.
- `categoryScrollInterval`: scroll interval.
- `categoryScrollStep`: scroll step per tick.

#### Unit (unit display)

Common fields:

- `showUnit`: whether to show unit.
- `unitText`: unit text (e.g. `ms`/`%`/`MB`).
- `unitLabelStyle`: unit label style.

---

## Polar Axis

When `coordinateSystem = Polar2D`, Axis Settings shows `polarAxes`:

- **Angle Axis** (angleAxis)
- **Radius Axis** (radiusAxis)

Common field meanings are similar to Cartesian:

- `labels`: angle/dimension labels (Radar dimension names typically come from here).
- `visible/color/width`: axis line style.
- `showLabels/fontSize/labelColor/labelPosition/labelOffset`: label display controls.
- `autoRangeMin/autoRangeMax/minValue/maxValue`: radius axis range.
- `autoTicks/splitCount`: tick count.

---

## Grid Settings (fields, Cartesian2D only)

Grid Settings is visible in Cartesian2D. Key fields come from `cartesianGrid`:

- **xGridColor / xGridLineWidth**: X-direction grid line color and width.
- **yGridColor / yGridLineWidth**: Y-direction grid line color and width.

If you need dashed lines:

- `xGridDashed` / `yGridDashed`: enable dashed.
- `xGridDashLength` / `yGridDashLength`: dash segment length.
- `xGridDashGap` / `yGridDashGap`: dash gap.
- `xGridDashOffset` / `yGridDashOffset`: dash offset.

---

## Hover Settings (fields, Cartesian2D only)

Hover Settings is visible in Cartesian2D. Key fields come from `hover`:

- **cursorLineColor**: hover cursor line color.
- **cursorLineWidth**: line width.
- **cursorLineDashed**: dashed or not.
- **cursorLineDashLength / cursorLineDashGap / cursorLineDashOffset**: dash parameters.

---

## Legend Settings (fields)

Legend Settings comes from `legendSettings` (it may be auto-hidden in some cases; see below).

- **enabled**: whether to show legend.
- **position**: legend position (Top/Bottom/Left/Right).
- **fontSize / color**: text size and color.
- **backgroundColor**: legend background color.
- **itemSpacing**: spacing between legend items.
- **offset**: offset relative to the edge.
  - When offset is default, a common offset is applied based on position (e.g. Bottom defaults to `y=-30`).

---

## Legend Settings (may be auto-hidden)

When the chart is a "pure Pie series" (only Pie/Ring/Pie3D, with no non-Pie series), Legend Settings may be hidden automatically.

This avoids showing meaningless or conflicting legend configuration in some layouts.

---

## Editing tips and troubleshooting

- **When making many changes**: use `Save` in the top toolbar to save the asset.
- **When changing key structure** (e.g. coordinate system, axis type, Series Type):
  - after the change, check whether Preview refreshes correctly
  - if inconsistent, try switching selection to trigger a rebuild

---

## Help

- Click the rightmost **Help** icon in the title bar to open this chapter.
