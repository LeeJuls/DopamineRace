# Runtime Data Injection (UI Toolkit)

This chapter explains how to inject data into `ChartElement` at runtime in a UI Toolkit workflow.

Related component: `EasyChartDataSource`

---

## 1. When should you use this approach?

- Your chart is built with UI Toolkit (`UIDocument` + UXML + `ChartElement`)
- You want a set of injection APIs that are easier to call from gameplay/business logic (labels / values / x-y / pie / ring)
- Or you want to inject a JSON payload directly (`ChartFeed`)

---

## 2. Quick start (recommended flow)

1. Prepare a `UIDocument` in the scene, and make sure there is a `ChartElement` in your UXML.
2. Add `EasyChartDataSource` to the same GameObject that has the `UIDocument`.
3. Fill in the Inspector fields:
   - `uiDocument`
   - `chartElementName` (default: `main-chart`, matches the `name` of the `ChartElement` in UXML)
   - `profile` (optional, but strongly recommended: lets style/Series type come from an editor-authored `ChartProfile`)
4. At runtime, call from code:
   - `SetCategoryLabels(...)`
   - `SetSeriesValues(...)` / `SetSeriesXY(...)`
   - or `ApplyJson(...)`

Internally, the component will:

- Find the target `ChartElement` from `UIDocument.rootVisualElement`
- Initialize chart data from `profile` when needed
- Modify `ChartElement.Data` and call `RefreshData()`

---

## 3. Inspector fields

Key fields of `EasyChartDataSource`:

- `uiDocument`
  - Points to the current UI `UIDocument`.
  - If not set, the script will try `GetComponent<UIDocument>()`.

- `chartElementName`
  - The `name` of the target `ChartElement` (the UXML/USS name). Default is `main-chart`.
  - If you want JSON `chartId/chartName` to locate the chart automatically, keep the `ChartElement.name` consistent with those values (see section 5).

- `profile`
  - Optional.
  - If set, the component assigns `ChartElement.Profile = profile` to initialize/preserve styles, Series structure, etc.

- `playAnimationOnRefresh`
  - After each injection, call `RefreshData(..., playAnimation: playAnimationOnRefresh)`.

- `allowCreateSeriesFromFeed`
  - When injecting via JSON (`ApplyJson`), if a series in the feed does not match any existing Serie:
    - `false` (default): do not create new Serie; only update matched ones.
    - `true`: allow creating new Serie from the feed (may rebuild renderers).

---

## 4. Common injection APIs (without JSON)

### 4.1 Set category axis labels

`SetCategoryLabels(labels, axisId = AxisId.XBottom)`

- Sets the axis to Category and overwrites `labels`.

### 4.2 Single-series Y values (auto x=0..n-1)

`SetSeriesValues("Sales", values)`

- Finds/creates a Serie by default (default type is Line; it does not force the type to change).
- Writes to `SeriesData.value` and sets `SeriesData.x` to the index.

### 4.3 XY points

`SetSeriesXY("Scatter", x, y)`

- Writes `x[]` into `SeriesData.x` and `y[]` into `SeriesData.value`.

### 4.4 Pie / Ring injection

- `SetPie(serieName, names, values)`
  - Forces the Serie type to `Pie`.
  - Uses `SeriesData.name` as slice name and `SeriesData.value` as slice value.

- `SetRing(serieName, names, percents)`
  - Forces the Serie type to `RingChart`.
  - Uses `SeriesData.name` as ring name and `SeriesData.value` as progress value.

---

## 5. JSON injection (ChartFeed)

You can call: `ApplyJson(json)`

This method parses JSON into `ChartFeed` and applies it to `ChartElement.Data`.

### 5.1 `ChartFeed` schema

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

See the runtime code `Scripts/Runtime/Feed/ChartFeed.cs` for the exact fields.

### 5.2 ChartElement lookup rules (`chartId` / `chartName`)

Internally, `ApplyJson` tries:

- If `chartId` is provided: `rootVisualElement.Q<ChartElement>(chartId)` first
- Else if `chartName` is provided: try `Q<ChartElement>(chartName)`
- If still not found: fall back to `chartElementName` (default `main-chart`)

Therefore:

- If you only have one chart, keeping the default is fine.
- If you have multiple `ChartElement` in one UI, it's recommended to align each chart's `name` with the feed `chartId` or `chartName`.

### 5.3 Series matching and type override

`ApplyJson` checks whether the JSON contains `"type":`. If present, it assumes you want to allow type override (`allowTypeOverride=true`).

Serie matching rules:

- If `serieId` is not empty: match by `Serie.id`
- Else if `name` is not empty: match by `Serie.name`
- Else (index mode): match by feed index (i-th to i-th)

When no Serie can be matched:

- `allowCreateSeriesFromFeed=false` (default): the feed series is skipped (no creation).
- `allowCreateSeriesFromFeed=true`: create a new Serie using the feed `type/name/serieId`.

For matched Serie:

- Only when `allowTypeOverride=true` and it's not index mode, overriding `id/name/type` is allowed.

---

## 6. Common issues & troubleshooting

- **Not visible / TryGetChart failed**
  - Make sure `uiDocument` is assigned correctly
  - Make sure the `ChartElement` `name` in UXML matches `chartElementName`

- **JSON parse failed**
  - When `EasyChartDataSource` parses JSON:
    - it tries Newtonsoft first (if `Newtonsoft.Json` exists in your project)
    - otherwise falls back to Unity `JsonUtility`, normalizing string forms like `type/axisId` into enum integers before parsing
  - Recommendation: start from a known-good JSON (e.g. generated from the editor JSON panel) and modify it.

- **Series mismatch after injection / updated the wrong line**
  - Prefer `serieId` for stable matching.
  - If you only use `name` and there are multiple series with the same name, the script uses the first one and logs a warning.

- **JSON wanted to add a Serie but none was added**
  - Enable `allowCreateSeriesFromFeed`.
