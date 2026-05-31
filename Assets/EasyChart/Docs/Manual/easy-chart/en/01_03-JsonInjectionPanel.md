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

- **Min/Max** (icon button)
  - Toggle panel height: Expand(□) / Collapse(-)
  - Collapsed: smaller height (more like an auxiliary tool)
  - Expanded: larger height (better for long JSON)

- **ApplyToChart** (icon button)
  - Attempts to parse the JSON in the text box as a Feed and apply it to the selected `ChartProfile`.
  - On success it will:
    - mark the asset dirty and call `SaveAssets()`
    - refresh the Series list
    - refresh Preview

- **Help** (icon button)
  - Click to open this documentation chapter.

---

## Controls (button row)

Below the header there is a row of buttons (may wrap):

- **API Envelope** (icon toggle)
  - Controls whether the example JSON is wrapped in an "API response" envelope.
  - Useful when you want to send the Feed directly to an HTTP API/service.
  - Toggling regenerates the example and overwrites the text box (see "overwrite rules").

- **Feed Mode** (dropdown)
  - Controls which levels/fields are included in the example JSON.
  - Options:
    - `Compact`: Data values only, minimal payload. Format: `{"series": [{"datas": [1, 2, 3]}]}`
    - `Standard`: Names + structured data objects. Includes `chartName`, `series[].name` and object array for `datas`
    - `Full`: All metadata including axes, types, colors, etc. For complete copy/migration of chart config
  - General recommendations:
    - **Quick data updates**: use `Compact`
    - **General editing and Apply**: use `Standard`
    - **Complete copy/migration**: use `Full`

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

---

## Runtime JSON Injection Components

In addition to the editor panel, EasyChart provides runtime JSON Injection components for updating chart data dynamically during gameplay.

### Component Locations

- **UGUI**: `EasyChart/UGUI Runtime JSON Injection`
- **UIToolKit**: `EasyChart/UI Toolkit Runtime JSON Injection`

### Main Properties

| Property | Description |
|----------|-------------|
| **Chart Element Name** (UIToolKit) | Optional. Specifies the target ChartElement name. Supports direct ChartElement lookup, or finding a TemplateContainer and extracting its internal ChartElement. |
| **JSON Mode** | JSON generation mode: `Compact` (values only), `Standard` (names + structured data), `Full` (all metadata) |
| **API Envelope** | Whether to wrap JSON in API response format |
| **Auto Generate** | When enabled, automatically regenerates example JSON when JSON Mode or API Envelope changes |
| **JSON Content** | The JSON string to inject. Can be edited directly or set via code |

### Main Methods

#### `ApplyJsonToChart()`
Parses JSON from `_jsonContent` and applies it to the chart.

```csharp
var injection = GetComponent<UIToolKitRuntimeJsonInjection>();
injection.JsonContent = jsonFromServer;  // Set JSON
injection.ApplyJsonToChart();            // Apply to chart
```

#### `ApplyFeed(ChartFeed feed)` ⭐ Recommended for periodic updates
Directly applies a pre-parsed Feed object, **skipping JSON parsing** for better performance.

```csharp
// Initial JSON parsing
var result = ChartJsonUtils.ValidateAndParseFeed(json);
if (result.IsValid) {
    _cachedFeed = result.Feed;  // Type is ChartFeed
}

// Subsequent periodic updates (called every frame/second)
injection.ApplyFeed(_cachedFeed);
```

#### `GenerateExampleJson()`
Generates example JSON based on current chart configuration. Returns the generated JSON string.

```csharp
string exampleJson = injection.GenerateExampleJson();
// Useful for understanding the current chart's data structure
```

### Event Callbacks

```csharp
injection.OnJsonApplied += (success, message) => {
    if (success) {
        Debug.Log("Data updated successfully!");
    } else {
        Debug.LogError("Update failed: " + message);
    }
};
```

### UIToolKit Multi-Chart Support

When the page has multiple charts, specify the target via **Chart Element Name**:

```xml
<!-- UXML Example -->
<ui:Instance template="BarDemo" name="Bar1" />
<ui:Instance template="PieDemo" name="Pie1" />
```

```csharp
// Specify name in script
injection.ChartElementName = "Bar1";  // Update Bar1
injection.ApplyJsonToChart();

injection.ChartElementName = "Pie1";  // Update Pie1
injection.ApplyJsonToChart();
```

**Lookup Logic**:
1. First tries to find a `ChartElement` with matching name directly
2. If not found, tries to find a `VisualElement` with matching name, then extracts its `ChartElement`

### Periodic Refresh Example

```csharp
public class ChartDataRefresher : MonoBehaviour
{
    public UIToolKitRuntimeJsonInjection injector;
    private ChartFeed _feed;  // Note: type is ChartFeed, not ChartJsonFeed
    
    void Start()
    {
        // Initial JSON parsing
        string initialJson = LoadInitialData();
        var result = ChartJsonUtils.ValidateAndParseFeed(initialJson);
        if (result.IsValid) _feed = result.Feed;
        
        // Refresh every 2 seconds
        InvokeRepeating(nameof(UpdateChartData), 2f, 2f);
    }
    
    void UpdateChartData()
    {
        // Fetch new data from server
        var newData = FetchFromServer();
        
        // Update values in feed
        for (int i = 0; i < _feed.data.Count; i++)
        {
            _feed.data[i].value = newData[i];
        }
        
        // Efficient apply (no JSON parsing overhead)
        injector.ApplyFeed(_feed);
    }
}
```

### Important Notes

1. **Profile Cloning**: Profile is automatically cloned on first injection to avoid modifying the original asset
2. **Multi-Chart Switching**: UIToolKit version supports dynamically switching `ChartElementName` to update different charts
3. **Error Handling**: Use `OnJsonApplied` event to catch parsing or application errors
4. **Performance Optimization**: For high-frequency updates, use `ApplyFeed()` instead of `ApplyJsonToChart()`
