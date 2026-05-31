# Preview Panel

This chapter explains the **Preview** panel at the top of the middle area in `Unity Easy Chart/Library Editor`.

The purpose of Preview is to render the currently selected `ChartProfile` directly, so you can see results immediately while editing.

---

## When does Preview refresh

Preview refresh is **delayed** (to avoid excessive redraw while you are continuously dragging/typing):

- When you modify any bound field in **Inspector** or **Series**, it triggers `ScheduleUpdatePreview()`.
- When you click **ApplyToChart** in **JSON Injection**, it triggers a refresh.
- When you switch to another `ChartProfile` in the left **Library** tree, Preview refreshes to the new Profile.

Implementation-wise, the refresh is scheduled via `EditorApplication.delayCall`, so you may feel it updates "a moment after" your change. This is expected.

---

## What does Preview display

- Preview draws using a runtime chart component (e.g. `ChartElement`).
- Preview reads data from the currently selected `ChartProfile` and renders it.

You can think of Preview as:

- **what you edit is what it renders**
- **what you see is (mostly) the runtime effect**

---

## Common issues and troubleshooting

### 1) Preview is empty

Check first:

- Is a `ChartProfile` selected?
- Is there at least one `Serie`?
- Is `seriesData` empty (no data points)?

### 2) Data exists but looks wrong / not visible

Common causes:

- **Coordinate system and SeriesType mismatch**: e.g. the Profile is `Polar2D` but the Series type is not Radar.
- **Axis range and data range mismatch**: e.g. all values are outside the axis range.
- **Category axis visible count (VisibleCount) is too small**: only a small segment is shown.

### 3) Console shows "Preview refresh failed"

If an exception occurs during refresh, the Console logs:

- `[EasyChartLibraryWindow] Preview refresh failed: ...`

This usually means:

- some configuration combination is invalid
- or some field value is unexpected (e.g. null / NaN)

Recommended handling:

- revert the most recent change first
- then re-apply changes step by step to locate which field triggers the exception

---

## Tips

- Preview only focuses on rendering results. Structural issues usually need to be fixed in **Inspector/Series/JSON Injection**.
- If you modify many fields in a short time, Preview may refresh only after your last change (for performance).

---

## Help

- Click the rightmost **Help** icon in the title bar to open this chapter.
