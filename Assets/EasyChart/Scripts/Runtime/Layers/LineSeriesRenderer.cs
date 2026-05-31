using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using EasyChart;

namespace EasyChart.Layers
{
    public class LineSeriesRenderer : BaseSeriesRenderer
    {
        private readonly List<SeriesData> _orderedVisiblePoints = new List<SeriesData>();
        private readonly Dictionary<long, float> _hoverAlpha = new Dictionary<long, float>();
        private readonly List<long> _tmpHoverKeys = new List<long>(16);

        private readonly HashSet<long> _categoryHover = new HashSet<long>();
        private readonly HashSet<long> _categoryHoverTmp = new HashSet<long>();

        private int _categoryHoverIndex = int.MinValue;

        private readonly HashSet<long> _activeHoverKeys = new HashSet<long>();

        // Stacked line support
        private readonly List<Serie> _stackedSeries = new List<Serie>();
        private readonly Dictionary<int, float> _stackedValues = new Dictionary<int, float>(); // xIndex -> accumulated value

        private bool _isRegisteredForGlobalUpdate = false;

        // Renderer for Line and Scatter charts
        public LineSeriesRenderer()
        {
            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            UpdateUpdateLoopState();
        }

        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            StopUpdateLoop();
        }

        private ChartElement GetChartElement()
        {
            var current = this.parent;
            while (current != null)
            {
                if (current is ChartElement chart) return chart;
                current = current.parent;
            }
            return null;
        }

        private void StartUpdateLoop()
        {
            if (_isRegisteredForGlobalUpdate) return;
            var chart = GetChartElement();
            if (chart != null)
            {
                ChartElement.RegisterGlobalAnimationCallback(chart, OnUpdate);
                _isRegisteredForGlobalUpdate = true;
            }
        }

        private void StopUpdateLoop()
        {
            if (!_isRegisteredForGlobalUpdate) return;
            var chart = GetChartElement();
            if (chart != null)
            {
                ChartElement.UnregisterGlobalAnimationCallback(chart, OnUpdate);
            }
            _isRegisteredForGlobalUpdate = false;
        }

        private void UpdateUpdateLoopState()
        {
            if (panel == null) { StopUpdateLoop(); return; }
            if (HasActiveUpdateWork())
            {
                if (!_isRegisteredForGlobalUpdate && GetChartElement() != null)
                    StartUpdateLoop();
            }
            else StopUpdateLoop();
        }

        private bool HasActiveUpdateWork()
        {
            if (_hoverAlpha.Count > 0 || _categoryHover.Count > 0) return true;
            if (Data == null || Data.Series == null) return false;
            if (!ProPackage.IsInstalled) return false;

            for (int i = 0; i < Data.Series.Count; i++)
            {
                var s = Data.Series[i];
                if (s == null || !s.visible) continue;
                if (s.type != SerieType.Line) continue;
                if (s.settings is not LineSettings settings) continue;
                EnsureLineSettingsInitialized(settings);

                if (settings.stroke != null && TextureFXBridge.HasAnyAnimation(settings.stroke.textureFXLayers)) return true;
                if (settings.area != null && TextureFXBridge.HasAnyAnimation(settings.area.textureFXLayers)) return true;
                if (settings.point != null && TextureFXBridge.HasAnyAnimation(settings.point.textureFXLayers)) return true;
            }

            return false;
        }

        public override void ClearHover()
        {
            bool changed = false;

            if (_categoryHover.Count > 0)
            {
                _categoryHover.Clear();
                changed = true;
            }

            if (_categoryHoverTmp.Count > 0)
            {
                _categoryHoverTmp.Clear();
            }

            if (_categoryHoverIndex != int.MinValue)
            {
                _categoryHoverIndex = int.MinValue;
                changed = true;
            }

            if (_hoverAlpha.Count > 0)
            {
                _hoverAlpha.Clear();
                changed = true;
            }

            if (changed) 
            {
                UpdateUpdateLoopState();
                MarkDirtyRepaint();
            }
        }

        private static void EnsureLineSettingsInitialized(LineSettings settings)
        {
            if (settings == null) return;
            if (settings.stroke == null) settings.stroke = new LineStrokeSettings();
            if (settings.point == null) settings.point = new PointSettings();
            if (settings.hover == null) settings.hover = new HoverHighlightSettings();
            if (settings.area == null) settings.area = new AreaFillSettings();
        }

        private bool IsCartesianTransposed()
        {
            if (Data == null) return false;
            AxisId xAxisId = (Data.Cartesian != null) ? Data.Cartesian.xAxisId : AxisId.XBottom;
            AxisId yAxisId = (Data.Cartesian != null) ? Data.Cartesian.yAxisId : AxisId.YLeft;

            var xAxis = GetAxisConfig(xAxisId);
            var yAxis = GetAxisConfig(yAxisId);
            var xDim = xAxis != null ? xAxis.axisType : AxisType.Category;
            var yDim = yAxis != null ? yAxis.axisType : AxisType.Value;
            return xDim == AxisType.Value && yDim == AxisType.Category;
        }

        protected override Vector2 GetPixelPos(Vector2 point, float width, float height)
        {
            if (IsCartesianTransposed())
            {
                return base.GetPixelPos(new Vector2(point.y, point.x), width, height);
            }
            return base.GetPixelPos(point, width, height);
        }

        private float GetEffectivePixelClipX(float width)
        {
            float pixelClipX = Mathf.Clamp01(_animationProgress) * width;
            if (width <= 0f) return pixelClipX;

            // When category axis windowing is active (labels count > visible span),
            // preload one extra item offscreen to avoid missing the edge segment.
            if (_animationProgress >= 0.999f)
            {
                AxisId xAxisId = (Data != null && Data.Cartesian != null) ? Data.Cartesian.xAxisId : AxisId.XBottom;
                var xAxis = GetAxisConfig(xAxisId);
                if (xAxis != null && xAxis.axisType == AxisType.Category && xAxis.labels != null && xAxis.labels.Count > 0)
                {
                    int labelCount = xAxis.labels.Count;
                    int span = Mathf.RoundToInt(_xMax - _xMin + 1);
                    if (span < 1) span = 1;
                    if (labelCount > span)
                    {
                        return width * 2f;
                    }
                }
            }

            return pixelClipX;
        }

        private void DrawPointMarkerWithAlpha(MeshGenerationContext context, Painter2D painter, Vector2 pos, float radius, TextureFillSettings fill, Color defaultColor, float alpha, Vector2 uvOffsetAdd)
        {
            if (radius <= 0f) return;
            if (alpha <= 0.001f) return;

            UnpackTextureFill(fill, defaultColor, out var tex, out var tiling, out var offset, out var color);

            offset += uvOffsetAdd;
            color.a *= alpha;
            if (color.a <= 0.001f) return;

            float r = radius;
            if (tex != null) r *= EvalTextureScaleSizeMul(fill);

            if (tex != null)
            {
                DrawTexturedQuad(
                    context,
                    new Rect(pos.x - r, pos.y - r, r * 2f, r * 2f),
                    tex,
                    tiling,
                    offset,
                    color,
                    true);
            }
            else
            {
                painter.fillColor = color;
                painter.BeginPath();
                painter.Arc(pos, r, 0, 360);
                painter.Fill();
            }
        }

        private void UpdateCategoryHover(int index, bool isCategoryAxis)
        {
            if (!isCategoryAxis)
            {
                if (_categoryHover.Count > 0 || _categoryHoverIndex != int.MinValue)
                {
                    _categoryHover.Clear();
                    _categoryHoverIndex = int.MinValue;
                    UpdateUpdateLoopState();
                    MarkDirtyRepaint();
                }
                return;
            }

            if (_categoryHoverIndex == index) return;

            _categoryHoverIndex = index;
            _categoryHoverTmp.Clear();

            if (Data == null || Data.Series == null) return;

            for (int si = 0; si < Data.Series.Count; si++)
            {
                var serie = Data.Series[si];
                if (serie == null || !serie.visible) continue;
                if (serie.type != SerieType.Line) continue;
                if (serie.settings is not LineSettings settings) continue;
                EnsureLineSettingsInitialized(settings);
                if (settings.hover == null || !settings.hover.enabled) continue;

                var points = serie.seriesData;
                if (points == null) continue;

                for (int pi = 0; pi < points.Count; pi++)
                {
                    var p = points[pi];
                    if (p == null) continue;
                    if (!Mathf.Approximately(p.x, index)) continue;
                    _categoryHoverTmp.Add(GetHoverKey(si, pi));
                    break;
                }
            }

            if (!_categoryHover.SetEquals(_categoryHoverTmp))
            {
                _categoryHover.Clear();
                foreach (var k in _categoryHoverTmp) _categoryHover.Add(k);
                UpdateUpdateLoopState();
                MarkDirtyRepaint();
            }
        }

        private static long GetHoverKey(int serieIndex, int pointIndex)
        {
            return ((long)serieIndex << 32) ^ (uint)pointIndex;
        }

        private float GetHoverAlpha(int serieIndex, int pointIndex)
        {
            return _hoverAlpha.TryGetValue(GetHoverKey(serieIndex, pointIndex), out float a) ? a : 0f;
        }

        private void DrawHoverSegmentOverlay(Painter2D painter, Serie serie, LineSettings settings, float width, float height, int pointIndex, float alpha, Dictionary<int, float> stackBase = null)
        {
            if (painter == null) return;
            if (serie == null || serie.seriesData == null) return;
            if (settings == null || settings.hover == null) return;

            if (alpha <= 0.001f) return;
            if (pointIndex < 0 || pointIndex >= serie.seriesData.Count) return;

            float pixelClipX = GetEffectivePixelClipX(width);

            int prev = pointIndex - 1;
            int next = pointIndex + 1;
            if (prev < 0 && next >= serie.seriesData.Count) return;

            Color c = settings.hover.lineColor;
            c.a *= alpha;
            if (c.a <= 0.001f) return;

            painter.strokeColor = c;
            painter.lineWidth = Mathf.Max(settings.stroke != null ? settings.stroke.width : 1f, 1f) * Mathf.Max(1f, settings.hover.scale);

            LineType lineType = (settings.stroke != null) ? settings.stroke.lineType : LineType.Straight;

            bool DrawClippedLine(Vector2 aPos, Vector2 bPos)
            {
                if (aPos.x > pixelClipX && bPos.x > pixelClipX) return false;

                if (aPos.x > pixelClipX)
                {
                    // Start already clipped out
                    return false;
                }

                if (bPos.x > pixelClipX)
                {
                    float denom = (bPos.x - aPos.x);
                    if (Mathf.Abs(denom) <= 0.00001f) return false;
                    float t = Mathf.Clamp01((pixelClipX - aPos.x) / denom);
                    bPos = Vector2.Lerp(aPos, bPos, t);
                    painter.BeginPath();
                    painter.MoveTo(aPos);
                    painter.LineTo(bPos);
                    painter.Stroke();
                    return false;
                }

                painter.BeginPath();
                painter.MoveTo(aPos);
                painter.LineTo(bPos);
                painter.Stroke();
                return true;
            }

            void DrawSmoothClipped(Vector2 p0, Vector2 p1)
            {
                float cpDistance = (p1.x - p0.x) * 0.5f;
                Vector2 cp1 = p0 + new Vector2(cpDistance, 0);
                Vector2 cp2 = p1 - new Vector2(cpDistance, 0);

                int samples = 10;
                Vector2 prevP = p0;
                if (prevP.x > pixelClipX) return;

                painter.BeginPath();
                painter.MoveTo(prevP);

                for (int i = 1; i <= samples; i++)
                {
                    float t = (float)i / samples;
                    Vector2 curP = CubicBezier(p0, cp1, cp2, p1, t);

                    if (curP.x > pixelClipX)
                    {
                        float denom = (curP.x - prevP.x);
                        if (Mathf.Abs(denom) > 0.00001f)
                        {
                            float tc = Mathf.Clamp01((pixelClipX - prevP.x) / denom);
                            Vector2 clipP = Vector2.Lerp(prevP, curP, tc);
                            painter.LineTo(clipP);
                        }
                        break;
                    }

                    painter.LineTo(curP);
                    prevP = curP;
                }

                painter.Stroke();
            }

            // For Straight and Step, draw as a continuous path to avoid gaps
            if (lineType == LineType.Straight || lineType == LineType.Step)
            {
                List<Vector2> pathPoints = new List<Vector2>();
                
                void AddPointsForSegment(int aIdx, int bIdx)
                {
                    if (aIdx < 0 || bIdx < 0) return;
                    if (aIdx >= serie.seriesData.Count || bIdx >= serie.seriesData.Count) return;

                    var aDp = serie.seriesData[aIdx];
                    var bDp = serie.seriesData[bIdx];
                    if (aDp == null || bDp == null) return;

                    float aY = settings.stacked ? GetStackedValue(aDp, stackBase) : aDp.value;
                    float bY = settings.stacked ? GetStackedValue(bDp, stackBase) : bDp.value;

                    Vector2 aPos = GetPixelPos(new Vector2(aDp.x, aY), width, height);
                    Vector2 bPos = GetPixelPos(new Vector2(bDp.x, bY), width, height);

                    if (pathPoints.Count == 0) pathPoints.Add(aPos);

                    if (lineType == LineType.Step)
                    {
                        Vector2 mid = new Vector2(bPos.x, aPos.y);
                        pathPoints.Add(mid);
                    }
                    pathPoints.Add(bPos);
                }

                AddPointsForSegment(prev, pointIndex);
                AddPointsForSegment(pointIndex, next);

                if (pathPoints.Count < 2) return;

                // Draw as continuous path with clipping
                painter.BeginPath();
                bool started = false;
                for (int i = 0; i < pathPoints.Count; i++)
                {
                    Vector2 p = pathPoints[i];
                    if (p.x > pixelClipX)
                    {
                        if (started && i > 0)
                        {
                            Vector2 prevP2 = pathPoints[i - 1];
                            float denom = p.x - prevP2.x;
                            if (Mathf.Abs(denom) > 0.00001f)
                            {
                                float t = Mathf.Clamp01((pixelClipX - prevP2.x) / denom);
                                Vector2 clipP = Vector2.Lerp(prevP2, p, t);
                                painter.LineTo(clipP);
                            }
                        }
                        break;
                    }

                    if (!started)
                    {
                        painter.MoveTo(p);
                        started = true;
                    }
                    else
                    {
                        painter.LineTo(p);
                    }
                }
                if (started) painter.Stroke();
                return;
            }

            // Smooth type - draw each segment with bezier curves
            void DrawSegment(int aIdx, int bIdx)
            {
                if (aIdx < 0 || bIdx < 0) return;
                if (aIdx >= serie.seriesData.Count || bIdx >= serie.seriesData.Count) return;

                var aDp = serie.seriesData[aIdx];
                var bDp = serie.seriesData[bIdx];
                if (aDp == null || bDp == null) return;

                float aY = settings.stacked ? GetStackedValue(aDp, stackBase) : aDp.value;
                float bY = settings.stacked ? GetStackedValue(bDp, stackBase) : bDp.value;

                Vector2 aPos = GetPixelPos(new Vector2(aDp.x, aY), width, height);
                Vector2 bPos = GetPixelPos(new Vector2(bDp.x, bY), width, height);

                DrawSmoothClipped(aPos, bPos);
            }

            DrawSegment(prev, pointIndex);
            DrawSegment(pointIndex, next);
        }

        private bool TryGetXAxisCategoryInfo(out AxisConfig axis, out int labelCount, out bool cellCenter)
        {
            axis = null;
            labelCount = 0;
            cellCenter = false;

            AxisId xAxisId = (Data != null && Data.Cartesian != null) ? Data.Cartesian.xAxisId : AxisId.XBottom;
            axis = GetAxisConfig(xAxisId);
            if (axis == null || axis.axisType != AxisType.Category) return false;
            if (axis.labels == null || axis.labels.Count <= 0) return false;

            labelCount = axis.labels.Count;
            cellCenter = axis.labelPlacement == CategoryLabelPlacement.CellCenter;
            return true;
        }

        public override bool GetTooltip(TooltipContext context, List<TooltipItem> items, ref Vector2? cursorPosition, ref string categoryLabel)
        {
            if (Data == null || Data.Series == null) return false;

            bool transposed = IsCartesianTransposed();

            float width = context.Width;
            float height = context.Height;
            float areaX = context.LocalPos.x;
            float areaY = context.LocalPos.y;

            if (width <= 0 || height <= 0) return false;

            float categoryPixel = transposed ? areaY : areaX;
            float categorySize = transposed ? height : width;
            float categoryMin = transposed ? _yMin : _xMin;
            float categoryMax = transposed ? _yMax : _xMax;

            AxisId xAxisId2 = (Data != null && Data.Cartesian != null) ? Data.Cartesian.xAxisId : AxisId.XBottom;
            bool xOnTop = xAxisId2 == AxisId.XTop;

            float ratioRaw = Mathf.Clamp01(categoryPixel / categorySize);
            float ratio = (transposed && xOnTop == false) ? (1.0f - ratioRaw) : ratioRaw;
            float dataCategory = ratio * (categoryMax - categoryMin) + categoryMin;

            List<string> labels = null;
            if (Data != null && Data.Axes != null)
            {
                AxisId xAxisId = (Data != null && Data.Cartesian != null) ? Data.Cartesian.xAxisId : AxisId.XBottom;
                AxisId yAxisId = (Data != null && Data.Cartesian != null) ? Data.Cartesian.yAxisId : AxisId.YLeft;
                AxisId categoryAxisId = transposed ? yAxisId : xAxisId;

                for (int i = 0; i < Data.Axes.Count; i++)
                {
                    var a = Data.Axes[i];
                    if (a != null && a.id == categoryAxisId && a.axisType == AxisType.Category)
                    {
                        labels = a.labels;
                        break;
                    }
                }
            }

            bool isCategoryAxis = labels != null && labels.Count > 0;
            bool cellCenter = false;
            if (isCategoryAxis)
            {
                AxisId xAxisId = (Data != null && Data.Cartesian != null) ? Data.Cartesian.xAxisId : AxisId.XBottom;
                AxisId yAxisId = (Data != null && Data.Cartesian != null) ? Data.Cartesian.yAxisId : AxisId.YLeft;
                AxisId categoryAxisId = transposed ? yAxisId : xAxisId;
                var axis = GetAxisConfig(categoryAxisId);
                cellCenter = axis != null && axis.labelPlacement == CategoryLabelPlacement.CellCenter;
            }

            int index;
            if (isCategoryAxis && cellCenter)
            {
                float idxF = (ratio * labels.Count) - 0.5f + categoryMin;
                index = Mathf.RoundToInt(idxF);
            }
            else
            {
                index = Mathf.RoundToInt(dataCategory);
            }

            // Validate index for Category Axis
            if (isCategoryAxis)
            {
                if (index < 0 || index >= labels.Count)
                {
                    UpdateCategoryHover(int.MinValue, false);
                    return false;
                }
            }

            UpdateCategoryHover(index, isCategoryAxis);

            // Set shared cursor position
            if (cursorPosition == null)
            {
                if (isCategoryAxis)
                {
                    // Snap to index
                    float t;
                    if (cellCenter)
                    {
                        t = (index - categoryMin + 0.5f) / Mathf.Max(1, labels.Count);
                    }
                    else
                    {
                        t = (float)(index - categoryMin) / (categoryMax - categoryMin);
                    }

                    if (float.IsNaN(t)) t = 0f;
                    t = Mathf.Clamp01(t);

                    float snapPixel = transposed
                        ? (xOnTop ? (t * categorySize) : ((1.0f - t) * categorySize))
                        : (t * categorySize);
                    cursorPosition = transposed ? new Vector2(0, snapPixel) : new Vector2(snapPixel, 0);
                }
                else
                {
                    // Continuous axis: Cursor follows mouse or snaps to closest point?
                    // For now, let's follow mouse X for smooth feel
                    cursorPosition = transposed ? new Vector2(0, areaY) : new Vector2(areaX, 0);
                }
            }

            // Set Category Label if not already set
            if (string.IsNullOrEmpty(categoryLabel))
            {
                if (isCategoryAxis && index < labels.Count && index >= 0)
                {
                    categoryLabel = labels[index];
                }
                else if (!isCategoryAxis)
                {
                    // Show value for continuous axis
                    AxisId xAxisId = (Data != null && Data.Cartesian != null) ? Data.Cartesian.xAxisId : AxisId.XBottom;
                    AxisId yAxisId = (Data != null && Data.Cartesian != null) ? Data.Cartesian.yAxisId : AxisId.YLeft;
                    AxisId axisId = transposed ? yAxisId : xAxisId;
                    var axis = GetAxisConfig(axisId);
                    categoryLabel = FormatAxisValue(dataCategory, axis, 2);
                }
            }

            bool hit = false;

            // Find values
            foreach (var serie in Data.Series)
            {
                if (!serie.visible) continue;
                if (serie.type != SerieType.Line) continue;

                Color seriesColor = Color.white;

                LineSettings settings = serie.settings as LineSettings;
                if (settings == null) continue;
                EnsureLineSettingsInitialized(settings);
                seriesColor = settings.stroke.color;

                bool found = false;
                Vector2 val = Vector2.zero;

                if (isCategoryAxis)
                {
                    // Look for exact index match
                    if (serie.seriesData == null) continue;
                    foreach (var p in serie.seriesData)
                    {
                        if (p == null) continue;
                        if (Mathf.Approximately(p.x, index))
                        {
                            val = new Vector2(p.x, p.value);
                            found = true;
                            break;
                        }
                    }
                }
                else
                {
                    // Look for closest point in X
                    float minDist = float.MaxValue;
                    // Define a search radius in data units relative to range
                    float range = categoryMax - categoryMin;
                    float threshold = (range > 0) ? range * 0.05f : 0.5f; // 5% range threshold

                    if (serie.seriesData == null) continue;
                    foreach (var p in serie.seriesData)
                    {
                        if (p == null) continue;
                        float dist = Mathf.Abs(p.x - dataCategory);
                        if (dist < minDist && dist < threshold)
                        {
                            minDist = dist;
                            val = new Vector2(p.x, p.value);
                            found = true;
                        }
                    }
                }

                if (found)
                {
                    items.Add(new TooltipItem
                    {
                        Name = serie.name,
                        Value = FormatAxisValue(
                            val.y,
                            GetAxisConfig((Data != null && Data.Cartesian != null) ? Data.Cartesian.yAxisId : AxisId.YLeft),
                            Mathf.Clamp(serie.labelSettings != null ? serie.labelSettings.decimalPlaces : 2, 0, 8)
                        ),
                        Color = seriesColor
                    });
                    hit = true;
                }
            }
            return hit;
        }

        private void OnUpdate()
        {
            if (panel == null) { StopUpdateLoop(); return; }

            if (!HasActiveUpdateWork())
            {
                StopUpdateLoop();
                return;
            }

            bool dirty = false;

            if (ProPackage.IsInstalled)
            {
                for (int i = 0; i < Data.Series.Count; i++)
                {
                    var s = Data.Series[i];
                    if (s == null || !s.visible) continue;
                    if (s.type != SerieType.Line) continue;
                    if (s.settings is not LineSettings settings) continue;
                    EnsureLineSettingsInitialized(settings);

                    if (settings.stroke != null && TextureFXBridge.HasAnyAnimation(settings.stroke.textureFXLayers)) { dirty = true; break; }
                    if (settings.area != null && TextureFXBridge.HasAnyAnimation(settings.area.textureFXLayers)) { dirty = true; break; }
                    if (settings.point != null && TextureFXBridge.HasAnyAnimation(settings.point.textureFXLayers)) { dirty = true; break; }
                }
            }

            if (_hoverAlpha.Count > 0 || _categoryHover.Count > 0)
            {
                float dt = Time.deltaTime;
                float speed = 12f;

                _activeHoverKeys.Clear();
                foreach (var k in _categoryHover) _activeHoverKeys.Add(k);

                foreach (var k in _activeHoverKeys)
                {
                    if (!_hoverAlpha.ContainsKey(k)) _hoverAlpha[k] = 0f;
                }

                if (_hoverAlpha.Count > 0)
                {
                    _tmpHoverKeys.Clear();
                    foreach (var k in _hoverAlpha.Keys)
                    {
                        _tmpHoverKeys.Add(k);
                    }

                    for (int j = 0; j < _tmpHoverKeys.Count; j++)
                    {
                        long k = _tmpHoverKeys[j];
                        float current = _hoverAlpha[k];
                        float target = _activeHoverKeys.Contains(k) ? 1f : 0f;
                        float next = Mathf.MoveTowards(current, target, speed * dt);
                        if (!Mathf.Approximately(next, current))
                        {
                            _hoverAlpha[k] = next;
                            dirty = true;
                        }
                        if (target <= 0f && next <= 0.0001f)
                        {
                            _hoverAlpha.Remove(k);
                            dirty = true;
                        }
                    }
                }
            }

            if (dirty) MarkDirtyRepaint();

            UpdateUpdateLoopState();
        }

        protected override void OnGenerateVisualContent(MeshGenerationContext context)
        {
            UpdateUpdateLoopState();
            if (Data == null || Data.Series == null) return;

            var width = contentRect.width;
            var height = contentRect.height;
            if (width <= 0 || height <= 0) return;

            var painter = context.painter2D;

            // Build stack groups for stacked line series
            BuildStackGroups();

            // Render in 3 passes to ensure correct z-order:
            // Pass 1: All Areas (bottom layer)
            // Pass 2: All Lines (middle layer)
            // Pass 3: All Points (top layer)

            // Pass 1: Draw all Areas
            for (int si = 0; si < Data.Series.Count; si++)
            {
                var serie = Data.Series[si];
                if (serie == null || !serie.visible || serie.seriesData == null || serie.seriesData.Count == 0) continue;
                if (serie.type != SerieType.Line) continue;

                LineSettings settings = serie.settings as LineSettings;
                if (settings == null) continue;
                EnsureLineSettingsInitialized(settings);

                Dictionary<int, float> stackBase = null;
                if (settings.stacked)
                {
                    stackBase = GetStackBaseForSerie(serie, settings);
                }

                if (settings.area.show)
                {
                    if (settings.area.textureFill != null && settings.area.textureFill.texture != null)
                    {
                        DrawAreaWithTexture(context, serie, settings, width, height, stackBase);
                    }
                    else
                    {
                        DrawArea(painter, serie, settings, width, height, stackBase);
                    }

                    // Draw TextureFX layers on top of the area (Pro only)
                    if (ProPackage.IsInstalled && settings.area.textureFXLayers != null && settings.area.textureFXLayers.Count > 0)
                    {
                        var areaTopVertices = BuildAreaTopVertices(serie, settings, width, height, stackBase);
                        if (areaTopVertices != null && areaTopVertices.Count >= 2)
                        {
                            var points = GetOrderedVisiblePoints(serie);
                            
                            if (settings.stacked && stackBase != null && points != null && points.Count > 0)
                            {
                                // For stacked areas, calculate bottom Y for each vertex using the same line type
                                var bottomYs = BuildStackedBottomYs(areaTopVertices, points, stackBase, settings.stroke.lineType, width, height);
                                TextureFXBridge.DrawAreaClippedLayers(context, areaTopVertices, bottomYs, settings.area.textureFXLayers, TextureFXBridge.GetAnimationTime());
                            }
                            else
                            {
                                // Non-stacked: use fixed bottom Y
                                TextureFXBridge.DrawAreaClippedLayers(context, areaTopVertices, height, settings.area.textureFXLayers, TextureFXBridge.GetAnimationTime());
                            }
                        }
                    }
                }

                if (settings.stacked)
                {
                    UpdateStackValues(serie, settings);
                }
            }

            // Reset stack values for Pass 2
            _stackedValues.Clear();

            // Pass 2: Draw all Lines
            for (int si = 0; si < Data.Series.Count; si++)
            {
                var serie = Data.Series[si];
                if (serie == null || !serie.visible || serie.seriesData == null || serie.seriesData.Count == 0) continue;
                if (serie.type != SerieType.Line) continue;

                LineSettings settings = serie.settings as LineSettings;
                if (settings == null) continue;
                EnsureLineSettingsInitialized(settings);

                Dictionary<int, float> stackBase = null;
                if (settings.stacked)
                {
                    stackBase = GetStackBaseForSerie(serie, settings);
                }

                bool useStrokeTexture = settings.stroke != null && settings.stroke.textureFill != null && settings.stroke.textureFill.texture != null;
                if (useStrokeTexture)
                {
                    DrawLineWithTexture(context, serie, settings, width, height, stackBase);
                }
                else
                {
                    DrawLine(painter, serie, settings, width, height, stackBase);
                }

                // Draw TextureFX layers on top of the stroke (Pro only)
                if (ProPackage.IsInstalled && settings.stroke != null && settings.stroke.textureFXLayers != null && settings.stroke.textureFXLayers.Count > 0)
                {
                    var linePoints = BuildLinePoints(serie, settings, width, height, stackBase);
                    if (linePoints != null && linePoints.Count >= 2)
                    {
                        TextureFXBridge.DrawLineLayers(context, linePoints, settings.stroke.width, settings.stroke.textureFXLayers, TextureFXBridge.GetAnimationTime());
                    }
                }

                if (settings.hover != null && settings.hover.enabled)
                {
                    if (_categoryHover.Count > 0)
                    {
                        foreach (var k in _categoryHover)
                        {
                            int hoveredSerieIndex = (int)(k >> 32);
                            if (hoveredSerieIndex != si) continue;
                            int hoveredPointIndex = (int)(uint)k;
                            float a = GetHoverAlpha(hoveredSerieIndex, hoveredPointIndex);
                            DrawHoverSegmentOverlay(painter, serie, settings, width, height, hoveredPointIndex, a, stackBase);
                        }
                    }
                }

                if (settings.stacked)
                {
                    UpdateStackValues(serie, settings);
                }
            }

            // Reset stack values for Pass 3
            _stackedValues.Clear();

            // Pass 3: Draw all Points
            for (int si = 0; si < Data.Series.Count; si++)
            {
                var serie = Data.Series[si];
                if (serie == null || !serie.visible || serie.seriesData == null || serie.seriesData.Count == 0) continue;
                if (serie.type != SerieType.Line) continue;

                LineSettings settings = serie.settings as LineSettings;
                if (settings == null) continue;
                EnsureLineSettingsInitialized(settings);

                Dictionary<int, float> stackBase = null;
                if (settings.stacked)
                {
                    stackBase = GetStackBaseForSerie(serie, settings);
                }

                bool basePointShow = settings.point != null && settings.point.show;
                bool hoverPointShow = settings.hover != null && settings.hover.enabled && settings.hover.point != null && settings.hover.point.show;
                if (basePointShow || hoverPointShow)
                {
                    DrawPoints(context, painter, si, serie, settings, width, height, stackBase);
                }

                // Draw TextureFX layers on top of points (Pro only)
                if (ProPackage.IsInstalled && settings.point != null && settings.point.textureFXLayers != null && settings.point.textureFXLayers.Count > 0)
                {
                    DrawPointTextureFXLayers(context, serie, settings, width, height, stackBase);
                }

                if (settings.stacked)
                {
                    UpdateStackValues(serie, settings);
                }
            }

            // Clear stacked series after rendering
            _stackedSeries.Clear();
            _stackedValues.Clear();
        }

        private void BuildStackGroups()
        {
            _stackedSeries.Clear();
            _stackedValues.Clear();

            if (Data == null || Data.Series == null) return;

            for (int i = 0; i < Data.Series.Count; i++)
            {
                var serie = Data.Series[i];
                if (serie == null || !serie.visible || serie.type != SerieType.Line) continue;
                if (!(serie.settings is LineSettings settings) || !settings.stacked) continue;

                _stackedSeries.Add(serie);
            }
        }

        private Dictionary<int, float> GetStackBaseForSerie(Serie serie, LineSettings settings)
        {
            if (!settings.stacked) return null;

            // Find the index of this series in the stacked list
            int serieIndex = _stackedSeries.IndexOf(serie);
            if (serieIndex <= 0) return null; // First series in stack has no base

            // Return current accumulated stack values as base
            return new Dictionary<int, float>(_stackedValues);
        }

        private void UpdateStackValues(Serie serie, LineSettings settings)
        {
            if (serie == null || serie.seriesData == null) return;
            if (!settings.stacked) return;

            foreach (var point in serie.seriesData)
            {
                if (point == null) continue;
                int xIndex = Mathf.RoundToInt(point.x);
                float currentValue = _stackedValues.TryGetValue(xIndex, out float v) ? v : 0f;
                _stackedValues[xIndex] = currentValue + point.value * _animationProgress;
            }
        }

        private float GetStackedValue(SeriesData point, Dictionary<int, float> stackBase)
        {
            if (point == null) return 0f;
            float baseValue = 0f;
            if (stackBase != null)
            {
                int xIndex = Mathf.RoundToInt(point.x);
                stackBase.TryGetValue(xIndex, out baseValue);
            }
            return baseValue + point.value * _animationProgress;
        }

        private float GetStackBaseValue(SeriesData point, Dictionary<int, float> stackBase)
        {
            if (stackBase == null || point == null) return 0f;
            int xIndex = Mathf.RoundToInt(point.x);
            stackBase.TryGetValue(xIndex, out float baseValue);
            return baseValue;
        }

        private void DrawLineWithTexture(MeshGenerationContext context, Serie serie, LineSettings settings, float width, float height, Dictionary<int, float> stackBase = null)
        {
            if (context.Equals(default(MeshGenerationContext))) return;
            if (serie == null || serie.seriesData == null || serie.seriesData.Count < 2) return;
            if (settings == null || settings.stroke == null || settings.stroke.textureFill == null) return;

            UnpackTextureFill(settings.stroke.textureFill, settings.stroke.color, out var tex, out var tiling, out var uvOffset, out var tint);
            if (tex == null) return;

            if (tint.a <= 0.001f) return;

            float lineWidth = Mathf.Max(0.1f, settings.stroke.width);
            float halfW = lineWidth * 0.5f;
            var lineType = settings.stroke.lineType;
            float pixelClipX = GetEffectivePixelClipX(width);

            var ordered = GetOrderedVisiblePoints(serie);
            if (ordered == null || ordered.Count < 2) return;

            var points = new List<Vector2>(serie.seriesData.Count * 2);

            // Helper to get stacked Y value
            float GetYValue(SeriesData dp)
            {
                return settings.stacked ? GetStackedValue(dp, stackBase) : dp.value;
            }

            bool AddPoint(Vector2 p)
            {
                points.Add(p);
                return true;
            }

            bool AddClipped(Vector2 p)
            {
                if (points.Count == 0)
                {
                    if (p.x > pixelClipX) return false;
                    return AddPoint(p);
                }

                var last = points[points.Count - 1];
                if (p.x <= pixelClipX)
                {
                    return AddPoint(p);
                }

                float denom = (p.x - last.x);
                if (Mathf.Abs(denom) <= 0.00001f)
                {
                    return false;
                }

                float t = Mathf.Clamp01((pixelClipX - last.x) / denom);
                Vector2 clipped = Vector2.Lerp(last, p, t);
                AddPoint(clipped);
                return false;
            }

            if (lineType == LineType.Step)
            {
                for (int i = 0; i < ordered.Count; i++)
                {
                    var dp = ordered[i];
                    if (dp == null) continue;

                    Vector2 pos = GetPixelPos(new Vector2(dp.x, GetYValue(dp)), width, height);
                    if (i > 0)
                    {
                        var prev = ordered[i - 1];
                        if (prev == null) continue;
                        Vector2 prevPos = GetPixelPos(new Vector2(prev.x, GetYValue(prev)), width, height);
                        if (!AddClipped(new Vector2(pos.x, prevPos.y))) break;
                    }
                    if (!AddClipped(pos)) break;
                }
            }
            else if (lineType == LineType.Smooth)
            {
                var first = ordered[0];
                if (first == null) return;
                if (!AddClipped(GetPixelPos(new Vector2(first.x, GetYValue(first)), width, height))) return;

                for (int i = 0; i < ordered.Count - 1; i++)
                {
                    var dp0 = ordered[i];
                    var dp1 = ordered[i + 1];
                    if (dp0 == null || dp1 == null) continue;
                    Vector2 p0 = GetPixelPos(new Vector2(dp0.x, GetYValue(dp0)), width, height);
                    Vector2 p1 = GetPixelPos(new Vector2(dp1.x, GetYValue(dp1)), width, height);

                    if (p0.x > pixelClipX) break;

                    float cpDistance = (p1.x - p0.x) * 0.5f;
                    Vector2 cp1 = p0 + new Vector2(cpDistance, 0);
                    Vector2 cp2 = p1 - new Vector2(cpDistance, 0);

                    int samples = 10;
                    bool finished = false;
                    for (int t = 1; t <= samples; t++)
                    {
                        float u = (float)t / samples;
                        Vector2 curvePoint = CubicBezier(p0, cp1, cp2, p1, u);
                        if (!AddClipped(curvePoint)) { finished = true; break; }
                    }
                    if (finished) break;
                }
            }
            else
            {
                for (int i = 0; i < ordered.Count; i++)
                {
                    var dp = ordered[i];
                    if (dp == null) continue;
                    if (!AddClipped(GetPixelPos(new Vector2(dp.x, GetYValue(dp)), width, height))) break;
                }
            }

            if (points.Count < 2) return;

            float totalLen = 0f;
            for (int i = 1; i < points.Count; i++)
            {
                totalLen += Vector2.Distance(points[i - 1], points[i]);
            }
            if (totalLen <= 0.00001f) totalLen = 1f;

            int segmentCount = points.Count - 1;
            int vertexCount = points.Count * 2;
            int indexCount = segmentCount * 6;
            var mesh = context.Allocate(vertexCount, indexCount, tex);

            var normals = new Vector2[points.Count];
            var miterLengths = new float[points.Count];
            for (int i = 0; i < points.Count; i++)
            {
                Vector2 n;
                float miterLen = 1f;
                
                if (i == 0)
                {
                    Vector2 d = points[1] - points[0];
                    n = new Vector2(-d.y, d.x);
                }
                else if (i == points.Count - 1)
                {
                    Vector2 d = points[i] - points[i - 1];
                    n = new Vector2(-d.y, d.x);
                }
                else
                {
                    Vector2 d0 = (points[i] - points[i - 1]).normalized;
                    Vector2 d1 = (points[i + 1] - points[i]).normalized;
                    Vector2 n0 = new Vector2(-d0.y, d0.x);
                    Vector2 n1 = new Vector2(-d1.y, d1.x);
                    
                    // Calculate miter direction and length
                    n = (n0 + n1).normalized;
                    float dot = Vector2.Dot(n0, n);
                    if (dot > 0.001f)
                    {
                        miterLen = 1f / dot; // Miter length to maintain line width
                        // Clamp miter length to avoid extreme values at sharp angles
                        miterLen = Mathf.Min(miterLen, 2f);
                    }
                }

                if (n.sqrMagnitude <= 0.00001f) n = Vector2.up;
                normals[i] = n.normalized;
                miterLengths[i] = miterLen;
            }

            float accLen = 0f;
            for (int i = 0; i < points.Count; i++)
            {
                if (i > 0) accLen += Vector2.Distance(points[i - 1], points[i]);
                float u = (accLen / totalLen) * tiling.x + uvOffset.x;

                Vector2 p = points[i];
                Vector2 n = normals[i] * halfW * miterLengths[i];
                Vector2 a = p + n;
                Vector2 b = p - n;

                mesh.SetNextVertex(new Vertex { position = new Vector3(a.x, a.y, Vertex.nearZ), tint = tint, uv = new Vector2(u, tiling.y + uvOffset.y) });
                mesh.SetNextVertex(new Vertex { position = new Vector3(b.x, b.y, Vertex.nearZ), tint = tint, uv = new Vector2(u, uvOffset.y) });
            }

            for (int i = 0; i < segmentCount; i++)
            {
                ushort a0 = (ushort)(i * 2);
                ushort b0 = (ushort)(i * 2 + 1);
                ushort a1 = (ushort)((i + 1) * 2);
                ushort b1 = (ushort)((i + 1) * 2 + 1);

                mesh.SetNextIndex(a0); mesh.SetNextIndex(b0); mesh.SetNextIndex(a1);
                mesh.SetNextIndex(a1); mesh.SetNextIndex(b0); mesh.SetNextIndex(b1);
            }
        }

        private void DrawScatterPoints(MeshGenerationContext context, Painter2D painter, Serie serie, PointSettings point, float width, float height)
        {
            if (point == null) return;

            var fill = point.textureFill;
            UnpackTextureFill(fill, out var pointTex, out var tiling, out var offset, out var color);
            float size = point.size;
            float radius = size / 2.0f;
            float pixelClipX = width * _animationProgress;

            if (pointTex != null)
            {
                if (serie.seriesData == null) return;
                foreach (var p in serie.seriesData)
                {
                    if (p == null) continue;
                    Vector2 pos = GetPixelPos(new Vector2(p.x, p.value), width, height);
                    if (pos.x > pixelClipX) continue;

                    DrawTexturedQuad(
                        context,
                        new Rect(pos.x - radius, pos.y - radius, radius * 2f, radius * 2f),
                        pointTex,
                        tiling,
                        offset,
                        color,
                        true);
                }

                return;
            }

            painter.fillColor = color;
            if (serie.seriesData == null) return;
            foreach (var dataPoint in serie.seriesData)
            {
                if (dataPoint == null) continue;
                Vector2 pos = GetPixelPos(new Vector2(dataPoint.x, dataPoint.value), width, height);
                if (pos.x <= pixelClipX)
                {
                    painter.BeginPath();
                    painter.Arc(pos, radius, 0, 360);
                    painter.Fill();
                }
            }
        }

        private List<SeriesData> GetOrderedVisiblePoints(Serie serie)
        {
            _orderedVisiblePoints.Clear();
            if (serie == null || serie.seriesData == null || serie.seriesData.Count == 0) return _orderedVisiblePoints;

            AxisId xAxisId = (Data != null && Data.Cartesian != null) ? Data.Cartesian.xAxisId : AxisId.XBottom;
            var xAxis = GetAxisConfig(xAxisId);
            bool xIsCategory = xAxis != null
                              && xAxis.axisType == AxisType.Category
                              && xAxis.labels != null
                              && xAxis.labels.Count > 0;

            if (!xIsCategory)
            {
                _orderedVisiblePoints.AddRange(serie.seriesData);
                return _orderedVisiblePoints;
            }

            int labelCount = xAxis.labels.Count;
            bool wraps = _xMax >= labelCount;
            float xMin = _xMin;
            float xMax = _xMax;

            int span = Mathf.RoundToInt(_xMax - _xMin + 1);
            if (span < 1) span = 1;
            int preloadExtra = labelCount > span ? 1 : 0;
            xMax += preloadExtra;

            bool effectiveWraps = wraps || xMax >= labelCount;

            for (int i = 0; i < serie.seriesData.Count; i++)
            {
                var dp = serie.seriesData[i];
                if (dp == null) continue;

                float xVal = dp.x;
                if (effectiveWraps && xVal < xMin)
                {
                    xVal += labelCount;
                }

                if (xVal < xMin - 0.0001f) continue;
                if (xVal > xMax + 0.0001f) continue;

                _orderedVisiblePoints.Add(dp);
            }

            if (_orderedVisiblePoints.Count > 1)
            {
                _orderedVisiblePoints.Sort((a, b) =>
                {
                    float ax = a != null ? a.x : 0f;
                    float bx = b != null ? b.x : 0f;

                    if (effectiveWraps)
                    {
                        if (ax < xMin) ax += labelCount;
                        if (bx < xMin) bx += labelCount;
                    }

                    return ax.CompareTo(bx);
                });
            }

            return _orderedVisiblePoints;
        }

        private void DrawLine(Painter2D painter, Serie serie, LineSettings settings, float width, float height, Dictionary<int, float> stackBase = null)
        {
            painter.lineWidth = settings.stroke.width;
            var strokeColor = settings.stroke.color;
            strokeColor.a *= 1f;
            if (strokeColor.a <= 0.001f) return;
            painter.strokeColor = strokeColor;
            var lineType = settings.stroke.lineType;
            painter.BeginPath();

            var points = GetOrderedVisiblePoints(serie);
            if (points == null || points.Count == 0) return;

            Vector2 prevPos = Vector2.zero;
            
            // Animation Clip X
            float clipX = _xMin + (_xMax - _xMin) * _animationProgress;
            // Convert to pixels (approximate, since GetPixelPos handles range)
            // Better to check pixel X directly?
            // GetPixelPos maps xMin to 0 and xMax to width (usually).
            float pixelClipX = GetEffectivePixelClipX(width);

            for (int i = 0; i < points.Count; i++)
            {
                var dp = points[i];
                if (dp == null) continue;
                // Apply stacking offset
                float yValue = settings.stacked ? GetStackedValue(dp, stackBase) : dp.value;
                Vector2 pos = GetPixelPos(new Vector2(dp.x, yValue), width, height);

                if (i == 0)
                {
                    if (pos.x > pixelClipX) 
                    {
                         // First point is already beyond clip? Just move there? 
                         // Or don't draw anything?
                         // If data is sorted by X, and we animate X, we shouldn't see it.
                         // But if X range is large and we start mid-way?
                         // Let's assume sorted X for "Reveal" effect.
                         break;
                    }
                    painter.MoveTo(pos);
                }
                else
                {
                    if (pos.x > pixelClipX)
                    {
                        // Clipping needed
                        // Calculate t based on X
                        float t = Mathf.Clamp01((pixelClipX - prevPos.x) / (pos.x - prevPos.x));
                        
                        if (lineType == LineType.Step)
                        {
                            Vector2 corner = new Vector2(pos.x, prevPos.y);
                            if (corner.x > pixelClipX)
                            {
                                // Clip at horizontal part
                                painter.LineTo(new Vector2(pixelClipX, prevPos.y));
                            }
                            else
                            {
                                // Draw horizontal, then clip vertical
                                painter.LineTo(corner);
                                // Vertical part is at pos.x (which is > pixelClipX), so we shouldn't have reached here if corner.x > pixelClipX?
                                // Wait, corner.x == pos.x.
                                // If pos.x > pixelClipX, then corner.x > pixelClipX.
                                // So we always fall into the first block?
                                // Step line: (prev.x, prev.y) -> (pos.x, prev.y) -> (pos.x, pos.y)
                                // If pos.x > pixelClipX, the horizontal line crosses the clip boundary.
                                painter.LineTo(new Vector2(pixelClipX, prevPos.y));
                            }
                        }
                        else if (lineType == LineType.Smooth)
                        {
                             float cpDistance = (pos.x - prevPos.x) * 0.5f;
                             Vector2 cp1 = prevPos + new Vector2(cpDistance, 0);
                             Vector2 cp2 = pos - new Vector2(cpDistance, 0);
                             
                             // Split Bezier at t
                             // De Casteljau
                             Vector2 q0 = Vector2.Lerp(prevPos, cp1, t);
                             Vector2 q1 = Vector2.Lerp(cp1, cp2, t);
                             Vector2 q2 = Vector2.Lerp(cp2, pos, t);
                             Vector2 r0 = Vector2.Lerp(q0, q1, t);
                             Vector2 r1 = Vector2.Lerp(q1, q2, t);
                             Vector2 s0 = Vector2.Lerp(r0, r1, t); // This is the point at t
                             
                             painter.BezierCurveTo(q0, r0, s0);
                        }
                        else // Straight
                        {
                            Vector2 target = Vector2.Lerp(prevPos, pos, t);
                            painter.LineTo(target);
                        }
                        
                        // Stop drawing after clipping
                        break;
                    }
                    else
                    {
                        // Fully visible segment
                        if (lineType == LineType.Step)
                        {
                            painter.LineTo(new Vector2(pos.x, prevPos.y));
                            painter.LineTo(pos);
                        }
                        else if (lineType == LineType.Smooth)
                        {
                            float cpDistance = (pos.x - prevPos.x) * 0.5f;
                            Vector2 cp1 = prevPos + new Vector2(cpDistance, 0);
                            Vector2 cp2 = pos - new Vector2(cpDistance, 0);
                            painter.BezierCurveTo(cp1, cp2, pos);
                        }
                        else
                        {
                            painter.LineTo(pos);
                        }
                    }
                }
                prevPos = pos;
            }
            painter.Stroke();
        }

        private void DrawArea(Painter2D painter, Serie serie, LineSettings settings, float width, float height, Dictionary<int, float> stackBase = null)
        {
            UnpackTextureFill(settings.area.textureFill, out var _areaTex, out var _areaTiling, out var _areaOffset, out var _areaColor);

            _areaColor.a *= 1f;
            if (_areaColor.a <= 0.001f) return;
            painter.fillColor = _areaColor;
            var lineType = settings.stroke.lineType;

            var points = GetOrderedVisiblePoints(serie);
            if (points == null || points.Count < 2) return;

            float pixelClipX = GetEffectivePixelClipX(width);

            // Special handling for Step + Stacked: draw each segment as independent rectangle
            if (lineType == LineType.Step && settings.stacked && stackBase != null)
            {
                DrawStackedStepArea(painter, points, settings, stackBase, width, height, pixelClipX);
                return;
            }

            painter.BeginPath();

            var firstDp = points[0];
            if (firstDp == null) return;

            // Apply stacking offset
            float firstYValue = settings.stacked ? GetStackedValue(firstDp, stackBase) : firstDp.value;
            float firstBaseY = settings.stacked ? GetStackBaseValue(firstDp, stackBase) : 0f;
            Vector2 firstPos = GetPixelPos(new Vector2(firstDp.x, firstYValue), width, height);
            
            if (firstPos.x > pixelClipX) return;

            // For stacked areas, bottom is the previous series' top; for non-stacked, bottom is the chart bottom
            float firstBottomY = settings.stacked && stackBase != null 
                ? GetPixelPos(new Vector2(firstDp.x, firstBaseY), width, height).y 
                : height;
            
            painter.MoveTo(new Vector2(firstPos.x, firstBottomY));
            painter.LineTo(firstPos);

            Vector2 prevPos = firstPos;
            Vector2 lastDrawnPos = firstPos;
            SeriesData lastDp = firstDp;

            for (int i = 1; i < points.Count; i++)
            {
                var dp = points[i];
                if (dp == null) continue;
                // Apply stacking offset
                float yValue = settings.stacked ? GetStackedValue(dp, stackBase) : dp.value;
                Vector2 pos = GetPixelPos(new Vector2(dp.x, yValue), width, height);
                lastDp = dp;
                
                if (pos.x > pixelClipX)
                {
                    // Clip
                    float t = Mathf.Clamp01((pixelClipX - prevPos.x) / (pos.x - prevPos.x));
                    
                    if (lineType == LineType.Step)
                    {
                         painter.LineTo(new Vector2(pixelClipX, prevPos.y));
                         lastDrawnPos = new Vector2(pixelClipX, prevPos.y);
                    }
                    else if (lineType == LineType.Smooth)
                    {
                         float cpDistance = (pos.x - prevPos.x) * 0.5f;
                         Vector2 cp1 = prevPos + new Vector2(cpDistance, 0);
                         Vector2 cp2 = pos - new Vector2(cpDistance, 0);
                         
                         Vector2 q0 = Vector2.Lerp(prevPos, cp1, t);
                         Vector2 q1 = Vector2.Lerp(cp1, cp2, t);
                         Vector2 q2 = Vector2.Lerp(cp2, pos, t);
                         Vector2 r0 = Vector2.Lerp(q0, q1, t);
                         Vector2 r1 = Vector2.Lerp(q1, q2, t);
                         Vector2 s0 = Vector2.Lerp(r0, r1, t);
                         
                         painter.BezierCurveTo(q0, r0, s0);
                         lastDrawnPos = s0;
                    }
                    else
                    {
                        Vector2 target = Vector2.Lerp(prevPos, pos, t);
                        painter.LineTo(target);
                        lastDrawnPos = target;
                    }
                    break;
                }
                else
                {
                    if (lineType == LineType.Step)
                    {
                        painter.LineTo(new Vector2(pos.x, prevPos.y));
                        painter.LineTo(pos);
                    }
                    else if (lineType == LineType.Smooth)
                    {
                        float cpDistance = (pos.x - prevPos.x) * 0.5f;
                        Vector2 cp1 = prevPos + new Vector2(cpDistance, 0);
                        Vector2 cp2 = pos - new Vector2(cpDistance, 0);
                        painter.BezierCurveTo(cp1, cp2, pos);
                    }
                    else
                    {
                        painter.LineTo(pos);
                    }
                    lastDrawnPos = pos;
                }
                
                prevPos = pos;
            }

            // Close the area path - for stacked areas, draw back along the base line
            if (settings.stacked && stackBase != null)
            {
                // Draw back along the base (previous series' top line) in reverse order
                float lastBaseY = GetStackBaseValue(lastDp, stackBase);
                Vector2 basePos = GetPixelPos(new Vector2(lastDp.x, lastBaseY), width, height);
                painter.LineTo(basePos);
                
                // Draw base line back to first point using the same lineType as the top line
                Vector2 prevBasePos = basePos;
                for (int i = points.Count - 2; i >= 0; i--)
                {
                    var dp = points[i];
                    if (dp == null) continue;
                    float baseY = GetStackBaseValue(dp, stackBase);
                    Vector2 currentBasePos = GetPixelPos(new Vector2(dp.x, baseY), width, height);
                    
                    if (lineType == LineType.Step)
                    {
                        // Step: For stacked areas, use next point's base value to match top line step pattern
                        var nextDp = points[i + 1];
                        float nextBaseY = GetStackBaseValue(nextDp, stackBase);
                        Vector2 nextBasePos = GetPixelPos(new Vector2(nextDp.x, nextBaseY), width, height);
                        // Mirror top line: horizontal to current x at next's y, then vertical to current y
                        painter.LineTo(new Vector2(currentBasePos.x, nextBasePos.y));
                        painter.LineTo(currentBasePos);
                    }
                    else if (lineType == LineType.Smooth)
                    {
                        // Smooth: use bezier curve, but use straight lines during animation to prevent path crossing
                        if (_animationProgress < 0.99f)
                        {
                            // During animation, use straight lines to avoid bezier curve crossing issues
                            painter.LineTo(currentBasePos);
                        }
                        else
                        {
                            float cpDistance = (prevBasePos.x - currentBasePos.x) * 0.5f;
                            Vector2 cp1 = prevBasePos - new Vector2(cpDistance, 0);
                            Vector2 cp2 = currentBasePos + new Vector2(cpDistance, 0);
                            painter.BezierCurveTo(cp1, cp2, currentBasePos);
                        }
                    }
                    else
                    {
                        // Straight line
                        painter.LineTo(currentBasePos);
                    }
                    prevBasePos = currentBasePos;
                }
            }
            else
            {
                // Non-stacked: close to bottom of chart
                painter.LineTo(new Vector2(lastDrawnPos.x, height));
                painter.LineTo(new Vector2(firstPos.x, height));
            }
            
            painter.ClosePath();
            painter.Fill();
        }

        private void DrawStackedStepArea(Painter2D painter, List<SeriesData> points, LineSettings settings, Dictionary<int, float> stackBase, float width, float height, float pixelClipX)
        {
            // For Step + Stacked, draw each segment as an independent rectangle
            for (int i = 0; i < points.Count - 1; i++)
            {
                var dp0 = points[i];
                var dp1 = points[i + 1];
                if (dp0 == null || dp1 == null) continue;

                float topY0 = GetStackedValue(dp0, stackBase);
                float baseY0 = GetStackBaseValue(dp0, stackBase);

                Vector2 topLeft = GetPixelPos(new Vector2(dp0.x, topY0), width, height);
                Vector2 topRight = GetPixelPos(new Vector2(dp1.x, topY0), width, height);
                Vector2 baseLeft = GetPixelPos(new Vector2(dp0.x, baseY0), width, height);

                if (topLeft.x > pixelClipX) continue;
                float rightX = Mathf.Min(topRight.x, pixelClipX);

                painter.BeginPath();
                painter.MoveTo(new Vector2(topLeft.x, baseLeft.y));
                painter.LineTo(topLeft);
                painter.LineTo(new Vector2(rightX, topLeft.y));
                painter.LineTo(new Vector2(rightX, baseLeft.y));
                painter.ClosePath();
                painter.Fill();
            }

            // Draw last vertical segment
            if (points.Count >= 2)
            {
                var lastDp = points[points.Count - 1];
                var prevDp = points[points.Count - 2];
                if (lastDp != null && prevDp != null)
                {
                    float topY = GetStackedValue(lastDp, stackBase);
                    float baseY = GetStackBaseValue(lastDp, stackBase);
                    float prevTopY = GetStackedValue(prevDp, stackBase);

                    Vector2 topPos = GetPixelPos(new Vector2(lastDp.x, topY), width, height);
                    Vector2 basePos = GetPixelPos(new Vector2(lastDp.x, baseY), width, height);
                    Vector2 prevTopPos = GetPixelPos(new Vector2(lastDp.x, prevTopY), width, height);
                    Vector2 prevBasePos = GetPixelPos(new Vector2(lastDp.x, GetStackBaseValue(prevDp, stackBase)), width, height);

                    if (topPos.x <= pixelClipX && Mathf.Abs(topY - prevTopY) > 0.001f)
                    {
                        painter.BeginPath();
                        painter.MoveTo(new Vector2(topPos.x, prevBasePos.y));
                        painter.LineTo(prevTopPos);
                        painter.LineTo(topPos);
                        painter.LineTo(basePos);
                        painter.ClosePath();
                        painter.Fill();
                    }
                }
            }
        }

        private void DrawAreaWithTexture(MeshGenerationContext context, Serie serie, LineSettings settings, float width, float height, Dictionary<int, float> stackBase = null)
        {
            var areaFill = settings.area.textureFill;
            UnpackTextureFill(areaFill, out var areaTex, out var areaTiling, out var areaOffset, out var areaColor);
            if (areaTex == null)
            {
                DrawArea(context.painter2D, serie, settings, width, height, stackBase);
                return;
            }

            areaColor.a *= 1f;
            if (areaColor.a <= 0.001f) return;

            // Simple clipping for textured area: just limit vertices?
            // Textured mesh generation is complex to clip perfectly without re-tessellating.
            // But we can just limit the number of vertices we generate.
            
            float pixelClipX = GetEffectivePixelClipX(width);
            
            // ... (Rest of Texture logic, but break if pos.x > pixelClipX)
            // Re-implementing lightly to include clip check:
            
            var points = GetOrderedVisiblePoints(serie);
            if (points == null || points.Count < 2) return;

            List<Vector2> topVertices = new List<Vector2>();
            
            // Generate full path then clip? Or clip during generation.
            // Clip during generation is better.

            // Helper to add vertex if within bounds, or add clipped and stop
            bool AddClipped(Vector2 p)
            {
                if (p.x <= pixelClipX)
                {
                    topVertices.Add(p);
                    return true;
                }
                else
                {
                    // Calculate intersection with last point
                    if (topVertices.Count > 0)
                    {
                        Vector2 last = topVertices[topVertices.Count - 1];
                        if (Mathf.Abs(p.x - last.x) > 0.001f)
                        {
                            float t = (pixelClipX - last.x) / (p.x - last.x);
                            Vector2 clipped = Vector2.Lerp(last, p, t);
                            topVertices.Add(clipped);
                        }
                    }
                    return false; // Stop
                }
            }

            // Helper to get Y value (stacked or raw)
            float GetYValue(SeriesData dp)
            {
                return settings.stacked ? GetStackedValue(dp, stackBase) : dp.value;
            }

            if (settings.stroke.lineType == LineType.Step)
            {
                for (int i = 0; i < points.Count; i++)
                {
                    var dp = points[i];
                    if (dp == null) continue;
                    Vector2 pos = GetPixelPos(new Vector2(dp.x, GetYValue(dp)), width, height);
                    if (i > 0)
                    {
                        var prevDp = points[i - 1];
                        if (prevDp == null) continue;
                        Vector2 prevPos = GetPixelPos(new Vector2(prevDp.x, GetYValue(prevDp)), width, height);
                        if (!AddClipped(new Vector2(pos.x, prevPos.y))) break;
                    }
                    if (!AddClipped(pos)) break;
                }
            }
            else if (settings.stroke.lineType == LineType.Smooth)
            {
                if (points.Count >= 2)
                {
                    var first = points[0];
                    if (first == null) return;
                    if (!AddClipped(GetPixelPos(new Vector2(first.x, GetYValue(first)), width, height))) return;

                    for (int i = 0; i < points.Count - 1; i++)
                    {
                        var dp0 = points[i];
                        var dp1 = points[i + 1];
                        if (dp0 == null || dp1 == null) continue;
                        Vector2 p0 = GetPixelPos(new Vector2(dp0.x, GetYValue(dp0)), width, height);
                        Vector2 p1 = GetPixelPos(new Vector2(dp1.x, GetYValue(dp1)), width, height);
                        
                        if (p0.x > pixelClipX) break;

                        float cpDistance = (p1.x - p0.x) * 0.5f;
                        Vector2 cp1 = p0 + new Vector2(cpDistance, 0);
                        Vector2 cp2 = p1 - new Vector2(cpDistance, 0);
                        
                        int samples = 10;
                        bool finished = false;
                        for (int t = 1; t <= samples; t++)
                        {
                            float u = (float)t / samples;
                            Vector2 curvePoint = CubicBezier(p0, cp1, cp2, p1, u);
                            if (!AddClipped(curvePoint)) { finished = true; break; }
                        }
                        if (finished) break;
                    }
                }
            }
            else // Straight
            {
                foreach(var dp in points)
                {
                    if (dp == null) continue;
                    if (!AddClipped(GetPixelPos(new Vector2(dp.x, GetYValue(dp)), width, height))) break;
                }
            }

            // Mesh Generation (Same as before, just using clipped topVertices)
            if (topVertices.Count < 2) return;

            // Use path length for UV calculation on Step type to avoid texture distortion
            bool usePathLength = settings.stroke.lineType == LineType.Step;

            // For stacked areas, calculate bottom Y for each vertex
            if (settings.stacked && stackBase != null)
            {
                // Build bottom Y array based on stack base values, matching the line type
                List<float> bottomYs = BuildStackedBottomYs(topVertices, points, stackBase, settings.stroke.lineType, width, height);
                
                DrawTexturedVerticalStrip(
                    context,
                    topVertices,
                    bottomYs,
                    areaTex,
                    areaTiling,
                    areaOffset,
                    areaColor,
                    true,
                    usePathLength);
            }
            else
            {
                // Non-stacked: use fixed bottom Y (chart baseline)
                DrawTexturedVerticalStrip(
                    context,
                    topVertices,
                    height,
                    areaTex,
                    areaTiling,
                    areaOffset,
                    areaColor,
                    true,
                    usePathLength);
            }
        }

        private Vector2 CubicBezier(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
        {
            float u = 1 - t;
            float tt = t * t;
            float uu = u * u;
            float uuu = uu * u;
            float ttt = tt * t;

            Vector2 p = uuu * p0; 
            p += 3 * uu * t * p1; 
            p += 3 * u * tt * p2; 
            p += ttt * p3; 

            return p;
        }

        private void DrawPoints(MeshGenerationContext context, Painter2D painter, int serieIndex, Serie serie, LineSettings settings, float width, float height, Dictionary<int, float> stackBase = null)
        {
            var point = settings.point;
            float pixelClipX = GetEffectivePixelClipX(width);

            if (serie.seriesData == null) return;
            bool hoverEnabled = settings.hover != null && settings.hover.enabled;
            bool overridePoint = hoverEnabled && settings.hover.point != null;
            var hoverPoint = overridePoint ? settings.hover.point : null;

            for (int pi = 0; pi < serie.seriesData.Count; pi++)
            {
                var dataPoint = serie.seriesData[pi];
                if (dataPoint == null) continue;
                // Apply stacking offset for point positions
                float yValue = settings.stacked ? GetStackedValue(dataPoint, stackBase) : dataPoint.value;
                Vector2 pos = GetPixelPos(new Vector2(dataPoint.x, yValue), width, height);
                if (pos.x > pixelClipX) continue;

                float a = hoverEnabled ? GetHoverAlpha(serieIndex, pi) : 0f;

                if (overridePoint)
                {
                    bool baseShow = point != null && point.show;
                    bool hoverShow = hoverPoint != null && hoverPoint.show;
                    if (!baseShow && (!hoverShow || a <= 0.001f)) continue;

                    float baseRadius = baseShow ? point.size * 0.5f : 0f;
                    float targetRadius = hoverShow ? hoverPoint.size * 0.5f : 0f;
                    float radius = Mathf.Lerp(baseRadius, targetRadius, a);

                    if (baseShow && (1f - a) > 0.001f)
                    {
                        DrawPointMarkerWithAlpha(context, painter, pos, radius, point.textureFill, Color.white, (1f - a), Vector2.zero);
                    }

                    if (hoverShow && a > 0.001f)
                    {
                        DrawPointMarkerWithAlpha(context, painter, pos, radius, hoverPoint.textureFill, Color.white, a, Vector2.zero);
                    }
                }
                else
                {
                    if (point == null || !point.show) continue;
                    float radius = point.size * 0.5f;
                    DrawPointMarkerWithAlpha(context, painter, pos, radius, point.textureFill, Color.white, 1f, Vector2.zero);
                }
            }
        }

        private void DrawPointTextureFXLayers(MeshGenerationContext context, Serie serie, LineSettings settings, float width, float height, Dictionary<int, float> stackBase)
        {
            if (serie.seriesData == null) return;
            var point = settings.point;
            if (point == null || !point.show) return;

            float pixelClipX = GetEffectivePixelClipX(width);
            float time = TextureFXBridge.GetAnimationTime();

            foreach (var dataPoint in serie.seriesData)
            {
                if (dataPoint == null) continue;
                float yValue = settings.stacked ? GetStackedValue(dataPoint, stackBase) : dataPoint.value;
                Vector2 pos = GetPixelPos(new Vector2(dataPoint.x, yValue), width, height);
                if (pos.x > pixelClipX) continue;

                float radius = point.size * 0.5f;
                var pointRect = new Rect(pos.x - radius, pos.y - radius, radius * 2f, radius * 2f);
                TextureFXBridge.DrawLayers(context, pointRect, point.textureFXLayers, time);
            }
        }

        private List<Vector2> BuildAreaTopVertices(Serie serie, LineSettings settings, float width, float height, Dictionary<int, float> stackBase)
        {
            if (serie == null || serie.seriesData == null || serie.seriesData.Count < 2) return null;

            var ordered = GetOrderedVisiblePoints(serie);
            if (ordered == null || ordered.Count < 2) return null;

            var lineType = settings.stroke.lineType;
            float pixelClipX = GetEffectivePixelClipX(width);
            var vertices = new List<Vector2>(serie.seriesData.Count * 2);

            float GetYValue(SeriesData dp)
            {
                return settings.stacked ? GetStackedValue(dp, stackBase) : dp.value;
            }

            if (lineType == LineType.Step)
            {
                for (int i = 0; i < ordered.Count; i++)
                {
                    var dp = ordered[i];
                    if (dp == null) continue;
                    Vector2 pos = GetPixelPos(new Vector2(dp.x, GetYValue(dp)), width, height);
                    if (i > 0)
                    {
                        var prev = ordered[i - 1];
                        if (prev != null)
                        {
                            Vector2 prevPos = GetPixelPos(new Vector2(prev.x, GetYValue(prev)), width, height);
                            Vector2 stepPos = new Vector2(pos.x, prevPos.y);
                            if (stepPos.x <= pixelClipX) vertices.Add(stepPos);
                            else break;
                        }
                    }
                    if (pos.x <= pixelClipX) vertices.Add(pos);
                    else break;
                }
            }
            else if (lineType == LineType.Smooth)
            {
                var first = ordered[0];
                if (first == null) return vertices;
                Vector2 firstPos = GetPixelPos(new Vector2(first.x, GetYValue(first)), width, height);
                if (firstPos.x <= pixelClipX) vertices.Add(firstPos);
                else return vertices;

                for (int i = 0; i < ordered.Count - 1; i++)
                {
                    var dp0 = ordered[i];
                    var dp1 = ordered[i + 1];
                    if (dp0 == null || dp1 == null) continue;
                    Vector2 p0 = GetPixelPos(new Vector2(dp0.x, GetYValue(dp0)), width, height);
                    Vector2 p1 = GetPixelPos(new Vector2(dp1.x, GetYValue(dp1)), width, height);

                    if (p0.x > pixelClipX) break;

                    float cpDistance = (p1.x - p0.x) * 0.5f;
                    Vector2 cp1 = p0 + new Vector2(cpDistance, 0);
                    Vector2 cp2 = p1 - new Vector2(cpDistance, 0);

                    int samples = 10;
                    for (int t = 1; t <= samples; t++)
                    {
                        float u = (float)t / samples;
                        Vector2 curvePoint = CubicBezier(p0, cp1, cp2, p1, u);
                        if (curvePoint.x <= pixelClipX) vertices.Add(curvePoint);
                        else break;
                    }
                }
            }
            else // Straight
            {
                for (int i = 0; i < ordered.Count; i++)
                {
                    var dp = ordered[i];
                    if (dp == null) continue;
                    Vector2 pos = GetPixelPos(new Vector2(dp.x, GetYValue(dp)), width, height);
                    if (pos.x <= pixelClipX) vertices.Add(pos);
                    else break;
                }
            }

            return vertices;
        }

        /// <summary>
        /// Build bottom Y values for stacked areas, matching the line type (Straight, Smooth, Step)
        /// </summary>
        private List<float> BuildStackedBottomYs(List<Vector2> topVertices, List<SeriesData> points, Dictionary<int, float> stackBase, LineType lineType, float width, float height)
        {
            var bottomYs = new List<float>(topVertices.Count);
            if (points == null || points.Count < 2)
            {
                for (int i = 0; i < topVertices.Count; i++)
                    bottomYs.Add(height);
                return bottomYs;
            }

            // Build base positions for each data point
            var basePositions = new List<Vector2>(points.Count);
            for (int i = 0; i < points.Count; i++)
            {
                var dp = points[i];
                if (dp == null)
                {
                    basePositions.Add(new Vector2(0, height));
                    continue;
                }
                float baseY = GetStackBaseValue(dp, stackBase);
                Vector2 basePos = GetPixelPos(new Vector2(dp.x, baseY), width, height);
                basePositions.Add(basePos);
            }

            if (lineType == LineType.Step)
            {
                // For Step type, we need to match the top vertex structure:
                // Top vertices for Step: [dp0], [stepPos(dp1.x, dp0.y)], [dp1], [stepPos(dp2.x, dp1.y)], [dp2], ...
                // Bottom should follow the same pattern with base Y values
                
                // Build expected bottom vertices matching the Step pattern
                var stepBottomYs = new List<float>();
                for (int i = 0; i < points.Count; i++)
                {
                    if (i > 0)
                    {
                        // Add horizontal extension point (uses previous point's base Y)
                        stepBottomYs.Add(basePositions[i - 1].y);
                    }
                    // Add actual data point's base Y
                    stepBottomYs.Add(basePositions[i].y);
                }
                
                // Now match topVertices to stepBottomYs by index (they should have same count)
                for (int vi = 0; vi < topVertices.Count; vi++)
                {
                    if (vi < stepBottomYs.Count)
                        bottomYs.Add(stepBottomYs[vi]);
                    else
                        bottomYs.Add(height);
                }
            }
            else
            {
                // For Smooth and Straight types, interpolate based on X position
                for (int vi = 0; vi < topVertices.Count; vi++)
                {
                    float x = topVertices[vi].x;
                    float baseYPixel = height; // Default

                    // Find the segment that contains this X position
                    for (int j = 0; j < basePositions.Count - 1; j++)
                    {
                        Vector2 bp0 = basePositions[j];
                        Vector2 bp1 = basePositions[j + 1];

                        if (x >= bp0.x - 0.001f && x <= bp1.x + 0.001f)
                        {
                            float segmentWidth = bp1.x - bp0.x;
                            float t = segmentWidth > 0.001f ? (x - bp0.x) / segmentWidth : 0f;

                            if (lineType == LineType.Smooth)
                            {
                                // Smooth: use cubic bezier interpolation
                                float cpDistance = segmentWidth * 0.5f;
                                Vector2 cp1 = bp0 + new Vector2(cpDistance, 0);
                                Vector2 cp2 = bp1 - new Vector2(cpDistance, 0);
                                Vector2 curvePoint = CubicBezier(bp0, cp1, cp2, bp1, t);
                                baseYPixel = curvePoint.y;
                            }
                            else // Straight
                            {
                                // Linear interpolation
                                baseYPixel = Mathf.Lerp(bp0.y, bp1.y, t);
                            }
                            break;
                        }
                    }

                    bottomYs.Add(baseYPixel);
                }
            }

            return bottomYs;
        }

        private Rect BuildAreaRect(Serie serie, LineSettings settings, float width, float height, Dictionary<int, float> stackBase)
        {
            if (serie == null || serie.seriesData == null || serie.seriesData.Count == 0)
                return new Rect(0, 0, 0, 0);

            float minX = float.MaxValue, maxX = float.MinValue;
            float minY = float.MaxValue, maxY = float.MinValue;

            foreach (var dp in serie.seriesData)
            {
                if (dp == null) continue;
                float yValue = settings.stacked ? GetStackedValue(dp, stackBase) : dp.value;
                Vector2 pos = GetPixelPos(new Vector2(dp.x, yValue), width, height);
                minX = Mathf.Min(minX, pos.x);
                maxX = Mathf.Max(maxX, pos.x);
                minY = Mathf.Min(minY, pos.y);
                maxY = Mathf.Max(maxY, pos.y);
            }

            // Extend to baseline (y=0 or stack base)
            Vector2 baselinePos = GetPixelPos(new Vector2(0, 0), width, height);
            maxY = Mathf.Max(maxY, baselinePos.y);

            if (minX >= maxX || minY >= maxY)
                return new Rect(0, 0, 0, 0);

            return new Rect(minX, minY, maxX - minX, maxY - minY);
        }

        private List<Vector2> BuildLinePoints(Serie serie, LineSettings settings, float width, float height, Dictionary<int, float> stackBase)
        {
            if (serie == null || serie.seriesData == null || serie.seriesData.Count < 2) return null;

            var ordered = GetOrderedVisiblePoints(serie);
            if (ordered == null || ordered.Count < 2) return null;

            var lineType = settings.stroke.lineType;
            float pixelClipX = GetEffectivePixelClipX(width);
            var points = new List<Vector2>(serie.seriesData.Count * 2);

            float GetYValue(SeriesData dp)
            {
                return settings.stacked ? GetStackedValue(dp, stackBase) : dp.value;
            }

            bool AddPoint(Vector2 p)
            {
                points.Add(p);
                return true;
            }

            bool AddClipped(Vector2 p)
            {
                if (points.Count == 0)
                {
                    if (p.x > pixelClipX) return false;
                    return AddPoint(p);
                }

                var last = points[points.Count - 1];
                if (p.x <= pixelClipX)
                {
                    return AddPoint(p);
                }

                float denom = (p.x - last.x);
                if (Mathf.Abs(denom) <= 0.00001f)
                {
                    return false;
                }

                float t = Mathf.Clamp01((pixelClipX - last.x) / denom);
                Vector2 clipped = Vector2.Lerp(last, p, t);
                AddPoint(clipped);
                return false;
            }

            if (lineType == LineType.Step)
            {
                for (int i = 0; i < ordered.Count; i++)
                {
                    var dp = ordered[i];
                    if (dp == null) continue;

                    Vector2 pos = GetPixelPos(new Vector2(dp.x, GetYValue(dp)), width, height);
                    if (i > 0)
                    {
                        var prev = ordered[i - 1];
                        if (prev == null) continue;
                        Vector2 prevPos = GetPixelPos(new Vector2(prev.x, GetYValue(prev)), width, height);
                        if (!AddClipped(new Vector2(pos.x, prevPos.y))) break;
                    }
                    if (!AddClipped(pos)) break;
                }
            }
            else if (lineType == LineType.Smooth)
            {
                var first = ordered[0];
                if (first == null) return points;
                if (!AddClipped(GetPixelPos(new Vector2(first.x, GetYValue(first)), width, height))) return points;

                for (int i = 0; i < ordered.Count - 1; i++)
                {
                    var dp0 = ordered[i];
                    var dp1 = ordered[i + 1];
                    if (dp0 == null || dp1 == null) continue;
                    Vector2 p0 = GetPixelPos(new Vector2(dp0.x, GetYValue(dp0)), width, height);
                    Vector2 p1 = GetPixelPos(new Vector2(dp1.x, GetYValue(dp1)), width, height);

                    if (p0.x > pixelClipX) break;

                    float cpDistance = (p1.x - p0.x) * 0.5f;
                    Vector2 cp1 = p0 + new Vector2(cpDistance, 0);
                    Vector2 cp2 = p1 - new Vector2(cpDistance, 0);

                    int samples = 10;
                    bool finished = false;
                    for (int t = 1; t <= samples; t++)
                    {
                        float u = (float)t / samples;
                        Vector2 curvePoint = CubicBezier(p0, cp1, cp2, p1, u);
                        if (!AddClipped(curvePoint)) { finished = true; break; }
                    }
                    if (finished) break;
                }
            }
            else
            {
                for (int i = 0; i < ordered.Count; i++)
                {
                    var dp = ordered[i];
                    if (dp == null) continue;
                    if (!AddClipped(GetPixelPos(new Vector2(dp.x, GetYValue(dp)), width, height))) break;
                }
            }

            return points;
        }
    }
}