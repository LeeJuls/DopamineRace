using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using EasyChart;

namespace EasyChart.Layers
{
    public abstract class BaseSeriesRenderer : VisualElement
    {
        public ChartData Data { get; set; }
        public Color BackgroundColor { get; set; } = Color.black; // Default
        
        public virtual void ClearHover() { }
        
        // Animation
        protected float _animationProgress = 1.0f;
        public float AnimationProgress
        {
            get => _animationProgress;
            set
            {
                if (!Mathf.Approximately(_animationProgress, value))
                {
                    _animationProgress = value;
                    MarkDirtyRepaint();
                    // Optional: UpdateLabels() if labels depend on animation?
                    // Usually labels might fade in or move. For now let's keep labels static or hide them during animation?
                    // Better to just repaint for now.
                }
            }
        }
        
        // Common properties for range, can be overridden or used by subclasses
        protected float _xMin, _xMax;
        protected float _yMin, _yMax;

        protected VisualElement _labelContainer;
        
        // Label Pooling
        private List<Label> _labelPool = new List<Label>();
        private int _activeLabelCount = 0;

        public BaseSeriesRenderer()
        {
            this.StretchToParentSize();
            pickingMode = PickingMode.Ignore;
            style.overflow = Overflow.Visible;
            generateVisualContent += OnGenerateVisualContent;

            _labelContainer = new VisualElement();
            _labelContainer.pickingMode = PickingMode.Ignore;
            _labelContainer.StretchToParentSize();
            _labelContainer.style.overflow = Overflow.Visible;
            Add(_labelContainer);
        }

        public void SetLabelRoot(VisualElement root)
        {
            if (root == null) return;
            if (_labelContainer == null) return;

            if (root.userData is ChartElement)
            {
                _labelContainer.userData = root.userData;
                userData = root.userData;
            }

            if (_labelContainer.parent != root)
            {
                _labelContainer.RemoveFromHierarchy();
                root.Add(_labelContainer);
            }
        }

        protected void BeginUpdateLabels()
        {
            _activeLabelCount = 0;
        }

        protected Label GetLabel()
        {
            Label lbl;
            if (_activeLabelCount < _labelPool.Count)
            {
                lbl = _labelPool[_activeLabelCount];
                lbl.visible = true;
            }
            else
            {
                lbl = new Label();
                lbl.pickingMode = PickingMode.Ignore;
                _labelContainer.Add(lbl);
                _labelPool.Add(lbl);
            }
            _activeLabelCount++;
            return lbl;
        }

        protected void ApplyCommonLabelStyle(Label lbl, Serie serie)
        {
            if (lbl == null || serie == null) return;

            lbl.style.position = Position.Absolute;
            lbl.style.color = serie.labelSettings.color;

            int fontSize = serie.labelSettings.fontSize;
            if (fontSize > 0)
            {
                lbl.style.fontSize = fontSize;
            }
            else
            {
                lbl.style.fontSize = StyleKeyword.Null;
            }

            ChartTextStyleApplier.ApplyLabel(lbl, _labelContainer != null ? _labelContainer : this, ChartTextRole.SeriesLabel);
        }

        protected void ApplyLabelOffset(Label lbl, Serie serie, float baseMarginLeft, float baseMarginTop)
        {
            if (lbl == null || serie == null) return;

            Vector2 offset = serie.labelSettings != null ? serie.labelSettings.offset : Vector2.zero;
            lbl.style.marginLeft = baseMarginLeft + offset.x;
            lbl.style.marginTop = baseMarginTop - offset.y;
        }

        protected Label DrawSerieLabel(Serie serie, string text, Vector2 position, float baseMarginLeft, float baseMarginTop)
        {
            if (serie == null) return null;

            Label lbl = GetLabel();
            lbl.text = text;
            ApplyCommonLabelStyle(lbl, serie);
            lbl.style.left = position.x;
            lbl.style.top = position.y;
            ApplyLabelOffset(lbl, serie, baseMarginLeft, baseMarginTop);
            return lbl;
        }

        protected void CalcQuadUV(Vector2 tiling, Vector2 offset, bool flipV, out float u0, out float u1, out float v0, out float v1)
        {
            u0 = offset.x;
            u1 = tiling.x + offset.x;

            if (flipV)
            {
                v0 = tiling.y + offset.y;
                v1 = offset.y;
            }
            else
            {
                v0 = offset.y;
                v1 = tiling.y + offset.y;
            }
        }

        protected void UnpackTextureFill(TextureFillSettings fill, Color defaultColor, out Texture2D texture, out Vector2 tiling, out Vector2 offset, out Color color)
        {
            if (fill != null)
            {
                texture = fill.texture;
                tiling = fill.tiling;
                offset = fill.offset;
                color = fill.color;

                bool hasPro = ProPackage.IsInstalled;
                if (hasPro && fill.animationType != TextureFillAnimationType.None)
                {
                    float t = Time.realtimeSinceStartup;
                    if (fill.animationType == TextureFillAnimationType.TextureUvMove)
                    {
                        offset -= fill.uvMoveSpeed * t;
                    }
                    else if (fill.animationType == TextureFillAnimationType.TextureScale)
                    {
                        var baseTiling = tiling;

                        float u = t * fill.scaleSpeed;
                        float c01;

                        Vector2 factor;
                        switch (fill.scaleType)
                        {
                            case TextureFillScaleType.ZoomIn:
                            {
                                float p = Mathf.Repeat(u, 1f);
                                var s = Vector2.Lerp(fill.scaleFrom, fill.scaleTo, p);
                                s.x = Mathf.Max(0.0001f, s.x);
                                s.y = Mathf.Max(0.0001f, s.y);
                                factor = new Vector2(1f / s.x, 1f / s.y);
                                c01 = p;
                                break;
                            }
                            case TextureFillScaleType.ZoomOut:
                            {
                                float p = Mathf.Repeat(u, 1f);
                                var s = Vector2.Lerp(fill.scaleTo, fill.scaleFrom, p);
                                s.x = Mathf.Max(0.0001f, s.x);
                                s.y = Mathf.Max(0.0001f, s.y);
                                factor = new Vector2(1f / s.x, 1f / s.y);
                                c01 = p;
                                break;
                            }
                            case TextureFillScaleType.PingPong:
                            {
                                float p = Mathf.PingPong(u, 1f);
                                var s = Vector2.Lerp(fill.scaleFrom, fill.scaleTo, p);
                                s.x = Mathf.Max(0.0001f, s.x);
                                s.y = Mathf.Max(0.0001f, s.y);
                                factor = new Vector2(1f / s.x, 1f / s.y);
                                c01 = p;
                                break;
                            }
                            case TextureFillScaleType.Sin:
                            default:
                            {
                                float s = Mathf.Sin(u);
                                float p = (s + 1f) * 0.5f;
                                var sc = Vector2.Lerp(fill.scaleFrom, fill.scaleTo, p);
                                sc.x = Mathf.Max(0.0001f, sc.x);
                                sc.y = Mathf.Max(0.0001f, sc.y);
                                factor = new Vector2(1f / sc.x, 1f / sc.y);
                                c01 = p;
                                break;
                            }
                        }

                        factor.x = Mathf.Max(0.0001f, factor.x);
                        factor.y = Mathf.Max(0.0001f, factor.y);
                        tiling = new Vector2(baseTiling.x * factor.x, baseTiling.y * factor.y);
                        offset += (baseTiling - tiling) * 0.5f;

                        if (fill.colorFadeGradient != null)
                            color *= fill.colorFadeGradient.Evaluate(c01);
                    }
                    else if (fill.animationType == TextureFillAnimationType.TextureFade)
                    {
                        float u = t * fill.colorFadeSpeed;
                        float c01;
                        switch (fill.colorFadeWrap)
                        {
                            case TextureFillColorOverLifeWrap.Loop:
                                c01 = Mathf.Repeat(u, 1f);
                                break;
                            case TextureFillColorOverLifeWrap.Clamp:
                                c01 = Mathf.Clamp01(u);
                                break;
                            case TextureFillColorOverLifeWrap.PingPong:
                            default:
                                c01 = Mathf.PingPong(u, 1f);
                                break;
                        }

                        if (fill.colorFadeGradient != null)
                            color = fill.colorFadeGradient.Evaluate(c01);
                        else
                            color = Color.Lerp(fill.colorFadeStart, fill.colorFadeEnd, c01);
                    }
                }

                if (texture != null && Application.isPlaying)
                {
                    var desiredWrap = (hasPro && fill.animationType == TextureFillAnimationType.TextureScale)
                        ? TextureWrapMode.Clamp
                        : TextureWrapMode.Repeat;

                    if (texture.wrapMode != desiredWrap)
                        texture.wrapMode = desiredWrap;
                }
            }
            else
            {
                texture = null;
                tiling = Vector2.one;
                offset = Vector2.zero;
                color = defaultColor;
            }
        }

        protected void UnpackTextureFill(TextureFillSettings fill, out Texture2D texture, out Vector2 tiling, out Vector2 offset, out Color color)
        {
            UnpackTextureFill(fill, Color.white, out texture, out tiling, out offset, out color);
        }

        protected Color ResolveFillColor(TextureFillSettings fill, Color defaultColor)
        {
            return fill != null ? fill.color : defaultColor;
        }

        protected bool TryResolveTextureFill(TextureFillSettings fill, Color defaultColor, bool requireTexture, out Texture2D texture, out Vector2 tiling, out Vector2 offset, out Color color)
        {
            if (fill == null || (requireTexture && fill.texture == null))
            {
                texture = null;
                tiling = Vector2.one;
                offset = Vector2.zero;
                color = defaultColor;
                return false;
            }

            texture = fill.texture;
            tiling = fill.tiling;
            offset = fill.offset;
            color = fill.color;
            return true;
        }

        protected bool DrawTexturedQuad(MeshGenerationContext context, Rect rect, Texture2D texture, Vector2 tiling, Vector2 offset, Color tint, bool flipV)
        {
            return DrawTexturedQuad(context, rect, (Texture)texture, tiling, offset, tint, flipV);
        }

        protected bool DrawTexturedQuad(MeshGenerationContext context, Rect rect, Texture texture, Vector2 tiling, Vector2 offset, Color tint, bool flipV)
        {
            if (texture == null) return false;
            CalcQuadUV(tiling, offset, flipV, out float u0, out float u1, out float v0, out float v1);
            MeshUtils.WriteTexturedQuad(context, rect.xMin, rect.yMin, rect.xMax, rect.yMax, texture, tint, u0, v0, u1, v1);
            return true;
        }

        protected bool DrawTexturedFan(MeshGenerationContext context, IList<Vector2> points, Rect rect, Texture texture, Vector2 tiling, Vector2 offset, Color tint, bool flipV)
        {
            if (texture == null) return false;
            if (points == null || points.Count < 3) return false;
            CalcQuadUV(tiling, offset, flipV, out float u0, out float u1, out float v0, out float v1);
            MeshUtils.WriteTexturedFan(context, points, texture, tint, rect, u0, v0, u1, v1);
            return true;
        }

        protected bool DrawTexturedVerticalStrip(MeshGenerationContext context, IList<Vector2> topVertices, float bottomY, Texture texture, Vector2 tiling, Vector2 offset, Color tint, bool doubleSided)
        {
            if (texture == null) return false;
            if (topVertices == null || topVertices.Count < 2) return false;
            MeshUtils.WriteTexturedVerticalStrip(context, topVertices, bottomY, texture, tint, tiling, offset, doubleSided);
            return true;
        }

        protected float EvalTextureScaleSizeMul(TextureFillSettings fill)
        {
            if (fill == null) return 1f;
            if (fill.texture == null) return 1f;

            bool hasPro = ProPackage.IsInstalled;
            if (!hasPro) return 1f;
            if (fill.animationType != TextureFillAnimationType.TextureScale) return 1f;

            float t = Time.realtimeSinceStartup;
            float u = t * fill.scaleSpeed;

            float p;
            switch (fill.scaleType)
            {
                case TextureFillScaleType.ZoomIn:
                    p = Mathf.Repeat(u, 1f);
                    break;
                case TextureFillScaleType.ZoomOut:
                    p = Mathf.Repeat(u, 1f);
                    break;
                case TextureFillScaleType.PingPong:
                    p = Mathf.PingPong(u, 1f);
                    break;
                case TextureFillScaleType.Sin:
                default:
                    p = (Mathf.Sin(u) + 1f) * 0.5f;
                    break;
            }

            Vector2 sc;
            switch (fill.scaleType)
            {
                case TextureFillScaleType.ZoomOut:
                    sc = Vector2.Lerp(fill.scaleTo, fill.scaleFrom, p);
                    break;
                case TextureFillScaleType.ZoomIn:
                case TextureFillScaleType.PingPong:
                case TextureFillScaleType.Sin:
                default:
                    sc = Vector2.Lerp(fill.scaleFrom, fill.scaleTo, p);
                    break;
            }

            sc.x = Mathf.Max(0.0001f, sc.x);
            sc.y = Mathf.Max(0.0001f, sc.y);
            float m = Mathf.Max(sc.x, sc.y);
            return Mathf.Max(1f, m);
        }

        protected void DrawPointMarker(MeshGenerationContext context, Painter2D painter, Vector2 pos, float radius, TextureFillSettings fill, Color defaultColor)
        {
            if (radius <= 0f) return;

            UnpackTextureFill(fill, defaultColor, out var tex, out var tiling, out var offset, out var color);

            bool hasPro = ProPackage.IsInstalled;
            if (hasPro && fill != null && fill.animationType == TextureFillAnimationType.TextureScale)
            {
                tiling = fill.tiling;
                offset = fill.offset;
            }

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

        protected void DrawPointOverlay(MeshGenerationContext context, Painter2D painter, Vector2 pos, float radius, TextureFillSettings fill)
        {
            if (radius <= 0f) return;
            if (fill == null || fill.texture == null) return;

            UnpackTextureFill(fill, Color.clear, out var tex, out var tiling, out var offset, out var tint);
            if (tint.a <= 0f) return;

            bool hasPro = ProPackage.IsInstalled;
            if (hasPro && fill.animationType == TextureFillAnimationType.TextureScale)
            {
                tiling = fill.tiling;
                offset = fill.offset;
            }

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
                    tint,
                    true);
            }
            else
            {
                painter.fillColor = tint;
                painter.BeginPath();
                painter.Arc(pos, r, 0, 360);
                painter.Fill();
            }
        }

        protected void EnsureRepeatWrapMode(Texture texture)
        {
            if (texture == null) return;
            if (!Application.isPlaying) return;
            if (texture.wrapMode != TextureWrapMode.Repeat)
            {
                texture.wrapMode = TextureWrapMode.Repeat;
            }
        }

        protected void EnsureRepeatWrapMode(Texture2D texture)
        {
            if (texture == null) return;
            if (!Application.isPlaying) return;
            if (texture.wrapMode != TextureWrapMode.Repeat)
            {
                texture.wrapMode = TextureWrapMode.Repeat;
            }
        }

        protected void EndUpdateLabels()
        {
            for (int i = _activeLabelCount; i < _labelPool.Count; i++)
            {
                _labelPool[i].visible = false;
            }
        }

        public void ClearLabels()
        {
            // Hide all labels in the pool
            for (int i = 0; i < _labelPool.Count; i++)
            {
                _labelPool[i].visible = false;
            }
            _activeLabelCount = 0;
        }

        public virtual void SetRange(float xMin, float xMax, float yMin, float yMax)
        {
            _xMin = xMin;
            _xMax = xMax;
            _yMin = yMin;
            _yMax = yMax;
            MarkDirtyRepaint();
        }

        public virtual void UpdateLabels()
        {
            // Base implementation does nothing
        }

        public virtual bool GetTooltip(TooltipContext context, List<TooltipItem> items, ref Vector2? cursorPosition, ref string categoryLabel)
        {
            // Base implementation does nothing
            return false;
        }

        protected AxisConfig GetAxisConfig(AxisId id)
        {
            if (Data == null || Data.Axes == null) return null;
            for (int i = 0; i < Data.Axes.Count; i++)
            {
                var a = Data.Axes[i];
                if (a != null && a.id == id) return a;
            }
            return null;
        }

        private static readonly string[] s_fixedPointFormats =
        {
            "F0",
            "F1",
            "F2",
            "F3",
            "F4",
            "F5",
            "F6",
            "F7",
            "F8",
        };

        protected static string FormatAxisValue(float value, AxisConfig axis, int decimalPlaces)
        {
            if (axis != null && !string.IsNullOrEmpty(axis.labelFormat))
            {
                return value.ToString(axis.labelFormat);
            }

            int p = Mathf.Clamp(decimalPlaces, 0, 8);
            return value.ToString(s_fixedPointFormats[p]);
        }

        private const int k_IntStringCacheSize = 512;
        private static readonly string[] s_intStringCache = BuildIntStringCache();

        private static string[] BuildIntStringCache()
        {
            var arr = new string[k_IntStringCacheSize];
            for (int i = 0; i < arr.Length; i++)
            {
                arr[i] = i.ToString();
            }
            return arr;
        }

        protected static string FormatIntCached(int value)
        {
            if (value >= 0 && value < k_IntStringCacheSize)
            {
                return s_intStringCache[value];
            }
            return value.ToString();
        }

        #region Label Descriptor Helpers

        protected bool TryGetChartAndLabelController(out ChartElement chart, out ChartLabelController controller)
        {
            chart = userData as ChartElement;
            if (chart == null && _labelContainer != null)
            {
                chart = _labelContainer.userData as ChartElement;
            }

            controller = chart != null ? chart.LabelControllerInternal : null;
            return controller != null;
        }

        protected void GetSmoothScrollOffsets(ChartElement chart, out float offsetX, out float offsetY)
        {
            offsetX = 0f;
            offsetY = 0f;
            if (chart == null) return;
            var scroll = chart.CategoryScrollControllerInternal;
            if (scroll == null) return;
            if (!scroll.SmoothTranslating) return;
            offsetX = scroll.ScrollOffsetX;
            offsetY = scroll.ScrollOffsetY;
        }

        protected static bool IsAnchorVisibleInPlot(Vector2 anchorPx, float plotWidth, float plotHeight, float scrollOffsetX, float scrollOffsetY)
        {
            float visX = anchorPx.x + scrollOffsetX;
            float visY = anchorPx.y + scrollOffsetY;
            return visX >= 0f && visX <= plotWidth && visY >= 0f && visY <= plotHeight;
        }

        protected string GetStableKeyForPoint(bool isCategoryAxis, float categoryIndex, string pointId, int fallbackIndex)
        {
            if (isCategoryAxis)
            {
                int idx = Mathf.RoundToInt(categoryIndex);
                return FormatIntCached(idx);
            }

            if (!string.IsNullOrEmpty(pointId)) return pointId;
            return fallbackIndex.ToString();
        }

        protected static LabelDescriptor BuildSeriesLabelDesc(
            string key,
            string text,
            Vector2 anchorPx,
            Vector2 offsetPx,
            bool clipToPlot,
            int fontSizeOverride,
            Color colorOverride,
            ChartLabelAnchor anchor = ChartLabelAnchor.TopLeft,
            Color backgroundColor = default,
            Texture2D backgroundTexture = null)
        {
            return new LabelDescriptor
            {
                key = key,
                text = text,
                visible = true,
                anchorPx = anchorPx,
                offsetPx = offsetPx,
                anchor = anchor,
                role = ChartTextRole.SeriesLabel,
                zOrder = 0,
                priority = 0,
                clipToPlot = clipToPlot,
                fontSizeOverride = fontSizeOverride,
                colorOverride = colorOverride,
                rotationDeg = 0f,
                backgroundColor = backgroundColor,
                backgroundTexture = backgroundTexture,
            };
        }

        #endregion

        protected virtual Vector2 GetPixelPos(Vector2 point, float width, float height)
        {
            if (Mathf.Approximately(_xMax, _xMin)) return new Vector2(0, height);
            if (Mathf.Approximately(_yMax, _yMin)) return new Vector2(point.x, height);

            AxisId xAxisId = (Data != null && Data.Cartesian != null) ? Data.Cartesian.xAxisId : AxisId.XBottom;
            AxisId yAxisId = (Data != null && Data.Cartesian != null) ? Data.Cartesian.yAxisId : AxisId.YLeft;

            var xAxis = GetAxisConfig(xAxisId);
            var yAxis = GetAxisConfig(yAxisId);

            bool xIsCategory = xAxis != null && xAxis.axisType == AxisType.Category && xAxis.labels != null && xAxis.labels.Count > 0;
            bool yIsCategory = yAxis != null && yAxis.axisType == AxisType.Category && yAxis.labels != null && yAxis.labels.Count > 0;

            float xVal = point.x;
            if (xIsCategory)
            {
                int labelCount = xAxis.labels.Count;
                int span = Mathf.RoundToInt(_xMax - _xMin + 1);
                if (span < 1) span = 1;
                int preloadExtra = labelCount > span ? 1 : 0;
                float effectiveMax = _xMax + preloadExtra;
                bool wraps = effectiveMax >= labelCount;
                if (wraps && xVal < _xMin)
                {
                    xVal += labelCount;
                }
            }

            float xRatio;
            if (xIsCategory && xAxis.labelPlacement == CategoryLabelPlacement.CellCenter)
            {
                int span = Mathf.RoundToInt(_xMax - _xMin + 1);
                if (span < 1) span = 1;
                xRatio = (xVal - _xMin + 0.5f) / span;
            }
            else
            {
                xRatio = (xVal - _xMin) / (_xMax - _xMin);
            }

            float yVal = point.y;
            if (yIsCategory)
            {
                int labelCount = yAxis.labels.Count;
                int span = Mathf.RoundToInt(_yMax - _yMin + 1);
                if (span < 1) span = 1;
                int preloadExtra = labelCount > span ? 1 : 0;
                float effectiveMax = _yMax + preloadExtra;
                bool wraps = effectiveMax >= labelCount;
                if (wraps && yVal < _yMin)
                {
                    yVal += labelCount;
                }
            }

            float yRatio;
            if (yIsCategory && yAxis.labelPlacement == CategoryLabelPlacement.CellCenter)
            {
                int span = Mathf.RoundToInt(_yMax - _yMin + 1);
                if (span < 1) span = 1;
                yRatio = (yVal - _yMin + 0.5f) / span;
            }
            else
            {
                yRatio = (yVal - _yMin) / (_yMax - _yMin);
            }
            
            if (float.IsNaN(xRatio)) xRatio = 0;
            if (float.IsNaN(yRatio)) yRatio = 0;

            float pixelX = xRatio * width;
            bool xOnTop = xAxisId == AxisId.XTop;
            float pixelY = (yIsCategory && xOnTop) ? (yRatio * height) : (height - (yRatio * height));
            return new Vector2(pixelX, pixelY);
        }

        protected abstract void OnGenerateVisualContent(MeshGenerationContext context);
    }
}






