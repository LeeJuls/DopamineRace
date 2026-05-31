# Editor Workflow and Panel Guide

This chapter helps you understand **what to edit where** in `EasyChart/Library Editor`, and the common editing workflows (create, clone, preview, export).

---

## 1. What are you editing? (ChartProfile)

The chart selected in the Library Editor is essentially a `ChartProfile` asset.

- It is a **reusable configuration**: the same Profile can be referenced by multiple scenes/prefabs.
- It is a **previewable configuration**: after modifying it in the editor, you can see the preview update immediately.

Recommendation: put your own Profiles under `Assets/EasyChart/Library/Custom/` (or a folder agreed by your team).

---

## 2. The three core areas of the Library Editor

Although UI details may change slightly across versions, you can understand the overall layout like this:

### 2.1 Left: Library Tree

This shows the folder structure where chart assets (`ChartProfile`) are located.

Common actions:

- Right-click a folder:
  - `New Folder...`: create a subfolder
  - `New Chart...`: create a new `ChartProfile`
- Right-click a chart:
  - `Clone`: duplicate a new Profile (to create variants)
  - `Export to UXML`: export (if your workflow needs to sync/export configuration into UXML)
  - `Ping`: locate the asset in the Project window
  - `Rename...` / `Delete`

> Tip: use `Clone` to create variants like "same chart with different colors / different data scale", instead of configuring from scratch.

### 2.2 Right: Inspector

This is where you do most of your editing.

It typically includes:

- **Basic settings**:
  - `coordinateSystem`
  - `padding` (if present)
  - `animationDuration` (if present)
- **Series list**: each Serie represents a line, a group of bars, a scatter series, etc.
- **Axes**:
  - choose which `XAxisId/YAxisId` to use
  - configure display/labels/range/ticks of the corresponding `AxisConfig`
- **Legend / Tooltip / Grid**: if your version exposes these settings

> Practical tip: configure `coordinateSystem`, `Series`, and `Axes` first. The rest is "nice to have".

### 2.3 Preview

Use it to check:

- whether data exists
- whether axis ranges are correct
- whether labels are crowded/misaligned
- Tooltip / Legend interactions (if enabled)

When preview looks wrong, troubleshoot in this order:

1. Is `coordinateSystem` correct?
2. Does `series` contain at least 1 serie and data points?
3. Does the axis `axisType` match the meaning of your data `x/y`?
4. Is the Value axis range locked manually (`autoRangeMin/autoRangeMax`)?

### 2.4 JSON Injection

Below the left panel there is a **JSON Injection** area, used to:

- quickly generate an "injection JSON example" for the selected `ChartProfile`
- apply your pasted/edited JSON back to the selected Profile (`ApplyToChart`)

Common controls:

- **API Envelope**: whether to wrap with `{ code, message, data }`.
  - When enabled: generated JSON will be wrapped; parsing can also recognize it and automatically extract `data`.
- **Feed Mode**: the structure/field completeness of the example JSON (to support different injection protocols).
- **Datas Format**: the point format inside the `datas` field (e.g. compact arrays, or more readable objects).
- **ApplyToChart**: parse JSON from the text box and write it back into the selected `ChartProfile`.

---

## 3. Recommended editing flow (from zero to reusable)

### Step 1: Create or select a ChartProfile

- New: right-click the target folder and choose `New Chart...`
- Existing: click to select in the left tree

If you prefer the "clone first, then modify" approach (recommended):

- First, use **Clone** in the top toolbar to create your own Library
- Then in your own library, right-click a Profile -> `Clone` to create variants

### Step 2: Decide the coordinate system

- `Cartesian2D`: Line/Bar/Scatter/Heatmap
- `Polar2D`: Radar

> Tip: decide the coordinate system first, then choose SeriesType, to avoid style/axis confusion after switching later.

### Step 3: Configure Series

- Add Series
- Set `type`
- Fill `seriesData`

Tip: start with a small number of points (3–8) to validate the look, then scale up.

### Step 4: Configure Axes

Most common combination:

- X: Category
  - put text into `labels`
- Y: Value
  - enable auto range (default)

When you want more professional axis formatting:

- Use `labelFormat` (e.g. `F1`, `N0`)
- Use `autoRangeMin/autoRangeMax` to lock only one side of the range
- If the Value axis needs a unit:
  - `showUnit=true`
  - `unitText="items"/"10k"`
  - use `unitLabelStyle` to adjust font/color/position

### Step 5: Clone variants (recommended)

When you need multiple versions of the same chart (colors, font size, slightly different axis display):

- Right-click the chart -> `Clone`
- Modify only the differences

This keeps style consistent and is easier for version management.

When you want to use the chart in UI:

- UI Toolkit: export to UXML, then compose the page in UI Builder (see demo scene `Assets/EasyChart/Demo/Scenes/EasyChart_QuickStart.unity` and template `Assets/EasyChart/Demo/UIToolKit/NewUXMLTemplate.uxml`)
- UGUI: export to a UGUI prefab and use it in a Canvas/RectTransform workflow

---

## 4. Common pitfalls (quick diagnosis)

- **Nothing shows up**
  - Is `seriesData` empty?
  - Does `AxisType` match the meaning of your data (does the Category axis have labels)?

- **Value axis looks weird (range too large/too small)**
  - Check `autoRangeMin/autoRangeMax`
  - Check whether rounding/unit snapped the range to an unsuitable unit

- **Bars and labels are misaligned**
  - Check `LabelPlacement` (Tick vs CellCenter)

---

## Next

- `00_02-WorkflowAndLibrary.md`: axis types, label placement, auto range, rounding, and unit display are merged into section 7
