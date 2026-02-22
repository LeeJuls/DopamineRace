using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using EasyChart;

namespace EasyChart.Layers
{
    public class ScatterSeriesRenderer : BaseSeriesRenderer
    {
        private struct ScatterHit
        {
            public int SerieIndex;
            public int PointIndex;
            public Vector2 PixelPos;
            public float Dist;
        }

        private ScatterHit? _hover;

        public ScatterSeriesRenderer()
        {
            schedule.Execute(OnUpdate).Every(16);
        }

        private static bool HasTextureFillAnimation(TextureFillSettings fill)
        {
            return fill != null && fill.animationType != TextureFillAnimationType.None;
        }

        private void OnUpdate()
        {
            if (panel == null) return;
            if (Data == null || Data.Series == null) return;

            if (!ProPackage.IsInstalled) return;

            for (int i = 0; i < Data.Series.Count; i++)
            {
                var s = Data.Series[i];
                if (s == null || !s.visible) continue;
                if (s.type != SerieType.Scatter) continue;
                if (s.settings is not ScatterSettings settings) continue;
                if (settings.point != null && HasTextureFillAnimation(settings.point.textureFill)) { MarkDirtyRepaint(); return; }
                if (settings.hover != null && HasTextureFillAnimation(settings.hover.textureFill)) { MarkDirtyRepaint(); return; }
                if (settings.hover != null && settings.hover.point != null && HasTextureFillAnimation(settings.hover.point.textureFill)) { MarkDirtyRepaint(); return; }
            }
        }

        private void DrawPointMarkerWithAlpha(MeshGenerationContext context, Painter2D painter, Vector2 pos, float radius, TextureFillSettings fill, Color defaultColor, float alpha, Vector2 uvOffsetAdd)
        {
            if (radius <= 0f) return;
            if (alpha <= 0.001f) return;

            UnpackTextureFill(fill, defaultColor, out var tex, out var tiling, out var offset, out var color);

            bool hasPro = ProPackage.IsInstalled;
            if (hasPro && fill != null && fill.animationType == TextureFillAnimationType.TextureScale)
            {
                tiling = fill.tiling;
                offset = fill.offset;
            }

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

        private bool IsCartesianTransposed()
        {
            if (Data == null) return false;
            AxisId xAxisId = (Data.Cartesian != null) ? Data.Cartesian.xAxisId : AxisId.XBottom;
            AxisId yAxisId = (Data.Cartesian != null) ? Data.Cartesian.yAxisId : AxisId.YLeft;

            var xAxis = GetAxis(xAxisId);
            var yAxis = GetAxis(yAxisId);
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

        private static float GetScatterY(SeriesData p, ScatterSettings settings)
        {
            if (p == null) return 0f;

            float y = p.y;
            if (Mathf.Approximately(y, 0f) && !Mathf.Approximately(p.value, 0f))
            {
                y = p.value;
            }

            return y;
        }

        private float EvalPointSize(SeriesData dataPoint, ScatterSettings settings)
        {
            if (settings == null || settings.point == null) return 0f;
            if (dataPoint == null) return 0f;

            if (settings.sizeMapping == null || !settings.sizeMapping.enabled)
            {
                return settings.point.size;
            }

            float minV = settings.sizeMapping.minValue;
            float maxV = settings.sizeMapping.maxValue;
            float t;
            if (Mathf.Approximately(maxV, minV))
            {
                t = 0f;
            }
            else t = (dataPoint.z - minV) / (maxV - minV);

            if (settings.sizeMapping.clamp) t = Mathf.Clamp01(t);

            float c = settings.sizeMapping.curve != null ? settings.sizeMapping.curve.Evaluate(t) : t;
            if (settings.sizeMapping.clamp) c = Mathf.Clamp01(c);

            return Mathf.Lerp(settings.sizeMapping.minSize, settings.sizeMapping.maxSize, c);
        }

        private float GetPickRadius(ScatterSettings settings)
        {
            if (settings == null || settings.hover == null || !settings.hover.enabled) return 0f;
            return Mathf.Max(0f, settings.hover.pickRadius);
        }

        private ScatterHit? FindHoverHit(TooltipContext context)
        {
            if (Data == null || Data.Series == null) return null;

            float width = context.Width;
            float height = context.Height;
            if (width <= 0 || height <= 0) return null;

            ScatterHit? best = null;

            for (int si = 0; si < Data.Series.Count; si++)
            {
                var serie = Data.Series[si];
                if (serie == null || !serie.visible) continue;
                if (serie.type != SerieType.Scatter) continue;
                if (serie.settings is not ScatterSettings settings) continue;

                float pickRadius = GetPickRadius(settings);
                if (pickRadius <= 0f) continue;

                float bestDistSqForSerie = pickRadius * pickRadius;

                if (serie.seriesData == null) continue;
                for (int pi = 0; pi < serie.seriesData.Count; pi++)
                {
                    var p = serie.seriesData[pi];
                    if (p == null) continue;
                    var pos = GetPixelPos(new Vector2(p.x, GetScatterY(p, settings)), width, height);

                    float dx = pos.x - context.LocalPos.x;
                    float dy = pos.y - context.LocalPos.y;
                    float distSq = dx * dx + dy * dy;

                    if (distSq > bestDistSqForSerie) continue;

                    float dist = Mathf.Sqrt(distSq);
                    if (best == null || dist < best.Value.Dist)
                    {
                        best = new ScatterHit
                        {
                            SerieIndex = si,
                            PointIndex = pi,
                            PixelPos = pos,
                            Dist = dist
                        };
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
                MarkDirtyRepaint();
            }
        }

        public override void UpdateLabels()
        {
            if (!TryGetChartAndLabelController(out var chart, out var labelController)) return;
            GetSmoothScrollOffsets(chart, out float scrollOffsetX, out float scrollOffsetY);

            if (Data == null || Data.Series == null) return;

            float w = contentRect.width;
            float h = contentRect.height;
            if (w <= 0 || h <= 0) return;

            AxisId xAxisId = (Data != null && Data.Cartesian != null) ? Data.Cartesian.xAxisId : AxisId.XBottom;
            var xAxis = GetAxisConfig(xAxisId);
            bool xIsCategory = xAxis != null && xAxis.axisType == AxisType.Category && xAxis.labels != null && xAxis.labels.Count > 0;

            AxisId yAxisId = (Data != null && Data.Cartesian != null) ? Data.Cartesian.yAxisId : AxisId.YLeft;
            var yAxis = GetAxisConfig(yAxisId);

            for (int si = 0; si < Data.Series.Count; si++)
            {
                var serie = Data.Series[si];
                if (serie == null) continue;
                if (!serie.visible) continue;
                if (serie.type != SerieType.Scatter) continue;
                if (!serie.labelSettings.enabled) continue;

                var settings = serie.settings as ScatterSettings;
                if (settings == null) continue;

                var points = serie.seriesData;
                if (points == null) continue;

                int dpPlaces = Mathf.Clamp(serie.labelSettings != null ? serie.labelSettings.decimalPlaces : 2, 0, 8);
                bool showName = serie.labelSettings != null && serie.labelSettings.showName;
                Vector2 extraOffset = serie.labelSettings != null ? serie.labelSettings.offset : Vector2.zero;
                int fontSizeOverride = serie.labelSettings != null ? serie.labelSettings.fontSize : 0;
                Color colorOverride = serie.labelSettings != null ? serie.labelSettings.color : Color.clear;

                float baseMarginTop;
                if (serie.labelSettings.position == LabelPosition.Inside)
                    baseMarginTop = 10;
                else if (serie.labelSettings.position == LabelPosition.Center)
                    baseMarginTop = -7;
                else
                    baseMarginTop = -20;

                for (int pi = 0; pi < points.Count; pi++)
                {
                    var point = points[pi];
                    if (point == null) continue;

                    float y = GetScatterY(point, settings);
                    Vector2 pos = GetPixelPos(new Vector2(point.x, y), w, h);
                    if (!IsAnchorVisibleInPlot(pos, w, h, scrollOffsetX, scrollOffsetY)) continue;

                    string text = FormatAxisValue(y, yAxis, dpPlaces);
                    if (showName) text = $"{serie.name}\n{text}";

                    string pointId = GetStableKeyForPoint(xIsCategory, point.x, point.id, pi);
                    var desc = BuildSeriesLabelDesc(
                        $"scatter:{si}:{pointId}",
                        text,
                        pos,
                        new Vector2(-10 + extraOffset.x, -baseMarginTop + extraOffset.y),
                        clipToPlot: true,
                        fontSizeOverride,
                        colorOverride);

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

            var serie = Data.Series[hit.Value.SerieIndex];
            if (serie == null || serie.seriesData == null) return false;
            if (hit.Value.PointIndex < 0 || hit.Value.PointIndex >= serie.seriesData.Count) return false;

            var dp = serie.seriesData[hit.Value.PointIndex];
            if (dp == null) return false;

            var settings = serie.settings as ScatterSettings;
            float y = GetScatterY(dp, settings);

            if (cursorPosition == null)
            {
                cursorPosition = hit.Value.PixelPos;
            }

            if (string.IsNullOrEmpty(categoryLabel))
            {
                if (!string.IsNullOrEmpty(dp.name))
                {
                    categoryLabel = dp.name;
                }
                else
                {
                    AxisId xAxisId = (Data != null && Data.Cartesian != null) ? Data.Cartesian.xAxisId : AxisId.XBottom;
                    var xAxis = GetAxisConfig(xAxisId);
                    categoryLabel = FormatAxisValue(dp.x, xAxis, 2);
                }
            }

            Color c = Color.white;
            if (settings != null && settings.point != null)
                c = ResolveFillColor(settings.point.textureFill, Color.white);

            items.Add(new TooltipItem
            {
                Name = serie.name,
                Value = FormatAxisValue(
                    y,
                    GetAxisConfig((Data != null && Data.Cartesian != null) ? Data.Cartesian.yAxisId : AxisId.YLeft),
                    Mathf.Clamp(serie.labelSettings != null ? serie.labelSettings.decimalPlaces : 2, 0, 8)
                ),
                Color = c
            });

            return true;
        }

        protected override void OnGenerateVisualContent(MeshGenerationContext context)
        {
            if (Data == null || Data.Series == null) return;

            var width = contentRect.width;
            var height = contentRect.height;
            if (width <= 0 || height <= 0) return;

            var painter = context.painter2D;

            float pixelClipX = width * _animationProgress;

            for (int si = 0; si < Data.Series.Count; si++)
            {
                var serie = Data.Series[si];
                if (serie == null || !serie.visible || serie.seriesData == null || serie.seriesData.Count == 0) continue;
                if (serie.type != SerieType.Scatter) continue;
                var settings = serie.settings as ScatterSettings;
                if (settings == null) continue;
                if (settings.point == null || !settings.point.show) continue;

                var pointFill = settings.point.textureFill;

                for (int pi = 0; pi < serie.seriesData.Count; pi++)
                {
                    var p = serie.seriesData[pi];
                    if (p == null) continue;
                    Vector2 pos = GetPixelPos(new Vector2(p.x, GetScatterY(p, settings)), width, height);
                    if (pos.x > pixelClipX) continue;

                    float size = EvalPointSize(p, settings);

                    bool hoverEnabled = settings.hover != null && settings.hover.enabled;
                    bool isHovered = _hover.HasValue && _hover.Value.SerieIndex == si && _hover.Value.PointIndex == pi;

                    if (hoverEnabled && isHovered && settings.hover.point != null)
                    {
                        var hp = settings.hover.point;

                        if (!hp.show) continue;
                        float radius = hp.size * 0.5f;
                        DrawPointMarkerWithAlpha(context, painter, pos, radius, hp.textureFill, Color.white, 1f, Vector2.zero);
                    }
                    else
                    {
                        TextureFillSettings hoverFill = null;
                        if (hoverEnabled && isHovered)
                        {
                            size *= Mathf.Max(0f, settings.hover.scale);
                            hoverFill = settings.hover.textureFill;
                        }

                        float radius = size * 0.5f;
                        DrawPointMarkerWithAlpha(context, painter, pos, radius, pointFill, Color.white, 1f, Vector2.zero);
                        DrawPointOverlay(context, painter, pos, radius, hoverFill);
                    }
                }
            }
        }
    }
}
