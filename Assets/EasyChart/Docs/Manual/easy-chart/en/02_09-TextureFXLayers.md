# TextureFX Layers Editing (Pro)

This chapter explains how to edit **TextureFX Layers** to add advanced visual effects to charts. TextureFX Layers can be configured in the Series panel, Inspector panel, and other locations.

**⚠️ Note**: TextureFX Layers is a **Pro version** feature.

---

## 1. TextureFX Layers Location

TextureFX Layers properties are located in different chart type settings:

### 1.1 Applicable Locations

| Chart Type | Setting Path |
|-----------|-------------|
| **Line** | Line Settings → `textureFXLayers` |
| **Line (Area)** | Line Settings → Area → `textureFXLayers` |
| **Line (Point)** | Line Settings → Point → `textureFXLayers` |
| **Bar** | Bar Settings → `textureFXLayers` |
| **Scatter** | Scatter Settings → Point → `textureFXLayers` |
| **Radar** | Radar Settings → Line/Area/Point → `textureFXLayers` |
| **Line3D** | Line3D Settings → Line/Point → `textureFXLayers` (Pro) |

### 1.2 How to Find

1. Select `ChartProfile` in Library panel
2. Select a Serie in Series panel
3. Expand corresponding settings foldout in Inspector
4. Find `Texture FX Layers` property

---

## 2. TextureFX Layer List Interface

### 2.1 List View

`Texture FX Layers` displays as a collapsible list:

```
Texture FX Layers
├── [0] Layer 0  ▼
├── [1] Layer 1  ▶
└── + (Add)
```

- Click `+` to add new layer
- Click layer name to expand/collapse detailed settings
- Right-click layer to show context menu

### 2.2 Layer Operations

Right-click a layer to show menu:

| Option | Function |
|--------|----------|
| **Copy** | Copy all settings of this layer |
| **Paste** | Paste to current layer (overwrite) |
| **Duplicate** | Duplicate this layer and create new |
| **Delete** | Delete this layer |
| **Move Up** | Move up |
| **Move Down** | Move down |

**Layer Order**: Upper layers overlay lower layers (Layer 1 renders above Layer 0).

---

## 3. Layer Detailed Settings

Expand a Layer to show these setting groups:

### 3.1 Base Texture (Fill)

| Property | Description | Default |
|----------|-------------|---------|
| `Texture` | Texture asset | None |
| `Color` | Texture tint color | White |
| `Tiling` | UV tiling (scale) | (1, 1) |
| `Offset` | UV offset | (0, 0) |

**Tiling Examples**:
- `(1, 1)` - Normal display
- `(2, 1)` - Repeat horizontally 2x
- `(1, 0.5)` - Vertical compression

### 3.2 Animation Type

#### Animation Type

| Option | Effect |
|--------|--------|
| **None** | Static texture, no animation |
| **Texture UV Move** | Continuous texture movement (flow effect) |
| **Texture Scale** | Texture scale animation (breathing effect) |

#### When Animation Type = Texture UV Move

| Property | Description | Default |
|----------|-------------|---------|
| `UV Move Speed` | UV movement speed | (1, 0) |

- X > 0: Texture moves right
- X < 0: Texture moves left
- Y > 0: Texture moves up
- Y < 0: Texture moves down

**Common Settings**:
- Horizontal flow: `(0.5, 0)`
- Vertical flow: `(0, 0.5)`
- Diagonal: `(0.3, 0.3)`

#### When Animation Type = Texture Scale

| Property | Description | Default |
|----------|-------------|---------|
| `Scale Type` | Scale mode | `Sin` |
| `Scale Speed` | Scale speed | `1` |
| `Scale From` | Start scale | `(1, 1)` |
| `Scale To` | End scale | `(1.2, 1.2)` |

**Scale Type Options**:

| Type | Effect |
|------|--------|
| `Zoom In` | Scale from small to large, loop |
| `Zoom Out` | Scale from large to small, loop |
| `Sin` | Smooth sine wave scaling |
| `PingPong` | Scale up→down→up |

### 3.3 Color Animation

Color animation can be used simultaneously with UV/scale animation.

| Property | Description | Default |
|----------|-------------|---------|
| `Color Animation Type` | Color animation mode | `PingPong` |
| `Color Animation Speed` | Animation speed | `1` |
| `Color Animation Gradient` | Color gradient definition | White→Transparent |

**Color Animation Type**:

| Type | Effect |
|------|--------|
| `None` | No color animation |
| `Loop` | Loop from start to end |
| `PingPong` | Back-and-forth gradient |
| `Clamp` | Play once and hold |

**Gradient Settings**:
- Click Gradient field to open editor
- Add/remove color keypoints
- Adjust time position and transparency

### 3.4 Fade

| Property | Description | Default |
|----------|-------------|---------|
| `Fade Type` | Fade type | `None` |
| `Fade Intensity` | Intensity (0-1) | `0.5` |
| `Fade Softness` | Softness (0-1) | `0.5` |

**Fade Type Options**:

| Type | Effect |
|------|--------|
| `None` | No fade |
| `Edge` | Edge fade |
| `Center` | Center transparent, edges visible |
| `Radial` | Radial vignette |
| `Direction Horizontal` | Horizontal gradient |
| `Direction Vertical` | Vertical gradient |

### 3.5 Deform Effects

| Property | Description | Default |
|----------|-------------|---------|
| `Deform Type` | Deformation type | `None` |
| `Deform Intensity` | Deformation intensity | `0.1` |
| `Deform Speed` | Animation speed | `1` |
| `Wave Frequency` | Wave frequency | `4` |

**Deform Type Options**:

| Type | Effect |
|------|--------|
| `None` | No deformation |
| `Wave` | Sine wave distortion |
| `Rotate` | UV rotation |
| `Pulse` | Radial pulse expansion |

---

## 4. Common Effect Configurations

### 4.1 Flow Effect (Lines)

For data lines, progress bars:

```
Animation Type: Texture UV Move
UV Move Speed: (0.5, 0)
Texture: Stripe or arrow texture
```

### 4.2 Pulse Glow (Point Markers)

For scatter plots, data point highlights:

```
Animation Type: Texture Scale
Scale Type: Sin
Scale Speed: 2
Scale From: (1, 1)
Scale To: (1.3, 1.3)

Color Animation Type: PingPong
Color Animation Speed: 2
Color Animation Gradient: White (alpha 1 → 0.3 → 1)
```

### 4.3 Breathing Bar Chart

For Bar charts:

```
Animation Type: Texture Scale
Scale Type: Sin
Scale Speed: 1
Scale From: (1, 1)
Scale To: (1.05, 1.05)

Texture: Gradient or grid texture
```

### 4.4 Flowing Area Fill

For line chart areas:

```
Animation Type: Texture UV Move
UV Move Speed: (0.2, 0.1)
Fade Type: Edge
Fade Intensity: 0.3

Texture: Noise or flow texture
```

### 4.5 Wavy Lines

For decorative lines:

```
Deform Type: Wave
Deform Intensity: 0.05
Deform Speed: 1.5
Wave Frequency: 6

Animation Type: Texture UV Move
UV Move Speed: (0.3, 0)
```

---

## 5. Layer Combinations

Multiple Layers can be stacked for complex effects:

### Example: Glow + Flow

**Layer 0 (Base)**: Glow effect
```
Texture: Circular gradient texture
Color: Blue
Animation Type: Texture Scale
Scale Type: Sin
Scale From: (1, 1)
Scale To: (1.5, 1.5)
```

**Layer 1 (Overlay)**: Flow effect
```
Texture: Stripe texture
Color: White
Animation Type: Texture UV Move
UV Move Speed: (1, 0)
Fade Type: Edge
```

Effect: White stripes continuously flow on blue glowing background.

### Layer Order Principles

- **Layer 0**: Base layer, recommended for static or slow animation
- **Layer 1+**: Overlay layers, can contain dynamic effects
- **Transparent textures**: Upper layers need transparency to see lower layers

---

## 6. Performance Considerations

### 6.1 Animation Overhead

Enabled animation Layers cause charts to **redraw every frame**:

| Case | Performance Impact |
|------|-------------------|
| No animation Layer | Normal rendering, no extra overhead |
| 1 animation Layer | Minor overhead |
| 3+ animation Layers | Noticeable overhead |
| Chart not visible | Animation auto-pauses |

### 6.2 Optimization Tips

1. **Limit animation layers**: Maximum 2-3 animated layers
2. **Static alternative**: If no animation needed, set Animation Type to None
3. **Visibility detection**: When chart is off-screen, animation auto-pauses
4. **Mobile devices**: Use cautiously, recommend static Layers
5. **Complex charts**: Reduce animation layers with many Series

### 6.3 Texture Resources

- Use **small textures** (256x256 or 512x512)
- Enable **Mipmaps** for better scaling quality
- Use **Repeat** wrap mode (handled automatically)

---

## 7. Copy/Paste Workflow

### 7.1 Copy Layer Settings

1. Configure all properties of a Layer
2. Right-click that Layer
3. Select **Copy**
4. Switch to another Serie
5. Find Texture FX Layers, right-click Layer
6. Select **Paste**

### 7.2 Cross-Chart-Type Copy

TextureFX Layers settings **can be copied between chart types**:

- Line Layer → Paste to Bar
- Bar Layer → Paste to Scatter
- Note: Certain specific settings may not apply

### 7.3 Batch Application

If multiple Series need the same effect:

1. Configure TextureFX Layers for one Serie
2. **Enable Sync** (Series panel toolbar top)
3. Copy Layer
4. Paste to other same-type Series
5. **Sync will auto-synchronize** all Layer settings

---

## 8. Preview and Debug

### 8.1 Real-time Preview

All TextureFX modifications **instantly reflect in Preview panel**:

- Parameter adjustments show effect immediately
- Animation effects auto-play in Preview
- Can pause/play animation to examine details

### 8.2 Debug Layers

If effect doesn't meet expectations:

1. **Check layer order**: Upper layers obscure lower layers
2. **Check transparency**: Transparent textures needed for overlay effects
3. **Simplify testing**: Leave only one Layer first, confirm base effect
4. **Check texture**: Ensure texture is properly assigned, not None

---

## 9. Charts Not Supporting TextureFX

These chart types **do not support** TextureFX Layers:

| Chart Type | Reason |
|-----------|--------|
| **Pie** | Uses fan-shaped rendering |
| **Ring** | Uses ring-shaped rendering |
| **Heatmap** | Uses color interpolation instead of textures |
| **Gauge** | Uses custom rendering |

---

## 10. FAQ

### Q: Why doesn't animation play?

Check:
1. **Animation Type** is not `None`
2. **Speed** is not 0
3. Preview panel is visible
4. Chart is not hidden (`visible` property)

### Q: Can't see lower layers after multi-layer overlay?

- Upper texture needs **alpha channel (transparency)**
- Check upper layer `Color` transparency (A value)
- Use `Fade Type` to create gradient transparency

### Q: Texture displays as solid color block?

- Texture may be **unassigned** (shows as white)
- Check texture's **Wrap Mode** is Repeat
- Check **Tiling** setting is reasonable

### Q: How to create seamless loop animation?

- Use **UV Move** animation
- Choose **seamless texture**
- Set appropriate **Tiling** values

### Q: Does TextureFX affect data updates?

**No**. TextureFX is purely visual effects, doesn't affect data injection or chart refresh.

---

## 11. Further Reading

- `03_01-LineChart.md`: Complete line chart guide
- `03_02-BarChart.md`: Complete bar chart guide
- `03_08-TextureFXLayers.md`: TextureFX feature overview
- `04_08-CommonRecipes.md`: Common effect recipes

---

## Next Chapter

- `03_01-LineChart.md`: Complete line chart configuration guide
