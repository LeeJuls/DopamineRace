using System;
using System.Collections.Generic;
using UnityEngine;

namespace EasyChart
{
    public sealed class DefaultChartLayoutModel : IChartLayoutModel
    {
        public AxisId GetMappedXAxisId(ChartData data)
        {
            return data != null && data.Cartesian != null ? data.Cartesian.xAxisId : AxisId.XBottom;
        }

        public AxisId GetMappedYAxisId(ChartData data)
        {
            return data != null && data.Cartesian != null ? data.Cartesian.yAxisId : AxisId.YLeft;
        }

        public bool IsCartesianTransposed(ChartData data, Func<AxisId, AxisConfig> getAxis)
        {
            if (data == null || getAxis == null) return false;

            AxisId xAxisId = GetMappedXAxisId(data);
            AxisId yAxisId = GetMappedYAxisId(data);

            var xAxis = getAxis(xAxisId);
            var yAxis = getAxis(yAxisId);

            var xDim = xAxis != null ? xAxis.axisType : AxisType.Category;
            var yDim = yAxis != null ? yAxis.axisType : AxisType.Value;
            return xDim == AxisType.Value && yDim == AxisType.Category;
        }

        public ChartRangeResult CalculateRange(ChartData data, Func<AxisId, AxisConfig> getAxis, int categoryWindowStartX, int categoryWindowStartY)
        {
            AxisId xAxisId = GetMappedXAxisId(data);
            AxisId yAxisId = GetMappedYAxisId(data);

            AxisConfig xAxisCfg = getAxis != null ? getAxis(xAxisId) : null;
            AxisConfig yAxisCfg = getAxis != null ? getAxis(yAxisId) : null;

            var xCategoryLabels = xAxisCfg != null ? xAxisCfg.labels : null;
            var yCategoryLabels = yAxisCfg != null ? yAxisCfg.labels : null;

            bool transposed = IsCartesianTransposed(data, getAxis);

            return ChartRangeCalculator.Calculate(
                data,
                transposed,
                xAxisCfg,
                yAxisCfg,
                xCategoryLabels,
                yCategoryLabels,
                categoryWindowStartX,
                categoryWindowStartY);
        }

        public void UpdateCategoryAxisRangeOnly(
            ChartData data,
            Func<AxisId, AxisConfig> getAxis,
            ref int categoryWindowStartX,
            ref int categoryWindowStartY,
            ref float xMin,
            ref float xMax,
            ref float yMin,
            ref float yMax)
        {
            if (data == null) return;
            if (data.CoordinateSystem != CoordinateSystemType.Cartesian2D) return;
            if (getAxis == null) return;

            var xAxisId = GetMappedXAxisId(data);
            var yAxisId = GetMappedYAxisId(data);
            var xAxis = getAxis(xAxisId);
            var yAxis = getAxis(yAxisId);

            if (xAxis != null && xAxis.axisType == AxisType.Category && xAxis.labels != null && xAxis.labels.Count > 0)
            {
                int count = xAxis.labels.Count;
                int visible = ClampCategoryVisibleCount(xAxis, count);
                if (count <= visible)
                {
                    categoryWindowStartX = 0;
                    xMin = 0;
                    xMax = count > 1 ? count - 1 : 1;
                }
                else
                {
                    categoryWindowStartX = Mathf.Clamp(categoryWindowStartX, 0, count - 1);
                    xMin = categoryWindowStartX;
                    xMax = categoryWindowStartX + (visible - 1);
                }
            }

            if (yAxis != null && yAxis.axisType == AxisType.Category && yAxis.labels != null && yAxis.labels.Count > 0)
            {
                int count = yAxis.labels.Count;
                int visible = ClampCategoryVisibleCount(yAxis, count);
                if (count <= visible)
                {
                    categoryWindowStartY = 0;
                    yMin = 0;
                    yMax = count > 1 ? count - 1 : 1;
                }
                else
                {
                    categoryWindowStartY = Mathf.Clamp(categoryWindowStartY, 0, count - 1);
                    yMin = categoryWindowStartY;
                    yMax = categoryWindowStartY + (visible - 1);
                }
            }
        }

        public List<string> GetCategoryLabelsWindowed(AxisId id, AxisConfig axis, int windowStart, List<string> buffer)
        {
            if (axis == null || axis.axisType != AxisType.Category) return null;
            var labels = axis.labels;
            if (labels == null || labels.Count == 0) return labels;

            int visible = ClampCategoryVisibleCount(axis, labels.Count);
            if (visible <= 0 || labels.Count <= visible) return labels;

            return BuildWindowedCategoryLabels(labels, windowStart, visible, buffer);
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

        private static List<string> BuildWindowedCategoryLabels(List<string> all, int start, int visible, List<string> buffer)
        {
            if (all == null || all.Count == 0) return all;
            if (visible <= 0) return all;
            if (visible >= all.Count) return all;
            if (buffer == null)
            {
                buffer = new List<string>(visible);
            }

            if (start < 0) start = 0;
            if (start >= all.Count) start = all.Count - 1;

            buffer.Clear();
            for (int i = 0; i < visible; i++)
            {
                int idx = (start + i) % all.Count;
                buffer.Add(all[idx]);
            }
            return buffer;
        }
    }
}
