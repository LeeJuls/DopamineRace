using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using EasyChart;

namespace EasyChart.Layers
{
    public class BarSeriesRenderer : BaseSeriesRenderer
    {
        private struct BarHit
        {
            public int SerieIndex;
            public int PointIndex;
            public float Dist;
        }

        private BarHit? _hover;
        private readonly HashSet<long> _categoryHover = new HashSet<long>();
        private readonly HashSet<long> _categoryHoverTmp = new HashSet<long>();
        private int _categoryHoverIndex = int.MinValue;

        private static long GetHoverKey(int serieIndex, int pointIndex)
        {
            return ((long)serieIndex << 32) ^ (uint)pointIndex;
        }

        private void UpdateCategoryHover(int index, bool isCategoryAxis)
        {
            if (!isCategoryAxis)
            {
                if (_categoryHover.Count > 0 || _categoryHoverIndex != int.MinValue)
                {
                    _categoryHover.Clear();
                    _categoryHoverIndex = int.MinValue;
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
                if (serie.type != SerieType.Bar) continue;
                if (serie.settings is not BarSettings settings) continue;
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
                MarkDirtyRepaint();
            }
        }

        private int GetSerieIndex(Serie target)
        {
            if (Data == null || Data.Series == null || target == null) return -1;
            for (int i = 0; i < Data.Series.Count; i++)
            {
                if (ReferenceEquals(Data.Series[i], target)) return i;
            }
            return -1;
        }

        private float GetPickRadius(BarSettings settings)
        {
            if (settings == null || settings.hover == null || !settings.hover.enabled) return 0f;
            return Mathf.Max(0f, settings.hover.pickRadius);
        }

        private BarHit? FindHoverHit(TooltipContext context)
        {
            if (Data == null || Data.Series == null) return null;

            float width = context.Width;
            float height = context.Height;
            if (width <= 0f || height <= 0f) return null;

            var groups = GetBarGroups(Data.Series);
            if (groups == null || groups.Count == 0) return null;

            float baseBarWidth;
            float barGap;
            float categoryGap;
            TryGetLayoutSettings(out baseBarWidth, out barGap, out categoryGap);

            int totalGroups = groups.Count;
            float layoutBarWidth = ComputeEffectiveBarWidth(baseBarWidth, width, totalGroups);

            float categoryPad = 0f;
            if (categoryGap > 0f && width > 0f && _xMax > _xMin)
            {
                float denom = _xMax - _xMin;
                if (TryGetXAxisCategoryInfo(out _, out int count, out bool cellCenter) && cellCenter && count > 0)
                {
                    int span = Mathf.RoundToInt(_xMax - _xMin + 1);
                    if (span < 1) span = 1;
                    denom = span;
                }

                float step = denom > 0f ? (width / denom) : width;
                categoryPad = step * categoryGap * 0.5f;
            }

            float totalGroupWidthForCategory = totalGroups * layoutBarWidth + (totalGroups - 1) * barGap;
            float edgePad = Mathf.Max(categoryPad, totalGroupWidthForCategory * 0.5f);
            edgePad = Mathf.Min(edgePad, width * 0.5f);

            var stackPos = new Dictionary<int, float>();
            var stackNeg = new Dictionary<int, float>();

            BarHit? best = null;
            Vector2 mousePos = context.LocalPos;

            for (int g = 0; g < groups.Count; g++)
            {
                var group = groups[g];
                if (group == null || group.Count == 0) continue;

                float totalGroupWidth = totalGroups * layoutBarWidth + (totalGroups - 1) * barGap;
                float startOffset = -totalGroupWidth / 2f + layoutBarWidth / 2f;
                float currentOffset = startOffset + g * (layoutBarWidth + barGap);

                bool isStackedGroup = false;
                if (group.Count > 0 && group[0].settings is BarSettings groupSettings)
                {
                    isStackedGroup = groupSettings.stacked;
                }

                stackPos.Clear();
                stackNeg.Clear();

                for (int sgi = 0; sgi < group.Count; sgi++)
                {
                    var serie = group[sgi];
                    if (serie == null || !serie.visible || serie.type != SerieType.Bar) continue;
                    if (serie.settings is not BarSettings settings) continue;

                    float pickRadius = GetPickRadius(settings);
                    if (pickRadius <= 0f) continue;

                    int serieIndex = GetSerieIndex(serie);
                    if (serieIndex < 0) continue;

                    var points = serie.seriesData;
                    if (points == null || points.Count == 0) continue;

                    for (int pi = 0; pi < points.Count; pi++)
                    {
                        var point = points[pi];
                        if (point == null) continue;

                        int xIndex = Mathf.RoundToInt(point.x);
                        float bottomVal = 0f;
                        if (isStackedGroup)
                        {
                            if (point.value >= 0)
                            {
                                if (stackPos.TryGetValue(xIndex, out float v)) bottomVal = v;
                            }
                            else
                            {
                                if (stackNeg.TryGetValue(xIndex, out float v)) bottomVal = v;
                            }
                        }

                        float animatedY = point.value * _animationProgress;
                        float topVal = bottomVal + animatedY;

                        if (isStackedGroup)
                        {
                            if (point.value >= 0) stackPos[xIndex] = topVal;
                            else stackNeg[xIndex] = topVal;
                        }

                        float pixelYBottom = GetPixelPos(new Vector2(point.x, bottomVal), width, height).y;
                        float pixelYTop = GetPixelPos(new Vector2(point.x, topVal), width, height).y;

                        float px = GetCategoryPixelX(point.x, width);
                        if (!IsXAxisCategoryWindowingActive() && !(TryGetXAxisCategoryInfo(out _, out _, out bool cellCenter) && cellCenter) && width > 0f && edgePad > 0f)
                        {
                            float ratio = Mathf.Clamp01(px / width);
                            px = Mathf.Lerp(edgePad, width - edgePad, ratio);
                        }

                        float actualBarWidth = settings.barWidth;
                        float x = px + currentOffset - actualBarWidth / 2f;
                        float drawY = pixelYTop;
                        float drawH = pixelYBottom - pixelYTop;
                        if (point.value < 0)
                        {
                            drawY = pixelYBottom;
                            drawH = pixelYTop - pixelYBottom;
                        }

                        if (drawH <= 0.001f) continue;

                        var rect = new Rect(x, drawY, actualBarWidth, drawH);
                        var pickRect = new Rect(rect.xMin - pickRadius, rect.yMin - pickRadius, rect.width + pickRadius * 2f, rect.height + pickRadius * 2f);
                        if (!pickRect.Contains(mousePos)) continue;

                        float dist = Vector2.Distance(mousePos, rect.center);
                        if (best == null || dist < best.Value.Dist)
                        {
                            best = new BarHit
                            {
                                SerieIndex = serieIndex,
                                PointIndex = pi,
                                Dist = dist
                            };
                        }
                    }
                }
            }

            return best;
        }

        public override void ClearHover()
        {
            if (_hover != null)
            {
                _hover = null;
            }

            if (_categoryHover.Count > 0 || _categoryHoverIndex != int.MinValue)
            {
                _categoryHover.Clear();
                _categoryHoverIndex = int.MinValue;
            }

            MarkDirtyRepaint();
        }

        private List<List<Serie>> GetBarGroups(List<Serie> series)
        {
            var groups = new List<List<Serie>>();
            var stackGroups = new Dictionary<string, List<Serie>>();
            var stackGroupOrder = new List<string>();

            foreach (var s in series)
            {
                if (!s.visible || s.type != SerieType.Bar) continue;
                if (s.settings is BarSettings settings)
                {
                    if (settings.stacked)
                    {
                        string key = string.IsNullOrEmpty(settings.stackGroup) ? "__default__" : settings.stackGroup;
                        if (!stackGroups.TryGetValue(key, out var list))
                        {
                            list = new List<Serie>();
                            stackGroups[key] = list;
                            stackGroupOrder.Add(key);
                        }
                        list.Add(s);
                    }
                    else
                    {
                        groups.Add(new List<Serie> { s });
                    }
                }
            }

            for (int i = 0; i < stackGroupOrder.Count; i++)
            {
                var key = stackGroupOrder[i];
                if (stackGroups.TryGetValue(key, out var list) && list != null && list.Count > 0)
                {
                    groups.Add(list);
                }
            }
            return groups;
        }

        private bool TryGetLayoutSettings(out float barWidth, out float barGap, out float categoryGap)
        {
            barWidth = 10f;
            barGap = 0f;
            categoryGap = 0f;

            if (Data == null || Data.Series == null) return false;
            for (int i = 0; i < Data.Series.Count; i++)
            {
                var s = Data.Series[i];
                if (!s.visible || s.type != SerieType.Bar) continue;
                if (s.settings is BarSettings bs)
                {
                    barWidth = bs.barWidth;
                    barGap = bs.barGap;
                    categoryGap = bs.categoryGap;
                    return true;
                }
            }
            return false;
        }

        private float ComputeEffectiveBarWidth(float baseBarWidth, float plotWidth, int groupCount)
        {
            if (plotWidth <= 0f || groupCount <= 0) return baseBarWidth;

            // Get visible category count
            int visibleCount = Mathf.RoundToInt(_xMax - _xMin + 1);
            if (visibleCount < 1) visibleCount = 1;

            // Calculate category slot width
            float categorySlotWidth = plotWidth / visibleCount;

            // Reserve some padding (e.g., 20% of slot for gaps)
            float availableWidth = categorySlotWidth * 0.8f;

            // Divide among groups
            float maxBarWidth = availableWidth / groupCount;

            // Use the smaller of configured width and calculated max
            return Mathf.Min(baseBarWidth, maxBarWidth);
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

        private bool IsXAxisCategoryIndexVisible(float categoryIndex)
        {
            if (TryGetXAxisCategoryInfo(out _, out int count, out _) && count > 0)
            {
                float xVal = categoryIndex;
                int span = Mathf.RoundToInt(_xMax - _xMin + 1);
                if (span < 1) span = 1;
                int preloadExtra = count > span ? 1 : 0;

                float xMax = _xMax + preloadExtra;
                bool wraps = xMax >= count;
                if (wraps && xVal < _xMin)
                {
                    xVal += count;
                }

                return xVal >= (_xMin - 0.0001f) && xVal <= (xMax + 0.0001f);
            }

            return categoryIndex >= (_xMin - 0.0001f) && categoryIndex <= (_xMax + 0.0001f);
        }

        private bool IsXAxisCategoryWindowingActive()
        {
            if (TryGetXAxisCategoryInfo(out _, out int count, out _) && count > 0)
            {
                int span = Mathf.RoundToInt(_xMax - _xMin + 1);
                if (span < 1) span = 1;
                return count > span;
            }

            return false;
        }

        private bool IsFirstPreloadedCategoryIndex(float categoryIndex)
        {
            if (!TryGetXAxisCategoryInfo(out _, out int count, out _) || count <= 0) return false;

            int span = Mathf.RoundToInt(_xMax - _xMin + 1);
            if (span < 1) span = 1;
            if (count <= span) return false;

            float xVal = categoryIndex;

            // Handle wrap-around
            bool wraps = _xMax >= count - 1;
            if (wraps && xVal < _xMin)
            {
                xVal += count;
            }

            // First visible item (at _xMin) is the one about to scroll out
            return Mathf.Abs(xVal - _xMin) < 0.0001f;
        }

        private bool IsLastPreloadedCategoryIndex(float categoryIndex)
        {
            if (!TryGetXAxisCategoryInfo(out _, out int count, out _) || count <= 0) return false;

            int span = Mathf.RoundToInt(_xMax - _xMin + 1);
            if (span < 1) span = 1;
            if (count <= span) return false;

            float xVal = categoryIndex;

            // Handle wrap-around
            bool wraps = _xMax >= count - 1;
            if (wraps && xVal < _xMin)
            {
                xVal += count;
            }

            // Last preloaded item (at _xMax + 1) is the one about to scroll in
            float lastPreloadedIndex = _xMax + 1;
            if (wraps && lastPreloadedIndex >= count)
            {
                lastPreloadedIndex = lastPreloadedIndex % count;
                if (xVal >= count) xVal = xVal % count;
            }
            return Mathf.Abs(xVal - lastPreloadedIndex) < 0.0001f || Mathf.Abs(categoryIndex - (_xMax + 1)) < 0.0001f;
        }

        private bool IsEdgeCategoryIndexForLabel(float categoryIndex)
        {
            // When windowing is active, skip labels for the first and last visible items
            if (!IsXAxisCategoryWindowingActive()) return false;
            return IsLastPreloadedCategoryIndex(categoryIndex);
        }

        private bool IsFirstVisibleCategoryIndexForScaleOut(float categoryIndex)
        {
            // Check if this is the first visible item (will become edge after scroll)
            // Note: Edge items are already skipped, so this checks the first non-edge item
            if (!TryGetXAxisCategoryInfo(out _, out int count, out _) || count <= 0) return false;

            int span = Mathf.RoundToInt(_xMax - _xMin + 1);
            if (span < 1) span = 1;
            if (count <= span) return false;

            float xVal = categoryIndex;

            // Handle wrap-around
            bool wraps = _xMax >= count - 1;
            if (wraps && xVal < _xMin)
            {
                xVal += count;
            }

            // First visible non-edge item (at _xMin + 1) is the one that will become edge after scroll
            return Mathf.Abs(xVal - (_xMin + 1)) < 0.0001f;
        }

        private float GetCategoryPixelX(float categoryIndex, float width)
        {
            if (width <= 0f) return 0f;

            if (TryGetXAxisCategoryInfo(out _, out int count, out bool cellCenter) && count > 0)
            {
                float xVal = categoryIndex;

                int span = Mathf.RoundToInt(_xMax - _xMin + 1);
                if (span < 1) span = 1;

                // Handle wrap-around for circular category scrolling
                bool windowingActive = count > span;
                bool wraps = _xMax >= count - 1 && windowingActive;
                if (wraps && xVal < _xMin)
                {
                    xVal += count;
                }

                float t;
                if (cellCenter)
                {
                    // Use visibleCount (span) for interval calculation
                    // Preloaded items will extend beyond the visible area
                    // t = (i + 0.5f) / visibleCount where i = xVal - _xMin
                    t = (xVal - _xMin + 0.5f) / span;
                    if (float.IsNaN(t)) t = 0f;
                    return t * width; // Don't clamp - preloaded items extend beyond
                }
                else
                {
                    // Tick mode: use span for interval calculation
                    // When windowing is active, use span directly (no edge padding)
                    // to match AxisLayer's label positioning
                    if (windowingActive)
                    {
                        // Match AxisLayer: t = i / (visibleCount - 1) where i = xVal - _xMin
                        t = span > 1 ? (xVal - _xMin) / (span - 1) : 0.5f;
                        if (float.IsNaN(t)) t = 0f;
                        return t * width;
                    }
                    else
                    {
                        // No windowing: use edge padding for better visual appearance
                        float baseBarWidth, barGap, categoryGap;
                        TryGetLayoutSettings(out baseBarWidth, out barGap, out categoryGap);

                        var groups = GetBarGroups(Data?.Series);
                        int totalGroups = groups != null ? groups.Count : 1;
                        float barWidth = ComputeEffectiveBarWidth(baseBarWidth, width, totalGroups);

                        float totalBarGroupWidth = totalGroups * barWidth + (totalGroups - 1) * barGap;
                        float edgePadding = totalBarGroupWidth * 0.5f + 10f;
                        edgePadding = Mathf.Min(edgePadding, width * 0.4f);

                        float availableWidth = width - 2f * edgePadding;
                        t = span > 1 ? (xVal - _xMin) / (span - 1) : 0.5f;
                        if (float.IsNaN(t)) t = 0f;

                        return edgePadding + t * availableWidth;
                    }
                }
            }

            if (Mathf.Approximately(_xMax, _xMin)) return 0f;
            float xRatio = (categoryIndex - _xMin) / (_xMax - _xMin);
            if (float.IsNaN(xRatio)) xRatio = 0f;
            return xRatio * width;
        }

        private int GetCategoryIndexFromPixelX(float pixelX, float width)
        {
            if (width <= 0f) return 0;
            float t = Mathf.Clamp01(pixelX / width);

            if (TryGetXAxisCategoryInfo(out _, out int count, out bool cellCenter) && count > 0 && cellCenter)
            {
                int span = Mathf.RoundToInt(_xMax - _xMin + 1);
                if (span < 1) span = 1;
                float idxF = (t * span) - 0.5f + _xMin;
                int idx = Mathf.RoundToInt(idxF);

                // Handle wrap-around
                bool wraps = _xMax >= count - 1 && count > span;
                if (wraps)
                {
                    idx = ((idx % count) + count) % count;
                }
                return idx;
            }

            int span2 = Mathf.RoundToInt(_xMax - _xMin + 1);
            if (span2 < 1) span2 = 1;
            float dataX = t * (span2 - 1) + _xMin;
            int iX = Mathf.RoundToInt(dataX);
            if (TryGetXAxisCategoryInfo(out _, out int count2, out _) && count2 > 0)
            {
                bool wraps = _xMax >= count2 - 1 && count2 > span2;
                if (wraps)
                {
                    iX = ((iX % count2) + count2) % count2;
                }
            }
            return iX;
        }

        public override void UpdateLabels()
        {
            if (!TryGetChartAndLabelController(out var chart, out var labelController)) return;
            GetSmoothScrollOffsets(chart, out float scrollOffsetX, out float scrollOffsetY);

            if (Data == null || Data.Series == null) return;

            float w = contentRect.width;
            float h = contentRect.height;
            if (w <= 0 || h <= 0) return;

            var groups = GetBarGroups(Data.Series);
            if (groups.Count == 0) return;

            AxisId xAxisId = (Data != null && Data.Cartesian != null) ? Data.Cartesian.xAxisId : AxisId.XBottom;
            var xAxis = GetAxisConfig(xAxisId);
            bool xIsCategory = xAxis != null && xAxis.axisType == AxisType.Category && xAxis.labels != null && xAxis.labels.Count > 0;
            var xLabels = xIsCategory ? xAxis.labels : null;

            AxisId yAxisId = (Data != null && Data.Cartesian != null) ? Data.Cartesian.yAxisId : AxisId.YLeft;
            var yAxis = GetAxisConfig(yAxisId);

            var stackPos = new Dictionary<int, float>();
            var stackNeg = new Dictionary<int, float>();

            float baseBarWidth, barGap, categoryGap;
            TryGetLayoutSettings(out baseBarWidth, out barGap, out categoryGap);

            int totalGroups = groups.Count;
            float barWidth = ComputeEffectiveBarWidth(baseBarWidth, w, totalGroups);

            float categoryPad = 0f;
            if (categoryGap > 0f && w > 0f && _xMax > _xMin)
            {
                float denom = _xMax - _xMin;
                if (TryGetXAxisCategoryInfo(out _, out int count, out bool cellCenter) && cellCenter && count > 0)
                {
                    int span = Mathf.RoundToInt(_xMax - _xMin + 1);
                    if (span < 1) span = 1;
                    denom = span;
                }
                float step = denom > 0f ? (w / denom) : w;
                categoryPad = step * categoryGap * 0.5f;
            }

            float totalGroupWidthForCategory = totalGroups * barWidth + (totalGroups - 1) * barGap;
            float edgePad = Mathf.Max(categoryPad, totalGroupWidthForCategory * 0.5f);
            edgePad = Mathf.Min(edgePad, w * 0.5f);

            for (int g = 0; g < groups.Count; g++)
            {
                var group = groups[g];
                if (group == null || group.Count == 0) continue;

                float totalGroupWidth = totalGroups * barWidth + (totalGroups - 1) * barGap;
                float startOffset = -totalGroupWidth / 2f + barWidth / 2f;
                float currentOffset = startOffset + g * (barWidth + barGap);

                bool isStackedGroup = group.Count > 0 && group[0].settings is BarSettings gs && gs.stacked;

                stackPos.Clear();
                stackNeg.Clear();

                for (int sgi = 0; sgi < group.Count; sgi++)
                {
                    var serie = group[sgi];
                    if (serie == null || !serie.visible || serie.type != SerieType.Bar) continue;

                    int serieIndex = GetSerieIndex(serie);
                    if (serieIndex < 0) continue;
                    if (serie.settings is not BarSettings settings) continue;

                    var points = serie.seriesData;
                    if (points == null) continue;

                    if (!serie.labelSettings.enabled)
                    {
                        if (isStackedGroup) AccumulateStackHeight(serie, stackPos, stackNeg);
                        continue;
                    }

                    int dpPlaces = Mathf.Clamp(serie.labelSettings != null ? serie.labelSettings.decimalPlaces : 2, 0, 8);
                    bool showName = serie.labelSettings != null && serie.labelSettings.showName;
                    Vector2 extraOffset = serie.labelSettings != null ? serie.labelSettings.offset : Vector2.zero;
                    int fontSizeOverride = serie.labelSettings != null ? serie.labelSettings.fontSize : 0;
                    Color colorOverride = serie.labelSettings != null ? serie.labelSettings.color : Color.clear;

                    for (int pi = 0; pi < points.Count; pi++)
                    {
                        var point = points[pi];
                        if (point == null) continue;
                        if (!IsXAxisCategoryIndexVisible(point.x)) continue;
                        // Skip labels for edge items when windowing is active
                        if (IsEdgeCategoryIndexForLabel(point.x)) continue;

                        int xIndex = Mathf.RoundToInt(point.x);

                        float bottomVal = 0f;
                        if (isStackedGroup)
                        {
                            if (point.value >= 0)
                            {
                                if (stackPos.TryGetValue(xIndex, out float v)) bottomVal = v;
                            }
                            else
                            {
                                if (stackNeg.TryGetValue(xIndex, out float v)) bottomVal = v;
                            }
                        }

                        float topVal = bottomVal + point.value;
                        if (isStackedGroup)
                        {
                            if (point.value >= 0) stackPos[xIndex] = topVal;
                            else stackNeg[xIndex] = topVal;
                        }

                        float pixelYTop = GetPixelPos(new Vector2(point.x, topVal), w, h).y;
                        float pixelYBottom = GetPixelPos(new Vector2(point.x, bottomVal), w, h).y;

                        float px = GetCategoryPixelX(point.x, w);
                        if (!IsXAxisCategoryWindowingActive() && !(TryGetXAxisCategoryInfo(out _, out _, out bool cellCenter) && cellCenter) && w > 0f && edgePad > 0f)
                        {
                            float ratio = Mathf.Clamp01(px / w);
                            px = Mathf.Lerp(edgePad, w - edgePad, ratio);
                        }

                        float x = px + currentOffset;
                        float y;

                        // Calculate label Y position based on LabelPosition
                        if (serie.labelSettings.position == LabelPosition.Center)
                        {
                            // Center of bar
                            y = (pixelYTop + pixelYBottom) / 2f;
                        }
                        else if (serie.labelSettings.position == LabelPosition.Inside)
                        {
                            // Inside bar, offset from top edge
                            y = point.value >= 0 ? pixelYTop + 15 : pixelYTop - 15;
                        }
                        else
                        {
                            // Outside: above bar for positive, below for negative
                            y = point.value >= 0 ? pixelYTop - 10 : pixelYTop + 10;
                        }

                        Vector2 anchor = new Vector2(x, y);
                        if (!IsAnchorVisibleInPlot(anchor, w, h, scrollOffsetX, scrollOffsetY)) continue;

                        string text = FormatAxisValue(point.value, yAxis, dpPlaces);
                        if (showName)
                        {
                            string name = (xLabels != null && xIndex < xLabels.Count && xIndex >= 0) ? xLabels[xIndex] : serie.name;
                            text = $"{name}\n{text}";
                        }

                        string pointId = GetStableKeyForPoint(xIsCategory, point.x, point.id, pi);
                        string labelKey = $"bar:{serieIndex}:{pointId}";
                        var bg = serie.labelSettings.background;
                        var bgColor = bg != null ? bg.color : default;
                        var bgTex = bg != null ? bg.texture : null;
                        var desc = BuildSeriesLabelDesc(
                            labelKey,
                            text,
                            anchor,
                            new Vector2(extraOffset.x, extraOffset.y),
                            clipToPlot: true,
                            fontSizeOverride,
                            colorOverride,
                            ChartLabelAnchor.Center,
                            bgColor,
                            bgTex);

                        labelController.Submit(desc);

                        // Animate scale out for the first visible item (will become edge after scroll)
                        if (IsXAxisCategoryWindowingActive() && IsFirstPreloadedCategoryIndex(point.x))
                        {
                            labelController.RequestScaleOut(labelKey);
                        }
                    }
                }
            }
        }

        public override bool GetTooltip(TooltipContext context, List<TooltipItem> items, ref Vector2? cursorPosition, ref string categoryLabel)
        {
            if (Data == null || Data.Series == null) return false;

            var hoverHit = FindHoverHit(context);
            bool hoverChanged = false;
            if (hoverHit.HasValue)
            {
                if (!_hover.HasValue || _hover.Value.SerieIndex != hoverHit.Value.SerieIndex || _hover.Value.PointIndex != hoverHit.Value.PointIndex)
                {
                    _hover = hoverHit;
                    hoverChanged = true;
                }
            }
            else
            {
                if (_hover != null)
                {
                    _hover = null;
                    hoverChanged = true;
                }
            }

            if (hoverChanged) MarkDirtyRepaint();

            float width = context.Width;
            float areaX = context.LocalPos.x;

            if (width <= 0) return false;

            // Calculate current index based on X position
            float ratio = Mathf.Clamp01(areaX / width);
            int index = GetCategoryIndexFromPixelX(areaX, width);

            List<string> labels = null;
            if (Data != null && Data.Axes != null)
            {
                AxisId xAxisId = (Data.Cartesian != null) ? Data.Cartesian.xAxisId : AxisId.XBottom;
                for (int i = 0; i < Data.Axes.Count; i++)
                {
                    var a = Data.Axes[i];
                    if (a != null && a.id == xAxisId && a.axisType == AxisType.Category)
                    {
                        labels = a.labels;
                        break;
                    }
                }
            }

            // Validate index
            bool isCategoryAxis = labels != null && labels.Count > 0;
            if (index < 0 || (isCategoryAxis && index >= labels.Count))
            {
                UpdateCategoryHover(int.MinValue, false);
                return false;
            }

            UpdateCategoryHover(index, isCategoryAxis);

            // Set shared cursor position (snap to X)
            if (cursorPosition == null)
            {
                float snapPixelX = GetCategoryPixelX(index, width);
                cursorPosition = new Vector2(snapPixelX, 0);
            }

            // Set Category Label if not already set
            if (string.IsNullOrEmpty(categoryLabel))
            {
                categoryLabel = (isCategoryAxis && index < labels.Count && index >= 0) 
                                ? labels[index] 
                                : FormatIntCached(index);
            }

            bool hit = false;

            // Find values for this index
            foreach (var serie in Data.Series)
            {
                if (!serie.visible) continue;
                if (serie.type != SerieType.Bar) continue;

                // Find point with approx X
                bool found = false;
                Vector2 val = Vector2.zero;
                
                if (serie.seriesData == null) continue;
                foreach(var p in serie.seriesData)
                {
                    if (p == null) continue;
                    if (Mathf.Approximately(p.x, index))
                    {
                        val = new Vector2(p.x, p.value);
                        found = true;
                        break;
                    }
                }

                if (found && serie.settings is BarSettings settings)
                {
                    Color color = ResolveFillColor(settings.textureFill, Color.white);
                    items.Add(new TooltipItem 
                    {
                        Name = serie.name,
                        Value = FormatAxisValue(
                            val.y,
                            GetAxisConfig((Data != null && Data.Cartesian != null) ? Data.Cartesian.yAxisId : AxisId.YLeft),
                            Mathf.Clamp(serie.labelSettings != null ? serie.labelSettings.decimalPlaces : 2, 0, 8)
                        ),
                        Color = color
                    });
                    hit = true;
                }
            }
            return hit;
        }
        
        private void AccumulateStackHeight(Serie serie, Dictionary<int, float> stackPos, Dictionary<int, float> stackNeg)
        {
            if (serie.seriesData == null) return;
            foreach (var p in serie.seriesData)
            {
                if (p == null) continue;
                if (!IsXAxisCategoryIndexVisible(p.x)) continue;
                int xIndex = Mathf.RoundToInt(p.x);
                if (p.value >= 0)
                {
                    if (!stackPos.ContainsKey(xIndex)) stackPos[xIndex] = 0;
                    stackPos[xIndex] += p.value;
                }
                else
                {
                    if (!stackNeg.ContainsKey(xIndex)) stackNeg[xIndex] = 0;
                    stackNeg[xIndex] += p.value;
                }
            }
        }

        private void CreateLabelsForSerie(Serie serie, float width, float height, float offset, bool isStacked, Dictionary<int, float> stackPos, Dictionary<int, float> stackNeg, float edgePad)
        {
            var points = serie.seriesData;
            if (points == null) return;
            float zeroPixelY = GetPixelPos(new Vector2(_xMin, 0), width, height).y;

            List<string> labels = null;
            if (Data != null && Data.Axes != null)
            {
                AxisId xAxisId = (Data.Cartesian != null) ? Data.Cartesian.xAxisId : AxisId.XBottom;
                for (int i = 0; i < Data.Axes.Count; i++)
                {
                    var a = Data.Axes[i];
                    if (a != null && a.id == xAxisId && a.axisType == AxisType.Category)
                    {
                        labels = a.labels;
                        break;
                    }
                }
            }
            
            for (int i = 0; i < points.Count; i++)
            {
                var point = points[i];
                if (point == null) continue;
                if (!IsXAxisCategoryIndexVisible(point.x)) continue;
                // Skip labels for edge items when windowing is active
                if (IsEdgeCategoryIndexForLabel(point.x)) continue;
                int xIndex = Mathf.RoundToInt(point.x);
                
                float bottomVal = 0;
                if (isStacked)
                {
                    if (point.value >= 0)
                    {
                        if (stackPos.TryGetValue(xIndex, out float v)) bottomVal = v;
                    }
                    else
                    {
                        if (stackNeg.TryGetValue(xIndex, out float v)) bottomVal = v;
                    }
                }

                float topVal = bottomVal + point.value;
                
                // Update Stack (Accumulate)
                if (isStacked)
                {
                    if (point.value >= 0) stackPos[xIndex] = topVal;
                    else stackNeg[xIndex] = topVal;
                }

                // Calculate Position
                // We want label at the "Tip" of the bar segment.
                // Tip is 'topVal'.
                
                // Get pixel positions
                // We need Top and Bottom of the segment to center vertically if needed.
                // topVal -> Top Y
                // bottomVal -> Bottom Y
                
                float pixelYTop = GetPixelPos(new Vector2(point.x, topVal), width, height).y;
                float pixelYBottom = GetPixelPos(new Vector2(point.x, bottomVal), width, height).y;
                
                float px = GetCategoryPixelX(point.x, width);
                if (!IsXAxisCategoryWindowingActive() && !(TryGetXAxisCategoryInfo(out _, out _, out bool cellCenter) && cellCenter) && width > 0f && edgePad > 0f)
                {
                    float ratio = Mathf.Clamp01(px / width);
                    px = Mathf.Lerp(edgePad, width - edgePad, ratio);
                }
                float x = px + offset;
                float y = pixelYTop; // Default Outside/Top

                if (serie.labelSettings.position == LabelPosition.Inside)
                {
                    // Slightly inside the top
                    y = pixelYTop + 15; // Move down (Y increases down)
                    if (point.value < 0) y = pixelYTop - 15; // Move up if negative bar?
                    
                    // Actually:
                    // Positive Bar: Top is pixelYTop (smaller Y). Bottom is pixelYBottom (larger Y).
                    // Inside Top: pixelYTop + padding.
                    
                    // Negative Bar: Top is pixelYTop (larger Y? No).
                    // If point.y < 0: topVal < bottomVal.
                    // pixelYTop > pixelYBottom. 
                    // Visual Top of bar is pixelYBottom? 
                    // Wait, negative bar extends DOWN from 0.
                    // 0 is at pixelYBottom. End is at pixelYTop.
                    // Visual "End" is pixelYTop.
                    // So "Inside" means slightly towards 0 (upwards, smaller Y).
                    
                    if (point.value >= 0) y = pixelYTop + 15; 
                    else y = pixelYTop - 15;
                }
                else if (serie.labelSettings.position == LabelPosition.Center)
                {
                    // Center of the segment
                    y = (pixelYTop + pixelYBottom) / 2f;
                }
                else // Outside
                {
                    y = pixelYTop;
                    // Adjust margin later
                }

                int dpPlaces = Mathf.Clamp(serie.labelSettings != null ? serie.labelSettings.decimalPlaces : 2, 0, 8);
                string text = FormatAxisValue(
                    point.value,
                    GetAxisConfig((Data != null && Data.Cartesian != null) ? Data.Cartesian.yAxisId : AxisId.YLeft),
                    dpPlaces
                );
                if (serie.labelSettings.showName)
                {
                     string name = (labels != null && xIndex < labels.Count && xIndex >= 0) ? labels[xIndex] : serie.name;
                     text = $"{name}\n{text}";
                }

                // Alignment adjustments based on position
                float baseMarginTop;
                if (serie.labelSettings.position == LabelPosition.Center || serie.labelSettings.position == LabelPosition.Inside)
                {
                    // Center align text vertically around y
                    baseMarginTop = -7; // Half line height approx
                }
                else
                {
                    // Outside
                    baseMarginTop = point.value < 0 ? 5 : -20;
                }

                DrawSerieLabel(serie, text, new Vector2(x, y), -10, baseMarginTop);
                //_labelContainer.Add(lbl); // Handled by GetLabel
            }
        }

        protected override void OnGenerateVisualContent(MeshGenerationContext context)
        {
            if (Data == null || Data.Series == null) return;

            var width = contentRect.width;
            var height = contentRect.height;
            if (width <= 0 || height <= 0) return;

            var painter = context.painter2D;

            var groups = GetBarGroups(Data.Series);
            if (groups.Count == 0) return;

            // Stacking Dictionaries
            var stackPos = new Dictionary<int, float>();
            var stackNeg = new Dictionary<int, float>();

            float baseBarWidth;
            float barGap;
            float categoryGap;
            TryGetLayoutSettings(out baseBarWidth, out barGap, out categoryGap);

            int totalGroups = groups.Count;

            // Compute effective bar width based on visible category count
            float barWidth = ComputeEffectiveBarWidth(baseBarWidth, width, totalGroups);

            float categoryPad = 0f;
            if (categoryGap > 0f && width > 0f && _xMax > _xMin)
            {
                float denom = _xMax - _xMin;
                if (TryGetXAxisCategoryInfo(out _, out int count, out bool cellCenter) && cellCenter && count > 0)
                {
                    int span = Mathf.RoundToInt(_xMax - _xMin + 1);
                    if (span < 1) span = 1;
                    denom = span;
                }

                float step = denom > 0f ? (width / denom) : width;
                categoryPad = step * categoryGap * 0.5f;
            }

            float totalGroupWidthForCategory = totalGroups * barWidth + (totalGroups - 1) * barGap;
            float edgePad = Mathf.Max(categoryPad, totalGroupWidthForCategory * 0.5f);
            edgePad = Mathf.Min(edgePad, width * 0.5f);

            for (int g = 0; g < groups.Count; g++)
            {
                var group = groups[g];
                if (group.Count == 0) continue;

                float totalGroupWidth = totalGroups * barWidth + (totalGroups - 1) * barGap;
                float startOffset = -totalGroupWidth / 2f + barWidth / 2f;
                float currentOffset = startOffset + g * (barWidth + barGap);
                
                bool isStackedGroup = false;
                if (group.Count > 0 && group[0].settings is BarSettings gs) isStackedGroup = gs.stacked;

                stackPos.Clear();
                stackNeg.Clear();

                for (int i = 0; i < group.Count; i++)
                {
                    var serie = group[i];
                    bool isLastInStackGroup = isStackedGroup && i == group.Count - 1;
                    DrawBarSerie(context, painter, serie, width, height, currentOffset, isStackedGroup, isLastInStackGroup, stackPos, stackNeg, edgePad, barWidth);
                }
            }
        }

        private void DrawBarSerie(MeshGenerationContext context, Painter2D painter, Serie serie, float width, float height, float offset, bool isStacked, bool isLastInStackGroup, Dictionary<int, float> stackPos, Dictionary<int, float> stackNeg, float edgePad, float effectiveBarWidth)
        {
            if (!(serie.settings is BarSettings settings)) return;

            int serieIndex = GetSerieIndex(serie);
            var points = serie.seriesData;
            if (points == null) return;

            float zeroY = GetPixelPos(new Vector2(_xMin, 0), width, height).y;
            float baseBarWidth = effectiveBarWidth;

            float cornerRadius = Mathf.Max(0f, settings.cornerRadius);
            int cornerSegments = Mathf.Clamp(settings.cornerSegments, 1, 16);
            bool canRound = cornerRadius > 0.01f;

            var roundedPoints = canRound ? new List<Vector2>(32) : null;

            for (int pi = 0; pi < points.Count; pi++)
            {
                var point = points[pi];
                if (point == null) continue;
                if (!IsXAxisCategoryIndexVisible(point.x)) continue;

                int xIndex = Mathf.RoundToInt(point.x);
                float bottomVal = 0f;
                if (isStacked)
                {
                    if (point.value >= 0)
                    {
                        if (stackPos.TryGetValue(xIndex, out float v)) bottomVal = v;
                    }
                    else
                    {
                        if (stackNeg.TryGetValue(xIndex, out float v)) bottomVal = v;
                    }
                }

                float animatedY = point.value * _animationProgress;
                float topVal = bottomVal + animatedY;

                if (isStacked)
                {
                    if (point.value >= 0) stackPos[xIndex] = topVal;
                    else stackNeg[xIndex] = topVal;
                }

                float pixelYBottom = GetPixelPos(new Vector2(point.x, bottomVal), width, height).y;
                float pixelYTop = GetPixelPos(new Vector2(point.x, topVal), width, height).y;

                float px = GetCategoryPixelX(point.x, width);
                if (!IsXAxisCategoryWindowingActive() && !(TryGetXAxisCategoryInfo(out _, out _, out bool cellCenter) && cellCenter) && width > 0f && edgePad > 0f)
                {
                    float ratio = Mathf.Clamp01(px / width);
                    px = Mathf.Lerp(edgePad, width - edgePad, ratio);
                }

                float drawY = pixelYTop;
                float drawH = pixelYBottom - pixelYTop;
                if (point.value < 0)
                {
                    drawY = pixelYBottom;
                    drawH = pixelYTop - pixelYBottom;
                }

                if (Mathf.Approximately(drawH, 0f))
                    continue;

                bool isHovered = false;
                if (settings.hover != null && settings.hover.enabled)
                {
                    if (_hover.HasValue && _hover.Value.SerieIndex == serieIndex && _hover.Value.PointIndex == pi)
                    {
                        isHovered = true;
                    }
                    else if (_categoryHover.Count > 0 && _categoryHover.Contains(GetHoverKey(serieIndex, pi)))
                    {
                        isHovered = true;
                    }
                }

                float localBarWidth = baseBarWidth;
                var barFill = settings.textureFill;
                UnpackTextureFill(barFill, Color.white, out var barTex, out var tiling, out var uvOffset, out var barTint);

                Texture2D hoverTex = null;
                Vector2 hoverTiling = Vector2.one;
                Vector2 hoverUvOffset = Vector2.zero;
                Color hoverOverlayTint = Color.clear;

                if (isHovered)
                {
                    float scale = Mathf.Max(0f, settings.hover.scale);
                    if (!Mathf.Approximately(scale, 0f) && !Mathf.Approximately(scale, 1f))
                    {
                        localBarWidth *= scale;
                    }

                    barTint = Color.Lerp(barTint, Color.white, 0.35f);
                    var hoverFill = settings.hover.textureFill;
                    TryResolveTextureFill(hoverFill, Color.clear, true, out hoverTex, out hoverTiling, out hoverUvOffset, out hoverOverlayTint);
                }

                float centerX = px + offset;
                float x = centerX - localBarWidth / 2f;
                var rect = new Rect(x, drawY, localBarWidth, drawH);

                bool roundThisSegment = canRound && (!isStacked || isLastInStackGroup);
                bool isPositive = point.value >= 0;
                bool roundTop = roundThisSegment && isPositive;
                bool roundBottom = roundThisSegment && !isPositive;

                float r = roundThisSegment ? cornerRadius : 0f;
                if (r > 0f)
                {
                    r = Mathf.Min(r, Mathf.Min(rect.width, rect.height) * 0.5f);
                }

                var backgroundSettings = settings.background;
                if (backgroundSettings != null && backgroundSettings.show)
                {
                    bool drawBackground = true;
                    if (isStacked)
                    {
                        drawBackground = Mathf.Approximately(bottomVal, 0f);
                    }

                    if (drawBackground)
                    {
                        var bgFill = backgroundSettings.textureFill;
                        UnpackTextureFill(bgFill, out var bgTex, out var bgTiling, out var bgOffset, out var bgTint);
                        if (bgTex != null)
                        {
                            DrawTexturedQuad(context, new Rect(x, 0, localBarWidth, height), bgTex, bgTiling, bgOffset, bgTint, true);
                        }
                        else
                        {
                            painter.fillColor = bgTint;
                            painter.BeginPath();
                            painter.MoveTo(new Vector2(x, 0));
                            painter.LineTo(new Vector2(x + localBarWidth, 0));
                            painter.LineTo(new Vector2(x + localBarWidth, height));
                            painter.LineTo(new Vector2(x, height));
                            painter.ClosePath();
                            painter.Fill();
                        }
                    }
                }

                if (barTex != null)
                {
                    Color tint = barTint;

                    if (r > 0f)
                    {
                        if (roundedPoints == null) roundedPoints = new List<Vector2>(32);
                        RoundedRectUtils.BuildRoundedRectPoints(roundedPoints, rect, r, cornerSegments, roundTop, roundTop, roundBottom, roundBottom);
                        DrawTexturedFan(context, roundedPoints, rect, barTex, tiling, uvOffset, tint, true);
                    }
                    else
                    {
                        DrawTexturedQuad(context, rect, barTex, tiling, uvOffset, tint, true);
                    }
                }
                else
                {
                    painter.fillColor = barTint;
                    if (r > 0f)
                    {
                        RoundedRectUtils.BeginRoundedRectPath(painter, rect, r, cornerSegments, roundTop, roundTop, roundBottom, roundBottom);
                    }
                    else
                    {
                        painter.BeginPath();
                        painter.MoveTo(new Vector2(rect.xMin, rect.yMin));
                        painter.LineTo(new Vector2(rect.xMax, rect.yMin));
                        painter.LineTo(new Vector2(rect.xMax, rect.yMax));
                        painter.LineTo(new Vector2(rect.xMin, rect.yMax));
                        painter.ClosePath();
                    }
                    painter.Fill();
                }

                if (hoverTex != null && hoverOverlayTint.a > 0f)
                {
                    if (r > 0f)
                    {
                        if (roundedPoints == null) roundedPoints = new List<Vector2>(32);
                        RoundedRectUtils.BuildRoundedRectPoints(roundedPoints, rect, r, cornerSegments, roundTop, roundTop, roundBottom, roundBottom);
                        DrawTexturedFan(context, roundedPoints, rect, hoverTex, hoverTiling, hoverUvOffset, hoverOverlayTint, true);
                    }
                    else
                    {
                        DrawTexturedQuad(context, rect, hoverTex, hoverTiling, hoverUvOffset, hoverOverlayTint, true);
                    }
                }

                var borderSettings = settings.border;
                float borderWidth = borderSettings != null ? borderSettings.width : 0f;
                Color borderColor = borderSettings != null ? borderSettings.color : new Color(0, 0, 0, 0);

                if (borderWidth > 0f && borderColor.a > 0f)
                {
                    painter.strokeColor = borderColor;
                    painter.lineWidth = borderWidth;

                    if (barTex != null)
                    {
                        if (r > 0f) RoundedRectUtils.BeginRoundedRectPath(painter, rect, r, cornerSegments, roundTop, roundTop, roundBottom, roundBottom);
                        else
                        {
                            painter.BeginPath();
                            painter.MoveTo(new Vector2(rect.xMin, rect.yMin));
                            painter.LineTo(new Vector2(rect.xMax, rect.yMin));
                            painter.LineTo(new Vector2(rect.xMax, rect.yMax));
                            painter.LineTo(new Vector2(rect.xMin, rect.yMax));
                            painter.ClosePath();
                        }
                    }

                    painter.Stroke();
                }

                _ = zeroY;
            }
        }
    }
}
