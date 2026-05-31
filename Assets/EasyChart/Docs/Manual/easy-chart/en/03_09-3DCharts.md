# 3D Charts (Pro)

This chapter explains the 3D chart types available in EasyChart Pro, including Line3D, Bar3D, Scatter3D, and Pie3D.

---

## 1. Overview

3D charts provide depth and perspective to your data visualization. They are rendered using Unity's 3D rendering pipeline and support:

- Interactive camera controls (rotation, zoom)
- Auto-rotation animation
- Hover effects
- TextureFXLayers for advanced effects

**Supported 3D Chart Types:**
- **Line3D**: 3D line/trajectory visualization
- **Bar3D**: 3D bar chart with depth
- **Scatter3D**: 3D scatter plot
- **Pie3D**: 3D pie chart with perspective

---

## 2. Coordinate System

3D charts use `CoordinateSystemType.Cartesian3D` which provides:

- **X Axis**: Horizontal (left-right)
- **Y Axis**: Vertical (height/value)
- **Z Axis**: Depth (front-back)

### 2.1 Cartesian3D Grid Settings

Configure the 3D grid in the Inspector panel:

- **show**: Show/hide the grid
- **gridColor**: Color of grid lines
- **gridLineWidth**: Width of grid lines
- **gridWidth/Height/Depth**: Dimensions of the 3D space
- **showXYPlane/XZPlane/YZPlane**: Toggle individual planes
- **useAxisColors**: Use different colors for each axis
- **showLabels**: Show axis labels
- **labelColor/fontSize**: Label styling

---

## 3. Line3D Chart

### 3.1 Data Mapping

- **X**: Position along X axis (category index or value)
- **Y (value)**: Height/value
- **Z**: Depth position

### 3.2 Line3DSettings

#### Stroke Settings (`stroke`)
- **lineType**: `Straight`, `Smooth`, or `Step`
- **width**: Line width (world units)
- **color**: Line color
- **textureFill**: Basic texture settings
- **textureFXLayers**: Advanced texture effects (Pro)

#### Point Settings (`point`)
- **show**: Show point markers
- **size**: Point size (world units)
- **texture**: Custom point texture
- **color**: Point color
- **textureFXLayers**: Advanced texture effects (Pro)

#### Camera Settings (`camera`)
- **yawDeg**: Horizontal rotation angle
- **pitchDeg**: Vertical rotation angle
- **distance**: Camera distance from target
- **fov**: Field of view
- **target**: Camera look-at target
- **autoRotate**: Enable auto-rotation
- **autoRotateSpeed**: Rotation speed (degrees/second)

#### Hover Settings (`hover`)
- **enabled**: Enable hover detection
- **highlightIntensity**: Brightness increase on hover
- **scaleMultiplier**: Size increase on hover

#### Background Settings (`background`)
- **transparent**: Use transparent background
- **color**: Background color (if not transparent)

---

## 4. Bar3D Chart

### 4.1 Data Mapping

- **X**: Category position
- **Y (value)**: Bar height
- **Z**: Depth position (for multiple rows)

### 4.2 Bar3DSettings

Similar structure to Line3D with:
- Bar-specific rendering settings
- Camera and hover settings
- TextureFXLayers support

---

## 5. Scatter3D Chart

### 5.1 Data Mapping

- **X**: X position
- **Y (value)**: Y position (height)
- **Z**: Z position (depth)

### 5.2 Scatter3DSettings

- Point rendering settings
- Size mapping options
- Camera and hover settings

---

## 6. Pie3D Chart

### 6.1 Data Mapping

- **value**: Slice size
- **name**: Slice label

### 6.2 Pie3DSettings

- Slice rendering settings
- Explosion/separation settings
- Camera settings
- Label positioning

---

## 7. Common 3D Features

### 7.1 Camera Auto-Rotation

Enable continuous rotation for showcase displays:

```
camera.autoRotate = true
camera.autoRotateSpeed = 30  // degrees per second
```

### 7.2 Hover Detection

3D charts support hover detection using raycasting:

1. Enable `hover.enabled`
2. Points/bars will highlight on mouse hover
3. Customize with `highlightIntensity` and `scaleMultiplier`

### 7.3 TextureFXLayers

3D charts support TextureFXLayers for:
- UV animation on lines
- Color animation on points
- See [Texture FX Layers](./03_08-TextureFXLayers.md) for details

---

## 8. Performance Considerations

- 3D charts render to a RenderTexture
- Auto-rotation triggers continuous redraws
- Hover detection uses physics raycasting
- Consider disabling features when not needed

---

## 9. Example Setup

### Line3D Quick Start

1. Create a new ChartProfile
2. Set `coordinateSystem = Cartesian3D`
3. Add a Series with `type = Line3D`
4. Add data points with X, value (Y), and Z coordinates
5. Configure camera settings for desired view angle

### Sample Data

```json
{
  "series": [{
    "type": "Line3D",
    "seriesData": [
      {"x": 0, "value": 10, "z": 0},
      {"x": 1, "value": 25, "z": 0.5},
      {"x": 2, "value": 15, "z": 1},
      {"x": 3, "value": 30, "z": 1.5}
    ]
  }]
}
```

---

## 10. Further Reading

- [Texture FX Layers](./03_08-TextureFXLayers.md)
- [Common Recipes](./04_08-CommonRecipes.md)
- [FAQ](./04_09-FAQ.md)
