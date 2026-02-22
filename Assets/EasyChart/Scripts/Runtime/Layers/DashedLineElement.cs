using UnityEngine;
using UnityEngine.UIElements;

namespace EasyChart.Layers
{
    internal sealed class DashedLineElement : VisualElement
    {
        private Color _color = Color.yellow;
        private float _lineWidth = 1f;
        private bool _dashed;
        private float _dashLength = 4f;
        private float _dashGap = 2f;
        private float _dashOffset;

        public Color Color
        {
            get => _color;
            set
            {
                if (_color == value) return;
                _color = value;
                MarkDirtyRepaint();
            }
        }

        public float LineWidth
        {
            get => _lineWidth;
            set
            {
                float v = Mathf.Max(0f, value);
                if (Mathf.Approximately(_lineWidth, v)) return;
                _lineWidth = v;
                MarkDirtyRepaint();
            }
        }

        public bool Dashed
        {
            get => _dashed;
            set
            {
                if (_dashed == value) return;
                _dashed = value;
                MarkDirtyRepaint();
            }
        }

        public float DashLength
        {
            get => _dashLength;
            set
            {
                float v = Mathf.Max(0f, value);
                if (Mathf.Approximately(_dashLength, v)) return;
                _dashLength = v;
                MarkDirtyRepaint();
            }
        }

        public float DashGap
        {
            get => _dashGap;
            set
            {
                float v = Mathf.Max(0f, value);
                if (Mathf.Approximately(_dashGap, v)) return;
                _dashGap = v;
                MarkDirtyRepaint();
            }
        }

        public float DashOffset
        {
            get => _dashOffset;
            set
            {
                if (Mathf.Approximately(_dashOffset, value)) return;
                _dashOffset = value;
                MarkDirtyRepaint();
            }
        }

        public DashedLineElement()
        {
            pickingMode = PickingMode.Ignore;
            generateVisualContent += OnGenerateVisualContent;
        }

        private void OnGenerateVisualContent(MeshGenerationContext context)
        {
            var width = contentRect.width;
            var height = contentRect.height;
            if (width <= 0 || height <= 0) return;

            var painter = context.painter2D;
            painter.lineWidth = Mathf.Max(1f, LineWidth);
            painter.strokeColor = Color;

            bool isVertical = height >= width;
            if (isVertical)
            {
                float x = width * 0.5f;
                var a = new Vector2(x, 0);
                var b = new Vector2(x, height);

                if (Dashed) DashedLineUtils.DrawDashedLine(painter, a, b, DashLength, DashGap, DashOffset);
                else
                {
                    painter.BeginPath();
                    painter.MoveTo(a);
                    painter.LineTo(b);
                    painter.Stroke();
                }
            }
            else
            {
                float y = height * 0.5f;
                var a = new Vector2(0, y);
                var b = new Vector2(width, y);

                if (Dashed) DashedLineUtils.DrawDashedLine(painter, a, b, DashLength, DashGap, DashOffset);
                else
                {
                    painter.BeginPath();
                    painter.MoveTo(a);
                    painter.LineTo(b);
                    painter.Stroke();
                }
            }
        }
    }
}
