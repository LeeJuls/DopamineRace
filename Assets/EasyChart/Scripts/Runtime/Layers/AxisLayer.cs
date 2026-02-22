using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace EasyChart
{
    public class AxisLayer : VisualElement
    {
        private const float LabelClipMarginPx = 2000f;
        private const float LabelEdgeClipMarginPx = 20f;

        private VisualElement _xAxisViewport;
        private VisualElement _yAxisViewport;
        private VisualElement _xAxisContainer;
        private VisualElement _yAxisContainer;
        
        // These are now mostly redundant if Settings are used, but kept for fallback
        public Color AxisColor { get; set; } = Color.white;
        public float AxisLineWidth { get; set; } = 2.0f;

        // Data for labels
        public List<string> XLabels { get; set; } = new List<string>();
        public List<string> YLabels { get; set; } = new List<string>();

        public float XMin { get; set; } = 0;
        public float XMax { get; set; } = 100;
        public int XSplitCount { get; set; } = 6;
        public float YMin { get; set; } = 0;
        public float YMax { get; set; } = 100;
        public int YSplitCount { get; set; } = 6;

        public bool XIsCategory { get; set; } = true;
        public bool YIsCategory { get; set; } = false;

        public AxisId XAxisId { get; set; } = AxisId.XBottom;
        public AxisId YAxisId { get; set; } = AxisId.YLeft;

        public AxisConfig XAxisConfig { get; set; }
        public AxisConfig YAxisConfig { get; set; }

        public AxisLayer()
        {
            this.StretchToParentSize();
            pickingMode = PickingMode.Ignore;

            _xAxisViewport = new VisualElement { name = "xAxisViewport", pickingMode = PickingMode.Ignore };
            _yAxisViewport = new VisualElement { name = "yAxisViewport", pickingMode = PickingMode.Ignore };

            _xAxisContainer = new VisualElement { name = "xAxis", pickingMode = PickingMode.Ignore };
            _yAxisContainer = new VisualElement { name = "yAxis", pickingMode = PickingMode.Ignore };

            // X axis labels should be clipped horizontally to plot width, but may extend above/below.
            // We achieve this with a tall viewport (extra margin on Y) and an inner container aligned to plot coords.
            _xAxisViewport.style.position = Position.Absolute;
            _xAxisViewport.style.left = -LabelEdgeClipMarginPx;
            _xAxisViewport.style.right = -LabelEdgeClipMarginPx;
            _xAxisViewport.style.top = -LabelClipMarginPx;
            _xAxisViewport.style.bottom = -LabelClipMarginPx;
            _xAxisViewport.style.overflow = Overflow.Hidden;

            // Y axis labels should be clipped vertically to plot height, but may extend left/right.
            // We achieve this with a wide viewport (extra margin on X) and an inner container aligned to plot coords.
            _yAxisViewport.style.position = Position.Absolute;
            _yAxisViewport.style.left = -LabelClipMarginPx;
            _yAxisViewport.style.right = -LabelClipMarginPx;
            _yAxisViewport.style.top = -LabelEdgeClipMarginPx;
            _yAxisViewport.style.bottom = -LabelEdgeClipMarginPx;
            _yAxisViewport.style.overflow = Overflow.Hidden;

            // Containers stretch to parent, labels positioned with pixel values
            _xAxisContainer.style.position = Position.Absolute;
            _xAxisContainer.style.left = LabelEdgeClipMarginPx;
            _xAxisContainer.style.right = LabelEdgeClipMarginPx;
            _xAxisContainer.style.top = LabelClipMarginPx;
            _xAxisContainer.style.bottom = LabelClipMarginPx;
            _xAxisContainer.style.overflow = Overflow.Visible;

            _yAxisContainer.style.position = Position.Absolute;
            _yAxisContainer.style.left = LabelClipMarginPx;
            _yAxisContainer.style.right = LabelClipMarginPx;
            _yAxisContainer.style.top = LabelEdgeClipMarginPx;
            _yAxisContainer.style.bottom = LabelEdgeClipMarginPx;
            _yAxisContainer.style.overflow = Overflow.Visible;
            
            _xAxisViewport.Add(_xAxisContainer);
            _yAxisViewport.Add(_yAxisContainer);

            Add(_xAxisViewport);
            Add(_yAxisViewport);

            generateVisualContent += OnGenerateVisualContent;
        }

        public void SetCategoryScrollOffset(float xPx, float yPx)
        {
            if (_xAxisContainer != null)
            {
                _xAxisContainer.style.translate = new Translate(xPx, 0, 0);
            }
            if (_yAxisContainer != null)
            {
                _yAxisContainer.style.translate = new Translate(0, yPx, 0);
            }
        }

        private static int ClampCategoryVisibleCount(AxisConfig axis, int labelsCount)
        {
            if (labelsCount <= 0) return 0;
            // When autoTicks is true, use all labels
            if (axis != null && axis.autoTicks) return labelsCount;
            int v = axis != null ? axis.splitCount : 0;
            if (v < 2) v = 2;
            if (v > labelsCount) v = labelsCount;
            return v;
        }

        private void OnGenerateVisualContent(MeshGenerationContext context)
        {
            var w = contentRect.width;
            var h = contentRect.height;

            if (w <= 0 || h <= 0) return;

            var painter = context.painter2D;
            
            // Use config if available, else fallback
            var xColor = XAxisConfig != null ? XAxisConfig.color : AxisColor;
            var xWidth = XAxisConfig != null ? XAxisConfig.width : AxisLineWidth;
            var yColor = YAxisConfig != null ? YAxisConfig.color : AxisColor;
            var yWidth = YAxisConfig != null ? YAxisConfig.width : AxisLineWidth;
            
            bool showX = XAxisConfig != null ? XAxisConfig.visible : true;
            bool showY = YAxisConfig != null ? YAxisConfig.visible : true;

            painter.BeginPath();

            float xAxisY = XAxisId == AxisId.XTop ? 0 : h;
            float yAxisX = YAxisId == AxisId.YRight ? w : 0;

            // X Axis Line
            if (showX)
            {
                painter.strokeColor = xColor;
                painter.lineWidth = xWidth;
                painter.MoveTo(new Vector2(0, xAxisY));
                painter.LineTo(new Vector2(w, xAxisY));
                painter.Stroke(); // Stroke separately to support different colors
                painter.BeginPath();
            }

            // Y Axis Line
            if (showY)
            {
                painter.strokeColor = yColor;
                painter.lineWidth = yWidth;
                painter.MoveTo(new Vector2(yAxisX, 0));
                painter.LineTo(new Vector2(yAxisX, h));
                painter.Stroke();
            }
        }

        private static void ApplyAxisLabelBaseStyle(Label label)
        {
            label.style.marginLeft = 0;
            label.style.marginRight = 0;
            label.style.marginTop = 0;
            label.style.marginBottom = 0;

            label.style.paddingLeft = 0;
            label.style.paddingRight = 0;
            label.style.paddingTop = 0;
            label.style.paddingBottom = 0;

            label.style.borderLeftWidth = 0;
            label.style.borderRightWidth = 0;
            label.style.borderTopWidth = 0;
            label.style.borderBottomWidth = 0;

            label.style.flexGrow = 0;
            label.style.flexShrink = 0;
            label.style.whiteSpace = WhiteSpace.NoWrap;
        }

        private static string FormatNumericTick(float val, string labelFormat)
        {
            if (!string.IsNullOrEmpty(labelFormat)) return val.ToString(labelFormat);

            float v = Mathf.Round(val * 1000f) / 1000f;
            float r = Mathf.Round(v);
            if (Mathf.Abs(v - r) < 0.0001f)
            {
                return ((int)r).ToString();
            }

            return v.ToString("0.###");
        }

        public void RefreshLabels()
        {
            _xAxisContainer.Clear();
            _yAxisContainer.Clear();

            float w = contentRect.width;
            float h = contentRect.height;

            var plotViewport = parent != null ? parent.Q<VisualElement>("plot-viewport") : null;
            float plotX0 = 0f;
            float plotW = w;
            if (plotViewport != null)
            {
                var rs = plotViewport.resolvedStyle;
                plotX0 = plotViewport.layout.x + rs.borderLeftWidth + rs.paddingLeft;
                plotW = plotViewport.contentRect.width;
            }

            bool xOnTop = XAxisId == AxisId.XTop;
            bool yOnRight = YAxisId == AxisId.YRight;
            float xAxisY = xOnTop ? 0 : h;
            float yAxisX = yOnRight ? w : 0;

            // --- X Axis Labels ---
            if (XAxisConfig != null && (XAxisConfig.labelStyle != null ? XAxisConfig.labelStyle.enabled : XAxisConfig.showLabels))
            {
                var xLabelStyle = XAxisConfig.labelStyle;
                var xLabels = XLabels;
                if ((xLabels == null || xLabels.Count == 0) && XAxisConfig != null && XAxisConfig.labels != null && XAxisConfig.labels.Count > 0)
                {
                    xLabels = XAxisConfig.labels;
                }

                if (xLabels != null && xLabels.Count > 0)
                {
                    var sourceLabels = xLabels;
                    if (XAxisConfig != null && XAxisConfig.axisType == AxisType.Category && XAxisConfig.labels != null && XAxisConfig.labels.Count > 0)
                    {
                        sourceLabels = XAxisConfig.labels;
                    }

                    int totalCount = sourceLabels != null ? sourceLabels.Count : 0;
                    if (totalCount <= 0) return;

                    int visibleCount = ClampCategoryVisibleCount(XAxisConfig, totalCount);
                    int startIndex = 0;
                    if (totalCount > visibleCount)
                    {
                        startIndex = Mathf.RoundToInt(XMin);
                        startIndex = Mathf.Clamp(startIndex, 0, totalCount - 1);
                    }

                    for (int i = 0; i < visibleCount; i++)
                    {
                        int labelIndex = (totalCount > visibleCount) ? ((startIndex + i) % totalCount) : i;
                        var label = new Label(sourceLabels[labelIndex]);
                        ApplyAxisLabelBaseStyle(label);
                        int fs = xLabelStyle != null ? xLabelStyle.fontSize : XAxisConfig.fontSize;
                        if (fs > 0) label.style.fontSize = fs;
                        else label.style.fontSize = StyleKeyword.Null;
                        label.style.color = xLabelStyle != null ? xLabelStyle.color : XAxisConfig.labelColor;
                        label.style.position = Position.Absolute;
                        label.style.unityTextAlign = TextAnchor.MiddleCenter;

                        ChartTextStyleApplier.ApplyLabel(label, this, ChartTextRole.AxisLabel);
                        
                        float xPos;
                        if (XAxisConfig != null && XAxisConfig.labelPlacement == CategoryLabelPlacement.CellCenter)
                        {
                            float step = visibleCount > 0 ? (plotW / visibleCount) : 0f;
                            xPos = plotX0 + (i + 0.5f) * step;
                        }
                        else
                        {
                            float denom = Mathf.Max(1, visibleCount - 1);
                            float step = denom > 0 ? (plotW / denom) : 0f;
                            xPos = plotX0 + i * step;
                        }
                        label.style.left = xPos;
                        
                        // Position Vertical (relative to X axis line)
                        var xPosMode = xLabelStyle != null ? xLabelStyle.position : XAxisConfig.labelPosition;
                        if (xPosMode == LabelPosition.Inside)
                        {
                            label.style.top = xOnTop ? xAxisY + 5 : xAxisY - 5;
                            label.style.translate = xOnTop
                                ? new Translate(new Length(-50, LengthUnit.Percent), 0, 0)
                                : new Translate(new Length(-50, LengthUnit.Percent), new Length(-100, LengthUnit.Percent), 0);
                        }
                        else if (xPosMode == LabelPosition.Center)
                        {
                            label.style.top = xAxisY;
                            label.style.translate = new Translate(new Length(-50, LengthUnit.Percent), new Length(-50, LengthUnit.Percent), 0);
                        }
                        else // Outside
                        {
                            label.style.top = xOnTop ? xAxisY - 5 : xAxisY + 5;
                            label.style.translate = xOnTop
                                ? new Translate(new Length(-50, LengthUnit.Percent), new Length(-100, LengthUnit.Percent), 0)
                                : new Translate(new Length(-50, LengthUnit.Percent), 0, 0);
                        }
                            
                        // Apply Offset
                        var xOffset = xLabelStyle != null ? xLabelStyle.offset : XAxisConfig.labelOffset;
                        if (xOffset != Vector2.zero)
                        {
                            label.style.marginLeft = xOffset.x;
                            label.style.marginTop = xOffset.y;
                        }
                        
                        _xAxisContainer.Add(label);
                    }
                }
                else
                {
                    // Numeric X axis labels (used by HorizontalBar)
                    int split = Mathf.Max(1, XSplitCount);

                    bool cellCenter = XAxisConfig != null && XAxisConfig.labelPlacement == CategoryLabelPlacement.CellCenter;
                    int labelCount = cellCenter ? split : (split + 1);
                    for (int i = 0; i < labelCount; i++)
                    {
                        float t = cellCenter ? ((i + 0.5f) / split) : ((float)i / split);
                        float val = Mathf.Lerp(XMin, XMax, t);
                        string text = FormatNumericTick(val, XAxisConfig.labelFormat);

                        var label = new Label(text);
                        ApplyAxisLabelBaseStyle(label);
                        int fs = xLabelStyle != null ? xLabelStyle.fontSize : XAxisConfig.fontSize;
                        if (fs > 0) label.style.fontSize = fs;
                        else label.style.fontSize = StyleKeyword.Null;
                        label.style.color = xLabelStyle != null ? xLabelStyle.color : XAxisConfig.labelColor;
                        label.style.position = Position.Absolute;
                        label.style.unityTextAlign = TextAnchor.MiddleCenter;

                        ChartTextStyleApplier.ApplyLabel(label, this, ChartTextRole.AxisLabel);
                        
                        float xPos = plotX0 + t * plotW;
                        label.style.left = xPos;

                        var xPosMode = xLabelStyle != null ? xLabelStyle.position : XAxisConfig.labelPosition;
                        if (xPosMode == LabelPosition.Inside)
                        {
                            label.style.top = xOnTop ? xAxisY + 5 : xAxisY - 5;
                            label.style.translate = xOnTop
                                ? new Translate(new Length(-50, LengthUnit.Percent), 0, 0)
                                : new Translate(new Length(-50, LengthUnit.Percent), new Length(-100, LengthUnit.Percent), 0);
                        }
                        else if (xPosMode == LabelPosition.Center)
                        {
                            label.style.top = xAxisY;
                            label.style.translate = new Translate(new Length(-50, LengthUnit.Percent), new Length(-50, LengthUnit.Percent), 0);
                        }
                        else
                        {
                            label.style.top = xOnTop ? xAxisY - 5 : xAxisY + 5;
                            label.style.translate = xOnTop
                                ? new Translate(new Length(-50, LengthUnit.Percent), new Length(-100, LengthUnit.Percent), 0)
                                : new Translate(new Length(-50, LengthUnit.Percent), 0, 0);
                        }

                        var xOffset = xLabelStyle != null ? xLabelStyle.offset : XAxisConfig.labelOffset;
                        if (xOffset != Vector2.zero)
                        {
                            label.style.marginLeft = xOffset.x;
                            label.style.marginTop = xOffset.y;
                        }

                        _xAxisContainer.Add(label);
                    }
                }
            }

            if (XAxisConfig != null && XAxisConfig.axisType == AxisType.Value && XAxisConfig.showUnit && !string.IsNullOrEmpty(XAxisConfig.unitText))
            {
                var unitStyle = XAxisConfig.unitLabelStyle;
                var unitLabel = new Label(XAxisConfig.unitText);
                ApplyAxisLabelBaseStyle(unitLabel);
                int fs = unitStyle != null ? unitStyle.fontSize : XAxisConfig.fontSize;
                if (fs > 0) unitLabel.style.fontSize = fs;
                else unitLabel.style.fontSize = StyleKeyword.Null;
                unitLabel.style.color = unitStyle != null ? unitStyle.color : XAxisConfig.labelColor;
                unitLabel.style.position = Position.Absolute;
                unitLabel.style.left = w;
                unitLabel.style.unityTextAlign = TextAnchor.MiddleLeft;

                ChartTextStyleApplier.ApplyLabel(unitLabel, this, ChartTextRole.AxisLabel);

                var xPosMode = unitStyle != null ? unitStyle.position : LabelPosition.Outside;
                if (xPosMode == LabelPosition.Inside)
                {
                    unitLabel.style.top = xOnTop ? xAxisY + 5 : xAxisY - 5;
                    unitLabel.style.translate = xOnTop
                        ? new Translate(0, 0, 0)
                        : new Translate(0, new Length(-100, LengthUnit.Percent), 0);
                }
                else if (xPosMode == LabelPosition.Center)
                {
                    unitLabel.style.top = xAxisY;
                    unitLabel.style.translate = new Translate(0, new Length(-50, LengthUnit.Percent), 0);
                }
                else
                {
                    unitLabel.style.top = xOnTop ? xAxisY - 5 : xAxisY + 5;
                    unitLabel.style.translate = xOnTop
                        ? new Translate(0, new Length(-100, LengthUnit.Percent), 0)
                        : new Translate(0, 0, 0);
                }

                var offset = unitStyle != null ? unitStyle.offset : Vector2.zero;
                if (offset != Vector2.zero)
                {
                    unitLabel.style.marginLeft = offset.x;
                    unitLabel.style.marginTop = offset.y;
                }

                _xAxisContainer.Add(unitLabel);
            }

            // --- Y Axis Labels ---
            if (YAxisConfig != null && (YAxisConfig.labelStyle != null ? YAxisConfig.labelStyle.enabled : YAxisConfig.showLabels))
            {
                var yLabelStyle = YAxisConfig.labelStyle;
                var yLabels = YLabels;
                if ((yLabels == null || yLabels.Count == 0) && YAxisConfig != null && YAxisConfig.labels != null && YAxisConfig.labels.Count > 0)
                {
                    yLabels = YAxisConfig.labels;
                }

                if (yLabels != null && yLabels.Count > 0)
                {
                    var sourceLabels = yLabels;
                    if (YAxisConfig != null && YAxisConfig.axisType == AxisType.Category && YAxisConfig.labels != null && YAxisConfig.labels.Count > 0)
                    {
                        sourceLabels = YAxisConfig.labels;
                    }

                    int totalCount = sourceLabels != null ? sourceLabels.Count : 0;
                    if (totalCount <= 0) return;

                    int visibleCount = ClampCategoryVisibleCount(YAxisConfig, totalCount);
                    int startIndex = 0;
                    if (totalCount > visibleCount)
                    {
                        startIndex = Mathf.RoundToInt(YMin);
                        startIndex = Mathf.Clamp(startIndex, 0, totalCount - 1);
                    }

                    for (int i = 0; i < visibleCount; i++)
                    {
                        int labelIndex = (totalCount > visibleCount) ? ((startIndex + i) % totalCount) : i;
                        var label = new Label(sourceLabels[labelIndex]);
                        ApplyAxisLabelBaseStyle(label);
                        int fs = yLabelStyle != null ? yLabelStyle.fontSize : YAxisConfig.fontSize;
                        if (fs > 0) label.style.fontSize = fs;
                        else label.style.fontSize = StyleKeyword.Null;
                        label.style.color = yLabelStyle != null ? yLabelStyle.color : YAxisConfig.labelColor;
                        label.style.position = Position.Absolute;

                        ChartTextStyleApplier.ApplyLabel(label, this, ChartTextRole.AxisLabel);

                        float t;
                        if (YAxisConfig != null && YAxisConfig.labelPlacement == CategoryLabelPlacement.CellCenter)
                        {
                            t = (i + 0.5f) / Mathf.Max(1, visibleCount);
                        }
                        else
                        {
                            t = visibleCount > 1 ? (float)i / (visibleCount - 1) : 0;
                        }

                        float yPos;
                        if (YIsCategory)
                        {
                            yPos = h > 0 ? (xOnTop ? (t * h) : ((1.0f - t) * h)) : 0;
                        }
                        else
                        {
                            yPos = h > 0 ? (1.0f - t) * h : 0;
                        }
                        label.style.top = yPos;

                        label.style.left = yOnRight ? yAxisX + 5 : yAxisX - 5;
                        label.style.unityTextAlign = yOnRight ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight;
                        label.style.translate = yOnRight
                            ? new Translate(0, new Length(-50, LengthUnit.Percent), 0)
                            : new Translate(new Length(-100, LengthUnit.Percent), new Length(-50, LengthUnit.Percent), 0);

                        var yOffset = yLabelStyle != null ? yLabelStyle.offset : YAxisConfig.labelOffset;
                        if (yOffset != Vector2.zero)
                        {
                            label.style.marginLeft = yOffset.x;
                            label.style.marginTop = yOffset.y;
                        }

                        _yAxisContainer.Add(label);
                    }

                    return;
                }


                bool cellCenter = YAxisConfig != null && YAxisConfig.labelPlacement == CategoryLabelPlacement.CellCenter;
                int labelCount = cellCenter ? Mathf.Max(1, YSplitCount) : (Mathf.Max(1, YSplitCount) + 1);
                for (int i = 0; i < labelCount; i++)
                {
                    float split = Mathf.Max(1, YSplitCount);
                    float t = cellCenter ? ((i + 0.5f) / split) : ((float)i / split);
                    float val = Mathf.Lerp(YMin, YMax, t);
                    string text = FormatNumericTick(val, YAxisConfig.labelFormat);
                    
                    var label = new Label(text);
                    ApplyAxisLabelBaseStyle(label);
                    int fs = yLabelStyle != null ? yLabelStyle.fontSize : YAxisConfig.fontSize;
                    if (fs > 0) label.style.fontSize = fs;
                    else label.style.fontSize = StyleKeyword.Null;
                    label.style.color = yLabelStyle != null ? yLabelStyle.color : YAxisConfig.labelColor;
                    label.style.position = Position.Absolute;

                    ChartTextStyleApplier.ApplyLabel(label, this, ChartTextRole.AxisLabel);
                    
                    // Use pixel position based on actual height
                    float yPos = h > 0 ? (1.0f - t) * h : 0;
                    label.style.top = yPos;
                    
                    // Position Horizontal (relative to Y axis line)
                    var yPosMode = yLabelStyle != null ? yLabelStyle.position : YAxisConfig.labelPosition;
                    if (yPosMode == LabelPosition.Inside)
                    {
                        label.style.left = yOnRight ? yAxisX - 5 : yAxisX + 5;
                        label.style.unityTextAlign = yOnRight ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft;
                        label.style.translate = yOnRight
                            ? new Translate(new Length(-100, LengthUnit.Percent), new Length(-50, LengthUnit.Percent), 0)
                            : new Translate(0, new Length(-50, LengthUnit.Percent), 0);
                    }
                    else if (yPosMode == LabelPosition.Center)
                    {
                        label.style.left = yAxisX;
                        label.style.translate = new Translate(new Length(-50, LengthUnit.Percent), new Length(-50, LengthUnit.Percent), 0);
                    }
                    else // Outside (Default) - left of axis
                    {
                        label.style.left = yOnRight ? yAxisX + 5 : yAxisX - 5;
                        label.style.unityTextAlign = yOnRight ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight;
                        label.style.translate = yOnRight
                            ? new Translate(0, new Length(-50, LengthUnit.Percent), 0)
                            : new Translate(new Length(-100, LengthUnit.Percent), new Length(-50, LengthUnit.Percent), 0);
                    }

                    // Apply Offset
                    var yOffset = yLabelStyle != null ? yLabelStyle.offset : YAxisConfig.labelOffset;
                    if (yOffset != Vector2.zero)
                    {
                        label.style.marginLeft = yOffset.x;
                        label.style.marginTop = yOffset.y;
                    }

                    _yAxisContainer.Add(label);
                }
            }

            if (YAxisConfig != null && YAxisConfig.axisType == AxisType.Value && YAxisConfig.showUnit && !string.IsNullOrEmpty(YAxisConfig.unitText))
            {
                var unitStyle = YAxisConfig.unitLabelStyle;
                var unitLabel = new Label(YAxisConfig.unitText);
                ApplyAxisLabelBaseStyle(unitLabel);
                int fs = unitStyle != null ? unitStyle.fontSize : YAxisConfig.fontSize;
                if (fs > 0) unitLabel.style.fontSize = fs;
                else unitLabel.style.fontSize = StyleKeyword.Null;
                unitLabel.style.color = unitStyle != null ? unitStyle.color : YAxisConfig.labelColor;
                unitLabel.style.position = Position.Absolute;

                ChartTextStyleApplier.ApplyLabel(unitLabel, this, ChartTextRole.AxisLabel);

                unitLabel.style.top = 0;

                var yPosMode = unitStyle != null ? unitStyle.position : LabelPosition.Outside;
                if (yPosMode == LabelPosition.Inside)
                {
                    unitLabel.style.left = yOnRight ? yAxisX - 5 : yAxisX + 5;
                    unitLabel.style.unityTextAlign = yOnRight ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft;
                    unitLabel.style.translate = yOnRight
                        ? new Translate(new Length(-100, LengthUnit.Percent), new Length(-100, LengthUnit.Percent), 0)
                        : new Translate(0, new Length(-100, LengthUnit.Percent), 0);
                }
                else if (yPosMode == LabelPosition.Center)
                {
                    unitLabel.style.left = yAxisX;
                    unitLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                    unitLabel.style.translate = new Translate(new Length(-50, LengthUnit.Percent), new Length(-100, LengthUnit.Percent), 0);
                }
                else
                {
                    unitLabel.style.left = yOnRight ? yAxisX + 5 : yAxisX - 5;
                    unitLabel.style.unityTextAlign = yOnRight ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight;
                    unitLabel.style.translate = yOnRight
                        ? new Translate(0, new Length(-100, LengthUnit.Percent), 0)
                        : new Translate(new Length(-100, LengthUnit.Percent), new Length(-100, LengthUnit.Percent), 0);
                }

                var offset = unitStyle != null ? unitStyle.offset : Vector2.zero;
                if (offset != Vector2.zero)
                {
                    unitLabel.style.marginLeft = offset.x;
                    unitLabel.style.marginTop = offset.y;
                }

                _yAxisContainer.Add(unitLabel);
            }

            MarkDirtyRepaint();
        }
    }
}
