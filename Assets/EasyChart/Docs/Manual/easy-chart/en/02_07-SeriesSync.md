# Series Panel - Sync Feature

This chapter explains the **Sync** toggle in the Series panel toolbar, used to quickly synchronize property settings across multiple Series.

---

## 1. Sync Toggle Location

In the Library Editor's **Series panel** toolbar, from left to right:

1. **Palette Icon** (colored circle) - Open color scheme selector
2. **Sync Toggle** (sync icon) - Enable/disable property synchronization
3. **Add Serie Button** (+) - Add new Series

The Sync toggle has two states:

- **OFF** (gray/yellow): Changes affect only the currently selected Serie
- **ON** (green): Changes automatically sync to all **same-type** Series

---

## 2. Enabling Sync

### Steps

1. Select a `ChartProfile` in the Library panel
2. Ensure the Profile has **multiple Series of the same type** (e.g., 3 Lines)
3. Find the Sync toggle in the Series panel toolbar
4. Click the toggle; it turns **green** when enabled

---

## 3. Using Sync to Synchronize Properties

### Example: Uniform Line Width

Suppose you have 3 Line series:

1. **Enable Sync** (toggle turns green)
2. Click any Line in the Series list
3. Find `Line Settings` → `lineWidth` in the Inspector panel
4. Change value from `2` to `5`
5. Observe the Preview panel - all Lines' width changes to 5

### Example: Uniform Animation Settings

1. **Enable Sync**
2. Select any Bar
3. Change `animationDuration` to `0.5`
4. Change `animationEasing` to `EaseInOut`
5. All Bar series' animation settings update simultaneously

---

## 4. Synchronization Rules

### 4.1 Type Matching

Sync **only synchronizes same-type** Series:

| Modified Serie Type | Affected Series |
|--------------------|-----------------|
| Line | All Lines |
| Bar | All Bars |
| Scatter | All Scatters |

**Mixed Chart Example**:
- Chart has Line + Bar + Line
- Modify first Line → only affects second Line
- Modify Bar → doesn't affect any Line

### 4.2 Excluded Properties

These properties **are NOT synchronized** (need independent setting):

| Property | Description |
|----------|-------------|
| `name` | Series name |
| `id` | Series unique identifier |
| `type` | Series type (Line/Bar/Scatter, etc.) |
| `data` / `seriesData` | Data point values |

### 4.3 Supported Property Categories

**General Properties**:
- `visible` - Visibility
- `showLabel` - Show labels
- `labelSettings` - Label settings

**Style Properties**:
- `color` / `gradient` / `opacity` - Color related

**Animation Properties**:
- `animationDuration` - Animation duration
- `animationEasing` - Easing function
- `animationDelay` - Delay

**Line Specific**:
- `lineWidth` / `lineStyle` / `smooth` / `showArea`

**Bar Specific**:
- `barWidth` / `barGap` / `stacked` / `stackGroup`

**Scatter Specific**:
- `pointSize` / `pointShape`

**TextureFX**:
- `textureFXLayers` - Texture FX layers (Pro)

---

## 5. Disabling Sync

When you need to **modify individually**:

1. Click the Sync toggle to turn it **gray/yellow** (OFF)
2. Select the Serie to modify
3. Change properties - affects only the selected Serie

---

## 6. Auto-Disable Mechanism

Sync automatically disables in these situations:

- **Switching Profile**: When selecting a different ChartProfile
- **Deleting Serie**: When deleting the last Serie of a certain type
- **Reimporting Library**: When refreshing Library data

This prevents accidental synchronization to wrong charts.

---

## 7. Recommendations

### Recommended Workflow

1. **Initial Setup**: Use Sync for uniform base styling
   - Enable Sync
   - Set line width, color, animation, etc.

2. **Personalization**: Disable Sync for fine-tuning
   - Disable Sync
   - Adjust specific Series' properties individually

3. **Data Setup**: Always set independently
   - Data (`data`) is never synchronized, needs per-Series setting

### Best Practices

| Scenario | Use Sync? | Reason |
|----------|-----------|--------|
| Uniform chart style | ✅ Enable | Maintain visual consistency |
| Set company brand colors | ✅ Enable | All series use same colors |
| Highlight specific line | ❌ Disable | Avoid affecting others |
| Set different data | ❌ Disable (automatic) | Data itself not synchronized |

---

## 8. Undo Support

All Sync operations support Unity's Undo:

- Press `Ctrl+Z` to undo synchronization
- Press `Ctrl+Y` to redo

Undo immediately if results are unexpected.

---

## 9. FAQ

### Q: Why don't other Series change after modification?

Check:

1. **Is Sync enabled?** (should be green)
2. **Are types the same?** (Line modification doesn't affect Bar)
3. **Is property in exclusion list?** (name/id/type/data not synced)
4. **Are there other same-type Series?**

### Q: Can Sync synchronize to different-type Series?

**No**. Sync is designed to only synchronize same-type Series. Different chart types (Line vs Bar) have different property structures and cannot be directly synchronized.

### Q: Does Sync affect Palette-applied colors?

**Yes**. If you modify `color` with Sync enabled, it synchronizes to all same-type Series, overriding Palette colors.

**Suggestion**:
1. Use Palette first for initial colors
2. Disable Sync before adjusting specific Series colors

### Q: How do I know which properties are synchronized?

Except for `name`, `id`, `type`, `data`, all other properties are synchronized.

### Q: Are sub-properties (e.g., labelSettings.fontSize) synchronized?

**Yes**. When the parent property object (`labelSettings`) is modified, all its sub-properties are synchronized.

---

## 10. Shortcuts

| Action | Shortcut |
|--------|----------|
| Toggle Sync | Click toggle |
| Undo sync | `Ctrl+Z` |
| Redo | `Ctrl+Y` |

---

## Next Chapter

- `02_08-ColorPalette.md`: Color palette feature for quick color scheme application
