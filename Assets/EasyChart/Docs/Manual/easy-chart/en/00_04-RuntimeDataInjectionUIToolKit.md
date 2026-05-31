# Runtime Data Injection (UI Toolkit)

This chapter explains how to inject data into `ChartElement` at runtime in a UI Toolkit workflow.

Related component: `UIToolKitRuntimeJsonInjection`

---

## 1. When should you use this approach?

- Your chart is built with UI Toolkit (`UIDocument` + UXML + `ChartElement`)
- You want to inject data via JSON at runtime
- You have already configured styles, axes, and Series types through a `ChartProfile`

This injection approach is primarily for **updating data**; structural changes (like adding new Series or forcing type overrides) are not its main purpose.

---

## 2. Quick start (recommended flow)

1. Prepare a `UIDocument` in the scene, and make sure there is a `ChartElement` in your UXML with a `ChartProfile` bound.
2. Add `UIToolKitRuntimeJsonInjection` to the same GameObject that has the `UIDocument` (Menu: `Add Component > EasyChart > UI Toolkit Runtime JSON Injection`).
3. Fill in the Inspector fields:
   - `Chart Element Name` (optional, used to specify the target `ChartElement` by name)
4. Click **Generate Example JSON** to generate a sample JSON matching the current Profile.
5. Modify the data in the `JSON Content` text area.
6. Click **Apply JSON to Chart**.

Internally, the component will:

- Find the target `ChartElement` from `UIDocument.rootVisualElement`
- Clone the Profile on first injection (to avoid modifying the original asset)
- Parse the JSON and apply it to the Profile
- Call `ForceRefreshProfile()` to refresh the chart

---

## 3. Inspector fields

Key fields of `UIToolKitRuntimeJsonInjection`:

### 3.1 Target

- **Chart Element Name**
  - The `name` of the target `ChartElement` (the UXML/USS name).
  - If empty, the component will find the first `ChartElement` in the UIDocument.

### 3.2 JSON Generation Settings

- **JSON Mode (`ChartJsonMode`)**
  - Controls the format when generating example JSON.
  - `Compact`: Data values only, minimal payload. Format: `{"series": [{"datas": [1, 2, 3]}]}`
  - `Standard`: Names + structured data objects. Includes `chartName`, `series[].name` and object array for `datas`
  - `Full`: All metadata including axes, types, colors, etc. For complete copy/migration of chart config
  - Recommended: start with `Standard` (more intuitive).

- **Use Api Envelope**
  - Whether to wrap generated JSON in an API response envelope:
    - `{ "code": 200, "message": "success", "data": { ...actual ChartFeed... } }`
  - When applying, it will also try to extract `data` from the envelope.

- **Auto Generate**
  - Automatically regenerate example JSON when `JSON Mode` or `Use Api Envelope` changes.

### 3.3 JSON Content

- **JSON Content**
  - The JSON string to inject.
  - If empty, clicking Apply will show a warning and return.

---

## 4. JSON format (ChartFeed)

The underlying data model is `ChartFeed`:

```json
{
  "chartId": "optional",
  "chartName": "optional",
  "axes": [
    {
      "axisId": "XBottom",
      "labels": ["Mon", "Tue", "Wed"]
    }
  ],
  "series": [
    {
      "serieId": "optional",
      "name": "optional",
      "type": "Line",
      "datas": [
        { "x": 0, "value": 12 },
        { "x": 1, "value": 18 }
      ]
    }
  ]
}
```

See the runtime code: `Scripts/Runtime/Feed/ChartFeed.cs`

### 4.1 axes field

- `axisId` is an `AxisId` enum (e.g., `XBottom`, `XTop`, `YLeft`, `YRight`).
- When `labels` is present, the axis is treated as Category and labels are overwritten.

### 4.2 series field

- **Matching priority**:
  - If `serieId` is provided: match by `Serie.id`
  - Else if `name` is provided: match by `Serie.name`
  - Else (index mode): match by feed index (i-th to i-th)
- `type`: mainly used for generating example JSON; the current injection path does not force type changes.
- `datas[]` for each point:
  - `x/y/z/value` numbers
  - `id/name` (optional)
  - `useColor/color` (optional)

---

## 5. Code usage

Besides manual operation in the Inspector, you can also call from code:

```csharp
var injection = GetComponent<UIToolKitRuntimeJsonInjection>();

// Set JSON content
injection.JsonContent = yourJsonString;

// Apply to chart
injection.ApplyJsonToChart();

// Or generate example JSON
injection.GenerateExampleJson();
string exampleJson = injection.JsonContent;
```

---

## 6. Common issues & troubleshooting

- **Not visible / TryGetChartElement failed**
  - Make sure the `UIDocument` component exists and is properly configured
  - Make sure there is a `ChartElement` in the UXML
  - If `Chart Element Name` is specified, make sure the name matches the UXML

- **JSON parse failed**
  - The component uses `ChartJsonUtils.ValidateAndParseFeed` for validation
  - It tries Newtonsoft first (if `Newtonsoft.Json` exists in your project)
  - Otherwise falls back to Unity `JsonUtility`
  - Recommendation: use **Generate Example JSON** to create a known-good JSON, then modify it

- **Series mismatch after injection / updated the wrong line**
  - Prefer `serieId` for stable matching
  - If you only use `name` and there are multiple series with the same name, the script uses the first one

- **Worried about modifying the original Profile asset**
  - The component automatically clones the Profile on first injection, creating a runtime copy
  - The cloned Profile name will have a `(Runtime)` suffix
  - The original asset will not be modified
