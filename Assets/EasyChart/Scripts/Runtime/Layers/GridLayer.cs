using UnityEngine;
using UnityEngine.UIElements;

namespace EasyChart.Layers
{
    public class GridLayer : VisualElement
    {
        public int HorizontalGridCount { get; set; } = 5;
        public int VerticalGridCount { get; set; } = 5;

        public bool VerticalPreload { get; set; } = false;
        public bool HorizontalPreload { get; set; } = false;

        public Color XGridColor { get; set; } = new Color(0.5f, 0.5f, 0.5f, 0.2f);
        public float XGridLineWidth { get; set; } = 1.0f;
        public bool XGridDashed { get; set; } = false;
        public float XGridDashLength { get; set; } = 4f;
        public float XGridDashGap { get; set; } = 2f;
        public float XGridDashOffset { get; set; } = 0f;
        public Color YGridColor { get; set; } = new Color(0.5f, 0.5f, 0.5f, 0.2f);
        public float YGridLineWidth { get; set; } = 1.0f;
        public bool YGridDashed { get; set; } = false;
        public float YGridDashLength { get; set; } = 4f;
        public float YGridDashGap { get; set; } = 2f;
        public float YGridDashOffset { get; set; } = 0f;

        public GridLayer()
        {
            // Make it fill the parent
            this.StretchToParentSize();
            pickingMode = PickingMode.Ignore; // Background doesn't need to block clicks
            style.overflow = Overflow.Visible; // Allow drawing outside contentRect for preload
            generateVisualContent += OnGenerateVisualContent;
        }

        private void OnGenerateVisualContent(MeshGenerationContext context)
        {
            var width = contentRect.width;
            var height = contentRect.height;
            
            if (width <= 0 || height <= 0) return;

            var painter = context.painter2D;

            // Calculate extended width/height for preload
            float extendedWidth = width;
            float extendedHeight = height;
            if (VerticalPreload && VerticalGridCount > 0)
            {
                float vStep = width / VerticalGridCount;
                extendedWidth = width + vStep;
            }
            if (HorizontalPreload && HorizontalGridCount > 0)
            {
                float hStep = height / HorizontalGridCount;
                extendedHeight = height + hStep;
            }

            // Draw Horizontal Lines (Y Axis steps)
            // Usually split into N parts means N+1 lines? Or N lines?
            // For Y axis, usually 0..Count means Count+1 lines including 0 and Max.
            if (HorizontalGridCount > 0)
            {
                painter.lineWidth = YGridLineWidth;
                painter.strokeColor = YGridColor;
                float hStep = height / HorizontalGridCount;
                // When preload is enabled, draw one extra line outside the visible area
                int lineCount = HorizontalGridCount + (HorizontalPreload ? 1 : 0);
                for (int i = 0; i <= lineCount; i++)
                {
                    float y = i * hStep;
                    // Use extendedWidth so horizontal lines cover the preload area
                    var a = new Vector2(0, y);
                    var b = new Vector2(extendedWidth, y);

                    if (YGridDashed)
                    {
                        DashedLineUtils.DrawDashedLine(painter, a, b, YGridDashLength, YGridDashGap, YGridDashOffset);
                    }
                    else
                    {
                        painter.BeginPath();
                        painter.MoveTo(a);
                        painter.LineTo(b);
                        painter.Stroke();
                    }
                }
            }

            // Draw Vertical Lines (X Axis steps)
            // For Category Axis, we want lines exactly at 0, 1, 2... (LabelCount - 1)
            // So VerticalGridCount should be set to (LabelCount > 1 ? LabelCount - 1 : 1) 
            // to match the logic: width / Count * i
            // BUT wait: if we have 5 labels (0,1,2,3,4), we want 5 lines at 0%, 25%, 50%, 75%, 100%.
            // This corresponds to dividing width by 4 (Count-1).
            
            if (VerticalGridCount > 0)
            {
                painter.lineWidth = XGridLineWidth;
                painter.strokeColor = XGridColor;
                // If VerticalGridCount means "Number of segments", step = width / Count.
                // If VerticalGridCount means "Number of labels/lines - 1", step = width / Count.
                // Let's standardize: VerticalGridCount passed in will be (LabelCount - 1).
                // If LabelCount=1, GridCount=0 (avoid div by zero).
                
                float vStep = width / VerticalGridCount;
                // When preload is enabled, draw one extra line outside the visible area
                int lineCount = VerticalGridCount + (VerticalPreload ? 1 : 0);
                
                for (int i = 0; i <= lineCount; i++)
                {
                    float x = i * vStep;
                    // Use extendedHeight so vertical lines cover the preload area
                    var a = new Vector2(x, 0);
                    var b = new Vector2(x, extendedHeight);

                    if (XGridDashed)
                    {
                        DashedLineUtils.DrawDashedLine(painter, a, b, XGridDashLength, XGridDashGap, XGridDashOffset);
                    }
                    else
                    {
                        painter.BeginPath();
                        painter.MoveTo(a);
                        painter.LineTo(b);
                        painter.Stroke();
                    }
                }
            }
            else if (VerticalGridCount == 0) // Special case: Single line or just edges?
            {
                 painter.lineWidth = XGridLineWidth;
                 painter.strokeColor = XGridColor;
                 // Maybe just draw left and right edge?
                 // For now do nothing or draw at 0.
                 var a = new Vector2(0, 0);
                 var b = new Vector2(0, height);

                 if (XGridDashed)
                 {
                     DashedLineUtils.DrawDashedLine(painter, a, b, XGridDashLength, XGridDashGap, XGridDashOffset);
                 }
                 else
                 {
                     painter.BeginPath();
                     painter.MoveTo(a);
                     painter.LineTo(b);
                     painter.Stroke();
                 }
            }
        }

        public void Redraw()
        {
            MarkDirtyRepaint();
        }

        public void SetScrollOffset(float xPx, float yPx)
        {
            style.translate = new Translate(xPx, yPx, 0);
        }
    }
}
