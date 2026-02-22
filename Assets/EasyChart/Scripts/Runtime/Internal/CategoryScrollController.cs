using UnityEngine;

namespace EasyChart
{
    internal sealed class CategoryScrollController
    {
        public int WindowStartX { get; set; }
        public int WindowStartY { get; set; }

        public float ScrollTimeX { get; set; }
        public float ScrollTimeY { get; set; }

        public float ScrollOffsetX { get; private set; }
        public float ScrollOffsetY { get; private set; }

        public bool SmoothTranslating { get; private set; }

        internal struct UpdateResult
        {
            public bool NeedsRangeUpdate;
            public bool NeedsWindowRefresh;
        }

        public void ResetOffsets()
        {
            ScrollOffsetX = 0f;
            ScrollOffsetY = 0f;
            SmoothTranslating = false;
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

        public UpdateResult Update(AxisId xAxisId, AxisConfig xAxis, AxisConfig yAxis, float plotW, float plotH, float dt)
        {
            var result = new UpdateResult();

            float xOffset = 0f;
            float yOffset = 0f;
            bool smoothActive = false;
            bool needsWindowRefresh = false;
            bool needsRangeUpdate = false;

            if (xAxis != null && xAxis.axisType == AxisType.Category && xAxis.labels != null && xAxis.labels.Count > 0)
            {
                int count = xAxis.labels.Count;
                int visible = ClampCategoryVisibleCount(xAxis, count);
                bool overflow = count > visible;

                if (!overflow)
                {
                    if (WindowStartX != 0)
                    {
                        WindowStartX = 0;
                        needsRangeUpdate = true;
                        needsWindowRefresh = true;
                    }
                    ScrollTimeX = 0f;
                }
                else
                {
                    bool autoScroll = xAxis.categoryAutoScroll;
                    bool smooth = xAxis.categorySmoothScroll;
                    float interval = xAxis.categoryScrollInterval;
                    if (interval <= 0.01f) interval = 0.01f;
                    int step = xAxis.categoryScrollStep;
                    if (step < 1) step = 1;

                    if (!autoScroll)
                    {
                        if (WindowStartX != 0)
                        {
                            WindowStartX = 0;
                            ScrollTimeX = 0f;
                            needsRangeUpdate = true;
                            needsWindowRefresh = true;
                        }
                    }
                    else
                    {
                        ScrollTimeX += dt;

                        if (ScrollTimeX >= interval)
                        {
                            ScrollTimeX -= interval;
                            if (ScrollTimeX < 0f) ScrollTimeX = 0f;

                            WindowStartX = (WindowStartX + step) % count;
                            needsRangeUpdate = true;
                            needsWindowRefresh = true;
                        }

                        if (smooth)
                        {
                            if (plotW > 0f)
                            {
                                bool cellCenter = xAxis.labelPlacement == CategoryLabelPlacement.CellCenter;
                                float stepPx = cellCenter ? (plotW / visible) : (plotW / Mathf.Max(1, visible - 1));
                                float t01 = Mathf.Clamp01(ScrollTimeX / interval);
                                xOffset = -t01 * step * stepPx;
                                smoothActive = true;
                            }
                        }
                    }
                }
            }
            else
            {
                if (WindowStartX != 0)
                {
                    WindowStartX = 0;
                    needsRangeUpdate = true;
                    needsWindowRefresh = true;
                }
                ScrollTimeX = 0f;
            }

            if (yAxis != null && yAxis.axisType == AxisType.Category && yAxis.labels != null && yAxis.labels.Count > 0)
            {
                int count = yAxis.labels.Count;
                int visible = ClampCategoryVisibleCount(yAxis, count);
                bool overflow = count > visible;

                if (!overflow)
                {
                    if (WindowStartY != 0)
                    {
                        WindowStartY = 0;
                        needsRangeUpdate = true;
                        needsWindowRefresh = true;
                    }
                    ScrollTimeY = 0f;
                }
                else
                {
                    bool autoScroll = yAxis.categoryAutoScroll;
                    bool smooth = yAxis.categorySmoothScroll;
                    float interval = yAxis.categoryScrollInterval;
                    if (interval <= 0.01f) interval = 0.01f;
                    int step = yAxis.categoryScrollStep;
                    if (step < 1) step = 1;

                    if (!autoScroll)
                    {
                        if (WindowStartY != 0)
                        {
                            WindowStartY = 0;
                            ScrollTimeY = 0f;
                            needsRangeUpdate = true;
                            needsWindowRefresh = true;
                        }
                    }
                    else
                    {
                        ScrollTimeY += dt;

                        if (ScrollTimeY >= interval)
                        {
                            ScrollTimeY -= interval;
                            if (ScrollTimeY < 0f) ScrollTimeY = 0f;

                            WindowStartY = (WindowStartY + step) % count;
                            needsRangeUpdate = true;
                            needsWindowRefresh = true;
                        }

                        if (smooth)
                        {
                            if (plotH > 0f)
                            {
                                bool cellCenter = yAxis.labelPlacement == CategoryLabelPlacement.CellCenter;
                                float stepPx = cellCenter ? (plotH / visible) : (plotH / Mathf.Max(1, visible - 1));
                                float t01 = Mathf.Clamp01(ScrollTimeY / interval);

                                bool xOnTop = xAxisId == AxisId.XTop;
                                float sign = xOnTop ? -1f : 1f;
                                yOffset = sign * t01 * step * stepPx;
                                smoothActive = true;
                            }
                        }
                    }
                }
            }
            else
            {
                if (WindowStartY != 0)
                {
                    WindowStartY = 0;
                    needsRangeUpdate = true;
                    needsWindowRefresh = true;
                }
                ScrollTimeY = 0f;
            }

            ScrollOffsetX = xOffset;
            ScrollOffsetY = yOffset;
            SmoothTranslating = smoothActive && (Mathf.Abs(xOffset) > 0.01f || Mathf.Abs(yOffset) > 0.01f);

            result.NeedsRangeUpdate = needsRangeUpdate;
            result.NeedsWindowRefresh = needsWindowRefresh;
            return result;
        }
    }
}
