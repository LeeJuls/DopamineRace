# Roadmap / Update Plan

This chapter describes the overall direction and planned items for future EasyChart updates.

- This file is **not split by version phases** (more detailed planning can be added inside each chart type chapter later)
- This file is organized by "capability categories" (you can treat it as a roadmap table of contents)

## Free / Pro split (release strategy)

### Free (standalone package)

- Positioning: cover the most common AssetStore use cases, forming a full loop of "configurable + previewable + runtime data injection".
- Includes: existing basic 2D chart types, runtime injection (`ChartFeed` / `ApplyJson`), editor workflow such as `EasyChartLibraryWindow`.

### Pro (standalone package, includes all Free)

- Positioning: provide clear differentiated capabilities and a long-term expansion path on top of Free.
- Includes: everything in Free + Pro incremental features (advanced effects / new chart types / 3D / tooling, etc.).

### Compatibility strategy

- When Free encounters Pro-only assets/configurations: it is allowed to fail with a direct error (no downgrade compatibility required).

### Pro initial selling points priority

- A: new chart types
- B: 3D charts
- C: advanced 2D visual effects (e.g. texture UV animation, special rendering effects)

---

## Appendix: version plan (tentative timeline)

> Note: the following is a tentative monthly/quarterly cadence. Specific dates can be refined after team capacity and release window are confirmed.

### 2026 Q1 (Jan–Mar): stabilization + complete Free loop

- 2026-01 (Free v1.0.x):
  - Fix/finalize: stability of JSON Injection panel and example generation/parsing (based on current TODO)
  - Docs & samples: align with the latest data structures and panel capabilities
- 2026-02 (Free v1.1.0):
  - 2D UX improvements (Free scope): Bar rounded corners, hover effects (if not implemented yet, land in this version)
  - Editor UX: small workflow improvements in LibraryWindow (no Pro dependencies)
- 2026-03 (Free v1.1.x):
  - Regression fixes + performance/GC optimization (large data refresh, tooltip/interaction stability)

### 2026 Q2 (Apr–Jun): Pro v1.0 (new chart types first)

Each series type adds an animation component collection, allowing effects such as Point blinking and TextureFill UV animation.
- 2026-04 (Pro v1.0.0):
  - Finalize Pro package structure and release workflow (Pro includes all Free)
  - New chart types (batch 1): Gauge / Funnel (one or both depending on complexity)
- 2026-05 (Pro v1.0.x):
  - New chart types (batch 2): BoxPlot / Candlestick (implement one with higher priority)
  - Improve Pro-only error hints and readable Editor-side error messages
- 2026-06 (Free v1.2.0 + Pro v1.1.0):
  - Free: continue filling common 2D capabilities and stability
  - Pro: expand new chart types (Treemap / Sunburst research or first version)

### 2026 H2 (Jul–Dec): 3D roadmap and effects

- 2026 Q3 (Jul–Sep) (Pro v2.0 or v1.2+):
  - 3D charts (batch 1): 3D Bar / 3D Scatter (prioritize one to complete an end-to-end workflow)
  - 3D rendering pipeline and interaction foundations (iterate by minimum viable slices)
- 2026 Q4 (Oct–Dec):
  - 3D Surface (research/experimental)
  - Advanced 2D visual effects (Pro): texture UV animation (and more complex effects later)
  - Tooling improvements: Theme / direct networking / automated tests (pick one as the main quality track)

---

## 1. Chart type expansion plan (Chart Types)

### 1.1 2D charts (enhancements to existing system)

- Goal: without adding too many `SerieType`, fill common expressions via settings/variants.
- Candidate directions (examples):
  - Line: more line types/fills/annotations (richer markers/threshold lines, etc.), texture UV animation (Pro)
  - Bar: more stacking modes, percent stacking, waterfall modes, rounded bar caps (Free), hover effects (Free), texture UV animation (Pro)
  - Scatter: more mapping dimensions (size/color), density expressions (aggregation/gridding)
  - Pie: more layout/aggregation strategies, label strategies, interactions

### 1.2 New chart types (may add new `SerieType`)

- Goal: support more common standalone chart categories in AssetStore.
- Candidate directions (examples):
  - Gauge
  - Funnel
  - BoxPlot
  - Candlestick (OHLC)
  - Treemap / Sunburst (hierarchical visualization)
  - Sankey / Graph (more complex structural charts; later)

### 1.3 3D charts (3D Charts)

- Goal: provide a 3D chart capability set (possibly a separate rendering pipeline).
- Candidate directions (examples):
  - 3D Bar / 3D Column
  - 3D Scatter
  - 3D Surface (higher complexity; later)

---

## 2. Multi-axis & coordinate systems

- Goal: enhance multi-axis scenarios while keeping semantics clear.
- Directions:
  - more axis combinations (dual Y axes, top/bottom X, mixed left/right Y)
  - clearer axis binding strategy (which axis a Serie binds to, which axis tooltip/label formats with)
  - constraints and hints for switching/mixing coordinate systems (avoid confusing configs)

---

## 3. Font & text system

- Goal: unify text rendering look and configurable options, reducing UI Toolkit cross-platform differences.
- Directions:
  - more complete text styles (font, size, weight, color, outline/shadow, etc.)
  - text layout strategies (wrap, truncate, ellipsis, alignment, anchors)
  - enhanced number formatting (thousands separator, units, percent, scientific notation, etc.)

---

## 4. Time axis & log axis

- Goal: improve expression for time series and wide-range values.
- Directions:
  - time axis: ticks, formatting, interval strategies (day/week/month/year)
  - log axis: log10/log2 ticks and labels
  - integration with data injection (how to feed time data, handle missing points)

---

## 5. Theme / palette system

- Goal: abstract "colors/fonts/default styles" from individual Profiles into reusable themes.
- Directions:
  - Theme assets (Palette + fonts + default styles)
  - override strategy between Profile and Theme (theme defaults vs Profile overrides)
  - theme preview, switching, theme library

---

## 6. Direct networking / data binding

- Goal: reduce integration cost from network API to chart.
- Directions:
  - standard input protocol based on `ChartFeed`
  - optional API Envelope support (e.g. `{code,message,data}`)
  - samples: HTTP fetch -> parse -> Apply
  - caching, throttling, error hints, fallback strategies

---

## 7. Automated tests & QA

- Goal: reduce iteration risk and make refactors safer.
- Directions:
  - data migration tests (serialization compatibility)
  - rendering regression tests (screenshot diff/pixel tolerance, or key mesh assertions)
  - interaction tests (tooltip/hit test stability)
  - performance benchmarks (large data refresh, GC, frame time)

---

## 8. Editor workflow & tooling

- Goal: make configuration, preview, injection, and reuse smoother.
- Directions:
  - LibraryWindow: templates/copy/import-export/batch operations
  - JSON Injection: stronger protocol compatibility, better error localization, better example generation
  - clearer manual and sample project
