# Texture FX Layers (Pro)

This chapter explains the TextureFXLayers feature in EasyChart Pro, which provides advanced texture effects and animations for chart elements.

---

## 1. Overview

TextureFXLayers is a Pro feature that allows you to apply animated texture effects to chart elements such as:

- **Line strokes** (Line, Line3D)
- **Area fills** (Line area)
- **Point markers** (Line, Scatter, Line3D)
- **Bar fills** (Bar)
- **Radar fills** (Radar)

Each element can have multiple texture layers with independent animation settings.

---

## 2. TextureFXLayer Properties

### 2.1 Base Texture Settings (`fill`)

- **Texture**: The texture to apply
- **Color**: Tint color for the texture
- **Tiling**: UV tiling (scale)
- **Offset**: UV offset

### 2.2 Animation Settings

#### Animation Type (`animationType`)

- **None**: No UV animation
- **TextureUvMove**: Continuously move the texture UV coordinates
- **TextureScale**: Animate the texture scale (zoom in/out)

#### UV Move Settings (when `animationType = TextureUvMove`)

- **uvMoveSpeed**: Speed of UV movement (default: `(1, 0)` - moves right)
  - Positive X: texture moves right
  - Positive Y: texture moves up

#### Scale Settings (when `animationType = TextureScale`)

- **scaleType**: Type of scale animation
  - `ZoomIn`: Scale from `scaleFrom` to `scaleTo` repeatedly
  - `ZoomOut`: Scale from `scaleTo` to `scaleFrom` repeatedly
  - `Sin`: Sinusoidal oscillation between `scaleFrom` and `scaleTo`
  - `PingPong`: Ping-pong between `scaleFrom` and `scaleTo`
- **scaleSpeed**: Speed of scale animation
- **scaleFrom**: Starting scale (default: `(1, 1)`)
- **scaleTo**: Ending scale (default: `(1.2, 1.2)`)

### 2.3 Color Animation Settings

Color animation can be combined with UV/Scale animation.

- **colorAnimationType**: Type of color animation (default: `PingPong`)
  - `None`: No color animation
  - `Loop`: Loop through gradient from start to end
  - `PingPong`: Ping-pong through gradient
  - `Clamp`: Play once and hold at end
- **colorAnimationSpeed**: Speed of color animation (default: `1`)
- **colorAnimationGradient**: Gradient defining color over time
  - Default: White with alpha fading from 255 to 0

### 2.4 Fade Effect Settings

- **fadeType**: Type of fade effect
  - `None`: No fade
  - `Edge`: Fade at all edges
  - `Center`: Fade at center (transparent center)
  - `Radial`: Radial vignette (fade at edges)
  - `DirectionHorizontal`: Horizontal gradient fade
  - `DirectionVertical`: Vertical gradient fade
- **fadeIntensity**: Intensity of fade effect (0-1)
- **fadeSoftness**: Softness of fade transition (0-1)

### 2.5 Deform Effect Settings

- **deformType**: Type of UV deformation
  - `None`: No deformation
  - `Wave`: Sinusoidal wave distortion
  - `Rotate`: UV rotation around center
  - `Pulse`: Radial expansion from center
- **deformIntensity**: Intensity of deformation
- **deformSpeed**: Speed of deformation animation
- **waveFrequency**: Frequency for wave deform (default: `4`)

---

## 3. Supported Chart Types

### 3.1 Line Chart (2D)

TextureFXLayers can be applied to:

- `LineSettings.stroke.textureFXLayers` - Line stroke
- `LineSettings.area.textureFXLayers` - Area fill under line
- `LineSettings.point.textureFXLayers` - Point markers

### 3.2 Bar Chart (2D)

- `BarSettings.textureFXLayers` - Bar fill

### 3.3 Scatter Chart (2D)

- `ScatterSettings.point.textureFXLayers` - Point markers

### 3.4 Radar Chart (2D)

- `RadarSettings.stroke.textureFXLayers` - Radar stroke
- `RadarSettings.area.textureFXLayers` - Radar area fill
- `RadarSettings.point.textureFXLayers` - Point markers

### 3.5 Line3D Chart (Pro)

- `Line3DSettings.stroke.textureFXLayers` - 3D line stroke
- `Line3DSettings.point.textureFXLayers` - 3D point markers

### 3.6 Not Supported (Currently)

The following chart types do not currently support TextureFXLayers:

- **Pie Chart** - Uses different rendering approach
- **Ring Chart** - Uses different rendering approach
- **Heatmap** - Uses color interpolation instead

---

## 4. Editor Workflow

### 4.1 Adding TextureFXLayers

1. Select a Series in the Series panel
2. Expand the settings foldout (e.g., "Line", "Point", "Area")
3. Find the `Texture FX Layers` property
4. Click "+" to add a new layer
5. Configure the layer properties

### 4.2 Copy/Paste Support

TextureFXLayers support copy/paste functionality:

- Right-click on a layer to copy
- Right-click on another layer to paste
- Gradient properties are fully preserved during copy/paste

---

## 5. Default Values

When creating a new TextureFXLayer, the following defaults are applied:

| Property | Default Value |
|----------|---------------|
| `uvMoveSpeed` | `(1, 0)` |
| `colorAnimationType` | `PingPong` |
| `colorAnimationGradient` | White, alpha 255→0 |
| `scaleFrom` | `(1, 1)` |
| `scaleTo` | `(1.2, 1.2)` |

---

## 6. Performance Considerations

- TextureFXLayers with animation enabled will trigger continuous redraws
- Use sparingly on mobile devices
- Consider disabling animations when charts are not visible
- Multiple layers stack and may impact performance

---

## 7. Examples

### 7.1 Flowing Water Effect (Line)

```
animationType: TextureUvMove
uvMoveSpeed: (0.5, 0)
```

### 7.2 Pulsing Glow Effect (Point)

```
colorAnimationType: PingPong
colorAnimationSpeed: 2
colorAnimationGradient: White (alpha 1→0.3→1)
```

### 7.3 Breathing Scale Effect (Bar)

```
animationType: TextureScale
scaleType: Sin
scaleSpeed: 1
scaleFrom: (1, 1)
scaleTo: (1.1, 1.1)
```

---

## 8. Further Reading

- [Line Chart](./03_01-LineChart.md)
- [Bar Chart](./03_02-BarChart.md)
- [Common Recipes](./04_08-CommonRecipes.md)
