# Runtime Data Injection (UGUI)

This chapter explains how to inject data into charts via JSON at runtime in a UGUI workflow (`UGUIChartBridge`).

Related component: `UGUIRuntimeJsonInjection`

---

## 1. When should you use this approach?

- You have JSON from a server/business layer (or you want to quickly edit JSON at runtime)
- You want to work like the editor's `JSON Injection` panel: "generate example → modify → apply"
- You have already configured styles, axes, and Series types through a `ChartProfile`

This injection approach is primarily for **updating data**; structural changes (like adding new Series or forcing type overrides) are not its main purpose.

---

## 2. Quick start (recommended flow)

1. Set up `UGUIChartBridge` in the scene (and make sure `Profile` is assigned).
2. Add `UGUIRuntimeJsonInjection` to the same GameObject (Menu: `Add Component > EasyChart > UGUI Runtime JSON Injection`).
3. Click **Generate Example JSON** to generate a sample JSON matching the current Profile.
4. Modify the data in the `JSON Content` text area.
5. Click **Apply JSON to Chart**.

Internally, the component will:

- Clone the Profile on first injection (to avoid modifying the original asset)
- Parse JSON → convert to `ChartFeed`
- Apply `ChartFeed` to the cloned Profile
- Call `_bridge.Refresh()` to refresh the chart

---

## 3. Component & Inspector fields

`UGUIRuntimeJsonInjection` must be on the same GameObject as `UGUIChartBridge` (the script has `[RequireComponent(typeof(UGUIChartBridge))]`).

### 3.1 JSON Generation Settings

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

### 3.2 JSON Content

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
var injection = GetComponent<UGUIRuntimeJsonInjection>();

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

- **Click Apply no response / console warning: No UGUIChartBridge or ChartProfile found**
  - Make sure the object has `UGUIChartBridge`
  - Make sure `UGUIChartBridge.Profile` is assigned

- **Error: Failed to parse JSON**
  - The component uses `ChartJsonUtils.ValidateAndParseFeed` for validation
  - Use Generate to create a known-good JSON first, then modify it
  - If your API response has an outer wrapper, check `Use Api Envelope`, or make sure the `data` field contains the `ChartFeed`

- **JSON applied but data didn't change / only partially changed**
  - Check the `series` matching method (`serieId` / `name` / index mode)
  - If using `serieId/name` matching: make sure the Profile actually has the corresponding Serie
  - If using "index mode" (`serieId` and `name` are both empty):
    - When feed's `series[]` count **exceeds** Profile's Series count, new Series will be auto-created
    - If you don't want auto-creation, explicitly fill `name` or `serieId` for each serie

- **Worried about modifying the original Profile asset**
  - The component automatically clones the Profile on first injection, creating a runtime copy
  - The cloned Profile name will have a `(Runtime)` suffix
  - The original asset will not be modified
