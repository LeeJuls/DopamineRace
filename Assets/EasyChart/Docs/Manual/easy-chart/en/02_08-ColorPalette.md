# Series Panel - Color Palette

This chapter explains the **Color Palette** feature in the Series panel toolbar, used to quickly apply unified color schemes to multiple Series.

---

## 1. Palette Icon Location

In the Library Editor's **Series panel** toolbar, from left to right:

1. **Palette Icon** (colored circle 🎨) - Open color scheme selector
2. **Sync Toggle** (sync icon) - Enable/disable property sync
3. **Add Serie Button** (+) - Add new Series

Click the **colored circle icon** to open the palette selector.

---

## 2. Palette Selector Interface

The palette selector is a popup containing:

### 2.1 Palette List

Displays all available color schemes, each showing:

- **Name**: Palette name (e.g., "Google Charts", "D3 Category")
- **Color Preview**: Horizontally arranged color samples
- **Color Count**: Shows number of color sets in the scheme

### 2.2 Action Buttons

- **Manage Palettes**: Open palette manager (locates asset file)
- **Reset to Defaults**: Reset to default palette configuration

---

## 3. Using Palettes

### 3.1 Apply Color Scheme

1. Select a `ChartProfile` in the Library panel
2. Ensure the Profile has multiple Series
3. Click the **palette icon** (colored circle) in the Series panel toolbar
4. Click the desired color scheme in the popup
5. Observe Preview panel - all Series colors are updated

### 3.2 Application Rules

Palette application rules:

| Serie Index | Color Set Used |
|-------------|---------------|
| Serie 1 | Palette color set 1 |
| Serie 2 | Palette color set 2 |
| Serie 3 | Palette color set 3 |
| ... | And so on |
| Exceeds color count | **Cycles through** |

**Example**: Palette has 6 color sets, chart has 8 Series:
- Series 1-6: Use colors 1-6
- Series 7-8: Cycle to colors 1-2

---

## 4. Built-in Palettes

EasyChart includes multiple professional color schemes:

### 4.1 Google Charts

- **Style**: Bright modern, based on Google Material Design
- **Colors**: Blue, Red, Yellow, Green, Purple, Cyan
- **For**: General business charts

### 4.2 D3 Category

- **Style**: Web visualization standard
- **Colors**: Blue, Orange, Green, Red, Purple, Brown, Pink, Gray
- **For**: Data visualization reports

### 4.3 Tableau 10

- **Style**: Industry standard, professional business
- **Colors**: Steel Blue, Orange, Green, Red, Purple, Brown, Pink, Gray
- **For**: Business presentations, analysis reports

### 4.4 Modern Blue

- **Style**: Modern clean, blue dominant
- **Colors**: Blue, Orange, Green, Purple, Cyan, Pink
- **For**: Corporate branding, tech style

### 4.5 Cool Ocean

- **Style**: Cool tones, ocean style
- **Colors**: Emerald, Teal, Blue, Purple, Indigo, Dark Blue
- **For**: Environmental data, ocean themes

### 4.6 Warm Sunset

- **Style**: Warm tones, sunset style
- **Colors**: Coral, Orange, Gold, Pink, Magenta, Dark Red
- **For**: Energetic, lively scenes

---

## 5. Custom Palettes

### 5.1 Open Palette Manager

1. Click **Manage Palettes** at the bottom of the selector
2. Project panel auto-locates: `Assets/EasyChart/Editor/SeriesColorPalettes.asset`

### 5.2 Edit Palettes

Select `SeriesColorPalettes.asset`, in Inspector:

#### Add New Palette

1. Expand `palettes` list
2. Click `+` button to add new palette
3. Set `name` field (palette name)
4. Expand `colorSets` to add color sets

#### Edit Color Sets

Each **SeriesColorSet** contains:

| Field | Description | Default |
|-------|-------------|---------|
| `baseColor` | Main color (line/bar/point) | White |
| `areaColor` | Area fill color | Semi-transparent baseColor |
| `highlightColor` | Highlight color (hover) | Brightened baseColor |

**Auto-generation**:
- If only `baseColor` is set, `areaColor` and `highlightColor` auto-generate
- `areaColor` = baseColor with reduced transparency
- `highlightColor` = baseColor with increased brightness

#### Delete Palette

1. Find the palette in `palettes` list
2. Click `-` button
3. Click **Reset to Defaults** to restore default config

---

## 6. Color Set Details

### 6.1 Color Set Structure

```
SeriesColorSet
├── baseColor (Color)      # Main color
├── areaColor (Color)      # Area fill
└── highlightColor (Color)  # Highlight
```

### 6.2 Application by Chart Type

| Chart Type | baseColor | areaColor | highlightColor |
|-----------|-----------|-----------|---------------|
| Line | Line color | Area below line | Point highlight |
| Bar | Bar color | - | Bar highlight |
| Scatter | Point color | - | Point highlight |
| Pie | Slice color | - | Slice highlight |
| Radar | Line color | Fill area | Point highlight |

---

## 7. Palettes and Sync

### 7.1 Difference Between Features

| Feature | Function | Persistence |
|---------|----------|-------------|
| **Palette** | One-time color application | One-time operation |
| **Sync** | Continuous property sync | Continuously active |

### 7.2 Recommended Workflow

**Scenario 1: Palette First, Then Sync**
1. Use Palette for initial color scheme
2. Enable Sync to adjust other properties (line width, animation)
3. Disable Sync for individual adjustments

**Scenario 2: Palette Only**
1. Use Palette for colors
2. Keep Sync disabled
3. Adjust other properties individually

**⚠️ Note**: If you modify `color` with Sync enabled, it synchronizes to all Series, overriding Palette's color assignment.

---

## 8. Recommendations

### 8.1 Palette Selection Principles

| Scenario | Recommended Palette |
|----------|---------------------|
| Business reports | Tableau 10, Google Charts |
| Tech style | Modern Blue |
| Environmental/ocean | Cool Ocean |
| Events/promotions | Warm Sunset |
| General purpose | D3 Category |

### 8.2 Insufficient Colors

If Series count > palette color set count:

- **Option 1**: Edit palette, add more color sets
- **Option 2**: Combine multiple similar palettes
- **Option 3**: Manually adjust colors after cycling

### 8.3 Brand Color Scheme

Create company brand palette:

1. Open `SeriesColorPalettes.asset`
2. Add new palette, name it "Company Brand"
3. Use company's brand colors as `baseColor`
4. Let `areaColor` and `highlightColor` auto-generate

---

## 9. FAQ

### Q: Colors don't change after applying palette?

Check:
1. Is the correct `ChartProfile` selected?
2. Does the chart have Series? (No effect on empty charts)
3. Try clicking refresh in Preview panel

### Q: How to modify only one Serie's color?

1. **Don't click palette** (applies to all)
2. Select that Serie in Series list
3. Directly modify `color` property in Inspector
4. Ensure Sync toggle is OFF

### Q: Does palette affect existing color settings?

**Yes, it overrides**. Clicking palette reassigns all Series colors. To preserve certain Series colors:

1. Record current colors
2. Apply palette
3. Manually restore specific Series colors

### Q: How to export/import custom palettes?

Palettes are stored in `SeriesColorPalettes.asset`:

- **Export**: Copy file to send to others
- **Import**: Place file in `Assets/EasyChart/Editor/` directory

### Q: Can gradients be set via palette?

**Not directly**. Palette sets base colors (`baseColor`).

For gradients:
1. Use Palette for base colors
2. Manually enable `gradient` in Inspector
3. Configure gradient colors

---

## 10. Shortcuts

| Action | Method |
|--------|--------|
| Open palette selector | Click palette icon |
| Apply color scheme | Click palette item |
| Open palette manager | Click Manage Palettes |

---

## Next Chapter

- `02_09-TextureFXLayers.md`: TextureFX Layers editing guide (Pro)
