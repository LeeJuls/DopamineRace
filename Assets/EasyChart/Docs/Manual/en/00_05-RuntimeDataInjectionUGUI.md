# Runtime Data Injection (UGUI)

Related scripts: `UGUIRuntimeJsonInjection`, `UGUIRuntimeJsonInjectionEditor`

This chapter explains how to inject data into charts at runtime via JSON in a UGUI workflow (`UGUIChartBridge`).

---

## 1. When should you use this approach?

- You have JSON coming from a server/business layer (or you want to quickly edit JSON at runtime)
- You want an editor-like workflow: "Generate example → Modify → Apply" (similar to the `JSON Injection` panel)
- You already configured the chart structure (style/axes/Series types) via `ChartProfile`

This injector is primarily designed for **updating data**. Structural changes (e.g. adding Series, force-overriding Series types) are not its main goal.

---

## 2. Quick start (recommended flow)

1. Set up `UGUIChartBridge` in the scene (and make sure `Profile` is assigned).
2. Add `UGUIRuntimeJsonInjection` to the same GameObject.
3. Click **Generate Example JSON** to generate sample JSON that matches your current Profile.
4. Modify the data in the `JSON Content` text box.
5. Click **Apply JSON to Chart**.

Internally, the component will:

- Parse JSON → convert to `ChartFeed`
- Apply `ChartFeed` to `UGUIChartBridge.Profile`
- Call `_bridge.Refresh()` to redraw

---

## 3. Component and Inspector fields

`UGUIRuntimeJsonInjection` must be on the same GameObject as `UGUIChartBridge` (the script has `[RequireComponent(typeof(UGUIChartBridge))]`).

### 3.1 JSON Generation Settings

- **Example Mode (`ChartJsonExampleMode`)**
  - Controls the format when generating example JSON.
  - Generally recommended to start with `Standard` or `Standard_Axis` (more intuitive).

- **Data Mode (`ChartJsonDatasMode`)**
  - Controls how `datas` is represented.
  - `Standard`: `datas` is an array of objects (e.g. `{ "x": 0, "value": 12 }`).
  - `Values`: `datas` is an array of raw numbers (shorter).
    - Note: this format requires the "flexible parser" in `ChartJsonUtils` (reflection-based parsing via Newtonsoft). If your project does not include Newtonsoft (`Newtonsoft.Json` / `Unity.Newtonsoft.Json`), parsing may fail.
    - Therefore **`Standard` is recommended by default**, unless you're sure Newtonsoft is available.

- **API Envelope (`UseApiEnvelope`)**
  - When generating example JSON, whether to wrap it with an API envelope:
    - `{ "code": 200, "message": "success", "data": { ...the real ChartFeed... } }`
  - When applying, it will also try to extract `data` automatically.

- **Auto Generate (`AutoGenerateJson`)**
  - Automatically regenerates example JSON when you change `Example Mode / Data Mode / API Envelope`.

### 3.2 JSON Content

- **JSON Content (`JsonContent`)**
  - The JSON string to inject.
  - If empty, clicking Apply will log a warning and return.

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

Field-to-code mapping notes:

- `chartId` / `chartName`
  - In the `UGUIRuntimeJsonInjection` injection path, it **will not overwrite** the Profile `chartId/chartName` (it calls `ChartJsonUtils.ApplyFeedToProfile(profile, feed)` with `allowMetaOverwrite=false`).
  - But these fields can help other injectors (e.g. `EasyChartDataSource`) locate a `ChartElement` by name/ID in the UI tree.

- `axes[]`
  - `axisId` is the `AxisId` enum (e.g. `XBottom`, `XTop`, `YLeft`, `YRight`).
  - If `labels` exists, that axis is treated as Category and labels are overwritten.

- `series[]`
  - **Matching priority**:
    - If `serieId` is provided: match by `Serie.id`
    - Else if `name` is provided: match by `Serie.name`
    - Else (both `serieId` and `name` are empty): match by index (feed 0 -> profile 0)
  - `type`
    - Mainly used when generating example JSON.
    - In the current injection path:
      - For existing matched Serie: it **will not force the type to change** (meta overwrite is not allowed).
      - For newly created Serie in "index mode + feed exceeds Profile series count": it will use the feed `type` as the new Serie type.
  - `datas[]` for each point:
    - numeric `x/y/z/value`
    - optional `id/name`
    - optional `useColor/color`

---

## 5. What happens when you apply? (injection flow)

When you click **Apply JSON to Chart**:

1. If the JSON is wrapped in an API envelope (contains `data`), it first tries to extract the object under `data`.
2. Calls `ChartJsonUtils.TryDeserializeFeed(json, out feed)` to deserialize into `ChartFeed`.
   - Tries Newtonsoft first (if available); otherwise falls back to Unity `JsonUtility`.
   - String values like `type: "Line"` / `axisId: "XBottom"` are normalized to enum values in the fallback path before parsing.
3. Calls `ChartJsonUtils.ApplyFeedToProfile(_bridge.Profile, feed)` to write the feed back into the Profile.
4. Calls `_bridge.Refresh()` to redraw.

---

## 6. Common issues & troubleshooting

- **Click Apply does nothing / console warns: No UGUIChartBridge or ChartProfile found**
  - Make sure the object has `UGUIChartBridge`
  - Make sure `UGUIChartBridge.Profile` is assigned

- **Error: Failed to parse JSON**
  - Generate a known-good JSON first, then modify it.
  - If your API response has an outer wrapper, enable `API Envelope`, or ensure the JSON `data` field contains the `ChartFeed`.

- **JSON applied but data didn't change / only partially changed**
  - Check how `series` is matched (`serieId` / `name` / index mode).
  - If you use `serieId/name` matching: make sure the corresponding Serie exists in the Profile (this injection path won't auto-create new Serie in this mode).
  - If you use "index mode" (both `serieId` and `name` are empty):
    - When feed `series[]` count **exceeds** the Profile series count, it will auto-create additional Serie.
    - If you don't want auto-creation, provide an explicit `name` or `serieId` for each serie.

- **After injecting in Play Mode, the Profile asset became dirty**
  - Injection essentially "applies the feed to the `ChartProfile`". If you drag the asset directly into the bridge, runtime changes may mark the asset dirty.
  - If you don't want to modify the asset, instantiate a runtime copy of the Profile and inject into that copy.
