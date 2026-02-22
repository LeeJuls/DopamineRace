using System.Collections.Generic;
using System.Text;
using UnityEngine;
#if UNITY_2019_3_OR_NEWER
using Unity.Profiling;
#endif
using UnityEngine.UIElements;
using EasyChart.Layers;

namespace EasyChart
{
    internal sealed class ChartTooltipController : IChartTooltipRuntime
    {
#if UNITY_2019_3_OR_NEWER
        private static readonly ProfilerMarker s_tooltipMoveMarker = new ProfilerMarker("EasyChart.Tooltip.OnPointerMove");
        private static readonly ProfilerMarker s_tooltipHitTestMarker = new ProfilerMarker("EasyChart.Tooltip.HitTest");
        private static readonly ProfilerMarker s_tooltipTextMarker = new ProfilerMarker("EasyChart.Tooltip.BuildText");
#endif
        private readonly List<TooltipItem> _tooltipItems = new List<TooltipItem>(16);
        private readonly StringBuilder _sb = new StringBuilder(256);
        private readonly TooltipContext _context = new TooltipContext();

        public IChartTooltipPolicy TooltipPolicy { get; set; }

        public IChartHitTestPolicy HitTestPolicy { get; set; }

        public ChartTooltipController()
        {
            TooltipPolicy = DefaultChartTooltipPolicy.Instance;
            HitTestPolicy = DefaultChartHitTestPolicy.Instance;
        }

        private static int ClampCategoryVisibleCount(AxisConfig axis, int labelsCount)
        {
            if (labelsCount <= 0) return 0;
            if (axis != null && axis.autoTicks) return labelsCount;
            int v = axis != null ? axis.splitCount : 0;
            if (v < 2) v = 2;
            if (v > labelsCount) v = labelsCount;
            return v;
        }

        private static bool TryGetCategoryXAxisInfo(ChartData data, out AxisConfig axis, out bool cellCenter, out int visibleCount)
        {
            axis = null;
            cellCenter = false;
            visibleCount = 0;

            if (data == null || data.Axes == null) return false;

            AxisId xAxisId = (data.Cartesian != null) ? data.Cartesian.xAxisId : AxisId.XBottom;
            for (int i = 0; i < data.Axes.Count; i++)
            {
                var a = data.Axes[i];
                if (a == null) continue;
                if (a.id != xAxisId) continue;
                if (a.axisType != AxisType.Category) continue;

                axis = a;
                cellCenter = a.labelPlacement == CategoryLabelPlacement.CellCenter;
                int totalCount = a.labels != null ? a.labels.Count : 0;
                visibleCount = ClampCategoryVisibleCount(a, totalCount);
                return visibleCount > 0;
            }

            return false;
        }

        private static bool TrySnapCursorXToGrid(
            ChartData data,
            VisualElement chartArea,
            float pointerXInChartArea,
            out float snappedX)
        {
            snappedX = pointerXInChartArea;
            if (data == null || chartArea == null) return false;

            if (!TryGetCategoryXAxisInfo(data, out _, out bool cellCenter, out int visibleCount)) return false;
            if (visibleCount <= 0) return false;

            var plotViewport = chartArea.Q<VisualElement>("plot-viewport");
            if (plotViewport == null) return false;

            var rs = plotViewport.resolvedStyle;
            float plotX0 = plotViewport.layout.x + rs.borderLeftWidth + rs.paddingLeft;
            float plotW = plotViewport.contentRect.width;
            if (plotW <= 0f) return false;

            float local = pointerXInChartArea - plotX0;
            float ratio = plotW > 0f ? (local / plotW) : 0f;

            if (cellCenter)
            {
                float step = plotW / Mathf.Max(1, visibleCount);
                int cell = Mathf.Clamp(Mathf.FloorToInt(ratio * visibleCount), 0, visibleCount - 1);
                snappedX = plotX0 + (cell + 0.5f) * step;
                return true;
            }
            else
            {
                int denom = Mathf.Max(1, visibleCount - 1);
                float step = plotW / denom;
                int line = Mathf.Clamp(Mathf.RoundToInt(ratio * denom), 0, denom);
                snappedX = plotX0 + line * step;
                return true;
            }
        }

        public void Hide(Label tooltip, VisualElement cursorLine)
        {
            if (cursorLine != null) cursorLine.visible = false;
            if (tooltip != null) tooltip.visible = false;
        }

        public void OnPointerMove(
            VisualElement host,
            Vector2 pointerWorldPos,
            ChartData data,
            IList<BaseSeriesRenderer> renderers,
            VisualElement chartArea,
            Vector4 padding,
            bool isTransposed,
            bool isCategorySmoothTranslating,
            Label tooltip,
            VisualElement cursorLine)
        {
#if UNITY_2019_3_OR_NEWER
            using (s_tooltipMoveMarker.Auto())
#endif
            {
            if (data == null || data.Series == null || data.Series.Count == 0) return;
            if (isCategorySmoothTranslating)
            {
                var policy = TooltipPolicy ?? DefaultChartTooltipPolicy.Instance;
                policy.ClearHover(renderers);
                Hide(tooltip, cursorLine);
                return;
            }

            if (host == null) return;
            if (chartArea == null) return;

            Vector2 localPos = host.WorldToLocal(pointerWorldPos);
            float areaX = localPos.x - padding.x;

            float width = chartArea.contentRect.width;
            float height = chartArea.contentRect.height;
            if (width <= 0) return;

            _context.LocalPos = new Vector2(areaX, localPos.y - padding.z);
            _context.Width = width;
            _context.Height = height;

            _tooltipItems.Clear();
            Vector2? cursorPosition = null;
            string categoryLabel = "";

#if UNITY_2019_3_OR_NEWER
            using (s_tooltipHitTestMarker.Auto())
#endif
            {
                var tooltipPolicy = TooltipPolicy;
                if (tooltipPolicy != null && !ReferenceEquals(tooltipPolicy, DefaultChartTooltipPolicy.Instance))
                {
                    tooltipPolicy.TryBuildTooltip(_context, renderers, _tooltipItems, ref cursorPosition, ref categoryLabel);
                }
                else
                {
                    var hitTest = HitTestPolicy ?? DefaultChartHitTestPolicy.Instance;
                    hitTest.TryBuildTooltipItems(_context, renderers, _tooltipItems, ref cursorPosition, ref categoryLabel);
                }
            }

            if (_tooltipItems.Count == 0)
            {
                var policy = TooltipPolicy ?? DefaultChartTooltipPolicy.Instance;
                policy.ClearHover(renderers);
                Hide(tooltip, cursorLine);
                return;
            }

            if (cursorLine != null)
            {
                if (cursorPosition.HasValue)
                {
                    if (isTransposed)
                    {
                        float thickness = GetBarWidthFromData(data);
                        if (thickness <= 0f)
                            thickness = cursorLine is DashedLineElement dl ? Mathf.Max(1f, dl.LineWidth) : 1f;
                        cursorLine.style.width = StyleKeyword.Null;
                        cursorLine.style.height = thickness;
                        cursorLine.style.left = 0;
                        cursorLine.style.right = 0;
                        cursorLine.style.top = cursorPosition.Value.y - (thickness * 0.5f);
                        cursorLine.style.bottom = StyleKeyword.Null;
                    }
                    else
                    {
                        if (TrySnapCursorXToGrid(data, chartArea, _context.LocalPos.x, out float snappedX))
                        {
                            cursorPosition = new Vector2(snappedX, cursorPosition.Value.y);
                        }
                        float thickness = cursorLine is DashedLineElement dl ? Mathf.Max(1f, dl.LineWidth) : 1f;
                        cursorLine.style.height = StyleKeyword.Null;
                        cursorLine.style.width = thickness;
                        cursorLine.style.top = 0;
                        cursorLine.style.bottom = 0;
                        cursorLine.style.left = cursorPosition.Value.x - (thickness * 0.5f);
                        cursorLine.style.right = StyleKeyword.Null;
                    }
                    cursorLine.visible = true;
                }
                else
                {
                    cursorLine.visible = false;
                }
            }

            if (tooltip == null) return;

#if UNITY_2019_3_OR_NEWER
            using (s_tooltipTextMarker.Auto())
#endif
            {
            _sb.Length = 0;
            if (!string.IsNullOrEmpty(categoryLabel))
            {
                _sb.Append(categoryLabel);
                _sb.Append('\n');
            }

            for (int i = 0; i < _tooltipItems.Count; i++)
            {
                var item = _tooltipItems[i];
                _sb.Append(item.Name);
                _sb.Append(": ");
                _sb.Append(item.Value);
                _sb.Append('\n');
            }

            if (_sb.Length > 0 && _sb[_sb.Length - 1] == '\n') _sb.Length -= 1;

            tooltip.text = _sb.ToString();
            tooltip.visible = true;
            }

            float snapPixelX = cursorPosition.HasValue ? cursorPosition.Value.x : areaX;
            float tooltipX = snapPixelX + 10;
            float tooltipY = localPos.y - padding.z;

            if (tooltipX + tooltip.layout.width > width)
                tooltipX = snapPixelX - tooltip.layout.width - 10;

            tooltip.style.left = tooltipX;
            tooltip.style.top = Mathf.Max(0, tooltipY);
            }
        }

        public void OnPointerLeave(IList<BaseSeriesRenderer> renderers, Label tooltip, VisualElement cursorLine)
        {
            var policy = TooltipPolicy ?? DefaultChartTooltipPolicy.Instance;
            policy.ClearHover(renderers);

            Hide(tooltip, cursorLine);
        }

        private static float GetBarWidthFromData(ChartData data)
        {
            if (data == null || data.Series == null) return 0f;
            foreach (var serie in data.Series)
            {
                if (serie == null || !serie.visible) continue;
                if (serie.type == SerieType.Bar || serie.type == SerieType.HorizontalBar)
                {
                    if (serie.settings is BarSettings bs)
                        return bs.barWidth;
                }
            }
            return 0f;
        }
    }
}
