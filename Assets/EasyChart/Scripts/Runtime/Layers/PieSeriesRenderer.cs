using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using EasyChart;

namespace EasyChart.Layers
{
    public class PieSeriesRenderer : BaseSeriesRenderer, IExclusiveTooltipRenderer, IChartInteractionStateConsumer
    {
        public const string OthersSliceId = "__ec_pie_others__";

        public ISet<string> HiddenSliceIds { get; set; }

        void IChartInteractionStateConsumer.SetInteractionState(ChartInteractionState state)
        {
            HiddenSliceIds = state != null ? state.HiddenPieSliceIds : null;
        }

        private struct LeaderLine
        {
            public Vector2 A;
            public Vector2 B;
            public Vector2 C;
            public Color Color;
            public float Width;
        }

        private readonly List<LeaderLine> _leaderLines = new List<LeaderLine>();

        private struct PieSlice
        {
            public int SourceIndex;
            public float Value;
            public string Name;
            public Color Color;
            public bool IsOthers;
        }

        private readonly List<PieSlice> _tmpSlices = new List<PieSlice>(32);

        // Simple palette for slices since we don't have per-point color yet
        public static readonly Color[] Palette = new Color[]
        {
            new Color(0.2f, 0.6f, 1.0f), // Blue
            new Color(1.0f, 0.4f, 0.4f), // Red
            new Color(0.4f, 0.8f, 0.4f), // Green
            new Color(1.0f, 0.8f, 0.2f), // Yellow
            new Color(0.8f, 0.4f, 1.0f), // Purple
            new Color(0.4f, 0.9f, 0.9f), // Cyan
            new Color(1.0f, 0.6f, 0.2f), // Orange
            new Color(0.6f, 0.6f, 0.6f)  // Grey
        };
        
        // Private alias for internal use if needed, or just replace usages
        private Color[] _palette => Palette;

        private int? _hoverSliceIndex;

        private static bool UsesExplodeDistance(PieExplodeType type)
        {
            return type == PieExplodeType.Translate || type == PieExplodeType.Pull;
        }

        private static Color GetHoverFillColor(Color baseColor)
        {
            Color c = Color.Lerp(baseColor, Color.white, 0.25f);
            c.a = baseColor.a;
            return c;
        }

        private static Color GetHoverStrokeColor(Color baseColor)
        {
            Color c = Color.Lerp(baseColor, Color.white, 0.6f);
            c.a = Mathf.Clamp01(baseColor.a + 0.2f);
            return c;
        }

        private static void StrokePieShape(Painter2D painter, Vector2 outerOrigin, Vector2 innerOrigin, float outerRadius, float innerRadius, float startAngleDeg, float endAngleDeg, bool clockwise)
        {
            if (outerRadius <= 0.001f) return;

            float sweep = endAngleDeg - startAngleDeg;
            if (Mathf.Abs(sweep) <= 0.001f) return;

            bool isFullCircle = Mathf.Abs(Mathf.Abs(sweep) - 360f) <= 0.01f;
            if (isFullCircle)
            {
                painter.BeginPath();
                painter.Arc(outerOrigin, outerRadius, 0f, 360f);
                painter.Stroke();
                if (innerRadius > 0.001f)
                {
                    painter.BeginPath();
                    painter.Arc(innerOrigin, innerRadius, 0f, 360f);
                    painter.Stroke();
                }
                return;
            }

            float startRad = startAngleDeg * Mathf.Deg2Rad;
            float endRad = endAngleDeg * Mathf.Deg2Rad;
            Vector2 outerStart = outerOrigin + new Vector2(Mathf.Cos(startRad), Mathf.Sin(startRad)) * outerRadius;
            Vector2 innerEnd = innerOrigin + new Vector2(Mathf.Cos(endRad), Mathf.Sin(endRad)) * innerRadius;
            Vector2 innerStart = innerOrigin + new Vector2(Mathf.Cos(startRad), Mathf.Sin(startRad)) * innerRadius;

            int outerSegments = Mathf.Clamp(Mathf.CeilToInt(Mathf.Abs(sweep) / 8f), 2, 96);
            int innerSegments = outerSegments;

            painter.BeginPath();
            painter.MoveTo(outerStart);
            LineArc(painter, outerOrigin, outerRadius, startAngleDeg, sweep, outerSegments);

            if (innerRadius > 0.001f)
            {
                painter.LineTo(innerEnd);
                LineArc(painter, innerOrigin, innerRadius, endAngleDeg, -sweep, innerSegments);
                painter.LineTo(innerStart);
            }
            else
            {
                painter.LineTo(innerOrigin);
            }

            painter.ClosePath();
            painter.Stroke();
        }

        private static float ResolveOuterRadius(float outerRadiusSetting, float maxAutoRadius)
        {
            if (maxAutoRadius <= 0f) return 0f;
            if (outerRadiusSetting <= 0f) return maxAutoRadius;
            if (outerRadiusSetting <= 1f) return Mathf.Clamp01(outerRadiusSetting) * maxAutoRadius;
            return Mathf.Min(outerRadiusSetting, maxAutoRadius);
        }

        private static float ResolveInnerRadius(float innerRadiusSetting, float resolvedOuterRadius)
        {
            if (resolvedOuterRadius <= 0f) return 0f;
            if (innerRadiusSetting <= 0f) return 0f;

            float inner = innerRadiusSetting <= 1f
                ? Mathf.Clamp01(innerRadiusSetting) * resolvedOuterRadius
                : innerRadiusSetting;

            return Mathf.Clamp(inner, 0f, resolvedOuterRadius);
        }

        private static float ResolveRoundedCapRadius(float roundedCapSetting, float thickness)
        {
            if (thickness <= 0f) return 0f;
            if (roundedCapSetting <= 0f) return 0f;

            // roundedCap is always in pixels.
            return Mathf.Clamp(roundedCapSetting, 0f, thickness * 0.5f);
        }

        private struct PieCandidate
        {
            public int SourceIndex;
            public float Value;
            public SeriesData Data;
        }

        private readonly List<PieCandidate> _tmpCandidates = new List<PieCandidate>(32);

        private static readonly System.Comparison<PieCandidate> s_compareCandidateValueDesc = CompareCandidateValueDesc;

        private static int CompareCandidateValueDesc(PieCandidate a, PieCandidate b)
        {
            return b.Value.CompareTo(a.Value);
        }

        private List<PieSlice> BuildSlices(Serie serie, PieSettings settings)
        {
            _tmpSlices.Clear();
            var slices = _tmpSlices;
            if (serie == null || serie.seriesData == null || serie.seriesData.Count == 0) return slices;

            // For Pie coordinate system, skip axis labels fallback - only use SeriesData.name
            bool isNoneCoord = Data != null && Data.CoordinateSystem == CoordinateSystemType.None;
            List<string> labels = null;
            if (!isNoneCoord && Data != null && Data.Axes != null)
            {
                AxisId preferredAxisId = Data.Cartesian != null ? Data.Cartesian.xAxisId : AxisId.XBottom;
                for (int j = 0; j < Data.Axes.Count; j++)
                {
                    var a = Data.Axes[j];
                    if (a != null && a.id == preferredAxisId && a.axisType == AxisType.Category)
                    {
                        labels = a.labels;
                        break;
                    }
                }

                if (labels == null)
                {
                    for (int j = 0; j < Data.Axes.Count; j++)
                    {
                        var a = Data.Axes[j];
                        if (a != null && a.axisType == AxisType.Category)
                        {
                            labels = a.labels;
                            break;
                        }
                    }
                }
            }

            _tmpCandidates.Clear();
            var candidates = _tmpCandidates;
            for (int i = 0; i < serie.seriesData.Count; i++)
            {
                var dp = serie.seriesData[i];
                if (dp == null) continue;
                if (dp.value <= 0) continue;
                if (HiddenSliceIds != null)
                {
                    string sliceId = !string.IsNullOrEmpty(dp.id) ? dp.id : FormatIntCached(i);
                    if (HiddenSliceIds.Contains(sliceId)) continue;
                }

                candidates.Add(new PieCandidate
                {
                    SourceIndex = i,
                    Value = dp.value,
                    Data = dp
                });
            }

            if (candidates.Count == 0) return slices;

            bool doAgg = settings != null && settings.aggregation != null && settings.aggregation.enabled;
            int keepTopN = doAgg ? Mathf.Max(0, settings.aggregation.keepTopN) : 0;
            bool sortByValue = settings != null && settings.sortByValue;

            if (sortByValue)
            {
                candidates.Sort(s_compareCandidateValueDesc);
            }

            float othersValue = 0f;
            int limit = (doAgg && keepTopN > 0) ? Mathf.Min(keepTopN, candidates.Count) : candidates.Count;

            for (int i = 0; i < candidates.Count; i++)
            {
                if (i < limit)
                {
                    var c = candidates[i];
                    string name = null;
                    if (c.Data != null && !string.IsNullOrEmpty(c.Data.name)) name = c.Data.name;
                    else if (labels != null && c.SourceIndex < labels.Count) name = labels[c.SourceIndex];
                    if (string.IsNullOrEmpty(name)) name = $"Slice {c.SourceIndex}";

                    Color color = (c.Data != null && c.Data.useColor) ? c.Data.color : _palette[slices.Count % _palette.Length];

                    slices.Add(new PieSlice
                    {
                        SourceIndex = c.SourceIndex,
                        Value = c.Value,
                        Name = name,
                        Color = color,
                        IsOthers = false
                    });
                }
                else
                {
                    othersValue += candidates[i].Value;
                }
            }

            if (doAgg && keepTopN > 0 && candidates.Count > keepTopN && othersValue > 0f)
            {
                if (HiddenSliceIds != null && HiddenSliceIds.Contains(OthersSliceId))
                {
                    return slices;
                }

                string name = settings != null && settings.aggregation != null && !string.IsNullOrEmpty(settings.aggregation.othersName)
                    ? settings.aggregation.othersName
                    : "Others";

                Color color = _palette[slices.Count % _palette.Length];
                if (settings != null && settings.aggregation != null && settings.aggregation.useOthersColor) color = settings.aggregation.othersColor;

                slices.Add(new PieSlice
                {
                    SourceIndex = -1,
                    Value = othersValue,
                    Name = name,
                    Color = color,
                    IsOthers = true
                });
            }

            return slices;
        }

        public override void ClearHover()
        {
            if (_hoverSliceIndex != null)
            {
                _hoverSliceIndex = null;
                MarkDirtyRepaint();
            }
        }

        protected override void OnGenerateVisualContent(MeshGenerationContext context)
        {
            if (Data == null || Data.Series == null) return;

            float width = contentRect.width;
            float height = contentRect.height;
            if (width <= 0 || height <= 0) return;

            var painter = context.painter2D;

            // For now, render the first visible Pie series
            foreach (var serie in Data.Series)
            {
                if (!serie.visible || serie.type != SerieType.Pie) continue;
                if (serie.seriesData == null || serie.seriesData.Count == 0) continue;

                // Handle Pie with PieSettings
                if (!(serie.settings is PieSettings settings)) continue;

                var layout = settings != null ? settings.layout : null;

                float padding = layout != null && layout.plot != null ? layout.plot.padding : 0f;
                float outerRadius = layout != null ? layout.outerRadius : 0f;
                float innerRadius = layout != null ? layout.innerRadius : 0f;

                float startAngleDeg = layout != null ? layout.startAngleDeg : -90f;
                bool clockwise = layout != null ? layout.clockwise : true;
                float angleRangeDeg = layout != null ? layout.angleRangeDeg : 360f;

                Vector2 centerOffset = layout != null && layout.plot != null ? layout.plot.centerOffset : Vector2.zero;
                bool hoverEnabled = settings != null && settings.hover != null && settings.hover.enabled;
                PieExplodeType explodeType = (settings != null && settings.hover != null) ? settings.hover.explodeType : PieExplodeType.Translate;
                float explodeDistance = hoverEnabled && UsesExplodeDistance(explodeType) ? Mathf.Max(0f, settings.hover.explodeDistance) : 0f;

                // Reserve explode distance so hovered slice won't be clipped by chart bounds/padding.
                float maxAutoRadius = Mathf.Max(0, Mathf.Min(width, height) * 0.5f - padding - explodeDistance);
                float radius = ResolveOuterRadius(outerRadius, maxAutoRadius);
                float innerRadiusPx = ResolveInnerRadius(innerRadius, radius);
                Vector2 center = new Vector2(width / 2f, height / 2f) + new Vector2(centerOffset.x, -centerOffset.y);

                DrawPieSerie(painter, serie, settings, center, radius, innerRadiusPx, startAngleDeg, clockwise, angleRangeDeg);

                if (serie.labelSettings != null && serie.labelSettings.enabled && serie.labelSettings.position == LabelPosition.Outside && _leaderLines.Count > 0)
                {
                    // Draw leader lines for outside labels.
                    painter.lineWidth = 1f;
                    for (int i = 0; i < _leaderLines.Count; i++)
                    {
                        var l = _leaderLines[i];
                        painter.strokeColor = l.Color;
                        painter.lineWidth = l.Width;
                        painter.BeginPath();
                        painter.MoveTo(l.A);
                        painter.LineTo(l.B);
                        painter.LineTo(l.C);
                        painter.Stroke();
                    }
                }

                // Only draw one pie series for now to avoid overlapping mess
                break;
            }
        }

        public override void UpdateLabels()
        {
            if (!TryGetChartAndLabelController(out var chart, out var labelController)) return;

            _leaderLines.Clear();

            if (Data == null || Data.Series == null) return;

            float width = contentRect.width;
            float height = contentRect.height;
            if (width <= 0 || height <= 0) return;

            // Only handle the first visible pie series (consistent with drawing)
            foreach (var serie in Data.Series)
            {
                if (!serie.visible || serie.type != SerieType.Pie) continue;
                if (serie.seriesData == null || serie.seriesData.Count == 0) continue;
                if (!(serie.settings is PieSettings settings)) continue;
                if (serie.labelSettings == null || !serie.labelSettings.enabled) break;

                var layout = settings != null ? settings.layout : null;

                float padding = layout != null && layout.plot != null ? layout.plot.padding : 0f;
                float outerRadius = layout != null ? layout.outerRadius : 0f;
                float innerRadius = layout != null ? layout.innerRadius : 0f;

                float startAngleDeg = layout != null ? layout.startAngleDeg : -90f;
                bool clockwise = layout != null ? layout.clockwise : true;
                float angleRangeDeg = layout != null ? layout.angleRangeDeg : 360f;

                Vector2 centerOffset = layout != null && layout.plot != null ? layout.plot.centerOffset : Vector2.zero;
                bool hoverEnabled = settings != null && settings.hover != null && settings.hover.enabled;
                PieExplodeType explodeType = (settings != null && settings.hover != null) ? settings.hover.explodeType : PieExplodeType.Translate;
                float explodeDistance = hoverEnabled && UsesExplodeDistance(explodeType) ? Mathf.Max(0f, settings.hover.explodeDistance) : 0f;

                float maxAutoRadius = Mathf.Max(0, Mathf.Min(width, height) * 0.5f - padding - explodeDistance);
                float radius = ResolveOuterRadius(outerRadius, maxAutoRadius);
                float innerRadiusPx = ResolveInnerRadius(innerRadius, radius);
                Vector2 center = new Vector2(width / 2f, height / 2f) + new Vector2(centerOffset.x, -centerOffset.y);

                if (radius <= 0f) break;

                var slices = BuildSlices(serie, settings);
                if (slices == null || slices.Count == 0) break;

                float total = 0f;
                for (int i = 0; i < slices.Count; i++) total += Mathf.Max(0f, slices[i].Value);
                if (total <= 0f) break;

                float fontSize = Mathf.Max(1, serie.labelSettings.fontSize);
                float estimatedLineHeight = fontSize + 4f;

                // Leader line geometry
                float radialLen = Mathf.Max(6f, fontSize * 0.6f);
                float labelPad = 8f;
                float horizLen = Mathf.Max(20f, fontSize * 2.0f);

                // Create labels + leader lines
                float currentAngle = startAngleDeg;
                for (int i = 0; i < slices.Count; i++)
                {
                    var slice = slices[i];
                    if (slice.Value <= 0) continue;

                    float sliceAngle = (slice.Value / total) * angleRangeDeg;
                    float sliceGapPx = layout != null ? Mathf.Max(0f, layout.sliceGapPx) : 0f;
                    SliceGapType labelGapType = layout != null ? layout.sliceGapType : SliceGapType.Radial;
                    float gapAngle = (labelGapType == SliceGapType.Radial && sliceGapPx > 0f && radius > 0.001f) ? (sliceGapPx / radius) * Mathf.Rad2Deg : 0f;
                    float halfGap = gapAngle * 0.5f;
                    bool wrapGaps = Mathf.Abs(angleRangeDeg) >= 359.99f;
                    float shrink = wrapGaps
                        ? gapAngle
                        : ((i == 0 || i == slices.Count - 1) ? halfGap : gapAngle);
                    float beforeGap = (i == 0 && !wrapGaps) ? 0f : halfGap;
                    float drawAngle = Mathf.Max(0f, sliceAngle - shrink);

                    float midAngle = clockwise
                        ? ((currentAngle + beforeGap) + drawAngle * 0.5f)
                        : ((currentAngle - beforeGap) - drawAngle * 0.5f);
                    float midRad = midAngle * Mathf.Deg2Rad;
                    Vector2 dir = new Vector2(Mathf.Cos(midRad), Mathf.Sin(midRad));
                    bool isRight = dir.x >= 0f;

                    float labelTranslateOffset = (labelGapType == SliceGapType.Translate) ? sliceGapPx * 0.5f : 0f;
                    float labelUniformShrink = (labelGapType == SliceGapType.Uniform) ? sliceGapPx * 0.5f : 0f;
                    Vector2 labelCenter = center + dir * labelTranslateOffset;
                    float labelRadius = radius - labelUniformShrink;
                    float labelInnerRadius = innerRadiusPx + labelUniformShrink;

                    bool showName = serie.labelSettings.showName;
                    string nameText = slice.Name;
                    int dpPlaces = Mathf.Clamp(serie.labelSettings != null ? serie.labelSettings.decimalPlaces : 2, 0, 8);
                    string valueText = FormatAxisValue(slice.Value, null, dpPlaces);
                    var bg = serie.labelSettings.background;
                    var bgColor = bg != null ? bg.color : default;
                    var bgTex = bg != null ? bg.texture : null;

                    if (serie.labelSettings.position == LabelPosition.Center)
                    {
                        float labelR = labelInnerRadius > 0 ? (labelInnerRadius + labelRadius) * 0.5f : labelRadius * 0.5f;
                        Vector2 finalPos = labelCenter + dir * labelR + serie.labelSettings.offset;

                        if (showName)
                        {
                            float nameY = finalPos.y - estimatedLineHeight * 0.5f;
                            float valueY = finalPos.y + estimatedLineHeight * 0.5f;

                            var descName = BuildSeriesLabelDesc(
                                $"pie:{i}:name",
                                nameText,
                                new Vector2(finalPos.x, nameY),
                                Vector2.zero,
                                clipToPlot: false,
                                (int)fontSize,
                                serie.labelSettings.color,
                                ChartLabelAnchor.Center,
                                bgColor,
                                bgTex);
                            labelController.Submit(descName);

                            var descValue = BuildSeriesLabelDesc(
                                $"pie:{i}:value",
                                valueText,
                                new Vector2(finalPos.x, valueY),
                                Vector2.zero,
                                clipToPlot: false,
                                (int)fontSize,
                                serie.labelSettings.color,
                                ChartLabelAnchor.Center,
                                bgColor,
                                bgTex);
                            labelController.Submit(descValue);
                        }
                        else
                        {
                            var descValue = BuildSeriesLabelDesc(
                                $"pie:{i}:value",
                                valueText,
                                finalPos,
                                Vector2.zero,
                                clipToPlot: false,
                                (int)fontSize,
                                serie.labelSettings.color,
                                ChartLabelAnchor.Center,
                                bgColor,
                                bgTex);
                            labelController.Submit(descValue);
                        }
                    }
                    else
                    {
                        // Outside with fixed leader segments: A->B radial, B->C horizontal.
                        Vector2 p0 = labelCenter + dir * labelRadius;
                        Vector2 p1 = labelCenter + dir * (labelRadius + radialLen);
                        Vector2 p2 = new Vector2(p1.x + (isRight ? horizLen : -horizLen), p1.y);

                        float y = p2.y;
                        float x = p2.x + (isRight ? labelPad : -labelPad);

                        ChartLabelAnchor anchor = isRight ? ChartLabelAnchor.Left : ChartLabelAnchor.Right;

                        if (showName)
                        {
                            float nameY = y - estimatedLineHeight * 0.5f;
                            float valueY = y + estimatedLineHeight * 0.5f;

                            var descName = BuildSeriesLabelDesc(
                                $"pie:{i}:name",
                                nameText,
                                new Vector2(x, nameY),
                                Vector2.zero,
                                clipToPlot: false,
                                (int)fontSize,
                                serie.labelSettings.color,
                                anchor,
                                bgColor,
                                bgTex);
                            labelController.Submit(descName);

                            var descValue = BuildSeriesLabelDesc(
                                $"pie:{i}:value",
                                valueText,
                                new Vector2(x, valueY),
                                Vector2.zero,
                                clipToPlot: false,
                                (int)fontSize,
                                serie.labelSettings.color,
                                anchor,
                                bgColor,
                                bgTex);
                            labelController.Submit(descValue);
                        }
                        else
                        {
                            var descValue = BuildSeriesLabelDesc(
                                $"pie:{i}:value",
                                valueText,
                                new Vector2(x, y),
                                Vector2.zero,
                                clipToPlot: false,
                                (int)fontSize,
                                serie.labelSettings.color,
                                anchor,
                                bgColor,
                                bgTex);
                            labelController.Submit(descValue);
                        }

                        _leaderLines.Add(new LeaderLine
                        {
                            A = p0,
                            B = p1,
                            C = p2,
                            Color = new Color(serie.labelSettings.color.r, serie.labelSettings.color.g, serie.labelSettings.color.b, Mathf.Clamp01(serie.labelSettings.color.a * 0.8f)),
                            Width = 1f
                        });
                    }

                    currentAngle = clockwise ? (currentAngle + sliceAngle) : (currentAngle - sliceAngle);
                }

                break;
            }
        }

        private void DrawPieSerie(Painter2D painter, Serie serie, PieSettings settings, Vector2 center, float radius, float innerRadius, float startAngleDeg, bool clockwise, float angleRangeDeg)
        {
            var slices = BuildSlices(serie, settings);
            if (slices == null || slices.Count == 0) return;

            float total = 0f;
            for (int i = 0; i < slices.Count; i++)
            {
                total += Mathf.Max(0f, slices[i].Value);
            }

            if (total <= 0) return;

            if (radius <= 0f) return;

            float currentAngle = startAngleDeg;
            
            float maxAngle = angleRangeDeg * _animationProgress;
            float accumulatedAngle = 0f;

            bool hoverEnabled = settings != null && settings.hover != null && settings.hover.enabled;
            PieExplodeType explodeType = (settings != null && settings.hover != null) ? settings.hover.explodeType : PieExplodeType.Translate;
            float explodeDistance = hoverEnabled && UsesExplodeDistance(explodeType) ? Mathf.Max(0f, settings.hover.explodeDistance) : 0f;

            float roundedCapSetting = 0f;
            if (settings != null && settings.layout != null)
            {
                roundedCapSetting = settings.layout.cornerRadius;
            }
            float thickness = Mathf.Max(0f, radius - Mathf.Max(0f, innerRadius));
            float capRadius = ResolveRoundedCapRadius(roundedCapSetting, thickness);

            float sliceGapPx = settings != null && settings.layout != null ? Mathf.Max(0f, settings.layout.sliceGapPx) : 0f;
            if (slices.Count <= 1) sliceGapPx = 0f;
            SliceGapType gapType = settings != null && settings.layout != null ? settings.layout.sliceGapType : SliceGapType.Radial;
            float gapAngle = (gapType == SliceGapType.Radial && sliceGapPx > 0f && radius > 0.001f) ? (sliceGapPx / radius) * Mathf.Rad2Deg : 0f;
            float halfGap = gapAngle * 0.5f;
            bool wrapGaps = Mathf.Abs(angleRangeDeg) >= 359.99f;
            float translateGapOffset = (gapType == SliceGapType.Translate) ? sliceGapPx * 0.5f : 0f;
            float uniformGapShrink = (gapType == SliceGapType.Uniform) ? sliceGapPx * 0.5f : 0f;

            for (int i = 0; i < slices.Count; i++)
            {
                var slice = slices[i];
                if (slice.Value <= 0) continue;

                float sliceAngle = (slice.Value / total) * angleRangeDeg;
                
                // Check animation bounds
                if (accumulatedAngle >= maxAngle) break;
                
                float visibleSliceAngle = sliceAngle;
                if (accumulatedAngle + sliceAngle > maxAngle)
                {
                    visibleSliceAngle = maxAngle - accumulatedAngle;
                }
                
                bool isHovered = hoverEnabled && _hoverSliceIndex.HasValue && _hoverSliceIndex.Value == i;
                bool hoverColor = isHovered && explodeType == PieExplodeType.Color;
                bool hoverStroke = isHovered && explodeType == PieExplodeType.Stroke;

                Color color = hoverColor ? GetHoverFillColor(slice.Color) : slice.Color;
                painter.fillColor = color;
                float endAngle = clockwise
                    ? (currentAngle + visibleSliceAngle)
                    : (currentAngle - visibleSliceAngle);

                float shrink = wrapGaps
                    ? gapAngle
                    : ((i == 0 || i == slices.Count - 1) ? halfGap : gapAngle);
                float beforeGap = (i == 0 && !wrapGaps) ? 0f : halfGap;
                float drawAngle = Mathf.Max(0f, visibleSliceAngle - shrink);
                float drawStartAngle = clockwise ? (currentAngle + beforeGap) : (currentAngle - beforeGap);
                float drawEndAngle = clockwise ? (drawStartAngle + drawAngle) : (drawStartAngle - drawAngle);

                if (drawAngle <= 0f)
                {
                    currentAngle = endAngle;
                    accumulatedAngle += visibleSliceAngle;
                    continue;
                }

                float sliceMidAngle = clockwise
                    ? (drawStartAngle + drawAngle * 0.5f)
                    : (drawStartAngle - drawAngle * 0.5f);
                float sliceMidRad = sliceMidAngle * Mathf.Deg2Rad;
                Vector2 sliceMidDir = new Vector2(Mathf.Cos(sliceMidRad), Mathf.Sin(sliceMidRad));

                Vector2 sliceCenter = center;
                float sliceRadius = radius;
                float sliceInnerRadius = innerRadius;

                if (gapType == SliceGapType.Translate && translateGapOffset > 0f)
                {
                    sliceCenter = center + sliceMidDir * translateGapOffset;
                }
                else if (gapType == SliceGapType.Uniform && uniformGapShrink > 0f)
                {
                    sliceRadius = Mathf.Max(0f, radius - uniformGapShrink);
                    sliceInnerRadius = Mathf.Max(0f, innerRadius + uniformGapShrink);
                }

                if (explodeDistance > 0f && _hoverSliceIndex.HasValue && _hoverSliceIndex.Value == i)
                {
                    sliceCenter = sliceCenter + sliceMidDir * explodeDistance;
                }

                // Painter2D.Arc can produce degenerate geometry when sweeping a full circle.
                // This happens frequently when only one slice is visible (e.g. after legend toggles).
                if (Mathf.Abs(visibleSliceAngle - angleRangeDeg) <= 0.01f)
                {
                    painter.BeginPath();
                    if (innerRadius > 0)
                    {
                        // Exploding a full donut ring looks odd (it becomes a whole ring translated).
                        // Keep it centered regardless of explode type.
                        Vector2 outerStart = center + Vector2.right * radius;
                        Vector2 innerStart = center + Vector2.right * innerRadius;

                        painter.MoveTo(outerStart);
                        // Avoid Painter2D.Arc here: reverse sweeps can fill the whole disk and cause flicker.
                        LineArc(painter, center, radius, 0f, 360f, 128);
                        painter.LineTo(innerStart);
                        LineArc(painter, center, innerRadius, 0f, -360f, 128);
                        painter.ClosePath();
                    }
                    else
                    {
                        painter.Arc(sliceCenter, radius, 0, 360);
                        painter.ClosePath();
                    }
                    painter.Fill();

                    if (hoverStroke)
                    {
                        painter.strokeColor = GetHoverStrokeColor(slice.Color);
                        painter.lineWidth = 2f;
                        StrokePieShape(painter, center, center, radius, innerRadius, 0f, 360f, true);
                    }

                    // Full circle consumes the whole range.
                    currentAngle = endAngle;
                    accumulatedAngle += visibleSliceAngle;
                    break;
                }

                bool didRoundedDonut = false;
                if (sliceInnerRadius > 0f && capRadius > 0f)
                {
                    // Rounded-corner donut requires concentric geometry.
                    // When explodeType == Pull and the hovered slice is exploded, outer/inner boundaries are not
                    // concentric, so fillet math would be invalid. Fall back to the non-rounded path in that case.
                    bool isExplodedPullSlice = explodeType == PieExplodeType.Pull
                        && explodeDistance > 0f
                        && _hoverSliceIndex.HasValue
                        && _hoverSliceIndex.Value == i;

                    if (!isExplodedPullSlice)
                    {
                        if (hoverStroke)
                        {
                            painter.strokeColor = GetHoverStrokeColor(slice.Color);
                            painter.lineWidth = 2f;
                        }
                        didRoundedDonut = TryDrawRoundedDonutSlice(painter, sliceCenter, sliceRadius, sliceInnerRadius, drawStartAngle, drawEndAngle, clockwise, capRadius, hoverStroke);
                    }
                }

                bool didRoundedPie = false;
                if (!didRoundedDonut && sliceInnerRadius <= 0f && capRadius > 0f)
                {
                    // For regular pie (no inner radius), rounded corners work for all hover types except Pull
                    // (Pull moves the slice center, making the geometry non-concentric with the origin).
                    bool isExplodedPullSlice = explodeType == PieExplodeType.Pull
                        && explodeDistance > 0f
                        && _hoverSliceIndex.HasValue
                        && _hoverSliceIndex.Value == i;

                    if (!isExplodedPullSlice)
                    {
                        if (hoverStroke)
                        {
                            painter.strokeColor = GetHoverStrokeColor(slice.Color);
                            painter.lineWidth = 2f;
                        }
                        didRoundedPie = TryDrawRoundedPieSlice(painter, sliceCenter, sliceRadius, drawStartAngle, drawEndAngle, clockwise, capRadius, hoverStroke);
                    }
                }

                if (!didRoundedDonut && !didRoundedPie)
                {
                    painter.BeginPath();
                    if (sliceInnerRadius > 0)
                    {
                        if (explodeType == PieExplodeType.Translate)
                        {
                            // Proper donut sector (no center mask needed). Translate both outer and inner arcs.
                            float startRad = drawStartAngle * Mathf.Deg2Rad;
                            float endRad = drawEndAngle * Mathf.Deg2Rad;
                            Vector2 outerStart = sliceCenter + new Vector2(Mathf.Cos(startRad), Mathf.Sin(startRad)) * sliceRadius;
                            Vector2 innerEnd = sliceCenter + new Vector2(Mathf.Cos(endRad), Mathf.Sin(endRad)) * sliceInnerRadius;
                            Vector2 innerStart = sliceCenter + new Vector2(Mathf.Cos(startRad), Mathf.Sin(startRad)) * sliceInnerRadius;

                            painter.MoveTo(outerStart);
                            int outerSegments = Mathf.Clamp(Mathf.CeilToInt(Mathf.Abs(drawAngle) / 8f), 2, 64);
                            float outerSweep = clockwise ? drawAngle : -drawAngle;
                            LineArc(painter, sliceCenter, sliceRadius, drawStartAngle, outerSweep, outerSegments);
                            painter.LineTo(innerEnd);
                            int innerSegments = Mathf.Clamp(Mathf.CeilToInt(Mathf.Abs(drawAngle) / 8f), 2, 64);
                            float innerSweep = clockwise ? -drawAngle : drawAngle;
                            LineArc(painter, sliceCenter, sliceInnerRadius, drawEndAngle, innerSweep, innerSegments);
                            painter.LineTo(innerStart);
                        }
                        else
                        {
                            // Pull: keep the inner boundary anchored to the original center so the donut hole doesn't
                            // look like it is being dragged out during hover.
                            float startRad = drawStartAngle * Mathf.Deg2Rad;
                            float endRad = drawEndAngle * Mathf.Deg2Rad;
                            Vector2 outerStart = sliceCenter + new Vector2(Mathf.Cos(startRad), Mathf.Sin(startRad)) * sliceRadius;
                            Vector2 innerEnd = center + new Vector2(Mathf.Cos(endRad), Mathf.Sin(endRad)) * sliceInnerRadius;
                            Vector2 innerStart = center + new Vector2(Mathf.Cos(startRad), Mathf.Sin(startRad)) * sliceInnerRadius;

                            painter.MoveTo(outerStart);
                            int outerSegments = Mathf.Clamp(Mathf.CeilToInt(Mathf.Abs(drawAngle) / 8f), 2, 64);
                            float outerSweep = clockwise ? drawAngle : -drawAngle;
                            LineArc(painter, sliceCenter, sliceRadius, drawStartAngle, outerSweep, outerSegments);
                            painter.LineTo(innerEnd);
                            int innerSegments = Mathf.Clamp(Mathf.CeilToInt(Mathf.Abs(drawAngle) / 8f), 2, 64);
                            float innerSweep = clockwise ? -drawAngle : drawAngle;
                            LineArc(painter, center, sliceInnerRadius, drawEndAngle, innerSweep, innerSegments);
                            painter.LineTo(innerStart);
                        }
                        painter.ClosePath();
                    }
                    else
                    {
                        if (explodeType == PieExplodeType.Translate)
                        {
                            // Rigid translation: the whole slice (including its center) is moved.
                            painter.MoveTo(sliceCenter);
                            Vector2 outerStart = sliceCenter + new Vector2(Mathf.Cos(drawStartAngle * Mathf.Deg2Rad), Mathf.Sin(drawStartAngle * Mathf.Deg2Rad)) * sliceRadius;
                            painter.LineTo(outerStart);
                            int outerSegments = Mathf.Clamp(Mathf.CeilToInt(Mathf.Abs(drawAngle) / 8f), 2, 64);
                            float outerSweep = clockwise ? drawAngle : -drawAngle;
                            LineArc(painter, sliceCenter, sliceRadius, drawStartAngle, outerSweep, outerSegments);
                            painter.ClosePath();
                        }
                        else
                        {
                            // Pull: keep the slice center anchored, but pull the outer arc outward.
                            painter.MoveTo(center);
                            Vector2 outerStart = sliceCenter + new Vector2(Mathf.Cos(drawStartAngle * Mathf.Deg2Rad), Mathf.Sin(drawStartAngle * Mathf.Deg2Rad)) * sliceRadius;
                            painter.LineTo(outerStart);
                            int outerSegments = Mathf.Clamp(Mathf.CeilToInt(Mathf.Abs(drawAngle) / 8f), 2, 64);
                            float outerSweep = clockwise ? drawAngle : -drawAngle;
                            LineArc(painter, sliceCenter, sliceRadius, drawStartAngle, outerSweep, outerSegments);
                            painter.ClosePath();
                        }
                    }
                    
                    painter.Fill();
                }

                if (hoverStroke && !didRoundedDonut && !didRoundedPie)
                {
                    painter.strokeColor = GetHoverStrokeColor(slice.Color);
                    painter.lineWidth = 2f;

                    if (sliceInnerRadius > 0)
                    {
                        if (explodeType == PieExplodeType.Translate)
                        {
                            StrokePieShape(painter, sliceCenter, sliceCenter, sliceRadius, sliceInnerRadius, drawStartAngle, drawEndAngle, clockwise);
                        }
                        else
                        {
                            StrokePieShape(painter, sliceCenter, center, sliceRadius, sliceInnerRadius, drawStartAngle, drawEndAngle, clockwise);
                        }
                    }
                    else
                    {
                        if (explodeType == PieExplodeType.Translate)
                        {
                            StrokePieShape(painter, sliceCenter, sliceCenter, sliceRadius, 0f, drawStartAngle, drawEndAngle, clockwise);
                        }
                        else
                        {
                            StrokePieShape(painter, sliceCenter, center, sliceRadius, 0f, drawStartAngle, drawEndAngle, clockwise);
                        }
                    }
                }
                
                currentAngle = endAngle;
                accumulatedAngle += visibleSliceAngle;
            }
            
            // Draw Inner Hole (Donut)
            if (innerRadius > 0)
            {
                // Optional: Fill the hole area with a color. If alpha is 0, the hole is truly transparent.
                Color holeColor = settings != null && settings.layout != null
                    ? settings.layout.innerRadiusColor
                    : BackgroundColor;

                if (holeColor.a > 0f)
                {
                    painter.fillColor = holeColor;
                    painter.BeginPath();
                    painter.Arc(center, innerRadius, 0, 360);
                    painter.Fill();
                }
            }
        }

        private static void LineArc(Painter2D painter, Vector2 center, float radius, float startAngleDeg, float sweepDeg, int segments)
        {
            if (segments <= 0) return;
            for (int s = 1; s <= segments; s++)
            {
                float t = s / (float)segments;
                float a = startAngleDeg + sweepDeg * t;
                float rad = a * Mathf.Deg2Rad;
                painter.LineTo(center + new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * radius);
            }
        }

        private static bool TryDrawRoundedDonutSlice(Painter2D painter, Vector2 origin, float outerRadius, float innerRadius, float startAngleDeg, float endAngleDeg, bool clockwise, float cornerRadius, bool strokeAfterFill = false)
        {
            if (outerRadius <= 0f) return false;
            if (innerRadius <= 0f) return false;
            if (cornerRadius <= 0f) return false;
            if (innerRadius >= outerRadius) return false;

            float sweepSign = clockwise ? 1f : -1f;
            float sliceSweep = endAngleDeg - startAngleDeg;
            if (Mathf.Abs(sliceSweep) <= 0.001f) return false;

            // Limit corner radius by local edge lengths.
            // For each corner, the adjacent edges are: (1) the circular arc (outer/inner) and (2) the radial edge (thickness).
            // To avoid invalid geometry, clamp to half of the shorter adjacent edge length.
            float thickness = outerRadius - innerRadius;
            float absSliceSweepRad = Mathf.Abs(sliceSweep) * Mathf.Deg2Rad;
            float outerArcLen = outerRadius * absSliceSweepRad;
            float innerArcLen = innerRadius * absSliceSweepRad;
            float maxByThickness = thickness * 0.5f;
            float maxByOuterArc = outerArcLen * 0.5f;
            float maxByInnerArc = innerArcLen * 0.5f;

            // Avoid impossible geometry for outer circle: f <= R/2
            if (cornerRadius > outerRadius * 0.5f) cornerRadius = outerRadius * 0.5f;
            cornerRadius = Mathf.Min(cornerRadius, maxByThickness, maxByOuterArc, maxByInnerArc);
            if (cornerRadius <= 0f) return false;

            if (!TryBuildCorner(origin, startAngleDeg, sweepSign, outerRadius, innerRadius, cornerRadius,
                    out var soOuterCircle, out var soOuterLine, out var soOuterCenter,
                    out var soInnerCircle, out var soInnerLine, out var soInnerCenter)) return false;

            if (!TryBuildCorner(origin, endAngleDeg, -sweepSign, outerRadius, innerRadius, cornerRadius,
                    out var eoOuterCircle, out var eoOuterLine, out var eoOuterCenter,
                    out var eoInnerCircle, out var eoInnerLine, out var eoInnerCenter)) return false;

            float angOuterStart = Mathf.Atan2(soOuterCircle.y - origin.y, soOuterCircle.x - origin.x) * Mathf.Rad2Deg;
            float angOuterEnd = Mathf.Atan2(eoOuterCircle.y - origin.y, eoOuterCircle.x - origin.x) * Mathf.Rad2Deg;
            float outerSweep = SignedAngleDelta(angOuterStart, angOuterEnd, sweepSign);

            float angInnerEnd = Mathf.Atan2(eoInnerCircle.y - origin.y, eoInnerCircle.x - origin.x) * Mathf.Rad2Deg;
            float angInnerStart = Mathf.Atan2(soInnerCircle.y - origin.y, soInnerCircle.x - origin.x) * Mathf.Rad2Deg;
            float innerSweep = SignedAngleDelta(angInnerEnd, angInnerStart, -sweepSign);

            // If computed sweeps are larger than the slice sweep itself, the fillet geometry is not valid.
            // This commonly happens during animation when the slice sweep is very small, and would otherwise
            // take the long way around (~360 deg), causing a full-ring flicker.
            float absOuterSweep = Mathf.Abs(outerSweep);
            float absInnerSweep = Mathf.Abs(innerSweep);
            float absSlice = Mathf.Abs(sliceSweep);
            if (absOuterSweep > 359f || absInnerSweep > 359f) return false;
            if (absSlice > 0.001f && (absOuterSweep > absSlice + 0.01f || absInnerSweep > absSlice + 0.01f)) return false;

            // If the slice is too small, corners overlap.
            if (Mathf.Abs(outerSweep) <= 0.5f) return false;
            if (Mathf.Abs(innerSweep) <= 0.5f) return false;

            int outerSeg = Mathf.Clamp(Mathf.CeilToInt(Mathf.Abs(outerSweep) / 8f), 2, 96);
            int innerSeg = Mathf.Clamp(Mathf.CeilToInt(Mathf.Abs(innerSweep) / 8f), 2, 96);

            painter.BeginPath();
            painter.MoveTo(soOuterCircle);
            LineArc(painter, origin, outerRadius, angOuterStart, outerSweep, outerSeg);

            LineCornerArc(painter, eoOuterCenter, cornerRadius, eoOuterCircle, eoOuterLine);
            painter.LineTo(eoInnerLine);
            LineCornerArc(painter, eoInnerCenter, cornerRadius, eoInnerLine, eoInnerCircle);
            LineArc(painter, origin, innerRadius, angInnerEnd, innerSweep, innerSeg);
            LineCornerArc(painter, soInnerCenter, cornerRadius, soInnerCircle, soInnerLine);
            painter.LineTo(soOuterLine);
            LineCornerArc(painter, soOuterCenter, cornerRadius, soOuterLine, soOuterCircle);
            painter.ClosePath();
            painter.Fill();
            if (strokeAfterFill)
            {
                painter.Stroke();
            }
            return true;
        }

        private static bool TryDrawRoundedPieSlice(Painter2D painter, Vector2 origin, float radius, float startAngleDeg, float endAngleDeg, bool clockwise, float cornerRadius, bool strokeAfterFill = false)
        {
            if (radius <= 0f) return false;
            if (cornerRadius <= 0f) return false;
            float sweepSign = clockwise ? 1f : -1f;
            float sliceSweep = endAngleDeg - startAngleDeg;
            if (Mathf.Abs(sliceSweep) <= 0.001f) return false;

            // Limit corner radius by local edge lengths.
            // Adjacent edges at the outer corners are: (1) the outer circular arc and (2) the radial edge from center to the outer arc.
            // Clamp to half of the shorter edge length.
            float absSliceSweepRad = Mathf.Abs(sliceSweep) * Mathf.Deg2Rad;
            float outerArcLen = radius * absSliceSweepRad;
            float maxByRadius = radius * 0.5f;
            float maxByOuterArc = outerArcLen * 0.5f;

            if (cornerRadius > radius * 0.5f) cornerRadius = radius * 0.5f;
            cornerRadius = Mathf.Min(cornerRadius, maxByRadius, maxByOuterArc);
            if (cornerRadius <= 0f) return false;

            if (!TryBuildOuterCornerOnly(origin, startAngleDeg, sweepSign, radius, cornerRadius,
                    out var soOuterCircle, out var soOuterLine, out var soOuterCenter)) return false;

            if (!TryBuildOuterCornerOnly(origin, endAngleDeg, -sweepSign, radius, cornerRadius,
                    out var eoOuterCircle, out var eoOuterLine, out var eoOuterCenter)) return false;

            float angOuterStart = Mathf.Atan2(soOuterCircle.y - origin.y, soOuterCircle.x - origin.x) * Mathf.Rad2Deg;
            float angOuterEnd = Mathf.Atan2(eoOuterCircle.y - origin.y, eoOuterCircle.x - origin.x) * Mathf.Rad2Deg;
            float outerSweep = SignedAngleDelta(angOuterStart, angOuterEnd, sweepSign);

            float absOuterSweep = Mathf.Abs(outerSweep);
            float absSlice = Mathf.Abs(sliceSweep);
            if (absOuterSweep > 359f) return false;
            if (absSlice > 0.001f && absOuterSweep > absSlice + 0.01f) return false;
            if (absOuterSweep <= 0.5f) return false;

            int outerSeg = Mathf.Clamp(Mathf.CeilToInt(absOuterSweep / 8f), 2, 96);

            painter.BeginPath();
            painter.MoveTo(origin);
            painter.LineTo(soOuterLine);
            LineCornerArc(painter, soOuterCenter, cornerRadius, soOuterLine, soOuterCircle);
            LineArc(painter, origin, radius, angOuterStart, outerSweep, outerSeg);
            LineCornerArc(painter, eoOuterCenter, cornerRadius, eoOuterCircle, eoOuterLine);
            painter.LineTo(origin);
            painter.ClosePath();
            painter.Fill();
            if (strokeAfterFill)
            {
                painter.Stroke();
            }
            return true;
        }

        private static bool TryBuildOuterCornerOnly(Vector2 origin, float boundaryAngleDeg, float interiorSign, float outerRadius, float cornerRadius,
            out Vector2 outerCircle, out Vector2 outerLine, out Vector2 outerCenter)
        {
            Vector2 u = new Vector2(Mathf.Cos(boundaryAngleDeg * Mathf.Deg2Rad), Mathf.Sin(boundaryAngleDeg * Mathf.Deg2Rad));
            Vector2 t = new Vector2(-u.y, u.x);

            float f = cornerRadius;
            float yc = interiorSign * f;

            float outerBase = outerRadius - f;
            float outerSq = outerBase * outerBase - f * f;
            if (outerSq <= 0f)
            {
                outerCircle = outerLine = outerCenter = Vector2.zero;
                return false;
            }

            float xo = Mathf.Sqrt(outerSq);
            Vector2 outerC = origin + u * xo + t * yc;
            Vector2 outerCirclePoint = origin + (outerRadius / outerBase) * (outerC - origin);
            Vector2 outerLinePoint = origin + u * xo;

            outerCircle = outerCirclePoint;
            outerLine = outerLinePoint;
            outerCenter = outerC;
            return true;
        }

        private static bool TryBuildCorner(Vector2 origin, float boundaryAngleDeg, float interiorSign, float outerRadius, float innerRadius, float cornerRadius,
            out Vector2 outerCircle, out Vector2 outerLine, out Vector2 outerCenter,
            out Vector2 innerCircle, out Vector2 innerLine, out Vector2 innerCenter)
        {
            Vector2 u = new Vector2(Mathf.Cos(boundaryAngleDeg * Mathf.Deg2Rad), Mathf.Sin(boundaryAngleDeg * Mathf.Deg2Rad));
            Vector2 t = new Vector2(-u.y, u.x);

            float f = cornerRadius;
            float yc = interiorSign * f;

            float outerBase = outerRadius - f;
            float outerSq = outerBase * outerBase - f * f;
            if (outerSq <= 0f)
            {
                outerCircle = outerLine = outerCenter = Vector2.zero;
                innerCircle = innerLine = innerCenter = Vector2.zero;
                return false;
            }

            float xo = Mathf.Sqrt(outerSq);
            Vector2 outerC = origin + u * xo + t * yc;
            Vector2 outerCirclePoint = origin + (outerRadius / outerBase) * (outerC - origin);
            Vector2 outerLinePoint = origin + u * xo;

            float innerBase = innerRadius + f;
            float innerSq = innerBase * innerBase - f * f;
            float xi = innerSq <= 0f ? 0f : Mathf.Sqrt(innerSq);
            Vector2 innerC = origin + u * xi + t * yc;
            Vector2 innerCirclePoint = origin + (innerRadius / innerBase) * (innerC - origin);
            Vector2 innerLinePoint = origin + u * xi;

            outerCircle = outerCirclePoint;
            outerLine = outerLinePoint;
            outerCenter = outerC;
            innerCircle = innerCirclePoint;
            innerLine = innerLinePoint;
            innerCenter = innerC;
            return true;
        }

        private static float SignedAngleDelta(float fromDeg, float toDeg, float preferredSign)
        {
            float d = Mathf.DeltaAngle(fromDeg, toDeg);
            if (Mathf.Sign(d) != Mathf.Sign(preferredSign) && Mathf.Abs(d) > 0.001f)
            {
                d += 360f * Mathf.Sign(preferredSign);
            }
            return d;
        }

        private static void LineCornerArc(Painter2D painter, Vector2 center, float radius, Vector2 from, Vector2 to)
        {
            float a0 = Mathf.Atan2(from.y - center.y, from.x - center.x) * Mathf.Rad2Deg;
            float a1 = Mathf.Atan2(to.y - center.y, to.x - center.x) * Mathf.Rad2Deg;
            float sweep = Mathf.DeltaAngle(a0, a1);
            int seg = Mathf.Clamp(Mathf.CeilToInt(Mathf.Abs(sweep) / 8f), 2, 32);
            LineArc(painter, center, radius, a0, sweep, seg);
        }

        public override bool GetTooltip(TooltipContext context, List<TooltipItem> items, ref Vector2? cursorPosition, ref string categoryLabel)
        {
            // Pie tooltip logic: check distance from center and angle
            if (Data == null || Data.Series == null) return false;

            float width = context.Width;
            float height = context.Height;
            Vector2 mousePos = context.LocalPos;
            bool hit = false;

            // Iterate first visible pie series
            foreach (var serie in Data.Series)
            {
                if (!serie.visible || serie.type != SerieType.Pie) continue;
                if (serie.seriesData == null || serie.seriesData.Count == 0) continue;
                if (!(serie.settings is PieSettings settings)) continue;

                var layout = settings != null ? settings.layout : null;

                float padding = layout != null && layout.plot != null ? layout.plot.padding : 0f;
                float outerRadius = layout != null ? layout.outerRadius : 0f;
                float innerRadius = layout != null ? layout.innerRadius : 0f;
                float startAngleDeg = layout != null ? layout.startAngleDeg : -90f;
                bool clockwise = layout != null ? layout.clockwise : true;
                float angleRangeDeg = layout != null ? layout.angleRangeDeg : 360f;

                Vector2 centerOffset = layout != null && layout.plot != null ? layout.plot.centerOffset : Vector2.zero;
                bool hoverEnabled = settings != null && settings.hover != null && settings.hover.enabled;
                PieExplodeType explodeType = (settings != null && settings.hover != null) ? settings.hover.explodeType : PieExplodeType.Translate;
                float explodeDistance = hoverEnabled && UsesExplodeDistance(explodeType) ? Mathf.Max(0f, settings.hover.explodeDistance) : 0f;

                // Keep hit-testing consistent with drawing: reserve explode distance.
                float maxAutoRadius = Mathf.Max(0, Mathf.Min(width, height) * 0.5f - padding - explodeDistance);
                float radius = ResolveOuterRadius(outerRadius, maxAutoRadius);
                float innerRadiusPx = ResolveInnerRadius(innerRadius, radius);
                Vector2 center = new Vector2(width / 2f, height / 2f) + new Vector2(centerOffset.x, -centerOffset.y);

                var slices = BuildSlices(serie, settings);
                if (slices == null || slices.Count == 0) return false;

                float total = 0f;
                for (int i = 0; i < slices.Count; i++) total += Mathf.Max(0f, slices[i].Value);
                if (total <= 0) return false;

                // IMPORTANT: hit-testing should be based on the original (non-exploded) geometry.
                // If we shift the center during hit-testing, the cursor can fall into the vacated gap
                // and cause hover state to toggle (flicker).
                float dist = Vector2.Distance(mousePos, center);

                Vector2 dir = mousePos - center;
                float mouseAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

                float startAngle = startAngleDeg;
                int? hitSlice = null;
                float sliceGapPx = layout != null ? Mathf.Max(0f, layout.sliceGapPx) : 0f;
                if (slices.Count <= 1) sliceGapPx = 0f;
                SliceGapType gapType = layout != null ? layout.sliceGapType : SliceGapType.Radial;
                float gapAngle = (gapType == SliceGapType.Radial && sliceGapPx > 0f && radius > 0.001f) ? (sliceGapPx / radius) * Mathf.Rad2Deg : 0f;
                float halfGap = gapAngle * 0.5f;
                bool wrapGaps = Mathf.Abs(angleRangeDeg) >= 359.99f;
                float uniformGapShrink = (gapType == SliceGapType.Uniform) ? sliceGapPx * 0.5f : 0f;
                float hitOuterRadius = radius - uniformGapShrink;
                float hitInnerRadius = innerRadiusPx + uniformGapShrink;
                if (dist > hitOuterRadius + explodeDistance) return false;
                if (hitInnerRadius > 0 && dist < hitInnerRadius) return false;
                for (int i = 0; i < slices.Count; i++)
                {
                    var slice = slices[i];
                    if (slice.Value <= 0) continue;

                    float sliceAngle = (slice.Value / total) * angleRangeDeg;

                    float endAngle = clockwise ? (startAngle + sliceAngle) : (startAngle - sliceAngle);

                    float shrink = wrapGaps
                        ? gapAngle
                        : ((i == 0 || i == slices.Count - 1) ? halfGap : gapAngle);
                    float beforeGap = (i == 0 && !wrapGaps) ? 0f : halfGap;
                    float effectiveAngle = Mathf.Max(0f, sliceAngle - shrink);

                    float hitStart = clockwise ? (startAngle + beforeGap) : (startAngle - beforeGap);
                    float hitEnd = clockwise ? (hitStart + effectiveAngle) : (hitStart - effectiveAngle);

                    if (effectiveAngle > 0f && IsAngleBetween(mouseAngle, hitStart, hitEnd, clockwise))
                    {
                        items.Add(new TooltipItem
                        {
                            Name = serie.name,
                            Value = FormatAxisValue(slice.Value, null, Mathf.Clamp(serie.labelSettings != null ? serie.labelSettings.decimalPlaces : 2, 0, 8)),
                            Color = slice.Color
                        });

                        if (string.IsNullOrEmpty(categoryLabel)) categoryLabel = slice.Name;

                        cursorPosition = null;
                        hit = true;
                        hitSlice = i;
                        break;
                    }

                    startAngle = endAngle;
                }

                bool hoverChanged = false;
                if (hitSlice.HasValue)
                {
                    if (!_hoverSliceIndex.HasValue || _hoverSliceIndex.Value != hitSlice.Value)
                    {
                        _hoverSliceIndex = hitSlice.Value;
                        hoverChanged = true;
                    }
                }
                else
                {
                    if (_hoverSliceIndex != null)
                    {
                        _hoverSliceIndex = null;
                        hoverChanged = true;
                    }
                }

                if (hoverChanged) MarkDirtyRepaint();
            }
            return hit;
        }

        private static bool IsAngleBetweenCW(float target, float start, float end)
        {
            // Normalize all to 0..360
            float t = Mathf.Repeat(target, 360f);
            float s = Mathf.Repeat(start, 360f);
            float e = Mathf.Repeat(end, 360f);

            if (e >= s)
            {
                return t >= s && t < e;
            }
            else
            {
                // Crossing 360 boundary (e.g. 350 to 10)
                return t >= s || t < e;
            }
        }

        private static bool IsAngleBetween(float target, float start, float end, bool clockwise)
        {
            if (clockwise) return IsAngleBetweenCW(target, start, end);
            // If we travel CCW from start to end, it's equivalent to traveling CW from end to start.
            return IsAngleBetweenCW(target, end, start);
        }
    }
}
