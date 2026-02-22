using UnityEngine;
using UnityEngine.UIElements;

namespace EasyChart.Layers
{
    internal static class DashedLineUtils
    {
        public static void DrawDashedLine(Painter2D painter, Vector2 start, Vector2 end, float dashLength, float gapLength, float offset)
        {
            if (painter == null) return;

            Vector2 delta = end - start;
            float len = delta.magnitude;
            if (len <= 0.0001f) return;

            float period = dashLength + gapLength;
            if (dashLength <= 0f || gapLength < 0f || period <= 0f)
            {
                painter.BeginPath();
                painter.MoveTo(start);
                painter.LineTo(end);
                painter.Stroke();
                return;
            }

            Vector2 dir = delta / len;
            float normalizedOffset = offset;
            if (!Mathf.Approximately(period, 0f))
            {
                normalizedOffset = offset % period;
                if (normalizedOffset < 0f) normalizedOffset += period;
            }

            float pos = -normalizedOffset;
            while (pos < len)
            {
                float segStart = Mathf.Max(pos, 0f);
                float segEnd = Mathf.Min(pos + dashLength, len);

                if (segEnd > segStart)
                {
                    Vector2 a = start + dir * segStart;
                    Vector2 b = start + dir * segEnd;
                    painter.BeginPath();
                    painter.MoveTo(a);
                    painter.LineTo(b);
                    painter.Stroke();
                }

                pos += period;
            }
        }
    }
}
