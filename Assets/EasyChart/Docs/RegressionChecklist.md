# EasyChart Regression Checklist (Minimal)

## Build
1. Open Unity and ensure no compile errors.

## Editor (Edit Mode)
### Coordinate System Switch
1. Toggle `ChartData.CoordinateSystem` between Cartesian2D and other modes.
2. Confirm no UI Toolkit assertion spam in Console.
3. Confirm the chart re-renders correctly after the deferred apply.

### Legend
1. Toggle series visibility from legend.
2. For Pie with aggregation (Others): toggle slices and confirm redraw + legend refresh.

### Tooltip
1. Hover points/bars/slices and confirm tooltip appears.
2. Move mouse out: tooltip disappears and hover states clear.

### Category Auto Scroll
1. Enable category axis `categoryAutoScroll`.
2. Toggle `categorySmoothScroll` on/off.
3. Confirm window step transition has no flicker.

## Play Mode
### Animation
1. Set `ChartData.animationDuration > 0`.
2. Call `PlayAnimation()` (or trigger via refresh) and confirm renderers animate.

### Scrolling + Tooltip Interaction
1. During smooth scrolling: tooltip should hide (no stale hover).
2. After scroll stops: tooltip works again.

## Performance Spot Check
1. Hover/tooltip: watch GC Alloc in Profiler; should not allocate every frame while moving mouse.
2. Auto-scroll + smooth: no large spikes per tick (baseline stable).
