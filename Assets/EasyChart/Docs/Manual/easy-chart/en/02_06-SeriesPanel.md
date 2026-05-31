# Series Panel (Data and Series)

This chapter explains the **Series** panel on the right side of `Unity Easy Chart/Library Editor`.

The Series panel edits `ChartProfile.series` from a "chart structure" perspective: you can add/remove/reorder series, set each Serie's type and parameters, and directly edit `seriesData` (data points).

---

## Panel structure overview

After you select a `ChartProfile`, the Series panel shows:

- a list of Serie cards (each card corresponds to `series[i]`)
- a **+ Add Series** button at the bottom

Each Serie card typically consists of three parts:

- **Header**: fold toggle + title + Help + Clone
- **Body**: Name / Id / Type / Settings / Data
- **Footer** (bottom-right controls): ↑ ↓ X

---

## Header: Collapse/Expand, Help, Clone

### Fold Toggle

- On the left side of the Header there is a small toggle:
  - `▼` means expanded
  - `▶` means collapsed
- The fold state is remembered (stored by Profile + index) to reduce visual clutter for long configurations.

### Help Button

- **Help** icon button on the right side of the Header
- Clicking opens the documentation chapter for the corresponding chart type:
  - Line → `03_01-LineChart`
  - Bar/HorizontalBar → `03_02-BarChart`
  - Scatter → `03_03-ScatterChart`
  - Heatmap → `03_04-HeatmapChart`
  - Radar → `03_05-RadarChart`
  - Pie/Pie3D → `03_06-PieChart`
  - RingChart → `03_07-RingChart`
  - Gauge → `03_08-GaugeChart`
  - Funnel → `03_09-FunnelChart`
  - Waterfall → `03_10-WaterfallChart`

### Clone Button

- **Clone** icon button on the right side of the Header
- Clicking clones the current serie and inserts a copy at the end of the array
- Cloning automatically generates a new unique `id` and increments the `name` (e.g., `Serie 1` → `Serie 2`)
- Useful for quickly creating new series with similar configurations

---

## Name and Serie Id

### Name

- The `Name` field displays and edits the name of the Serie.
- When you change Name, the card title updates and triggers a Preview refresh.

### Serie Id (read-only)

If the Serie supports an `id` field, the panel shows:

- **Serie Id** (read-only text field)
- **Copy** button to copy the id to the clipboard

This id is commonly used for:

- interaction/highlighting/external systems referencing a serie
- keeping references stable (especially when you reorder/add/remove series)

---

## Type (series type) and compatibility tips

### Type dropdown

- `Type` is used to select the SerieType (Line/Bar/Scatter/Pie/Radar...).
- The dropdown provides options based on the registry. If the current type is not in the list, it will be temporarily inserted to keep it visible.

### Compatibility warning

When the SeriesType is incompatible with the Profile `coordinateSystem`, the Series panel shows a warning message:

- rendering is still allowed (not forcibly blocked)
- but it warns that axis/grid semantics may be inconsistent

Typical example:

- Profile is `Polar2D` but the SeriesType is Line/Bar (not recommended)

### Pro-only type restrictions

Some types are not available in the Free version (e.g. RingChart / HorizontalBar / Heatmap / Pie3D).

- When you try to select these types without Pro installed:
  - a hint text will be shown
  - and the dropdown will automatically revert to the previous type (it will not modify the asset)

---

## Settings (series parameters)

The Series panel shows a group of Settings for each serie:

- The root foldout name changes by type (e.g. `LineSettings` / `BarSettings` / `PieSettings` ...).
- Some types have more detailed sub-foldouts (e.g. Ring layout/valueMapping, etc.).

Notes:

- Switching Type may trigger a "Settings instance replacement" (managedReference structure changes).
- When replacement happens, the UI rebuild is delayed by one frame to avoid invalid serialized handles.

---

## Data: seriesData (data points)

The Series panel directly shows the `seriesData` array (Unity's default array editor).

- It is expanded by default (easier to edit).
- When you add/remove/modify points, it triggers a Preview refresh.

Recommendation:

- If you have many data points, use the JSON Injection panel for batch editing.

---

## Footer: Reorder and delete (render order)

Each serie card has three buttons at the bottom-right:

- **↑**: move the serie up (`MoveArrayElement(index, index-1)`)
- **↓**: move the serie down (`MoveArrayElement(index, index+1)`)
- **X**: delete the serie (`DeleteArrayElementAtIndex(index)`)

Render order tip:

- Usually, **later series are drawn on top**.
- So you can use ↑↓ to control overlap (e.g. points/lines on top of bars).

---

## + Add Series

Click **+ Add Series** at the bottom:

- Inserts a new element at the end of the `series` array.
- Note: if there is already at least one serie, Unity's `InsertArrayElementAtIndex(arraySize)` will **duplicate the last element** (including type/settings).
- If this is the first serie, a default type is chosen based on coordinate system:
  - Polar2D: defaults to Radar
  - otherwise: defaults to Line

After adding, it typically will:

- auto-fill a name (e.g. `Serie N`)
- call `EnsureRuntimeData()`
- refresh the Series list and Preview

---

## Recommended workflows

### 1) Create a basic chart from scratch

- + Add Series
- Choose Line or Bar in Type
- Add a few points in seriesData
- Adjust axis range/visible count in Inspector

### 2) Adjust overlap

- Use ↑↓ to adjust series order
- Observe layering changes in Preview

### 3) Large data / batch editing

- Select appropriate `Feed Mode` (Compact/Standard/Full) in the JSON Injection panel
- Copy to an external editor to batch-generate/replace data
- Paste back and ApplyToChart

### 4) Quickly derive similar series

- After configuring a serie, click the **Clone** button
- Fine-tune data or style on the cloned serie

---

## Help

- Click the rightmost **Help** icon in the title bar to open this chapter.
