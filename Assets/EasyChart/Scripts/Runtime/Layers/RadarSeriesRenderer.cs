using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using EasyChart;

namespace EasyChart.Layers
{
    public class RadarSeriesRenderer : BaseSeriesRenderer
    {
        private struct RadarHit
        {
            public int SerieIndex;
            public int PointIndex;
            public Vector2 PixelPos;
            public float Dist;
        }

        private RadarHit? _hover;

        private AxisConfig GetAxis(AxisId id)
        {
            if (Data == null || Data.Axes == null) return null;
            for (int i = 0; i < Data.Axes.Count; i++)
            {
                var a = Data.Axes[i];
                if (a != null && a.id == id) return a;
            }
            return null;
        }

        private static string ResolveDimensionLabel(List<string> categoryLabels, Serie serie, int index)
        {
            if (categoryLabels != null && index >= 0 && index < categoryLabels.Count)
            {
                string l = categoryLabels[index];
                if (!string.IsNullOrEmpty(l)) return l;
            }

            if (serie != null && serie.seriesData != null && index >= 0 && index < serie.seriesData.Count)
            {
                var dp = serie.seriesData[index];
                if (dp != null && !string.IsNullOrEmpty(dp.name)) return dp.name;
            }

            return $"Dim {index}";
        }

        private float GetPickRadius(RadarSettings settings)
        {
            if (settings == null || settings.point == null || !settings.point.show) return 0f;
            float baseRadius = Mathf.Max(0f, settings.point.size) * 0.75f;
            return Mathf.Max(6f, baseRadius);
        }

        private List<Serie> GetRadarSeries()
        {
            if (Data == null || Data.Series == null) return null;
            var list = new List<Serie>();
            for (int i = 0; i < Data.Series.Count; i++)
            {
                var s = Data.Series[i];
                if (s != null && s.visible && s.type == SerieType.Radar)
                {
                    list.Add(s);
                }
            }
            return list;
        }

        private int GetRadarSerieIndex(Serie target)
        {
            if (Data == null || Data.Series == null || target == null) return -1;
            for (int i = 0; i < Data.Series.Count; i++)
            {
                if (ReferenceEquals(Data.Series[i], target)) return i;
            }
            return -1;
        }

        private RadarHit? FindHoverHit(TooltipContext context)
        {
            if (Data == null) return null;

            float width = context.Width;
            float height = context.Height;
            if (width <= 0 || height <= 0) return null;

            var series = GetRadarSeries();
            if (series == null || series.Count == 0) return null;
            Serie primarySerie = series[0];
            if (primarySerie == null) return null;

            var labels = GetCategoryLabels();
            int dimensionCount = labels != null ? labels.Count : 0;
            if (dimensionCount <= 0) dimensionCount = primarySerie.seriesData != null ? primarySerie.seriesData.Count : 0;
            if (dimensionCount <= 2) return null;

            var radiusAxis = Data.PolarAxes != null ? Data.PolarAxes.radiusAxis : null;

            GetPolarLayout(primarySerie, width, height, out var center, out var outerRadiusPx, out var innerRadiusPx, out var startAngleDeg, out var clockwise);
            if (outerRadiusPx <= 0f) return null;

            ResolveValueRange(series, radiusAxis, dimensionCount, out float minV, out float maxV);

            float step = 360f / dimensionCount;
            float sign = clockwise ? 1f : -1f;

            float bestDistSq = float.MaxValue;
            RadarHit? best = null;

            for (int si = 0; si < series.Count; si++)
            {
                var serie = series[si];
                if (serie == null || serie.seriesData == null || serie.seriesData.Count == 0) continue;
                if (serie.settings is not RadarSettings settings) continue;

                float pickRadius = GetPickRadius(settings);
                if (pickRadius <= 0f) continue;
                float thresholdSq = pickRadius * pickRadius;

                int serieIndex = GetRadarSerieIndex(serie);
                if (serieIndex < 0) continue;

                for (int i = 0; i < dimensionCount; i++)
                {
                    float value = 0f;
                    if (i < serie.seriesData.Count)
                    {
                        var dp = serie.seriesData[i];
                        if (dp != null) value = dp.value;
                    }

                    float tt = (value - minV) / (maxV - minV);
                    tt = Mathf.Clamp01(float.IsNaN(tt) ? 0f : tt);

                    float rr = Mathf.Lerp(innerRadiusPx, outerRadiusPx, tt);
                    rr = Mathf.Lerp(innerRadiusPx, rr, _animationProgress);

                    float angle = startAngleDeg + sign * (i * step);
                    Vector2 pos = center + AngleToDir(angle) * rr;

                    float dx = pos.x - context.LocalPos.x;
                    float dy = pos.y - context.LocalPos.y;
                    float distSq = dx * dx + dy * dy;
                    if (distSq > thresholdSq) continue;
                    if (distSq >= bestDistSq) continue;

                    bestDistSq = distSq;
                    best = new RadarHit
                    {
                        SerieIndex = serieIndex,
                        PointIndex = i,
                        PixelPos = pos,
                        Dist = Mathf.Sqrt(distSq)
                    };
                }
            }

            return best;
        }

        public override void ClearHover()
        {
            if (_hover != null)
            {
                _hover = null;
                MarkDirtyRepaint();
            }
        }

        private List<string> GetCategoryLabels()
        {
            if (Data == null) return null;

            var angleAxis = Data.PolarAxes != null ? Data.PolarAxes.angleAxis : null;
            if (angleAxis != null && angleAxis.labels != null && angleAxis.labels.Count > 0)
            {
                return angleAxis.labels;
            }

            if (Data.Axes == null) return null;

            AxisId preferredAxisId = Data.XAxisId;  // Use XAxisId for radar dimension labels
            for (int i = 0; i < Data.Axes.Count; i++)
            {
                var a = Data.Axes[i];
                if (a != null && a.id == preferredAxisId && a.axisType == AxisType.Category)
                {
                    return a.labels;
                }
            }

            for (int i = 0; i < Data.Axes.Count; i++)
            {
                var a = Data.Axes[i];
                if (a != null && a.axisType == AxisType.Category)
                {
                    return a.labels;
                }
            }

            return null;
        }

        private void ResolveValueRange(List<Serie> series, PolarAxisStyle radiusAxis, int dimensionCount, out float minV, out float maxV)
        {
            minV = 0f;
            maxV = 1f;

            RangeUtils.ResolveAutoRange(
                series,
                Mathf.Max(0, dimensionCount),
                dp => dp != null ? dp.value : float.NaN,
                true,
                0f,
                1f,
                out minV,
                out maxV);

            bool autoMin = radiusAxis == null || radiusAxis.autoRangeMin;
            bool autoMax = radiusAxis == null || radiusAxis.autoRangeMax;
            if (!autoMin && radiusAxis != null) minV = radiusAxis.minValue;
            if (!autoMax && radiusAxis != null) maxV = radiusAxis.maxValue;

            if (radiusAxis != null && (autoMin || autoMax))
            {
                float unit;
                switch (radiusAxis.autoRangeRounding)
                {
                    case AutoRangeRoundingMode.Integer: unit = 1f; break;
                    case AutoRangeRoundingMode.Tens: unit = 10f; break;
                    case AutoRangeRoundingMode.Hundreds: unit = 100f; break;
                    case AutoRangeRoundingMode.Custom: unit = radiusAxis.autoRangeUnit; break;
                    default: unit = 0f; break;
                }

                unit = Mathf.Abs(unit);
                if (unit > 0f)
                {
                    if (autoMin) minV = Mathf.Floor(minV / unit) * unit;
                    if (autoMax) maxV = Mathf.Ceil(maxV / unit) * unit;
                }
            }

            if (Mathf.Approximately(minV, maxV))
            {
                maxV = minV + 1f;
            }
        }

        private static float ResolveOuterRadius(float outerRadiusSetting, float maxAutoRadius)
        {
            if (maxAutoRadius <= 0f) return 0f;
            if (outerRadiusSetting <= 0f) return maxAutoRadius;
            return Mathf.Min(outerRadiusSetting, maxAutoRadius);
        }

        private static float ResolveInnerRadius(float innerRadiusSetting, float resolvedOuterRadius)
        {
            if (resolvedOuterRadius <= 0f) return 0f;
            if (innerRadiusSetting <= 0f) return 0f;
            return Mathf.Clamp(innerRadiusSetting, 0f, resolvedOuterRadius);
        }

        private static Vector2 AngleToDir(float angleDeg)
        {
            float rad = angleDeg * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        }

        private void GetPolarLayout(Serie serie, float width, float height, out Vector2 center, out float outerRadiusPx, out float innerRadiusPx, out float startAngleDeg, out bool clockwise)
        {
            var settings = serie != null ? serie.settings as RadarSettings : null;
            var layout = settings != null ? settings.radar : null;
            var plot = layout != null ? layout.plot : null;

            float padding = plot != null ? plot.padding : 0f;
            float outerRadius = layout != null ? layout.outerRadius : 0f;
            float innerRadius = layout != null ? layout.innerRadius : 0f;

            startAngleDeg = layout != null ? layout.startAngleDeg : -90f;
            clockwise = layout == null || layout.clockwise;

            Vector2 centerOffset = plot != null ? plot.centerOffset : Vector2.zero;

            float maxAutoRadius = Mathf.Max(0, Mathf.Min(width, height) * 0.5f - padding);
            outerRadiusPx = ResolveOuterRadius(outerRadius, maxAutoRadius);
            innerRadiusPx = ResolveInnerRadius(innerRadius, outerRadiusPx);

            center = new Vector2(width / 2f, height / 2f) + new Vector2(centerOffset.x, -centerOffset.y);
        }

        private static void ApplyAxisLabelStyle(Label label, PolarAxisStyle axis)
        {
            label.style.position = Position.Absolute;
            label.style.whiteSpace = WhiteSpace.NoWrap;

            if (axis != null)
            {
                var s = axis.labelStyle;
                label.style.fontSize = s != null ? s.fontSize : axis.fontSize;
                label.style.color = s != null ? s.color : axis.labelColor;
            }
        }

        private static void ApplyDataLabelStyle(Label label, SerieLabelSettings settings)
        {
            label.style.position = Position.Absolute;
            label.style.whiteSpace = WhiteSpace.NoWrap;

            if (settings != null)
            {
                label.style.fontSize = settings.fontSize;
                label.style.color = settings.color;
            }
        }

        public override void UpdateLabels()
        {
            if (!TryGetChartAndLabelController(out var chart, out var labelController)) return;

            if (Data == null) return;

            float w = contentRect.width;
            float h = contentRect.height;
            if (w <= 0 || h <= 0) return;

            var series = GetRadarSeries();
            if (series == null || series.Count == 0) return;

            var primarySerie = series[0];
            if (primarySerie == null) return;

            var labels = GetCategoryLabels();
            int dimensionCount = labels != null ? labels.Count : 0;
            if (dimensionCount <= 0)
            {
                for (int si = 0; si < series.Count; si++)
                {
                    var s = series[si];
                    if (s != null && s.seriesData != null)
                        dimensionCount = Mathf.Max(dimensionCount, s.seriesData.Count);
                }
            }
            if (dimensionCount <= 0) return;

            var angleAxis = Data.PolarAxes != null ? Data.PolarAxes.angleAxis : null;
            var radiusAxis = Data.PolarAxes != null ? Data.PolarAxes.radiusAxis : null;

            GetPolarLayout(primarySerie, w, h, out var center, out var outerRadiusPx, out var innerRadiusPx, out var startAngleDeg, out var clockwise);
            if (outerRadiusPx <= 0f) return;

            ResolveValueRange(series, radiusAxis, dimensionCount, out float minV, out float maxV);

            float step = 360f / dimensionCount;
            float sign = clockwise ? 1f : -1f;

            // 1) Dimension labels (Angle Axis)
            bool showAngleLabels = angleAxis != null && (angleAxis.labelStyle != null ? angleAxis.labelStyle.enabled : angleAxis.showLabels);
            if (angleAxis != null && angleAxis.visible && showAngleLabels)
            {
                int fontSize = angleAxis.labelStyle != null ? angleAxis.labelStyle.fontSize : angleAxis.fontSize;
                var labelColor = angleAxis.labelStyle != null ? angleAxis.labelStyle.color : angleAxis.labelColor;
                float labelRadius = outerRadiusPx + Mathf.Max(6f, fontSize * 0.6f);
                Vector2 axisOffset = angleAxis.labelStyle != null ? angleAxis.labelStyle.offset : angleAxis.labelOffset;

                for (int i = 0; i < dimensionCount; i++)
                {
                    float angle = startAngleDeg + sign * (i * step);
                    Vector2 dir = AngleToDir(angle);
                    string text = ResolveDimensionLabel(labels, primarySerie, i);

                    Vector2 pos = center + dir * labelRadius + axisOffset;

                    float ax = Mathf.Abs(dir.x);
                    float ay = Mathf.Abs(dir.y);
                    bool isVertical = ay >= ax;
                    bool isRight = dir.x > 0f;

                    ChartLabelAnchor anchor = isVertical ? ChartLabelAnchor.Center : (isRight ? ChartLabelAnchor.Left : ChartLabelAnchor.Right);
                    Vector2 anchorOffset = isVertical ? Vector2.zero : new Vector2(isRight ? 2f : -2f, 0f);

                    var desc = new LabelDescriptor
                    {
                        key = $"radar:axis:{i}",
                        text = text,
                        visible = true,
                        anchorPx = pos,
                        offsetPx = new Vector2(anchorOffset.x, -anchorOffset.y),
                        anchor = anchor,
                        role = ChartTextRole.AxisLabel,
                        zOrder = 0,
                        priority = 0,
                        clipToPlot = false,
                        fontSizeOverride = fontSize,
                        colorOverride = labelColor,
                        rotationDeg = 0f,
                    };

                    labelController.Submit(desc);
                }
            }

            // 2) Data point labels (per serie)
            for (int si = 0; si < series.Count; si++)
            {
                var s = series[si];
                if (s == null || s.seriesData == null || s.seriesData.Count == 0) continue;
                if (s.labelSettings == null || !s.labelSettings.enabled) continue;

                int serieIndex = GetRadarSerieIndex(s);
                if (serieIndex < 0) continue;

                // Use final position (animationProgress = 1) for labels to avoid position drift during animation
                var valueVertices = BuildVertices(s, dimensionCount, minV, maxV, center, outerRadiusPx, innerRadiusPx, startAngleDeg, sign, step, 1f);

                for (int i = 0; i < dimensionCount; i++)
                {
                    var dp = i < s.seriesData.Count ? s.seriesData[i] : null;
                    float y = dp != null ? dp.value : 0f;

                    string dimLabel = ResolveDimensionLabel(labels, s, i);
                    int dpPlaces = Mathf.Clamp(s.labelSettings.decimalPlaces, 0, 8);
                    string yText = (radiusAxis != null && !string.IsNullOrEmpty(radiusAxis.labelFormat))
                        ? y.ToString(radiusAxis.labelFormat)
                        : FormatAxisValue(y, null, dpPlaces);
                    string text = s.labelSettings.showName ? $"{dimLabel}: {yText}" : yText;

                    float angle = startAngleDeg + sign * (i * step);
                    Vector2 dir = AngleToDir(angle);

                    Vector2 pos = (i < valueVertices.Count) ? valueVertices[i] : center;
                    
                    // Direction from chart center to data point
                    Vector2 radialDir = (pos - center).normalized;
                    if (radialDir.sqrMagnitude < 0.001f) radialDir = dir;
                    
                    float shift = Mathf.Max(10f, s.labelSettings.fontSize * 1.2f);
                    if (s.labelSettings.position == LabelPosition.Outside) pos += radialDir * shift;
                    else if (s.labelSettings.position == LabelPosition.Inside) pos -= radialDir * shift;
                    pos += s.labelSettings.offset;

                    // Always use Center anchor for consistent positioning
                    ChartLabelAnchor anchor = ChartLabelAnchor.Center;
                    Vector2 anchorOffset = Vector2.zero;

                    var bg = s.labelSettings.background;
                    var bgColor = bg != null ? bg.color : default;
                    var bgTex = bg != null ? bg.texture : null;
                    var desc = BuildSeriesLabelDesc(
                        $"radar:{serieIndex}:{i}",
                        text,
                        pos,
                        new Vector2(anchorOffset.x, -anchorOffset.y),
                        clipToPlot: false,
                        s.labelSettings.fontSize,
                        s.labelSettings.color,
                        anchor,
                        bgColor,
                        bgTex);

                    labelController.Submit(desc);
                }
            }
        }

        public override bool GetTooltip(TooltipContext context, List<TooltipItem> items, ref Vector2? cursorPosition, ref string categoryLabel)
        {
            var hit = FindHoverHit(context);

            bool hoverChanged = false;
            if (hit.HasValue)
            {
                if (!_hover.HasValue || _hover.Value.SerieIndex != hit.Value.SerieIndex || _hover.Value.PointIndex != hit.Value.PointIndex)
                {
                    _hover = hit;
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

            if (!hit.HasValue) return false;

            if (Data == null || Data.Series == null) return false;
            if (hit.Value.SerieIndex < 0 || hit.Value.SerieIndex >= Data.Series.Count) return false;
            var serie = Data.Series[hit.Value.SerieIndex];
            if (serie == null || serie.type != SerieType.Radar || !serie.visible || serie.seriesData == null) return false;
            if (hit.Value.PointIndex < 0 || hit.Value.PointIndex >= serie.seriesData.Count) return false;

            var dp = serie.seriesData[hit.Value.PointIndex];
            if (dp == null) return false;

            var radiusAxis = Data.PolarAxes != null ? Data.PolarAxes.radiusAxis : null;

            var labels = GetCategoryLabels();
            string dimLabel = ResolveDimensionLabel(labels, serie, hit.Value.PointIndex);
            if (string.IsNullOrEmpty(categoryLabel)) categoryLabel = dimLabel;

            Color c = Color.white;
            if (serie.settings is RadarSettings rs)
            {
                if (rs.point != null) c = ResolveFillColor(rs.point.textureFill, Color.white);
                else if (rs.stroke != null) c = rs.stroke.color;
            }

            items.Add(new TooltipItem
            {
                Name = serie.name,
                Value = (radiusAxis != null && !string.IsNullOrEmpty(radiusAxis.labelFormat))
                    ? dp.value.ToString(radiusAxis.labelFormat)
                    : FormatAxisValue(dp.value, null, Mathf.Clamp(serie.labelSettings != null ? serie.labelSettings.decimalPlaces : 2, 0, 8)),
                Color = c
            });

            // For Radar/Polar charts, keep cursor line hidden.
            cursorPosition = null;
            return true;
        }

        private static List<Vector2> BuildVertices(Serie serie, int dimensionCount, float minV, float maxV, Vector2 center, float outerRadiusPx, float innerRadiusPx, float startAngleDeg, float sign, float step, float animationProgress)
        {
            var vertices = new List<Vector2>(dimensionCount);
            if (serie == null || serie.seriesData == null) return vertices;

            int valueCount = Mathf.Min(dimensionCount, serie.seriesData.Count);
            for (int i = 0; i < dimensionCount; i++)
            {
                float value = 0f;
                if (i < valueCount)
                {
                    var dp = serie.seriesData[i];
                    if (dp != null) value = dp.value;
                }

                float tt = (value - minV) / (maxV - minV);
                tt = Mathf.Clamp01(float.IsNaN(tt) ? 0f : tt);

                float rr = Mathf.Lerp(innerRadiusPx, outerRadiusPx, tt);
                rr = Mathf.Lerp(innerRadiusPx, rr, animationProgress);

                float angle = startAngleDeg + sign * (i * step);
                vertices.Add(center + AngleToDir(angle) * rr);
            }

            return vertices;
        }

        protected override void OnGenerateVisualContent(MeshGenerationContext context)
        {
            if (Data == null) return;

            float width = contentRect.width;
            float height = contentRect.height;
            if (width <= 0 || height <= 0) return;

            var series = GetRadarSeries();
            if (series == null || series.Count == 0) return;
            var primarySerie = series[0];
            if (primarySerie == null || primarySerie.seriesData == null || primarySerie.seriesData.Count == 0) return;

            var labels = GetCategoryLabels();
            int dimensionCount = labels != null ? labels.Count : 0;
            if (dimensionCount <= 0) dimensionCount = primarySerie.seriesData != null ? primarySerie.seriesData.Count : 0;
            if (dimensionCount <= 2) return;

            var angleAxis = Data.PolarAxes != null ? Data.PolarAxes.angleAxis : null;
            var radiusAxis = Data.PolarAxes != null ? Data.PolarAxes.radiusAxis : null;

            GetPolarLayout(primarySerie, width, height, out var center, out var outerRadiusPx, out var innerRadiusPx, out var startAngleDeg, out var clockwise);
            if (outerRadiusPx <= 0f) return;

            ResolveValueRange(series, radiusAxis, dimensionCount, out float minV, out float maxV);

            float step = 360f / dimensionCount;
            float sign = clockwise ? 1f : -1f;

            var painter = context.painter2D;

            // Background fill (from first serie's settings)
            var primarySettings = primarySerie.settings as RadarSettings;
            var radarLayout = primarySettings != null ? primarySettings.radar : null;
            if (radarLayout != null && radarLayout.background != null)
            {
                UnpackTextureFill(radarLayout.background, out var bgTex, out var bgTiling, out var bgOffset, out var bgColor);
                if (bgColor.a > 0f || bgTex != null)
                {
                    // Build polygon vertices
                    var bgVertices = new List<Vector2>(dimensionCount);
                    for (int i = 0; i < dimensionCount; i++)
                    {
                        float angle = startAngleDeg + sign * (i * step);
                        Vector2 p = center + AngleToDir(angle) * outerRadiusPx;
                        bgVertices.Add(p);
                    }

                    if (bgTex != null)
                    {
                        // Draw textured polygon
                        Rect bounds = new Rect(center.x - outerRadiusPx, center.y - outerRadiusPx, outerRadiusPx * 2f, outerRadiusPx * 2f);
                        DrawTexturedFan(context, bgVertices, bounds, bgTex, bgTiling, bgOffset, bgColor, true);
                    }
                    else
                    {
                        // Draw solid color polygon
                        painter.fillColor = bgColor;
                        painter.BeginPath();
                        for (int i = 0; i < bgVertices.Count; i++)
                        {
                            if (i == 0) painter.MoveTo(bgVertices[i]);
                            else painter.LineTo(bgVertices[i]);
                        }
                        painter.ClosePath();
                        painter.Fill();
                    }
                }
            }

            // Grid: polygon rings
            int ringCount = radiusAxis != null ? Mathf.Max(1, radiusAxis.splitCount) : 5;
            if (radiusAxis != null && radiusAxis.visible)
            {
                painter.lineWidth = Mathf.Max(0f, radiusAxis.width);

                for (int r = 1; r <= ringCount; r++)
                {
                    float t = r / (float)ringCount;
                    float rr = Mathf.Lerp(innerRadiusPx, outerRadiusPx, t);

                    // Use edgeColor for the outermost ring
                    bool isOuterRing = r == ringCount;
                    painter.strokeColor = isOuterRing ? radiusAxis.edgeColor : radiusAxis.color;

                    painter.BeginPath();
                    for (int i = 0; i < dimensionCount; i++)
                    {
                        float angle = startAngleDeg + sign * (i * step);
                        Vector2 p = center + AngleToDir(angle) * rr;
                        if (i == 0) painter.MoveTo(p);
                        else painter.LineTo(p);
                    }
                    painter.ClosePath();
                    painter.Stroke();
                }
            }

            // Axis spokes
            if (angleAxis != null && angleAxis.visible)
            {
                painter.strokeColor = angleAxis.color;
                painter.lineWidth = Mathf.Max(0f, angleAxis.width);

                for (int i = 0; i < dimensionCount; i++)
                {
                    float angle = startAngleDeg + sign * (i * step);
                    Vector2 dir = AngleToDir(angle);
                    Vector2 p0 = center + dir * innerRadiusPx;
                    Vector2 p1 = center + dir * outerRadiusPx;

                    painter.BeginPath();
                    painter.MoveTo(p0);
                    painter.LineTo(p1);
                    painter.Stroke();
                }
            }

            for (int si = 0; si < series.Count; si++)
            {
                var serie = series[si];
                if (serie == null || serie.seriesData == null || serie.seriesData.Count == 0) continue;
                if (serie.settings is not RadarSettings settings) continue;

                int valueCount = Mathf.Min(dimensionCount, serie.seriesData.Count);
                if (valueCount <= 2) continue;

                var vertices = BuildVertices(serie, dimensionCount, minV, maxV, center, outerRadiusPx, innerRadiusPx, startAngleDeg, sign, step, _animationProgress);

                // Fill
                if (settings.area != null && settings.area.show)
                {
                    UnpackTextureFill(settings.area.textureFill, out var _areaTex, out var _areaTiling, out var _areaOffset, out var _areaColor);
                    painter.fillColor = _areaColor;
                    painter.BeginPath();
                    for (int i = 0; i < vertices.Count; i++)
                    {
                        if (i == 0) painter.MoveTo(vertices[i]);
                        else painter.LineTo(vertices[i]);
                    }
                    painter.ClosePath();
                    painter.Fill();
                }

                // Stroke
                if (settings.stroke != null)
                {
                    painter.strokeColor = settings.stroke.color;
                    painter.lineWidth = Mathf.Max(0f, settings.stroke.width);
                    painter.BeginPath();
                    for (int i = 0; i < vertices.Count; i++)
                    {
                        if (i == 0) painter.MoveTo(vertices[i]);
                        else painter.LineTo(vertices[i]);
                    }
                    painter.ClosePath();
                    painter.Stroke();
                }

                // Points
                if (settings.point != null && settings.point.show)
                {
                    float size = Mathf.Max(0f, settings.point.size);
                    float pr = size * 0.5f;
                    int serieIndex = GetRadarSerieIndex(serie);

                    for (int i = 0; i < vertices.Count; i++)
                    {
                        var pos = vertices[i];

                        float localPr = pr;
                        if (_hover.HasValue && _hover.Value.SerieIndex == serieIndex && _hover.Value.PointIndex == i)
                        {
                            localPr *= 1.6f;
                        }

                        DrawPointMarker(context, painter, pos, localPr, settings.point.textureFill, Color.white);
                    }
                }
            }
        }
    }
}
