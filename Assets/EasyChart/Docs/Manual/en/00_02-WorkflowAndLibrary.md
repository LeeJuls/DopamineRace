# UI Toolkit Workflow (Recommended)

Goal of this chapter: explain the overall recommended EasyChart approach (primarily for UI Toolkit):

1. Edit `ChartProfile` in the editor with **`EasyChartLibraryWindow`**
2. **Export** `ChartProfile` **to `.uxml`** as your chart library assets
3. Compose pages in UI Toolkit using **UI Builder** / UXML, enabling fast UI assembly

This chapter focuses on **UI Toolkit (recommended)**. If you need the UGUI (Canvas/RectTransform) workflow, see:

- `00_03-UGUIWorkflow.md`

## 0. Why the UI Toolkit workflow is recommended

The key idea is to separate the "chart configuration source" (`ChartProfile`) from the "UI artifact" (exported UXML).

- `ChartProfile`: defines what the chart looks like, which axes it uses, what Series it has, and how data should be interpreted. This is best maintained centrally in the editor.
- Exported `.uxml`: places the chart into a page as a UI Toolkit component. This is best suited for reuse, composition, and version control.

What you get:

- Reusable configuration (multiple pages can share one chart style)
- Composable pages (drag & drop in UI Builder; no need to rebuild UI from scratch each time)
- Clearer collaboration (Profile as the "source", UXML as the "product/component library")

---

## 1. Why export to UXML

In a project, `ChartProfile` describes "what the chart looks like", "which axes it uses", "which Series it has", and "how data points are interpreted".

After exporting to `.uxml`, you get a reusable UI asset for UI Toolkit:

- Can be dragged directly in UI Builder
- Can be reused by multiple pages (same chart style)
- Can be managed by version control and asset pipelines (your "chart library")

---

## 2. Recommended workflow (from configuration to page)

### Step 1: Clone your working library and charts (recommended)

- Open from the Unity menu: `EasyChart/Library Editor`

Recommended process:

- **Clone Library**: clone your own Library first (avoid modifying the built-in demo library directly)
- **Clone ChartProfile**: in your library tree, right-click a Profile close to your target look, then choose `Clone` to create a variant
- Modify in the Inspector on the right:
  - `coordinateSystem`
  - `series`
  - `axes`

> Recommendation: keep your own Profiles under `Assets/EasyChart/Library/Custom/` (or a team-agreed folder).

### Step 2: Export to UXML (generate library assets)

You can export from the Library Editor:

- For a Profile: `Export to UXML`
- For a folder:
  - `Export Folder to UXML (Mirror)`
  - `Export Folder to UXML (Backup)`
- For all:
  - `Export All UXML (Mirror)`
  - `Export All UXML (Backup)`

Export root folder:

- `Assets/EasyChart/LibraryUxml/`

With multiple Libraries, the usual structure is:

- `Assets/EasyChart/LibraryUxml/<LibraryName>/...`

The `_Backups` subfolder is used for backup exports (and some JSON backups generated during exporting):

- `Assets/EasyChart/LibraryUxml/<LibraryName>/_Backups/...`

The exported UXML usually looks like:

- A `<ec:ChartElement profile-name="..." />`
- `profile-name` corresponds to the ChartProfile key (usually the asset file name)
- Width/height styles for the chart are written into the UXML as well

> Key point: treat exported `.uxml` as **reusable chart components**, not something you hand-write for every UI.

### Mirror vs Backup (which one should you use?)

- **Mirror**:
  - used to "mirror the current Profile state into UXML"
  - typically overwrites exports with the same name, and may remove stale files that no longer exist (keep the mirror consistent)
- **Backup**:
  - used to "export a snapshot" by time/tag
  - not recommended as the primary path that your pages reference (better for history/rollback)

### Step 3: Compose pages in UI Builder

In UI Builder:

- Open your page UXML
- Drag the exported chart `.uxml` from the Project window
- Combine it with other UI (Label, Button, ListView, etc.) into a full page

For the quickest export-pipeline verification, you can use the demo scene and template:

- Scene: `Assets/EasyChart/Demo/Scenes/EasyChart_QuickStart.unity`
- Template: `Assets/EasyChart/Demo/UIToolKit/NewUXMLTemplate.uxml`

Drag the exported chart `.uxml` into `NewUXMLTemplate.uxml`, then make sure the `UIDocument` in the scene references that template.

#### Exact steps in UI Builder (recommended order)

1. Open UI Builder (or double-click your page `.uxml`).
2. In the Project window, locate the exported chart `.uxml` (usually under `Assets/EasyChart/LibraryUxml/<LibraryName>/...`).
3. Drag the `.uxml` into the page hierarchy (recommended to put it inside a container `VisualElement`).
4. Save the page UXML.
5. Back in the scene, make sure `UIDocument` references the page `.uxml` you just saved.

#### What's inside an exported chart UXML

Exported `.uxml` typically contains an `EasyChart.ChartElement` with attributes like:

- `profile-name`: usually the ChartProfile asset file name (key)
- `profile-guid`: a more stable way to locate the asset

Therefore:

- If you only modify the Profile, the page will not change automatically: you need to re-export (Mirror) so the UXML gets updated.
- If you rename the Profile asset, the exported `profile-name` will also change (so keep naming stable when possible).

### Step 4: Load/replace data at runtime (depends on your product)

`ChartProfile`/UXML defines the "style and structure", while your data usually comes from business logic.

- Static display: fill data directly in the Profile `seriesData`
- Dynamic display: write/replace `seriesData` at runtime (and keep `SeriesData.id` stable)

---

## 3. Folder layout recommendations for your chart library

Recommended to separate the "source configuration" and the "exported artifacts":

- `Assets/EasyChart/Library/Custom/`: the `ChartProfile` assets you maintain
- `Assets/EasyChart/LibraryUxml/`: exported UXML (both Mirror and Backup exports live under this root)

When using multiple Libraries, exported assets are typically organized by library name:

- `Assets/EasyChart/LibraryUxml/<LibraryName>/...`

Recommended conventions:

- **Maintain Profiles only under `Assets/EasyChart/Library/...`** (as the source of truth)
- **Pages reference only Mirror exports** (as your component library)
- Treat Backup exports purely as **historical snapshots**

> Benefits:
> - Your configuration source stays readable and editable
> - Exported artifacts are reusable, composable, and can be used directly in UI Builder

---

## 4. Common issues & troubleshooting

- **Can't find the exported chart UXML in UI Builder**
  - First check whether files were generated under `Assets/EasyChart/LibraryUxml/`
  - If you use multiple Libraries, check under `Assets/EasyChart/LibraryUxml/<LibraryName>/`
  - Mirror/Backup exports may appear under `_Backups`; pages should not reference files under `_Backups`

- **The page references the UXML, but nothing shows at runtime**
  - Make sure the scene `UIDocument` references the page `.uxml` you edited
  - In the Library Editor, use Preview to verify the Profile renders correctly (rule out Profile configuration issues first)

- **You modified the Profile but the page didn't change**
  - The Profile is the "source"; the page references exported UXML
  - After modifying a Profile, re-export (Mirror), then return to the page and refresh/save

- **The component is visible in UI Builder, but still doesn't show at runtime**
  - First verify the scene `UIDocument` truly references the intended page (not an older page)
  - Then verify the Profile renders correctly in the Library Editor Preview

---

## 5. What to read next

- To quickly complete a single chart end-to-end: `00_01-QuickStart.md`
- To use charts with UGUI (Canvas/RectTransform): `00_03-UGUIWorkflow.md`

---

## 6. Editor workflow & panels quick reference (Library Editor)

This section consolidates the editor workflow and panel explanations that were previously spread across multiple chapters, serving as a quick reference when working in `EasyChart/Library Editor`.

### 6.1 What are you editing? (ChartProfile)

The chart selected in the Library Editor is essentially a `ChartProfile` asset.

- It's a reusable configuration: the same Profile can be referenced by multiple scenes/prefabs.
- It's previewable: changes in the editor can be previewed immediately.

### 6.2 Main areas of the Library Editor

You can think of the window as four areas:

- Left: Library (asset tree)
- Center: Preview
- Right: Inspector (configuration)
- Right: Series (series and data)

Additionally, there is usually a JSON Injection panel on the left.

### 6.3 Library panel (asset tree)

Overview:

- Displays folders and `ChartProfile` (`.asset`) files in a tree.
- Selecting a `ChartProfile` drives binding and refresh for Inspector/Series/Preview.
- Supports create/rename/delete, drag-move, and sorting.

Selection logic:

- Selecting a folder: clears the Inspector/Series panels (no Profile to edit).
- Selecting a ChartProfile: binds the right panels to that Profile.

Common actions (title bar and right-click menu; may vary by version):

- Folder: New Folder / New Chart / Export Folder to UXML (Mirror/Backup) / Rename / Delete
- ChartProfile: Export to UXML / Clone / Rename / Delete

### 6.4 Preview panel

Preview renders the currently selected `ChartProfile` directly, so you can validate changes while editing.

Common issues:

- Preview is empty: make sure there is at least 1 `Serie` and its `seriesData` is not empty.
- Data exists but looks wrong: verify CoordinateSystem matches the SeriesType, and axis ranges are not excluding your data.

### 6.5 Inspector panel

Inspector edits the serialized fields of the Profile (coordinate system, axes, grid, interaction, legend, etc.), and drives Preview updates.

Tip:

- If some field changes appear to have no effect, confirm the page references the exported UXML, not the Profile directly.

### 6.6 Series panel

The Series panel edits `ChartProfile.series` from a "chart-structure" perspective:

- Add/remove/reorder series
- Choose `type` for each serie and edit `settings`
- Edit `seriesData` (data points)

### 6.7 JSON Injection panel

Purpose: represent the current Profile as copyable JSON, and support parsing JSON to write back into the current Profile.

Recommended workflow:

1. Generate example JSON from the current Profile
2. Copy it into an external editor for batch edits
3. Paste it back and ApplyToChart

---

## 7. Axes & ranges (Axis & Range)

### 7.1 AxisType: Category vs Value

- Category: use `labels` to define discrete categories (A/B/C, or Mon/Tue/Wed).
- Value: continuous numeric range (0~100, -3~3, 0~1e6).

#### 7.1.1 When to use Category

- The X axis is a sequence of text labels
- You want points to land on `labels[i]`
- Typical: bar charts (one bar group per category), line charts (aligned by categories)

Key points for Category:

- `labels[0]` corresponds to category index `0`
- `labels[1]` corresponds to category index `1`

#### 7.1.2 When to use Value

- X or Y is a continuous numeric value (timestamp, money, temperature, etc.)
- You want to scale/pan the axis by numeric values

Key points for Value:

- Axis range is usually computed by auto range (if enabled)
- You can lock only one side (e.g. fix min=0 and keep max auto)

### 7.2 Category axis: labels and LabelPlacement

`labels` determines the number of categories and the label text.

`LabelPlacement` affects alignment:

- `Tick`: labels align to tick marks; better for Line/Scatter.
- `CellCenter`: labels align to the center of a cell; better for Bar/Heatmap.

Common symptom:

- Bars appear between two labels: set `LabelPlacement` to `CellCenter`.

### 7.3 Value axis: autoRangeMin / autoRangeMax

If the range is "locked" and data is not visible, revert to full auto range first:

- Enable `autoRangeMin/autoRangeMax`

After it's visible, add business constraints gradually (e.g. make bar chart Y start at 0).

#### 7.3.1 Common template: Y axis starts at 0

- `axisType = Value`
- Fix `minValue = 0`
- `autoRangeMax = true`

#### 7.3.2 Common template: lock only Max (e.g. percentages)

- Fix `maxValue = 100`
- `autoRangeMin = true`

### 7.4 rounding / unit / labelFormat

- rounding: snap the range to "nicer" numbers.
- unit: display unit scaling (K/M, ten-thousand/million, etc.).
- labelFormat: control number formatting (N0/N2/F1/percent, etc.).

#### 7.4.1 Unit display (showUnit / unitText)

When values are large (e.g. 10,000+), a common approach is showing a unit at the end of the axis (e.g. "k", "M").

#### 7.4.2 Quick troubleshooting

- Labels misaligned / bars centered between labels: check Category axis `LabelPlacement`
- Range looks weird (too large/too small): check if min/max is locked; check rounding/unit
- Too many decimals in ticks: set `labelFormat`

---

## 8. Series and data (Serie / SeriesData)

### 8.1 Serie (one series)

Each element in `ChartProfile.series` is a `Serie`:

- `name`
- `type`
- `visible`
- `settings`
- `labelSettings`
- `seriesData`

Note: `settings` is usually a polymorphic object (`SerializeReference`). When you change `type`, the editor will try to preserve the last used settings for each type (better editing experience).

### 8.2 SeriesData (one data point)

Common `SeriesData` fields:

- `id`: stable identifier (tooltip/hover/hidden state).
- `x`: X coordinate or Category index.
- `value`: main value.
- `y`: second dimension (scatter/heatmap, etc.).
- `z`: third dimension (e.g. sizeMapping).
- `name`: point name (often used by Radar/Pie/Ring).
- `useColor` + `color`: point-level color override.

If interactions are enabled, keep `SeriesData.id` stable to avoid generating a new set of ids on every data refresh.

### 8.3 Matching SerieType and coordinate system

- Cartesian2D: Line/Bar/Scatter/Heatmap
- Polar2D: Radar

It's not recommended to mix Polar and Cartesian series in a single (non-Pie) ChartProfile. If you do mix them, be careful about whether axes/grid semantics remain consistent.

### 8.4 Common data patterns (by type)

#### 8.4.1 Line

- Common: Category X + Value Y
  - Data point: `x=category index`, `value=value`
- Continuous: Value X + Value Y
  - Data point: `x=x value`, `value=y value`

#### 8.4.2 Bar

- Category X + Value Y
  - One point per bar: `x=category index`, `value=bar height`
- Grouped: multiple Bar series share the same Category X
- Stacked: series with `stacked=true` and the same `stackGroup` will stack

#### 8.4.3 Scatter

- Common: X=Value, Y=Value
- Recommended to explicitly write `x/y` for data points

#### 8.4.4 Heatmap

- Triplet: `x=column index`, `y=row index`, `value=intensity`

#### 8.4.5 Radar

- Typical: `x=dimension index`, `value=value of that dimension`, `name=dimension label`

### 8.5 Common data pitfalls (symptom-driven)

- Category chart uses Category axis on X, but point `x` is not 0/1/2...
  - Symptom: points/bars don't align with labels
  - Fix: ensure `x=category index`, or change X axis to Value

- NaN/Infinity appears
  - Symptom: chart doesn't render, range explodes
  - Fix: filter invalid values at the data source

- Chart is not visible (but `seriesData` is not empty)
  - Check: coordinate system matches (Cartesian vs Polar)
  - Check: AxisType matches your data meaning

- Interactions/tooltip mapping feels wrong
  - Check: `SeriesData.id` is stable (don't randomly regenerate ids on each refresh)
