using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace EasyChart.Layers
{
    public static class RoundedRectUtils
    {
        private static readonly List<Vector2> s_tempRoundedPoints = new List<Vector2>(64);

        public static void BuildRoundedRectPoints(List<Vector2> points, Rect rect, float radius, int segments, bool roundTL, bool roundTR, bool roundBR, bool roundBL)
        {
            if (points == null) return;
            points.Clear();

            float x = rect.xMin;
            float y = rect.yMin;
            float w = rect.width;
            float h = rect.height;

            if (w <= 0f || h <= 0f)
            {
                return;
            }

            float r = Mathf.Max(0f, radius);
            r = Mathf.Min(r, Mathf.Min(w, h) * 0.5f);
            int seg = Mathf.Clamp(segments, 1, 16);

            void AddPoint(Vector2 p)
            {
                if (points.Count > 0)
                {
                    var last = points[points.Count - 1];
                    if (Mathf.Approximately(last.x, p.x) && Mathf.Approximately(last.y, p.y)) return;
                }
                points.Add(p);
            }

            void AddArc(Vector2 center, float startDeg, float endDeg)
            {
                for (int i = 1; i <= seg; i++)
                {
                    float t = (float)i / seg;
                    float a = Mathf.Deg2Rad * Mathf.Lerp(startDeg, endDeg, t);
                    float px = center.x + Mathf.Cos(a) * r;
                    float py = center.y + Mathf.Sin(a) * r;
                    AddPoint(new Vector2(px, py));
                }
            }

            bool useTL = roundTL && r > 0f;
            bool useTR = roundTR && r > 0f;
            bool useBR = roundBR && r > 0f;
            bool useBL = roundBL && r > 0f;

            AddPoint(new Vector2(x + (useTL ? r : 0f), y));
            AddPoint(new Vector2(x + w - (useTR ? r : 0f), y));
            if (useTR) AddArc(new Vector2(x + w - r, y + r), 270f, 360f);
            else AddPoint(new Vector2(x + w, y));

            AddPoint(new Vector2(x + w, y + h - (useBR ? r : 0f)));
            if (useBR) AddArc(new Vector2(x + w - r, y + h - r), 0f, 90f);
            else AddPoint(new Vector2(x + w, y + h));

            AddPoint(new Vector2(x + (useBL ? r : 0f), y + h));
            if (useBL) AddArc(new Vector2(x + r, y + h - r), 90f, 180f);
            else AddPoint(new Vector2(x, y + h));

            AddPoint(new Vector2(x, y + (useTL ? r : 0f)));
            if (useTL) AddArc(new Vector2(x + r, y + r), 180f, 270f);
            else AddPoint(new Vector2(x, y));
        }

        public static void BeginRoundedRectPath(Painter2D painter, Rect rect, float radius, int segments, bool roundTL, bool roundTR, bool roundBR, bool roundBL)
        {
            if (painter == null) return;

            s_tempRoundedPoints.Clear();
            BuildRoundedRectPoints(s_tempRoundedPoints, rect, radius, segments, roundTL, roundTR, roundBR, roundBL);
            if (s_tempRoundedPoints.Count == 0) return;

            painter.BeginPath();
            painter.MoveTo(s_tempRoundedPoints[0]);
            for (int i = 1; i < s_tempRoundedPoints.Count; i++)
            {
                painter.LineTo(s_tempRoundedPoints[i]);
            }
            painter.ClosePath();
        }
    }
}
