# JSON Injection Panel

This chapter explains the **JSON Injection** panel at the bottom-left of `Unity Easy Chart/Library Editor`.

Its purpose is to represent the current `ChartProfile` configuration (or externally imported configuration) as readable/copyable JSON, and supports **ApplyToChart** to parse the JSON and write it back into the selected Profile.

---

## Location and purpose

- **Location**: below the Library panel (tree view).
- **Main uses**:
  - **Export**: convert the selected `ChartProfile` into example JSON (Feed)
  - **Edit**: manually edit the JSON in the text box
  - **Import/Apply**: click **ApplyToChart** to parse and apply JSON into the selected `ChartProfile`

Use cases:

- **Debugging**: quickly validate whether a specific field takes effect.
- **Batch edits**: copy JSON to an external editor (multi-cursor/find-replace), then paste back and Apply.
- **Integrations**: e.g. your toolchain/scripts generate a Feed and you Apply it in the editor.

---

## Controls (header bar)

The header bar typically contains (left to right):

- **Min/Max** (label changes)
  - Toggles panel height.
  - `Min`: collapse to a smaller height (more like an auxiliary tool).
  - `Max`: expand to a larger height (better for long JSON).

- **ApplyToChart** (icon button)
  - Attempts to parse the JSON in the text box as a Feed and apply it to the selected `ChartProfile`.
  - On success it will:
    - mark the asset dirty and call `SaveAssets()`
    - refresh the Series list
    - refresh Preview

- **Help** (icon button)
  - Opens this chapter.

---

## Controls (button row)

Below the header there is a row of buttons (may wrap):

- **API Envelope** (icon toggle)
  - Controls whether the example JSON is wrapped in an "API response" envelope.
  - Useful when you want to send the Feed directly to an HTTP API/service.
  - Toggling regenerates the example and overwrites the text box (see "overwrite rules").

- **Feed Mode** (dropdown)
  - Controls which levels/fields are included in the example JSON.
  - Options come from an internal enum (common ones include):
    - `Lite`
    - `Standard / ID`
    - `Standard / Default`
    - `Standard / With Axes`
    - `Full`
  - General recommendations:
    - **Quickly inspect structure**: use `Lite`
    - **Need stable references**: use `Standard / ID`
    - **Need to include axes config**: use `Standard / With Axes`
    - **Need full copy/migration**: use `Full`

- **Datas Format** (dropdown)
  - Controls the output format of `seriesData` (data points).
  - Common options:
    - `Values`: more compact, mostly "values only".
    - `Standard`: default format, good for editing and Apply.
    - `Full`: more complete (may include more fields/structure), good for migration/restoration.

- **Copy** (icon button)
  - Copies the current text box content to the clipboard.

---

## Text box and "overwrite rules" (important)

The JSON text box is editable. To prevent your manual edits from being overwritten automatically, the panel has a "dirty" flag logic:

- **As soon as you manually change the text box**, it is considered "user modified" (dirty).
- When dirty:
  - the editor will not automatically overwrite your content with example JSON.
- However, switching the following options will **force overwrite** (and clear dirty):
  - `API Envelope`
  - `Feed Mode`
  - `Datas Format`
  - or when switching the selected Profile (resets to that Profile's example)

Recommendation:

- If you plan to do major edits:
  - Copy to an external editor first
  - Paste back and Apply when done

---

## ApplyToChart behavior and notes

- **ApplyToChart modifies the selected `ChartProfile` asset**.
- If JSON parsing fails, an error is logged to the Console:
  - `ApplyToChart failed: invalid JSON or unsupported format.`
- In `Full` mode, more meta/structural information may be overwritten (IDs/config, etc.), which is more powerful but also more dangerous.

Recommendations:

- Before applying, make sure:
  - the correct `ChartProfile` is selected on the left
  - JSON format is valid (brackets/commas)
  - you understand what the current Feed Mode will overwrite

---

## Recommended workflows

### 1) Export from current Profile and tweak

- Select a `ChartProfile`
- Choose appropriate `Feed Mode` / `Datas Format`
- Copy to an external editor for tweaks
- Paste back
- ApplyToChart

### 2) Import configuration from external sources

- Paste external JSON into the text box
- ApplyToChart
- Fine-tune further in Inspector / Series

---

## Help

- Click the rightmost **Help** icon in the header to open this chapter.
