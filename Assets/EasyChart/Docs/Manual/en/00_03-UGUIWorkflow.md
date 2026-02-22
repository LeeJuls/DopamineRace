# UGUI Workflow

Goal of this chapter: use EasyChart with a UGUI workflow (Canvas/RectTransform), and understand the choice between two rendering modes:

- `ScreenSpaceOverlay`: best visual quality (no RenderTexture), but typically visible only in the Game view
- `WorldSpace`: visible in both Scene/Game views (uses RenderTexture), suitable for 3D world-space UI

---

## 1. Recommended approach: UGUIChartBridge

Recommended component: `UGUIChartBridge`.

Its role is:

- Still uses **UI Toolkit `ChartElement`** as the core chart renderer
- Bridges the chart so it "fits" onto a target UGUI `RectTransform`

So you get the best of both:

- Chart capability + UI Toolkit rendering
- UGUI scene/prefab layout workflow and habits

---

## 2. Common prerequisites

No matter which mode you choose, prepare the following first:

- A `ChartProfile` to display (recommended: clone one from `EasyChart/Library Editor` and modify it)
- A `PanelSettings` asset, and assign it to `UGUIChartBridge` via `Panel Settings Asset`

> Note: providing `Panel Settings Asset` is usually better for font rendering and overall stability.

---

## 3. Screen Space Overlay (recommended for HUD/panels)

### Use cases

- HUD, UI panels, dialogs
- You care most about clarity and visual quality

### Characteristics

- No RenderTexture
- Typically visible only in the Game view

### Setup steps (overview)

1. Create a `Canvas`
2. Under the Canvas, create a node with `RectTransform` (`Image` or an empty GameObject both work)
3. Add `UGUIChartBridge`
4. Configure:
   - `Profile`
   - `Panel Settings Asset`
   - `Render Mode = ScreenSpaceOverlay`
   - `Sort Order` (controls overlay order; effective only in Screen Space Overlay mode)

Key points:

- This mode renders the chart inside a runtime-created/reused `UIDocument`.
- If the chart is covered by other UI, increase `Sort Order` first.

---

## 4. World Space (recommended for 3D world-space panels)

### Use cases

- Billboards/screens/panels inside a 3D world
- You want to see the result in the Scene view as well

### Characteristics

- Uses RenderTexture
- Usually visible in both Scene and Game views
- Visual quality can be affected by RenderTexture resolution

### Setup steps (overview)

1. Create a `Canvas`
2. Set `Render Mode = World Space`
3. Under the Canvas, create a node with `RectTransform` (recommended: `RawImage`)
4. Add `UGUIChartBridge`
5. Configure:
   - `Profile`
   - `Panel Settings Asset`
   - `Render Mode = WorldSpace`

Key points:

- World Space mode creates and maintains a `RenderTexture` and displays it via `RawImage`.
- Clarity is strongly tied to the `RenderTexture` resolution, which usually comes from the target `RectTransform` width/height.
  - If the chart looks blurry, make the target `RectTransform` larger first (e.g. 600x400+).

---

## 5. Mode selection (quick conclusion)

- Prefer **ScreenSpaceOverlay** when:
  - you're building traditional UI (HUD/panels)
  - clarity is your top priority

- Prefer **WorldSpace** when:
  - your chart needs to appear in a 3D world
  - you want it visible in the Scene view

---

## 6. Common issues & troubleshooting

- **Not visible at runtime**
  - Make sure the target `RectTransform` size is not 0
  - Make sure `Profile` is assigned, and the Profile renders correctly in Library Editor Preview
  - If fonts look wrong, check whether `Panel Settings Asset` is missing

- **Blurry chart in World Space mode**
  - Increase the target `RectTransform` size (this increases RenderTexture resolution)
  - Avoid frequent aggressive scaling at runtime (may trigger RenderTexture resizing)

---

## 7. Alternative: export as a UGUI Prefab

If your version provides `Export UGUI Prefab`:

- You can export the Profile to a UGUI prefab and use it directly under a Canvas
- Coverage for interaction/compatibility depends on the exporter version
